"""A10 / FX-SLAM — gerador determinístico do impacto do Slam do Guardião (Eldoria).

Desenha 8 quadros 256×256 de um efeito de chão, em vista ortográfica de cima, para um quad
deitado no piso: um anel baixo e fino de poeira de pedra que se expande do pé do Guardião até
o raio real do golpe, com borda laranja apagada, e seis lascas angulares de pedra que saem do
centro e somem. O centro fica aberto, o último quadro é vazio e não sobra área no chão.

Escala: o quad é dimensionado pelo raio REAL do Slam (BossData.slamRadius × multiplicador da
fase). Na célula, 1 R = UNIT_PX px a partir do centro; o anel termina em 0,985 R. Assim a mesma
sheet serve para 3,5 m (fase 1) e 4,55 m (fase 2): lado do quad = 2 R × CELL / (2 UNIT_PX).

Polígonos com supersampling 8× e redução com alpha pré-multiplicado (NumPy). Sem IA generativa
nem asset de terceiros.

Uso (Python 3.11 + Pillow 13 + NumPy 2):
    python Tools/FxSlamGenerator/generate.py            # sheet, .meta (só se faltar), Source~
    python Tools/FxSlamGenerator/generate.py --out DIR  # só sheet/quadros/checagens em DIR

Sai com código 1 se alguma verificação falhar.
"""
from __future__ import annotations

import argparse
import colorsys
import hashlib
import json
import math
import re
import subprocess
import sys
import uuid
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ASSET_ID = "FX-SLAM"
STAGING = ROOT / "Assets/_Project/Art/ProductionStaging" / ASSET_ID
SOURCE = STAGING / "Source~"
SHEET_NAME = f"{ASSET_ID}-Sheet.png"
BOSS_DATA = ROOT / "Assets/_Project/ScriptableObjects/Enemies/Boss_Warden_Data.asset"
READ_ONLY = [
    "Assets/_Project/Scripts/Boss/BossBrain.cs",
    "Assets/_Project/Scripts/Boss/BossAttackTelegraph.cs",
    "Assets/_Project/Scripts/Boss/BossData.cs",
    "Assets/_Project/ScriptableObjects/Enemies/Boss_Warden_Data.asset",
]

# Captura real da arena do Boss (F08, 2560×1440): Hero parado no piso aberto.
ARENA_CAPTURE = ROOT / "Reports/F08-AllRooms/Visual02/boss-opened.png"
ARENA_FEET = (1280, 725)  # centro das botas do Hero na captura
ARENA_PPM = 122.5  # px por metro no piso perto do Hero (ladrilho de 2 m ≈ 245 px na horizontal)
ARENA_HERO_BOX = (1195, 470, 1372, 738)  # Hero + baú atrás dele (objetos opacos à frente do quad)
SLAM_CENTRE_AHEAD_M = 2.0  # Guardião 2 m à frente do Hero (dentro de 0,85 R, condição do Slam)
TELEGRAPH_OUTER = (0.35, 0.05, 0.05)  # Mat_TelegraphOuter (DungeonFloorSceneBuilder)
TELEGRAPH_FILL = (1.0, 0.2, 0.15)  # Mat_TelegraphFill

S = 8  # supersampling
CELL = 256
COLS, ROWS = 4, 2
FRAMES = COLS * ROWS
DURATION = 0.35  # s (ficha A10)
CENTRE = (128.0, 128.0)
UNIT_PX = 116.0  # 1 R (raio real do Slam) em px da célula
PITCH = math.radians(45.0)
LIGHT = (-0.6, -0.8)  # luz de cima/esquerda na imagem (como A04/A09 e a UI)

# ---------------------------------------------------------------- paleta (sRGB)
PAL = {
    "stone": "#55596B",  # pedra da masmorra (ficha)
    "stone_dark": "#3E4152",
    "stone_light": "#7C8194",
    "dust": "#A2A6B4",  # poeira clara da mesma pedra (sat. baixa, matiz 230°)
    "dust_shade": "#6E7385",
    "rust": "#8A4A22",  # ferrugem (A09)
    "ember_muted": "#C4773D",  # laranja apagado: #FF9A2E dessaturado e escurecido
}

# ---------------------------------------------------------------- parâmetros editáveis
# Anel: raio médio, largura e alpha por quadro (em R). Quadro 7 é vazio.
RING = {
    "segments": 28,
    "radius_mid": [0.34, 0.56, 0.71, 0.815, 0.885, 0.935, 0.965],
    "width": [0.052, 0.062, 0.064, 0.060, 0.052, 0.042, 0.032],
    "alpha": [0.86, 0.94, 0.86, 0.70, 0.50, 0.30, 0.13],
    "edge_orange": [1.00, 0.90, 0.72, 0.52, 0.34, 0.18, 0.06],  # força do laranja na borda de fora
    "jitter_out": 0.014,  # recorte facetado da borda (fração de R), determinístico
    "jitter_in": 0.010,
    # Bandas de fora para dentro (fração da largura): borda laranja, núcleo de poeira (alpha
    # alto no centro da faixa), rastro mais escuro e mais fraco.
    "bands": [(0.00, 0.22, "edge", 0.80), (0.22, 0.72, "core", 1.00), (0.72, 1.00, "trail", 0.55)],
}
# Lascas: (ângulo°, u inicial, u final, tamanho em R, rotação inicial°, giro°, nº vértices)
CHIPS = [
    (18.0, 0.32, 0.66, 0.066, 10.0, 70.0, 5),
    (84.0, 0.33, 0.55, 0.054, 40.0, -55.0, 4),
    (141.0, 0.32, 0.72, 0.070, -20.0, 85.0, 5),
    (203.0, 0.34, 0.60, 0.058, 65.0, -40.0, 4),
    (262.0, 0.32, 0.69, 0.062, 5.0, 60.0, 5),
    (318.0, 0.33, 0.52, 0.050, -35.0, -75.0, 4),
]
CHIP = {
    "alpha": [1.0, 1.0, 1.0, 0.92, 0.70, 0.40, 0.12],  # por quadro; 7 = 0
    "travel": [0.10, 0.42, 0.64, 0.79, 0.89, 0.95, 0.99],  # fração do percurso, ease-out
    "lift": 0.22,  # aumento de escala no topo do arco (lasca no ar)
    "shrink_end": 0.30,  # encolhe no fim
    "shadow_alpha": 0.30,
    "shadow_offset": (0.020, 0.022),  # em R, para baixo/direita (contra a luz)
    "rim_alpha": 0.62,  # aresta voltada ao centro, aquecida pelo impacto
    "elongation": 1.55,  # lasca alongada (estilhaço), não pastilha
}
# Tufos de poeira: aglomerados quebrados logo atrás da frente do anel (não entram no centroide do anel).
PUFF = {
    "keep_above": 0.30,  # hash por segmento: ~70% dos segmentos têm tufo
    "depth_R": (0.035, 0.105),  # profundidade radial mín./máx.
    "grow": 0.6,  # o tufo se alonga ao longo do efeito
    "alpha": 0.40,  # × alpha do anel no quadro
}
CENTRE_OPEN_R = 0.22  # nada desenhado dentro deste raio (pé do Guardião, piso legível)
MAX_COVER_50 = 0.10  # fração máx. da célula com alpha > 50%
MAX_MEAN_ALPHA_IN_R = 0.16  # alpha médio máx. dentro de R (piso continua legível)


def rgb(h: str) -> tuple[int, int, int]:
    h = h.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def mix(a, b, t):
    a = rgb(a) if isinstance(a, str) else a
    b = rgb(b) if isinstance(b, str) else b
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


def hash01(*key) -> float:
    """Ruído determinístico em [0, 1) (sem RNG global)."""
    h = hashlib.sha256(("A10/" + "/".join(str(k) for k in key)).encode()).digest()
    return int.from_bytes(h[:4], "little") / 2 ** 32


def to_px(u: float, v: float) -> tuple[float, float]:
    """(u, v) em R, u para a direita (+X) e v para longe da câmera (+Z) → px da célula."""
    return CENTRE[0] + u * UNIT_PX, CENTRE[1] - v * UNIT_PX


def shade(normal_xy, base, lift=0.30, drop=0.22):
    """Tom chapado de faceta pela normal (x direita, y para cima na imagem)."""
    nx, ny = normal_xy
    ln = math.hypot(nx, ny) or 1.0
    d = -(nx / ln * LIGHT[0] + (-ny / ln) * LIGHT[1])  # LIGHT em coords de imagem (y para baixo)
    if d >= 0:
        return mix(base, "#FFFFFF", lift * d * 0.5)
    return mix(base, "#000000", drop * -d)


# ---------------------------------------------------------------- camada (NumPy, supersampled)
class Layer:
    """RGBA float pré-multiplicado em alta resolução; 'over' recortado ao bbox do polígono."""

    def __init__(self):
        self.a = np.zeros((CELL * S, CELL * S, 4), np.float32)

    def poly(self, pts, colour, alpha):
        if alpha <= 0:
            return
        P = [(x * S, y * S) for x, y in pts]
        x0 = max(int(math.floor(min(p[0] for p in P))) - 1, 0)
        y0 = max(int(math.floor(min(p[1] for p in P))) - 1, 0)
        x1 = min(int(math.ceil(max(p[0] for p in P))) + 2, CELL * S)
        y1 = min(int(math.ceil(max(p[1] for p in P))) + 2, CELL * S)
        if x1 <= x0 or y1 <= y0:
            return
        m = Image.new("L", (x1 - x0, y1 - y0), 0)
        ImageDraw.Draw(m).polygon([(x - x0, y - y0) for x, y in P], fill=255)
        cov = np.asarray(m, np.float32)[..., None] / 255.0 * alpha
        src = np.concatenate([np.array(colour, np.float32) / 255.0 * cov, cov], axis=2)
        dst = self.a[y0:y1, x0:x1]
        self.a[y0:y1, x0:x1] = src + dst * (1.0 - src[..., 3:4])

    def downsample(self) -> Image.Image:
        """BOX S×S em pré-multiplicado → RGBA reto; RGB zerado onde alpha = 0."""
        n = CELL
        small = self.a.reshape(n, S, n, S, 4).mean(axis=(1, 3))
        alpha = small[..., 3:4]
        a8 = np.round(alpha * 255).astype(np.uint8)
        rgb_ = np.where(alpha > 0, small[..., :3] / np.maximum(alpha, 1e-6), 0.0)
        rgb8 = np.clip(np.round(rgb_ * 255), 0, 255).astype(np.uint8)
        rgb8[a8[..., 0] == 0] = 0
        return Image.fromarray(np.concatenate([rgb8, a8], axis=2), "RGBA")


# ---------------------------------------------------------------- anel de poeira
def ring_radii(k: int, i: int, frac: float) -> float:
    """Raio (em R) da aresta a 'frac' da largura (0 = borda de fora, 1 = de dentro) no vértice i."""
    seg = RING["segments"]
    i %= seg // 2  # recorte simétrico em ponto: o centroide fica no centro (sem 'shake' embutido)
    mid, w = RING["radius_mid"][k], RING["width"][k]
    r_out = mid + w / 2 + (hash01("out", i) - 0.5) * 2 * RING["jitter_out"] * (w / 0.06)
    r_in = mid - w / 2 + (hash01("in", i) - 0.5) * 2 * RING["jitter_in"] * (w / 0.06)
    r_out = min(r_out, 0.985)  # nunca passa do raio real
    return r_out + (r_in - r_out) * frac


def draw_ring(layer: Layer, k: int):
    if k >= len(RING["radius_mid"]):
        return
    seg = RING["segments"]
    alpha = RING["alpha"][k]
    orange = RING["edge_orange"][k]
    for i in range(seg):
        a0 = 2 * math.pi * i / seg + math.radians(4.0)  # deslocado: nenhuma aresta alinhada ao eixo
        a1 = 2 * math.pi * (i + 1) / seg + math.radians(4.0)
        am = (a0 + a1) / 2
        tone = 0.92 + 0.16 * hash01("tone", i)  # alternância sutil de facetas
        for f0, f1, kind, amul in RING["bands"]:
            pts = []
            for a, idx in ((a0, i), (a1, i + 1)):
                r = ring_radii(k, idx, f0)
                pts.append(to_px(r * math.cos(a), r * math.sin(a)))
            for a, idx in ((a1, i + 1), (a0, i)):
                r = ring_radii(k, idx, f1)
                pts.append(to_px(r * math.cos(a), r * math.sin(a)))
            normal = (math.cos(am), math.sin(am))  # para fora, y para cima
            if kind == "edge":
                base = mix(PAL["dust"], PAL["ember_muted"], orange)
                col = shade(normal, base, 0.18, 0.12)
            elif kind == "core":
                base = mix(PAL["dust"], PAL["stone_light"], 0.15 + 0.35 * (k / 6))
                col = shade(normal, base, 0.22, 0.18)
            else:
                base = mix(PAL["dust_shade"], PAL["stone"], 0.3 + 0.4 * (k / 6))
                col = shade((-normal[0], -normal[1]), base, 0.14, 0.12)
            col = tuple(min(255, round(c * tone)) for c in col)
            layer.poly(pts, col, alpha * amul)


def draw_puffs(layer: Layer, k: int):
    if k >= len(RING["radius_mid"]):
        return
    seg = RING["segments"]
    alpha = RING["alpha"][k] * PUFF["alpha"]
    for i in range(seg):
        h = hash01("puff", i)
        if h < PUFF["keep_above"]:
            continue
        a0 = 2 * math.pi * (i + 0.08) / seg + math.radians(4.0)
        a1 = 2 * math.pi * (i + 0.92 + 0.5 * hash01("pw", i)) / seg + math.radians(4.0)
        am = (a0 + a1) / 2 + (hash01("pm", i) - 0.5) * (a1 - a0) * 0.4
        lo, hi = PUFF["depth_R"]
        d = (lo + (hi - lo) * hash01("pd", i)) * (1 + PUFF["grow"] * k / 6)
        r_top0 = ring_radii(k, i, 0.85)
        r_top1 = ring_radii(k, i + 1, 0.85)
        r_bot = max(CENTRE_OPEN_R + 0.02, (r_top0 + r_top1) / 2 - d)
        r_mid = (r_top0 + r_top1) / 2 - d * 0.55
        pts = [
            (r_top0 * math.cos(a0), r_top0 * math.sin(a0)),
            (r_top1 * math.cos(a1), r_top1 * math.sin(a1)),
            (r_mid * math.cos(a1 - (a1 - a0) * 0.15), r_mid * math.sin(a1 - (a1 - a0) * 0.15)),
            (r_bot * math.cos(am), r_bot * math.sin(am)),
            (r_mid * 1.02 * math.cos(a0 + (a1 - a0) * 0.2), r_mid * 1.02 * math.sin(a0 + (a1 - a0) * 0.2)),
        ]
        normal = (-math.cos(am), -math.sin(am))
        base = mix(PAL["dust_shade"], PAL["dust"], 0.35 - 0.25 * k / 6)
        layer.poly([to_px(*q) for q in pts], shade(normal, base, 0.16, 0.12), alpha * (0.75 + 0.25 * h))


# ---------------------------------------------------------------- lascas de pedra
def chip_state(c, k):
    if k >= len(CHIP["alpha"]):
        return None
    ang, u0, u1, size, rot0, spin, nv = c
    p = CHIP["travel"][k]
    u = u0 + (u1 - u0) * p
    rot = rot0 + spin * p
    scale = 1 + CHIP["lift"] * math.sin(math.pi * min(p / 0.9, 1.0))
    scale *= 1 - CHIP["shrink_end"] * max(0.0, (k - 4) / 2)
    return u, rot, size * scale, CHIP["alpha"][k], p


def chip_shape(c):
    """Polígono angular (raios por vértice determinísticos) no espaço local, em unidades de tamanho."""
    ang, u0, u1, size, rot0, spin, nv = c
    pts = []
    for j in range(nv):
        a = 2 * math.pi * j / nv + (hash01("cv", ang, j) - 0.5) * 0.5
        r = 0.70 + 0.45 * hash01("cr", ang, j)
        pts.append((math.cos(a) * r * CHIP["elongation"], math.sin(a) * r / CHIP["elongation"] ** 0.35))
    return pts


def draw_chip(layer: Layer, c, k):
    st = chip_state(c, k)
    if st is None:
        return
    u, rot, size, alpha, p = st
    if alpha <= 0:
        return
    ang = math.radians(c[0])
    cx, cy = u * math.cos(ang), u * math.sin(ang)
    cr, sr = math.cos(math.radians(rot)), math.sin(math.radians(rot))
    local = chip_shape(c)
    world = [(cx + (x * cr - y * sr) * size, cy + (x * sr + y * cr) * size) for x, y in local]
    # sombra no piso (cresce com a altura do arco), deslocada contra a luz
    h = math.sin(math.pi * min(p / 0.9, 1.0))
    ox, oy = CHIP["shadow_offset"]
    sh = [to_px(x + ox * (0.4 + h), y - oy * (0.4 + h)) for x, y in world]
    layer.poly(sh, rgb("#1E2029"), CHIP["shadow_alpha"] * alpha * (0.5 + 0.5 * h))
    # facetas: platô de cima (mais claro) e faces laterais pela normal da aresta
    plateau = [(cx + (x - cx) * 0.52 + 0.08 * size, cy + (y - cy) * 0.52 + 0.08 * size) for x, y in world]
    n = len(world)
    outward = (math.cos(ang), math.sin(ang))
    for j in range(n):
        a, b = world[j], world[(j + 1) % n]
        pa, pb = plateau[j], plateau[(j + 1) % n]
        ex, ey = b[0] - a[0], b[1] - a[1]
        normal = (ey, -ex)  # vértices em sentido anti-horário → normal para fora
        mx, my = (a[0] + b[0]) / 2 - cx, (a[1] + b[1]) / 2 - cy
        if normal[0] * mx + normal[1] * my < 0:
            normal = (-normal[0], -normal[1])
        col = shade(normal, PAL["stone"], 0.32, 0.30)
        layer.poly([to_px(*a), to_px(*b), to_px(*pb), to_px(*pa)], col, alpha)
        # aresta voltada ao centro do impacto: ferrugem/laranja apagado, fino
        facing_centre = -(normal[0] * outward[0] + normal[1] * outward[1]) / (math.hypot(*normal) or 1)
        if facing_centre > 0.55:
            ia = (a[0] + (pa[0] - a[0]) * 0.32, a[1] + (pa[1] - a[1]) * 0.32)
            ib = (b[0] + (pb[0] - b[0]) * 0.32, b[1] + (pb[1] - b[1]) * 0.32)
            rim = mix(PAL["rust"], PAL["ember_muted"], max(0.0, 1 - k / 4))
            layer.poly([to_px(*a), to_px(*b), to_px(*ib), to_px(*ia)], rim, CHIP["rim_alpha"] * alpha)
    layer.poly([to_px(*q) for q in plateau], mix(PAL["stone_light"], PAL["dust"], 0.25), alpha)


def render_frame(k: int):
    """(quadro completo, só anel, só lascas)."""
    ring, chips, full = Layer(), Layer(), Layer()
    draw_ring(ring, k)
    draw_puffs(full, k)
    draw_ring(full, k)
    for c in CHIPS:
        draw_chip(chips, c, k)
        draw_chip(full, c, k)
    return full.downsample(), ring.downsample(), chips.downsample()


# ---------------------------------------------------------------- dados reais do Guardião
def boss_values() -> dict:
    txt = BOSS_DATA.read_text(encoding="utf-8")

    def val(name):
        m = re.search(rf"^\s*{name}:\s*([0-9.]+)", txt, re.M)
        return float(m.group(1))

    r1 = val("slamRadius")
    mult = val("phase2SlamRadiusMultiplier")
    return {
        "slamRadius": r1,
        "phase2SlamRadiusMultiplier": mult,
        "radius_phase1_m": round(r1, 4),
        "radius_phase2_m": round(r1 * mult, 4),
        "slamTelegraph": val("slamTelegraph"),
        "phase2SlamTelegraph": val("phase2SlamTelegraph"),
        "recoverTime": val("recoverTime"),
        "cooldownPhase1": val("cooldownPhase1"),
        "cooldownPhase2": val("cooldownPhase2"),
    }


def quad_side_m(radius_m: float) -> float:
    return 2 * radius_m * (CELL / 2) / UNIT_PX


# ---------------------------------------------------------------- verificações
def rel_lum(c):
    def ch(v):
        v /= 255
        return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4
    return 0.2126 * ch(c[0]) + 0.7152 * ch(c[1]) + 0.0722 * ch(c[2])


def contrast(a, b):
    la, lb = rel_lum(a), rel_lum(b)
    return (max(la, lb) + 0.05) / (min(la, lb) + 0.05)


def over(fg, a, bg):
    return tuple(fg[i] * a + bg[i] * (1 - a) for i in range(3))


def centroid(arr_a):
    w = arr_a.astype(np.float64)
    tot = w.sum()
    if tot == 0:
        return None
    ys, xs = np.indices(w.shape)
    return float((xs * w).sum() / tot) + 0.5, float((ys * w).sum() / tot) + 0.5


def git_unchanged(paths) -> tuple[bool, str]:
    try:
        out = subprocess.run(["git", "status", "--porcelain", "--", *paths], cwd=ROOT, capture_output=True,
                             text=True, check=True).stdout.strip()
        return out == "", out or "sem alterações"
    except Exception as exc:  # noqa: BLE001
        return False, f"git indisponível: {exc}"


def validate(frames, rings, chips, sheet, boss) -> dict:
    res = {"checks": [], "frames": []}

    def check(name, ok, detail):
        res["checks"].append({"name": name, "ok": bool(ok), "detail": detail})

    sh = np.asarray(sheet)
    check("sheet_size_mode", sheet.size == (1024, 512) and sheet.mode == "RGBA", f"{sheet.size} {sheet.mode}")
    check("frames_256_rgba", all(f.size == (CELL, CELL) and f.mode == "RGBA" for f in frames), f"{FRAMES} quadros")
    ys, xs = np.indices((CELL, CELL))
    rr = np.hypot(xs + 0.5 - CENTRE[0], ys + 0.5 - CENTRE[1]) / UNIT_PX
    inside_r = rr <= 1.0
    margin_min, dirty_total = CELL, 0
    green = blue = red = orange = visible = 0
    peak_sum = 0
    for k, (fr, rg, ch) in enumerate(zip(frames, rings, chips)):
        a = np.asarray(fr)
        al = a[..., 3]
        info = {"frame": k, "start_s": round(k * DURATION / FRAMES, 4)}
        cell = sh[(k // COLS) * CELL:(k // COLS + 1) * CELL, (k % COLS) * CELL:(k % COLS + 1) * CELL]
        info["sheet_cell_equals_frame"] = bool(np.array_equal(cell, a))
        dirty = int(((al == 0) & (a[..., :3].sum(axis=2) > 0)).sum())
        dirty_total += dirty
        info["alpha_sum"] = int(al.sum())
        info["alpha_max"] = int(al.max())
        peak_sum = max(peak_sum, info["alpha_sum"])
        info["cover_over_50pct"] = round(float((al > 127).mean()), 5)
        info["mean_alpha_inside_R"] = round(float(al[inside_r].mean() / 255), 5)
        info["max_alpha_inside_centre_open"] = int(al[rr < CENTRE_OPEN_R].max())
        nz = np.argwhere(al > 0)
        if len(nz):
            y0, x0 = nz.min(axis=0)
            y1, x1 = nz.max(axis=0) + 1
            m = int(min(x0, y0, CELL - x1, CELL - y1))
            margin_min = min(margin_min, m)
            info["bbox"] = [int(x0), int(y0), int(x1), int(y1)]
            info["edge_margin_px"] = m
            info["max_radius_R"] = round(float(rr[al > 0].max()), 4)
        ra = np.asarray(rg)[..., 3]
        c = centroid(ra)
        if c:
            info["ring_centroid"] = [round(c[0], 3), round(c[1], 3)]
            info["ring_outer_R"] = round(float(rr[ra > 8].max()), 4)
        cha = np.asarray(ch)[..., 3]
        if cha.any():
            info["chips_max_radius_R"] = round(float(rr[cha > 0].max()), 4)
        # matizes
        vis = al >= 24
        px = a[vis][:, :3].astype(np.float32) / 255
        visible += len(px)
        g = b = r_ = o = 0
        for pr, pg, pb in px:
            h, s, v = colorsys.rgb_to_hsv(pr, pg, pb)
            hd = h * 360
            if s > 0.20 and 70 <= hd <= 170:
                g += 1
            if s > 0.40 and 175 <= hd <= 265:
                b += 1
            if s > 0.50 and (hd < 14 or hd > 345):
                r_ += 1
            if s > 0.30 and 14 <= hd <= 48:
                o += 1
        green += g
        blue += b
        red += r_
        orange += o
        info.update({"green_px": g, "hero_blue_px": b, "telegraph_red_px": r_, "orange_px": o})
        res["frames"].append(info)

    F = res["frames"]
    check("sheet_cells_equal_frames", all(f["sheet_cell_equals_frame"] for f in F), "célula k = quadro k")
    check("no_dirty_transparent_px", dirty_total == 0, f"{dirty_total} px com alpha 0 e RGB != 0")
    check("edge_margin_ge_8px", margin_min >= 8, f"margem mínima {margin_min} px")
    check("final_frame_transparent", F[-1]["alpha_max"] == 0, f"alpha máx. do quadro 7 = {F[-1]['alpha_max']}")
    check("frames_0_to_6_visible", all(f["alpha_sum"] > 0 for f in F[:-1]), "quadros 0–6 não vazios")
    check("dissipates", F[6]["alpha_sum"] <= 0.30 * peak_sum and F[5]["alpha_sum"] < F[2]["alpha_sum"],
          f"quadro 6 = {F[6]['alpha_sum'] / peak_sum:.0%} do pico; sem área persistente")
    check("centre_open", all(f["max_alpha_inside_centre_open"] == 0 for f in F),
          f"alpha 0 dentro de {CENTRE_OPEN_R} R em todos os quadros")
    check("floor_readable_cover", all(f["cover_over_50pct"] <= MAX_COVER_50 for f in F),
          f"alpha>50% ≤ {MAX_COVER_50:.0%} da célula (máx. {max(f['cover_over_50pct'] for f in F):.1%})")
    check("floor_readable_mean_alpha", all(f["mean_alpha_inside_R"] <= MAX_MEAN_ALPHA_IN_R for f in F),
          f"alpha médio dentro de R ≤ {MAX_MEAN_ALPHA_IN_R} (máx. {max(f['mean_alpha_inside_R'] for f in F):.3f})")
    cen = [f["ring_centroid"] for f in F if "ring_centroid" in f]
    drift = max(math.hypot(x - CENTRE[0], y - CENTRE[1]) for x, y in cen)
    check("fixed_centre_no_baked_shake", drift <= 0.75, f"centroide do anel a ≤ {drift:.3f} px de {CENTRE}")
    outer = [f["ring_outer_R"] for f in F if "ring_outer_R" in f]
    check("ring_expands_monotonic", all(b > a for a, b in zip(outer, outer[1:])), f"raio externo por quadro {outer}")
    check("ring_reaches_real_radius", 0.95 <= outer[-1] <= 1.0 and max(f.get("max_radius_R", 0) for f in F) <= 1.0,
          f"anel termina em {outer[-1]} R; nada passa de 1,0 R")
    check("chips_inside_radius", all(f.get("chips_max_radius_R", 0) <= 0.85 for f in F),
          f"lascas ≤ 0,85 R (máx. {max(f.get('chips_max_radius_R', 0) for f in F)})")
    check("few_chips", 3 <= len(CHIPS) <= 8, f"{len(CHIPS)} lascas")
    check("no_green", green == 0, f"{green} px verdes (veneno)")
    check("no_hero_blue", blue == 0, f"{blue} px azul/ciano saturado (magia do Hero)")
    check("no_telegraph_red", red == 0, f"{red} px vermelho saturado (aviso #FF2A2A/Mat_TelegraphFill)")
    check("orange_is_subtle", orange <= 0.25 * visible, f"laranja {orange / max(visible, 1):.1%} dos px visíveis (≤ 25%)")
    # contraste do núcleo de poeira (quadro 1) sobre a pedra
    core = mix(PAL["dust"], PAL["stone_light"], 0.15 + 0.35 * (1 / 6))
    a1 = RING["alpha"][1]
    cs = {bg: round(contrast(over(core, a1, rgb(bg)), rgb(bg)), 2) for bg in (PAL["stone"], PAL["stone_dark"])}
    check("dust_readable_on_stone", min(cs.values()) >= 1.8, f"WCAG poeira/piso {cs} (≥ 1,8)")
    check("duration_in_envelope", 0.25 <= DURATION <= 0.45, f"{DURATION} s (ficha 0,25–0,45 s)")
    gap = boss["recoverTime"]
    check("never_overlaps_telegraph", DURATION < gap and DURATION < min(boss["cooldownPhase1"], boss["cooldownPhase2"]),
          f"efeito {DURATION} s < recoverTime {gap} s e cooldown ≥ {min(boss['cooldownPhase1'], boss['cooldownPhase2'])} s;"
          " BossBrain.ResolveAttack chama telegraph.Hide() antes do ResolveSlam")
    check("telegraph_min_1s_intact", min(boss["slamTelegraph"], boss["phase2SlamTelegraph"]) >= 1.0,
          f"slamTelegraph {boss['slamTelegraph']} s / fase 2 {boss['phase2SlamTelegraph']} s ≥ 1 s (só lidos)")
    check("radius_from_real_data", abs(boss["radius_phase2_m"] - 4.55) < 1e-6 and abs(boss["radius_phase1_m"] - 3.5) < 1e-6,
          f"R = {boss['radius_phase1_m']} m / {boss['radius_phase2_m']} m lidos de Boss_Warden_Data.asset;"
          f" quad {quad_side_m(boss['radius_phase1_m']):.3f} m / {quad_side_m(boss['radius_phase2_m']):.3f} m")
    check("particle_budget_le_24", 1 <= 24, "1 partícula (quad com flipbook); lascas desenhadas na sheet")
    ok, det = git_unchanged(READ_ONLY)
    check("boss_code_and_data_read_only", ok, det)
    res["ok"] = all(c["ok"] for c in res["checks"])
    return res


# ---------------------------------------------------------------- revisão (PREVIEW)
def font(size):
    for name in ("segoeui.ttf", "arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default(size=size)


def label(im: Image.Image, text: str, size=22, pos=(8, 6)):
    d = ImageDraw.Draw(im)
    f = font(size)
    x, y = pos
    bb = d.textbbox((x, y), text, font=f)
    d.rectangle([bb[0] - 4, bb[1] - 3, bb[2] + 4, bb[3] + 3], fill=(0, 0, 0, 170))
    d.text((x, y), text, font=f, fill=(255, 255, 255, 255))


def hero_mask(cap: Image.Image) -> Image.Image:
    """Máscara aproximada do Hero/baú (objetos opacos que, na Unity, ficam à frente do quad no piso)."""
    x0, y0, x1, y1 = ARENA_HERO_BOX
    box = np.asarray(cap.crop(ARENA_HERO_BOX).convert("RGB"), np.float32) / 255
    mx, mn = box.max(axis=2), box.min(axis=2)
    sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    r, g, b = box[..., 0], box[..., 1], box[..., 2]
    floorish = (b >= r) & (b >= g * 0.98) & (sat < 0.42) & (mx < 0.62)
    m = Image.fromarray(np.where(floorish, 0, 255).astype(np.uint8), "L")
    m = m.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.MinFilter(3))
    full = Image.new("L", cap.size, 0)
    full.paste(m, (x0, y0))
    return full


def ground_overlay(frame: Image.Image, radius_m: float, centre_px, ppm) -> tuple[Image.Image, tuple[int, int]]:
    """Quad deitado no piso visto pela câmera de 45° (aproximação ortográfica local)."""
    side = quad_side_m(radius_m) * ppm
    w, h = max(1, round(side)), max(1, round(side * math.sin(PITCH)))
    im = frame.resize((w, h), Image.LANCZOS)
    return im, (round(centre_px[0] - w / 2), round(centre_px[1] - h / 2))


def draw_telegraph(base: Image.Image, radius_m: float, centre_px, ppm, progress: float):
    """Aproximação 2D do telegraph existente (disco externo + preenchimento), só para referência."""
    lay = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(lay)
    rx, ry = radius_m * ppm, radius_m * ppm * math.sin(PITCH)
    cx, cy = centre_px
    d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=tuple(round(c * 255 * 0.85) for c in TELEGRAPH_OUTER) + (255,))
    if progress > 0:
        fx, fy = rx * progress, ry * progress
        d.ellipse([cx - fx, cy - fy, cx + fx, cy + fy], fill=tuple(round(c * 255 * 0.8) for c in TELEGRAPH_FILL) + (255,))
    base.alpha_composite(lay)


def arena_scene(frame: Image.Image | None, radius_m: float, cap, mask, telegraph_progress=None):
    fx, fy = ARENA_FEET
    centre = (fx, fy - SLAM_CENTRE_AHEAD_M * ARENA_PPM * math.sin(PITCH))
    im = cap.copy()
    if telegraph_progress is not None:
        draw_telegraph(im, radius_m, centre, ARENA_PPM, telegraph_progress)
    if frame is not None:
        ov, pos = ground_overlay(frame, radius_m, centre, ARENA_PPM)
        im.alpha_composite(ov, pos)
    im.paste(cap, (0, 0), mask)  # Hero/baú por cima (depth test do quad no piso)
    return im, centre


def review_floor(frames, boss) -> Image.Image:
    cap = Image.open(ARENA_CAPTURE).convert("RGBA")
    mask = hero_mask(cap)
    tw, th = 400, 300
    cols = 1 + FRAMES
    out = Image.new("RGB", (cols * tw, 2 * th + 40), (18, 18, 22))
    for row, key in enumerate(("radius_phase1_m", "radius_phase2_m")):
        R = boss[key]
        for col in range(cols):
            if col == 0:
                im, c = arena_scene(None, R, cap, mask, telegraph_progress=0.75)
                txt = f"aviso existente ~ R {R} m"
            else:
                im, c = arena_scene(frames[col - 1], R, cap, mask)
                txt = f"F{col - 1}  {round((col - 1) * DURATION / FRAMES, 3)} s"
            hw = 5.2 * ARENA_PPM
            hh = hw * 0.75
            crop = im.crop((round(c[0] - hw), round(c[1] - hh), round(c[0] + hw), round(c[1] + hh)))
            tile = crop.resize((tw, th), Image.LANCZOS)
            label(tile, txt, 18)
            out.paste(tile.convert("RGB"), (col * tw, 40 + row * th))
    hdr = out.convert("RGBA")
    label(hdr, "PREVIEW A10 FX-SLAM — captura real F08 (arena), câmera 45°, quad no piso por projeção ortográfica local; "
               "linha 1: R 3,5 m (fase 1), linha 2: R 4,55 m (fase 2). Não é captura da Unity.", 20, (8, 8))
    return hdr.convert("RGB")


def review_floor_zoom(frames, boss) -> Image.Image:
    cap = Image.open(ARENA_CAPTURE).convert("RGBA")
    mask = hero_mask(cap)
    im, c = arena_scene(frames[2], boss["radius_phase2_m"], cap, mask)
    hw, hh = 6.0 * ARENA_PPM, 4.0 * ARENA_PPM
    crop = im.crop((round(c[0] - hw), round(c[1] - hh), round(c[0] + hw), round(c[1] + hh)))
    crop = crop.resize((crop.width * 2 // 3, crop.height * 2 // 3), Image.LANCZOS)
    label(crop, "PREVIEW — F2 (0,088 s), R 4,55 m, Hero dentro do raio; não é captura da Unity", 20)
    return crop.convert("RGB")


def review_topdown(frames) -> Image.Image:
    """Vista de cima com guias: 1,0 R (raio real), centro aberto e pivô."""
    bgs = ["#3E4152", "#55596B", "#0E131A", "#B9C4CF"]
    out = Image.new("RGB", (FRAMES * CELL, CELL * len(bgs) + 36), (14, 14, 18))
    for r, bg in enumerate(bgs):
        for k, fr in enumerate(frames):
            tile = Image.new("RGBA", (CELL, CELL), (*rgb(bg), 255))
            tile.alpha_composite(fr)
            d = ImageDraw.Draw(tile)
            if r == 0:
                cx, cy = CENTRE
                for rad, colr in ((UNIT_PX, (255, 255, 255, 110)), (CENTRE_OPEN_R * UNIT_PX, (255, 255, 255, 60))):
                    d.ellipse([cx - rad, cy - rad, cx + rad, cy + rad], outline=colr, width=1)
                d.line([(cx - 5, cy), (cx + 5, cy)], fill=(255, 255, 255, 120))
                d.line([(cx, cy - 5), (cx, cy + 5)], fill=(255, 255, 255, 120))
            out.paste(tile.convert("RGB"), (k * CELL, 36 + r * CELL))
    o = out.convert("RGBA")
    label(o, "PREVIEW — vista de cima (fonte do quad). Linha 1 com guias: círculo externo = 1,0 R real; interno = centro "
             f"aberto {CENTRE_OPEN_R} R. Fundos: pedra #3E4152, #55596B, painel #0E131A, aço #B9C4CF", 18, (8, 7))
    return o.convert("RGB")


def preview_gif(frames, boss, path: Path):
    """Aviso (aproximado) → Hide → impacto; pausa congelada; segunda ativação reinicia do quadro 0."""
    cap = Image.open(ARENA_CAPTURE).convert("RGBA")
    mask = hero_mask(cap)
    R = boss["radius_phase2_m"]
    hw, hh = 5.4 * ARENA_PPM, 4.0 * ARENA_PPM
    size = (640, round(640 * hh / hw))
    seq, dur = [], []

    def shot(frame=None, tele=None, text="", ms=90):
        im, c = arena_scene(frame, R, cap, mask, telegraph_progress=tele)
        crop = im.crop((round(c[0] - hw), round(c[1] - hh), round(c[0] + hw), round(c[1] + hh))).resize(size, Image.LANCZOS)
        label(crop, "PREVIEW 2× mais lento — " + text, 16)
        seq.append(crop.convert("RGB"))
        dur.append(ms)

    fd = round(DURATION / FRAMES * 1000 * 2)
    for i in range(6):
        shot(tele=(i + 1) / 6, text=f"aviso existente (aprox.) {boss['phase2SlamTelegraph']} s", ms=170)
    for k in range(FRAMES):
        shot(frames[k], text=f"impacto F{k}", ms=fd)
    shot(text="repouso", ms=500)
    for k in range(4):
        shot(frames[k], text=f"2ª ativação F{k}", ms=fd)
    shot(frames[3], text="PAUSA (timeScale 0): quadro congelado", ms=700)
    shot(text="cancelamento/morte: Clear() → vazio", ms=500)
    for k in range(FRAMES):
        shot(frames[k], text=f"reuso após cancelamento F{k}", ms=fd)
    shot(text="repouso", ms=700)
    seq[0].save(path, save_all=True, append_images=seq[1:], duration=dur, loop=0, optimize=False, disposal=1)


# ---------------------------------------------------------------- saída
def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def stable_guid(key: str) -> str:
    return uuid.UUID(hashlib.sha256(("Eldoria/A10/" + key).encode()).hexdigest()[:32]).hex


def write_meta(png: Path) -> bool:
    """Cria a .meta só se faltar (preserva o GUID depois da importação). Template de FX_Spark/FX-HEAL:
    1024×512 é potência de 2; clamp, bilinear, mips, sRGB, alphaIsTransparency."""
    meta = png.with_suffix(png.suffix + ".meta")
    if meta.exists():
        return False
    tpl = (Path(__file__).parent / "texture.meta.template").read_text(encoding="utf-8")
    meta.write_text(tpl.replace("{{GUID}}", stable_guid(png.name)), encoding="utf-8", newline="\n")
    return True


def write_folder_meta() -> bool:
    meta = STAGING.with_suffix(".meta")
    if meta.exists():
        return False
    meta.write_text("fileFormatVersion: 2\nguid: " + stable_guid("folder/" + ASSET_ID) + "\nfolderAsset: yes\n"
                    "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n"
                    "  assetBundleVariant: \n", encoding="utf-8", newline="\n")
    return True


def timing_json(boss) -> dict:
    fd = DURATION / FRAMES
    r1, r2 = boss["radius_phase1_m"], boss["radius_phase2_m"]
    return {
        "asset_id": ASSET_ID,
        "catalog_id": "A10",
        "sheet": f"../{SHEET_NAME}",
        "sheet_size": [CELL * COLS, CELL * ROWS],
        "grid": {"columns": COLS, "rows": ROWS, "cell": [CELL, CELL],
                 "order": "linha a linha, da esquerda para a direita, de cima para baixo"},
        "frames": [{"index": k, "cell": [k % COLS, k // COLS], "start_s": round(k * fd, 4), "duration_s": round(fd, 4),
                    "file": f"frames/{ASSET_ID}-F{k}.png"} for k in range(FRAMES)],
        "duration_s": DURATION,
        "fps": round(FRAMES / DURATION, 3),
        "loop": False,
        "final_frame": "quadro 7 totalmente transparente (fim do efeito, nada persiste no chão)",
        "view": "ortográfica de cima, plano do piso; +X da imagem = +X do mundo, topo da imagem = +Z do mundo",
        "pivot": {"meaning": "ponto do impacto = transform.position do Guardião no ResolveSlam, no piso",
                  "cell_px_from_top_left": list(CENTRE), "cell_normalized": [0.5, 0.5]},
        "scale": {
            "unit_radius_px": UNIT_PX,
            "rule": "lado do quad (m) = 2 × R × (CELL/2) / UNIT_PX = R × " + f"{2 * (CELL / 2) / UNIT_PX:.6f}",
            "ring_final_outer_R": 0.985,
            "centre_open_R": CENTRE_OPEN_R,
            "phase1": {"radius_m": r1, "quad_side_m": round(quad_side_m(r1), 4),
                       "source": "Boss_Warden_Data.slamRadius"},
            "phase2": {"radius_m": r2, "quad_side_m": round(quad_side_m(r2), 4),
                       "source": "slamRadius × phase2SlamRadiusMultiplier (BossBrain.SlamRadius)"},
            "note": "Usar sempre o SlamRadius efetivo no momento da resolução, nunca um tamanho fixo de imagem.",
        },
        "unity_integration_proposal": {
            "status": "Proposta; NÃO VALIDADO NO EDITOR. Hook e prefab são tarefa Codex separada.",
            "hook": {
                "event": "BossBrain: evento público de apresentação, p. ex. 'event Action<Vector3, float> SlamResolved'",
                "raise": "Em ResolveAttack, logo depois de ResolveSlam(), com (transform.position, SlamRadius), acerte ou não o Hero",
                "rules": [
                    "O efeito só escuta; nunca chama ResolveSlam, TakeDamage nem altera tempos, raios ou estado do BossBrain.",
                    "Não publicar em EnterIdle, morte do Boss, transição de fase ou cancelamento do windup.",
                    "Telegraph intacto: BossAttackTelegraph continua com mínimo de 1 s e é escondido antes do impacto.",
                ],
            },
            "texture_import": "Default, sRGB, alphaIsTransparency, clamp, bilinear, mipmaps on (template FX_Spark/FX-HEAL).",
            "material": "URP/Particles/Unlit, Surface Transparent, Blend Alpha, cor branca, sem emissão/bloom; "
                        "material compartilhado (sharedMaterial), nada criado por golpe.",
            "particle_system": {
                "max_particles": 1,
                "looping": False,
                "play_on_awake": False,
                "emission": "nenhuma automática; Emit(1) por SlamResolved",
                "start_lifetime_s": DURATION,
                "start_size_m": "quad_side_m do SlamRadius recebido (7,7241 m fase 1; 10,0414 m fase 2)",
                "start_rotation": "0 (o efeito é quase radial; a luz das facetas vem de cima/esquerda da imagem)",
                "simulation_space": "World",
                "scaling_mode": "Local",
                "texture_sheet_animation": {"mode": "Grid", "tiles": [COLS, ROWS], "animation": "Whole Sheet",
                                             "frame_over_time": "linear 0→1", "cycles": 1},
                "renderer": {"render_mode": "Horizontal Billboard", "sorting_fudge": 0,
                             "position_y_m": 0.04,
                             "note": "Acima do piso e abaixo do telegraph (0,03/0,05), que já está escondido no impacto. "
                                     "Conferir no Editor a orientação de +Z da textura no Horizontal Billboard."},
            },
            "reuse_and_reset": [
                "Um único ParticleSystem filho do Boss (ou do arena root), reutilizado; nada de Instantiate/Destroy por golpe.",
                "Retrigger (duas ativações rápidas): Clear() + Emit(1) reinicia do quadro 0 no novo centro/raio.",
                "Pausa: simulação em tempo escalado; com timeScale 0 o quadro congela (conferir).",
                "Morte do Boss, reset do andar, desativação e retorno de pool: Stop(true, StopEmittingAndClear) / Clear().",
                "Sem flash nem shake próprios: não adiciona trauma de câmera; opções existentes de flash/shake ficam intactas.",
            ],
            "budget": "1 partícula visível por golpe (≤ 24 da ficha); 6 lascas e o anel estão desenhados na sheet.",
        },
    }


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", type=Path, help="escreve só sheet, quadros e checagens nesta pasta")
    args = ap.parse_args()

    boss = boss_values()
    frames, rings, chips = [], [], []
    for k in range(FRAMES):
        f, r, c = render_frame(k)
        frames.append(f)
        rings.append(r)
        chips.append(c)
    sheet = Image.new("RGBA", (CELL * COLS, CELL * ROWS), (0, 0, 0, 0))
    for k, f in enumerate(frames):
        sheet.paste(f, ((k % COLS) * CELL, (k // COLS) * CELL))

    if args.out:
        args.out.mkdir(parents=True, exist_ok=True)
        sheet.save(args.out / SHEET_NAME, optimize=False)
        for k, f in enumerate(frames):
            f.save(args.out / f"{ASSET_ID}-F{k}.png", optimize=False)
        review_topdown(frames).save(args.out / "review_topdown.png")
        review_floor(frames, boss).save(args.out / "review_floor_preview.png")
        review_floor_zoom(frames, boss).save(args.out / "review_floor_zoom.png")
        res = validate(frames, rings, chips, sheet, boss)
        for c in res["checks"]:
            print(("ok  " if c["ok"] else "FAIL") + f" {c['name']}: {c['detail']}")
        return 0 if res["ok"] else 1

    (SOURCE / "frames").mkdir(parents=True, exist_ok=True)
    sheet_path = STAGING / SHEET_NAME
    sheet.save(sheet_path, optimize=False)
    created_meta = write_meta(sheet_path)
    folder_meta = write_folder_meta()
    files = {SHEET_NAME: sheet_path}
    for k, f in enumerate(frames):
        p = SOURCE / "frames" / f"{ASSET_ID}-F{k}.png"
        f.save(p, optimize=False)
        files[f"frames/{ASSET_ID}-F{k}.png"] = p
    review_topdown(frames).save(SOURCE / "review_topdown.png", optimize=False)
    review_floor(frames, boss).save(SOURCE / "review_floor_preview.png", optimize=False)
    review_floor_zoom(frames, boss).save(SOURCE / "review_floor_zoom.png", optimize=False)
    preview_gif(frames, boss, SOURCE / "preview_slam.gif")
    files["preview_slam.gif"] = SOURCE / "preview_slam.gif"

    (SOURCE / f"{ASSET_ID}.timing.json").write_text(json.dumps(timing_json(boss), ensure_ascii=False, indent=2) + "\n",
                                                   encoding="utf-8", newline="\n")
    source = {
        "asset_id": ASSET_ID, "catalog_id": "A10", "generator": "Tools/FxSlamGenerator/generate.py",
        "supersampling": S, "cell": CELL, "grid": [COLS, ROWS], "duration_s": DURATION, "unit_radius_px": UNIT_PX,
        "centre_px": CENTRE, "light_image_xy": LIGHT, "palette": PAL, "ring": RING, "chips": [
            dict(zip(["angle_deg", "u_start_R", "u_end_R", "size_R", "rot0_deg", "spin_deg", "vertices"], c)) for c in CHIPS],
        "chip": CHIP, "centre_open_R": CENTRE_OPEN_R, "boss_data": boss,
        "references": {
            "slam_code": "Assets/_Project/Scripts/Boss/BossBrain.cs (SlamRadius, ResolveAttack → telegraph.Hide → ResolveSlam)",
            "telegraph": "Assets/_Project/Scripts/Boss/BossAttackTelegraph.cs; Mat_TelegraphOuter (0.35,0.05,0.05), "
                         "Mat_TelegraphFill (1,0.2,0.15) em DungeonFloorSceneBuilder.cs",
            "boss_data": str(BOSS_DATA.relative_to(ROOT)).replace("\\", "/"),
            "arena_capture": str(ARENA_CAPTURE.relative_to(ROOT)).replace("\\", "/"),
            "arena_calibration": {"feet_px": ARENA_FEET, "ppm": ARENA_PPM, "hero_box": ARENA_HERO_BOX,
                                  "slam_centre_ahead_m": SLAM_CENTRE_AHEAD_M},
            "palette_refs": "#55596B/#3E4152 pedra (ART_DIRECTION); ferrugem #8A4A22 e laranja #FF9A2E (A08/A09)",
        },
    }
    (SOURCE / f"{ASSET_ID}.source.json").write_text(json.dumps(source, ensure_ascii=False, indent=2) + "\n",
                                                   encoding="utf-8", newline="\n")
    res = validate(frames, rings, chips, sheet, boss)
    res["sha256"] = {name: sha(p) for name, p in files.items()}
    res["meta_created"] = created_meta
    res["folder_meta_created"] = folder_meta
    (SOURCE / "FxSlam.validation.json").write_text(json.dumps(res, ensure_ascii=False, indent=2) + "\n",
                                                  encoding="utf-8", newline="\n")
    for c in res["checks"]:
        print(("ok  " if c["ok"] else "FAIL") + f" {c['name']}: {c['detail']}")
    return 0 if res["ok"] else 1


if __name__ == "__main__":
    sys.exit(main())

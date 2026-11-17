"""A05 / FX-LEVEL — gerador determinístico da animação de subida de nível (Eldoria).

Desenha 12 quadros 256×256 de um efeito de nível ganho: um único anel ciano fino e aberto
atrás do Hero sobe das botas até a cintura, acompanhado por poucos motes largos de latão nas
laterais, e se dissolve por completo. Centro livre, sem disco, texto, número ou personagem.
Projeção fixa da câmera do jogo (pitch 45°, offset 0,10,-10), pivô no centro dos pés,
mesma escala em todos os quadros. Polígonos com supersampling 8× e redução com alpha
pré-multiplicado. Sem IA generativa nem asset de terceiros.

Uso (Python 3.11 + Pillow 13, ver .claude/skills/create-game-assets/scripts/requirements.txt):
    python Tools/FxLevelGenerator/generate.py            # escreve sheet, .meta (só se faltar), Source~
    python Tools/FxLevelGenerator/generate.py --out DIR  # escreve só sheet/quadros em DIR (iteração)

Sai com código 1 se alguma verificação falhar (tamanho, RGBA, alpha sujo, corte na borda da
célula, pivô, centro livre, quadro final não transparente, matiz de ameaça/verde, branco opaco,
cobertura de flash, continuidade entre quadros, raio e orçamento de partículas).
"""
from __future__ import annotations

import argparse
import colorsys
import hashlib
import json
import math
import sys
import uuid
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
STAGING = ROOT / "Assets/_Project/Art/ProductionStaging/FX-LEVEL"
SOURCE = STAGING / "Source~"
SHEET_NAME = "FX-LEVEL-Sheet.png"
META_TEMPLATE = ROOT / "Tools/FxHealGenerator/texture.meta.template"  # cópia de FX_Spark.png.meta
FLOOR_CAPTURE = ROOT / "Reports/F05/Visual-final/1920-level.png"
FLOOR_FEET_1080 = (959, 543)  # centro das botas do Hero na captura F05 1920×1080
FLOOR_PPM_1080 = 75.0  # px por metro no plano do piso perto do Hero (ladrilho 2 m ≈ 150 px)

S = 8  # supersampling
CELL = 256
COLS, ROWS = 4, 3
FRAMES = COLS * ROWS
DURATION = 0.7  # s (ficha A05)
PPM = 128.0  # px por metro no plano dos pés, dentro da célula (célula de 2,0 m)
PIVOT = (128.0, 178.0)  # centro dos pés, em px a partir do topo-esquerda da célula
PITCH = math.radians(45.0)  # câmera do jogo (ArtTestSceneBuilder/DungeonMvpSceneBuilder: offset 0,10,-10)
LIGHT = (-0.55, -0.83)  # luz de cima/esquerda, como ícones, UI-SKIN e FX-HEAL
_ln = math.hypot(*LIGHT)
LIGHT = (LIGHT[0] / _ln, LIGHT[1] / _ln)
HERO_HEIGHT_M = 2.0  # CharacterController.height em DungeonMvpSceneBuilder

# ---------------------------------------------------------------- paleta (sRGB)
# Ciano/anel da orbe de XP real: Tools/Lemo3DGenerator/src/production/ItemModels.mjs
# (M.xp #4FD8FF, emissive #2FB8E8, M.xpRing #DFFBFF). Latão da direção de arte #E0B040.
# Faces escuras de latão mantêm matiz ≥ 40°, longe do laranja de ameaça (#FF9A2E, 32°).
PAL = {
    "cyan_shade": "#2FB8E8",
    "cyan": "#4FD8FF",
    "cyan_pale": "#DFFBFF",
    "brass_shade": "#B08A2A",
    "brass": "#E0B040",
    "brass_light": "#F2D27A",
}

# ---------------------------------------------------------------- parâmetros editáveis
# Anel: um só, fino, aberto atrás do Hero. A abertura cobre o arco de trás que, no billboard,
# ficaria desenhado sobre o tronco; o que sobra fica fora da silhueta (|x| > ~0,43 m).
RING = {
    "radius_m": (0.58, 0.50),  # nas botas → na cintura (abraça o corpo ao subir; ≤ 0,8 m)
    "rise_m": 1.00,  # botas → cintura (Hero de 2 m)
    "rise_end_t": 0.80,  # fração da duração em que chega à cintura (~quadro 9)
    "width_m": 0.055,  # largura radial: fino, mas ≥ 3 px no jogo
    "segments": 24,  # polígono de 24 lados: planos largos, como o low-poly
    "back_gap_deg": 120.0,  # abertura centrada atrás do Hero
    "back_alpha": 0.55,  # arco de trás mais apagado que o da frente (profundidade)
    "alpha_peak": 0.82,  # brilho contido: nunca opaco
    "fade": [0.50, 0.85, 1.0, 1.0, 1.0, 1.0, 0.95, 0.85, 0.70, 0.48, 0.24, 0.0],  # por quadro
    "dissolve": [0, 0, 0, 0, 0, 0, 0, 0, 0.20, 0.45, 0.70, 1.0],  # fração de facetas que somem
}
# Motes de latão: poucos e largos, só nas laterais (|x| ≥ 0,70 m), fora do centro, da silhueta
# e do anel (latão sobre ciano mistura em verde). Nascem perto da altura do anel e sobem pouco.
MOTES = [
    # x,     z,    alt. inicial, subida, atraso, tamanho, deriva x
    (-0.71, -0.10, 0.10, 0.34, 0.06, 1.00, -0.02),
    (0.72, -0.04, 0.24, 0.32, 0.14, 0.95, 0.02),
    (-0.73, 0.12, 0.44, 0.30, 0.26, 0.85, -0.01),
    (0.73, 0.14, 0.56, 0.30, 0.34, 0.85, 0.01),
    (-0.71, -0.16, 0.70, 0.28, 0.42, 0.90, -0.02),
    (0.71, -0.14, 0.80, 0.28, 0.48, 0.90, 0.02),
    (-0.72, 0.04, 0.86, 0.28, 0.54, 0.80, -0.02),
    (0.71, 0.02, 0.06, 0.30, 0.02, 0.80, 0.02),
]
MOTE = {
    "half_w_m": 0.055,
    "half_h_m": 0.080,  # losango vertical largo, legível a ~0,59× no jogo
    "life": 0.40,  # fração da duração que cada mote vive depois do atraso
    "alpha_peak": 0.88,
    "halo_alpha": 0.16,
    "halo_scale": 1.45,
}
GREEN_HUE = (70.0, 170.0)  # proibido: lembra veneno/nuvem de dano
THREAT_HUES = (0.0, 36.0)  # vermelho #FF2A2A (0°), telegraph (1,.2,.15) ≈ 3°, laranja #FF9A2E (32°)
FLASH_MAX_COVER = 0.06  # fração máxima da célula com alpha > 50%
OPAQUE_WHITE = (0.18, 0.92, 200)  # saturação <, valor >, alpha > → branco opaco proibido
CENTRE_COLUMN_M = 0.30  # meia-largura da coluna do corpo que os motes nunca ocupam
MAX_STEP_PX = 16.0  # deslocamento máximo do anel entre quadros consecutivos (sem saltos)


def rgb(h: str) -> tuple[int, int, int]:
    h = h.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def mix(a, b, t):
    a, b = rgb(a) if isinstance(a, str) else a, rgb(b) if isinstance(b, str) else b
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


def smooth(t: float) -> float:
    t = min(max(t, 0.0), 1.0)
    return t * t * (3 - 2 * t)


def ease_out(t: float) -> float:
    t = min(max(t, 0.0), 1.0)
    return 1 - (1 - t) ** 2


def project(x: float, y: float, z: float) -> tuple[float, float]:
    """Mundo (x direita, y cima, z para longe da câmera) → px da célula (câmera pitch 45°)."""
    u = PIVOT[0] + x * PPM
    v = PIVOT[1] - (y * math.cos(PITCH) + z * math.sin(PITCH)) * PPM
    return u, v


def frame_t(k: int) -> float:
    """Tempo normalizado do quadro k: 0 no primeiro, 1 no último (que é vazio)."""
    return k / (FRAMES - 1)


def ring_height(t: float) -> float:
    """Altura do anel: sobe com aceleração suave e assenta na cintura."""
    u = min(max(t, 0.0) / RING["rise_end_t"], 1.0)
    # metade linear + metade smoothstep: começa e assenta suave, com velocidade de pico baixa
    return RING["rise_m"] * (0.5 * u + 0.5 * smooth(u))


def ring_radius(t: float) -> float:
    r0, r1 = RING["radius_m"]
    return r0 + (r1 - r0) * smooth(max(t, 0.0) / RING["rise_end_t"])


def seg_rand(i: int) -> float:
    """Ordem determinística de dissolução das facetas (simétrica em x)."""
    n = RING["segments"]
    j = min(i, (n // 2 - 1 - i) % n)  # faceta espelhada em x (ângulo a → 180° − a)
    return int(hashlib.sha256(f"FX-LEVEL/{j}".encode()).hexdigest()[:8], 16) / 0xFFFFFFFF


# ---------------------------------------------------------------- desenho (supersampled)
class Layer:
    """Camada RGBA em alta resolução; polígonos chapados compostos por 'over'."""

    def __init__(self):
        self.img = Image.new("RGBA", (CELL * S, CELL * S), (0, 0, 0, 0))

    def poly(self, pts, colour, alpha):
        if alpha <= 0:
            return
        tmp = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        ImageDraw.Draw(tmp).polygon([(x * S, y * S) for x, y in pts], fill=(*colour, round(255 * alpha)))
        self.img = Image.alpha_composite(self.img, tmp)

    def ellipse_soft(self, cx, cy, rx, ry, colour, alpha, steps=6):
        """Halo de poucos degraus concêntricos (baixo ruído, sem gradiente contínuo ruidoso)."""
        if alpha <= 0:
            return
        tmp = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        for i in range(steps):
            f = 1 - i / steps
            sub = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
            ImageDraw.Draw(sub).ellipse(
                [(cx - rx * f) * S, (cy - ry * f) * S, (cx + rx * f) * S, (cy + ry * f) * S],
                fill=(*colour, round(255 * alpha / steps)),
            )
            tmp = Image.alpha_composite(tmp, sub)
        self.img = Image.alpha_composite(self.img, tmp)

    def downsample(self) -> Image.Image:
        """Redução com alpha pré-multiplicado; RGB zerado onde alpha = 0 (sem alpha sujo)."""
        small = self.img.convert("RGBa").resize((CELL, CELL), Image.BOX).convert("RGBA")
        mask = small.getchannel("A").point(lambda v: 255 if v > 0 else 0)
        return Image.composite(small, Image.new("RGBA", small.size, (0, 0, 0, 0)), mask)


def ring_segments():
    """Facetas (índice, a0, a1) em graus (0 = direita, 90 = atrás) fora da abertura de trás."""
    seg = 360.0 / RING["segments"]
    keep = []
    for i in range(RING["segments"]):
        a0, a1 = i * seg, (i + 1) * seg
        d = ((a0 + a1) / 2 - 90.0 + 180) % 360 - 180
        if abs(d) >= RING["back_gap_deg"] / 2:
            keep.append((i, a0, a1))
    return keep


def draw_ring(layer: Layer, t: float, alpha: float, width_mul: float, dissolve: float):
    if alpha <= 0:
        return
    h, r = ring_height(t), ring_radius(t)
    w = RING["width_m"] * width_mul
    for i, a0, a1 in ring_segments():
        if seg_rand(i) < dissolve:
            continue
        pts_out, pts_in = [], []
        for a in (a0, a1):
            ra = math.radians(a)
            pts_out.append(project((r + w / 2) * math.cos(ra), h, (r + w / 2) * math.sin(ra)))
            pts_in.append(project((r - w / 2) * math.cos(ra), h, (r - w / 2) * math.sin(ra)))
        mid = math.radians((a0 + a1) / 2)
        # faceta chapada: tom pela normal da aresta (luz de cima/esquerda)
        nx, ny = math.cos(mid), -math.sin(mid) * math.cos(PITCH)
        lit = 0.5 + 0.5 * -(nx * LIGHT[0] + ny * LIGHT[1])
        colour = mix(PAL["cyan_shade"], PAL["cyan"], min(1.0, lit * 1.6))
        colour = mix(colour, PAL["cyan_pale"], 0.40 * max(0.0, lit - 0.5) * 2)
        back = math.sin(mid) > 0
        a = alpha * (RING["back_alpha"] if back else 1.0)
        layer.poly(pts_out + pts_in[::-1], colour, a)


def draw_rings(layer: Layer, k: int):
    """Um único anel por quadro (sem rastro: um segundo anel leria como dois)."""
    draw_ring(layer, frame_t(k), RING["alpha_peak"] * RING["fade"][k], 1.0, RING["dissolve"][k])


def mote_state(m, t):
    x, z, h0, rise, delay, size, drift = m
    lt = (t - delay) / MOTE["life"]
    if lt < 0 or lt >= 1:
        return None
    y = h0 + rise * ease_out(lt)
    xx = x + drift * lt
    alpha = smooth(lt / 0.22) * (1 - smooth((lt - 0.50) / 0.50))
    scale = size * (0.70 + 0.30 * smooth(lt / 0.3)) * (1 - 0.30 * smooth((lt - 0.6) / 0.4))
    return xx, y, z, alpha, scale, lt


def draw_mote(layer: Layer, m, t):
    st = mote_state(m, t)
    if st is None:
        return
    x, y, z, alpha, scale, lt = st
    if alpha <= 0.002:
        return
    cx, cy = project(x, y, z)
    hw = MOTE["half_w_m"] * PPM * scale
    hh = MOTE["half_h_m"] * PPM * scale
    base = mix(PAL["brass"], PAL["brass_light"], 0.35 * lt)
    layer.ellipse_soft(cx, cy, hw * MOTE["halo_scale"], hh * MOTE["halo_scale"] * 0.8, base,
                       MOTE["halo_alpha"] * alpha)
    top, right, bottom, left = (cx, cy - hh), (cx + hw, cy), (cx, cy + hh), (cx - hw, cy)
    c = (cx + hw * 0.12, cy - hh * 0.10)  # vértice interno deslocado: plano largo facetado
    facets = [([left, top, c], 1.00), ([top, right, c], 0.72), ([right, bottom, c], 0.40), ([bottom, left, c], 0.60)]
    for pts, lum in facets:
        col = mix(PAL["brass_shade"], mix(base, PAL["brass_light"], 0.5), lum)
        layer.poly(pts, col, MOTE["alpha_peak"] * alpha)


def render_frame(k: int):
    """Retorna (quadro completo, só o anel, só os motes) do quadro k."""
    t = frame_t(k)
    ring = Layer()
    draw_rings(ring, k)
    motes = Layer()
    full = Layer()
    draw_rings(full, k)
    for m in sorted(MOTES, key=lambda m: -m[1]):  # mais distantes primeiro
        draw_mote(full, m, t)
        draw_mote(motes, m, t)
    return full.downsample(), ring.downsample(), motes.downsample()


# ---------------------------------------------------------------- verificações
def bbox_alpha(im: Image.Image, thr: int = 0):
    return im.getchannel("A").point(lambda v: 255 if v > thr else 0).getbbox()


def alpha_max_in(im: Image.Image, box) -> int:
    return im.getchannel("A").crop(tuple(round(v) for v in box)).getextrema()[1]


def validate(frames, rings, motes, sheet) -> dict:
    res = {"checks": [], "frames": []}

    def check(name, ok, detail):
        res["checks"].append({"name": name, "ok": bool(ok), "detail": detail})

    check("sheet_size_mode", sheet.size == (CELL * COLS, CELL * ROWS) and sheet.mode == "RGBA",
          f"{sheet.size} {sheet.mode}")
    margin_min = CELL
    dirty_total = green_total = threat_total = white_total = 0
    col_l = PIVOT[0] - CENTRE_COLUMN_M * PPM
    col_r = PIVOT[0] + CENTRE_COLUMN_M * PPM
    for k, (fr, rg, mo) in enumerate(zip(frames, rings, motes)):
        info = {"frame": k, "time_s": round(k * DURATION / FRAMES, 4)}
        cell = sheet.crop(((k % COLS) * CELL, (k // COLS) * CELL, (k % COLS + 1) * CELL, (k // COLS + 1) * CELL))
        info["sheet_cell_equals_frame"] = cell.tobytes() == fr.tobytes()
        raw = fr.tobytes()
        px = [tuple(raw[i:i + 4]) for i in range(0, len(raw), 4)]
        dirty_total += sum(1 for r, g, b, a in px if a == 0 and (r or g or b))
        info["alpha_sum"] = sum(p[3] for p in px)
        info["alpha_max"] = max(p[3] for p in px)
        info["cover_over_50pct"] = round(sum(1 for p in px if p[3] > 127) / len(px), 5)
        bb = bbox_alpha(fr)
        info["bbox"] = bb
        if bb:
            m = min(bb[0], bb[1], CELL - bb[2], CELL - bb[3])
            margin_min = min(margin_min, m)
            info["edge_margin_px"] = m
        g = th = wh = 0
        for r, gg, b, a in px:
            if a < 24:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, gg / 255, b / 255)
            hd = h * 360
            if s > 0.15 and GREEN_HUE[0] <= hd <= GREEN_HUE[1]:
                g += 1
            if s > 0.55 and THREAT_HUES[0] <= hd <= THREAT_HUES[1]:
                th += 1
            if s < OPAQUE_WHITE[0] and v > OPAQUE_WHITE[1] and a > OPAQUE_WHITE[2]:
                wh += 1
        green_total += g
        threat_total += th
        white_total += wh
        info["green_px"], info["threat_hue_px"], info["opaque_white_px"] = g, th, wh
        rb = bbox_alpha(rg)
        if rb:
            info["ring_bbox"] = rb
            info["ring_centre_x"] = (rb[0] + rb[2]) / 2
        # centro livre: miolo do anel (elipse interna a 75% do raio) vazio na camada do anel
        t = frame_t(k)
        cu, cv = project(0, ring_height(t), 0)
        rx = 0.75 * ring_radius(t) * PPM
        ry = rx * math.sin(PITCH)
        info["ring_inner_alpha_max"] = alpha_max_in(rg, (cu - rx * 0.7, cv - ry * 0.7, cu + rx * 0.7, cv + ry * 0.7))
        # coluna do corpo (|x| < 0,30 m) sem motes
        info["motes_in_body_column_alpha_max"] = alpha_max_in(mo, (col_l, 0, col_r, CELL))
        res["frames"].append(info)

    fr_ = res["frames"]
    last = FRAMES - 1
    check("frames_are_256_rgba", all(f.size == (CELL, CELL) and f.mode == "RGBA" for f in frames), f"{FRAMES} quadros")
    check("sheet_cells_equal_frames", all(f["sheet_cell_equals_frame"] for f in fr_), "célula k = quadro k")
    check("no_dirty_transparent_px", dirty_total == 0, f"{dirty_total} px com alpha 0 e RGB != 0")
    check("no_edge_cut_margin_ge_16px", margin_min >= 16, f"margem mínima {margin_min} px")
    check("final_frame_transparent", fr_[last]["alpha_max"] == 0, f"alpha máx. quadro {last} = {fr_[last]['alpha_max']}")
    check("frames_before_last_visible", all(f["alpha_sum"] > 0 for f in fr_[:last]), f"quadros 0–{last - 1} não vazios")
    peak = max(f["alpha_sum"] for f in fr_)
    check("fades_out", fr_[last - 1]["alpha_sum"] < 0.30 * peak, f"quadro {last - 1} < 30% do pico")
    check("no_green_hue", green_total == 0, f"{green_total} px em matiz {GREEN_HUE}")
    check("no_threat_red_orange", threat_total == 0, f"{threat_total} px saturados em matiz {THREAT_HUES}")
    check("no_opaque_white", white_total == 0,
          f"{white_total} px com saturação < {OPAQUE_WHITE[0]}, valor > {OPAQUE_WHITE[1]} e alpha > {OPAQUE_WHITE[2]}")
    check("no_wide_flash", all(f["cover_over_50pct"] <= FLASH_MAX_COVER for f in fr_),
          f"cobertura alpha>50% ≤ {FLASH_MAX_COVER:.0%} por quadro")
    check("centre_free", all(f["ring_inner_alpha_max"] == 0 for f in fr_), "miolo do anel vazio (sem disco)")
    check("body_column_free_of_motes", all(f["motes_in_body_column_alpha_max"] == 0 for f in fr_),
          f"nenhum mote em |x| < {CENTRE_COLUMN_M} m")
    # pivô: anel centrado em x = pivô e base = pivô − altura + raio projetado
    piv_ok = True
    steps = []
    prev_bottom = None
    for k, f in enumerate(fr_):
        if "ring_bbox" not in f or RING["dissolve"][k] > 0:
            continue
        t = frame_t(k)
        exp_bottom = project(0, ring_height(t), -(ring_radius(t) + RING["width_m"] / 2))[1]
        f["ring_bottom_expected"] = round(exp_bottom, 2)
        if abs(f["ring_centre_x"] - PIVOT[0]) > 1.0 or abs(f["ring_bbox"][3] - exp_bottom) > 1.5:
            piv_ok = False
        if prev_bottom is not None:
            steps.append(round(abs(f["ring_bbox"][3] - prev_bottom), 2))
        prev_bottom = f["ring_bbox"][3]
    check("pivot_consistent", piv_ok, f"anel centrado em x={PIVOT[0]} e base coerente com pivô y={PIVOT[1]}")
    res["ring_bottom_steps_px"] = steps
    check("continuity_no_jumps", steps and max(steps) <= MAX_STEP_PX,
          f"deslocamento máx. do anel entre quadros {max(steps) if steps else '-'} px ≤ {MAX_STEP_PX}")
    reach = max(RING["radius_m"]) + RING["width_m"] / 2
    reach = max([reach] + [math.hypot(abs(m[0]) + abs(m[6]) + MOTE["half_w_m"] * m[5], m[1]) for m in MOTES])
    check("gameplay_radius_le_0_8m", reach <= 0.8, f"alcance máx. {reach:.3f} m")
    check("particle_budget_le_20", len(MOTES) + 1 <= 20, f"{len(MOTES)} motes + 1 anel")
    res["ok"] = all(c["ok"] for c in res["checks"])
    return res


# ---------------------------------------------------------------- revisão
def _capture_crop(cap, feet, scale, cw):
    ox, oy = round(feet[0] - PIVOT[0] * scale), round(feet[1] - PIVOT[1] * scale)
    return cap.crop((ox, oy, ox + cw, oy + cw)).copy()


def review_floor(frames) -> Image.Image:
    """Quadros sobre a captura real F05 (1920×1080) na escala de gameplay e ampliados 2×."""
    cap = Image.open(FLOOR_CAPTURE).convert("RGBA")
    scale = FLOOR_PPM_1080 / PPM
    cw = round(CELL * scale)
    spots = [FLOOR_FEET_1080, (700, 330)]  # com o Hero e piso limpo
    tile = 2 * cw
    out = Image.new("RGBA", (COLS * tile, ROWS * tile * len(spots) + 6), (20, 20, 24, 255))
    for si, spot in enumerate(spots):
        for k, fr in enumerate(frames):
            crop = _capture_crop(cap, spot, scale, cw)
            crop.alpha_composite(fr.resize((cw, cw), Image.LANCZOS))
            out.paste(crop.resize((tile, tile), Image.NEAREST),
                      ((k % COLS) * tile, si * (ROWS * tile + 6) + (k // COLS) * tile))
    return out.convert("RGB")


def review_backgrounds(frames) -> Image.Image:
    bgs = ["#3E4152", "#55596B", "#0E131A", "#B9C4CF"]
    out = Image.new("RGB", (FRAMES * CELL // 2, CELL // 2 * len(bgs)), (0, 0, 0))
    for r, bg in enumerate(bgs):
        for k, fr in enumerate(frames):
            tile = Image.new("RGBA", (CELL, CELL), (*rgb(bg), 255))
            d = ImageDraw.Draw(tile)
            d.line([(PIVOT[0] - 6, PIVOT[1]), (PIVOT[0] + 6, PIVOT[1])], fill=(255, 255, 255, 90))
            d.line([(PIVOT[0], PIVOT[1] - 6), (PIVOT[0], PIVOT[1] + 6)], fill=(255, 255, 255, 90))
            tile.alpha_composite(fr)
            out.paste(tile.convert("RGB").resize((CELL // 2, CELL // 2), Image.LANCZOS), (k * CELL // 2, r * CELL // 2))
    return out


def review_danger(frames) -> Image.Image:
    """Pico do efeito ao lado de um telegraph de Boss (disco vermelho preenchido) na mesma escala,
    para conferir que sucesso e perigo não se confundem."""
    pk = 4
    out = Image.new("RGBA", (CELL * 2, CELL), (*rgb("#3E4152"), 255))
    left = Image.new("RGBA", (CELL, CELL), (0, 0, 0, 0))
    d = ImageDraw.Draw(left)
    r = 1.2 * PPM  # telegraph de exemplo, raio 1,2 m, projetado
    cx, cy = PIVOT[0], PIVOT[1] - 30
    d.ellipse([cx - r, cy - r * math.sin(PITCH), cx + r, cy + r * math.sin(PITCH)], fill=(255, 51, 38, 120),
              outline=(89, 13, 13, 255), width=5)
    out.alpha_composite(left, (0, 0))
    out.alpha_composite(frames[pk], (CELL, 0))
    return out.convert("RGB")


def preview_gif(frames, path: Path):
    cap = Image.open(FLOOR_CAPTURE).convert("RGBA")
    scale = FLOOR_PPM_1080 / PPM
    cw = round(CELL * scale)
    base = _capture_crop(cap, FLOOR_FEET_1080, scale, cw)
    seq = []
    for fr in frames:
        im = base.copy()
        im.alpha_composite(fr.resize((cw, cw), Image.LANCZOS))
        seq.append(im.resize((cw * 3, cw * 3), Image.NEAREST).convert("RGB"))
    hold = base.resize((cw * 3, cw * 3), Image.NEAREST).convert("RGB")
    ms = round(DURATION / FRAMES * 1000)
    seq[0].save(path, save_all=True, append_images=seq[1:] + [hold], duration=[ms] * FRAMES + [600], loop=0,
                optimize=False, disposal=1)


# ---------------------------------------------------------------- saída
def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_meta(png: Path):
    meta = png.with_suffix(png.suffix + ".meta")
    if meta.exists():
        return False
    guid = uuid.UUID(hashlib.sha256(("Eldoria/A05/" + png.name).encode()).hexdigest()[:32]).hex
    meta.write_text(META_TEMPLATE.read_text(encoding="utf-8").replace("{{GUID}}", guid), encoding="utf-8", newline="\n")
    return True


def timing_json() -> dict:
    fd = DURATION / FRAMES
    pivot_norm = (PIVOT[0] / CELL, 1 - PIVOT[1] / CELL)  # Unity: origem embaixo-esquerda
    quad_m = CELL / PPM
    return {
        "asset_id": "FX-LEVEL",
        "sheet": f"../{SHEET_NAME}",
        "sheet_size": [CELL * COLS, CELL * ROWS],
        "grid": {"columns": COLS, "rows": ROWS, "cell": [CELL, CELL],
                 "order": "linha a linha, da esquerda para a direita, de cima para baixo"},
        "frames": [{"index": k, "cell": [k % COLS, k // COLS], "start_s": round(k * fd, 4), "duration_s": round(fd, 4),
                    "ring_height_m": round(ring_height(frame_t(k)), 3), "ring_radius_m": round(ring_radius(frame_t(k)), 3),
                    "file": f"frames/FX-LEVEL-F{k}.png"} for k in range(FRAMES)],
        "duration_s": DURATION,
        "fps": round(FRAMES / DURATION, 3),
        "loop": False,
        "final_frame": f"quadro {FRAMES - 1} totalmente transparente (fim do efeito)",
        "pivot": {
            "meaning": "centro dos pés do Hero, sobre o piso",
            "cell_px_from_top_left": list(PIVOT),
            "cell_normalized_unity_bottom_left": [round(pivot_norm[0], 4), round(pivot_norm[1], 4)],
        },
        "scale": {
            "pixels_per_metre_at_feet": PPM,
            "cell_size_m": round(quad_m, 3),
            "gameplay_radius_m": 0.8,
            "in_game_reference": f"~{FLOOR_PPM_1080:.0f} px/m perto do Hero a 1920×1080 → sheet exibida a ~{FLOOR_PPM_1080 / PPM:.2f}×",
        },
        "camera": {"pitch_deg": 45, "offset": [0, 10, -10],
                   "note": "Billboard voltado para a câmera; anel e alturas já vêm projetados a 45°."},
        "unity_integration_proposal": {
            "status": "Proposta; NÃO VALIDADO NO EDITOR. Integração é tarefa de código separada.",
            "texture_import": "Default, sRGB, alphaIsTransparency, clamp, bilinear, mipmaps on (como FX_Spark).",
            "material": "URP/Particles/Unlit, Surface Transparent, Blend Alpha, cor branca; opacidade multiplicada pela opção de flash do F05 (options.Current.flash).",
            "particle_system": {
                "max_particles": 1,
                "emission": "1 burst por PlayerProgression.LeveledUp, sem loop",
                "start_lifetime_s": DURATION,
                "start_size_m": round(quad_m, 3),
                "simulation_space": "Local ao Hero (o anel acompanha o corpo enquanto sobe)",
                "texture_sheet_animation": {"mode": "Grid", "tiles": [COLS, ROWS], "animation": "Whole Sheet",
                                             "frame_over_time": "linear 0→1", "cycles": 1},
                "renderer": {"render_mode": "Billboard",
                             "pivot": [0, round(0.5 - pivot_norm[1], 4), -0.2],
                             "pivot_note": ("y desloca o quad para o pivô dos pés coincidir com a partícula; z (≈0,4 m) "
                                            "aproxima o quad da câmera para o arco da frente não ser escondido pelas pernas. "
                                            "A abertura de 120° atrás evita que o arco de trás seja desenhado sobre o tronco. "
                                            "Conferir os sinais de y e z no Editor.")},
            },
            "rules": [
                "Disparar só em PlayerProgression.LeveledUp; é só apresentação e não lê nem altera HP, XP ou fórmula.",
                "Vários níveis no mesmo ganho de XP: um único restart do mesmo emissor (Clear + Emit); nenhum efeito extra de cura ou dano.",
                "A cura de LevelUp continua sem disparar o FX-HEAL (regra atual do F05).",
                "Pausa congela; morte, reset e desativação chamam Clear; opção de flash reduz a opacidade e 0 desliga.",
                "Não é AnimationClip corporal nem altera o Animator do Hero.",
            ],
        },
    }


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", type=Path, help="escreve só sheet e quadros nesta pasta")
    args = ap.parse_args()

    frames, rings, motes = [], [], []
    for k in range(FRAMES):
        f, r, m = render_frame(k)
        frames.append(f)
        rings.append(r)
        motes.append(m)
    sheet = Image.new("RGBA", (CELL * COLS, CELL * ROWS), (0, 0, 0, 0))
    for k, f in enumerate(frames):
        sheet.paste(f, ((k % COLS) * CELL, (k // COLS) * CELL))

    if args.out:
        args.out.mkdir(parents=True, exist_ok=True)
        sheet.save(args.out / SHEET_NAME, optimize=False)
        for k, f in enumerate(frames):
            f.save(args.out / f"FX-LEVEL-F{k}.png", optimize=False)
        review_floor(frames).save(args.out / "review_floor.png", optimize=False)
        review_backgrounds(frames).save(args.out / "review_backgrounds.png", optimize=False)
        review_danger(frames).save(args.out / "review_danger.png", optimize=False)
        res = validate(frames, rings, motes, sheet)
        for c in res["checks"]:
            print(("ok  " if c["ok"] else "FAIL") + f" {c['name']}: {c['detail']}")
        return 0 if res["ok"] else 1

    (SOURCE / "frames").mkdir(parents=True, exist_ok=True)
    sheet_path = STAGING / SHEET_NAME
    sheet.save(sheet_path, optimize=False)
    created_meta = write_meta(sheet_path)
    files = {SHEET_NAME: sheet_path}
    for k, f in enumerate(frames):
        p = SOURCE / "frames" / f"FX-LEVEL-F{k}.png"
        f.save(p, optimize=False)
        files[f"frames/FX-LEVEL-F{k}.png"] = p
    review_floor(frames).save(SOURCE / "review_floor.png", optimize=False)
    review_backgrounds(frames).save(SOURCE / "review_backgrounds.png", optimize=False)
    review_danger(frames).save(SOURCE / "review_danger.png", optimize=False)
    preview_gif(frames, SOURCE / "preview_floor.gif")

    (SOURCE / "FX-LEVEL.timing.json").write_text(json.dumps(timing_json(), ensure_ascii=False, indent=2) + "\n",
                                                 encoding="utf-8", newline="\n")
    source = {
        "asset_id": "FX-LEVEL", "catalog_id": "A05", "generator": "Tools/FxLevelGenerator/generate.py",
        "supersampling": S, "cell": CELL, "grid": [COLS, ROWS], "duration_s": DURATION, "ppm": PPM,
        "pivot_px": PIVOT, "camera_pitch_deg": 45, "light": LIGHT, "hero_height_m": HERO_HEIGHT_M,
        "palette": PAL, "ring": RING,
        "ring_segments_deg": [[a0, a1] for _i, a0, a1 in ring_segments()],
        "motes": [dict(zip(["x_m", "z_m", "start_height_m", "rise_m", "delay", "size", "drift_x_m"], m)) for m in MOTES],
        "mote": MOTE, "references": {
            "floor_capture": str(FLOOR_CAPTURE.relative_to(ROOT)).replace("\\", "/"),
            "xp_orb": "Tools/Lemo3DGenerator/src/production/ItemModels.mjs M.xp #4FD8FF, M.xpRing #DFFBFF (torus 12 segmentos)",
            "f05_level_burst": "Assets/_Project/Scripts/UI/PlayerRewardFeedback.cs OnLevel: 12 partículas Color(.9,.76,.42)",
            "boss_telegraph": "Assets/_Project/Scripts/Editor/DungeonFloorSceneBuilder.cs Mat_TelegraphFill (1,.2,.15)",
            "hero_height": "Assets/_Project/Scripts/Editor/DungeonMvpSceneBuilder.cs controller.height = 2",
            "camera": "Assets/_Project/Scripts/Editor/ArtTestSceneBuilder.cs (offset 0,10,-10, pitch 45°)",
        },
    }
    (SOURCE / "FX-LEVEL.source.json").write_text(json.dumps(source, ensure_ascii=False, indent=2) + "\n",
                                                 encoding="utf-8", newline="\n")
    res = validate(frames, rings, motes, sheet)
    res["sha256"] = {name: sha(p) for name, p in files.items()}
    res["meta_created"] = created_meta
    (SOURCE / "FxLevel.validation.json").write_text(json.dumps(res, ensure_ascii=False, indent=2) + "\n",
                                                    encoding="utf-8", newline="\n")
    for c in res["checks"]:
        print(("ok  " if c["ok"] else "FAIL") + f" {c['name']}: {c['detail']}")
    return 0 if res["ok"] else 1


if __name__ == "__main__":
    sys.exit(main())

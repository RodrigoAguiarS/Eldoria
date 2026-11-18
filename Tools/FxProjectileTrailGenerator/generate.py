"""A09 / FX-PROJECTILE-TRAIL — gerador determinístico da textura da cauda do projétil hostil (Eldoria).

Desenha UMA textura 128×32 RGBA para TrailRenderer (textureMode Stretch): uma gota compacta e
afilada, cabeça larga laranja de ameaça (#FF9A2E) que escurece até ferrugem (#8A4A22) e some em
alpha 0, com um centro fino mais quente ao longo do eixo. Facetas chapadas (luz de cima), sem
faísca fora do envelope, explosão, fumaça, ciano, texto nem fundo.

Convenção de UV (igual ao A07 / FX_SlashTrail): U percorre o trajeto; u = 0 é a cabeça (junto ao
projétil, ponto mais novo) e u = 1 a ponta da cauda (ponto mais antigo). HEAD_AT_U0 = False espelha.

Polígonos com supersampling 8× e redução com alpha pré-multiplicado; envelope de alpha por coluna
aplicado depois. Sem IA generativa nem asset de terceiros.

Uso (Python 3.11 + Pillow 13 de .claude/skills/create-game-assets/scripts/requirements.txt + NumPy):
    python Tools/FxProjectileTrailGenerator/generate.py            # PNG, .meta (só se faltar), Source~
    python Tools/FxProjectileTrailGenerator/generate.py --out DIR  # só PNG e pranchas em DIR

Sai com código 1 se alguma verificação falhar.
"""
from __future__ import annotations

import argparse
import colorsys
import hashlib
import json
import math
import re
import sys
import uuid
from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
STAGING = ROOT / "Assets/_Project/Art/ProductionStaging/FX-PROJECTILE-TRAIL"
SOURCE = STAGING / "Source~"
ASSET_ID = "FX-PROJECTILE-TRAIL"
PNG_NAME = "FX-PROJECTILE-TRAIL.png"
META_TEMPLATE = ROOT / "Tools/FxHealGenerator/texture.meta.template"  # cópia de FX_Spark.png.meta
SLASH = ROOT / "Assets/_Project/Art/Items/VFX/FX_SlashTrail.png"
DASH = ROOT / "Assets/_Project/Art/ProductionStaging/FX-DASH/FX-DASH-Trail.png"
PROJECTILE_CS = ROOT / "Assets/_Project/Scripts/Systems/Projectile.cs"
POOL_CS = ROOT / "Assets/_Project/Scripts/Systems/ProjectilePool.cs"
ENEMY_ASSETS = ROOT / "Assets/_Project/ScriptableObjects/Enemies"
MAT_PROJECTILE = ROOT / "Assets/_Project/Materials/Mat_Projectile.mat"
FLOOR_CAPTURE = ROOT / "Reports/F05/Visual-final/1920-collection.png"
FLOOR_FEET_1080 = (959, 543)  # centro das botas do Hero na captura F05 1920×1080 (mesma constante do A07)
FLOOR_PPM_1080 = 75.0  # px por metro no plano do piso perto do Hero (ladrilho 2 m ≈ 150 px)
PITCH = math.radians(45.0)

S = 8  # supersampling
W, H = 128, 32
HEAD_AT_U0 = True

# ---------------------------------------------------------------- paleta (sRGB)
# Laranja de ameaça #FF9A2E e ferrugem #8A4A22 (ficha A09; mesmas cores da brasa e da parede de
# fenda do A08). "core" é o centro fino, um pouco mais claro no mesmo matiz (não branco);
# "shade" é a faceta de baixo da cauda, ferrugem mais escura.
PAL = {
    "core": "#FFB866",
    "head": "#FF9A2E",
    "rust": "#8A4A22",
    "shade": "#6A3618",
}

# ---------------------------------------------------------------- gota (px na textura 128×32)
# Linha central reta em y = cy (UV horizontal plano, sem perspectiva). Da raiz (x0, cabeça) à
# ponta (x1): raiz chanfrada com head_hw × hw, ponto mais largo em peak (0..1), afinando até 0.
DROP = {"x0": 2.0, "x1": 121.0, "cy": 16.0, "hw": 9.0, "peak": 0.17, "head_hw": 0.74,
        "tail_power": 1.2, "segments": 7, "alpha": 1.0}
# Centro fino: faixa quente afilada sobre a linha central, só na metade da cabeça.
CORE = {"from": 0.02, "to": 0.58, "half_px": 1.5, "alpha": 0.95}
# Gradiente de cor ao longo de t: laranja até head_hold, ferrugem a partir de rust_at.
GRAD = {"head_hold": 0.18, "rust_at": 0.66}

# Envelope de alpha por coluna (fração de W): sobe de 0 na cabeça (escondida no projétil), platô,
# cai até 0 na ponta.
ENV = {"head_in": 0.07, "tail_from": 0.30, "tail_power": 1.5, "peak": 0.92}

# ---------------------------------------------------------------- integração proposta
ORB_DIAMETER_M = 0.40  # esfera atual (Unity Sphere × 0,4) e envelope do A08
TRAIL = {
    "time_s": 0.05,  # 12 m/s × 0,05 s = 0,60 m (ficha: < 0,15 s e 0,6 m)
    "width_m": 0.26,  # 65% do diâmetro do orbe: a cauda nunca é mais larga que o perigo real
    "width_curve": [[0.0, 1.0], [1.0, 0.8]],
    "mount_back_m": 0.14,  # filho atrás do centro, ainda dentro do orbe (raio 0,20): a parte larga da gota nasce na borda dele
    "min_vertex_distance_m": 0.05,
}
LIMITS = {"tail_s": 0.15, "tail_m": 0.60, "max_width_frac_of_orb": 0.75, "extra_particles": 6}

COLD_HUES = (165.0, 265.0)  # ciano/azul do Hero e magia amiga
GREEN_HUE = (70.0, 165.0)
OPAQUE_WHITE = (0.18, 0.92, 200)
MAX_ALPHA = 240
MAX_COVER = 0.35  # fração da textura com alpha > 50%
HEAD_FADE_PX = 3  # primeiras colunas (dentro do orbe): alpha ≤ 25% do pico
TAIL_FADE = 0.08  # últimos 8% de U: alpha ≤ 10% do pico
MIN_V_MARGIN = 3
STONES = ["#3E4152", "#55596B"]
TELEGRAPH = (255, 51, 38)  # Mat_TelegraphFill (1, .2, .15)


def rgb(h: str) -> tuple[int, int, int]:
    h = h.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def mix(a, b, t):
    a, b = rgb(a) if isinstance(a, str) else a, rgb(b) if isinstance(b, str) else b
    t = min(max(t, 0.0), 1.0)
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


def smoothstep(e0, e1, x):
    t = min(max((x - e0) / (e1 - e0), 0.0), 1.0)
    return t * t * (3 - 2 * t)


# ---------------------------------------------------------------- geometria
def drop_rails():
    """Pontos (x, y_top, y_bot, t) ao longo da gota; raiz chanfrada, ponta fecha em 0."""
    d = DROP
    n = d["segments"]
    pts = []
    for i in range(n + 1):
        # segmentos mais curtos perto da cabeça (onde a gota é mais larga e a forma muda mais)
        t = (i / n) ** 1.35
        x = d["x0"] + (d["x1"] - d["x0"]) * t
        if t <= d["peak"]:
            w = d["head_hw"] + (1 - d["head_hw"]) * math.sin(0.5 * math.pi * t / d["peak"])
        else:
            w = (1 - (t - d["peak"]) / (1 - d["peak"])) ** d["tail_power"]
        hw = d["hw"] * w
        pts.append((x, d["cy"] - hw, d["cy"] + hw, t))
    return pts


def base_colour(t: float):
    g = smoothstep(GRAD["head_hold"], GRAD["rust_at"], t)
    return mix(PAL["head"], PAL["rust"], g)


def facets():
    """Lista de (pontos, cor, alpha, papel) de trás para a frente."""
    out = []
    rails = drop_rails()
    cy = DROP["cy"]
    for i in range(len(rails) - 1):
        x0, t0, b0, u0 = rails[i]
        x1, t1, b1, u1 = rails[i + 1]
        um = 0.5 * (u0 + u1)
        base = base_colour(um)
        alt = 0.10 if i % 2 == 0 else -0.06  # ritmo facetado simples: planos alternados
        upper = mix(base, PAL["core"], 0.22 + alt) if alt > 0 else mix(base, PAL["head"], 0.10)
        lower = mix(base, PAL["shade"], 0.32 - alt)
        out.append(([(x0, t0), (x1, t1), (x1, cy), (x0, cy)], upper, DROP["alpha"], "upper"))
        out.append(([(x0, cy), (x1, cy), (x1, b1), (x0, b0)], lower, DROP["alpha"], "lower"))
    out.extend(core_facets(rails))
    return out


def core_facets(rails):
    """Centro fino: losangos afilados sobre a linha central, mais quentes, nas duas pontas zerados."""
    res = []
    cy = DROP["cy"]

    def wid(u):
        m = (u - CORE["from"]) / (CORE["to"] - CORE["from"])
        m = min(max(m, 0.0), 1.0)
        return CORE["half_px"] * math.sin(math.pi * min(1.0, m * 1.6)) ** 0.6 if m < 0.31 else \
            CORE["half_px"] * (1 - (m - 0.31) / 0.69) ** 0.8

    for i in range(len(rails) - 1):
        x0, *_, u0 = rails[i]
        x1, *_, u1 = rails[i + 1]
        if u1 <= CORE["from"] or u0 >= CORE["to"]:
            continue
        w0, w1 = wid(u0), wid(u1)
        res.append(([(x0, cy - w0), (x1, cy - w1), (x1, cy + w1 * 0.6), (x0, cy + w0 * 0.6)],
                    rgb(PAL["core"]), CORE["alpha"], "core"))
    return res


def envelope(x: float) -> float:
    u = x / (W - 1)
    head = smoothstep(0.0, ENV["head_in"], u)
    tail = 1.0 if u <= ENV["tail_from"] else (1 - (u - ENV["tail_from"]) / (1 - ENV["tail_from"])) ** ENV["tail_power"]
    return head * max(tail, 0.0)


def render() -> Image.Image:
    big = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    for pts, col, a, _role in facets():
        tmp = Image.new("RGBA", big.size, (0, 0, 0, 0))
        ImageDraw.Draw(tmp).polygon([(x * S, y * S) for x, y in pts], fill=(*col, round(255 * a)))
        big = Image.alpha_composite(big, tmp)
    small = big.convert("RGBa").resize((W, H), Image.BOX).convert("RGBA")
    arr = np.asarray(small).astype(np.float64)
    env = np.array([envelope(x) for x in range(W)]) * ENV["peak"]
    arr[:, :, 3] = np.round(arr[:, :, 3] * env[None, :])
    arr[arr[:, :, 3] == 0, :3] = 0
    im = Image.fromarray(arr.astype(np.uint8), "RGBA")
    return im if HEAD_AT_U0 else im.transpose(Image.FLIP_LEFT_RIGHT)


# ---------------------------------------------------------------- leitura do projeto (só leitura)
def luminance(c) -> float:
    def ch(x):
        x /= 255
        return x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4
    return 0.2126 * ch(c[0]) + 0.7152 * ch(c[1]) + 0.0722 * ch(c[2])


def contrast(a, b) -> float:
    la, lb = sorted((luminance(a), luminance(b)), reverse=True)
    return (la + 0.05) / (lb + 0.05)


def projectile_params() -> dict:
    """Valores serializados padrão de Projectile.cs/ProjectilePool.cs e velocidades dos EnemyData."""
    src = PROJECTILE_CS.read_text(encoding="utf-8")
    pool = POOL_CS.read_text(encoding="utf-8")
    out = {}
    for name in ("radius", "lifetime"):
        m = re.search(rf"float {name} = ([0-9.]+)f;", src)
        out[name] = float(m.group(1)) if m else None
    for name in ("defaultCapacity", "maxSize"):
        m = re.search(rf"int {name} = ([0-9]+);", pool)
        out[name] = int(m.group(1)) if m else None
    speeds = {}
    for f in sorted(ENEMY_ASSETS.glob("*.asset")):
        m = re.search(r"projectileSpeed: ([0-9.]+)", f.read_text(encoding="utf-8"))
        if m:
            speeds[f.stem] = float(m.group(1))
    out["projectile_speeds"] = speeds
    out["max_speed"] = max(speeds.values()) if speeds else None
    m = re.search(r"_BaseColor: \{r: ([0-9.]+), g: ([0-9.]+), b: ([0-9.]+)", MAT_PROJECTILE.read_text(encoding="utf-8"))
    out["mat_projectile_base"] = [float(m.group(i)) for i in (1, 2, 3)] if m else [1.0, 0.55, 0.1]
    out["timescale_guard"] = "Time.timeScale <= 0f" in src
    out["pause_guard_note"] = "Projectile.Update retorna com timeScale 0 (não anda nem expira)." if out["timescale_guard"] else ""
    return out


def components(mask: np.ndarray) -> int:
    """Componentes 8-conexos (sem SciPy)."""
    seen = np.zeros_like(mask, dtype=bool)
    n = 0
    hh, ww = mask.shape
    for y0, x0 in zip(*np.nonzero(mask)):
        if seen[y0, x0]:
            continue
        n += 1
        q = deque([(y0, x0)])
        seen[y0, x0] = True
        while q:
            y, x = q.popleft()
            for dy in (-1, 0, 1):
                for dx in (-1, 0, 1):
                    yy, xx = y + dy, x + dx
                    if 0 <= yy < hh and 0 <= xx < ww and mask[yy, xx] and not seen[yy, xx]:
                        seen[yy, xx] = True
                        q.append((yy, xx))
    return n


def mean_colour(a: np.ndarray, cols: slice):
    sub = a[:, cols]
    w = sub[:, :, 3].astype(np.float64)
    return (sub[:, :, :3] * w[:, :, None]).sum(axis=(0, 1)) / max(w.sum(), 1e-6)


# ---------------------------------------------------------------- verificações
def validate(im: Image.Image) -> dict:
    res = {"checks": []}

    def check(name, ok, detail):
        res["checks"].append({"name": name, "ok": bool(ok), "detail": detail})

    a = np.asarray(im).astype(np.int32)
    if not HEAD_AT_U0:
        a = a[:, ::-1]
    al = a[:, :, 3]
    check("size_mode", im.size == (W, H) and im.mode == "RGBA", f"{im.size} {im.mode}")
    dirty = int(((al == 0) & (a[:, :, :3].sum(axis=2) > 0)).sum())
    check("no_dirty_transparent_px", dirty == 0, f"{dirty} px com alpha 0 e RGB != 0")
    border = int(max(al[0].max(), al[-1].max(), al[:, 0].max(), al[:, -1].max()))
    check("transparent_border", border == 0, f"alpha máx. na moldura de 1 px = {border}")
    rows = np.where(al.max(axis=1) > 0)[0]
    vmargin = int(min(rows[0], H - 1 - rows[-1]))
    check("v_margin", vmargin >= MIN_V_MARGIN, f"margem vertical {vmargin} px (≥ {MIN_V_MARGIN})")
    amax = int(al.max())
    res["alpha_max"] = amax
    check("alpha_ceiling", amax <= MAX_ALPHA, f"alpha máx. {amax} ≤ {MAX_ALPHA} (sem miolo totalmente opaco)")
    col_max = al.max(axis=0)
    hf = int(col_max[:HEAD_FADE_PX].max())
    nt = round(W * TAIL_FADE)
    tf = int(col_max[W - nt:].max())
    check("fade_both_ends", hf <= 0.25 * amax and tf <= 0.10 * amax,
          f"alpha máx. nas {HEAD_FADE_PX} colunas da cabeça {hf} ≤ 25% do pico ({0.25 * amax:.0f}); "
          f"nos últimos {nt} px da cauda {tf} ≤ 10% ({0.10 * amax:.0f})")
    step = int(np.abs(np.diff(col_max)).max())
    check("smooth_column_profile", step <= 40, f"maior salto do alpha máx. entre colunas vizinhas = {step} (≤ 40)")

    # gota: largura (linhas com alpha ≥ 64) máxima no primeiro terço e não crescente depois
    width = (al >= 64).sum(axis=0)
    wmax_col = int(np.argmax(width))
    after = width[wmax_col:]
    regrow = int(max(0, (np.maximum.accumulate(after[::-1])[::-1] - after).max())) if len(after) else 0
    grow = int(max(0, np.max(after[1:] - np.minimum.accumulate(after)[:-1]))) if len(after) > 1 else 0
    res["width_profile_px"] = [int(x) for x in width]
    check("teardrop_profile", wmax_col <= 0.35 * W and grow <= 1,
          f"mais larga na coluna {wmax_col} ({width[wmax_col]} px, ≤ 35% de U); depois afina sem "
          f"alargar mais de 1 px (máx. {grow})")
    tip = int(np.nonzero(col_max > 0)[0].max())
    check("compact_tip", width[int(0.8 * W):].max() <= 2,
          f"nos últimos 20% de U a largura com alpha ≥ 64 é ≤ 2 px ({int(width[int(0.8 * W):].max())}); "
          f"último px visível na coluna {tip}")
    ncomp = components(al >= 8)
    check("no_sparks_outside_envelope", ncomp == 1, f"{ncomp} componente(s) com alpha ≥ 8 (1 = só a gota)")

    # centro fino: px próximos da cor "core" formam uma faixa de ≤ 4 px ao longo do eixo
    core = np.array(rgb(PAL["core"]))
    near = (np.linalg.norm(a[:, :, :3] - core, axis=2) < 36) & (al >= 64)
    core_h = near.sum(axis=0)
    core_cols = int((core_h > 0).sum())
    res["core_columns"] = core_cols
    check("thin_hot_centre", core_cols >= 0.2 * W and core_h.max() <= 4,
          f"centro quente em {core_cols} colunas (≥ 20% de U), altura máx. {int(core_h.max())} px (≤ 4)")
    cover = float((al > 127).mean())
    res["cover_over_50pct"] = round(cover, 4)
    check("no_explosive_area", cover <= MAX_COVER, f"cobertura alpha>50% {cover:.1%} ≤ {MAX_COVER:.0%}")

    vis = a[al >= 24]
    green = cold = white = 0
    for r, g, b, aa in vis:
        h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
        hd = h * 360
        green += s > 0.15 and GREEN_HUE[0] <= hd < GREEN_HUE[1]
        cold += s > 0.15 and COLD_HUES[0] <= hd <= COLD_HUES[1]
        white += s < OPAQUE_WHITE[0] and v > OPAQUE_WHITE[1] and aa > OPAQUE_WHITE[2]
    check("no_cyan_friendly_magic", cold == 0, f"{cold} px em matiz ciano/azul {COLD_HUES}")
    check("no_green_hue", green == 0, f"{green} px em matiz {GREEN_HUE}")
    check("no_opaque_white", white == 0, f"{white} px de branco opaco")
    w = vis[:, 3].astype(np.float64)
    mean = (vis[:, :3] * w[:, None]).sum(axis=0) / w.sum()
    h, s, v = colorsys.rgb_to_hsv(*(mean / 255))
    res["mean_rgb"] = [round(c) for c in mean]
    res["mean_hsv"] = [round(float(h) * 360, 1), round(float(s), 3), round(float(v), 3)]
    check("reads_hostile_orange", 18 <= h * 360 <= 36 and s >= 0.6,
          f"média HSV {res['mean_hsv']} (laranja #FF9A2E = 31°, ferrugem #8A4A22 = 23°)")
    head_c = mean_colour(a, slice(round(0.08 * W), round(0.22 * W)))
    tail_c = mean_colour(a, slice(round(0.55 * W), round(0.80 * W)))
    dh = float(np.linalg.norm(head_c - np.array(rgb(PAL["head"]))))
    dt = float(np.linalg.norm(tail_c - np.array(rgb(PAL["rust"]))))
    res["head_mean_rgb"] = [round(c) for c in head_c]
    res["tail_mean_rgb"] = [round(c) for c in tail_c]
    check("orange_to_rust_gradient", dh <= 45 and dt <= 45 and luminance(head_c) > luminance(tail_c),
          f"cabeça média {res['head_mean_rgb']} a {dh:.0f} de #FF9A2E; cauda {res['tail_mean_rgb']} a "
          f"{dt:.0f} de #8A4A22 (≤ 45); a cabeça é mais clara")

    res["contrast_on_stone"] = {}
    sel = al > 127
    for bg in STONES:
        base = np.array(rgb(bg), dtype=np.float64)
        comp = a[:, :, :3] * (al[:, :, None] / 255) + base * (1 - al[:, :, None] / 255)
        res["contrast_on_stone"][bg] = round(contrast(comp[sel].mean(axis=0), base), 2)
    cmin = min(res["contrast_on_stone"].values())
    check("readable_on_stone", cmin >= 2.0,
          f"contraste WCAG da cauda composta (alpha>50%) sobre a pedra {STONES}: mín. {cmin:.2f} ≥ 2,0")

    # diferente do rastro do dash (azul do Hero) e do arco de ataque (cinza)
    da = np.asarray(Image.open(DASH).convert("RGBA")).astype(np.int32)
    dvis = da[da[:, :, 3] >= 24]
    dw = dvis[:, 3].astype(np.float64)
    dmean = (dvis[:, :3] * dw[:, None]).sum(axis=0) / dw.sum()
    dh_ = colorsys.rgb_to_hsv(*(dmean / 255))[0] * 360
    hue_gap = abs(((h * 360 - dh_) + 180) % 360 - 180)
    sl = np.asarray(Image.open(SLASH).convert("RGBA")).astype(np.int32)
    sl_vis = sl[sl[:, :, 3] >= 24]
    sl_sat = float(np.mean([colorsys.rgb_to_hsv(*(p[:3] / 255))[1] for p in sl_vis]))
    res["compare"] = {"dash_mean_hue": round(dh_, 1), "hue_gap_vs_dash": round(hue_gap, 1),
                      "slash_mean_sat": round(sl_sat, 3), "trail_mean_sat": round(float(s), 3)}
    check("distinct_from_hero_trails", hue_gap >= 150 and s - sl_sat >= 0.4,
          f"matiz {h * 360:.0f}° vs. FX-DASH {dh_:.0f}° (distância {hue_gap:.0f}° ≥ 150); saturação "
          f"{s:.2f} vs. FX_SlashTrail {sl_sat:.2f}")

    pp = projectile_params()
    res["projectile_defaults"] = pp
    vmax = pp["max_speed"]
    length = vmax * TRAIL["time_s"] if vmax else None
    res["trail_length_m_at_max_speed"] = round(length, 3) if length else None
    check("tail_time_and_length", TRAIL["time_s"] < LIMITS["tail_s"] and length is not None and length <= LIMITS["tail_m"] + 1e-6,
          f"time {TRAIL['time_s']} s < {LIMITS['tail_s']} s; a {vmax} m/s (maior projectileSpeed) a cauda "
          f"tem {length:.2f} m ≤ {LIMITS['tail_m']} m")
    wfrac = TRAIL["width_m"] / ORB_DIAMETER_M
    check("danger_area_not_exaggerated", wfrac <= LIMITS["max_width_frac_of_orb"] and TRAIL["width_curve"][0][1] <= 1.0,
          f"largura {TRAIL['width_m']} m = {wfrac:.0%} do orbe de {ORB_DIAMETER_M} m (≤ "
          f"{LIMITS['max_width_frac_of_orb']:.0%}); a gota afina para 0 dentro dela")
    ratio = length / TRAIL["width_m"] if length else None
    check("texture_aspect_matches_trail", ratio is not None and 1.5 <= ratio <= 6.0,
          f"cauda ≈ {length:.2f} m × {TRAIL['width_m']} m (razão {ratio:.2f}; textura 4:1, faixa 1,5–6)")
    check("projectile_rules_untouched", pp["radius"] and pp["lifetime"] and pp["timescale_guard"],
          f"Projectile.cs/ProjectilePool.cs só lidos: radius {pp['radius']} m, lifetime {pp['lifetime']} s, "
          f"pool {pp['defaultCapacity']}/{pp['maxSize']}, velocidades {pp['projectile_speeds']}; "
          f"guarda de pausa presente")
    res["ok"] = all(c["ok"] for c in res["checks"])
    return res


# ---------------------------------------------------------------- prévias (revisão)
def font(size: int):
    for name in ("arialbd.ttf", "DejaVuSans-Bold.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def label(im: Image.Image, text: str, size=13, at=(4, 4)):
    d = ImageDraw.Draw(im)
    f = font(size)
    bb = d.textbbox((0, 0), text, font=f)
    x, y = at
    d.rectangle([x, y, x + 8 + bb[2] - bb[0], y + 6 + bb[3] - bb[1]], fill=(14, 19, 26, 220))
    d.text((x + 4, y + 1), text, font=f, fill=(230, 230, 230, 255))


def project(x, y, z, origin, ppm):
    """Câmera fixa de 45°: x para a direita, z para longe da câmera (sobe na tela), y para cima."""
    return origin[0] + x * ppm, origin[1] - (y * math.cos(PITCH) + z * math.sin(PITCH)) * ppm


def apply_trail(base, tex, origin, ppm, pos, heading_deg, length_m, alpha=1.0):
    """Faixa alinhada à vista (alignment View) atrás do projétil, projetada pela câmera de 45°.
    pos = (x, y, z) do centro do projétil; a cabeça fica mount_back_m atrás; u = 0 na cabeça."""
    hd = math.radians(heading_deg)
    dx, dz = math.cos(hd), math.sin(hd)
    px, py, pz = pos
    mb = TRAIL["mount_back_m"]
    hx, hy = project(px - dx * mb, py, pz - dz * mb, origin, ppm)
    tx, ty = project(px - dx * (mb + length_m), py, pz - dz * (mb + length_m), origin, ppm)
    vx, vy = tx - hx, ty - hy
    lpx = math.hypot(vx, vy)
    if length_m <= 0.005 or lpx < 0.5:
        return base.copy()
    ux, uy = vx / lpx, vy / lpx
    nx, ny = -uy, ux
    wpx = TRAIL["width_m"] * ppm
    texa = np.asarray(tex).astype(np.float64)
    bw, bh = base.size
    gx, gy = np.meshgrid(np.arange(bw) + 0.5, np.arange(bh) + 0.5)
    s = ((gx - hx) * ux + (gy - hy) * uy) / lpx
    d = (gx - hx) * nx + (gy - hy) * ny
    wc = np.interp(s, [k[0] for k in TRAIL["width_curve"]], [k[1] for k in TRAIL["width_curve"]])
    v = 0.5 + d / (wpx * wc)
    m = (s >= 0) & (s <= 1) & (v >= 0) & (v <= 1)
    out = np.asarray(base.convert("RGBA")).astype(np.float64).copy()
    if m.any():
        fu = np.clip(s[m] * (W - 1), 0, W - 1)
        fv = np.clip(v[m] * (H - 1), 0, H - 1)
        u0, v0 = np.floor(fu).astype(int), np.floor(fv).astype(int)
        u1, v1 = np.minimum(u0 + 1, W - 1), np.minimum(v0 + 1, H - 1)
        au, av = (fu - u0)[:, None], (fv - v0)[:, None]
        pre = texa.copy()
        pre[:, :, :3] *= pre[:, :, 3:4] / 255
        smp = (pre[v0, u0] * (1 - au) * (1 - av) + pre[v0, u1] * au * (1 - av)
               + pre[v1, u0] * (1 - au) * av + pre[v1, u1] * au * av)
        sa = smp[:, 3:4] / 255 * alpha
        out[m, :3] = smp[:, :3] * alpha + out[m, :3] * (1 - sa)
    return Image.fromarray(np.clip(np.round(out), 0, 255).astype(np.uint8), "RGBA")


def draw_orb(base: Image.Image, origin, ppm, pos, colour) -> Image.Image:
    """Esfera atual (Mat_Projectile, sem emissão) como disco sombreado de 0,40 m: luz de cima-esquerda."""
    cx, cy = project(*pos, origin, ppm)
    r = ORB_DIAMETER_M * 0.5 * ppm
    out = np.asarray(base.convert("RGBA")).astype(np.float64).copy()
    bw, bh = base.size
    ss = 4
    gx, gy = np.meshgrid((np.arange(bw * ss) + 0.5) / ss, (np.arange(bh * ss) + 0.5) / ss)
    nx_, ny_ = (gx - cx) / r, (gy - cy) / r
    rr = nx_ ** 2 + ny_ ** 2
    inside = rr <= 1
    nz = np.sqrt(np.clip(1 - rr, 0, 1))
    L = np.array([-0.45, -0.6, 0.66])
    L /= np.linalg.norm(L)
    shade = 0.45 + 0.75 * np.clip(nx_ * L[0] + ny_ * L[1] + nz * L[2], 0, 1)
    col = np.array(colour) * 255
    rgbimg = np.clip(col[None, None, :] * shade[:, :, None], 0, 255)
    cov = inside.reshape(bh, ss, bw, ss).mean(axis=(1, 3))
    rgbd = (rgbimg * inside[:, :, None]).reshape(bh, ss, bw, ss, 3).sum(axis=(1, 3)) / np.maximum(
        inside.reshape(bh, ss, bw, ss).sum(axis=(1, 3)), 1)[:, :, None]
    out[:, :, :3] = rgbd * cov[:, :, None] + out[:, :, :3] * (1 - cov[:, :, None])
    return Image.fromarray(np.clip(np.round(out), 0, 255).astype(np.uint8), "RGBA")


def shot(base, tex, origin, ppm, pos, heading, length, orb_rgb):
    """Ordem de desenho como a Unity faria: se a cauda fica mais perto da câmera que o orbe
    (projétil indo para longe), a faixa transparente cobre a borda do orbe; senão o orbe a cobre."""
    away = math.sin(math.radians(heading)) > 0
    if away:
        return apply_trail(draw_orb(base, origin, ppm, pos, orb_rgb), tex, origin, ppm, pos, heading, length)
    return draw_orb(apply_trail(base, tex, origin, ppm, pos, heading, length), origin, ppm, pos, orb_rgb)


def crop_floor(half_w=240, half_h=150, up=40):
    cap = Image.open(FLOOR_CAPTURE).convert("RGBA")
    fx, fy = FLOOR_FEET_1080
    box = (fx - half_w, fy - half_h - up, fx + half_w, fy + half_h - up)
    return cap.crop(box), (half_w, half_h + up)


HEIGHT_M = 1.0  # EnemyData.projectileHeight padrão (Enemy_Ranged.asset)
HEADINGS = [0.0, 90.0, 180.0, 270.0, 30.0, 215.0]
SPOTS = [(1.6, 1.4), (-1.9, 1.3), (1.4, -0.9), (-1.6, -1.0), (-0.3, 1.9), (0.3, -1.9)]


def review_floor(tex, pp) -> Image.Image:
    """PRÉVIA 45°: esfera atual + cauda proposta no comprimento máximo (12 m/s), seis rumos, sobre a
    captura real F05 em escala de gameplay (1×) e ampliada 2×. Não é captura da Unity."""
    crop, origin = crop_floor()
    length = pp["max_speed"] * TRAIL["time_s"]
    im = crop
    for (x, z), hd in zip(SPOTS, HEADINGS):
        im = shot(im, tex, origin, FLOOR_PPM_1080, (x, HEIGHT_M, z), hd, length, pp["mat_projectile_base"])
    label(im, "PREVIEW 45° (não é captura da Unity): esfera atual + A09, 6 rumos", 12)
    big = im.resize((im.width * 2, im.height * 2), Image.NEAREST)
    out = Image.new("RGB", (big.width, im.height + big.height + 6), (20, 20, 24))
    out.paste(im.convert("RGB"), ((big.width - im.width) // 2, 0))
    out.paste(big.convert("RGB"), (0, im.height + 6))
    return out


def review_compare(tex, pp) -> Image.Image:
    """Esfera atual sem cauda × com cauda, mesmo rumo, 3× sobre a pedra real: a área de perigo
    (o orbe) não cresce; a cauda é mais estreita que ele."""
    crop, origin = crop_floor(90, 60, 40)
    length = pp["max_speed"] * TRAIL["time_s"]
    pos = (0.0, HEIGHT_M, 0.0)
    a = draw_orb(crop, origin, FLOOR_PPM_1080, pos, pp["mat_projectile_base"])
    b = shot(crop, tex, origin, FLOOR_PPM_1080, pos, 160.0, length, pp["mat_projectile_base"])
    a, b = (x.resize((x.width * 3, x.height * 3), Image.NEAREST) for x in (a, b))
    label(a, "atual: só a esfera", 13)
    label(b, "PREVIEW: esfera + cauda A09 (0,6 m)", 13)
    out = Image.new("RGB", (a.width * 2 + 6, a.height), (20, 20, 24))
    out.paste(a.convert("RGB"), (0, 0))
    out.paste(b.convert("RGB"), (a.width + 6, 0))
    return out


def review_backgrounds(tex) -> Image.Image:
    bgs = ["#3E4152", "#55596B", "#0E131A", "#B9C4CF", "#2F6BFF"]
    sc = 4
    out = Image.new("RGB", (W * sc, H * sc * (len(bgs) + 1)), (0, 0, 0))
    big = tex.resize((W * sc, H * sc), Image.NEAREST)
    for r, bg in enumerate(bgs):
        tile = Image.new("RGBA", big.size, (*rgb(bg), 255))
        tile.alpha_composite(big)
        out.paste(tile.convert("RGB"), (0, r * H * sc))
    tile = Image.new("RGBA", big.size, (*rgb("#3E4152"), 255))
    ImageDraw.Draw(tile).rectangle([0, H * sc // 4, W * sc, H * sc * 3 // 4], fill=(*TELEGRAPH, 120))
    tile.alpha_composite(big)
    out.paste(tile.convert("RGB"), (0, len(bgs) * H * sc))
    return out


def review_vs_hero_trails(tex) -> Image.Image:
    """FX_SlashTrail (ataque), FX-DASH (dash do Hero) e A09 (hostil), mesma largura, sobre a pedra."""
    ww, hh = 512, 128
    out = Image.new("RGBA", (ww, hh * 3 + 8), (*rgb("#3E4152"), 255))
    for i, p in enumerate([Image.open(SLASH).convert("RGBA"), Image.open(DASH).convert("RGBA"), tex]):
        out.alpha_composite(p.resize((ww, hh), Image.NEAREST), (0, i * (hh + 4)))
    label(out, "FX_SlashTrail (ataque) / FX-DASH (Hero) / FX-PROJECTILE-TRAIL (hostil)", 12)
    return out.convert("RGB")


def preview_gif(tex, pp, path: Path):
    """PRÉVIA animada (4× mais lenta): tiro 1 voa 0,40 s, a cauda cresce até 0,6 m e some junto no
    retorno ao pool; pausa congela tudo; tiro 2 (mesmo objeto do pool, outro ponto) nasce sem faixa
    ligando ao ponto antigo."""
    crop, origin = crop_floor(185, 100, 40)
    spd = pp["max_speed"]
    life = TRAIL["time_s"]
    dt = 0.02
    frames, durs = [], []

    def frame(img, txt):
        label(img, txt, 12)
        frames.append(img.resize((img.width * 2, img.height * 2), Image.NEAREST).convert("RGB"))
        durs.append(round(dt * 1000 * 4))

    def flight(start, hd, t_end, t0_label, pause_at=None):
        t = 0.0
        hdr = math.radians(hd)
        while t <= t_end + 1e-6:
            p = (start[0] + math.cos(hdr) * spd * t, HEIGHT_M, start[1] + math.sin(hdr) * spd * t)
            im = shot(crop, tex, origin, FLOOR_PPM_1080, p, hd, spd * min(t, life), pp["mat_projectile_base"])
            frame(im, f"PREVIEW  {t0_label} t={t:.2f}s")
            if pause_at is not None and abs(t - pause_at) < 1e-6:
                for _ in range(10):
                    frame(im.copy(), f"PREVIEW  {t0_label} PAUSA (timeScale 0)")
            t += dt

    flight((-2.6, 0.0), 10.0, 0.40, "tiro 1", pause_at=0.20)
    for _ in range(6):
        frame(crop.copy(), "PREVIEW  retorno ao pool: Clear(), sem cauda")
    flight((2.3, 1.2), 215.0, 0.30, "tiro 2 (reuso)")
    for _ in range(8):
        frame(crop.copy(), "PREVIEW  repouso")
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=durs, loop=0, optimize=False, disposal=1)


def source_svg() -> str:
    lines = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',
             "  <!-- FX-PROJECTILE-TRAIL: facetas da gota antes do envelope de alpha por coluna "
             f"(head_in {ENV['head_in']}, tail_from {ENV['tail_from']}, pico {ENV['peak']}). "
             "u = 0 (esquerda) = cabeça, junto ao projétil. Gerado por Tools/FxProjectileTrailGenerator/generate.py. -->"]
    for role in ("upper", "lower", "core"):
        lines.append(f'  <g id="{role}">')
        for pts, col, a, r in facets():
            if r != role:
                continue
            p = " ".join(f"{x:.2f},{y:.2f}" for x, y in pts)
            lines.append(f'    <polygon points="{p}" fill="#{col[0]:02X}{col[1]:02X}{col[2]:02X}" fill-opacity="{a:.2f}"/>')
        lines.append("  </g>")
    lines.append("</svg>")
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------- saída
def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def stable_guid(key: str) -> str:
    return uuid.UUID(hashlib.sha256(("Eldoria/A09/" + key).encode()).hexdigest()[:32]).hex


def write_meta(png: Path) -> bool:
    """Cria a .meta só se faltar (preserva o GUID depois da importação). 128×32 é potência de 2:
    o template de FX_Spark serve sem mudanças (clamp, bilinear, mips, sRGB, alphaIsTransparency)."""
    meta = png.with_suffix(png.suffix + ".meta")
    if meta.exists():
        return False
    text = META_TEMPLATE.read_text(encoding="utf-8").replace("{{GUID}}", stable_guid(png.name))
    meta.write_text(text, encoding="utf-8", newline="\n")
    return True


def write_folder_meta() -> bool:
    meta = STAGING.with_suffix(".meta")
    if meta.exists():
        return False
    meta.write_text("fileFormatVersion: 2\nguid: " + stable_guid("folder/" + ASSET_ID) + "\nfolderAsset: yes\n"
                    "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n"
                    "  assetBundleVariant: \n", encoding="utf-8", newline="\n")
    return True


def integration_json(pp: dict) -> dict:
    return {
        "asset_id": ASSET_ID,
        "texture": f"../{PNG_NAME}",
        "texture_size": [W, H],
        "uv_convention": {
            "u": "ao longo do trajeto; u = 0 na cabeça (junto ao projétil, ponto mais novo), u = 1 na ponta da cauda",
            "v": "através da faixa; v = 0,5 no centro (centro quente fino)",
            "head_at_u0": HEAD_AT_U0,
            "note": "Mesma convenção do FX_SlashTrail.png e do FX-DASH. Conferir no Editor; se aparecer invertida, "
                    "gerar com HEAD_AT_U0 = False (ou tiling X = −1).",
        },
        "unity_integration_proposal": {
            "status": "Proposta; NÃO VALIDADO NO EDITOR. TrailRenderer no Projectile.prefab e o reset no "
                      "Projectile.cs são tarefa Codex separada.",
            "texture_import": "Default, sRGB, alphaIsTransparency, clamp, bilinear, mipmaps on (template FX_Spark).",
            "material": "UM material compartilhado (sharedMaterial) URP/Particles/Unlit, Surface Transparent, Blend "
                        "Alpha, cor branca (a cor vem da textura), sem emissão HDR; nunca material/textura por tiro.",
            "trail_renderer": {
                "parent": "filho \"Trail\" do Projectile.prefab, já presente no prefab: as instâncias do pool o "
                          "herdam; nada é criado por disparo",
                "local_offset_world_m": [0.0, 0.0, -TRAIL["mount_back_m"]],
                "local_offset_note": "Atrás do centro, ainda dentro do orbe (raio 0,20 m). Com a raiz em escala "
                                     "0,4, localPosition z = −0,35; com a raiz em 1 (opção do A08), z = −0,14.",
                "time_s": TRAIL["time_s"],
                "width_multiplier_m": TRAIL["width_m"],
                "width_curve": TRAIL["width_curve"],
                "min_vertex_distance_m": TRAIL["min_vertex_distance_m"],
                "texture_mode": "Stretch",
                "alignment": "View",
                "num_cap_vertices": 0,
                "num_corner_vertices": 0,
                "color_gradient": "branco; alpha 1 → 1 (o fade já está na textura)",
                "emitting": "false no prefab; true só depois do Clear() em Launch",
                "autodestruct": False,
                "shadows_lighting_probes": "desligados, como o SlashTrail",
                "expected_length_m": {k: round(v * TRAIL["time_s"], 3) for k, v in pp["projectile_speeds"].items()},
            },
            "extra_particles": 0,
            "reset_rules": [
                "Launch: depois de definir transform.position/rotation, trail.Clear() e emitting = true. O Clear vem "
                "DEPOIS do teleporte: sem ele a faixa liga o ponto onde o objeto saiu do pool ao novo disparo.",
                "OnDespawned (retorno ao pool por colisão, timeout ou ForceRelease): emitting = false e Clear(). "
                "A cauda some junto com o orbe; nenhuma trilha antiga sobrevive à reutilização.",
                "OnEnable/OnDisable do objeto (pool.Get/Release, troca de sala, reset de checkpoint, morte do Hero): "
                "Clear(); nunca Instantiate/Destroy de TrailRenderer, material ou textura por tiro.",
                "Pausa (timeScale 0): Projectile.Update já retorna sem mover; conferir no Editor que a cauda congela "
                "em vez de encolher (o TrailRenderer envelhece pelo tempo escalado) e que não salta ao despausar.",
                "Dois tiros rápidos do mesmo objeto do pool: cada Launch chama Clear(); sem emenda entre tiros.",
                "Só apresentação: não altera radius (SphereCast 0,25 m), lifetime, hitMask, velocidade, dano, pool "
                "nem colisão. Sem collider no filho.",
                "A cauda é sinal estável de ameaça, não flash: não depender da opção de flash; se a opção de reduzir "
                "efeitos for aplicada a ela, manter no mínimo o orbe visível.",
            ],
            "projectile_defaults_read_only": pp,
        },
    }


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", type=Path, help="escreve só PNG e pranchas nesta pasta")
    args = ap.parse_args()

    tex = render()
    out = args.out if args.out else SOURCE
    out.mkdir(parents=True, exist_ok=True)
    png = (args.out if args.out else STAGING) / PNG_NAME
    tex.save(png, optimize=False)
    pp = projectile_params()
    review_floor(tex, pp).save(out / "review_floor_preview.png", optimize=False)
    review_compare(tex, pp).save(out / "review_vs_current_sphere.png", optimize=False)
    review_backgrounds(tex).save(out / "review_backgrounds.png", optimize=False)
    review_vs_hero_trails(tex).save(out / "review_vs_hero_trails.png", optimize=False)
    preview_gif(tex, pp, out / "preview_projectile.gif")
    res = validate(Image.open(png))

    if not args.out:
        res["meta_created"] = write_meta(png)
        res["folder_meta_created"] = write_folder_meta()
        (SOURCE / f"{ASSET_ID}.source.svg").write_text(source_svg(), encoding="utf-8", newline="\n")
        (SOURCE / f"{ASSET_ID}.trail.json").write_text(json.dumps(integration_json(pp), ensure_ascii=False, indent=2)
                                                       + "\n", encoding="utf-8", newline="\n")
        source = {
            "asset_id": ASSET_ID, "catalog_id": "A09", "generator": "Tools/FxProjectileTrailGenerator/generate.py",
            "supersampling": S, "size": [W, H], "head_at_u0": HEAD_AT_U0, "palette": PAL,
            "drop": DROP, "core": CORE, "gradient": GRAD, "envelope": ENV, "trail_proposal": TRAIL, "limits": LIMITS,
            "facets": [{"role": role, "alpha": a, "colour": "#%02X%02X%02X" % col,
                        "points": [[round(x, 2), round(y, 2)] for x, y in pts]} for pts, col, a, role in facets()],
            "references": {
                "palette": "Ficha A09 (PROMPT_CATALOG.md): #FF9A2E → #8A4A22 → alpha 0; ameaça #FF9A2E em "
                           "Art/Concepts/ART_DIRECTION_AND_PROMPTS.md",
                "projectile_core_a08": "Assets/_Project/Art/ProductionStaging/FX-PROJECTILE (brasa #FF9A2E, fenda #8A4A22)",
                "current_sphere": "Assets/_Project/Materials/Mat_Projectile.mat _BaseColor (1, .55, .1); Sphere × 0,4",
                "projectile": "Assets/_Project/Scripts/Systems/Projectile.cs, ProjectilePool.cs (só leitura)",
                "speeds": "Assets/_Project/ScriptableObjects/Enemies/*.asset projectileSpeed (só leitura)",
                "slash_trail": "Assets/_Project/Art/Items/VFX/FX_SlashTrail.png",
                "dash_trail": "Assets/_Project/Art/ProductionStaging/FX-DASH/FX-DASH-Trail.png",
                "floor_capture": str(FLOOR_CAPTURE.relative_to(ROOT)).replace("\\", "/"),
            },
        }
        (SOURCE / f"{ASSET_ID}.source.json").write_text(json.dumps(source, ensure_ascii=False, indent=2) + "\n",
                                                        encoding="utf-8", newline="\n")
        res["sha256"] = {PNG_NAME: sha(png), "preview_projectile.gif": sha(SOURCE / "preview_projectile.gif")}
        (SOURCE / "FxProjectileTrail.validation.json").write_text(json.dumps(res, ensure_ascii=False, indent=2) + "\n",
                                                                  encoding="utf-8", newline="\n")
    for c in res["checks"]:
        print(("ok  " if c["ok"] else "FAIL") + f" {c['name']}: {c['detail']}")
    return 0 if res["ok"] else 1


if __name__ == "__main__":
    sys.exit(main())

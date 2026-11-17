"""A07 / FX-DASH — gerador determinístico da textura de faixa do rastro do dash (Eldoria).

Desenha UMA textura 256×64 RGBA para TrailRenderer (textureMode Stretch): três dobras largas e
afiladas de pano azul-real (a identidade da capa do Hero, #2F6BFF), facetadas em planos chapados,
com uma borda ciano contida (#4FD8FF) só na dobra principal. O alpha zera nas duas pontas de U e
nas bordas de V. Não há personagem, clone, arco de arma, raio, fumaça, texto nem fundo.

Convenção de UV (decisão registrada no README): U percorre o trajeto do movimento; u = 0 é a
cabeça do rastro (ponto mais novo, junto ao Hero) e u = 1 a cauda (ponto mais antigo), como o
FX_SlashTrail.png já integrado e o mapeamento Stretch do TrailRenderer. HEAD_AT_U0 = False espelha.

Polígonos com supersampling 8× e redução com alpha pré-multiplicado; envelope de alpha por coluna
aplicado depois. Sem IA generativa nem asset de terceiros.

Uso (Python 3.11 + Pillow 13 de .claude/skills/create-game-assets/scripts/requirements.txt + NumPy):
    python Tools/FxDashGenerator/generate.py            # escreve PNG, .meta (só se faltar), Source~
    python Tools/FxDashGenerator/generate.py --out DIR  # escreve só PNG e pranchas em DIR

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
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
STAGING = ROOT / "Assets/_Project/Art/ProductionStaging/FX-DASH"
SOURCE = STAGING / "Source~"
PNG_NAME = "FX-DASH-Trail.png"
META_TEMPLATE = ROOT / "Tools/FxHealGenerator/texture.meta.template"  # cópia de FX_Spark.png.meta
SLASH = ROOT / "Assets/_Project/Art/Items/VFX/FX_SlashTrail.png"
PLAYER_DASH = ROOT / "Assets/_Project/Scripts/Player/PlayerDash.cs"
FLOOR_CAPTURE = ROOT / "Reports/F05/Visual-final/1920-collection.png"
FLOOR_FEET_1080 = (959, 543)  # centro das botas do Hero na captura F05 1920×1080
FLOOR_PPM_1080 = 75.0  # px por metro no plano do piso perto do Hero (ladrilho 2 m ≈ 150 px)
PITCH = math.radians(45.0)

S = 8  # supersampling
W, H = 256, 64
HEAD_AT_U0 = True
LIGHT_FROM_TOP = True  # luz de cima (−V), como ícones, UI-SKIN e FX-HEAL/LEVEL/LOOT

# ---------------------------------------------------------------- paleta (sRGB)
# Capa do Hero #2F6BFF (ART_DIRECTION_AND_PROMPTS.md); sombra e realce do mesmo matiz, um pouco
# dessaturados para o tom escuro medieval; borda ciano #4FD8FF contida.
PAL = {
    "shade": "#1F45A8",
    "mid": "#2F6BFF",
    "light": "#6E98FF",
    "edge": "#4FD8FF",
}

# ---------------------------------------------------------------- dobras (px na textura 256×64)
# x: de x0 (lado da cabeça) até x1 (ponta afilada, lado da cauda). y_a → y_b: linha central; bow:
# arqueamento máximo no meio (negativo = para cima). hw: meia-largura máxima; peak: posição (0..1)
# do ponto mais largo; head_hw: fração de hw na raiz (raiz larga e chanfrada, não ponta).
# Ordem = ordem de desenho (de trás para a frente).
SWEEPS = [
    {"id": "lower_fold", "x0": 9.0, "x1": 176.0, "y_a": 41.0, "y_b": 49.0, "bow": 3.0,
     "hw": 8.0, "peak": 0.22, "head_hw": 0.55, "segments": 6, "tone": -0.18, "alpha": 0.72},
    {"id": "upper_fold", "x0": 12.0, "x1": 204.0, "y_a": 20.0, "y_b": 14.0, "bow": -3.5,
     "hw": 8.5, "peak": 0.25, "head_hw": 0.55, "segments": 6, "tone": 0.06, "alpha": 0.72},
    {"id": "main_sweep", "x0": 3.0, "x1": 236.0, "y_a": 31.0, "y_b": 34.0, "bow": -4.5,
     "hw": 13.5, "peak": 0.22, "head_hw": 0.6, "segments": 8, "tone": 0.0, "alpha": 0.9},
]
EDGE = {"sweep": "main_sweep", "from": 0.10, "to": 0.78, "width_px": 2.6, "alpha": 0.85}

# Envelope de alpha por coluna (fração de W): sobe de 0 na cabeça, platô, cai até 0 na cauda.
ENV = {"head_in": 0.20, "tail_from": 0.42, "tail_power": 1.6, "peak": 0.64}

# ---------------------------------------------------------------- integração proposta
TRAIL = {
    "time_s": 0.16,  # cauda após o fim do dash (ficha: ≤ 0,20 s)
    "width_m": 0.60,
    "width_curve": [[0.0, 1.0], [0.6, 0.85], [1.0, 0.55]],
    "mount_height_m": 0.80,  # altura da capa, atrás do Hero
    "min_vertex_distance_m": 0.06,
}
LIMITS = {"tail_s": 0.20, "extra_particles": 8}

GREEN_HUE = (70.0, 170.0)
THREAT_HUES = (0.0, 36.0)
OPAQUE_WHITE = (0.18, 0.92, 200)
MAX_ALPHA = 165  # ≤ 0,65: o piso/telegraph sempre aparece ≥ 35%
MAX_COVER = 0.22  # fração da textura com alpha > 50%
HEAD_CLEAR_PX = 8  # primeiras colunas (junto ao Hero): alpha ≤ HEAD_CLEAR_ALPHA
HEAD_CLEAR_ALPHA = 40
END_FADE = 0.06  # primeiros/últimos 6% de U: alpha ≤ 25% do pico
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
def sweep_rails(sw: dict):
    """Pontos (x, y_top, y_bot) ao longo da dobra; a raiz é chanfrada e a ponta fecha em 0."""
    n = sw["segments"]
    pts = []
    for i in range(n + 1):
        t = i / n
        x = sw["x0"] + (sw["x1"] - sw["x0"]) * t
        yc = sw["y_a"] + (sw["y_b"] - sw["y_a"]) * t + sw["bow"] * math.sin(math.pi * t)
        if t <= sw["peak"]:
            w = sw["head_hw"] + (1 - sw["head_hw"]) * math.sin(0.5 * math.pi * t / sw["peak"])
        else:
            w = math.cos(0.5 * math.pi * (t - sw["peak"]) / (1 - sw["peak"])) ** 0.9
        hw = sw["hw"] * w
        # dobra de pano: a borda de cima se adianta um pouco em relação à de baixo (cisalhamento)
        pts.append((x, yc - hw, yc + hw, yc, t))
    return pts


def facets():
    """Lista de (pontos, cor, alpha, sweep_id, papel) de trás para a frente."""
    out = []
    for sw in SWEEPS:
        rails = sweep_rails(sw)
        for i in range(len(rails) - 1):
            x0, t0, b0, c0, _ = rails[i]
            x1, t1, b1, c1, tt = rails[i + 1]
            # inclinação da linha central → faceta voltada para a luz (cima) fica mais clara
            slope = (c1 - c0) / max(x1 - x0, 1e-6)
            fold = 0.5 - 0.9 * slope  # sobe → recebe mais luz
            alt = 0.12 if i % 2 == 0 else -0.04  # alterna planos: leitura de dobras largas
            top_lit = min(max(0.62 + alt + 0.25 * (fold - 0.5) + sw["tone"], 0.0), 1.0)
            bot_lit = min(max(0.30 + alt * 0.5 + 0.2 * (fold - 0.5) + sw["tone"], 0.0), 1.0)
            upper = [(x0, t0), (x1, t1), (x1, c1), (x0, c0)]
            lower = [(x0, c0), (x1, c1), (x1, b1), (x0, b0)]
            out.append((upper, lit_colour(top_lit), sw["alpha"], sw["id"], "upper"))
            out.append((lower, lit_colour(bot_lit), sw["alpha"], sw["id"], "lower"))
        if sw["id"] == EDGE["sweep"]:
            out.extend(edge_facets(rails))
    return out


def edge_facets(rails):
    """Faixa ciano fina sobre a borda de cima da dobra principal, afilada nas duas pontas."""
    res = []
    for i in range(len(rails) - 1):
        x0, t0, *_ , u0 = rails[i]
        x1, t1, *_ , u1 = rails[i + 1]
        if u1 <= EDGE["from"] or u0 >= EDGE["to"]:
            continue

        def wid(u):
            m = (u - EDGE["from"]) / (EDGE["to"] - EDGE["from"])
            return EDGE["width_px"] * math.sin(math.pi * min(max(m, 0.0), 1.0))

        w0, w1 = wid(u0), wid(u1)
        res.append(([(x0, t0), (x1, t1), (x1, t1 + w1), (x0, t0 + w0)], rgb(PAL["edge"]), EDGE["alpha"],
                    EDGE["sweep"], "edge"))
    return res


def lit_colour(lit: float):
    if lit < 0.5:
        return mix(PAL["shade"], PAL["mid"], lit * 2)
    return mix(PAL["mid"], PAL["light"], (lit - 0.5) * 1.2)


def envelope(x: float) -> float:
    u = x / (W - 1)
    head = smoothstep(0.0, ENV["head_in"], u)
    tail = 1.0 if u <= ENV["tail_from"] else (1 - (u - ENV["tail_from"]) / (1 - ENV["tail_from"])) ** ENV["tail_power"]
    return head * max(tail, 0.0)


def render() -> Image.Image:
    """Dobras compostas numa camada (sobreposição não soma brilho além do alpha da dobra da frente),
    reduzidas com alpha pré-multiplicado; envelope por coluna × pico; RGB zerado onde alpha = 0."""
    big = Image.new("RGBA", (W * S, H * S), (0, 0, 0, 0))
    for pts, col, a, _sid, _role in facets():
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


# ---------------------------------------------------------------- verificações
def luminance(c) -> float:
    def ch(x):
        x /= 255
        return x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4
    return 0.2126 * ch(c[0]) + 0.7152 * ch(c[1]) + 0.0722 * ch(c[2])


def contrast(a, b) -> float:
    la, lb = sorted((luminance(a), luminance(b)), reverse=True)
    return (la + 0.05) / (lb + 0.05)


def dash_params() -> dict:
    """Lê os valores serializados padrão de PlayerDash.cs (só leitura; a proposta não os altera)."""
    src = PLAYER_DASH.read_text(encoding="utf-8")
    out = {}
    for name in ("dashSpeed", "dashDuration", "invulnerabilityDuration", "cooldown"):
        m = re.search(rf"float {name} = ([0-9.]+)f;", src)
        out[name] = float(m.group(1)) if m else None
    return out


def validate(im: Image.Image) -> dict:
    res = {"checks": []}

    def check(name, ok, detail):
        res["checks"].append({"name": name, "ok": bool(ok), "detail": detail})

    a = np.asarray(im).astype(np.int32)
    al = a[:, :, 3]
    head = (lambda x: x) if HEAD_AT_U0 else (lambda x: W - 1 - x)
    check("size_mode", im.size == (W, H) and im.mode == "RGBA", f"{im.size} {im.mode}")
    dirty = int(((al == 0) & (a[:, :, :3].sum(axis=2) > 0)).sum())
    check("no_dirty_transparent_px", dirty == 0, f"{dirty} px com alpha 0 e RGB != 0")
    border = int(max(al[0].max(), al[-1].max(), al[:, 0].max(), al[:, -1].max()))
    check("transparent_border", border == 0, f"alpha máx. na moldura de 1 px = {border}")
    rows = np.where(al.max(axis=1) > 0)[0]
    vmargin = int(min(rows[0], H - 1 - rows[-1]))
    check("v_margin", vmargin >= MIN_V_MARGIN, f"margem vertical {vmargin} px (≥ {MIN_V_MARGIN})")
    amax = int(al.max())
    check("restrained_alpha", amax <= MAX_ALPHA, f"alpha máx. {amax} ≤ {MAX_ALPHA}")
    col_max = al.max(axis=0)
    n_end = round(W * END_FADE)
    head_cols = [head(x) for x in range(n_end)]
    tail_cols = [head(W - 1 - x) for x in range(n_end)]
    he, te = int(col_max[head_cols].max()), int(col_max[tail_cols].max())
    check("fade_both_ends", he <= 0.25 * amax and te <= 0.25 * amax,
          f"alpha máx. nos {n_end} px da cabeça {he} e da cauda {te} ≤ 25% do pico ({0.25 * amax:.0f})")
    hc = int(col_max[[head(x) for x in range(HEAD_CLEAR_PX)]].max())
    check("head_does_not_cover_hero", hc <= HEAD_CLEAR_ALPHA,
          f"alpha máx. nas {HEAD_CLEAR_PX} colunas junto ao Hero = {hc} ≤ {HEAD_CLEAR_ALPHA} (sem duplicata opaca)")
    # perfil de alpha suave: sem degrau maior que 18 entre colunas vizinhas no máximo por coluna
    step = int(np.abs(np.diff(col_max)).max())
    check("smooth_column_profile", step <= 18, f"maior salto do alpha máx. entre colunas vizinhas = {step}")
    cover = float((al > 127).mean())
    res["cover_over_50pct"] = round(cover, 4)
    check("floor_not_hidden", cover <= MAX_COVER, f"cobertura alpha>50% {cover:.1%} ≤ {MAX_COVER:.0%}")
    keep = 1 - amax / 255
    check("telegraph_visible_under_peak", keep >= 0.35,
          f"no pixel mais opaco, {keep:.0%} da diferença telegraph↔piso continua visível (≥ 35%)")

    vis = a[al >= 24]
    green = threat = white = cyan = 0
    hsv = []
    for r, g, b, aa in vis:
        h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
        hd = h * 360
        green += s > 0.15 and GREEN_HUE[0] <= hd <= GREEN_HUE[1]
        threat += s > 0.55 and THREAT_HUES[0] <= hd <= THREAT_HUES[1]
        white += s < OPAQUE_WHITE[0] and v > OPAQUE_WHITE[1] and aa > OPAQUE_WHITE[2]
        cyan += s > 0.4 and 182 <= hd <= 200
    check("no_green_hue", green == 0, f"{green} px em matiz {GREEN_HUE}")
    check("no_threat_red_orange", threat == 0, f"{threat} px saturados em matiz {THREAT_HUES}")
    check("no_opaque_white", white == 0, f"{white} px de branco opaco")
    w = vis[:, 3].astype(np.float64)
    mean = (vis[:, :3] * w[:, None]).sum(axis=0) / w.sum()
    h, s, v = colorsys.rgb_to_hsv(*(mean / 255))
    res["mean_rgb"] = [round(c) for c in mean]
    res["mean_hsv"] = [round(float(h) * 360, 1), round(float(s), 3), round(float(v), 3)]
    check("reads_royal_blue", 214 <= h * 360 <= 232 and s >= 0.55,
          f"média HSV {res['mean_hsv']} (capa #2F6BFF = 223°)")
    cf = cyan / len(vis)
    res["cyan_fraction"] = round(cf, 4)
    check("restrained_cyan_edge", 0.02 <= cf <= 0.20, f"{cf:.1%} dos px visíveis em ciano (2–20%)")
    check("two_or_three_sweeps", 2 <= len(SWEEPS) <= 3, f"{len(SWEEPS)} dobras: {[s_['id'] for s_ in SWEEPS]}")

    # contraste sobre a pedra (média dos px com alpha > 50% compostos sobre o fundo)
    res["contrast_on_stone"] = {}
    sel = al > 127
    for bg in STONES:
        base = np.array(rgb(bg), dtype=np.float64)
        comp = a[:, :, :3] * (al[:, :, None] / 255) + base * (1 - al[:, :, None] / 255)
        avg = comp[sel].mean(axis=0)
        res["contrast_on_stone"][bg] = round(contrast(avg, base), 2)
    # o azul tem luminância próxima da pedra: a leitura vem do matiz. Mede-se a distância RGB da
    # média composta à pedra (≥ 60) e registra-se o contraste WCAG só como informação.
    res["rgb_distance_on_stone"] = {}
    for bg in STONES:
        base = np.array(rgb(bg), dtype=np.float64)
        comp = a[:, :, :3] * (al[:, :, None] / 255) + base * (1 - al[:, :, None] / 255)
        res["rgb_distance_on_stone"][bg] = round(float(np.linalg.norm(comp[sel].mean(axis=0) - base)), 1)
    dmin = min(res["rgb_distance_on_stone"].values())
    cmin = min(res["contrast_on_stone"].values())
    check("readable_on_stone", dmin >= 60,
          f"distância RGB mínima da faixa (alpha>50%) à pedra {STONES} = {dmin:.1f} ≥ 60 "
          f"(contraste WCAG {cmin:.2f}, discreto por design)")

    # distinto do FX_SlashTrail (arco de ataque cinza): saturação e perfil de alpha diferentes
    sl = np.asarray(Image.open(SLASH).convert("RGBA")).astype(np.int32)
    sl_vis = sl[sl[:, :, 3] >= 24]
    sl_sat = float(np.mean([colorsys.rgb_to_hsv(*(p[:3] / 255))[1] for p in sl_vis]))
    dalpha = float(np.abs(sl[:, :, 3] - al).mean()) if sl.shape == a.shape else None
    res["slash_compare"] = {"slash_mean_sat": round(sl_sat, 3), "dash_mean_sat": round(s, 3),
                            "mean_abs_alpha_diff": None if dalpha is None else round(dalpha, 2)}
    check("not_slash_copy", s - sl_sat >= 0.4 and (dalpha is None or dalpha >= 25),
          f"saturação dash {s:.2f} vs slash {sl_sat:.2f}; diferença média de alpha {dalpha:.1f}")

    dp = dash_params()
    res["player_dash_defaults"] = dp
    check("tail_le_0_20s", TRAIL["time_s"] <= LIMITS["tail_s"], f"time {TRAIL['time_s']} s ≤ {LIMITS['tail_s']} s")
    length = dp["dashSpeed"] * min(TRAIL["time_s"], dp["dashDuration"]) if dp["dashSpeed"] else None
    res["trail_length_m"] = round(length, 3) if length else None
    ratio = length / (TRAIL["width_m"] * TRAIL["width_curve"][0][1]) if length else None
    check("texture_aspect_matches_trail", ratio is not None and 3.0 <= ratio <= 6.0,
          f"rastro ≈ {length:.2f} m × {TRAIL['width_m']} m (razão {ratio:.2f}; textura 4:1, faixa 3–6)")
    check("dash_rules_untouched", all(dp.values()),
          f"PlayerDash.cs só lido: velocidade {dp['dashSpeed']}, duração {dp['dashDuration']} s, "
          f"invulnerabilidade {dp['invulnerabilityDuration']} s, recarga {dp['cooldown']} s")
    res["ok"] = all(c["ok"] for c in res["checks"])
    return res


# ---------------------------------------------------------------- aplicação / revisão
def font(size: int):
    for name in ("arialbd.ttf", "DejaVuSans-Bold.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def label(im: Image.Image, text: str, size=14):
    d = ImageDraw.Draw(im)
    f = font(size)
    bb = d.textbbox((0, 0), text, font=f)
    d.rectangle([4, 4, 12 + bb[2] - bb[0], 10 + bb[3] - bb[1]], fill=(14, 19, 26, 220))
    d.text((8, 5), text, font=f, fill=(230, 230, 230, 255))


def width_curve(u: float) -> float:
    k = TRAIL["width_curve"]
    for (u0, w0), (u1, w1) in zip(k, k[1:]):
        if u <= u1:
            return w0 + (w1 - w0) * (u - u0) / (u1 - u0)
    return k[-1][1]


def project(x, y, z, origin, ppm):
    return origin[0] + x * ppm, origin[1] - (y * math.cos(PITCH) + z * math.sin(PITCH)) * ppm


def apply_trail(base: Image.Image, tex: Image.Image, origin, ppm, length_m, heading_deg, alpha=1.0):
    """Faixa alinhada à vista (TrailRenderer alignment View) ao longo de um trajeto reto atrás do
    Hero, projetado pela câmera de 45°. u = 0 na cabeça (Hero), Stretch ao longo do comprimento."""
    if length_m <= 0.01:
        return base.copy()
    hd = math.radians(heading_deg)
    dx, dz = math.cos(hd), math.sin(hd)
    y = TRAIL["mount_height_m"]
    hx, hy = project(0, y, 0, origin, ppm)
    tx, ty = project(-dx * length_m, y, -dz * length_m, origin, ppm)
    vx, vy = tx - hx, ty - hy
    lpx = math.hypot(vx, vy)
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


HEADING = 195.0  # dash para a esquerda (um pouco para a câmera); rastro para a direita, atrás do Hero


def crop_floor(half_w=220, half_h=150, up=40):
    cap = Image.open(FLOOR_CAPTURE).convert("RGBA")
    fx, fy = FLOOR_FEET_1080
    box = (fx - half_w, fy - half_h - up, fx + half_w, fy + half_h - up)
    return cap.crop(box), (half_w, half_h + up)


def review_floor(tex) -> Image.Image:
    """PRÉVIA: rastro proposto no fim do dash (comprimento máximo) sobre a captura real F05 a 45°,
    em escala de gameplay (1×) e ampliado 2×. A captura não é do dash; não é render da Unity."""
    crop, origin = crop_floor()
    length = TRAIL["time_s"] * dash_params()["dashSpeed"]
    im = apply_trail(crop, tex, origin, FLOOR_PPM_1080, length, HEADING)
    label(im, "PREVIEW (não é captura da Unity)", 13)
    big = im.resize((im.width * 2, im.height * 2), Image.NEAREST)
    out = Image.new("RGB", (big.width, im.height + big.height + 6), (20, 20, 24))
    out.paste(im.convert("RGB"), ((big.width - im.width) // 2, 0))
    out.paste(big.convert("RGB"), (0, im.height + 6))
    return out


def review_telegraph(tex) -> Image.Image:
    """PRÉVIA: rastro no pico atravessando um telegraph vermelho de Boss desenhado no piso real."""
    crop, origin = crop_floor()
    over = Image.new("RGBA", crop.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(over)
    cx, cy = origin[0] + 120, origin[1] - 30  # sob o trajeto do rastro (piso), à direita do Hero
    rx, ry = 1.1 * FLOOR_PPM_1080, 1.1 * FLOOR_PPM_1080 * math.sin(PITCH)
    d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=(*TELEGRAPH, 120), outline=(89, 13, 13, 255), width=3)
    base = Image.alpha_composite(crop, over)
    length = TRAIL["time_s"] * dash_params()["dashSpeed"]
    a = apply_trail(base, tex, origin, FLOOR_PPM_1080, length, HEADING)
    label(base, "sem rastro", 13)
    label(a, "PREVIEW: rastro sobre telegraph", 13)
    out = Image.new("RGB", (crop.width * 2 + 6, crop.height), (20, 20, 24))
    out.paste(base.convert("RGB"), (0, 0))
    out.paste(a.convert("RGB"), (crop.width + 6, 0))
    return out


def review_backgrounds(tex) -> Image.Image:
    bgs = ["#3E4152", "#55596B", "#0E131A", "#B9C4CF"]
    sc = 2
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


def review_slash(tex) -> Image.Image:
    """FX_SlashTrail.png (arco de ataque já integrado) acima e FX-DASH abaixo, mesma escala."""
    sc = 2
    slash = Image.open(SLASH).convert("RGBA").resize((W * sc, H * sc), Image.NEAREST)
    out = Image.new("RGBA", (W * sc, H * sc * 2 + 4), (*rgb("#3E4152"), 255))
    out.alpha_composite(slash, (0, 0))
    out.alpha_composite(tex.resize((W * sc, H * sc), Image.NEAREST), (0, H * sc + 4))
    label(out, "FX_SlashTrail (ataque)  /  abaixo: FX-DASH", 12)
    return out.convert("RGB")


def preview_gif(tex, path: Path):
    """PRÉVIA animada: o rastro cresce durante o dash (0,18 s) e encolhe pela cauda em 0,16 s.
    O Hero da captura é estático; só o comprimento do rastro é simulado. Último quadro: vazio."""
    crop, origin = crop_floor(170, 120, 40)
    dp = dash_params()
    dur, spd, life = dp["dashDuration"], dp["dashSpeed"], TRAIL["time_s"]
    dt = 0.02
    frames, durs = [], []
    t = 0.0
    while t <= dur + life + 1e-6:
        length = spd * (min(t, life) if t <= dur else max(0.0, min(dur, life) - (t - dur)))
        im = apply_trail(crop, tex, origin, FLOOR_PPM_1080, length, HEADING)
        label(im, f"PREVIEW  t={t:.2f}s", 12)
        frames.append(im.resize((im.width * 2, im.height * 2), Image.NEAREST).convert("RGB"))
        durs.append(round(dt * 1000 * 4))  # 4× mais lento para revisão
        t += dt
    im = crop.copy()
    label(im, "PREVIEW  reset: sem rastro", 12)
    frames.append(im.resize((im.width * 2, im.height * 2), Image.NEAREST).convert("RGB"))
    durs.append(700)
    frames[0].save(path, save_all=True, append_images=frames[1:], duration=durs, loop=0, optimize=False, disposal=1)


def source_svg() -> str:
    """Fonte vetorial editável: as facetas antes do envelope de alpha (aplicado pelo script)."""
    lines = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',
             "  <!-- FX-DASH: facetas das dobras antes do envelope de alpha por coluna "
             f"(head_in {ENV['head_in']}, tail_from {ENV['tail_from']}, pico {ENV['peak']}). "
             "u = 0 (esquerda) = cabeça do rastro. Gerado por Tools/FxDashGenerator/generate.py. -->"]
    for sid in [s_["id"] for s_ in SWEEPS]:
        lines.append(f'  <g id="{sid}">')
        for pts, col, a, s_id, role in facets():
            if s_id != sid:
                continue
            p = " ".join(f"{x:.2f},{y:.2f}" for x, y in pts)
            lines.append(f'    <polygon data-role="{role}" points="{p}" fill="#{col[0]:02X}{col[1]:02X}{col[2]:02X}" '
                         f'fill-opacity="{a:.2f}"/>')
        lines.append("  </g>")
    lines.append("</svg>")
    return "\n".join(lines) + "\n"


# ---------------------------------------------------------------- saída
def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def stable_guid(key: str) -> str:
    return uuid.UUID(hashlib.sha256(("Eldoria/A07/" + key).encode()).hexdigest()[:32]).hex


def write_meta(png: Path) -> bool:
    """Cria a .meta só se faltar (preserva o GUID depois da importação). 256×64 é potência de 2:
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
    meta.write_text("fileFormatVersion: 2\nguid: " + stable_guid("folder/FX-DASH") + "\nfolderAsset: yes\n"
                    "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n"
                    "  assetBundleVariant: \n", encoding="utf-8", newline="\n")
    return True


def integration_json(dp: dict) -> dict:
    return {
        "asset_id": "FX-DASH",
        "texture": f"../{PNG_NAME}",
        "texture_size": [W, H],
        "uv_convention": {
            "u": "ao longo do trajeto; u = 0 na cabeça (Hero, ponto mais novo), u = 1 na cauda (ponto mais antigo)",
            "v": "através da faixa; v = 0,5 no centro do rastro",
            "head_at_u0": HEAD_AT_U0,
            "note": "Mesma convenção do FX_SlashTrail.png integrado e do TrailRenderer textureMode Stretch. "
                    "Conferir no Editor; se aparecer invertido, gerar com HEAD_AT_U0 = False (ou tiling X = −1).",
        },
        "unity_integration_proposal": {
            "status": "Proposta; NÃO VALIDADO NO EDITOR. Ponte de início/fim do dash é tarefa Codex separada.",
            "texture_import": "Default, sRGB, alphaIsTransparency, clamp, bilinear, mipmaps on (template FX_Spark).",
            "material": "URP/Particles/Unlit, Surface Transparent, Blend Alpha, cor branca (a cor vem da textura); "
                        "opacidade multiplicada pela opção de flash existente (0 desliga).",
            "trail_renderer": {
                "parent": "filho do Hero na altura da capa, atrás do corpo (reutilizado; nunca instanciado por dash)",
                "local_height_m": TRAIL["mount_height_m"],
                "time_s": TRAIL["time_s"],
                "width_multiplier_m": TRAIL["width_m"],
                "width_curve": TRAIL["width_curve"],
                "min_vertex_distance_m": TRAIL["min_vertex_distance_m"],
                "texture_mode": "Stretch",
                "alignment": "View",
                "num_cap_vertices": 0,
                "num_corner_vertices": 1,
                "color_gradient": "branco; alpha 1 → 1 (o fade já está na textura)",
                "emitting": "false por padrão; true só enquanto PlayerDash.IsDashing",
                "autodestruct": False,
                "shadows_lighting_probes": "desligados, como o SlashTrail",
                "expected_length_m": round(dp["dashSpeed"] * min(TRAIL["time_s"], dp["dashDuration"]), 3),
            },
            "extra_particles": 0,
            "bridge_rules": [
                "Início: borda false→true de PlayerDash.IsDashing (não há evento) → Clear() e emitting = true.",
                "Fim: borda true→false → emitting = false; a cauda some sozinha em ≤ time_s.",
                "Reset/checkpoint (RestoreCheckpoint), morte, OnDisable/reativação e retorno ao menu → emitting = false "
                "e Clear(): nenhuma trilha antiga sobrevive.",
                "Teleporte/troca de sala durante ou logo após o dash → Clear() antes do novo ponto (sem faixa "
                "atravessando a sala).",
                "Pausa (timeScale 0): não emitir pontos novos; ao despausar continuar ou limpar, sem salto.",
                "Dois dashes rápidos: o segundo início chama Clear() (sem emendar com a cauda anterior).",
                "Só apresentação: não altera dashSpeed, dashDuration, invulnerabilityDuration, cooldown, colisão, "
                "dano nem input. Sem collider.",
            ],
            "player_dash_defaults_read_only": dp,
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
    review_floor(tex).save(out / "review_floor_preview.png", optimize=False)
    review_telegraph(tex).save(out / "review_telegraph_preview.png", optimize=False)
    review_backgrounds(tex).save(out / "review_backgrounds.png", optimize=False)
    review_slash(tex).save(out / "review_vs_slash.png", optimize=False)
    preview_gif(tex, out / "preview_dash.gif")
    res = validate(tex)
    dp = res["player_dash_defaults"]

    if not args.out:
        res["meta_created"] = write_meta(png)
        res["folder_meta_created"] = write_folder_meta()
        (SOURCE / "FX-DASH.source.svg").write_text(source_svg(), encoding="utf-8", newline="\n")
        (SOURCE / "FX-DASH.trail.json").write_text(json.dumps(integration_json(dp), ensure_ascii=False, indent=2)
                                                   + "\n", encoding="utf-8", newline="\n")
        source = {
            "asset_id": "FX-DASH", "catalog_id": "A07", "generator": "Tools/FxDashGenerator/generate.py",
            "supersampling": S, "size": [W, H], "head_at_u0": HEAD_AT_U0, "palette": PAL,
            "sweeps": SWEEPS, "edge": EDGE, "envelope": ENV, "trail_proposal": TRAIL, "limits": LIMITS,
            "facets": [{"sweep": sid, "role": role, "alpha": a, "colour": "#%02X%02X%02X" % col,
                        "points": [[round(x, 2), round(y, 2)] for x, y in pts]} for pts, col, a, sid, role in facets()],
            "references": {
                "hero_cape": "Assets/_Project/Art/Concepts/ART_DIRECTION_AND_PROMPTS.md capa #2F6BFF, ciano #4FD8FF",
                "slash_trail": "Assets/_Project/Art/Items/VFX/FX_SlashTrail.png (u = 0 claro na cabeça; Stretch)",
                "slash_trail_config": "Assets/_Project/Scripts/Editor/ProductionVisualIntegrator.cs ConfigureTrail",
                "player_dash": "Assets/_Project/Scripts/Player/PlayerDash.cs (IsDashing, sem evento)",
                "floor_capture": str(FLOOR_CAPTURE.relative_to(ROOT)).replace("\\", "/"),
                "boss_telegraph": "Assets/_Project/Scripts/Editor/DungeonFloorSceneBuilder.cs Mat_TelegraphFill (1,.2,.15)",
            },
        }
        (SOURCE / "FX-DASH.source.json").write_text(json.dumps(source, ensure_ascii=False, indent=2) + "\n",
                                                    encoding="utf-8", newline="\n")
        res["sha256"] = {PNG_NAME: sha(png)}
        (SOURCE / "FxDash.validation.json").write_text(json.dumps(res, ensure_ascii=False, indent=2) + "\n",
                                                       encoding="utf-8", newline="\n")
    for c in res["checks"]:
        print(("ok  " if c["ok"] else "FAIL") + f" {c['name']}: {c['detail']}")
    return 0 if res["ok"] else 1


if __name__ == "__main__":
    sys.exit(main())

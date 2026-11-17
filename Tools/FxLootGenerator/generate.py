"""A06 / FX-LOOT — gerador determinístico dos glints de coleta confirmada (Eldoria).

Desenha uma família de três glints 128×128 com a MESMA forma (o canal alpha é idêntico byte a
byte) e só a cor muda: ciano para XP, latão contido para ouro e marfim quente para equipamento ou
poção guardados. Forma única: estrela compacta de quatro pontas largas, cada ponta em duas facetas
chapadas, núcleo em losango e quatro pontas diagonais curtas; halo curto de poucos degraus. Luz de
cima/esquerda, como ícones, UI-SKIN, FX-HEAL e FX-LEVEL. Saída: três PNG isolados e um atlas
384×128 (três células, não é spritesheet de animação) com mapa de rects. Polígonos com
supersampling 8× e redução com alpha pré-multiplicado. Sem IA generativa nem asset de terceiros.

Uso (Python 3.11 + Pillow 13, ver .claude/skills/create-game-assets/scripts/requirements.txt):
    python Tools/FxLootGenerator/generate.py            # escreve atlas, .meta (só se faltar), Source~
    python Tools/FxLootGenerator/generate.py --out DIR  # escreve só atlas/glints/pranchas em DIR

Sai com código 1 se alguma verificação falhar (tamanhos, RGBA, alpha sujo, margem, forma idêntica
nas três variantes, centro, brilho contido, matiz verde/ameaça, branco opaco, distinção entre as
variantes, contraste sobre a pedra, envelope de integração ≤ 0,35 m e ≤ 8 partículas).
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
STAGING = ROOT / "Assets/_Project/Art/ProductionStaging/FX-LOOT"
SOURCE = STAGING / "Source~"
ATLAS_NAME = "FX-LOOT-Atlas.png"
META_TEMPLATE = ROOT / "Tools/FxHealGenerator/texture.meta.template"  # cópia de FX_Spark.png.meta
FLOOR_CAPTURE = ROOT / "Reports/F05/Visual-final/1920-collection.png"
FLOOR_FEET_1080 = (959, 543)  # centro das botas do Hero na captura F05 1920×1080 (coleta ocorre aqui)
FLOOR_PPM_1080 = 75.0  # px por metro no plano do piso perto do Hero (ladrilho 2 m ≈ 150 px)
ICONS = ROOT / "Assets/_Project/Art/Items/Icons"

S = 8  # supersampling
CELL = 128
CENTRE = (64.0, 64.0)
LIGHT = (-0.55, -0.83)  # luz de cima/esquerda (eixo y da imagem para baixo)
_ln = math.hypot(*LIGHT)
LIGHT = (LIGHT[0] / _ln, LIGHT[1] / _ln)

# ---------------------------------------------------------------- variantes (sRGB)
# Ordem = ordem das células do atlas. "kinds" mapeia LootKind (Assets/_Project/Scripts/Systems/
# LootTable.cs: Xp=0, Gold=1, Potion=2, Weapon=3, Armor=4) para a célula.
# XP: tons da orbe real (ItemModels.mjs M.xp #4FD8FF, emissivo #2FB8E8, M.xpRing #DFFBFF).
# Ouro: latão contido, menos saturado que as moedas M.gold #F2C14E para não duplicá-las; faces
#   escuras com matiz ≥ 40°, longe do laranja de ameaça #FF9A2E (32°).
# Guardado: marfim quente, pouco saturado, sem chegar ao branco opaco.
VARIANTS = [
    {"id": "xp", "label": "XP", "kinds": ["Xp"],
     "shade": "#2A9CC8", "mid": "#4FD8FF", "light": "#C8F4FF", "halo": "#4FD8FF"},
    {"id": "gold", "label": "Ouro", "kinds": ["Gold"],
     "shade": "#A8822E", "mid": "#D4A846", "light": "#EED38A", "halo": "#D4A846"},
    {"id": "stored", "label": "Equipamento/poção guardados", "kinds": ["Potion", "Weapon", "Armor"],
     "shade": "#A8977A", "mid": "#D9CCAC", "light": "#EADCBA", "halo": "#D9CCAC"},
]

# ---------------------------------------------------------------- forma (px na célula 128)
SHAPE = {
    # pontas principais: comprimento a partir do centro (px). Topo um pouco maior e base mais
    # curta: o glint "pousa" sobre o pickup visto pela câmera de 45°, sem parecer cruz simétrica.
    "tip_up": 50.0, "tip_down": 40.0, "tip_side": 43.0,
    "inner_r": 14.0,  # raio dos vértices internos (diagonais): base larga das pontas
    "core_r": 9.0,  # núcleo em losango (faceta mais clara)
    "core_offset": (-1.5, -1.5),  # vértice interno deslocado para a luz: plano facetado
    "diag_tip": 23.0,  # pontas diagonais curtas, atrás da estrela
    "diag_half_w": 5.5,
    "diag_alpha": 0.50,
    "alpha_peak": 0.74,  # opacidade da forma inteira (sobre o halo): brilho contido, nunca opaco
    "halo_r": 30.0,
    "halo_alpha": 0.16,
    "halo_steps": 6,
}

# ---------------------------------------------------------------- integração proposta
# Envelope da ficha A06: 0,20–0,35 s, raio ≤ 0,35 m, ≤ 8 partículas.
EMIT = {
    "particles": 5,
    "lifetime_s": 0.28,
    "start_size_m": 0.20,  # largura do quad; a forma visível ocupa ~73% dele
    "ring_radius_m": 0.20,  # posições fixas num círculo (sem RNG), como o Burst atual do F05
    "height_m": 0.15,
    "rise_m_per_s": 0.40,
}
ENVELOPE = {"radius_m": 0.35, "particles": 8, "lifetime_s": (0.20, 0.35)}

GREEN_HUE = (70.0, 170.0)  # proibido: lembra veneno/nuvem de dano
THREAT_HUES = (0.0, 36.0)  # vermelho #FF2A2A (0°), telegraph (1,.2,.15) ≈ 3°, laranja #FF9A2E (32°)
OPAQUE_WHITE = (0.18, 0.92, 200)  # saturação <, valor >, alpha > → branco opaco proibido
MAX_ALPHA = 205
MAX_COVER = 0.22  # fração máxima da célula com alpha > 50% (glint compacto)
MIN_MARGIN = 10  # px até a borda da célula (mips/bilinear sem vazar entre células)
MIN_CONTRAST = 2.0  # contraste (WCAG) da forma composta vs. pedra #3E4152/#55596B
STONES = ["#3E4152", "#55596B"]


def rgb(h: str) -> tuple[int, int, int]:
    h = h.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def mix(a, b, t):
    a, b = rgb(a) if isinstance(a, str) else a, rgb(b) if isinstance(b, str) else b
    t = min(max(t, 0.0), 1.0)
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


# ---------------------------------------------------------------- desenho (supersampled)
class Layer:
    """Camada RGBA em alta resolução; polígonos chapados compostos por 'over'."""

    def __init__(self):
        self.img = Image.new("RGBA", (CELL * S, CELL * S), (0, 0, 0, 0))

    def poly(self, pts, colour, alpha):
        tmp = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        ImageDraw.Draw(tmp).polygon([(x * S, y * S) for x, y in pts], fill=(*colour, round(255 * alpha)))
        self.img = Image.alpha_composite(self.img, tmp)

    def halo(self, cx, cy, r, colour, alpha, steps):
        tmp = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        for i in range(steps):
            f = 1 - i / steps
            sub = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
            ImageDraw.Draw(sub).ellipse([(cx - r * f) * S, (cy - r * f) * S, (cx + r * f) * S, (cy + r * f) * S],
                                        fill=(*colour, round(255 * alpha / steps)))
            tmp = Image.alpha_composite(tmp, sub)
        self.img = Image.alpha_composite(self.img, tmp)

    def downsample(self) -> Image.Image:
        """Redução com alpha pré-multiplicado; RGB zerado onde alpha = 0 (sem alpha sujo)."""
        small = self.img.convert("RGBa").resize((CELL, CELL), Image.BOX).convert("RGBA")
        mask = small.getchannel("A").point(lambda v: 255 if v > 0 else 0)
        return Image.composite(small, Image.new("RGBA", small.size, (0, 0, 0, 0)), mask)


def lit_of(pts) -> float:
    """Luminosidade 0..1 de uma faceta pela direção do seu centro (normal 'para fora' do glint)."""
    cx = sum(p[0] for p in pts) / len(pts) - CENTRE[0]
    cy = sum(p[1] for p in pts) / len(pts) - CENTRE[1]
    n = math.hypot(cx, cy) or 1.0
    return 0.5 + 0.5 * (cx / n * LIGHT[0] + cy / n * LIGHT[1])


def facets():
    """Lista (pontos, lit, alpha_mul, papel) da forma única, de trás para a frente."""
    cx, cy = CENTRE
    out = []
    # pontas diagonais curtas (atrás)
    for a in (45, 135, 225, 315):
        ra = math.radians(a)
        dx, dy = math.cos(ra), -math.sin(ra)
        px, py = -dy, dx
        tip = (cx + dx * SHAPE["diag_tip"], cy + dy * SHAPE["diag_tip"])
        b1 = (cx + px * SHAPE["diag_half_w"], cy + py * SHAPE["diag_half_w"])
        b2 = (cx - px * SHAPE["diag_half_w"], cy - py * SHAPE["diag_half_w"])
        out.append(([b1, tip, b2, (cx, cy)], lit_of([tip]), SHAPE["diag_alpha"], "diag"))
    # estrela de 4 pontas: cada ponta em duas facetas (centro deslocado → vértice interno → ponta)
    tips = {90: SHAPE["tip_up"], 0: SHAPE["tip_side"], 270: SHAPE["tip_down"], 180: SHAPE["tip_side"]}
    c = (cx + SHAPE["core_offset"][0], cy + SHAPE["core_offset"][1])
    for a, ln in tips.items():
        ra = math.radians(a)
        tip = (cx + math.cos(ra) * ln, cy - math.sin(ra) * ln)
        for side in (-45, 45):
            rb = math.radians(a + side)
            inner = (cx + math.cos(rb) * SHAPE["inner_r"], cy - math.sin(rb) * SHAPE["inner_r"])
            pts = [c, inner, tip]
            out.append((pts, lit_of(pts), 1.0, "tip"))
    # núcleo em losango, 4 facetas claras
    core = [(cx + math.cos(math.radians(a)) * SHAPE["core_r"], cy - math.sin(math.radians(a)) * SHAPE["core_r"])
            for a in (90, 0, 270, 180)]
    for i in range(4):
        pts = [c, core[i], core[(i + 1) % 4]]
        out.append((pts, lit_of(pts), 1.0, "core"))
    return out


def facet_colour(v: dict, lit: float, role: str):
    if role == "core":
        return mix(v["mid"], v["light"], 0.55 + 0.45 * lit)
    if role == "diag":
        return mix(v["shade"], v["mid"], 0.35 + 0.5 * lit)
    # pontas: sombra → meio → realce, com contraste contido
    if lit < 0.5:
        return mix(v["shade"], v["mid"], lit * 2)
    return mix(v["mid"], v["light"], (lit - 0.5) * 1.3)


def render_variant(v: dict) -> Image.Image:
    """Halo atrás; facetas compostas numa camada própria (a estrela cobre as diagonais) e só
    então multiplicadas por alpha_peak, para a sobreposição não somar brilho."""
    layer = Layer()
    layer.halo(CENTRE[0], CENTRE[1], SHAPE["halo_r"], rgb(v["halo"]), SHAPE["halo_alpha"], SHAPE["halo_steps"])
    shape = Layer()
    for pts, lit, amul, role in facets():
        shape.poly(pts, facet_colour(v, lit, role), amul)
    peak = SHAPE["alpha_peak"]
    shape.img.putalpha(shape.img.getchannel("A").point(lambda a: round(a * peak)))
    layer.img = Image.alpha_composite(layer.img, shape.img)
    return layer.downsample()


# ---------------------------------------------------------------- verificações
def luminance(c) -> float:
    def ch(x):
        x /= 255
        return x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4
    return 0.2126 * ch(c[0]) + 0.7152 * ch(c[1]) + 0.0722 * ch(c[2])


def contrast(a, b) -> float:
    la, lb = sorted((luminance(a), luminance(b)), reverse=True)
    return (la + 0.05) / (lb + 0.05)


def pixels(im: Image.Image):
    raw = im.tobytes()
    return [tuple(raw[i:i + 4]) for i in range(0, len(raw), 4)]


def validate(glints, atlas) -> dict:
    res = {"checks": [], "variants": []}

    def check(name, ok, detail):
        res["checks"].append({"name": name, "ok": bool(ok), "detail": detail})

    n = len(VARIANTS)
    check("atlas_size_mode", atlas.size == (CELL * n, CELL) and atlas.mode == "RGBA", f"{atlas.size} {atlas.mode}")
    check("glints_are_128_rgba", all(g.size == (CELL, CELL) and g.mode == "RGBA" for g in glints), f"{n} glints")
    cells_eq = [atlas.crop((i * CELL, 0, (i + 1) * CELL, CELL)).tobytes() == g.tobytes() for i, g in enumerate(glints)]
    check("atlas_cells_equal_glints", all(cells_eq), "célula i do atlas = glint i (byte a byte)")
    alphas = [g.getchannel("A").tobytes() for g in glints]
    check("same_shape_all_variants", all(a == alphas[0] for a in alphas), "canal alpha idêntico nas três variantes")

    dirty = green = threat = white = 0
    margin_min = CELL
    means = []
    for v, g in zip(VARIANTS, glints):
        px = pixels(g)
        info = {"id": v["id"]}
        d = sum(1 for r, gg, b, a in px if a == 0 and (r or gg or b))
        dirty += d
        bb = g.getchannel("A").getbbox()
        info["bbox"] = bb
        m = min(bb[0], bb[1], CELL - bb[2], CELL - bb[3])
        margin_min = min(margin_min, m)
        info["edge_margin_px"] = m
        info["alpha_max"] = max(p[3] for p in px)
        info["cover_over_50pct"] = round(sum(1 for p in px if p[3] > 127) / len(px), 4)
        wsum = sum(p[3] for p in px)
        info["alpha_centroid"] = [round(sum(i % CELL * p[3] for i, p in enumerate(px)) / wsum, 2),
                                  round(sum(i // CELL * p[3] for i, p in enumerate(px)) / wsum, 2)]
        g_ = th = wh = 0
        hs = []
        for r, gg, b, a in px:
            if a < 24:
                continue
            h, s, val = colorsys.rgb_to_hsv(r / 255, gg / 255, b / 255)
            hd = h * 360
            if s > 0.15 and GREEN_HUE[0] <= hd <= GREEN_HUE[1]:
                g_ += 1
            if s > 0.55 and THREAT_HUES[0] <= hd <= THREAT_HUES[1]:
                th += 1
            if s < OPAQUE_WHITE[0] and val > OPAQUE_WHITE[1] and a > OPAQUE_WHITE[2]:
                wh += 1
            hs.append((a, r, gg, b))
        green += g_
        threat += th
        white += wh
        info["green_px"], info["threat_hue_px"], info["opaque_white_px"] = g_, th, wh
        # cor média ponderada pelo alpha (pixels com alpha ≥ 24)
        aw = sum(a for a, *_ in hs)
        mean = tuple(sum(a * c[i] for a, *c in hs) / aw for i in range(3))
        h, s, val = colorsys.rgb_to_hsv(*(c / 255 for c in mean))
        info["mean_rgb"] = [round(c) for c in mean]
        info["mean_hsv"] = [round(h * 360, 1), round(s, 3), round(val, 3)]
        means.append(mean)
        # contraste da forma composta sobre a pedra (média dos pixels com alpha > 50%)
        info["contrast_on_stone"] = {}
        for bg in STONES:
            tile = Image.new("RGBA", (CELL, CELL), (*rgb(bg), 255))
            tile.alpha_composite(g)
            sel = [p for p, q in zip(pixels(tile), px) if q[3] > 127]
            avg = tuple(sum(p[i] for p in sel) / len(sel) for i in range(3))
            info["contrast_on_stone"][bg] = round(contrast(avg, rgb(bg)), 2)
        res["variants"].append(info)

    vi = {v["id"]: i for i, v in enumerate(res["variants"])}
    hsv = {k: res["variants"][i]["mean_hsv"] for k, i in vi.items()}
    check("no_dirty_transparent_px", dirty == 0, f"{dirty} px com alpha 0 e RGB != 0")
    check("margin_ge_10px", margin_min >= MIN_MARGIN, f"margem mínima {margin_min} px (≥ {MIN_MARGIN})")
    cen = [res["variants"][0]["alpha_centroid"]]
    check("centred_pivot", abs(cen[0][0] - CENTRE[0]) <= 2.0 and abs(cen[0][1] - CENTRE[1]) <= 3.0,
          f"centroide do alpha {cen[0]} ≈ centro {CENTRE} (pivô no centro do quad)")
    amax = max(v["alpha_max"] for v in res["variants"])
    check("restrained_brightness", amax <= MAX_ALPHA, f"alpha máx. {amax} ≤ {MAX_ALPHA} (nunca opaco)")
    cov = max(v["cover_over_50pct"] for v in res["variants"])
    check("compact_cover", cov <= MAX_COVER, f"cobertura alpha>50% {cov:.1%} ≤ {MAX_COVER:.0%}")
    check("no_green_hue", green == 0, f"{green} px em matiz {GREEN_HUE}")
    check("no_threat_red_orange", threat == 0, f"{threat} px saturados em matiz {THREAT_HUES}")
    check("no_opaque_white", white == 0,
          f"{white} px com saturação < {OPAQUE_WHITE[0]}, valor > {OPAQUE_WHITE[1]} e alpha > {OPAQUE_WHITE[2]}")
    check("xp_reads_cyan", 180 <= hsv["xp"][0] <= 205 and hsv["xp"][1] >= 0.45, f"XP média HSV {hsv['xp']}")
    check("gold_reads_brass", 40 <= hsv["gold"][0] <= 50 and 0.45 <= hsv["gold"][1] <= 0.75,
          f"ouro média HSV {hsv['gold']} (latão contido: matiz 40–50°, saturação 0,45–0,75)")
    check("stored_reads_ivory", 35 <= hsv["stored"][0] <= 55 and hsv["stored"][1] <= 0.25 and hsv["stored"][2] >= 0.75,
          f"guardado média HSV {hsv['stored']} (marfim: saturação ≤ 0,25, valor ≥ 0,75)")
    dsat = hsv["gold"][1] - hsv["stored"][1]
    check("gold_vs_stored_distinct", dsat >= 0.30, f"diferença de saturação ouro − marfim {dsat:.3f} ≥ 0,30")
    pair = min(math.dist(means[i], means[j]) for i in range(len(means)) for j in range(i + 1, len(means)))
    check("variants_distinct_rgb", pair >= 60, f"menor distância RGB entre médias {pair:.1f} ≥ 60")
    cmin = min(c for v in res["variants"] for c in v["contrast_on_stone"].values())
    check("readable_on_stone", cmin >= MIN_CONTRAST, f"contraste mínimo sobre {STONES} = {cmin:.2f} ≥ {MIN_CONTRAST}")
    reach = EMIT["ring_radius_m"] + EMIT["start_size_m"] / 2
    check("envelope_radius_le_0_35m", reach <= ENVELOPE["radius_m"],
          f"raio do anel {EMIT['ring_radius_m']} + meio quad {EMIT['start_size_m'] / 2} = {reach:.3f} m")
    check("envelope_particles_le_8", EMIT["particles"] <= ENVELOPE["particles"], f"{EMIT['particles']} partículas")
    lo, hi = ENVELOPE["lifetime_s"]
    check("envelope_lifetime", lo <= EMIT["lifetime_s"] <= hi, f"{EMIT['lifetime_s']} s em [{lo}, {hi}]")
    res["ok"] = all(c["ok"] for c in res["checks"])
    return res


# ---------------------------------------------------------------- revisão
def review_backgrounds(glints) -> Image.Image:
    bgs = ["#3E4152", "#55596B", "#0E131A", "#B9C4CF"]
    out = Image.new("RGB", (CELL * len(glints), CELL * len(bgs)), (0, 0, 0))
    for r, bg in enumerate(bgs):
        for k, g in enumerate(glints):
            tile = Image.new("RGBA", (CELL, CELL), (*rgb(bg), 255))
            tile.alpha_composite(g)
            out.paste(tile.convert("RGB"), (k * CELL, r * CELL))
    return out


def burst_positions(scale_px_per_m: float, t: float):
    """Posições projetadas (câmera 45°) das partículas propostas no tempo t (s)."""
    pts = []
    for i in range(EMIT["particles"]):
        a = i * 2 * math.pi / EMIT["particles"] + math.pi / 2
        x, z = math.cos(a) * EMIT["ring_radius_m"], math.sin(a) * EMIT["ring_radius_m"]
        y = EMIT["height_m"] + EMIT["rise_m_per_s"] * t
        pts.append((x * scale_px_per_m, -(y * math.cos(math.radians(45)) + z * math.sin(math.radians(45))) * scale_px_per_m,
                    z))
    return sorted(pts, key=lambda p: -p[2])


def life_alpha(t: float) -> float:
    u = t / EMIT["lifetime_s"]
    return min(1.0, u / 0.25) * max(0.0, 1 - max(0.0, u - 0.45) / 0.55)


def burst_image(base: Image.Image, glint: Image.Image, origin, ppm, t, size_scale=1.0):
    im = base.copy()
    quad = max(4, round(EMIT["start_size_m"] * ppm * size_scale))
    a = life_alpha(t)
    g = glint.resize((quad, quad), Image.LANCZOS)
    if a < 1:
        g.putalpha(g.getchannel("A").point(lambda v: round(v * a)))
    for dx, dy, _z in burst_positions(ppm, t):
        im.alpha_composite(g, (round(origin[0] + dx - quad / 2), round(origin[1] + dy - quad / 2)))
    return im


def review_floor(glints) -> Image.Image:
    """Burst proposto sobre a captura real F05 de coleta, na escala de gameplay (1×) e ampliado 3×,
    no pico (t = 0,10 s), para as três variantes."""
    cap = Image.open(FLOOR_CAPTURE).convert("RGBA")
    half = 70
    crop = cap.crop((FLOOR_FEET_1080[0] - half, FLOOR_FEET_1080[1] - half - 20,
                     FLOOR_FEET_1080[0] + half, FLOOR_FEET_1080[1] + half - 20))
    origin = (half, half + 20)
    tile = 3 * 2 * half
    out = Image.new("RGB", (tile * len(glints), tile + 2 * half + 4), (20, 20, 24))
    for k, g in enumerate(glints):
        im = burst_image(crop, g, origin, FLOOR_PPM_1080, 0.10)
        out.paste(im.convert("RGB"), (k * tile + (tile - 2 * half) // 2, 0))  # 1× (escala de gameplay)
        out.paste(im.resize((tile, tile), Image.NEAREST).convert("RGB"), (k * tile, 2 * half + 4))
    return out


def review_icons(glints) -> Image.Image:
    """Glints ao lado dos ícones reais de item e das cores dos pickups (orbe XP, moedas, vidro)."""
    bg = (*rgb("#3E4152"), 255)
    out = Image.new("RGBA", (CELL * 4, CELL * 2), bg)
    refs = [
        ("xp", Image.new("RGBA", (CELL, CELL), bg), "#4FD8FF"),
        ("gold", Image.new("RGBA", (CELL, CELL), bg), "#F2C14E"),
        ("stored", Image.open(ICONS / "ICON-ITEM-HEALTH.png").convert("RGBA").resize((CELL, CELL), Image.LANCZOS), None),
    ]
    for k, (vid, ref, col) in enumerate(refs):
        tile = Image.new("RGBA", (CELL, CELL), bg)
        if col:  # amostra chapada da cor do material do pickup (não é render do modelo)
            ImageDraw.Draw(tile).ellipse([34, 34, 94, 94], fill=(*rgb(col), 255))
        else:
            tile.alpha_composite(ref)
        out.alpha_composite(tile, (k * CELL, 0))
        out.alpha_composite(glints[k], (k * CELL, CELL))
    extra = Image.open(ICONS / "ICON-ITEM-IRON.png").convert("RGBA").resize((CELL, CELL), Image.LANCZOS)
    out.alpha_composite(extra, (3 * CELL, 0))
    out.alpha_composite(glints[2], (3 * CELL, CELL))
    return out.convert("RGB")


def review_danger(glints) -> Image.Image:
    """Glints em escala ampliada ao lado de um telegraph vermelho de Boss e do laranja de ameaça."""
    w = CELL
    out = Image.new("RGBA", (w * 5, w), (*rgb("#3E4152"), 255))
    d = ImageDraw.Draw(out)
    d.ellipse([10, 34, w - 10, w - 34], fill=(255, 51, 38, 120), outline=(89, 13, 13, 255), width=4)
    d.polygon([(w + 64, 18), (w + 100, 64), (w + 64, 110), (w + 28, 64)], fill=(*rgb("#FF9A2E"), 200))
    for k, g in enumerate(glints):
        out.alpha_composite(g, ((k + 2) * w, 0))
    return out.convert("RGB")


def preview_gif(glints, path: Path):
    cap = Image.open(FLOOR_CAPTURE).convert("RGBA")
    half = 60
    base = cap.crop((FLOOR_FEET_1080[0] - half, FLOOR_FEET_1080[1] - half - 20,
                     FLOOR_FEET_1080[0] + half, FLOOR_FEET_1080[1] + half - 20))
    origin = (half, half + 20)
    seq, durs = [], []
    steps = 7
    dt = EMIT["lifetime_s"] / steps
    for g in glints:
        for i in range(steps + 1):
            im = burst_image(base, g, origin, FLOOR_PPM_1080, i * dt) if i < steps else base.copy()
            seq.append(im.resize((half * 6, half * 6), Image.NEAREST).convert("RGB"))
            durs.append(round(dt * 1000) if i < steps else 500)
    seq[0].save(path, save_all=True, append_images=seq[1:], duration=durs, loop=0, optimize=False, disposal=1)


# ---------------------------------------------------------------- saída
def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_meta(png: Path):
    """Cria a .meta só se faltar. nPOTScale 0 (None): mantém as células 128 alinhadas a pixel
    (384 não é potência de 2; ToNearest do template reescalaria o atlas)."""
    meta = png.with_suffix(png.suffix + ".meta")
    if meta.exists():
        return False
    guid = uuid.UUID(hashlib.sha256(("Eldoria/A06/" + png.name).encode()).hexdigest()[:32]).hex
    text = META_TEMPLATE.read_text(encoding="utf-8").replace("{{GUID}}", guid)
    assert "  nPOTScale: 1\n" in text
    text = text.replace("  nPOTScale: 1\n", "  nPOTScale: 0\n")
    meta.write_text(text, encoding="utf-8", newline="\n")
    return True


def atlas_json() -> dict:
    n = len(VARIANTS)
    rects = []
    for i, v in enumerate(VARIANTS):
        rects.append({
            "index": i, "id": v["id"], "label": v["label"], "loot_kinds": v["kinds"],
            "rect_px_top_left": {"x": i * CELL, "y": 0, "w": CELL, "h": CELL},
            "rect_uv_unity_bottom_left": {"x": round(i / n, 6), "y": 0.0, "w": round(1 / n, 6), "h": 1.0},
            "source_file": f"glints/FX-LOOT-{v['id']}.png",
            "palette": {k: v[k] for k in ("shade", "mid", "light", "halo")},
        })
    return {
        "asset_id": "FX-LOOT",
        "atlas": f"../{ATLAS_NAME}",
        "atlas_size": [CELL * n, CELL],
        "kind": "atlas 2D de três variantes estáticas (não é spritesheet de animação nem corporal)",
        "cell": [CELL, CELL],
        "pivot": {"meaning": "centro do glint = centro do quad", "cell_normalized": [0.5, 0.5]},
        "rects": rects,
        "loot_kind_to_index": {k: i for i, v in enumerate(VARIANTS) for k in v["kinds"]},
        "unity_integration_proposal": {
            "status": "Proposta; NÃO VALIDADO NO EDITOR. Integração é tarefa de código separada.",
            "texture_import": "Default, sRGB, alphaIsTransparency, clamp, bilinear, mipmaps on (como FX_Spark); "
                              "nPOTScale None para manter as células 128 alinhadas a pixel.",
            "material": "URP/Particles/Unlit, Surface Transparent, Blend Alpha, cor branca (a cor já vem na textura); "
                        "opacidade multiplicada pela opção de flash do F05 (options.Current.flash; 0 desliga).",
            "particle_system": {
                "emitter": "reutilizar o collectionEffect existente do PlayerRewardFeedback (sem instanciar por coleta)",
                "max_particles": EMIT["particles"],
                "emission": "um burst por PlayerProgression.CollectionAccepted (Stop+Clear, depois Emit), sem loop",
                "start_lifetime_s": EMIT["lifetime_s"],
                "start_size_m": EMIT["start_size_m"],
                "positions": f"{EMIT['particles']} posições fixas num círculo de {EMIT['ring_radius_m']} m a "
                             f"{EMIT['height_m']} m do piso, subindo {EMIT['rise_m_per_s']} m/s (como o Burst atual; sem RNG)",
                "start_color": "branco × alpha da opção de flash (não tingir: a textura já distingue o tipo)",
                "color_over_lifetime": "alpha 0 → 1 em 25% da vida, cai a partir de 45% até 0",
                "texture_sheet_animation": {
                    "mode": "Grid", "tiles": [len(VARIANTS), 1], "animation": "Whole Sheet",
                    "frame_over_time": "constante 0", "start_frame": "constante = loot_kind_to_index[drop.Kind]",
                    "cycles": 1,
                    "note": "Definir startFrame no módulo antes do Emit (struct do módulo, sem alocação). "
                            "Conferir no Editor a unidade de startFrame (índice de quadro × fração). "
                            "Alternativa: três materiais com offset/tiling fixos.",
                },
                "renderer": {"render_mode": "Billboard", "pivot": [0, 0, 0]},
            },
            "rules": [
                "Emitir só depois de CollectionAccepted (coleta aceita). Pickup recusado (bolsa cheia, morto, "
                "desativado) não emite nem toca a textura.",
                "É só apresentação: não lê nem altera LootTable, quantidades, chances ou estado do RNG de loot; "
                "posições fixas, sem UnityEngine.Random.",
                "Coletas seguidas reiniciam o mesmo emissor (Clear + Emit); no máximo "
                f"{EMIT['particles']} partículas vivas.",
                "Pausa congela; morte, reset e desativação chamam Clear; opção de flash reduz a opacidade e 0 desliga.",
                "Não introduz tipo novo de recompensa: Potion, Weapon e Armor usam a mesma célula marfim.",
            ],
        },
    }


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", type=Path, help="escreve só atlas, glints e pranchas nesta pasta")
    args = ap.parse_args()

    glints = [render_variant(v) for v in VARIANTS]
    atlas = Image.new("RGBA", (CELL * len(VARIANTS), CELL), (0, 0, 0, 0))
    for i, g in enumerate(glints):
        atlas.paste(g, (i * CELL, 0))

    out = args.out if args.out else SOURCE
    gdir = out / "glints" if not args.out else out
    gdir.mkdir(parents=True, exist_ok=True)
    atlas_path = (args.out if args.out else STAGING) / ATLAS_NAME
    atlas.save(atlas_path, optimize=False)
    files = {ATLAS_NAME: atlas_path}
    for v, g in zip(VARIANTS, glints):
        p = gdir / f"FX-LOOT-{v['id']}.png"
        g.save(p, optimize=False)
        files[f"glints/FX-LOOT-{v['id']}.png"] = p
    review_floor(glints).save(out / "review_floor.png", optimize=False)
    review_backgrounds(glints).save(out / "review_backgrounds.png", optimize=False)
    review_icons(glints).save(out / "review_icons.png", optimize=False)
    review_danger(glints).save(out / "review_danger.png", optimize=False)
    preview_gif(glints, out / "preview_floor.gif")
    res = validate(glints, atlas)

    if not args.out:
        created_meta = write_meta(atlas_path)
        (SOURCE / "FX-LOOT.atlas.json").write_text(json.dumps(atlas_json(), ensure_ascii=False, indent=2) + "\n",
                                                   encoding="utf-8", newline="\n")
        source = {
            "asset_id": "FX-LOOT", "catalog_id": "A06", "generator": "Tools/FxLootGenerator/generate.py",
            "supersampling": S, "cell": CELL, "centre_px": CENTRE, "light": LIGHT,
            "variants": VARIANTS, "shape": SHAPE, "emit_proposal": EMIT, "envelope": ENVELOPE,
            "facets": [{"role": role, "lit": round(lit, 3), "alpha_mul": amul,
                        "points": [[round(x, 2), round(y, 2)] for x, y in pts]} for pts, lit, amul, role in facets()],
            "references": {
                "floor_capture": str(FLOOR_CAPTURE.relative_to(ROOT)).replace("\\", "/"),
                "xp_orb": "Tools/Lemo3DGenerator/src/production/ItemModels.mjs M.xp #4FD8FF, emissivo #2FB8E8, M.xpRing #DFFBFF",
                "gold_coins": "ItemModels.mjs M.gold #F2C14E (glint usa latão mais contido #D4A846)",
                "item_icons": "Assets/_Project/Art/Items/Icons/ICON-ITEM-HEALTH.png, ICON-ITEM-IRON.png",
                "f05_collect_burst": "Assets/_Project/Scripts/UI/PlayerRewardFeedback.cs OnCollected: 4 partículas, "
                                     "XP (.3,.85,.95), ouro (.85,.65,.25), demais (.9,.86,.72), raio .48, vida .42 s",
                "loot_kinds": "Assets/_Project/Scripts/Systems/LootTable.cs enum LootKind",
                "boss_telegraph": "Assets/_Project/Scripts/Editor/DungeonFloorSceneBuilder.cs Mat_TelegraphFill (1,.2,.15)",
            },
        }
        (SOURCE / "FX-LOOT.source.json").write_text(json.dumps(source, ensure_ascii=False, indent=2) + "\n",
                                                    encoding="utf-8", newline="\n")
        res["sha256"] = {name: sha(p) for name, p in files.items()}
        res["meta_created"] = created_meta
        (SOURCE / "FxLoot.validation.json").write_text(json.dumps(res, ensure_ascii=False, indent=2) + "\n",
                                                       encoding="utf-8", newline="\n")
    for c in res["checks"]:
        print(("ok  " if c["ok"] else "FAIL") + f" {c['name']}: {c['detail']}")
    return 0 if res["ok"] else 1


if __name__ == "__main__":
    sys.exit(main())

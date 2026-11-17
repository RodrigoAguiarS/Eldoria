"""A04 / FX-HEAL — gerador determinístico da animação de cura discreta (Eldoria).

Desenha 8 quadros 256×256 de um efeito de cura aceita: poucos motes facetados rubi → branco
quente subindo junto às botas e um anel baixo, aberto e muito tênue, que some por completo.
Projeção fixa da câmera do jogo (pitch 45°, offset 0,10,-10), pivô no centro dos pés,
mesma escala em todos os quadros. Polígonos com supersampling 8× e redução com alpha
pré-multiplicado. Sem IA generativa nem asset de terceiros.

Uso (Python 3.11 + Pillow 13, ver .claude/skills/create-game-assets/scripts/requirements.txt):
    python Tools/FxHealGenerator/generate.py            # escreve sheet, .meta (só se faltar), Source~
    python Tools/FxHealGenerator/generate.py --out DIR  # escreve só sheet/quadros em DIR (iteração)

Sai com código 1 se alguma verificação falhar (tamanho, RGBA, alpha sujo, corte na borda da
célula, pivô, quadro final não transparente, matiz verde/ameaça, cobertura de flash).
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
STAGING = ROOT / "Assets/_Project/Art/ProductionStaging/FX-HEAL"
SOURCE = STAGING / "Source~"
SHEET_NAME = "FX-HEAL-Sheet.png"
FLOOR_CAPTURE = ROOT / "Reports/F05/Visual-final/1920-healing.png"
FLOOR_FEET_1080 = (957, 537)  # centro das botas do Hero na captura F05 1920×1080
FLOOR_PPM_1080 = 75.0  # px por metro no plano do piso perto do Hero (ladrilho 2 m ≈ 150 px)

S = 8  # supersampling
CELL = 256
COLS, ROWS = 4, 2
FRAMES = COLS * ROWS
DURATION = 0.4  # s (ficha A04)
PPM = 160.0  # px por metro no plano dos pés, dentro da célula
PIVOT = (128.0, 180.0)  # centro dos pés, em px a partir do topo-esquerda da célula
PITCH = math.radians(45.0)  # câmera do jogo (ArtTestSceneBuilder/DungeonMvpSceneBuilder: offset 0,10,-10)
LIGHT = (-0.55, -0.83)  # luz de cima/esquerda, como ícones e UI-SKIN
_ln = math.hypot(*LIGHT)
LIGHT = (LIGHT[0] / _ln, LIGHT[1] / _ln)

# ---------------------------------------------------------------- paleta (sRGB)
# Rubi do frasco real: Tools/Lemo3DGenerator/src/production/ItemModels.mjs, M.ruby #C21D3A.
# Cor do F05 atual (PlayerRewardFeedback: Color(.92,.32,.42) = #EB526B) fica entre os dois.
PAL = {
    "ruby_deep": "#C21D3A",
    "ruby": "#E04A63",
    "ruby_light": "#F07C8C",
    "warm_white": "#FFEBD8",
    "ivory": "#FFF6EA",
}

# ---------------------------------------------------------------- parâmetros editáveis
# Anel: aberto atrás do Hero (a abertura fica no arco de trás, que no billboard ficaria por
# cima das botas) e partido em arcos facetados; alpha muito baixo para não ler como telegraph.
RING = {
    "radius_m": (0.30, 0.47),  # raio no início/fim (≤ 0,6 m da ficha)
    "width_m": 0.045,
    "segments": 20,  # polígono de 20 lados: planos largos, como o low-poly
    "back_gap_deg": 84.0,  # abertura centrada atrás do Hero
    "front_gaps_deg": [(-62.0, 9.0), (62.0, 9.0)],  # (centro, largura) — quebra o anel em arcos
    "alpha_peak": 0.30,
    "fade": [0.55, 1.0, 0.85, 0.62, 0.40, 0.20, 0.07, 0.0],  # por quadro
    "colour_in": "ruby_light",
    "colour_out": "warm_white",
}
# Motes: 7 (como os do F05), só à frente e aos lados (nenhum atrás do Hero, onde o billboard
# os desenharia por cima do corpo). x/z em metros a partir do centro dos pés; z<0 = frente.
MOTES = [
    # x,     z,    alt. final, atraso, tamanho, deriva x
    (-0.36, -0.08, 0.80, 0.00, 1.00, -0.04),
    (0.38, -0.04, 0.86, 0.06, 1.05, 0.05),
    (-0.22, -0.30, 0.56, 0.10, 0.90, -0.03),
    (0.25, -0.32, 0.62, 0.02, 0.95, 0.03),
    (-0.50, 0.08, 0.46, 0.16, 0.80, -0.02),
    (0.50, 0.10, 0.50, 0.20, 0.82, 0.02),
    (0.02, -0.36, 0.42, 0.08, 0.85, 0.00),
]
MOTE = {
    "half_w_m": 0.06,
    "half_h_m": 0.10,  # losango vertical: forma grande, legível a 0,47× no jogo
    "life": 0.80,  # fração da duração que cada mote vive depois do atraso
    "alpha_peak": 0.92,
    "halo_alpha": 0.20,
    "halo_scale": 2.0,
}
GREEN_HUE = (70.0, 170.0)  # proibido: lembra veneno/nuvem de dano
THREAT_HUES = (0.0, 45.0)  # vermelho #FF2A2A (0°) e laranja #FF9A2E (32°) de ameaça
FLASH_MAX_COVER = 0.06  # fração máxima da célula com alpha > 50%


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


def mote_t(k: int) -> float:
    """Tempo dos motes: começa em 0,10 para o quadro 0 já mostrar os motes nascendo;
    no quadro 7 passa de 1,0 e todos já morreram (atraso + vida <= 1,0)."""
    return 0.10 + frame_t(k) * 0.92


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
            a = alpha / steps
            sub = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
            ImageDraw.Draw(sub).ellipse(
                [(cx - rx * f) * S, (cy - ry * f) * S, (cx + rx * f) * S, (cy + ry * f) * S],
                fill=(*colour, round(255 * a)),
            )
            tmp = Image.alpha_composite(tmp, sub)
        self.img = Image.alpha_composite(self.img, tmp)

    def downsample(self) -> Image.Image:
        """Redução com alpha pré-multiplicado; RGB zerado onde alpha = 0 (sem alpha sujo)."""
        small = self.img.convert("RGBa").resize((CELL, CELL), Image.BOX).convert("RGBA")
        mask = small.getchannel("A").point(lambda v: 255 if v > 0 else 0)
        return Image.composite(small, Image.new("RGBA", small.size, (0, 0, 0, 0)), mask)


def ring_arcs():
    """Intervalos de ângulo (graus, 0 = direita, 90 = atrás) desenhados do anel aberto."""
    seg = 360.0 / RING["segments"]
    gaps = [(90.0, RING["back_gap_deg"])] + [(270.0 + c, w) for c, w in RING["front_gaps_deg"]]

    def in_gap(a):
        for c, w in gaps:
            d = (a - c + 180) % 360 - 180
            if abs(d) < w / 2:
                return True
        return False

    keep = []
    for i in range(RING["segments"]):
        a0, a1 = i * seg, (i + 1) * seg
        if not in_gap((a0 + a1) / 2):
            keep.append((a0, a1))
    return keep


def draw_ring(layer: Layer, k: int):
    t = frame_t(k)
    fade = RING["fade"][k]
    if fade <= 0:
        return
    r = RING["radius_m"][0] + (RING["radius_m"][1] - RING["radius_m"][0]) * ease_out(t)
    w = RING["width_m"]
    colour_base = mix(PAL[RING["colour_in"]], PAL[RING["colour_out"]], t)
    for a0, a1 in ring_arcs():
        pts_out, pts_in = [], []
        for a in (a0, a1):
            ra = math.radians(a)
            # ângulo 90° = atrás (z+), 270° = frente (z−)
            pts_out.append(project((r + w / 2) * math.cos(ra), 0.0, (r + w / 2) * math.sin(ra)))
            pts_in.append(project((r - w / 2) * math.cos(ra), 0.0, (r - w / 2) * math.sin(ra)))
        # faceta chapada: tom pela normal da aresta (luz de cima/esquerda)
        mid = math.radians((a0 + a1) / 2)
        nx, ny = math.cos(mid), -math.sin(mid) * math.cos(PITCH)
        lit = 0.5 + 0.5 * -(nx * LIGHT[0] + ny * LIGHT[1])
        colour = mix(colour_base, PAL["warm_white"], 0.28 * lit)
        layer.poly(pts_out + pts_in[::-1], colour, RING["alpha_peak"] * fade)


def mote_state(m, t):
    x, z, h, delay, size, drift = m
    life = MOTE["life"]
    lt = (t - delay) / life
    if lt < 0 or lt >= 1:
        return None
    y = h * ease_out(lt)
    xx = x + drift * lt
    # entra rápido, segura, some por completo antes do fim
    alpha = smooth(lt / 0.18) * (1 - smooth((lt - 0.55) / 0.45))
    scale = size * (0.65 + 0.35 * smooth(lt / 0.3)) * (1 - 0.35 * smooth((lt - 0.6) / 0.4))
    colour_t = smooth(lt / 0.85) ** 0.8
    return xx, y, z, alpha, scale, colour_t


def draw_mote(layer: Layer, m, t):
    st = mote_state(m, t)
    if st is None:
        return
    x, y, z, alpha, scale, ct = st
    if alpha <= 0.002:
        return
    cx, cy = project(x, y, z)
    hw = MOTE["half_w_m"] * PPM * scale
    hh = MOTE["half_h_m"] * PPM * scale
    base = mix(PAL["ruby"], PAL["warm_white"], ct)
    # halo baixo e largo (sem bloom amplo)
    layer.ellipse_soft(cx, cy, hw * MOTE["halo_scale"], hh * MOTE["halo_scale"] * 0.8,
                       mix(base, PAL["ruby_light"], 0.3), MOTE["halo_alpha"] * alpha)
    # losango em 4 facetas: superior-esquerda mais clara, inferior-direita mais escura
    top, right, bottom, left = (cx, cy - hh), (cx + hw, cy), (cx, cy + hh), (cx - hw, cy)
    c = (cx + hw * 0.12, cy - hh * 0.10)  # vértice interno deslocado: plano largo facetado
    facets = [
        ([left, top, c], 1.00),
        ([top, right, c], 0.78),
        ([right, bottom, c], 0.55),
        ([bottom, left, c], 0.70),
    ]
    dark = mix(PAL["ruby_deep"], base, 0.35 + 0.65 * ct)
    for pts, lum in facets:
        col = mix(dark, mix(base, PAL["ivory"], 0.45 * ct), lum)
        layer.poly(pts, col, MOTE["alpha_peak"] * alpha)


def render_frame(k: int) -> tuple[Image.Image, Image.Image]:
    """Retorna (quadro completo, só o anel) do quadro k."""
    t = mote_t(k)
    ring = Layer()
    draw_ring(ring, k)
    full = Layer()
    draw_ring(full, k)
    # ordem: motes mais distantes (z maior) primeiro
    for m in sorted(MOTES, key=lambda m: -m[1]):
        draw_mote(full, m, t)
    return full.downsample(), ring.downsample()


# ---------------------------------------------------------------- verificações
def bbox_alpha(im: Image.Image, thr: int = 0):
    a = im.getchannel("A").point(lambda v: 255 if v > thr else 0)
    return a.getbbox()


def validate(frames, rings, sheet) -> dict:
    res = {"checks": [], "frames": []}

    def check(name, ok, detail):
        res["checks"].append({"name": name, "ok": bool(ok), "detail": detail})

    check("sheet_size_mode", sheet.size == (CELL * COLS, CELL * ROWS) and sheet.mode == "RGBA",
          f"{sheet.size} {sheet.mode}")
    margin_min = CELL
    dirty_total = 0
    green_total = 0
    threat_total = 0
    for k, (fr, rg) in enumerate(zip(frames, rings)):
        info = {"frame": k, "time_s": round(k * DURATION / FRAMES, 3)}
        cell = sheet.crop(((k % COLS) * CELL, (k // COLS) * CELL, (k % COLS + 1) * CELL, (k // COLS + 1) * CELL))
        info["sheet_cell_equals_frame"] = cell.tobytes() == fr.tobytes()
        raw = fr.tobytes()
        px = [tuple(raw[i:i + 4]) for i in range(0, len(raw), 4)]
        dirty = sum(1 for r, g, b, a in px if a == 0 and (r or g or b))
        dirty_total += dirty
        alpha_sum = sum(p[3] for p in px)
        info["alpha_sum"] = alpha_sum
        info["alpha_max"] = max(p[3] for p in px)
        cover = sum(1 for p in px if p[3] > 127) / len(px)
        info["cover_over_50pct"] = round(cover, 5)
        bb = bbox_alpha(fr)
        info["bbox"] = bb
        if bb:
            m = min(bb[0], bb[1], CELL - bb[2], CELL - bb[3])
            margin_min = min(margin_min, m)
            info["edge_margin_px"] = m
        g = t = 0
        for r, gg, b, a in px:
            if a < 24:
                continue
            h, s, v = colorsys.rgb_to_hsv(r / 255, gg / 255, b / 255)
            hd = h * 360
            if s > 0.15 and GREEN_HUE[0] <= hd <= GREEN_HUE[1]:
                g += 1
            if s > 0.55 and THREAT_HUES[0] <= hd <= THREAT_HUES[1]:
                t += 1
        green_total += g
        threat_total += t
        info["green_px"], info["threat_hue_px"] = g, t
        rb = bbox_alpha(rg)
        if rb:
            info["ring_bbox"] = rb
            info["ring_centre_x"] = (rb[0] + rb[2]) / 2
        res["frames"].append(info)

    check("frames_are_256_rgba", all(f.size == (CELL, CELL) and f.mode == "RGBA" for f in frames), "8 quadros")
    check("sheet_cells_equal_frames", all(f["sheet_cell_equals_frame"] for f in res["frames"]), "célula k = quadro k")
    check("no_dirty_transparent_px", dirty_total == 0, f"{dirty_total} px com alpha 0 e RGB != 0")
    check("no_edge_cut_margin_ge_8px", margin_min >= 8, f"margem mínima {margin_min} px")
    check("final_frame_transparent", res["frames"][-1]["alpha_max"] == 0, f"alpha máx. quadro 7 = {res['frames'][-1]['alpha_max']}")
    check("frames_0_to_6_visible", all(f["alpha_sum"] > 0 for f in res["frames"][:-1]), "quadros 0–6 não vazios")
    check("fades_out", res["frames"][6]["alpha_sum"] < 0.35 * max(f["alpha_sum"] for f in res["frames"]),
          "quadro 6 < 35% do pico")
    check("no_green_hue", green_total == 0, f"{green_total} px em matiz {GREEN_HUE}")
    check("no_threat_red_orange", threat_total == 0, f"{threat_total} px saturados em matiz {THREAT_HUES}")
    check("no_wide_flash", all(f["cover_over_50pct"] <= FLASH_MAX_COVER for f in res["frames"]),
          f"cobertura alpha>50% ≤ {FLASH_MAX_COVER:.0%} por quadro")
    # pivô: anel centrado em x = pivô e base do anel = pivô + ry(raio do quadro) em todos os quadros
    piv_ok = True
    for k, f in enumerate(res["frames"]):
        if "ring_bbox" not in f:
            continue
        r = RING["radius_m"][0] + (RING["radius_m"][1] - RING["radius_m"][0]) * ease_out(frame_t(k))
        exp_bottom = PIVOT[1] + (r + RING["width_m"] / 2) * math.sin(PITCH) * PPM
        f["ring_bottom_expected"] = round(exp_bottom, 2)
        if abs(f["ring_centre_x"] - PIVOT[0]) > 1.0 or abs(f["ring_bbox"][3] - exp_bottom) > 1.5:
            piv_ok = False
    check("pivot_consistent", piv_ok, f"anel centrado em x={PIVOT[0]} e base coerente com pivô y={PIVOT[1]}")
    radius_ok = RING["radius_m"][1] + RING["width_m"] / 2 <= 0.6 and all(
        abs(m[0]) + abs(m[5]) + MOTE["half_w_m"] * m[4] <= 0.6 and abs(m[1]) <= 0.6 for m in MOTES)
    check("gameplay_radius_le_0_6m", radius_ok, "anel e motes dentro de 0,6 m")
    check("particle_budget_le_16", len(MOTES) + 1 <= 16, f"{len(MOTES)} motes + 1 anel")
    res["ok"] = all(c["ok"] for c in res["checks"])
    return res


# ---------------------------------------------------------------- revisão
def review_floor(frames) -> Image.Image:
    """Quadros sobre a captura real F05 (1920×1080) na escala de gameplay e ampliados 2×."""
    cap = Image.open(FLOOR_CAPTURE).convert("RGBA")
    scale = FLOOR_PPM_1080 / PPM
    cw = round(CELL * scale)
    fx, fy = FLOOR_FEET_1080
    # recorte com o Hero (validação no contexto) e um trecho de piso limpo
    spots = [("hero", (fx, fy)), ("piso", (700, 330))]
    tile = 2 * cw
    rows = []
    for _name, (sx, sy) in spots:
        row = Image.new("RGBA", (FRAMES * tile, tile), (0, 0, 0, 255))
        for k, fr in enumerate(frames):
            small = fr.resize((cw, cw), Image.LANCZOS)
            ox = round(sx - PIVOT[0] * scale)
            oy = round(sy - PIVOT[1] * scale)
            crop = cap.crop((ox, oy, ox + cw, oy + cw)).copy()
            crop.alpha_composite(small)
            row.paste(crop.resize((tile, tile), Image.NEAREST), (k * tile, 0))
        rows.append(row)
    out = Image.new("RGBA", (FRAMES * tile, tile * len(rows) + 4), (20, 20, 24, 255))
    for i, r in enumerate(rows):
        out.paste(r, (0, i * (tile + 4)))
    return out.convert("RGB")


def review_backgrounds(frames) -> Image.Image:
    bgs = ["#3E4152", "#55596B", "#0E131A", "#B9C4CF"]
    out = Image.new("RGB", (FRAMES * CELL, CELL * len(bgs)), (0, 0, 0))
    for r, bg in enumerate(bgs):
        for k, fr in enumerate(frames):
            tile = Image.new("RGBA", (CELL, CELL), (*rgb(bg), 255))
            d = ImageDraw.Draw(tile)
            d.line([(PIVOT[0] - 6, PIVOT[1]), (PIVOT[0] + 6, PIVOT[1])], fill=(255, 255, 255, 90))
            d.line([(PIVOT[0], PIVOT[1] - 6), (PIVOT[0], PIVOT[1] + 6)], fill=(255, 255, 255, 90))
            tile.alpha_composite(fr)
            out.paste(tile.convert("RGB"), (k * CELL, r * CELL))
    return out


def preview_gif(frames, path: Path):
    cap = Image.open(FLOOR_CAPTURE).convert("RGBA")
    scale = FLOOR_PPM_1080 / PPM
    cw = round(CELL * scale)
    fx, fy = FLOOR_FEET_1080
    ox, oy = round(fx - PIVOT[0] * scale), round(fy - PIVOT[1] * scale)
    base = cap.crop((ox, oy, ox + cw, oy + cw))
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
    guid = uuid.UUID(hashlib.sha256(("Eldoria/A04/" + png.name).encode()).hexdigest()[:32]).hex
    tpl = (Path(__file__).parent / "texture.meta.template").read_text(encoding="utf-8")
    meta.write_text(tpl.replace("{{GUID}}", guid), encoding="utf-8", newline="\n")
    return True


def timing_json() -> dict:
    fd = DURATION / FRAMES
    pivot_norm = (PIVOT[0] / CELL, 1 - PIVOT[1] / CELL)  # Unity: origem embaixo-esquerda
    quad_m = CELL / PPM
    return {
        "asset_id": "FX-HEAL",
        "sheet": f"../{SHEET_NAME}",
        "sheet_size": [CELL * COLS, CELL * ROWS],
        "grid": {"columns": COLS, "rows": ROWS, "cell": [CELL, CELL], "order": "linha a linha, da esquerda para a direita, de cima para baixo"},
        "frames": [{"index": k, "cell": [k % COLS, k // COLS], "start_s": round(k * fd, 3), "duration_s": round(fd, 3),
                    "file": f"frames/FX-HEAL-F{k}.png"} for k in range(FRAMES)],
        "duration_s": DURATION,
        "fps": round(FRAMES / DURATION, 3),
        "loop": False,
        "final_frame": "quadro 7 totalmente transparente (fim do efeito)",
        "pivot": {
            "meaning": "centro dos pés do Hero, sobre o piso",
            "cell_px_from_top_left": list(PIVOT),
            "cell_normalized_unity_bottom_left": [round(pivot_norm[0], 4), round(pivot_norm[1], 4)],
        },
        "scale": {
            "pixels_per_metre_at_feet": PPM,
            "cell_size_m": round(quad_m, 3),
            "gameplay_radius_m": 0.6,
            "in_game_reference": f"~{FLOOR_PPM_1080:.0f} px/m perto do Hero a 1920×1080 → sheet exibida a ~{FLOOR_PPM_1080 / PPM:.2f}×",
        },
        "camera": {"pitch_deg": 45, "offset": [0, 10, -10], "note": "Billboard voltado para a câmera; o anel já vem projetado a 45°."},
        "unity_integration_proposal": {
            "status": "Proposta; NÃO VALIDADO NO EDITOR. Integração é tarefa de código separada.",
            "texture_import": "Default, sRGB, alphaIsTransparency, clamp, bilinear, mipmaps on (como FX_Spark).",
            "material": "URP/Particles/Unlit, Surface Transparent, Blend Alpha, cor branca; opacidade multiplicada pela opção de flash do F05.",
            "particle_system": {
                "max_particles": 1,
                "emission": "1 burst por cura aceita (PlayerRewardFeedback.OnHealed), sem loop",
                "start_lifetime_s": DURATION,
                "start_size_m": round(quad_m, 3),
                "simulation_space": "World (efeito fica no piso se o Hero andar)",
                "texture_sheet_animation": {"mode": "Grid", "tiles": [COLS, ROWS], "animation": "Whole Sheet",
                                             "frame_over_time": "linear 0→1", "cycles": 1},
                "renderer": {"render_mode": "Billboard",
                             "pivot": [0, round((0.5 - pivot_norm[1]), 4), 0],
                             "pivot_note": "Desloca o quad para o pivô dos pés coincidir com a posição da partícula (unidades do tamanho). Conferir o sinal no Editor."},
            },
            "rules": [
                "Disparar só após cura efetiva (HealingAccepted, origem != LevelUp); HP cheio/morto não publica.",
                "Não atrasar a cura: o efeito é só apresentação, iniciado depois do evento.",
                "Pausa congela (Time.timeScale/ParticleSystem.Pause); morte, reset e desativação chamam Clear.",
                "Retrigger reusa o mesmo ParticleSystem (Clear + Emit), sem instanciar.",
            ],
        },
    }


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", type=Path, help="escreve só sheet e quadros nesta pasta")
    args = ap.parse_args()

    frames, rings = [], []
    for k in range(FRAMES):
        f, r = render_frame(k)
        frames.append(f)
        rings.append(r)
    sheet = Image.new("RGBA", (CELL * COLS, CELL * ROWS), (0, 0, 0, 0))
    for k, f in enumerate(frames):
        sheet.paste(f, ((k % COLS) * CELL, (k // COLS) * CELL))

    if args.out:
        args.out.mkdir(parents=True, exist_ok=True)
        sheet.save(args.out / SHEET_NAME, optimize=False)
        for k, f in enumerate(frames):
            f.save(args.out / f"FX-HEAL-F{k}.png", optimize=False)
        res = validate(frames, rings, sheet)
        print(json.dumps(res["checks"], ensure_ascii=False, indent=1))
        return 0 if res["ok"] else 1

    (SOURCE / "frames").mkdir(parents=True, exist_ok=True)
    sheet_path = STAGING / SHEET_NAME
    sheet.save(sheet_path, optimize=False)
    created_meta = write_meta(sheet_path)
    files = {SHEET_NAME: sheet_path}
    for k, f in enumerate(frames):
        p = SOURCE / "frames" / f"FX-HEAL-F{k}.png"
        f.save(p, optimize=False)
        files[f"frames/FX-HEAL-F{k}.png"] = p
    review_floor(frames).save(SOURCE / "review_floor.png", optimize=False)
    review_backgrounds(frames).save(SOURCE / "review_backgrounds.png", optimize=False)
    preview_gif(frames, SOURCE / "preview_floor.gif")

    (SOURCE / "FX-HEAL.timing.json").write_text(json.dumps(timing_json(), ensure_ascii=False, indent=2) + "\n",
                                                encoding="utf-8", newline="\n")
    source = {
        "asset_id": "FX-HEAL", "catalog_id": "A04", "generator": "Tools/FxHealGenerator/generate.py",
        "supersampling": S, "cell": CELL, "grid": [COLS, ROWS], "duration_s": DURATION, "ppm": PPM,
        "pivot_px": PIVOT, "camera_pitch_deg": 45, "light": LIGHT, "palette": PAL, "ring": RING,
        "ring_arcs_deg": ring_arcs(), "motes": [dict(zip(["x_m", "z_m", "rise_m", "delay", "size", "drift_x_m"], m)) for m in MOTES],
        "mote": MOTE, "references": {
            "floor_capture": str(FLOOR_CAPTURE.relative_to(ROOT)).replace("\\", "/"),
            "potion_ruby": "Tools/Lemo3DGenerator/src/production/ItemModels.mjs M.ruby #C21D3A",
            "f05_heal_colour": "Assets/_Project/Scripts/UI/PlayerRewardFeedback.cs Color(.92,.32,.42)",
            "camera": "Assets/_Project/Scripts/Editor/ArtTestSceneBuilder.cs (offset 0,10,-10, pitch 45°)",
        },
    }
    (SOURCE / "FX-HEAL.source.json").write_text(json.dumps(source, ensure_ascii=False, indent=2) + "\n",
                                                encoding="utf-8", newline="\n")
    res = validate(frames, rings, sheet)
    res["sha256"] = {name: sha(p) for name, p in files.items()}
    res["meta_created"] = created_meta
    (SOURCE / "FxHeal.validation.json").write_text(json.dumps(res, ensure_ascii=False, indent=2) + "\n",
                                                   encoding="utf-8", newline="\n")
    for c in res["checks"]:
        print(("ok  " if c["ok"] else "FAIL") + f" {c['name']}: {c['detail']}")
    return 0 if res["ok"] else 1


if __name__ == "__main__":
    sys.exit(main())

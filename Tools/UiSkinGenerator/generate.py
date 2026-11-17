"""A03 / UI-SKIN — gerador determinístico do kit modular de UI (Eldoria).

Desenha painéis, botões, slots, marcadores e o canto de foco como polígonos facetados
(chanfros de 45°, facetas sombreadas por uma luz fixa vinda de cima/esquerda), com
supersampling 8× e alpha pré-multiplicado na redução. Sem IA generativa nem asset de terceiros.

Uso (Python 3.11 + Pillow 13, ver .claude/skills/create-game-assets/scripts/requirements.txt):
    python Tools/UiSkinGenerator/generate.py            # escreve PNG, .meta (só se faltar), Source~
    python Tools/UiSkinGenerator/generate.py --out DIR  # escreve só os PNG em DIR (iteração)

Sai com código 1 se alguma verificação falhar (tamanho, RGBA, alpha sujo, bordas 9-slice
não uniformes, centro não liso, contraste de texto abaixo do mínimo).
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
import sys
import uuid
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
STAGING = ROOT / "Assets/_Project/Art/ProductionStaging/UI-SKIN"
SOURCE = STAGING / "Source~"
S = 8  # supersampling
LIGHT = (-0.45, -1.0)  # luz vinda de cima, levemente da esquerda (y para baixo)
_ln = math.hypot(*LIGHT)
LIGHT = (LIGHT[0] / _ln, LIGHT[1] / _ln)
R2 = math.sqrt(2.0)
# normais externas das 8 arestas do octógono, na ordem de octagon()
NORMALS = [(0, -1), (1 / R2, -1 / R2), (1, 0), (1 / R2, 1 / R2), (0, 1), (-1 / R2, 1 / R2), (-1, 0), (-1 / R2, -1 / R2)]

# ---------------------------------------------------------------- paleta (sRGB)
# Fundo da janela atual (InventorySceneBuilder: Color(.055,.075,.1)) = #0E131A, igual à ficha A03.
PAL = {
    "outline": "#05070A",
    "panel_face": "#0E131A", "panel_hi": "#2B3746", "panel_lo": "#05080B",
    "inset_face": "#0A0E13", "inset_line": "#253040",
    # latão fosco: #E0B040 da direção, dessaturado para linhas discretas
    "brass_hi": "#C9A65A", "brass_lo": "#6F5A30",
    "brass_bright_hi": "#E8C878", "brass_bright_lo": "#94773E",
    # botões: dourado fosco atual (Color(.8,.68,.45) = #CCAD73), mantido para o texto escuro atual
    "btn_face": "#C8AA70", "btn_hi": "#EBD6A4", "btn_lo": "#6E5730", "btn_line_hi": "#8F7340", "btn_line_lo": "#6A532A",
    "btn_hover_face": "#D9BE88", "btn_hover_line": "#F7E8C2",
    "btn_pressed_face": "#A68C5A",
    "btn_disabled_face": "#6B6E75", "btn_disabled_hi": "#8A8D94", "btn_disabled_lo": "#3A3D44",
    # slots: fundo atual Color(.10,.13,.17) = #1A2129 e selecionado Color(.32,.26,.16)
    "slot_face": "#1A2129", "slot_hi": "#344252", "slot_lo": "#090C10",
    "slot_hover_face": "#222B36",
    "slot_empty_face": "#11161C",
    "slot_selected_face": "#4A3B24",
    # foco: marfim claro + contorno escuro (lê em painel escuro e em botão dourado)
    "focus_hi": "#FFF4D6", "focus_lo": "#CDBE98", "focus_dark": "#070A0E",
    "steel_hi": "#D6DEE6", "steel_lo": "#8693A1", "steel_dim": "#5B6675",
    "white": "#FFFFFF",
}
# Cores de texto atuais (UGUI Text com LegacyRuntime.ttf) usadas na checagem de contraste.
TEXT = {
    "light": "#FFFFFF",          # rótulos sobre painel/slot
    "dark": "#0A0F14",           # Color(.04,.06,.08) dos botões
    "empty": "#808FA1",  # Color(.5,.56,.63) de slot vazio
    "notice": "#FFD173",         # Color(1,.82,.45)
    "rarity_comum": "#C7D1DE", "rarity_raro": "#4DB8FF", "rarity_epico": "#D98CFF",
}


def rgb(h: str) -> tuple[int, int, int]:
    h = h.lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def lerp(a, b, t):
    a, b = rgb(a), rgb(b)
    return tuple(round(a[i] + (b[i] - a[i]) * t) for i in range(3))


def shade(normal, lo: str, hi: str, invert=False):
    d = normal[0] * LIGHT[0] + normal[1] * LIGHT[1]
    t = (d + 1.0) / 2.0
    if invert:
        t = 1.0 - t
    # degraus discretos por faceta (planos chapados, sem gradiente ruidoso)
    return lerp(lo, hi, t)


# ---------------------------------------------------------------- geometria
def octagon(w, h, d, c):
    """Octógono do retângulo (0,0,w,h) recuado d, com chanfro externo c."""
    cc = max(c - d * (2 - R2), 0.0) if c > 0 else 0.0
    x0, y0, x1, y1 = d, d, w - d, h - d
    return [(x0 + cc, y0), (x1 - cc, y0), (x1, y0 + cc), (x1, y1 - cc),
            (x1 - cc, y1), (x0 + cc, y1), (x0, y1 - cc), (x0, y0 + cc)]


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.im = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.im)

    @staticmethod
    def _pts(pts):
        """Pillow preenche floor(x0)..floor(x1) (inclusivo nas duas pontas). Recuar cada aresta
        0,5 subpixel para dentro (offset com miter) dá cobertura exata [a, b) em subpixels,
        então polígonos vizinhos não se sobrepõem e as bordas 9-slice ficam uniformes."""
        p = []
        for x, y in pts:
            q = (x * S, y * S)
            if not p or abs(q[0] - p[-1][0]) > 1e-9 or abs(q[1] - p[-1][1]) > 1e-9:
                p.append(q)
        if len(p) > 1 and abs(p[0][0] - p[-1][0]) < 1e-9 and abs(p[0][1] - p[-1][1]) < 1e-9:
            p.pop()
        n = len(p)
        area = sum(p[i][0] * p[(i + 1) % n][1] - p[(i + 1) % n][0] * p[i][1] for i in range(n))
        sgn = 1.0 if area > 0 else -1.0  # y para baixo: área > 0 = sentido horário na tela
        e = 0.5
        lines = []
        for i in range(n):
            (x0, y0), (x1, y1) = p[i], p[(i + 1) % n]
            dx, dy = x1 - x0, y1 - y0
            ln = math.hypot(dx, dy)
            nx, ny = -dy / ln * sgn, dx / ln * sgn  # normal para dentro
            lines.append(((x0 + nx * e, y0 + ny * e), (dx, dy), (nx, ny)))
        out = []
        for i in range(n):
            (ax, ay), (adx, ady), _ = lines[i - 1]
            (bx, by), (bdx, bdy), (bnx, bny) = lines[i]
            den = adx * bdy - ady * bdx
            if abs(den) < 1e-9:
                out.append((p[i][0] + bnx * e, p[i][1] + bny * e))
            else:
                t = ((bx - ax) * bdy - (by - ay) * bdx) / den
                out.append((ax + adx * t, ay + ady * t))
        return out

    def poly(self, pts, color):
        c = color if isinstance(color, tuple) else rgb(color)
        self.d.polygon(self._pts(pts), fill=c + (255,))

    def ring(self, w, h, c, a, b, lo, hi, invert=False, only=None):
        """Faixa facetada entre os recuos a e b; cada aresta recebe o tom da sua normal."""
        o, i = octagon(w, h, a, c), octagon(w, h, b, c)
        for k in range(8):
            if only is not None and k not in only:
                continue
            q = [o[k], o[(k + 1) % 8], i[(k + 1) % 8], i[k]]
            self.poly(q, shade(NORMALS[k], lo, hi, invert))

    def fill(self, w, h, c, d, color):
        self.poly(octagon(w, h, d, c), color)

    def finish(self) -> Image.Image:
        small = self.im.reduce(S)  # média por bloco: RGB sai pré-multiplicado (fora = 0)
        px = small.load()
        for y in range(small.height):
            for x in range(small.width):
                r, g, b, a = px[x, y]
                if a == 0:
                    px[x, y] = (0, 0, 0, 0)
                elif a < 255:
                    px[x, y] = (min(255, round(r * 255 / a)), min(255, round(g * 255 / a)), min(255, round(b * 255 / a)), a)
        return small


# ---------------------------------------------------------------- componentes
# Parâmetros editáveis: tamanho, borda 9-slice (L,B,R,T como no spriteBorder da Unity), chanfro,
# largura do chanfro (bevel) e posição/largura da linha de latão — tudo dentro da borda.
PANEL = {"size": (128, 128), "border": 16, "chamfer": 11, "outline": 1, "bevel": 5, "line_at": 9, "line_w": 2}
BUTTON = {"size": (256, 80), "border": 12, "chamfer": 8, "outline": 1, "bevel": 4, "line_at": 7, "line_w": 1}
SLOT = {"size": (192, 112), "border": 8, "chamfer": 5, "outline": 1, "bevel": 3, "line_at": 5, "line_w": 1}
MARKER = 24
CORNER = 32


def base(p, face, lo, hi, invert=False, line=None, line_hi=None, line_lo=None, line_w=None, line_at=None):
    w, h = p["size"]
    c = p["chamfer"]
    cv = Canvas(w, h)
    ol, bv = p["outline"], p["bevel"]
    cv.fill(w, h, c, 0, PAL["outline"])
    cv.ring(w, h, c, ol, ol + bv, lo, hi, invert)
    cv.fill(w, h, c, ol + bv, face)
    if line:
        at = p["line_at"] if line_at is None else line_at
        lw = p["line_w"] if line_w is None else line_w
        cv.ring(w, h, c, at, at + lw, line_lo, line_hi)
        cv.fill(w, h, c, at + lw, face)
    return cv


def panel_window():
    cv = base(PANEL, PAL["panel_face"], PAL["panel_lo"], PAL["panel_hi"], line=True,
              line_hi=PAL["brass_hi"], line_lo=PAL["brass_lo"])
    return cv.finish()


def panel_inset():
    p = PANEL
    cv = base(p, PAL["inset_face"], PAL["panel_lo"], PAL["panel_hi"], invert=True, line=True,
              line_hi=PAL["inset_line"], line_lo=PAL["inset_line"], line_w=1)
    return cv.finish()


def button(state):
    p = BUTTON
    if state == "Disabled":
        # sem linha de latão: a forma também muda, não só a cor
        return base(p, PAL["btn_disabled_face"], PAL["btn_disabled_lo"], PAL["btn_disabled_hi"]).finish()
    face = {"Normal": "btn_face", "Hover": "btn_hover_face", "Focus": "btn_face", "Pressed": "btn_pressed_face"}[state]
    invert = state == "Pressed"
    if state == "Hover":
        # hover: face clara + linha interna dupla clara (forma extra)
        cv = base(p, PAL[face], PAL["btn_lo"], PAL["btn_hi"], line=True, line_hi=PAL["btn_hover_line"],
                  line_lo=PAL["btn_line_hi"], line_w=2)
    else:
        cv = base(p, PAL[face], PAL["btn_lo"], PAL["btn_hi"], invert=invert, line=True,
                  line_hi=PAL["btn_line_hi"], line_lo=PAL["btn_line_lo"], line_at=p["line_at"] + (1 if invert else 0))
    if state == "Focus":
        focus_caps(cv, p)
    return cv.finish()


def focus_caps(cv, p):
    """Capas geométricas nos quatro cantos (somente dentro das células de canto da 9-slice)."""
    w, h = p["size"]
    b, c = p["border"], p["chamfer"]
    outer, mid = 2.5, 6.5
    o, m = octagon(w, h, 0, c), octagon(w, h, outer, c)
    i = octagon(w, h, mid, c)
    # canto superior esquerdo em coordenadas locais; os demais por espelhamento
    def tl(path):
        return [path[7], path[0]]  # (x0, y0+cc) -> (x0+cc, y0)
    for mx, my in ((0, 0), (1, 0), (1, 1), (0, 1)):
        def f(pt):
            x, y = pt
            return (w - x if mx else x, h - y if my else y)
        a0, a1 = tl(o)
        b0, b1 = tl(m)
        c0, c1 = tl(i)
        dark = [(0, b), a0, a1, (b, 0), (b, outer), b1, b0, (outer, b)]
        light = [(outer, b), b0, b1, (b, outer), (b, mid), c1, c0, (mid, b)]
        cv.poly([f(q) for q in dark], PAL["focus_dark"])
        cv.poly([f(q) for q in light], PAL["focus_hi"])


def slot(state):
    p = SLOT
    if state == "Empty":
        # rebaixado (chanfro invertido) e sem linha de latão
        return base(p, PAL["slot_empty_face"], PAL["slot_lo"], PAL["slot_hi"], invert=True).finish()
    if state == "Selected":
        # moldura de latão grossa (3 px) no lugar do chanfro: espessura muda, não só a cor
        w, h = p["size"]
        c = p["chamfer"]
        cv = Canvas(w, h)
        cv.fill(w, h, c, 0, PAL["outline"])
        cv.ring(w, h, c, 1, 4, PAL["brass_bright_lo"], PAL["brass_bright_hi"])
        cv.fill(w, h, c, 4, PAL["slot_selected_face"])
        cv.ring(w, h, c, 5, 6, PAL["brass_lo"], PAL["brass_hi"])
        cv.fill(w, h, c, 6, PAL["slot_selected_face"])
        return cv.finish()
    face = PAL["slot_hover_face"] if state == "Hover" else PAL["slot_face"]
    lh, ll = (PAL["brass_bright_hi"], PAL["brass_hi"]) if state == "Hover" else (PAL["brass_hi"], PAL["brass_lo"])
    return base(p, face, PAL["slot_lo"], PAL["slot_hi"], line=True, line_hi=lh, line_lo=ll,
                line_w=2 if state == "Hover" else 1).finish()


def slot_rarity_band():
    """Faixa branca na borda esquerda, tingida em runtime com ItemRarity.color (nome da raridade segue no texto)."""
    w, h = SLOT["size"]
    cv = Canvas(w, h)
    x0, x1 = 2, 6
    t = 2
    cv.poly([(x0, 6 + t), (x0 + t, 6), (x1, 6), (x1, h - 6), (x0 + t, h - 6), (x0, h - 6 - t)], PAL["white"])
    return cv.finish()


def marker_selected():
    n = MARKER
    cv = Canvas(n, n)
    cx = cy = n / 2
    r_out, r_in = 11.5, 9.5
    pts_o = [(cx, cy - r_out), (cx + r_out, cy), (cx, cy + r_out), (cx - r_out, cy)]
    cv.poly(pts_o, PAL["outline"])
    top, right, bottom, left = (cx, cy - r_in), (cx + r_in, cy), (cx, cy + r_in), (cx - r_in, cy)
    c = (cx, cy)
    for tri, nrm in (([top, right, c], (1, -1)), ([right, bottom, c], (1, 1)), ([bottom, left, c], (-1, 1)), ([left, top, c], (-1, -1))):
        ln = math.hypot(*nrm)
        cv.poly(tri, shade((nrm[0] / ln, nrm[1] / ln), PAL["brass_bright_lo"], PAL["focus_hi"]))
    return cv.finish()


def marker_equipped():
    """Escudo pequeno de aço com aro de latão: forma diferente do losango de seleção."""
    n = MARKER
    cv = Canvas(n, n)
    outer = [(2, 2), (22, 2), (22, 12), (12, 23), (2, 12)]
    cv.poly(outer, PAL["outline"])
    rim = [(3.5, 3.5), (20.5, 3.5), (20.5, 11.5), (12, 21), (3.5, 11.5)]
    cv.poly(rim, PAL["brass_hi"])
    left = [(5.5, 5.5), (12, 5.5), (12, 18.2), (5.5, 11)]
    right = [(12, 5.5), (18.5, 5.5), (18.5, 11), (12, 18.2)]
    cv.poly(left, PAL["steel_hi"])
    cv.poly(right, PAL["steel_lo"])
    return cv.finish()


def marker_empty():
    """Octógono vazado em aço apagado: indica slot livre sem competir com o texto."""
    n = MARKER
    cv = Canvas(n, n)
    cv.fill(n, n, 7, 1, PAL["outline"])
    cv.ring(n, n, 7, 2, 4.5, PAL["steel_dim"], PAL["steel_lo"])
    cv.fill(n, n, 7, 4.5, PAL["outline"])
    im = cv.finish()
    # abre o centro (transparente) mantendo contorno escuro de 1 px por dentro do aro
    hole = Canvas(n, n)
    hole.fill(n, n, 7, 5.5, PAL["white"])
    mask = hole.finish().getchannel("A")
    out = Image.new("RGBA", (n, n), (0, 0, 0, 0))
    px, pm, po = im.load(), mask.load(), out.load()
    for y in range(n):
        for x in range(n):
            r, g, b, al = px[x, y]
            na = round(al * (255 - pm[x, y]) / 255)
            po[x, y] = (r, g, b, na) if na else (0, 0, 0, 0)
    return out


def focus_corner():
    """Colchete em L com ponta chanfrada (canto superior esquerdo); rotacionar/espelhar para os outros."""
    n, t, c = CORNER, 8, 9
    cv = Canvas(n, n)
    outer = [(0, c), (c, 0), (n, 0), (n - 2, t), (t + (c - t) * 0 + 2.5, t), (t, t + 2.5), (t, n - 2), (0, n)]
    cv.poly(outer, PAL["focus_dark"])
    i = 2
    inner = [(i, c + 0.8), (c + 0.8, i), (n - 3.2, i), (n - 4.6, t - 2), (t + 1.6, t - 2), (t - 2, t + 1.6), (t - 2, n - 4.6), (i, n - 3.2)]
    cv.poly(inner, PAL["focus_lo"])
    hi = [(i, c + 0.8), (c + 0.8, i), (n - 3.2, i), (n - 3.9, i + 2.2), (c + 1.7, i + 2.2), (i + 2.2, c + 1.7), (i + 2.2, n - 3.9), (i, n - 3.2)]
    cv.poly(hi, PAL["focus_hi"])
    return cv.finish()


ASSETS = [
    # nome, função, borda (L,B,R,T) ou None, papel
    ("UI-SKIN-Panel-Window", panel_window, PANEL["border"], "base"),
    ("UI-SKIN-Panel-Inset", panel_inset, PANEL["border"], "base"),
    ("UI-SKIN-Button-Normal", lambda: button("Normal"), BUTTON["border"], "base"),
    ("UI-SKIN-Button-Hover", lambda: button("Hover"), BUTTON["border"], "base"),
    ("UI-SKIN-Button-Focus", lambda: button("Focus"), BUTTON["border"], "base"),
    ("UI-SKIN-Button-Pressed", lambda: button("Pressed"), BUTTON["border"], "base"),
    ("UI-SKIN-Button-Disabled", lambda: button("Disabled"), BUTTON["border"], "base"),
    ("UI-SKIN-Slot-Normal", lambda: slot("Normal"), SLOT["border"], "base"),
    ("UI-SKIN-Slot-Hover", lambda: slot("Hover"), SLOT["border"], "base"),
    ("UI-SKIN-Slot-Selected", lambda: slot("Selected"), SLOT["border"], "base"),
    ("UI-SKIN-Slot-Empty", lambda: slot("Empty"), SLOT["border"], "base"),
    ("UI-SKIN-Slot-RarityBand", slot_rarity_band, SLOT["border"], "overlay"),
    ("UI-SKIN-Marker-Selected", marker_selected, None, "marker"),
    ("UI-SKIN-Marker-Equipped", marker_equipped, None, "marker"),
    ("UI-SKIN-Marker-Empty", marker_empty, None, "marker"),
    ("UI-SKIN-FocusCorner", focus_corner, None, "marker"),
]
EXPECTED = {
    "Panel": (128, 128), "Button": (256, 80), "Slot": (192, 112), "Marker": (MARKER, MARKER), "FocusCorner": (CORNER, CORNER),
}


def expected_size(name):
    for k, v in EXPECTED.items():
        if f"-{k}" in name:
            return v
    raise KeyError(name)


# ---------------------------------------------------------------- verificações
def luminance(c):
    def ch(v):
        v /= 255
        return v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4
    r, g, b = c[:3]
    return 0.2126 * ch(r) + 0.7152 * ch(g) + 0.0722 * ch(b)


def contrast(a, b):
    la, lb = luminance(a), luminance(b)
    hi, lo = max(la, lb), min(la, lb)
    return (hi + 0.05) / (lo + 0.05)


def check_nine_slice(im, border):
    """Bordas uniformes ao longo do eixo esticado e centro liso: esticar não deforma nada."""
    w, h = im.size
    px = im.load()
    issues = []
    L = B = R = T = border
    for y in list(range(T)) + list(range(h - B, h)):
        ref = px[L, y]
        if any(px[x, y] != ref for x in range(L, w - R)):
            issues.append(f"linha {y} varia na borda horizontal")
    for x in list(range(L)) + list(range(w - R, w)):
        ref = px[x, T]
        if any(px[x, y] != ref for y in range(T, h - B)):
            issues.append(f"coluna {x} varia na borda vertical")
    centre = {px[x, y] for y in range(T, h - B) for x in range(L, w - R)}
    if len(centre) != 1:
        issues.append(f"centro com {len(centre)} cores (esperado 1, liso)")
    return issues, (next(iter(centre)) if len(centre) == 1 else None)


def dirty_transparent(im):
    return sum(1 for y in range(im.height) for x in range(im.width) if im.getpixel((x, y))[3] == 0 and im.getpixel((x, y))[:3] != (0, 0, 0))


def grey_delta(a, b, border):
    """Diferença média de luminância (0–255) na faixa de borda: estados diferentes também em cinza."""
    ga, gb = a.convert("L").load(), b.convert("L").load()
    w, h = a.size
    tot = n = 0
    for y in range(h):
        for x in range(w):
            if x < border or x >= w - border or y < border or y >= h - border:
                tot += abs(ga[x, y] - gb[x, y]); n += 1
    return tot / n


def struct_delta(a, b, border):
    """Pixels de borda cuja luminância muda > 24 entre estados: mede diferença de forma/estrutura."""
    ga, gb = a.convert("L").load(), b.convert("L").load()
    w, h = a.size
    return sum(1 for y in range(h) for x in range(w)
               if (x < border or x >= w - border or y < border or y >= h - border) and abs(ga[x, y] - gb[x, y]) > 24)


# ---------------------------------------------------------------- .meta Unity
def meta_text(guid, sprite_id, border):
    lb = f"{{x: {border}, y: {border}, z: {border}, w: {border}}}" if border else "{x: 0, y: 0, z: 0, w: 0}"
    tpl = (Path(__file__).with_name("sprite.meta.template")).read_text(encoding="utf-8")
    return (tpl.replace("{GUID}", guid).replace("{SPRITE_ID}", sprite_id).replace("{BORDER}", lb)
            .replace("{MESH_TYPE}", "0"))


def stable_hex(name, salt):
    return uuid.UUID(hashlib.sha256(f"eldoria/ui-skin/{salt}/{name}".encode()).hexdigest()[:32]).hex


# ---------------------------------------------------------------- revisão
def nine_slice(im, border, w, h):
    """Replica Image.Type.Sliced (fillCenter, sem tile) para a prancha de revisão."""
    sw, sh = im.size
    b = border
    out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    xs = [(0, b, 0, b), (b, sw - b, b, w - b), (sw - b, sw, w - b, w)]
    ys = [(0, b, 0, b), (b, sh - b, b, h - b), (sh - b, sh, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 <= dx0 or dy1 <= dy0:
                continue
            part = im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.Resampling.NEAREST)
            out.alpha_composite(part, (dx0, dy0))
    return out


def font(size):
    for f in ("C:/Windows/Fonts/arial.ttf", "/System/Library/Fonts/Supplemental/Arial.ttf", "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"):
        if Path(f).exists():
            return ImageFont.truetype(f, size)
    return ImageFont.load_default(size)


def review_sheet(imgs):
    """Composição só para revisão (não é entregável): tamanhos reais da UI atual em 1920×1080."""
    W, H = 1920, 1080
    sheet = Image.new("RGBA", (W, H), rgb("#1B1F27") + (255,))
    d = ImageDraw.Draw(sheet)
    win = nine_slice(imgs["UI-SKIN-Panel-Window"], 16, 1820, 1000)
    sheet.alpha_composite(win, (50, 40))
    f22, f17, f30 = font(22), font(17), font(30)
    d.text((82, 70), "INVENTÁRIO   16/16  (revisão A03 — texto nativo simulado)", font=f30, fill=TEXT["light"])
    states = ["Normal", "Hover", "Selected", "Empty"]
    for i in range(16):
        col, row = i % 4, i // 4
        x, y = 82 + col * 313, 190 + row * 161
        st = states[i % 4] if i < 8 else ("Normal" if i % 3 else "Empty")
        s = nine_slice(imgs[f"UI-SKIN-Slot-{st}"], 8, 301, 149)
        sheet.alpha_composite(s, (x, y))
        if st != "Empty":
            band = nine_slice(imgs["UI-SKIN-Slot-RarityBand"], 8, 301, 149)
            tint = [rgb(TEXT["rarity_comum"]), rgb(TEXT["rarity_raro"]), rgb(TEXT["rarity_epico"])][i % 3]
            tb = Image.new("RGBA", band.size, tint + (255,))
            tb.putalpha(band.getchannel("A"))
            sheet.alpha_composite(tb, (x, y))
            d.text((x + 74, y + 44), "Espada ancestral do\nsentinela d…", font=f17, fill=TEXT["light"])
            d.text((x + 74, y + 86), ["Comum", "Raro", "Épico"][i % 3], font=f17, fill=[TEXT["rarity_comum"], TEXT["rarity_raro"], TEXT["rarity_epico"]][i % 3])
        else:
            d.text((x + 74, y + 54), "—\nVazio", font=f17, fill=TEXT["empty"])
            sheet.alpha_composite(imgs["UI-SKIN-Marker-Empty"], (x + 28, y + 62))
        d.text((x + 8, y + 6), st, font=font(13), fill="#7F8C9A")
        if st == "Selected":
            sheet.alpha_composite(imgs["UI-SKIN-Marker-Selected"], (x - 8, y - 8))
        if i == 1:
            fc = imgs["UI-SKIN-FocusCorner"]
            for fx, fy, img in ((x - 6, y - 6, fc), (x + 301 + 6 - 32, y - 6, fc.transpose(Image.Transpose.FLIP_LEFT_RIGHT)),
                                (x - 6, y + 149 + 6 - 32, fc.transpose(Image.Transpose.FLIP_TOP_BOTTOM)),
                                (x + 301 + 6 - 32, y + 149 + 6 - 32, fc.transpose(Image.Transpose.ROTATE_180))):
                sheet.alpha_composite(img, (fx, fy))
            d.text((x + 120, y + 6), "+ FocusCorner ×4", font=font(13), fill="#7F8C9A")
        if i == 4:
            sheet.alpha_composite(imgs["UI-SKIN-Marker-Equipped"], (x + 301 - 32, y + 8))
            d.text((x + 74, y + 106), "Equipado", font=f17, fill=TEXT["light"])
    inset = nine_slice(imgs["UI-SKIN-Panel-Inset"], 16, 488, 716)
    sheet.alpha_composite(inset, (1350, 190))
    d.text((1376, 214), "ARMA\nEspada ancestral\n\nDano 26   Defesa 0\nHP 100/100", font=f22, fill=TEXT["light"])
    labels = [("Normal", "Equipar"), ("Hover", "Desequipar"), ("Focus", "Usar"), ("Pressed", "Largar 1"), ("Disabled", "Voltar")]
    for k, (st, lab) in enumerate(labels):
        x = 82 + k * 313 if k < 4 else 1350
        wbtn = 301 if k < 4 else 488
        b = nine_slice(imgs[f"UI-SKIN-Button-{st}"], 12, wbtn, 64)
        sheet.alpha_composite(b, (x, 962))
        tw = d.textlength(lab, font=f22)
        d.text((x + (wbtn - tw) / 2, 962 + 32 - 13), lab, font=f22, fill=TEXT["dark"])
        d.text((x + 4, 940), st, font=font(13), fill="#7F8C9A")
    # botão largo do menu principal (600×80)
    b = nine_slice(imgs["UI-SKIN-Button-Normal"], 12, 600, 80)
    sheet.alpha_composite(b, (395, 842))
    tw = d.textlength("Jogar", font=f22)
    d.text((395 + (600 - tw) / 2, 842 + 40 - 13), "Jogar", font=f22, fill=TEXT["dark"])
    d.text((395 + 4, 822), "Normal 600×80 (MainMenu)", font=font(13), fill="#7F8C9A")
    return sheet


# ---------------------------------------------------------------- principal
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", type=Path, help="pasta alternativa só para PNG (iteração)")
    args = ap.parse_args()
    out = args.out or STAGING
    out.mkdir(parents=True, exist_ok=True)
    import PIL
    report = {"generator": "Tools/UiSkinGenerator/generate.py", "python": sys.version.split()[0], "pillow": PIL.__version__,
              "supersampling": S, "light_dir": LIGHT, "assets": [], "contrast": [], "state_deltas": [], "ok": True}
    imgs = {}
    for name, fn, border, role in ASSETS:
        im = fn()
        imgs[name] = im
        path = out / f"{name}.png"
        im.save(path, optimize=False, compress_level=9)
        disk = Image.open(path)
        disk.load()
        ew, eh = expected_size(name)
        issues = []
        if disk.size != (ew, eh):
            issues.append(f"tamanho {disk.size} != {(ew, eh)}")
        if disk.mode != "RGBA":
            issues.append(f"modo {disk.mode}")
        amin, amax = disk.getchannel("A").getextrema()
        if amin != 0:
            issues.append("sem pixel transparente (alpha real exigido)")
        dirty = dirty_transparent(disk)
        if dirty:
            issues.append(f"{dirty} pixels transparentes com RGB != 0")
        centre = None
        if border:
            ns, centre = check_nine_slice(disk, border)
            issues += ns
        entry = {"file": path.name, "size": list(disk.size), "mode": disk.mode, "role": role,
                 "border_LBRT": [border] * 4 if border else None, "alpha_min": amin, "alpha_max": amax,
                 "transparent_pct": round(100 * sum(1 for v in disk.getchannel("A").tobytes() if v == 0) / (ew * eh), 2),
                 "centre_rgba": list(centre) if centre else None,
                 "sha256": hashlib.sha256(path.read_bytes()).hexdigest(), "issues": issues}
        report["assets"].append(entry)
        if issues:
            report["ok"] = False

    # contraste do texto atual sobre o centro (área de texto) de cada base
    pairs = [
        ("UI-SKIN-Panel-Window", "light", 4.5), ("UI-SKIN-Panel-Window", "notice", 4.5),
        ("UI-SKIN-Panel-Inset", "light", 4.5),
        ("UI-SKIN-Slot-Normal", "light", 4.5), ("UI-SKIN-Slot-Hover", "light", 4.5), ("UI-SKIN-Slot-Selected", "light", 4.5),
        ("UI-SKIN-Slot-Empty", "empty", 4.5),
        ("UI-SKIN-Slot-Normal", "rarity_comum", 4.5), ("UI-SKIN-Slot-Normal", "rarity_raro", 4.5), ("UI-SKIN-Slot-Normal", "rarity_epico", 4.5),
        ("UI-SKIN-Slot-Selected", "rarity_comum", 4.5), ("UI-SKIN-Slot-Selected", "rarity_raro", 4.5), ("UI-SKIN-Slot-Selected", "rarity_epico", 4.5),
        ("UI-SKIN-Button-Normal", "dark", 4.5), ("UI-SKIN-Button-Hover", "dark", 4.5), ("UI-SKIN-Button-Focus", "dark", 4.5),
        ("UI-SKIN-Button-Pressed", "dark", 4.5), ("UI-SKIN-Button-Disabled", "dark", 3.0),
    ]
    centres = {a["file"][:-4]: a["centre_rgba"] for a in report["assets"]}
    for spr, txt, need in pairs:
        cr = round(contrast(rgb(TEXT[txt]), tuple(centres[spr][:3])), 2)
        ok = cr >= need
        report["contrast"].append({"sprite": spr, "text": txt, "text_hex": TEXT[txt], "ratio": cr, "min": need, "ok": ok})
        if not ok:
            report["ok"] = False

    # estados distinguíveis além da cor: diferença em cinza + pixels estruturais na faixa de borda
    groups = {"Button": (["Normal", "Hover", "Focus", "Pressed", "Disabled"], 12), "Slot": (["Normal", "Hover", "Selected", "Empty"], 8)}
    for g, (sts, b) in groups.items():
        for i, a in enumerate(sts):
            for c in sts[i + 1:]:
                A, Bm = imgs[f"UI-SKIN-{g}-{a}"], imgs[f"UI-SKIN-{g}-{c}"]
                gd = round(grey_delta(A, Bm, b), 2)
                sd = struct_delta(A, Bm, b)
                ok = sd >= 40
                report["state_deltas"].append({"group": g, "a": a, "b": c, "grey_mean_delta_border": gd, "structural_px": sd, "ok": ok})
                if not ok:
                    report["ok"] = False

    if args.out is None:
        SOURCE.mkdir(exist_ok=True)
        # .meta: só cria se faltar (preserva GUID se a Unity já importou)
        for name, _, border, _ in ASSETS:
            mp = out / f"{name}.png.meta"
            if not mp.exists():
                mp.write_text(meta_text(stable_hex(name, "guid"), stable_hex(name, "sprite"), border), encoding="utf-8", newline="\n")
        folder_meta = STAGING.parent / "UI-SKIN.meta"
        if not folder_meta.exists():
            folder_meta.write_text(
                f"fileFormatVersion: 2\nguid: {stable_hex('folder', 'guid')}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n"
                "  userData: \n  assetBundleName: \n  assetBundleVariant: \n", encoding="utf-8", newline="\n")
        sheet = review_sheet(imgs)
        sheet.convert("RGB").save(SOURCE / "review_1080.png", optimize=True)
        sheet.resize((1280, 720), Image.Resampling.BILINEAR).convert("RGB").save(SOURCE / "review_720.png", optimize=True)
        # estados lado a lado ampliados 3× (vizinho mais próximo) em cor e cinza
        cells = [n for n, *_ in ASSETS]
        cw, chh = 256 * 3 // 2, 112 * 3 // 2
        grid = Image.new("RGBA", (cw * 4 + 40, (chh + 20) * 8 + 20), rgb("#2A2F38") + (255,))
        for k, n in enumerate(cells):
            im = imgs[n]
            sc = min(cw / im.width, chh / im.height, 3)
            big = im.resize((max(1, int(im.width * sc)), max(1, int(im.height * sc))), Image.Resampling.NEAREST)
            gx, gy = 10 + (k % 4) * (cw + 8), 10 + (k // 4) * (chh + 20)
            grid.alpha_composite(big, (gx, gy))
            ImageDraw.Draw(grid).text((gx, gy + chh + 2), n.replace("UI-SKIN-", ""), font=font(13), fill="#C7D1DE")
            gb = Image.merge("RGBA", (*([big.convert("L")] * 3), big.getchannel("A")))
            grid.alpha_composite(gb, (gx, gy + (chh + 20) * 4))
        grid.convert("RGB").save(SOURCE / "states_colour_grey_x3.png", optimize=True)
        params = {"PANEL": PANEL, "BUTTON": BUTTON, "SLOT": SLOT, "MARKER": MARKER, "CORNER": CORNER, "PAL": PAL, "TEXT": TEXT,
                  "LIGHT": LIGHT, "S": S}
        (SOURCE / "UI-SKIN.source.json").write_text(json.dumps(params, indent=2, ensure_ascii=False), encoding="utf-8", newline="\n")
        (SOURCE / "UiSkin.validation.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8", newline="\n")

    bad = [a for a in report["assets"] if a["issues"]] + [c for c in report["contrast"] if not c["ok"]] + [s for s in report["state_deltas"] if not s["ok"]]
    for b in bad:
        print("FALHA", json.dumps(b, ensure_ascii=False))
    print(f"{len(report['assets'])} sprites; contraste {sum(c['ok'] for c in report['contrast'])}/{len(report['contrast'])}; "
          f"estados {sum(s['ok'] for s in report['state_deltas'])}/{len(report['state_deltas'])}; ok={report['ok']}")
    return 0 if report["ok"] else 1


if __name__ == "__main__":
    sys.exit(main())

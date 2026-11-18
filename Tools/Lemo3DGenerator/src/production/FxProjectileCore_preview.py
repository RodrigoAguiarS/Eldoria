"""A08 — FX-PROJECTILE: validação independente (relê do disco) + previews.

Rode depois de `node src/production/FxProjectileCore.mjs`:
    python -I Tools/Lemo3DGenerator/src/production/FxProjectileCore_preview.py

1. Relê FX-PROJECTILE-Core.fbx (FBX 7.4 ASCII) e as paletas PNG do disco, sem three.js, e confere:
   triângulos <= 300, raio máx <= 0,2 m (envelope da esfera atual), centro dos bounds no pivô, unidade (UnitScaleFactor 100 = m),
   eixos (+Y up, +Z front), 1 material, texturas referenciadas existentes, ausência de rig/deformador/animação/câmera/luz,
   cada peça fechada (cada aresta em exatamente 2 triângulos), volume com sinal > 0 por peça (winding para fora),
   normais = normal geométrica, UVs no centro de células da paleta, cores da paleta = PALETTE do gerador.
2. Renderiza previews ortográficos (frontal, lateral, topo, câmera do jogo a 45°) com sombreamento simples e emissão
   — rasterizador próprio em NumPy, rotulados PREVIEW (não são captura da Unity).
3. Mede, pela câmera do jogo (pitch 45°, yaw 0, DungeonMvpSceneBuilder.CameraOffset), para 24 rumos: área da silhueta
   vs. esfera atual, fração da silhueta com brasa/parede de fenda e contraste com o piso.
4. Compõe a prévia em escala (~75 px/m) sobre a captura real F05 1920×1080.

Coordenadas: o FBX está em convenção three.js (destro); a Unity espelha X na importação (mesma convenção do lote
ITEMS-COMBAT). O preview aplica x → −x para mostrar o que a Unity exibirá. A forma é simétrica em X, exceto o
núcleo interno.
"""
from __future__ import annotations

import hashlib
import json
import math
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[4]
STAGING = ROOT / "Assets/_Project/Art/ProductionStaging/FX-PROJECTILE"
SRC = STAGING / "Source~"
NAME = "FX-PROJECTILE-Core"
FBX = STAGING / f"{NAME}.fbx"
F05 = ROOT / "Reports/F05/Visual-final/1920-collection.png"
F05_PPM = 75.0           # px por metro perto do Hero na captura F05 (mesma constante do A07)
ENVELOPE = 0.2
BUDGET = 300
CUR_COLOR = (1.0, 0.55, 0.1)   # Mat_Projectile _BaseColor (sem emissão)
FLOOR = {"#3E4152": (0x3E, 0x41, 0x52), "#55596B": (0x55, 0x59, 0x6B)}

def srgb_to_lin(c):
    c = np.asarray(c, dtype=np.float64)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)

def lin_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)

def hex_rgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))

# ———————————————————————— leitura do FBX ASCII ————————————————————————
def fbx_array(text, name):
    m = re.search(rf"{name}: \*(\d+) \{{\s*a: ([^}}]*)\}}", text)
    if not m:
        raise ValueError(f"array {name} ausente")
    vals = [float(v) for v in m.group(2).replace("\n", "").split(",")]
    if len(vals) != int(m.group(1)):
        raise ValueError(f"{name}: contagem declarada {m.group(1)} != {len(vals)}")
    return vals

def read_fbx(path: Path):
    text = path.read_text(encoding="utf-8")
    verts = np.array(fbx_array(text, "Vertices")).reshape(-1, 3)
    pvi = [int(v) for v in fbx_array(text, "PolygonVertexIndex")]
    normals = np.array(fbx_array(text, "Normals")).reshape(-1, 3)
    uv = np.array(fbx_array(text, "UV")).reshape(-1, 2)
    uvi = [int(v) for v in fbx_array(text, "UVIndex")]
    polys, cur = [], []
    for v in pvi:
        if v < 0:
            cur.append(-v - 1)
            polys.append(cur)
            cur = []
        else:
            cur.append(v)
    settings = dict(re.findall(r'P: "(\w+)", "(?:int|double)", "(?:Integer|Number)", "",(-?[\d.]+)', text))
    objects = re.findall(r"^\t(\w+): \d+, \"(\w+)::", text, flags=re.M)
    textures = re.findall(r'\tTexture: \d+, "Texture::[^"]+", "" \{.*?RelativeFilename: "([^"]+)"', text, flags=re.S)
    tex_conn = re.findall(r'C: "OP",\d+,\d+, "(\w+)"', text)
    return {"text": text, "verts": verts, "polys": polys, "normals": normals, "uv": uv, "uvi": uvi,
            "settings": settings, "objects": objects, "textures": textures, "tex_conn": tex_conn}

# ———————————————————————— validação ————————————————————————
def components(tris_idx):
    parent = {}
    def find(a):
        while parent.setdefault(a, a) != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a
    for t in tris_idx:
        for v in t[1:]:
            ra, rb = find(t[0]), find(v)
            if ra != rb:
                parent[ra] = rb
    groups = {}
    for i, t in enumerate(tris_idx):
        groups.setdefault(find(t[0]), []).append(i)
    return list(groups.values())

def validate(fbx, source):
    checks = []
    def check(name, ok, detail):
        checks.append({"check": name, "ok": bool(ok), "detail": detail})

    V, polys = fbx["verts"], fbx["polys"]
    tri = [p for p in polys if len(p) == 3]
    check("triangulos", len(tri) == len(polys) and len(tri) <= BUDGET, f"{len(tri)} triângulos (todos tri: {len(tri) == len(polys)}), orçamento ≤ {BUDGET}")
    r = np.linalg.norm(V, axis=1)
    check("envelope", r.max() <= ENVELOPE + 1e-5, f"raio máx {r.max():.4f} m ≤ {ENVELOPE} (diâmetro ≤ 0,4 m, igual à esfera atual); raio de dano SphereCast 0,25 m inalterado")
    bmin, bmax = V.min(0), V.max(0)
    ctr, size = (bmin + bmax) / 2, bmax - bmin
    check("pivo_centro", np.linalg.norm(ctr) <= 0.01, f"centro dos bounds {np.round(ctr, 4).tolist()} (≤ 1 cm do pivô); tamanho {np.round(size, 4).tolist()} m")
    s = fbx["settings"]
    check("unidade_eixos", s.get("UnitScaleFactor") == "100" and s.get("UpAxis") == "1" and s.get("FrontAxis") == "2" and s.get("FrontAxisSign") == "1",
          f"UnitScaleFactor {s.get('UnitScaleFactor')} (cm×100 = m), UpAxis {s.get('UpAxis')} (+Y), FrontAxis {s.get('FrontAxis')}/{s.get('FrontAxisSign')} (+Z)")
    kinds = sorted(set(k for k, _ in fbx["objects"]))
    forbidden = {"Deformer", "AnimationStack", "AnimationCurve", "AnimationCurveNode", "NodeAttribute", "Pose"}
    n_mat = sum(1 for k, _ in fbx["objects"] if k == "Material")
    check("objetos", n_mat == 1 and not forbidden & set(kinds) and sum(1 for k, _ in fbx["objects"] if k == "Model") == 1,
          f"tipos {kinds}; 1 Model, {n_mat} Material; sem rig/deformador/animação/câmera/luz/colisor")
    tex_ok = all((STAGING / t).is_file() for t in fbx["textures"]) and sorted(fbx["tex_conn"]) == ["DiffuseColor", "EmissiveColor"]
    check("texturas_referenciadas", tex_ok, f"{fbx['textures']} ao lado do FBX, ligadas a {sorted(fbx['tex_conn'])}")

    # peças fechadas, winding para fora
    comps = components(tri)
    pieces = []
    closed_all, vol_all = True, True
    for c in comps:
        edges = {}
        vol = 0.0
        for i in c:
            a, b, d = tri[i]
            for e in ((a, b), (b, d), (d, a)):
                k = (min(e), max(e))
                edges[k] = edges.get(k, 0) + 1
            vol += np.dot(V[a], np.cross(V[b], V[d])) / 6
        closed = all(n == 2 for n in edges.values())
        closed_all &= closed
        vol_all &= vol > 0
        pieces.append({"triangles": len(c), "closed": closed, "signed_volume_m3": round(float(vol), 7)})
    check("pecas_fechadas", closed_all and len(comps) == 5, f"{len(comps)} peças (4 placas + brasa), todas fechadas: {closed_all}")
    check("winding_para_fora", vol_all, "volume com sinal > 0 em todas as peças")

    # normais por vértice de polígono = normal geométrica
    N = fbx["normals"]
    worst = 1.0
    for i, (a, b, d) in enumerate(tri):
        g = np.cross(V[b] - V[a], V[d] - V[a])
        g /= np.linalg.norm(g)
        for k in range(3):
            n = N[i * 3 + k] / np.linalg.norm(N[i * 3 + k])
            worst = min(worst, float(np.dot(n, g)))
    check("normais", worst > 0.999, f"menor cos(normal FBX, normal geométrica) = {worst:.5f} (flat shading, para fora)")

    # UVs no centro das células; paleta no disco = gerador
    pal = source["palette"]
    centers = {name: ((sw["cell"][0] * 8 + 4) / 16, 1 - (sw["cell"][1] * 8 + 4) / 16) for name, sw in pal.items()}
    uv_ok, swatch_of_face = True, []
    for i in range(len(tri)):
        u = {tuple(np.round(fbx["uv"][fbx["uvi"][i * 3 + k]], 4)) for k in range(3)}
        if len(u) != 1:
            uv_ok = False
            swatch_of_face.append(None)
            continue
        uu = next(iter(u))
        name = next((n for n, c in centers.items() if abs(c[0] - uu[0]) < 1e-3 and abs(c[1] - uu[1]) < 1e-3), None)
        uv_ok &= name is not None
        swatch_of_face.append(name)
    counts = {n: swatch_of_face.count(n) for n in pal}
    check("uv_paleta", uv_ok and counts["ember"] > 0 and counts["iron"] > 0 and counts["fissureWall"] > 0,
          f"cada face com UV constante no centro de uma célula; faces por amostra {counts}")
    pal_ok, pal_detail = True, []
    for chan, file in (("base", source["material"]["baseMap"]), ("emission", source["material"]["emissionMap"])):
        im = np.asarray(Image.open(STAGING / file).convert("RGBA"))
        ok = im.shape == (16, 16, 4) and (im[..., 3] == 255).all()
        for name, sw in pal.items():
            cx, cy = sw["cell"]
            cell = im[cy * 8:cy * 8 + 8, cx * 8:cx * 8 + 8, :3].reshape(-1, 3)
            ok &= (cell == np.array(hex_rgb(sw[chan]))).all()
        pal_ok &= ok
        pal_detail.append(f"{file} {im.shape[1]}×{im.shape[0]} {'ok' if ok else 'DIVERGE'}")
    check("paleta_png", pal_ok, "; ".join(pal_detail))
    return checks, pieces, swatch_of_face, {"min": np.round(bmin, 4).tolist(), "max": np.round(bmax, 4).tolist(), "size": np.round(size, 4).tolist(), "center": np.round(ctr, 4).tolist(), "max_radius": round(float(r.max()), 5)}

# ———————————————————————— rasterizador ————————————————————————
LIGHT = np.array([-0.35, 0.85, -0.4])
LIGHT /= np.linalg.norm(LIGHT)
AMBIENT, DIFFUSE = 0.32, 0.78

def camera(forward):
    f = np.asarray(forward, float)
    f /= np.linalg.norm(f)
    right = np.cross([0, 1, 0], f)
    right /= np.linalg.norm(right)
    up = np.cross(f, right)
    return right, up, f

GAME_CAM = camera([0, -math.sin(math.radians(45)), math.cos(math.radians(45))])

def yaw(points, deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    R = np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])   # Unity: rotação Y positiva leva +Z para +X
    return points @ R.T

def shade(faces_unity, mat, emission_hdr):
    """faces_unity: lista de (tri[3,3], normal[3], swatch). mat: swatch → (base_lin, emission_lin)."""
    out = []
    for p, n, sw in faces_unity:
        base, emis = mat[sw]
        lit = base * (AMBIENT + DIFFUSE * max(0.0, float(np.dot(n, LIGHT)))) + emis * emission_hdr
        out.append((p, lit, sw))
    return out

def raster(shaded, cam, size, scale, center=(0.5, 0.5), ss=4):
    """Ortográfico. scale = px por metro (na resolução final). Retorna RGBA float linear (premultiplicado) e mapa de amostra."""
    right, up, f = cam
    W = size[0] * ss
    H = size[1] * ss
    S = scale * ss
    cx, cy = center[0] * W, center[1] * H
    col = np.zeros((H, W, 3))
    cov = np.zeros((H, W))
    zbuf = np.full((H, W), np.inf)
    sid = np.full((H, W), -1, dtype=np.int16)
    names = ["iron", "fissureWall", "ember", "spare", "sphere"]
    for p, lit, sw in shaded:
        x = cx + (p @ right) * S
        y = cy - (p @ up) * S
        z = p @ f
        x0, x1 = max(int(math.floor(x.min())), 0), min(int(math.ceil(x.max())), W - 1)
        y0, y1 = max(int(math.floor(y.min())), 0), min(int(math.ceil(y.max())), H - 1)
        if x0 > x1 or y0 > y1:
            continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        d = (x[1] - x[0]) * (y[2] - y[0]) - (x[2] - x[0]) * (y[1] - y[0])
        if abs(d) < 1e-12:
            continue
        w1 = ((gx - x[0]) * (y[2] - y[0]) - (x[2] - x[0]) * (gy - y[0])) / d
        w2 = ((x[1] - x[0]) * (gy - y[0]) - (gx - x[0]) * (y[1] - y[0])) / d
        w0 = 1 - w1 - w2
        inside = (w0 >= 0) & (w1 >= 0) & (w2 >= 0)
        zz = w0 * z[0] + w1 * z[1] + w2 * z[2]
        sub = zbuf[y0:y1 + 1, x0:x1 + 1]
        m = inside & (zz < sub)
        sub[m] = zz[m]
        col[y0:y1 + 1, x0:x1 + 1][m] = lit
        cov[y0:y1 + 1, x0:x1 + 1][m] = 1
        sid[y0:y1 + 1, x0:x1 + 1][m] = names.index(sw)
    # downsample
    col = col.reshape(size[1], ss, size[0], ss, 3).mean((1, 3))
    a = cov.reshape(size[1], ss, size[0], ss).mean((1, 3))
    return col, a, sid

def to_image(col_premul, alpha, bg):
    """Composição sobre fundo sRGB (array HxWx3 0-255 ou cor)."""
    h, w = alpha.shape
    if isinstance(bg, tuple):
        bgl = np.broadcast_to(srgb_to_lin(np.array(bg) / 255.0), (h, w, 3))
    else:
        bgl = srgb_to_lin(np.asarray(bg, float) / 255.0)
    # ACES-ish leve: só clamp suave para a emissão HDR não estourar plana
    c = col_premul / (1 + 0.15 * np.maximum(col_premul - 1, 0))
    out = c + bgl * (1 - alpha[..., None])
    return Image.fromarray((lin_to_srgb(out) * 255 + 0.5).astype(np.uint8))

# ———————————————————————— modelos para o render ————————————————————————
def faces_from_fbx(fbx, swatches):
    V = fbx["verts"] * np.array([-1, 1, 1])   # FBX destro → Unity (x espelhado)
    out = []
    for (a, b, d), sw in zip([p for p in fbx["polys"]], swatches):
        p = V[[a, b, d]]
        n = np.cross(p[1] - p[0], p[2] - p[0])
        n = -n / np.linalg.norm(n)   # espelhar X inverte o winding; a normal geométrica troca de sinal
        out.append((p, n, sw))
    return out

def sphere_faces(r=ENVELOPE, sub=3):
    t = (1 + 5 ** 0.5) / 2
    v = [(-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0), (0, -1, t), (0, 1, t), (0, -1, -t), (0, 1, -t), (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1)]
    v = [np.array(p) / np.linalg.norm(p) for p in v]
    f = [(0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11), (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6), (7, 1, 8), (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9), (4, 9, 5), (2, 4, 11), (6, 2, 10), (8, 6, 7), (9, 8, 1)]
    tris = [[v[a], v[b], v[c]] for a, b, c in f]
    for _ in range(sub):
        nt = []
        for a, b, c in tris:
            ab, bc, ca = [(x + y) / np.linalg.norm(x + y) for x, y in ((a, b), (b, c), (c, a))]
            nt += [[a, ab, ca], [b, bc, ab], [c, ca, bc], [ab, bc, ca]]
        tris = nt
    out = []
    for a, b, c in tris:
        p = np.array([a, b, c]) * r
        n = (a + b + c) / np.linalg.norm(a + b + c)   # suave aproximado (esfera Unity é lisa)
        out.append((p, n, "sphere"))
    return out

def render(faces, mat, hdr, cam, size, ppm, heading=0.0, center=(0.5, 0.5), ss=4):
    rot = [(yaw(p, heading), yaw(n[None], heading)[0], sw) for p, n, sw in faces]
    return raster(shade(rot, mat, hdr), cam, size, ppm, center, ss)

# ———————————————————————— pranchas ————————————————————————
def font(sz):
    for name in ("arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, sz)
        except OSError:
            pass
    return ImageFont.load_default()

def label(im, text, xy, sz=15, fill=(235, 235, 235)):
    d = ImageDraw.Draw(im)
    d.text(xy, text, font=font(sz), fill=fill, stroke_width=2, stroke_fill=(10, 10, 12))

BG = (0x24, 0x26, 0x30)

def ortho_sheet(core, mat, hdr):
    views = [("frontal (olhando −Z, frente do voo)", camera([0, 0, -1])),
             ("lateral (olhando −X)", camera([-1, 0, 0])),
             ("topo (olhando −Y)", camera([0, -1, 1e-4])),
             ("câmera do jogo 45° (voo para +Z)", GAME_CAM)]
    tiles = []
    for title, cam in views:
        col, a, _ = render(core, mat, hdr, cam, (360, 360), 760)
        im = to_image(col, a, BG)
        d = ImageDraw.Draw(im)
        r = int(ENVELOPE * 760)
        d.ellipse([180 - r, 180 - r, 180 + r, 180 + r], outline=(120, 124, 140), width=1)
        label(im, title, (10, 8), 15)
        tiles.append(im)
    sheet = Image.new("RGB", (360 * 4, 360 + 34), BG)
    for i, t in enumerate(tiles):
        sheet.paste(t, (i * 360, 0))
    label(sheet, "PREVIEW (rasterizador próprio, não é captura da Unity) · círculo = envelope 0,4 m da esfera atual · ortográfico", (10, 366), 15, (255, 200, 120))
    return sheet

def headings_sheet(core, sphere, mat, hdr):
    hs = [0, 45, 90, 135, 180, 270]
    sheet = Image.new("RGB", (6 * 200, 2 * 200 + 60), BG)
    for i, h in enumerate(hs):
        col, a, _ = render(core, mat, hdr, GAME_CAM, (200, 200), 420, h)
        sheet.paste(to_image(col, a, BG), (i * 200, 0))
        label(sheet, f"rumo {h}°", (i * 200 + 8, 6), 14)
        col, a, _ = render(sphere, mat, hdr, GAME_CAM, (200, 200), 420, h)
        sheet.paste(to_image(col, a, BG), (i * 200, 200))
    label(sheet, "linha 1: núcleo A08 · linha 2: esfera atual (Mat_Projectile, sem emissão) · câmera do jogo 45°, rumo 0° = voo para longe da câmera", (8, 408), 14)
    label(sheet, "PREVIEW — não é captura da Unity", (8, 432), 14, (255, 200, 120))
    return sheet

def measure(core, sphere, mat, hdr):
    rows = []
    _, a_s, _ = render(sphere, mat, hdr, GAME_CAM, (160, 160), 300)
    sphere_area = a_s.sum()
    for h in range(0, 360, 15):
        col, a, sid = render(core, mat, hdr, GAME_CAM, (160, 160), 300, h)
        cov = sid >= 0
        warm = np.isin(sid, [1, 2]).sum() / cov.sum()
        ember = (sid == 2).sum() / cov.sum()
        # cor média vista (sRGB) da silhueta para contraste com o piso
        mean_lin = col.sum((0, 1)) / a.sum()
        rows.append({"heading_deg": h, "area_vs_sphere": round(float(a.sum() / sphere_area), 3), "warm_fraction": round(float(warm), 3), "ember_fraction": round(float(ember), 3),
                     "mean_srgb": [int(v) for v in (lin_to_srgb(mean_lin) * 255 + .5)]})
    # luminância relativa / contraste WCAG da cor quente máxima e média contra o piso
    def lum(rgb):
        l = srgb_to_lin(np.array(rgb) / 255.0)
        return float(0.2126 * l[0] + 0.7152 * l[1] + 0.0722 * l[2])
    def contrast(a, b):
        la, lb = sorted([lum(a), lum(b)], reverse=True)
        return (la + 0.05) / (lb + 0.05)
    # brasa e esfera atual sob luz média (n·L = 0,5) do mesmo modelo de sombreamento
    ember_rgb = [int(v) for v in lin_to_srgb(srgb_to_lin(np.array(hex_rgb("#FF9A2E")) / 255) * (AMBIENT + DIFFUSE * 0.5) + srgb_to_lin(np.array(hex_rgb("#FF9A2E")) / 255) * hdr) * 255 + .5]
    cur_rgb = [int(v) for v in lin_to_srgb(srgb_to_lin(np.array(CUR_COLOR)) * (AMBIENT + DIFFUSE * 0.5)) * 255 + .5]
    contrasts = {}
    for fname, frgb in FLOOR.items():
        contrasts[fname] = {"ember_lit": round(contrast(ember_rgb, frgb), 2), "core_mean_worst": round(min(contrast(r["mean_srgb"], frgb) for r in rows), 2),
                            "current_sphere": round(contrast(cur_rgb, frgb), 2)}
    return rows, contrasts

def f05_preview(core, sphere, mat, hdr):
    if not F05.is_file():
        return None
    base = Image.open(F05).convert("RGB")
    arr = np.asarray(base).astype(float)
    hs = [0, 90, 180, 270]
    spots_core = [(1150 + i * 80, 330) for i in range(4)] + [(600 + i * 80, 420) for i in range(4)]
    spots_sph = [(1150 + i * 80, 430) for i in range(4)]
    def stamp(faces, h, x, y):
        nonlocal arr
        S = 64
        col, a, _ = render(faces, mat, hdr, GAME_CAM, (S, S), F05_PPM, h)
        y0, x0 = y - S // 2, x - S // 2
        bg = arr[y0:y0 + S, x0:x0 + S]
        arr[y0:y0 + S, x0:x0 + S] = np.asarray(to_image(col, a, bg)).astype(float)
    for i, (x, y) in enumerate(spots_core):
        stamp(core, hs[i % 4], x, y)
    for i, (x, y) in enumerate(spots_sph):
        stamp(sphere, hs[i], x, y)
    im = Image.fromarray(arr.astype(np.uint8))
    label(im, "PREVIEW sobre captura real F05 (~75 px/m): núcleo A08 (rumos 0/90/180/270°) acima, esfera atual abaixo", (1000, 250), 16, (255, 200, 120))
    label(im, "núcleo A08 sobre ladrilho escuro", (560, 460), 15, (255, 200, 120))
    crop = im.crop((520, 230, 1520, 500))
    zoom = crop.resize((crop.width * 2, crop.height * 2), Image.NEAREST)
    return im, zoom

def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    source = json.loads((SRC / "FX-PROJECTILE.source.json").read_text(encoding="utf-8"))
    fbx = read_fbx(FBX)
    checks, pieces, swatches, bounds = validate(fbx, source)
    pal = source["palette"]
    mat = {n: (srgb_to_lin(np.array(hex_rgb(sw["base"])) / 255), srgb_to_lin(np.array(hex_rgb(sw["emission"])) / 255)) for n, sw in pal.items()}
    mat["sphere"] = (srgb_to_lin(np.array(CUR_COLOR)), np.zeros(3))
    hdr = float(source["material"]["emissionColorHDR"][0])
    core = faces_from_fbx(fbx, swatches)
    sphere = sphere_faces()

    rows, contrasts = measure(core, sphere, mat, hdr)
    warm_min = min(r["warm_fraction"] for r in rows)
    area_max = max(r["area_vs_sphere"] for r in rows)
    checks.append({"check": "leitura_quente_todos_rumos", "ok": warm_min >= 0.12, "detail": f"fração quente (brasa+parede) da silhueta pela câmera do jogo, 24 rumos: mín {warm_min:.3f} (≥ 0,12), máx {max(r['warm_fraction'] for r in rows):.3f}"})
    checks.append({"check": "silhueta_nao_maior", "ok": area_max <= 1.0, "detail": f"área da silhueta / esfera atual: máx {area_max:.3f} ≤ 1 (não aumenta a leitura de perigo)"})
    worst_mean = min(c["core_mean_worst"] for c in contrasts.values())
    checks.append({"check": "contraste_medio_piso", "ok": worst_mean >= 1.5, "detail": f"WCAG da cor média da silhueta (pior rumo) vs piso: {json.dumps({k: v['core_mean_worst'] for k, v in contrasts.items()})} (≥ 1,5; esfera atual {json.dumps({k: v['current_sphere'] for k, v in contrasts.items()})}); a variante de fenda fina (meia-largura 1,9 cm) dava ~1,0 e foi rejeitada"})
    checks.append({"check": "contraste_brasa_piso", "ok": all(c["ember_lit"] >= 3.0 for c in contrasts.values()), "detail": f"WCAG brasa iluminada vs piso: {json.dumps({k: v['ember_lit'] for k, v in contrasts.items()})} (≥ 3)"})

    ortho_sheet(core, mat, hdr).save(SRC / "preview_ortho.png", optimize=False)
    headings_sheet(core, sphere, mat, hdr).save(SRC / "review_headings_vs_current.png", optimize=False)
    f = f05_preview(core, sphere, mat, hdr)
    if f:
        f[0].save(SRC / "review_floor_preview.png", optimize=False)
        f[1].save(SRC / "review_floor_zoom.png", optimize=False)
    # contact sheet: 45° grande + paletas
    col, a, _ = render(core, mat, hdr, GAME_CAM, (300, 300), 640, 30)
    cs = Image.new("RGB", (300 + 180, 300), BG)
    cs.paste(to_image(col, a, BG), (0, 0))
    for i, file in enumerate((source["material"]["baseMap"], source["material"]["emissionMap"])):
        cs.paste(Image.open(STAGING / file).convert("RGB").resize((96, 96), Image.NEAREST), (320, 30 + i * 130))
        label(cs, "BaseMap" if i == 0 else "EmissionMap", (320, 8 + i * 130), 13)
    label(cs, "PREVIEW", (8, 276), 13, (255, 200, 120))
    cs.save(SRC / "contact_sheet.png", optimize=False)

    ok = all(c["ok"] for c in checks)
    files = [FBX, STAGING / source["material"]["baseMap"], STAGING / source["material"]["emissionMap"], SRC / f"{NAME}.glb"]
    report = {"ok": ok, "asset": "FX-PROJECTILE", "batch": "A08", "validator": "Tools/Lemo3DGenerator/src/production/FxProjectileCore_preview.py",
              "python": sys.version.split()[0], "numpy": np.__version__, "read_from_disk": str(FBX.relative_to(ROOT)).replace("\\", "/"),
              "checks": checks, "bounds_m": bounds, "pieces": pieces, "game_camera": "pitch 45°, yaw 0 (DungeonMvpSceneBuilder.CameraOffset 0,14,−14), ortográfica no preview",
              "per_heading": rows, "contrast_wcag": contrasts,
              "sha256": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in files},
              "not_validated": "Importação na Unity, material URP, Projectile.prefab, pool de 16 tiros, reset e GPU NÃO observados (NÃO VALIDADO NO EDITOR)."}
    (SRC / "FxProjectile.validation.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    for c in checks:
        print(("OK  " if c["ok"] else "FALHA ") + c["check"] + ": " + c["detail"])
    print("validação", "ok" if ok else "FALHOU", f"({sum(c['ok'] for c in checks)}/{len(checks)})")
    return 0 if ok else 1

if __name__ == "__main__":
    sys.exit(main())

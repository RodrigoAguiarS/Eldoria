"""A12 — MODEL-DESTRUCTIBLE: validação independente (relê do disco) + previews.

Rode depois de `node src/production/ModelDestructible.mjs`:
    python -I Tools/Lemo3DGenerator/src/production/ModelDestructible_preview.py

1. Relê os quatro FBX (FBX 7.4 ASCII) e a paleta PNG do disco, sem three.js, e confere por variante: triângulos <= 600,
   só nós Mesh filhos da raiz com nomes da ficha (`<Família>_Intact` único; `<Família>_Broken_Base` + fragmentos),
   sem rig/deformador/pose/câmera/luz/colisor/animação, um único material com o mesmo nome nos quatro arquivos, textura
   existente, unidade e eixos, pivô do prop na base central (origem), pivô de cada fragmento na base central da própria
   peça, peças fechadas/convexas/winding para fora, normais = normal geométrica, UV constante no centro de uma célula,
   paleta PNG = gerador.
2. Footprint: as quatro variantes cabem no vaso F09 de primitivas (±0,45 m em X/Z, 1,04 m) e a quebrada não sai dos
   bounds X/Z/altura da intacta da mesma família.
3. Leitura pela câmera do jogo (pitch 45°, câmera em −Z; prop girado 180° para a frente encarar a câmera) em tiles de
   128 px: IoU das silhuetas entre as quatro variantes (alinhadas no pivô), fração de interior escuro na quebrada e
   contraste da cor média com o piso (ΔE CIELAB e WCAG), para 5 rumos (−30…+30°).
4. Previews rotulados PREVIEW (rasterizador próprio em NumPy, ortográfico — não são captura da Unity): frente/lado/45°,
   prancha 128 px, prévia em escala sobre a captura real do F09 e contact sheet.

Coordenadas: o FBX está em convenção three.js (destro); a Unity espelha X na importação. O preview aplica x → −x.
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
STAGING = ROOT / "Assets/_Project/Art/ProductionStaging/MODEL-DESTRUCTIBLE"
SRC = STAGING / "Source~"
NAME = "MODEL-DESTRUCTIBLE"
F09 = ROOT / "Reports/F09/Visual02/vase-intact.png"
F09_PPM = 111.0               # px/m perto do vaso na captura F09 2560×1440 (vaso de 0,9 m ≈ 100 px)
F09_PIVOT = (1122, 634)        # pivô (base central) estimado do vaso de primitivas na captura
BUDGET = 600
FLOOR = {"#3E4152": (0x3E, 0x41, 0x52), "#55596B": (0x55, 0x59, 0x6B)}
FAMILY = {"jar": "Jar", "crate": "Crate"}

def srgb_to_lin(c):
    c = np.asarray(c, dtype=np.float64)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)

def lin_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)

def hex_rgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))

def lab(rgb):
    l = srgb_to_lin(np.array(rgb) / 255.0)
    M = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    xyz = M @ l / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > (6 / 29) ** 3, np.cbrt(xyz), xyz / (3 * (6 / 29) ** 2) + 4 / 29)
    return np.array([116 * f[1] - 16, 500 * (f[0] - f[1]), 200 * (f[1] - f[2])])

def lum(rgb):
    l = srgb_to_lin(np.array(rgb) / 255.0)
    return float(0.2126 * l[0] + 0.7152 * l[1] + 0.0722 * l[2])

def wcag(a, b):
    la, lb = sorted([lum(a), lum(b)], reverse=True)
    return (la + 0.05) / (lb + 0.05)

# ———————————————————————— leitura do FBX ASCII ————————————————————————
def fbx_array(text, name, required=True):
    m = re.search(rf"{name}: \*(\d+) \{{\s*a: ([^}}]*)\}}", text)
    if not m:
        if required:
            raise ValueError(f"array {name} ausente")
        return None
    vals = [float(v) for v in m.group(2).replace("\n", "").split(",")]
    if len(vals) != int(m.group(1)):
        raise ValueError(f"{name}: contagem declarada {m.group(1)} != {len(vals)}")
    return vals

def read_fbx(path: Path):
    text = path.read_text(encoding="utf-8")
    objs_txt = text[text.index("Objects:  {"):text.index("Connections:  {")]
    heads = list(re.finditer(r'^\t(\w+): (\d+), "(\w+)::([^"]*)", "([^"]*)" \{', objs_txt, flags=re.M))
    objects = {}
    for i, h in enumerate(heads):
        body = objs_txt[h.end():heads[i + 1].start() if i + 1 < len(heads) else len(objs_txt)]
        objects[int(h.group(2))] = {"type": h.group(1), "name": h.group(4), "sub": h.group(5), "body": body}
    conns = [(m.group(1), int(m.group(2)), int(m.group(3)), m.group(4)) for m in re.finditer(r'C: "(OO|OP)",(\d+),(\d+)(?:, "([^"]+)")?', text)]
    settings = dict(re.findall(r'P: "(\w+)", "(?:int|double|enum)", "(?:Integer|Number|)", "",(-?[\d.]+)', text))
    models = {}
    for oid, o in objects.items():
        if o["type"] != "Model":
            continue
        tr = re.search(r'"Lcl Translation", "Lcl Translation", "", "A",([^\n]+)', o["body"])
        ro = re.search(r'"Lcl Rotation", "Lcl Rotation", "", "A",([^\n]+)', o["body"])
        sc = re.search(r'"Lcl Scaling", "Lcl Scaling", "", "A",([^\n]+)', o["body"])
        geo_id = next(c for k, c, p, _ in conns if k == "OO" and p == oid and objects.get(c, {}).get("type") == "Geometry")
        mats = [objects[c]["name"] for k, c, p, _ in conns if k == "OO" and p == oid and objects.get(c, {}).get("type") == "Material"]
        parent = next(p for k, c, p, _ in conns if c == oid and k == "OO")
        g = objects[geo_id]["body"]
        pvi = [int(v) for v in fbx_array(g, "PolygonVertexIndex")]
        polys, cur = [], []
        for v in pvi:
            if v < 0:
                cur.append(-v - 1); polys.append(cur); cur = []
            else:
                cur.append(v)
        models[o["name"]] = {"id": oid, "sub": o["sub"], "parent": parent, "translation": [float(v) for v in tr.group(1).split(",")],
                             "rotation": [float(v) for v in ro.group(1).split(",")], "scaling": [float(v) for v in sc.group(1).split(",")], "materials": mats,
                             "verts": np.array(fbx_array(g, "Vertices")).reshape(-1, 3), "polys": polys,
                             "normals": np.array(fbx_array(g, "Normals")).reshape(-1, 3), "uv": np.array(fbx_array(g, "UV")).reshape(-1, 2),
                             "uvi": [int(v) for v in fbx_array(g, "UVIndex")], "matidx": [int(v) for v in fbx_array(g, "Materials")],
                             "mat_mapping": re.search(r'LayerElementMaterial: 0 \{.*?MappingInformationType: "(\w+)"', g, flags=re.S).group(1)}
    textures = [o for o in objects.values() if o["type"] == "Texture"]
    tex_files = [re.search(r'RelativeFilename: "([^"]+)"', o["body"]).group(1) for o in textures]
    tex_conn = [(objects[p]["name"], prop) for k, c, p, prop in conns if k == "OP" and objects.get(c, {}).get("type") == "Texture"]
    return {"text": text, "objects": objects, "models": models, "settings": settings, "tex_files": tex_files, "tex_conn": tex_conn,
            "has_takes": "Takes:" in text}

# ———————————————————————— geometria auxiliar ————————————————————————
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

def piece_stats(V, tri):
    vids = sorted({v for t in tri for v in t})
    remap = {v: i for i, v in enumerate(vids)}
    P = V[vids]
    T = np.array([[remap[v] for v in t] for t in tri])
    edges = {}
    for a, b, d in T:
        for e in ((a, b), (b, d), (d, a)):
            k = (min(e), max(e)); edges[k] = edges.get(k, 0) + 1
    closed = all(n == 2 for n in edges.values())
    vol = float(np.sum(np.einsum("ij,ij->i", P[T[:, 0]], np.cross(P[T[:, 1]], P[T[:, 2]]))) / 6)
    n = np.cross(P[T[:, 1]] - P[T[:, 0]], P[T[:, 2]] - P[T[:, 0]])
    n /= np.linalg.norm(n, axis=1, keepdims=True)
    d = np.einsum("ij,ij->i", n, P[T[:, 0]])
    convex = bool(((P @ n.T) - d[None, :] <= 1e-5).all())
    return closed, vol, convex

# ———————————————————————— validação ————————————————————————
def validate(files, source):
    checks = []
    def check(name, ok, detail):
        checks.append({"check": name, "ok": bool(ok), "detail": detail})

    pal = source["palette"]
    mat_name = source["material"]["name"]
    centers = {name: ((sw["cell"][0] * 4 + 2) / 16, 1 - (sw["cell"][1] * 4 + 2) / 16) for name, sw in pal.items()}
    swatches, bounds, rows = {}, {}, []
    tri_rows, obj_ok, name_rows, mat_rows, tex_ok_all, unit_ok_all = [], True, [], [], True, True
    closed_all = vol_all = convex_all = True
    worst_n, uv_ok = 1.0, True
    counts = {n: 0 for n in pal}
    pivot_rows, pivot_ok = [], True
    allowed = {"Model", "Geometry", "Material", "Texture", "Video"}
    for fname, (meta, fbx) in files.items():
        M = fbx["models"]
        fam = FAMILY[meta["family"]]
        n_tri = sum(len(m["polys"]) for m in M.values())
        all_tri = all(len(p) == 3 for m in M.values() for p in m["polys"])
        tri_rows.append(f"{fname.replace(NAME + '_', '')} {n_tri}{'' if all_tri else ' (não-tri!)'}")
        if not (all_tri and n_tri <= BUDGET):
            tri_rows[-1] += " > orçamento"
        kinds = {o["type"] for o in fbx["objects"].values()}
        names = sorted(M)
        if meta["state"] == "intact":
            names_ok = names == [f"{fam}_Intact"]
        else:
            names_ok = f"{fam}_Broken_Base" in names and all(n.startswith(f"{fam}_Broken_") for n in names) and len(names) >= 2
        obj_ok &= kinds <= allowed and all(m["sub"] == "Mesh" and m["parent"] == 0 for m in M.values()) and not fbx["has_takes"] and names_ok
        name_rows.append(f"{fname.replace(NAME + '_', '')}: {names}")
        mats = [o["name"] for o in fbx["objects"].values() if o["type"] == "Material"]
        mat_rows.append(mats == [mat_name] and all(m["materials"] == [mat_name] and m["mat_mapping"] == "AllSame" and set(m["matidx"]) == {0} for m in M.values()))
        tex_ok_all &= all((STAGING / t).is_file() for t in fbx["tex_files"]) and fbx["tex_conn"] == [(mat_name, "DiffuseColor")]
        s = fbx["settings"]
        unit_ok_all &= s.get("UnitScaleFactor") == "100" and s.get("UpAxis") == "1" and s.get("FrontAxis") == "2" and s.get("FrontAxisSign") == "1"
        # peças, normais, UV
        sw_file = {}
        allv = []
        for mname, m in M.items():
            V, tri = m["verts"], m["polys"]
            for ci, c in enumerate(components(tri)):
                closed, vol, convex = piece_stats(V, [tri[i] for i in c])
                closed_all &= closed; vol_all &= vol > 0; convex_all &= convex
                rows.append({"file": fname, "node": mname, "piece": ci, "triangles": len(c), "closed": closed, "convex": convex, "signed_volume_m3": round(vol, 7)})
            N = m["normals"]
            for i, (a, b, d) in enumerate(tri):
                g = np.cross(V[b] - V[a], V[d] - V[a]); g /= np.linalg.norm(g)
                for k in range(3):
                    worst_n = min(worst_n, float(np.dot(N[i * 3 + k] / np.linalg.norm(N[i * 3 + k]), g)))
            sw_list = []
            for i in range(len(tri)):
                u = {tuple(np.round(m["uv"][m["uvi"][i * 3 + k]], 4)) for k in range(3)}
                nm = None
                if len(u) == 1:
                    uu = next(iter(u))
                    nm = next((n for n, cc in centers.items() if abs(cc[0] - uu[0]) < 1e-3 and abs(cc[1] - uu[1]) < 1e-3), None)
                uv_ok &= nm is not None
                sw_list.append(nm)
                if nm:
                    counts[nm] += 1
            sw_file[mname] = sw_list
            W = V + np.array(m["translation"])
            allv.append(W)
            # pivôs: intacto/base na origem; fragmento na base central da própria peça
            lo, hi = W.min(0), W.max(0)
            want = np.zeros(3) if (mname.endswith("_Intact") or mname.endswith("_Base")) else np.array([(lo[0] + hi[0]) / 2, lo[1], (lo[2] + hi[2]) / 2])
            ok = np.allclose(m["translation"], want, atol=2e-4) and m["rotation"] == [0, 0, 0] and m["scaling"] == [1, 1, 1]
            pivot_ok &= ok
            pivot_rows.append(f"{mname} @ {np.round(m['translation'], 3).tolist()}")
        swatches[fname] = sw_file
        A = np.vstack(allv)
        bmin, bmax = A.min(0), A.max(0)
        bounds[fname] = {"min": np.round(bmin, 4).tolist(), "max": np.round(bmax, 4).tolist(), "size": np.round(bmax - bmin, 4).tolist(), "center": np.round((bmin + bmax) / 2, 4).tolist(), "triangles": n_tri}

    check("triangulos", all("orçamento" not in r and "não-tri" not in r for r in tri_rows), f"por variante: {', '.join(tri_rows)} (orçamento ≤ {BUDGET} por prop, todos triângulos)")
    check("unidade_eixos", unit_ok_all, "UnitScaleFactor 100 (cm×100 = m), UpAxis 1 (+Y), FrontAxis 2/+1 (+Z) nos quatro arquivos")
    check("objetos_nomes", obj_ok, "só Model/Geometry/Material/Texture/Video; nós Mesh filhos da raiz; sem Takes/AnimationStack, Deformer, Pose ou NodeAttribute (sem rig, câmera, luz, colisor, animação); nomes: " + "; ".join(name_rows))
    check("material_unico", all(mat_rows), f"um único material \"{mat_name}\" em cada FBX (mesmo nome nos quatro), ligado a todos os nós, índice AllSame = 0")
    check("textura", tex_ok_all, f"{source['material']['baseMap']} ao lado dos FBX, DiffuseColor de {mat_name}")
    check("pecas_fechadas_convexas", closed_all and convex_all, f"{len(rows)} peças; todas fechadas: {closed_all}; todas convexas: {convex_all}")
    check("winding_para_fora", vol_all, "volume com sinal > 0 em todas as peças")
    check("normais", worst_n > 0.999, f"menor cos(normal FBX, normal geométrica) = {worst_n:.5f} (flat shading, para fora)")
    check("uv_paleta", uv_ok and all(c > 0 for c in counts.values()), f"cada face com UV constante no centro de uma célula; faces por amostra (4 arquivos) {counts}")
    im = np.asarray(Image.open(STAGING / source["material"]["baseMap"]).convert("RGBA"))
    pal_ok = im.shape == (16, 16, 4) and (im[..., 3] == 255).all()
    for name, sw in pal.items():
        cx, cy = sw["cell"]
        pal_ok &= bool((im[cy * 4:cy * 4 + 4, cx * 4:cx * 4 + 4, :3].reshape(-1, 3) == np.array(hex_rgb(sw["hex"]))).all())
    check("paleta_png", pal_ok, f"{source['material']['baseMap']} {im.shape[1]}×{im.shape[0]}, {len(pal)} células = paleta do gerador")
    check("pivos", pivot_ok, "intacto e *_Broken_Base na origem (base central do prop), fragmentos na base central da própria peça, sem rotação/escala: " + ", ".join(pivot_rows))

    fp = source["footprint_f09"]
    fp_rows, fp_ok = [], True
    for f, b in bounds.items():
        ok = max(abs(b["min"][0]), abs(b["max"][0])) <= fp["halfX"] and max(abs(b["min"][2]), abs(b["max"][2])) <= fp["halfZ"] and b["max"][1] <= fp["height"] and abs(b["min"][1]) < 1e-4
        fp_ok &= ok
        fp_rows.append(f"{f.replace(NAME + '_', '').replace('.fbx', '')} {b['size'][0]:.3f}×{b['size'][1]:.3f}×{b['size'][2]:.3f} m")
    check("footprint_f09", fp_ok, f"todas apoiam em y 0 e cabem no vaso F09 (±{fp['halfX']} m em X/Z, {fp['height']} m de altura): " + "; ".join(fp_rows) + " (L × A × P)")
    st_rows, st_ok = [], True
    for fam in ("jar", "crate"):
        fi = next(f for f, (m, _) in files.items() if m["family"] == fam and m["state"] == "intact")
        fb = next(f for f, (m, _) in files.items() if m["family"] == fam and m["state"] == "broken")
        I, B = bounds[fi], bounds[fb]
        ok = all(B["min"][i] >= I["min"][i] - 1e-4 and B["max"][i] <= I["max"][i] + 1e-4 for i in (0, 2)) and B["max"][1] <= I["max"][1] and abs(I["center"][0]) < 0.01 and abs(I["center"][2]) < 0.01
        st_ok &= ok
        st_rows.append(f"{fam}: quebrado X [{B['min'][0]:.3f}, {B['max'][0]:.3f}] Z [{B['min'][2]:.3f}, {B['max'][2]:.3f}] A {B['max'][1]:.3f} ⊆ intacto X [{I['min'][0]:.3f}, {I['max'][0]:.3f}] Z [{I['min'][2]:.3f}, {I['max'][2]:.3f}] A {I['max'][1]:.3f}")
    check("bounds_estaveis", st_ok, "mesmo pivô; a quebrada não aumenta o footprint nem a altura da intacta: " + "; ".join(st_rows))
    return checks, swatches, rows, bounds

# ———————————————————————— rasterizador (mesmo modelo do A08/A11) ————————————————————————
LIGHT = np.array([-0.35, 0.85, -0.4]); LIGHT /= np.linalg.norm(LIGHT)
AMBIENT, DIFFUSE = 0.32, 0.78

def camera(forward):
    f = np.asarray(forward, float); f /= np.linalg.norm(f)
    right = np.cross([0, 1, 0], f); right /= np.linalg.norm(right)
    return right, np.cross(f, right), f

GAME_CAM = camera([0, -math.sin(math.radians(45)), math.cos(math.radians(45))])

def yaw_m(deg):
    a = math.radians(deg); c, s = math.cos(a), math.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])   # Unity: rotação Y positiva leva +Z para +X

def assemble(fbx, sw, heading=0.0, offset=(0, 0, 0)):
    out = []
    Y = yaw_m(heading)
    for mname, m in fbx["models"].items():
        V = m["verts"] + np.array(m["translation"])
        V = (V * np.array([-1, 1, 1])) @ Y.T + np.array(offset)
        for (a, b, d), s in zip(m["polys"], sw[mname]):
            p = V[[a, b, d]]
            n = np.cross(p[1] - p[0], p[2] - p[0]); n = -n / np.linalg.norm(n)   # espelho em X inverte o winding
            out.append((p, n, s))
    return out

def raster(faces, mat, ids, cam, size, scale, center=(0.5, 0.5), ss=4, origin=np.zeros(3)):
    right, up, f = cam
    W, H, S = size[0] * ss, size[1] * ss, scale * ss
    cx, cy = center[0] * W, center[1] * H
    col = np.zeros((H, W, 3)); cov = np.zeros((H, W)); zbuf = np.full((H, W), np.inf); sid = np.full((H, W), -1, dtype=np.int16)
    for p, n, sw in faces:
        lit = mat[sw] * (AMBIENT + DIFFUSE * max(0.0, float(np.dot(n, LIGHT))))
        q = p - origin
        x = cx + (q @ right) * S; y = cy - (q @ up) * S; z = q @ f
        x0, x1 = max(int(math.floor(x.min())), 0), min(int(math.ceil(x.max())), W - 1)
        y0, y1 = max(int(math.floor(y.min())), 0), min(int(math.ceil(y.max())), H - 1)
        if x0 > x1 or y0 > y1:
            continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        dd = (x[1] - x[0]) * (y[2] - y[0]) - (x[2] - x[0]) * (y[1] - y[0])
        if abs(dd) < 1e-12:
            continue
        w1 = ((gx - x[0]) * (y[2] - y[0]) - (x[2] - x[0]) * (gy - y[0])) / dd
        w2 = ((x[1] - x[0]) * (gy - y[0]) - (gx - x[0]) * (y[1] - y[0])) / dd
        w0 = 1 - w1 - w2
        inside = (w0 >= 0) & (w1 >= 0) & (w2 >= 0)
        zz = w0 * z[0] + w1 * z[1] + w2 * z[2]
        sub = zbuf[y0:y1 + 1, x0:x1 + 1]
        mm = inside & (zz < sub)
        sub[mm] = zz[mm]
        col[y0:y1 + 1, x0:x1 + 1][mm] = lit
        cov[y0:y1 + 1, x0:x1 + 1][mm] = 1
        sid[y0:y1 + 1, x0:x1 + 1][mm] = ids.index(sw)
    col = col.reshape(size[1], ss, size[0], ss, 3).mean((1, 3))
    a = cov.reshape(size[1], ss, size[0], ss).mean((1, 3))
    return col, a, sid

def to_image(col_premul, alpha, bg):
    h, w = alpha.shape
    bgl = np.broadcast_to(srgb_to_lin(np.array(bg) / 255.0), (h, w, 3)) if isinstance(bg, tuple) else srgb_to_lin(np.asarray(bg, float) / 255.0)
    return Image.fromarray((lin_to_srgb(col_premul + bgl * (1 - alpha[..., None])) * 255 + 0.5).astype(np.uint8))

def font(sz):
    for name in ("arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, sz)
        except OSError:
            pass
    return ImageFont.load_default()

def label(im, text, xy, sz=15, fill=(235, 235, 235)):
    ImageDraw.Draw(im).text(xy, text, font=font(sz), fill=fill, stroke_width=2, stroke_fill=(10, 10, 12))

BG = (0x24, 0x26, 0x30)
FLOOR_BG = FLOOR["#55596B"]
WARN = (255, 200, 120)
TILE128_PPM = 120.0            # px/m no tile de 128 px (a caixa intacta, a maior projeção a 45°, ocupa ~114 px de altura)

def short(f):
    return f.replace(NAME + "_", "").replace(".fbx", "")

def ortho_sheet(files, sw, mat, ids):
    views = [("frente (olhando −Z)", camera([0, 0, -1]), 0), ("lado (olhando −X)", camera([-1, 0, 0]), 0),
             ("topo (olhando −Y)", camera([0, -1, 1e-4]), 0), ("câmera do jogo 45° (frente p/ câmera)", GAME_CAM, 180)]
    T = 300
    sheet = Image.new("RGB", (T * len(views), T * len(files) + 40), BG)
    for row, (f, (meta, fbx)) in enumerate(files.items()):
        for i, (title, cam, hd) in enumerate(views):
            col, a, _ = raster(assemble(fbx, sw[f], hd), mat, ids, cam, (T, T), 300, center=(0.5, 0.6), origin=np.array([0, 0.32, 0]))
            im = to_image(col, a, BG)
            label(im, f"{short(f)} · {title}", (8, 6), 13)
            sheet.paste(im, (i * T, row * T))
    label(sheet, "PREVIEW (rasterizador próprio, ortográfico; não é captura da Unity) · pivô na base central · +Z frente", (8, T * len(files) + 10), 15, WARN)
    return sheet

def tile128(fbx, sw, mat, ids, heading=180):
    return raster(assemble(fbx, sw, heading), mat, ids, GAME_CAM, (128, 128), TILE128_PPM, center=(0.5, 0.72), origin=np.zeros(3), ss=4)

def measure(files, sw, mat, ids):
    """Silhuetas 128 px pela câmera do jogo, alinhadas no pivô, para 5 rumos; IoU par a par e leitura de cor."""
    names = list(files)
    pairs = [(a, b) for i, a in enumerate(names) for b in names[i + 1:]]
    per_heading, iou_max = [], {f"{short(a)} × {short(b)}": 0.0 for a, b in pairs}
    diff_min = {f"{short(a)} × {short(b)}": 1.0 for a, b in pairs}
    area_ratio_max = {f"{short(a)} × {short(b)}": 0.0 for a, b in pairs}
    color = {short(f): {"interior_frac_min": 1.0, "mean_srgb": None} for f in names}
    for off in (-30, -15, 0, 15, 30):
        tiles = {f: tile128(files[f][1], sw[f], mat, ids, 180 + off) for f in names}
        masks = {f: t[1] >= 0.5 for f, t in tiles.items()}
        row = {"heading_offset_deg": off, "area_px": {short(f): int(m.sum()) for f, m in masks.items()}, "iou": {}}
        row["appearance_diff"], row["area_ratio"] = {}, {}
        for a, b in pairs:
            iou = float((masks[a] & masks[b]).sum() / (masks[a] | masks[b]).sum())
            k = f"{short(a)} × {short(b)}"
            row["iou"][k] = round(iou, 3)
            iou_max[k] = max(iou_max[k], round(iou, 3))
            # diferença de aparência: fração da união (supersample) onde a amostra da paleta difere ou só um cobre
            sa, sb = tiles[a][2], tiles[b][2]
            uni = (sa >= 0) | (sb >= 0)
            d = float(((sa != sb) & uni).sum() / uni.sum())
            row["appearance_diff"][k] = round(d, 3)
            diff_min[k] = min(diff_min[k], round(d, 3))
            ar = float(min(masks[a].sum(), masks[b].sum()) / max(masks[a].sum(), masks[b].sum()))
            row["area_ratio"][k] = round(ar, 3)
            area_ratio_max[k] = max(area_ratio_max[k], round(ar, 3))
        for f, (col, a, sid) in tiles.items():
            m = sid >= 0
            frac = float(np.isin(sid, [ids.index("interior")]).sum() / max(1, m.sum()))   # sid é 4× maior (supersample)
            color[short(f)]["interior_frac_min"] = min(color[short(f)]["interior_frac_min"], round(frac, 4))
            if off == 0:
                color[short(f)]["mean_srgb"] = [int(v) for v in (lin_to_srgb(col.sum((0, 1)) / a.sum()) * 255 + .5)]
        per_heading.append(row)
    for f, c in color.items():
        c["vs_floor"] = {k: {"deltaE76": round(float(np.linalg.norm(lab(c["mean_srgb"]) - lab(rgb))), 1), "wcag": round(wcag(c["mean_srgb"], rgb), 2)} for k, rgb in FLOOR.items()}
    return per_heading, iou_max, color, diff_min, area_ratio_max

def board128(files, sw, mat, ids):
    n = len(files)
    W = 128 * n + 16 * (n + 1)
    sheet = Image.new("RGB", (W, 128 * 2 + 16 * 3 + 64), BG)
    for i, (f, (meta, fbx)) in enumerate(files.items()):
        col, a, _ = tile128(fbx, sw[f], mat, ids)
        x = 16 + i * (128 + 16)
        sheet.paste(to_image(col, a, FLOOR_BG), (x, 16))
        sil = Image.fromarray(np.where(a >= 0.5, 20, 235).astype(np.uint8)).convert("RGB")
        sheet.paste(sil, (x, 16 * 2 + 128))
        label(sheet, short(f), (x, 16 * 3 + 256), 13)
    label(sheet, "PREVIEW 128 px reais (sem ampliação) · câmera do jogo 45°, frente p/ câmera · cima: cor sobre piso #55596B · baixo: silhueta", (16, 16 * 3 + 256 + 24), 13, WARN)
    return sheet

def f09_preview(files, sw, mat, ids):
    if not F09.is_file():
        return None
    arr = np.asarray(Image.open(F09).convert("RGB")).astype(float)
    S = 220
    spots = {"Jar_Intact": (1480, 800), "Jar_Broken": (1700, 800), "Crate_Intact": (1480, 990), "Crate_Broken": (1700, 990)}
    for f, (meta, fbx) in files.items():
        px, py = spots[short(f)]
        col, a, _ = raster(assemble(fbx, sw[f], 180), mat, ids, GAME_CAM, (S, S), F09_PPM)   # pivô no centro do tile
        y0, x0 = py - S // 2, px - S // 2
        bg = arr[y0:y0 + S, x0:x0 + S]
        arr[y0:y0 + S, x0:x0 + S] = np.asarray(to_image(col, a, bg)).astype(float)
    im = Image.fromarray(arr.astype(np.uint8))
    for f in files:
        px, py = spots[short(f)]
        label(im, short(f), (px - 60, py + 30), 18, WARN)
    label(im, "vaso F09 atual (primitivas)", (F09_PIVOT[0] - 130, F09_PIVOT[1] + 30), 18, WARN)
    label(im, f"PREVIEW em escala (~{F09_PPM:.0f} px/m, ortográfico) sobre a captura real F09 2560×1440 (vase-intact.png); não é captura da Unity", (40, 1380), 22, WARN)
    return im.crop((960, 470, 1900, 1100))

def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    source = json.loads((SRC / f"{NAME}.source.json").read_text(encoding="utf-8"))
    files = {f: (meta, read_fbx(STAGING / f)) for f, meta in source["variants"].items()}
    checks, sw, piece_rows, bounds = validate(files, source)

    pal = source["palette"]
    ids = list(pal)
    mat = {n: srgb_to_lin(np.array(hex_rgb(s["hex"])) / 255) for n, s in pal.items()}
    per_heading, iou_max, color, diff_min, area_ratio_max = measure(files, sw, mat, ids)
    fam_pairs = {k: v for k, v in iou_max.items() if ("Jar_Intact" in k and "Jar_Broken" in k) or ("Crate_Intact" in k and "Crate_Broken" in k)}
    cross_pairs = {k: v for k, v in iou_max.items() if k not in fam_pairs}
    fam_diff = {k: diff_min[k] for k in fam_pairs}
    fam_area = {k: area_ratio_max[k] for k in fam_pairs}
    checks.append({"check": "formas_distintas_128px", "ok": max(iou_max.values()) <= 0.8 and max(fam_area.values()) <= 0.85 and min(fam_diff.values()) >= 0.4,
                   "detail": f"tiles 128 px, câmera do jogo, rumos −30…+30°, silhuetas alinhadas no pivô. Todas as duplas: IoU máx ≤ 0,8 — entre famílias/estados {json.dumps(cross_pairs, ensure_ascii=False)}, "
                             f"intacto×quebrado {json.dumps(fam_pairs, ensure_ascii=False)}. Intacto×quebrado divide o footprint por exigência da ficha (bounds estáveis), então a silhueta sozinha não basta: "
                             f"razão de área máx {json.dumps(fam_area, ensure_ascii=False)} (≤ 0,85: o quebrado é mais baixo) e diferença de aparência mín {json.dumps(fam_diff, ensure_ascii=False)} "
                             f"(≥ 0,4: fração da união em que a amostra da paleta muda — interior escuro, madeira partida, cacos)"})
    broken_int = {k: v["interior_frac_min"] for k, v in color.items() if "Broken" in k}
    intact_int = {k: v["interior_frac_min"] for k, v in color.items() if "Intact" in k}
    checks.append({"check": "quebrado_le_aberto", "ok": all(v >= 0.03 for v in broken_int.values()),
                   "detail": f"fração mínima de interior escuro visível nos quebrados {broken_int} (≥ 0,03; boca/corte/fundo) vs. intactos {intact_int}"})
    de = {k: min(x["deltaE76"] for x in v["vs_floor"].values()) for k, v in color.items()}
    checks.append({"check": "contraste_piso", "ok": all(v >= 15 for v in de.values()),
                   "detail": f"ΔE76 mínimo da cor média (rumo 0) contra os pisos #55596B/#3E4152: {de} (≥ 15; barro/madeira quentes sobre pedra fria); "
                             f"WCAG informativo {json.dumps({k: {f: x['wcag'] for f, x in v['vs_floor'].items()} for k, v in color.items()})}"})

    SRC.mkdir(exist_ok=True)
    ortho_sheet(files, sw, mat, ids).save(SRC / "preview_ortho.png", optimize=False)
    board128(files, sw, mat, ids).save(SRC / "preview_128.png", optimize=False)
    f09 = f09_preview(files, sw, mat, ids)
    if f09:
        f09.save(SRC / "review_f09_scale.png", optimize=False)
    cs = Image.new("RGB", (4 * 260 + 150, 330), BG)
    for i, (f, (meta, fbx)) in enumerate(files.items()):
        col, a, _ = raster(assemble(fbx, sw[f], 180 + 20), mat, ids, GAME_CAM, (260, 300), 280, center=(0.5, 0.72), origin=np.zeros(3))
        cs.paste(to_image(col, a, FLOOR_BG), (i * 260, 0))
        label(cs, short(f), (i * 260 + 6, 4), 13)
    cs.paste(Image.open(STAGING / source["material"]["baseMap"]).convert("RGB").resize((112, 112), Image.NEAREST), (4 * 260 + 20, 40))
    label(cs, "Palette 16×16", (4 * 260 + 20, 16), 13)
    label(cs, "PREVIEW · 45° · fundo = piso #55596B", (8, 306), 13, WARN)
    cs.save(SRC / "contact_sheet.png", optimize=False)

    ok = all(c["ok"] for c in checks)
    hashed = [STAGING / f for f in files] + [STAGING / f"{f}.meta" for f in files] + [STAGING / source["material"]["baseMap"], SRC / f"{NAME}.glb"]
    report = {"ok": ok, "asset": NAME, "batch": "A12", "validator": "Tools/Lemo3DGenerator/src/production/ModelDestructible_preview.py",
              "python": sys.version.split()[0], "numpy": np.__version__, "read_from_disk": [str((STAGING / f).relative_to(ROOT)).replace("\\", "/") for f in files],
              "checks": checks, "bounds_m": bounds, "pieces": piece_rows,
              "game_camera": "pitch 45°, câmera em −Z (DungeonMvpSceneBuilder.CameraOffset 0,14,−14), ortográfica no preview; prop girado 180° (frente +Z para a câmera)",
              "readability_128px": {"ppm": TILE128_PPM, "per_heading": per_heading, "iou_max": iou_max, "appearance_diff_min": diff_min, "area_ratio_max": area_ratio_max, "color": color},
              "sha256": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in hashed},
              "not_validated": "Importação na Unity (ModelImporter estático, material pela MaterialDescription, remapeamento para material único), URP, cena F09, presenter, DamageFlash e PlayMode NÃO observados (NÃO VALIDADO NO EDITOR)."}
    (SRC / "ModelDestructible.validation.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    for c in checks:
        print(("OK  " if c["ok"] else "FALHA ") + c["check"] + ": " + c["detail"])
    print("validação", "ok" if ok else "FALHOU", f"({sum(c['ok'] for c in checks)}/{len(checks)})")
    return 0 if ok else 1

if __name__ == "__main__":
    sys.exit(main())

"""A11 — MODEL-CHEST: validação independente (relê do disco) + previews.

Rode depois de `node src/production/ModelChest.mjs`:
    python -I Tools/Lemo3DGenerator/src/production/ModelChest_preview.py

1. Relê MODEL-CHEST.fbx (FBX 7.4 ASCII) e a paleta PNG do disco, sem three.js, e confere: triângulos <= 1200,
   2 materiais (pedra/latão) ligados aos dois nós na ordem dos índices por polígono, textura existente, só nós Mesh "Base"
   e "Lid" no topo (sem rig/deformador/pose/câmera/luz/colisor), unidade e eixos, pivô da base (origem na base central),
   pivô da tampa no eixo da dobradiça (fundo da tampa em y local 0, toda a pedra/latão da tampa à frente do eixo, nós
   coaxiais), dimensões ~1,2 × 0,8 × 0,7 m, cada peça fechada/convexa/winding para fora, normais = normal geométrica,
   UV constante no centro de uma célula da paleta, paleta PNG = gerador, take "Open" (0,6 s, 30 fps, só Lcl Rotation X
   do Lid, 0° → −105°, base sem curvas = in-place).
2. Interpenetração tampa × base ao longo do take: SAT entre todas as peças convexas (normais de face + produtos de
   arestas), amostrado a 120 Hz incluindo as chaves, tolerância 0,5 mm.
3. Leitura pela câmera do jogo (pitch 45°, câmera em −Z; o baú gira 180° para a frente encarar a câmera, como na
   integração proposta): IoU das silhuetas fechado × aberto, fração de interior escuro, fração de latão e contraste com o
   piso, para 5 rumos (−30…+30°).
4. Previews rotulados PREVIEW (rasterizador próprio em NumPy, ortográfico — não são captura da Unity): ortográficos
   frente/lado/topo/45° fechado e aberto, tira do take, GIF do take, prévia em escala sobre a captura real F08.

Coordenadas: o FBX está em convenção three.js (destro); a Unity espelha X na importação. O preview aplica x → −x.
O modelo é simétrico em X. Rotação X mantém o sinal no espelho (−105° ergue a frente nos dois espaços).
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
STAGING = ROOT / "Assets/_Project/Art/ProductionStaging/MODEL-CHEST"
SRC = STAGING / "Source~"
NAME = "MODEL-CHEST"
FBX = STAGING / f"{NAME}.fbx"
F08 = ROOT / "Reports/F08/Visual/chest-locked.png"
F08_PPM = 112.0               # px/m perto do baú na captura F08 2560×1440 (tampa de 1,48 m ≈ 166 px)
F08_PIVOT = (1280, 578)        # pivô (base central) do baú de primitivas na captura
BUDGET = 1200
TOL = 0.0005                   # m, interpenetração tolerada (contato)
KTIME = 46186158000
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
                             "rotation": [float(v) for v in ro.group(1).split(",")], "materials": mats,
                             "verts": np.array(fbx_array(g, "Vertices")).reshape(-1, 3), "polys": polys,
                             "normals": np.array(fbx_array(g, "Normals")).reshape(-1, 3), "uv": np.array(fbx_array(g, "UV")).reshape(-1, 2),
                             "uvi": [int(v) for v in fbx_array(g, "UVIndex")], "matidx": [int(v) for v in fbx_array(g, "Materials")],
                             "mat_mapping": re.search(r'LayerElementMaterial: 0 \{.*?MappingInformationType: "(\w+)"', g, flags=re.S).group(1)}
    # animação
    stacks = [(oid, o) for oid, o in objects.items() if o["type"] == "AnimationStack"]
    curve_nodes = []
    for oid, o in objects.items():
        if o["type"] != "AnimationCurveNode":
            continue
        target = [(p, prop) for k, c, p, prop in conns if c == oid and k == "OP"]
        curves = {}
        for k, c, p, prop in conns:
            if p == oid and k == "OP" and objects[c]["type"] == "AnimationCurve":
                b = objects[c]["body"]
                curves[prop] = {"t": [int(v) for v in fbx_array(b, "KeyTime")], "v": fbx_array(b, "KeyValueFloat")}
        curve_nodes.append({"name": o["name"], "targets": [(objects[p]["name"], prop) for p, prop in target], "curves": curves})
    textures = [o for o in objects.values() if o["type"] == "Texture"]
    tex_files = [re.search(r'RelativeFilename: "([^"]+)"', o["body"]).group(1) for o in textures]
    tex_conn = [(objects[p]["name"], prop) for k, c, p, prop in conns if k == "OP" and objects.get(c, {}).get("type") == "Texture"]
    stop = [int(re.search(r'"LocalStop", "KTime", "Time", "",(\d+)', o["body"]).group(1)) for _, o in stacks]
    takes = re.findall(r'Take: "([^"]+)"', text)
    return {"text": text, "objects": objects, "models": models, "settings": settings, "stacks": [o["name"] for _, o in stacks], "stack_stop": stop,
            "takes": takes, "curve_nodes": curve_nodes, "tex_files": tex_files, "tex_conn": tex_conn}

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

def rot_x(deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return np.array([[1, 0, 0], [0, c, -s], [0, s, c]])

class Piece:
    def __init__(self, part, idx, V, tri):
        self.part, self.idx = part, idx
        vids = sorted({v for t in tri for v in t})
        remap = {v: i for i, v in enumerate(vids)}
        self.V = V[vids]
        self.T = np.array([[remap[v] for v in t] for t in tri])
        e = set()
        for a, b, c in self.T:
            for x, y in ((a, b), (b, c), (c, a)):
                e.add((min(x, y), max(x, y)))
        self.E = np.array(sorted(e))

    def normals(self, V=None):
        V = self.V if V is None else V
        n = np.cross(V[self.T[:, 1]] - V[self.T[:, 0]], V[self.T[:, 2]] - V[self.T[:, 0]])
        return n / np.linalg.norm(n, axis=1, keepdims=True)

def unique_dirs(d):
    d = d / np.linalg.norm(d, axis=1, keepdims=True)
    out = []
    for v in d:
        if not any(abs(abs(np.dot(v, u)) - 1) < 1e-6 for u in out):
            out.append(v)
    return np.array(out)

def sat_penetration(VA, NA, EA, VB, NB, EB):
    """Menor sobreposição entre os eixos de SAT (> 0 ⇒ interpenetração dessa magnitude; ≤ 0 ⇒ separados/encostados)."""
    cross = np.cross(EA[:, None, :], EB[None, :, :]).reshape(-1, 3)
    ln = np.linalg.norm(cross, axis=1)
    axes = np.vstack([NA, NB, cross[ln > 1e-9] / ln[ln > 1e-9, None]])
    pa, pb = VA @ axes.T, VB @ axes.T
    overlap = np.minimum(pa.max(0) - pb.min(0), pb.max(0) - pa.min(0))
    return float(overlap.min())

# ———————————————————————— validação ————————————————————————
def validate(fbx, source):
    checks = []
    def check(name, ok, detail):
        checks.append({"check": name, "ok": bool(ok), "detail": detail})

    M = fbx["models"]
    base, lid = M.get("Base"), M.get("Lid")
    n_tri = sum(len(m["polys"]) for m in M.values())
    all_tri = all(len(p) == 3 for m in M.values() for p in m["polys"])
    check("triangulos", all_tri and n_tri <= BUDGET, f"{n_tri} triângulos (Base {len(base['polys'])}, Lid {len(lid['polys'])}; todos tri: {all_tri}), orçamento ≤ {BUDGET}")
    s = fbx["settings"]
    check("unidade_eixos", s.get("UnitScaleFactor") == "100" and s.get("UpAxis") == "1" and s.get("FrontAxis") == "2" and s.get("FrontAxisSign") == "1",
          f"UnitScaleFactor {s.get('UnitScaleFactor')} (cm×100 = m), UpAxis {s.get('UpAxis')} (+Y), FrontAxis {s.get('FrontAxis')}/{s.get('FrontAxisSign')} (+Z), TimeMode {s.get('TimeMode')} (6 = 30 fps)")
    kinds = sorted({o["type"] for o in fbx["objects"].values()})
    allowed = {"Model", "Geometry", "Material", "Texture", "Video", "AnimationStack", "AnimationLayer", "AnimationCurveNode", "AnimationCurve"}
    models_ok = sorted(M) == ["Base", "Lid"] and all(m["sub"] == "Mesh" and m["parent"] == 0 for m in M.values())
    check("objetos", set(kinds) <= allowed and models_ok,
          f"tipos {kinds}; nós {sorted(M)} (Mesh, filhos da raiz); sem Deformer/Pose/NodeAttribute: sem rig, ossos, câmera, luz ou colisor")
    mats = [o["name"] for o in fbx["objects"].values() if o["type"] == "Material"]
    want = [m["name"] for m in source["materials"]]
    order_ok = all(m["materials"] == want for m in M.values())
    idx_ok = all(m["mat_mapping"] == "ByPolygon" and len(m["matidx"]) == len(m["polys"]) and set(m["matidx"]) == {0, 1} for m in M.values())
    check("materiais", len(mats) == 2 and sorted(mats) == sorted(want) and order_ok and idx_ok,
          f"{len(mats)} materiais {mats}; ordem ligada a cada nó = {want} (índice 0 pedra, 1 latão); índice por polígono ByPolygon usando os dois em Base e Lid")
    tex_ok = all((STAGING / t).is_file() for t in fbx["tex_files"]) and sorted(fbx["tex_conn"]) == sorted((m, "DiffuseColor") for m in want)
    check("textura", tex_ok, f"{fbx['tex_files']} ao lado do FBX, DiffuseColor de {sorted(m for m, _ in fbx['tex_conn'])}")

    # peças (componentes) por parte
    pieces = {}
    closed_all = vol_all = convex_all = True
    piece_rows = []
    for part, m in M.items():
        V, tri = m["verts"], m["polys"]
        comps = components(tri)
        plist = []
        for ci, c in enumerate(comps):
            pc = Piece(part, ci, V, [tri[i] for i in c])
            edges = {}
            for a, b, d in pc.T:
                for e in ((a, b), (b, d), (d, a)):
                    k = (min(e), max(e)); edges[k] = edges.get(k, 0) + 1
            closed = all(n == 2 for n in edges.values())
            P = pc.V
            vol = float(np.sum(np.einsum("ij,ij->i", P[pc.T[:, 0]], np.cross(P[pc.T[:, 1]], P[pc.T[:, 2]]))) / 6)
            n = pc.normals()
            d = np.einsum("ij,ij->i", n, P[pc.T[:, 0]])
            convex = bool(((P @ n.T) - d[None, :] <= 1e-5).all())
            closed_all &= closed; vol_all &= vol > 0; convex_all &= convex
            pc.closed, pc.volume, pc.convex = closed, vol, convex
            plist.append(pc)
            piece_rows.append({"part": part, "piece": ci, "triangles": len(c), "closed": closed, "convex": convex, "signed_volume_m3": round(vol, 7)})
        pieces[part] = plist
    check("pecas_fechadas_convexas", closed_all and convex_all, f"Base {len(pieces['Base'])} peças, Lid {len(pieces['Lid'])} peças; todas fechadas: {closed_all}; todas convexas: {convex_all}")
    check("winding_para_fora", vol_all, "volume com sinal > 0 em todas as peças")

    # normais por vértice de polígono = normal geométrica
    worst = 1.0
    for m in M.values():
        V, N = m["verts"], m["normals"]
        for i, (a, b, d) in enumerate(m["polys"]):
            g = np.cross(V[b] - V[a], V[d] - V[a]); g /= np.linalg.norm(g)
            for k in range(3):
                worst = min(worst, float(np.dot(N[i * 3 + k] / np.linalg.norm(N[i * 3 + k]), g)))
    check("normais", worst > 0.999, f"menor cos(normal FBX, normal geométrica) = {worst:.5f} (flat shading, para fora)")

    # UV → amostra da paleta; paleta no disco = gerador
    pal = source["palette"]
    centers = {name: ((sw["cell"][0] * 4 + 2) / 16, 1 - (sw["cell"][1] * 4 + 2) / 16) for name, sw in pal.items()}
    uv_ok, swatches = True, {}
    mat_of = {n: i for i, n in enumerate(want)}
    stone_sw, brass_sw = {"stoneLight", "stoneDark", "interior"}, {"brass", "brassWorn"}
    mat_sw_ok = True
    for part, m in M.items():
        sw_list = []
        for i in range(len(m["polys"])):
            u = {tuple(np.round(m["uv"][m["uvi"][i * 3 + k]], 4)) for k in range(3)}
            name = None
            if len(u) == 1:
                uu = next(iter(u))
                name = next((n for n, c in centers.items() if abs(c[0] - uu[0]) < 1e-3 and abs(c[1] - uu[1]) < 1e-3), None)
            uv_ok &= name is not None
            sw_list.append(name)
            if name in brass_sw and m["matidx"][i] != 1:
                mat_sw_ok = False
        swatches[part] = sw_list
    counts = {n: sum(swatches[p].count(n) for p in swatches) for n in pal}
    check("uv_paleta", uv_ok and all(counts[n] > 0 for n in pal) and mat_sw_ok,
          f"cada face com UV constante no centro de uma célula; faces por amostra {counts}; latão só no material Brass: {mat_sw_ok}")
    im = np.asarray(Image.open(STAGING / source["materials"][0]["baseMap"]).convert("RGBA"))
    pal_ok = im.shape == (16, 16, 4) and (im[..., 3] == 255).all()
    for name, sw in pal.items():
        cx, cy = sw["cell"]
        pal_ok &= bool((im[cy * 4:cy * 4 + 4, cx * 4:cx * 4 + 4, :3].reshape(-1, 3) == np.array(hex_rgb(sw["hex"]))).all())
    check("paleta_png", pal_ok, f"{source['materials'][0]['baseMap']} {im.shape[1]}×{im.shape[0]}, {len(pal)} células = paleta do gerador")

    # pivôs, dimensões
    hinge = np.array(source["hinge"])
    VB = base["verts"]
    VL = lid["verts"] + np.array(lid["translation"])
    allv = np.vstack([VB, VL])
    bmin, bmax = allv.min(0), allv.max(0)
    size = bmax - bmin
    ctr = (bmin + bmax) / 2
    base_ok = base["translation"] == [0, 0, 0] and base["rotation"] == [0, 0, 0] and abs(bmin[1]) < 1e-4 and abs(ctr[0]) < 0.005 and abs(ctr[2]) < 0.02
    check("pivo_base", base_ok, f"Base em (0,0,0) sem rotação; fundo em y {bmin[1]:.4f}; centro XZ dos bounds ({ctr[0]:.4f}, {ctr[2]:.4f}) m (pega na frente e dobradiças atrás quase se equilibram)")
    D = source["dimensions"]
    dim_ok = abs(size[0] - D["width"]) <= 0.03 and abs(size[1] - D["height"]) <= 0.02 and abs(size[2] - D["depth"]) <= 0.05
    check("dimensoes", dim_ok, f"fechado {size[0]:.3f} × {size[1]:.3f} × {size[2]:.3f} m (L × A × P) vs. ficha ~{D['width']} × {D['height']} × {D['depth']} (tol. 3/2/5 cm; profundidade inclui pega e nós)")
    # tampa: pivô no eixo; nós = peças cujos vértices ficam todos a ≤ raio+1 mm do eixo X local
    r_k = source["shape"]["hinge"]["knuckleRadius"]
    def is_knuckle(P, axis_yz):
        return bool((np.hypot(P[:, 1] - axis_yz[0], P[:, 2] - axis_yz[1]) <= r_k + 1e-3).all())
    lid_k = [p for p in pieces["Lid"] if is_knuckle(p.V, (0, 0))]
    base_k = [p for p in pieces["Base"] if is_knuckle(p.V, (hinge[1], hinge[2]))]
    rest = np.vstack([p.V for p in pieces["Lid"] if p not in lid_k])
    lid_ok = np.allclose(lid["translation"], hinge, atol=1e-6) and lid["rotation"] == [0, 0, 0] and abs(rest[:, 1].min()) < 1e-5 and rest[:, 2].min() >= -1e-5
    gaps = []
    for a in lid_k:
        for b in base_k:
            gaps.append(max(b.V[:, 0].min() - a.V[:, 0].max(), a.V[:, 0].min() - b.V[:, 0].max()))
    min_gap = min(gaps) if gaps else -1
    check("pivo_tampa_dobradica", lid_ok and len(lid_k) == 2 and len(base_k) == 4 and min_gap >= 0.003,
          f"Lid em {np.round(lid['translation'], 4).tolist()} = eixo da dobradiça, rotação de repouso 0; fundo da tampa em y local {rest[:, 1].min():.5f}; "
          f"menor z local fora dos nós {rest[:, 2].min():.4f} (≥ 0: tudo à frente do eixo); nós coaxiais: {len(lid_k)} da tampa, {len(base_k)} da base, folga mín. em X {min_gap * 1000:.1f} mm")

    # take
    stacks_ok = fbx["stacks"] == ["Open"] and fbx["takes"] == ["Open"]
    stop = fbx["stack_stop"][0] if fbx["stack_stop"] else 0
    dur = stop / KTIME
    cn = fbx["curve_nodes"]
    node = cn[0] if len(cn) == 1 else None
    anim_ok = stacks_ok and abs(dur - 0.6) < 1e-6 and node is not None and node["targets"] == [("Lid", "Lcl Rotation")] and sorted(node["curves"]) == ["d|X", "d|Y", "d|Z"]
    xs = node["curves"]["d|X"] if node else {"t": [0], "v": [0]}
    times = np.array(xs["t"]) / KTIME
    vals = np.array(xs["v"])
    uniform = np.allclose(np.diff(times), 1 / 30, atol=1e-9) if len(times) > 1 else False
    yz_zero = node is not None and all(np.allclose(node["curves"][k]["v"], 0) for k in ("d|Y", "d|Z"))
    step = float(np.abs(np.diff(vals)).max()) if len(vals) > 1 else 0
    anim_ok &= uniform and yz_zero and abs(vals[0]) < 1e-6 and abs(vals[-1] - source["clip"]["openDeg"]) < 1e-3 and len(vals) == 19
    check("take_open", anim_ok,
          f"stacks {fbx['stacks']}, takes {fbx['takes']}; duração {dur:.3f} s; {len(vals)} chaves a 30 fps; só {node['targets'] if node else None}; X {vals[0]:.1f}° → {vals[-1]:.1f}° "
          f"(mín {vals.min():.1f}°, passo máx {step:.2f}°/quadro); Y/Z constantes 0; nenhuma curva na Base/raiz (in-place)")

    return checks, pieces, swatches, piece_rows, (times, vals), {"min": np.round(bmin, 4).tolist(), "max": np.round(bmax, 4).tolist(), "size": np.round(size, 4).tolist(), "center": np.round(ctr, 4).tolist()}

def interpenetration(pieces, hinge, curve):
    times, vals = curve
    ts = sorted(set(np.round(np.concatenate([np.linspace(0, times[-1], int(round(times[-1] * 120)) + 1), times]), 9)))
    # SAT: pré-cálculo de normais/arestas únicas por peça (no espaço de repouso)
    def prep(p):
        return p.V, unique_dirs(p.normals()), unique_dirs(p.V[p.E[:, 1]] - p.V[p.E[:, 0]])
    B = [prep(p) for p in pieces["Base"]]
    L = [prep(p) for p in pieces["Lid"]]
    worst = {"penetration_m": -1.0}
    rows = []
    for t in ts:
        ang = float(np.interp(t, times, vals))
        R = rot_x(ang)
        pen_t = -1.0
        for li, (VL, NL, EL) in enumerate(L):
            W = VL @ R.T + hinge
            NW, EW = NL @ R.T, EL @ R.T
            lo, hi = W.min(0), W.max(0)
            for bi, (VB, NB, EB) in enumerate(B):
                if (lo > VB.max(0) + TOL).any() or (hi < VB.min(0) - TOL).any():
                    continue
                pen = sat_penetration(W, NW, EW, VB, NB, EB)
                if pen > pen_t:
                    pen_t = pen
                if pen > worst["penetration_m"]:
                    worst = {"penetration_m": pen, "t": t, "angle_deg": ang, "lid_piece": li, "base_piece": bi}
        rows.append({"t": round(t, 5), "angle_deg": round(ang, 3), "max_penetration_mm": round(pen_t * 1000, 3)})
    return worst, rows, len(ts)

# ———————————————————————— rasterizador (mesmo modelo do A08) ————————————————————————
LIGHT = np.array([-0.35, 0.85, -0.4]); LIGHT /= np.linalg.norm(LIGHT)
AMBIENT, DIFFUSE = 0.32, 0.78
SWATCH_IDS = ["stoneLight", "stoneDark", "interior", "brass", "brassWorn", "floor"]

def camera(forward):
    f = np.asarray(forward, float); f /= np.linalg.norm(f)
    right = np.cross([0, 1, 0], f); right /= np.linalg.norm(right)
    return right, np.cross(f, right), f

GAME_CAM = camera([0, -math.sin(math.radians(45)), math.cos(math.radians(45))])

def yaw_m(deg):
    a = math.radians(deg); c, s = math.cos(a), math.sin(a)
    return np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])   # Unity: rotação Y positiva leva +Z para +X

def assemble(fbx, swatches, angle, heading=0.0):
    """Faces no espaço Unity (x espelhado) com a tampa girada `angle` graus em X e o baú girado `heading` em Y."""
    out = []
    Y = yaw_m(heading)
    for part in ("Base", "Lid"):
        m = fbx["models"][part]
        V = m["verts"]
        if part == "Lid":
            V = V @ rot_x(angle).T + np.array(m["translation"])
        V = (V * np.array([-1, 1, 1])) @ Y.T
        for (a, b, d), sw in zip(m["polys"], swatches[part]):
            p = V[[a, b, d]]
            n = np.cross(p[1] - p[0], p[2] - p[0]); n = -n / np.linalg.norm(n)   # espelho em X inverte o winding
            out.append((p, n, sw))
    return out

def raster(faces, mat, cam, size, scale, center=(0.5, 0.5), ss=4, origin=np.zeros(3)):
    right, up, f = cam
    W, H, S = size[0] * ss, size[1] * ss, scale * ss
    cx, cy = center[0] * W, center[1] * H
    col = np.zeros((H, W, 3)); cov = np.zeros((H, W)); zbuf = np.full((H, W), np.inf); sid = np.full((H, W), -1, dtype=np.int16)
    for p, n, sw in faces:
        base = mat[sw]
        lit = base * (AMBIENT + DIFFUSE * max(0.0, float(np.dot(n, LIGHT))))
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
        sid[y0:y1 + 1, x0:x1 + 1][mm] = SWATCH_IDS.index(sw)
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

def ortho_sheet(fbx, sw, mat, open_deg):
    views = [("frente (olhando −Z)", camera([0, 0, -1]), 0), ("lado (olhando −X)", camera([-1, 0, 0]), 0),
             ("topo (olhando −Y)", camera([0, -1, 1e-4]), 0), ("câmera do jogo 45° (frente p/ câmera)", GAME_CAM, 180)]
    T = 340
    sheet = Image.new("RGB", (T * 4, T * 2 + 40), BG)
    for row, (ang, tag) in enumerate(((0, "FECHADO"), (open_deg, f"ABERTO {open_deg:.0f}°"))):
        for i, (title, cam, hd) in enumerate(views):
            faces = assemble(fbx, sw, ang, hd)
            col, a, _ = raster(faces, mat, cam, (T, T), 230, center=(0.5, 0.62), origin=np.array([0, 0.4, 0]))
            im = to_image(col, a, BG)
            label(im, f"{tag} · {title}", (8, 6), 14)
            sheet.paste(im, (i * T, row * T))
    label(sheet, "PREVIEW (rasterizador próprio, ortográfico; não é captura da Unity) · pivô na base central · tampa gira no eixo da dobradiça traseira", (8, T * 2 + 10), 15, WARN)
    return sheet

def take_strip(fbx, sw, mat, curve):
    times, vals = curve
    frames = [0, 2, 4, 7, 10, 13, 15, 18]
    T, TH = 220, 290
    sheet = Image.new("RGB", (T * len(frames), TH + 34), BG)
    for i, f in enumerate(frames):
        col, a, _ = raster(assemble(fbx, sw, vals[f], 180), mat, GAME_CAM, (T, TH), 150, center=(0.5, 0.74), origin=np.array([0, 0.4, 0]))
        sheet.paste(to_image(col, a, FLOOR_BG), (i * T, 0))
        label(sheet, f"q{f} · {times[f]:.2f} s · {vals[f]:.0f}°", (i * T + 6, 4), 13)
    label(sheet, "take Open (0,6 s, in-place) pela câmera do jogo 45°, sobre o piso #55596B · PREVIEW, não é captura da Unity", (8, TH + 8), 14, WARN)
    return sheet

def take_gif(fbx, sw, mat, curve, path):
    times, vals = curve
    seq = [0.0] * 8 + list(vals) + [vals[-1]] * 12
    ims = []
    for ang in seq:
        col, a, _ = raster(assemble(fbx, sw, ang, 180), mat, GAME_CAM, (240, 240), 140, center=(0.5, 0.7), origin=np.array([0, 0.4, 0]), ss=3)
        im = to_image(col, a, FLOOR_BG)
        label(im, "PREVIEW", (6, 220), 12, WARN)
        ims.append(im.convert("P", palette=Image.ADAPTIVE, colors=64))
    ims[0].save(path, save_all=True, append_images=ims[1:], duration=[33] * len(ims), loop=0, optimize=True)

def measure(fbx, sw, mat, open_deg):
    def lum(rgb):
        l = srgb_to_lin(np.array(rgb) / 255.0)
        return float(0.2126 * l[0] + 0.7152 * l[1] + 0.0722 * l[2])
    def contrast(a, b):
        la, lb = sorted([lum(a), lum(b)], reverse=True)
        return (la + 0.05) / (lb + 0.05)
    rows = []
    for off in (-30, -15, 0, 15, 30):
        rc = raster(assemble(fbx, sw, 0, 180 + off), mat, GAME_CAM, (200, 200), 120, origin=np.array([0, 0.4, 0]))
        ro = raster(assemble(fbx, sw, open_deg, 180 + off), mat, GAME_CAM, (200, 200), 120, origin=np.array([0, 0.4, 0]))
        mc, mo = rc[2] >= 0, ro[2] >= 0
        iou = (mc & mo).sum() / (mc | mo).sum()
        frac = lambda sid, m, names: float(np.isin(sid, [SWATCH_IDS.index(n) for n in names]).sum() / m.sum())
        mean = lambda col, a: [int(v) for v in (lin_to_srgb(col.sum((0, 1)) / a.sum()) * 255 + .5)]
        rows.append({"heading_offset_deg": off, "iou_closed_open": round(float(iou), 3), "area_open_vs_closed": round(float(mo.sum() / mc.sum()), 3),
                     "interior_closed": round(frac(rc[2], mc, ["interior"]), 4), "interior_open": round(frac(ro[2], mo, ["interior"]), 4),
                     "brass_closed": round(frac(rc[2], mc, ["brass", "brassWorn"]), 4), "mean_srgb_closed": mean(rc[0], rc[1]), "mean_srgb_open": mean(ro[0], ro[1])})
    # brass_same_light: albedo do latão vs albedo do piso (faces de cima do latão e piso sob a mesma luz) — critério.
    # brass_half_lit_vs_floor_albedo: latão a n·L 0,5 contra o piso sem sombra (comparação conservadora do A08) — só informativo.
    brass = hex_rgb("#E0B040")
    brass_half = [int(v) for v in lin_to_srgb(srgb_to_lin(np.array(brass) / 255) * (AMBIENT + DIFFUSE * 0.5)) * 255 + .5]
    contrasts = {f: {"brass_same_light": round(contrast(brass, rgb), 2), "brass_half_lit_vs_floor_albedo": round(contrast(brass_half, rgb), 2),
                     "chest_mean_closed_worst": round(min(contrast(r["mean_srgb_closed"], rgb) for r in rows), 2)} for f, rgb in FLOOR.items()}
    return rows, contrasts

def f08_preview(fbx, sw, mat, open_deg):
    if not F08.is_file():
        return None
    arr = np.asarray(Image.open(F08).convert("RGB")).astype(float)
    S = 260
    def stamp(ang, px, py):
        col, a, _ = raster(assemble(fbx, sw, ang, 180), mat, GAME_CAM, (S, S), F08_PPM)   # pivô no centro do tile
        y0, x0 = py - S // 2, px - S // 2
        bg = arr[y0:y0 + S, x0:x0 + S]
        arr[y0:y0 + S, x0:x0 + S] = np.asarray(to_image(col, a, bg)).astype(float)
    stamp(0, F08_PIVOT[0] - 300, F08_PIVOT[1])
    stamp(open_deg, F08_PIVOT[0] + 300, F08_PIVOT[1])
    im = Image.fromarray(arr.astype(np.uint8))
    label(im, "A11 fechado (PREVIEW)", (F08_PIVOT[0] - 410, F08_PIVOT[1] + 110), 20, WARN)
    label(im, "baú F08 atual (primitivas)", (F08_PIVOT[0] - 120, F08_PIVOT[1] + 150), 20, WARN)
    label(im, f"A11 aberto {open_deg:.0f}° (PREVIEW)", (F08_PIVOT[0] + 190, F08_PIVOT[1] + 110), 20, WARN)
    label(im, f"PREVIEW em escala (~{F08_PPM:.0f} px/m, ortográfico) sobre a captura real F08 2560×1440 (chest-locked.png); não é captura da Unity", (40, 1380), 22, WARN)
    return im.crop((700, 260, 1880, 900))

def main():
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    source = json.loads((SRC / f"{NAME}.source.json").read_text(encoding="utf-8"))
    fbx = read_fbx(FBX)
    checks, pieces, sw, piece_rows, curve, bounds = validate(fbx, source)
    open_deg = float(curve[1][-1])

    worst, pen_rows, n_samples = interpenetration(pieces, np.array(source["hinge"]), curve)
    checks.append({"check": "tampa_nao_corta_base", "ok": worst["penetration_m"] <= TOL,
                   "detail": f"SAT entre {len(pieces['Lid'])} peças da tampa × {len(pieces['Base'])} da base em {n_samples} instantes do take (120 Hz + chaves, ângulo interpolado das chaves do FBX): "
                             f"maior sobreposição {worst['penetration_m'] * 1000:.3f} mm (≤ {TOL * 1000:.1f} mm = contato) em t {worst.get('t', 0):.3f} s / {worst.get('angle_deg', 0):.1f}°"})

    pal = source["palette"]
    mat = {n: srgb_to_lin(np.array(hex_rgb(s["hex"])) / 255) for n, s in pal.items()}
    rows, contrasts = measure(fbx, sw, mat, open_deg)
    iou_max = max(r["iou_closed_open"] for r in rows)
    int_open = min(r["interior_open"] for r in rows)
    int_closed = max(r["interior_closed"] for r in rows)
    brass_min = min(r["brass_closed"] for r in rows)
    checks.append({"check": "aberto_fechado_distinguiveis", "ok": iou_max <= 0.8 and int_open >= 0.05 and int_closed <= 0.01,
                   "detail": f"câmera do jogo, rumos −30…+30°: IoU silhueta fechado×aberto máx {iou_max:.3f} (≤ 0,8); interior escuro visível aberto mín {int_open:.3f} (≥ 0,05) vs. fechado máx {int_closed:.4f} (≤ 0,01, só a fechadura)"})
    checks.append({"check": "latao_legivel", "ok": brass_min >= 0.08 and all(c["brass_same_light"] >= 3.0 for c in contrasts.values()),
                   "detail": f"fração de latão na silhueta fechada mín {brass_min:.3f} (≥ 0,08); WCAG latão vs piso sob a mesma luz {json.dumps({k: v['brass_same_light'] for k, v in contrasts.items()})} (≥ 3); "
                             f"informativo: latão a n·L 0,5 vs piso sem sombra {json.dumps({k: v['brass_half_lit_vs_floor_albedo'] for k, v in contrasts.items()})}; "
                             f"cor média do baú fechado vs piso (pior rumo) {json.dumps({k: v['chest_mean_closed_worst'] for k, v in contrasts.items()})} — pedra oficial = cor do piso, ver limitações"})

    SRC.mkdir(exist_ok=True)
    ortho_sheet(fbx, sw, mat, open_deg).save(SRC / "preview_ortho.png", optimize=False)
    take_strip(fbx, sw, mat, curve).save(SRC / "preview_take_strip.png", optimize=False)
    take_gif(fbx, sw, mat, curve, SRC / "preview_open.gif")
    f08 = f08_preview(fbx, sw, mat, open_deg)
    if f08:
        f08.save(SRC / "review_f08_scale.png", optimize=False)
    # contact sheet: 45° fechado/aberto + paleta
    cs = Image.new("RGB", (300 * 2 + 150, 380), BG)
    for i, ang in enumerate((0, open_deg)):
        col, a, _ = raster(assemble(fbx, sw, ang, 180 + 20), mat, GAME_CAM, (300, 380), 180, center=(0.5, 0.7), origin=np.array([0, 0.4, 0]))
        cs.paste(to_image(col, a, FLOOR_BG), (i * 300, 0))
    cs.paste(Image.open(STAGING / source["materials"][0]["baseMap"]).convert("RGB").resize((112, 112), Image.NEAREST), (620, 40))
    label(cs, "Palette 16×16", (620, 16), 13)
    label(cs, "PREVIEW · 45° · fechado / aberto · fundo = piso #55596B", (8, 356), 13, WARN)
    cs.save(SRC / "contact_sheet.png", optimize=False)

    ok = all(c["ok"] for c in checks)
    files = [FBX, STAGING / f"{NAME}.fbx.meta", STAGING / source["materials"][0]["baseMap"], SRC / f"{NAME}.glb"]
    report = {"ok": ok, "asset": "MODEL-CHEST", "batch": "A11", "validator": "Tools/Lemo3DGenerator/src/production/ModelChest_preview.py",
              "python": sys.version.split()[0], "numpy": np.__version__, "read_from_disk": str(FBX.relative_to(ROOT)).replace("\\", "/"),
              "checks": checks, "bounds_closed_m": bounds, "pieces": piece_rows, "interpenetration": {"worst": worst, "per_sample": pen_rows},
              "game_camera": "pitch 45°, câmera em −Z (DungeonMvpSceneBuilder.CameraOffset 0,14,−14), ortográfica no preview; baú girado 180° (frente para a câmera)",
              "readability": rows, "contrast_wcag": contrasts,
              "sha256": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in files},
              "not_validated": "Importação na Unity (ModelImporter Generic, clip padrão do take, materiais por MaterialDescription), URP, cena F08, presenter, collider e PlayMode NÃO observados (NÃO VALIDADO NO EDITOR)."}
    (SRC / "ModelChest.validation.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    for c in checks:
        print(("OK  " if c["ok"] else "FALHA ") + c["check"] + ": " + c["detail"])
    print("contraste:", json.dumps(contrasts), "leitura:", json.dumps(rows[2]))
    print("validação", "ok" if ok else "FALHOU", f"({sum(c['ok'] for c in checks)}/{len(checks)})")
    return 0 if ok else 1

if __name__ == "__main__":
    sys.exit(main())

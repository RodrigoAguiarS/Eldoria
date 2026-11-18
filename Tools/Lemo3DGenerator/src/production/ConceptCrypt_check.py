#!/usr/bin/env python3
"""A15 — checagem e publicação da folha de conceito da Cripta inundada.

Lê as capturas gravadas por preview/concept_crypt.html (modos sheet/layout) em preview/captures e as caixas por peça
de FloodedCrypt.source.json, mede os critérios de aceite da ficha A15 e publica:
  Assets/_Project/Art/Concepts/FloodedCrypt_Concept_01.png   (RGB, 2048x1536, sem alpha)
  Assets/_Project/Art/Concepts/Source~/FloodedCrypt/          (planta de revisão, medidas, validação)

    python src/production/ConceptCrypt_check.py [--captures preview/captures] [--no-publish]

Sai com código 1 se algum item falhar. Análise de imagem e geometria fora da Unity; não é mapa de colisão, NavMesh
nem prova de navegação. Não substitui a importação no Editor.
"""
import argparse
import colorsys
import json
import shutil
import sys
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
LAB = HERE.parent.parent
PROJECT = LAB.parent.parent
CONCEPTS = PROJECT / "Assets/_Project/Art/Concepts"
SOURCE = CONCEPTS / "Source~/FloodedCrypt"

SPEC_PALETTE = {"stone": "#55596B", "stoneDark": "#3E4152", "moss": "#4C5A44", "flame": "#FF9A2E", "rune": "#4FD8FF"}
RAISED = 0.06          # acima disso a peça conta como obstáculo/volume sobre o piso
EPS = 0.02


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def overlaps(b, x0, z0, x1, z1):
    return b["min"][0] < x1 - EPS and b["max"][0] > x0 + EPS and b["min"][2] < z1 - EPS and b["max"][2] > z0 + EPS


def srgb_to_lab(rgb):
    def lin(c):
        c /= 255
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    r, g, b = (lin(c) for c in rgb)
    x = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047
    y = 0.2126 * r + 0.7152 * g + 0.0722 * b
    z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883
    f = lambda t: t ** (1 / 3) if t > 0.008856 else 7.787 * t + 16 / 116
    return 116 * f(y) - 16, 500 * (f(x) - f(y)), 200 * (f(y) - f(z))


def mean_rgb(img, mask, include=True):
    px, mk = img.load(), mask.load()
    w, h = img.size
    s = [0, 0, 0]
    n = 0
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            if (mk[x, y] > 128) == include:
                p = px[x, y]
                s[0] += p[0]; s[1] += p[1]; s[2] += p[2]; n += 1
    return tuple(c / n for c in s) if n else (0, 0, 0), n


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser()
    ap.add_argument("--captures", default=str(LAB / "preview/captures"))
    ap.add_argument("--no-publish", action="store_true")
    args = ap.parse_args()
    cap = Path(args.captures)
    checks = []

    def check(name, ok, detail):
        checks.append({"item": name, "ok": bool(ok), "detail": detail})

    sheet = Image.open(cap / "FloodedCrypt_Concept_01.png")
    sm = json.loads((cap / "sheet_measure.json").read_text(encoding="utf-8"))
    lm = json.loads((cap / "layout_measure.json").read_text(encoding="utf-8"))
    src = json.loads((SOURCE / "FloodedCrypt.source.json").read_text(encoding="utf-8"))
    panels = {p["id"]: p for p in sm["panels"]}
    room = src["room"]
    boxes = src["boxes"]

    # 1. formato
    alpha_min = sheet.getchannel("A").getextrema()[0] if sheet.mode == "RGBA" else 255
    rgb = sheet.convert("RGB")
    check("formato PNG 2048x1536", sheet.size == (2048, 1536), f"{sheet.size}, modo {sheet.mode}, alpha mínimo {alpha_min}")
    check("opaco (publicado em RGB sem alpha)", alpha_min == 255, "canvas totalmente opaco; publicação converte para RGB")

    # 2. paleta da ficha e água azul-escura dessaturada
    pal = {k: src["palette"][k].upper() for k in SPEC_PALETTE}
    check("paleta da ficha nos materiais", pal == SPEC_PALETTE, json.dumps(pal))
    h, s, v = colorsys.rgb_to_hsv(*(c / 255 for c in hex_rgb(src["palette"]["water"])))
    main_view = rgb.crop(tuple(panels["room45"]["region"][:2]) + (panels["room45"]["region"][2], panels["room45"]["region"][3]))
    water_mask = Image.open(cap / "mask_water.png").convert("L")
    wmean, wn = mean_rgb(main_view, water_mask)
    wh, ws, wv = colorsys.rgb_to_hsv(*(c / 255 for c in wmean))
    check("água azul-escura dessaturada (material e render)",
          200 <= h * 360 <= 235 and s <= 0.45 and v <= 0.36 and wmean[2] >= max(wmean[:2]) and wv <= 0.4,
          f"material {src['palette']['water']} (matiz {h * 360:.0f}°, S {s:.2f}, V {v:.2f}); render médio rgb{tuple(round(c) for c in wmean)} "
          f"(matiz {wh * 360:.0f}°, S {ws:.2f}, V {wv:.2f}) em {wn} amostras")

    # 3. gabarito: interior 16x16, vãos de 4 m ao norte/sul, ritmo de 2 m
    walls = [b for b in boxes if b["tag"] in ("wall", "jamb")]
    ew = [b for b in walls if b["min"][0] > 7.5]
    ns = [b for b in walls if b["min"][2] > 7.5]
    inner_x = min(b["min"][0] for b in ew)
    inner_z = min(b["min"][2] for b in ns if b["tag"] == "wall")
    check("interior 16x16 com paredes de 1 m", abs(inner_x - 8) <= 0.1 and abs(inner_z - 8) <= 0.1 and abs(max(b["max"][0] for b in ew) - 9) <= 0.1,
          f"face interna x {inner_x:.2f} m, z {inner_z:.2f} m; face externa x {max(b['max'][0] for b in ew):.2f} m")
    gaps = {}
    for side, sz in (("N", 1), ("S", -1)):
        blocking = [b for b in walls if b["mat"] == "stoneDark" and (b["min"][2] * sz > 7.5 or b["max"][2] * sz > 7.5) and min(abs(b["min"][2]), abs(b["max"][2])) >= 7.5]
        right = min(b["min"][0] for b in blocking if b["min"][0] > -0.5)
        left = max(b["max"][0] for b in blocking if b["max"][0] < 0.5)
        gaps[side] = round(right - left, 3)
    check("vãos de porta de 4 m (norte e sul)", all(abs(g - room["door"]) <= 0.12 for g in gaps.values()), f"largura livre entre batentes {gaps} m")
    mods = [b for b in boxes if b["tag"] == "wall" and b["mat"] == "stoneDark" and b["max"][1] > 1.3]
    lens = sorted({round(max(b["max"][0] - b["min"][0], b["max"][2] - b["min"][2]), 1) for b in mods if min(b["max"][0] - b["min"][0], b["max"][2] - b["min"][2]) > 0.9})
    check("ritmo modular de 2 m", all(abs(l - 2) <= 0.12 or abs(l - 1) <= 0.12 for l in lens), f"comprimentos de bloco de parede {lens} m (módulos de 2 m e cantos de 1 m); placas de piso em grade de 2 m")

    # 4. centro 10x10 livre, água só nas bordas, rota seca porta-centro-porta
    c = room["clear"] / 2
    raised = [b for b in boxes if b["max"][1] > RAISED and b["tag"] not in ("floor", "floorBase", "bridge") and overlaps(b, -c, -c, c, c)]
    wet_center = [b for b in boxes if b["tag"] in ("water", "puddle") and overlaps(b, -c, -c, c, c)]
    check("centro 10x10 sem volumes nem água", not raised and not wet_center,
          f"{len(raised)} peças acima de {RAISED} m e {len(wet_center)} de água em [-{c:g}, {c:g}]²; planta de cima: {lm['panels'][0]['center10']['water']} px de água em {lm['panels'][0]['center10']['px']} px")
    water = [b for b in boxes if b["tag"] == "water"]
    inner = [b for b in water if max(abs(b["min"][0]), abs(b["max"][0])) < 6.9 and max(abs(b["min"][2]), abs(b["max"][2])) < 6.9]
    band = min(max(abs(b["min"][0]), abs(b["max"][0]), abs(b["min"][2]), abs(b["max"][2])) for b in water)
    check("água rasa só nas bordas", not inner and all(b["max"][1] <= room["curbTop"] for b in water if b["min"][1] < 0),
          f"{len(water)} peças de água, todas na faixa da borda (|x| ou |z| ≥ {band:.2f} m), topo ≤ meio-fio ({room['curbTop']} m); poças rasas só nas placas da borda")
    route_wet = [b for b in water if overlaps(b, -2, -9, 2, 9)]
    bridges = [b for b in boxes if b["tag"] == "bridge"]
    bridge_z = sorted({(round(b["min"][2], 1), round(b["max"][2], 1)) for b in bridges})
    check("rota seca porta-centro-porta", not route_wet and lm["panels"][0]["walkway"]["water"] == 0 and len(bridges) >= 6,
          f"{len(route_wet)} peças de água na faixa x ∈ [-2, 2]; planta: {lm['panels'][0]['walkway']['water']} px de água na passarela; lajes secas em z {bridge_z}")

    # 5. leitura na câmera de jogo (45°, ortográfica)
    zone = panels["room45"]["zone"]
    check("centro inteiro visível na câmera de jogo", zone["fraction"] >= 0.9, f"{zone['visible']}/{zone['total']} px do quadrado 10x10 sem oclusão por paredes/pilares ({zone['fraction']:.1%})")
    zmask = Image.open(cap / "mask_zone_visible.png").convert("L")
    amask = Image.open(cap / "mask_actors.png").convert("L")
    zpx, apx = zmask.load(), amask.load()
    floor_only = Image.new("L", zmask.size)
    fp = floor_only.load()
    for y in range(zmask.size[1]):
        for x in range(zmask.size[0]):
            fp[x, y] = 255 if zpx[x, y] > 128 and apx[x, y] < 128 else 0
    fmean, fn = mean_rgb(main_view, floor_only)
    amean, an = mean_rgb(main_view, amask)
    fl, al = srgb_to_lab(fmean), srgb_to_lab(amean)
    de = sum((p - q) ** 2 for p, q in zip(fl, al)) ** 0.5
    check("piso legível e personagens destacados", 22 <= fl[0] <= 60 and de >= 15,
          f"piso do centro L* {fl[0]:.1f} rgb{tuple(round(c) for c in fmean)} ({fn} amostras); personagens rgb{tuple(round(c) for c in amean)}; ΔE76 {de:.1f}")

    # 6. tochas e runas presentes e contidas na vista principal
    raw = main_view.tobytes()
    n = len(raw) // 3
    orange = cyan = 0
    for i in range(0, len(raw), 3):
        r, g, b = raw[i], raw[i + 1], raw[i + 2]
        if r > 200 and 90 < g < 210 and b < 120 and r - b > 120:
            orange += 1
        elif b > 200 and g > 170 and r < 170 and b - r > 70:
            cyan += 1
    check("tochas laranja e runas ciano presentes e discretas", orange >= 200 and cyan >= 60 and orange / n <= 0.04 and cyan / n <= 0.01,
          f"laranja {orange} px ({orange / n:.2%}), ciano {cyan} px ({cyan / n:.2%}) na vista principal; {len(src['layout']['torches'])} tochas, {len(src['layout']['niches'])} nichos")

    # 7. estudo de peças separado, sem texto (por construção), cada peça inteira na célula
    kit = [p for p in sm["panels"] if p["id"].startswith("Kit_")]
    inside = all(p["px"]["area"] > 0.05 * p["cell"][2] * p["cell"][3] and p["px"]["x0"] > 2 and p["px"]["x1"] < p["cell"][2] - 3 and p["px"]["y0"] >= 0 and p["px"]["y1"] < p["cell"][3] - 3 for p in kit)
    check("estudo de peças separado (6 células)", len(kit) == 6 and inside, "; ".join(f"{p['id']} {p['px']['w']}×{p['px']['h']} px" for p in kit))

    ok = all(c["ok"] for c in checks)
    report = {
        "id": "A15", "resource": "REF-CONCEPT-12", "ok": ok, "passed": sum(c["ok"] for c in checks), "total": len(checks),
        "scope": "análise de imagem e das caixas da geometria de render fora da Unity; não é colisão, NavMesh nem navegação; importação no Editor NÃO VALIDADA",
        "checks": checks,
        "measures": {"sheet": sm["panels"], "layout": lm["panels"]},
    }

    if not args.no_publish:
        SOURCE.mkdir(parents=True, exist_ok=True)
        rgb.save(CONCEPTS / "FloodedCrypt_Concept_01.png", optimize=True)
        for name in ("review_layout.png", "sheet_measure.json", "layout_measure.json"):
            shutil.copyfile(cap / name, SOURCE / name)
        (SOURCE / "ConceptCrypt.validation.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")

    for c in checks:
        print(("OK  " if c["ok"] else "FAIL") + f" {c['item']}: {c['detail']}")
    print(f"{report['passed']}/{report['total']} {'ok' if ok else 'FALHOU'}")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())

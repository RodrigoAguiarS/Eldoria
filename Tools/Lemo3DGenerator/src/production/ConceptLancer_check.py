#!/usr/bin/env python3
"""A13 — checagem e publicação da folha de conceito do lanceiro corrompido.

Lê as capturas gravadas por preview/concept_lancer.html (modos sheet/review/scale) em preview/captures,
mede os critérios de aceite da ficha A13 e publica:
  Assets/_Project/Art/Concepts/SkeletonLancer_Concept_01.png   (RGB, 2048x1536, sem alpha)
  Assets/_Project/Art/Concepts/Source~/SkeletonLancer/          (quadros de revisão, medidas, validação)

    python src/production/ConceptLancer_check.py [--captures preview/captures] [--no-publish]

Sai com código 1 se algum item falhar. Análise de imagem fora da Unity; não substitui a importação no Editor.
"""
import argparse
import json
import math
import shutil
import sys
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
LAB = HERE.parent.parent
PROJECT = LAB.parent.parent
CONCEPTS = PROJECT / "Assets/_Project/Art/Concepts"
SOURCE = CONCEPTS / "Source~/SkeletonLancer"

SPEC_PALETTE = {"bone": "#CFC6A8", "armor": "#3E4152", "rust": "#8A4A22", "eyes": "#FF2A2A"}
BG = (0x2B, 0x2C, 0x31)
HIT_RADIUS = 0.75          # Enemy_Lancer_Lunge.hitRadius (docs/LANCER-F13.md)
FAMILY = ["SkeletonMelee:Idle", "SkeletonMelee:Attack", "SkeletonElite:Idle", "SkeletonRanged:Idle", "Hero:Idle"]


def mask_from_bits(bits, n):
    return [[bits[y * n + x] == "1" for x in range(n)] for y in range(n)]


def bbox(m):
    ys = [y for y, row in enumerate(m) if any(row)]
    xs = [x for x in range(len(m[0])) if any(m[y][x] for y in range(len(m)))]
    return min(xs), min(ys), max(xs), max(ys)


def centered_iou(a, b):
    """IoU com as duas silhuetas alinhadas pelo centro da caixa (altura já normalizada a 128 px)."""
    n = len(a)
    ax0, ay0, ax1, ay1 = bbox(a)
    bx0, by0, bx1, by1 = bbox(b)
    dx = round((ax0 + ax1) / 2 - (bx0 + bx1) / 2)
    dy = round((ay0 + ay1) / 2 - (by0 + by1) / 2)
    inter = union = 0
    for y in range(n):
        for x in range(n):
            pa = a[y][x]
            sx, sy = x - dx, y - dy
            pb = b[sy][sx] if 0 <= sx < n and 0 <= sy < n else False
            inter += pa and pb
            union += pa or pb
    return inter / union if union else 0.0


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--captures", default=str(LAB / "preview/captures"))
    ap.add_argument("--no-publish", action="store_true")
    args = ap.parse_args()
    cap = Path(args.captures)
    checks = []

    def check(name, ok, detail):
        checks.append({"item": name, "ok": bool(ok), "detail": detail})

    sheet = Image.open(cap / "SkeletonLancer_Concept_01.png")
    sm = json.loads((cap / "sheet_measure.json").read_text(encoding="utf-8"))
    rv = json.loads((cap / "review_measure.json").read_text(encoding="utf-8"))
    src = json.loads((SOURCE / "SkeletonLancer.source.json").read_text(encoding="utf-8"))
    panels = {p["id"]: p for p in sm["panels"]}

    # 1. formato
    alpha_min = sheet.getchannel("A").getextrema()[0] if sheet.mode == "RGBA" else 255
    rgb = sheet.convert("RGB")
    check("formato PNG 2048x1536", sheet.size == (2048, 1536), f"{sheet.size}, modo {sheet.mode}, alpha mínimo {alpha_min}")
    check("opaco (publicado em RGB sem alpha)", alpha_min == 255, "canvas totalmente opaco; publicação converte para RGB")

    # 2. fundo neutro: amostras em áreas vazias conhecidas da composição (sem personagem)
    spots = [(700, 60), (1300, 60), (1450, 880), (700, 900), (40, 900), (1500, 920), (2000, 520)]
    samples = [rgb.getpixel(s) for s in spots]
    chroma = max(max(c) - min(c) for c in samples)
    centre = rgb.getpixel((1300, 60))
    # o próprio #2B2C31 tem croma 6 (0x31 - 0x2B); a vinheta é preta e não acrescenta matiz
    check("fundo neutro (cinza, sem matiz)", chroma <= 6, f"croma máximo {chroma} em {len(spots)} amostras; centro {'#%02X%02X%02X' % centre} (alvo #2B2C31 antes da vinheta)")

    # 3. paleta pedida e olhos vermelhos legíveis
    pal = {k: src["palette"][k].upper() for k in SPEC_PALETTE}
    check("paleta da ficha nos materiais", pal == SPEC_PALETTE, json.dumps(pal))
    px = rgb.tobytes()
    reds = sum(1 for i in range(0, len(px), 3) if px[i] > 200 and px[i + 1] < 120 and px[i + 2] < 140)
    check("olhos vermelhos visíveis", reds >= 40, f"{reds} px vermelhos saturados (só olhos são emissivos)")

    # 4. identidade entre vistas: mesma malha, pose, escala e linha de chão
    f, s, b = panels["front"], panels["side"], panels["back"]
    hs = [f["meters"]["h"], s["meters"]["h"], b["meters"]["h"]]
    tops = [f["px"]["y0"], s["px"]["y0"], b["px"]["y0"]]
    check("frente/lado/costas com a mesma altura", max(hs) - min(hs) <= 0.01, f"alturas {hs} m; topo do crânio y {tops} px; {f['ppm']} px/m")
    check("frente e costas espelhadas (mesma largura)", abs(f["meters"]["w"] - b["meters"]["w"]) <= 0.01, f"{f['meters']['w']} / {b['meters']['w']} m")

    # 5. pegada e linha da investida
    def yaw(d):
        return math.degrees(math.atan2(d[0], d[2]))

    g, w = panels["side"]["grip"], panels["windup_side"]["grip"]
    check("lança à frente na guarda (desvio horizontal ≤ 15°)", abs(yaw(g["dir"])) <= 15, f"desvio {yaw(g['dir']):.1f}°, inclinação {math.degrees(math.asin(g['dir'][1])):.1f}° para cima, mãos a {g['gap']} m")
    check("windup na mesma linha reta (desvio ≤ 15°)", abs(yaw(w["dir"])) <= 15, f"desvio {yaw(w['dir']):.1f}°, mãos a {w['gap']} m, lança recolhida {g['L'][2] - w['L'][2]:.2f} m")
    check("windup mais baixo que a guarda, mesmas proporções", panels["windup_side"]["meters"]["h"] < s["meters"]["h"], f"{panels['windup_side']['meters']['h']} m vs {s['meters']['h']} m (mesma malha/escala; só a pose muda)")

    # 6. a arma não promete alcance de dano: ponta vs hitRadius da F13 (contato = distância do segmento ao alvo)
    reach_g = math.hypot(g["tip"][0], g["tip"][2])
    reach_w = math.hypot(w["tip"][0], w["tip"][2])
    check("ponta da lança perto do raio de contato F13", reach_g - HIT_RADIUS <= 0.25, f"ponta a {reach_g:.2f} m do pivô na guarda, {reach_w:.2f} m no windup; hitRadius {HIT_RADIUS} m (excesso visual {reach_g - HIT_RADIUS:+.2f} m)")

    # 7. silhueta a 128 px (câmera de jogo 45°): forma distinta da família
    n = rv["cell"]
    masks = {k: mask_from_bits(v, n) for k, v in rv["masks"].items()}
    lance = masks["Lancer:Guard"]
    lx0, ly0, lx1, ly1 = bbox(lance)
    l_aspect = (lx1 - lx0 + 1) / (ly1 - ly0 + 1)
    rows = []
    worst = 0
    for k in FAMILY:
        m = masks[k]
        x0, y0, x1, y1 = bbox(m)
        iou = centered_iou(lance, m)
        worst = max(worst, iou)
        rows.append({"vs": k, "iou": round(iou, 3), "aspect": round((x1 - x0 + 1) / (y1 - y0 + 1), 3)})
    check("silhueta 128 px distinta da família", worst <= 0.6, f"guarda: proporção L/A {l_aspect:.2f}; IoU máx {worst:.2f}; " + "; ".join(f"{r['vs']} IoU {r['iou']} L/A {r['aspect']}" for r in rows))
    wi = centered_iou(masks["Lancer:Windup"], lance)
    check("windup distinguível da guarda a 128 px", wi <= 0.8, f"IoU guarda×windup {wi:.2f}")

    ok = all(c["ok"] for c in checks)
    report = {
        "id": "A13", "resource": "REF-CONCEPT-10", "ok": ok, "passed": sum(c["ok"] for c in checks), "total": len(checks),
        "scope": "análise de imagem e medidas do render fora da Unity; importação no Editor NÃO VALIDADA",
        "checks": checks, "silhouette128": rows,
        "measures": {"sheet": [{k: p[k] for k in ("id", "clip", "ppm", "meters", "px")} for p in sm["panels"]], "grip": {"guard": g, "windup": w}},
    }

    if not args.no_publish:
        SOURCE.mkdir(parents=True, exist_ok=True)
        rgb.save(CONCEPTS / "SkeletonLancer_Concept_01.png", optimize=True)
        for name in ("review_silhouette_128.png", "review_scale.png", "sheet_measure.json"):
            shutil.copyfile(cap / name, SOURCE / name)
        slim = {k: v for k, v in rv.items() if k != "masks"}
        slim["masks"] = "omitidas (bits 200x200 por silhueta); regenerar com ?mode=review"
        (SOURCE / "review_measure.json").write_text(json.dumps(slim, indent=2, ensure_ascii=False), encoding="utf-8")
        (SOURCE / "ConceptLancer.validation.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")

    for c in checks:
        print(("OK  " if c["ok"] else "FAIL") + f" {c['item']}: {c['detail']}")
    print(f"{report['passed']}/{report['total']} {'ok' if ok else 'FALHOU'}")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())

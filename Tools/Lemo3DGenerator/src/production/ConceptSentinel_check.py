#!/usr/bin/env python3
"""A14 — checagem e publicação da folha de conceito da Sentinela de Pedra.

Lê as capturas gravadas por preview/concept_sentinel.html (modos sheet/review/scale) em preview/captures,
mede os critérios de aceite da ficha A14 e publica:
  Assets/_Project/Art/Concepts/StoneSentinel_Concept_01.png   (RGB, 2048x1536, sem alpha)
  Assets/_Project/Art/Concepts/Source~/StoneSentinel/          (quadros de revisão, medidas, validação)

    python src/production/ConceptSentinel_check.py [--captures preview/captures] [--no-publish]

Sai com código 1 se algum item falhar. Análise de imagem fora da Unity; não substitui a importação no Editor.
"""
import argparse
import json
import shutil
import sys
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
LAB = HERE.parent.parent
PROJECT = LAB.parent.parent
CONCEPTS = PROJECT / "Assets/_Project/Art/Concepts"
SOURCE = CONCEPTS / "Source~/StoneSentinel"

SPEC_PALETTE = {"stone": "#55596B", "stoneDark": "#3E4152", "moss": "#4C5A44", "rune": "#4FD8FF", "core": "#4FD8FF", "ember": "#FF9A2E"}
# Primitiva/dados F15 (StoneSentinelSceneBuilder + Boss_StoneSentinel_Data): referência de escala, não contrato do conceito
F15_COLLIDER = (2.5, 2.7, 1.4)
F15_PRIMITIVE_WIDTH = 3.1          # braços em x ±1,25 m com 0,6 m de largura
OTHERS = ["Warden:Idle", "Warden:SlamWindup", "SkeletonElite:Idle", "SkeletonMelee:Idle", "Hero:Idle"]


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


def is_cyan(p):
    r, g, b = p
    return b > 200 and g > 170 and r < 170 and b - r > 70


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

    sheet = Image.open(cap / "StoneSentinel_Concept_01.png")
    sm = json.loads((cap / "sheet_measure.json").read_text(encoding="utf-8"))
    rv = json.loads((cap / "review_measure.json").read_text(encoding="utf-8"))
    src = json.loads((SOURCE / "StoneSentinel.source.json").read_text(encoding="utf-8"))
    panels = {p["id"]: p for p in sm["panels"]}

    # 1. formato
    alpha_min = sheet.getchannel("A").getextrema()[0] if sheet.mode == "RGBA" else 255
    rgb = sheet.convert("RGB")
    check("formato PNG 2048x1536", sheet.size == (2048, 1536), f"{sheet.size}, modo {sheet.mode}, alpha mínimo {alpha_min}")
    check("opaco (publicado em RGB sem alpha)", alpha_min == 255, "canvas totalmente opaco; publicação converte para RGB")

    # 2. fundo neutro: áreas vazias conhecidas (faixa superior, vão entre as linhas, cantos do painel 45°)
    spots = [(40, 40), (1000, 40), (1500, 40), (300, 720), (1000, 720), (1700, 720), (560, 1500), (40, 800)]
    samples = [rgb.getpixel(s) for s in spots]
    chroma = max(max(c) - min(c) for c in samples)
    check("fundo neutro (cinza, sem matiz)", chroma <= 6, f"croma máximo {chroma} em {len(spots)} amostras (o próprio #2B2C31 tem croma 6; vinheta preta)")

    # 3. paleta da ficha e ciano contido
    pal = {k: src["palette"][k].upper() for k in SPEC_PALETTE}
    check("paleta da ficha nos materiais", pal == SPEC_PALETTE, json.dumps(pal))
    f, s, b = panels["front"], panels["side"], panels["back"]
    fx0, fy0, fx1, fy1 = f["px"]["x0"], f["px"]["y0"], f["px"]["x1"], f["px"]["y1"]
    front = rgb.crop((fx0, fy0, fx1 + 1, fy1 + 1))
    raw = front.tobytes()
    cyan = sum(1 for i in range(0, len(raw), 3) if is_cyan(raw[i:i + 3]))
    frac = cyan / f["px"]["area"]
    check("núcleo/runas ciano visíveis e contidos", 300 <= cyan and frac <= 0.03, f"{cyan} px ciano na vista frontal = {frac:.2%} da área da silhueta ({f['px']['area']} px); limite 3 %")

    # 4. identidade entre vistas: mesma malha, pose, escala e linha de chão
    hs = [f["meters"]["h"], s["meters"]["h"], b["meters"]["h"]]
    tops = [f["px"]["y0"], s["px"]["y0"], b["px"]["y0"]]
    check("frente/lado/costas com a mesma altura", max(hs) - min(hs) <= 0.01, f"alturas {hs} m; topo y {tops} px; {f['ppm']} px/m")
    check("frente e costas espelhadas (mesma largura)", abs(f["meters"]["w"] - b["meters"]["w"]) <= 0.01, f"{f['meters']['w']} / {b['meters']['w']} m")

    # 5. escala: herói na mesma vista e porte próximo da primitiva F15 (leitura de massa, não colisor)
    hero_h = s["hero"]["meters"]["h"]
    check("herói de escala na vista lateral", 1.6 <= hero_h <= 2.1, f"herói {hero_h} m ao lado da Sentinela {s['meters']['h']} m (razão {s['meters']['h'] / hero_h:.2f}×)")
    w, d, h = f["meters"]["w"], s["meters"]["w"], f["meters"]["h"]
    check("porte compatível com a primitiva F15", abs(h - F15_COLLIDER[1]) <= 0.27 and w <= F15_PRIMITIVE_WIDTH * 1.12 and d <= 1.7,
          f"altura {h} m (colisor {F15_COLLIDER[1]} m), largura {w} m (primitiva {F15_PRIMITIVE_WIDTH} m, colisor {F15_COLLIDER[0]} m), profundidade {d} m (colisor {F15_COLLIDER[2]} m)")

    # 6. silhueta a 128 px (câmera de jogo 45°): distinta do Warden e da família
    n = rv["cell"]
    masks = {k: mask_from_bits(v, n) for k, v in rv["masks"].items()}
    stand, windup = masks["Sentinel:Stand"], masks["Sentinel:Windup"]
    x0, y0, x1, y1 = bbox(stand)
    aspect = (x1 - x0 + 1) / (y1 - y0 + 1)
    rows, worst = [], 0
    for k in OTHERS:
        m = masks[k]
        mx0, my0, mx1, my1 = bbox(m)
        iou = max(centered_iou(stand, m), centered_iou(windup, m))
        worst = max(worst, iou)
        rows.append({"vs": k, "iou": round(iou, 3), "aspect": round((mx1 - mx0 + 1) / (my1 - my0 + 1), 3)})
    warden = max(r["iou"] for r in rows if r["vs"].startswith("Warden"))
    check("silhueta 128 px distinta do Warden", warden <= 0.5, f"IoU máx contra Warden (Idle/SlamWindup) {warden:.2f}")
    check("silhueta 128 px distinta dos demais", worst <= 0.6, f"Stand L/A {aspect:.2f}; " + "; ".join(f"{r['vs']} IoU {r['iou']} L/A {r['aspect']}" for r in rows))
    wi = centered_iou(windup, stand)
    ws, ww = panels["silhouette_Stand"]["px"]["w"], panels["silhouette_Windup"]["px"]["w"]
    check("antecipação distinguível da postura neutra", wi <= 0.8 and ww >= ws * 1.2, f"IoU 128 px {wi:.2f}; largura 45° {ws}→{ww} px (+{ww / ws - 1:.0%}) com braços abertos")

    # 7. leitura sobre o chão: no painel do leque o corpo inteiro e o herói estão no quadro
    wf = panels["windup_fan"]
    inside = wf["px"]["x0"] > 4 and wf["px"]["x1"] < 896 and wf["px"]["y0"] > 4 and wf["px"]["y1"] < 772
    check("antecipação inteira sobre o piso com a área F15", inside and wf["hero"]["area"] > 0, f"Sentinela {wf['px']['w']}×{wf['px']['h']} px, herói {wf['hero']['w']}×{wf['hero']['h']} px; setor {wf['fan']['angle']}°/{wf['fan']['radius']} m (dados do protótipo, só desenho)")

    ok = all(c["ok"] for c in checks)
    report = {
        "id": "A14", "resource": "REF-CONCEPT-11", "ok": ok, "passed": sum(c["ok"] for c in checks), "total": len(checks),
        "scope": "análise de imagem e medidas do render fora da Unity; importação no Editor NÃO VALIDADA",
        "checks": checks, "silhouette128": rows,
        "measures": {"sheet": [{k: p[k] for k in ("id", "ppm", "px") if k in p} | ({"meters": p["meters"]} if "meters" in p else {}) for p in sm["panels"]]},
    }

    if not args.no_publish:
        SOURCE.mkdir(parents=True, exist_ok=True)
        rgb.save(CONCEPTS / "StoneSentinel_Concept_01.png", optimize=True)
        for name in ("review_silhouette_128.png", "review_scale.png", "sheet_measure.json", "scale_measure.json"):
            shutil.copyfile(cap / name, SOURCE / name)
        slim = {k: v for k, v in rv.items() if k != "masks"}
        slim["masks"] = "omitidas (bits 200x200 por silhueta); regenerar com ?mode=review"
        (SOURCE / "review_measure.json").write_text(json.dumps(slim, indent=2, ensure_ascii=False), encoding="utf-8")
        (SOURCE / "ConceptSentinel.validation.json").write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")

    for c in checks:
        print(("OK  " if c["ok"] else "FAIL") + f" {c['item']}: {c['detail']}")
    print(f"{report['passed']}/{report['total']} {'ok' if ok else 'FALHOU'}")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())

"""V03 — simula a amostragem dos ícones A01/A02 a 32/48 px sem e com mipmaps.

Não substitui a captura da Unity: modela o filtro (bilinear sem mip vs. trilinear
com cadeia box pré-multiplicada, equivalente à dilatação de alphaIsTransparency)
para justificar a importação. Uso: python -I icon_sampling.py <dash.png> <spin.png> <out_dir>
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

BACKGROUNDS = {"escuro": (13, 18, 24), "pedra": (90, 95, 102), "claro": (200, 204, 208)}
SIZES = (48, 32)


def load(path):
    rgba = np.asarray(Image.open(path).convert("RGBA"), dtype=np.float64) / 255.0
    premul = rgba.copy(); premul[..., :3] *= rgba[..., 3:4]
    return premul  # cor pré-multiplicada: média box equivale a dilatar cor nos pixels transparentes


def bilinear(img, n):
    """Uma amostra bilinear no centro de cada pixel de saída (GPU sem mipmaps)."""
    h, w = img.shape[:2]
    c = (np.arange(n) + .5) * (w / n) - .5
    x0 = np.clip(np.floor(c).astype(int), 0, w - 1); x1 = np.clip(x0 + 1, 0, w - 1)
    f = np.clip(c - np.floor(c), 0, 1)
    top = img[x0][:, x0] * (1 - f)[None, :, None] + img[x0][:, x1] * f[None, :, None]
    bot = img[x1][:, x0] * (1 - f)[None, :, None] + img[x1][:, x1] * f[None, :, None]
    return top * (1 - f)[:, None, None] + bot * f[:, None, None]


def mips(img):
    chain = [img]
    while chain[-1].shape[0] > 1:
        m = chain[-1]; chain.append((m[0::2, 0::2] + m[1::2, 0::2] + m[0::2, 1::2] + m[1::2, 1::2]) / 4)
    return chain


def trilinear(img, n):
    lod = np.log2(img.shape[0] / n); chain = mips(img)
    lo = int(np.floor(lod)); t = lod - lo
    a = bilinear(chain[lo], n)
    return a if t < 1e-6 else a * (1 - t) + bilinear(chain[lo + 1], n) * t


def reference(path, n):
    """Redução por área (box exato) sobre cor pré-multiplicada: alvo ideal sem serrilhado."""
    img = load(path); k = img.shape[0] / n
    out = np.zeros((n, n, 4))
    for i in range(n):
        for j in range(n):
            y0, y1 = int(round(i * k)), int(round((i + 1) * k)); x0, x1 = int(round(j * k)), int(round((j + 1) * k))
            out[i, j] = img[y0:y1, x0:x1].reshape(-1, 4).mean(0)
    return out


def over(premul, bg):
    bg = np.array(bg) / 255.0
    return premul[..., :3] + bg * (1 - premul[..., 3:4])


def grey(rgb):
    return rgb @ np.array([.2126, .7152, .0722])


def to_img(arr, scale=4):
    a = np.clip(arr * 255 + .5, 0, 255).astype(np.uint8)
    mode = "L" if a.ndim == 2 else "RGB"
    im = Image.fromarray(a, mode)
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)


def main(dash, spin, out):
    out = Path(out); out.mkdir(parents=True, exist_ok=True)
    icons = {"A01-dash": dash, "A02-spin": spin}
    results = {"method": __doc__.strip().splitlines()[0], "sizes": SIZES, "icons": {}}
    tiles = []  # (label, image)
    for name, path in icons.items():
        src = load(path); results["icons"][name] = {}
        for n in SIZES:
            ref = reference(path, n); nomip = bilinear(src, n); mip = trilinear(src, n)
            entry = {}
            for label, arr in (("sem_mip", nomip), ("com_mip", mip)):
                err = {bg: float(np.abs(over(arr, c) - over(ref, c)).mean() * 255) for bg, c in BACKGROUNDS.items()}
                entry[label] = {"erro_medio_vs_area_0_255": err, "erro_alpha_0_255": float(np.abs(arr[..., 3] - ref[..., 3]).mean() * 255)}
            # Halo: borda semitransparente não pode clarear sobre fundo escuro além do próprio contorno.
            edge = (mip[..., 3] > .02) & (mip[..., 3] < .5)
            straight = mip[..., :3][edge] / mip[..., 3][edge][:, None]
            entry["halo_borda_lum_media_0_255"] = float(grey(straight).mean() * 255) if edge.any() else 0.0
            entry["halo_borda_pixels"] = int(edge.sum())
            results["icons"][name][str(n)] = entry
            for bg, c in BACKGROUNDS.items():
                tiles.append((f"{name} {n}px {bg}", nomip, mip, c))
    # Distinção em cinza a 32 px: sobreposição de silhuetas e diferença média de luminância.
    m = {k: trilinear(load(p), 32) for k, p in icons.items()}
    a, b = (m["A01-dash"][..., 3] > .5), (m["A02-spin"][..., 3] > .5)
    results["cinza_32px"] = {
        "silhueta_IoU": float((a & b).sum() / (a | b).sum()),
        "diferenca_media_lum_sobre_pedra_0_255": float(np.abs(grey(over(m["A01-dash"], BACKGROUNDS["pedra"])) - grey(over(m["A02-spin"], BACKGROUNDS["pedra"]))).mean() * 255),
    }
    (out / "icon_sampling.json").write_text(json.dumps(results, indent=2, ensure_ascii=False), encoding="utf-8")
    # Prancha: por linha, sem mip | com mip, em cor e em cinza (4× vizinho mais próximo).
    rows = []
    for label, nomip, mip, c in tiles:
        parts = [to_img(over(nomip, c)), to_img(over(mip, c)), to_img(grey(over(nomip, c))).convert("RGB"), to_img(grey(over(mip, c))).convert("RGB")]
        w = sum(p.width for p in parts) + 8 * 3; h = max(p.height for p in parts)
        row = Image.new("RGB", (w, h), (40, 40, 40)); x = 0
        for p in parts: row.paste(p, (x, 0)); x += p.width + 8
        rows.append(row)
    sheet = Image.new("RGB", (max(r.width for r in rows), sum(r.height + 6 for r in rows)), (40, 40, 40)); y = 0
    for r in rows: sheet.paste(r, (0, y)); y += r.height + 6
    sheet.save(out / "sampling_sheet.png")
    print(json.dumps(results, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main(*sys.argv[1:4])

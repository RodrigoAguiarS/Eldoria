"""V03: recorta os ícones das capturas reais da Unity (coordenadas de measurements.txt) em cor e cinza, ampliados 4x.
Uso: python -I crop_sheet.py <captures_dir> <out.png>"""
import re, sys
from pathlib import Path
from PIL import Image, ImageOps
src = Path(sys.argv[1]); rows = []
for line in (src / "measurements.txt").read_text(encoding="utf-8").splitlines()[1:]:
    name = line.split()[0]
    if "-release-" not in name: continue
    im = Image.open(src / (name + ".png")).convert("RGB"); H = im.height; tiles = []
    for m in re.finditer(r"Icon@(\w+)=\((\d+),(\d+),(\d+),(\d+)\)", line):
        x, y, w, h = map(int, m.groups()[1:]); pad = w // 4
        box = (x - pad, H - y - h - pad, x + w + pad, H - y + pad)
        crop = im.crop(box)
        crop = crop.resize((crop.width * 4, crop.height * 4), Image.NEAREST)
        tiles += [crop, ImageOps.grayscale(crop).convert("RGB")]
    row = Image.new("RGB", (sum(t.width for t in tiles) + 8 * len(tiles), max(t.height for t in tiles)), (30, 30, 30)); x = 0
    for t in tiles: row.paste(t, (x, 0)); x += t.width + 8
    rows.append(row)
sheet = Image.new("RGB", (max(r.width for r in rows), sum(r.height + 8 for r in rows)), (30, 30, 30)); y = 0
for r in rows: sheet.paste(r, (0, y)); y += r.height + 8
sheet.save(sys.argv[2]); print(sheet.size, len(rows), "linhas")

"""V05: prancha de recortes ampliados (2×) em torno do herói, a partir dos PNG reais da captura."""
import sys
from PIL import Image, ImageDraw
src, out = sys.argv[1], sys.argv[2]
names = ["heal-f3", "loot-xp", "loot-gold", "loot-stored", "flash0-heal", "level-f5", "loot-gold-hero-hidden"]
tiles = []
for width in (1280, 1920):
    s = width / 1920
    box = [int(v * s) for v in (760, 330, 1160, 600)]
    for n in names:
        im = Image.open(f"{src}/{width}-{n}.png").convert("RGB").crop(box)
        im = im.resize((400, 270), Image.LANCZOS)
        d = ImageDraw.Draw(im); d.rectangle((0, 0, 170, 18), fill=(0, 0, 0)); d.text((4, 3), f"{width} {n}", fill=(255, 255, 255))
        tiles.append(im)
sheet = Image.new("RGB", (400 * 7, 270 * 2))
for i, t in enumerate(tiles): sheet.paste(t, ((i % 7) * 400, (i // 7) * 270))
sheet.save(out)

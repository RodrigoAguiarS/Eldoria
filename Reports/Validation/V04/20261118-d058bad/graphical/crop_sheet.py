"""V04: crops real Unity captures (focus/normal/disabled buttons, slot states) into one review sheet, 3x nearest."""
import sys
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
crops = [  # (file, box, label)
    ("1280x720-100-pause.png", (425, 180, 855, 300), "720p pausa: Continuar (Focus) x Talentos (Normal)"),
    ("1920x1080-100-pause.png", (630, 260, 1290, 440), "1080p pausa: Focus x Normal"),
    ("1280x720-100-bag.png", (40, 120, 680, 340), "720p bolsa: selecionado/equipado/raridade/vazio"),
    ("1280x720-100-bag.png", (40, 630, 690, 690), "720p bolsa: Equipar/Usar Disabled x Normal"),
]
tiles = []
for name, box, _ in crops:
    im = Image.open(f"{src}/{name}").convert("RGB").crop(box)
    scale = 3 if im.width < 500 else 2
    tiles.append(im.resize((im.width * scale, im.height * scale), Image.NEAREST))
w = max(t.width for t in tiles); h = sum(t.height for t in tiles) + 12 * len(tiles)
sheet = Image.new("RGB", (w, h), (40, 40, 40)); y = 0
for t in tiles: sheet.paste(t, (0, y)); y += t.height + 12
sheet.save(out); print(out, sheet.size)

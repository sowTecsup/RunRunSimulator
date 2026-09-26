import sys
from PIL import Image
ref, box, out = sys.argv[1], [int(x) for x in sys.argv[2].split(",")], sys.argv[3]
renders = sys.argv[4:]
H = 360
crop = Image.open(ref).convert("RGB").crop(box)
crop = crop.resize((int(crop.width * H / crop.height), H))
tiles = [crop] + [Image.open(r).convert("RGB").resize((H, H)) for r in renders]
img = Image.new("RGB", (sum(t.width for t in tiles) + 8 * (len(tiles) - 1), H), (40, 40, 40))
x = 0
for t in tiles:
    img.paste(t, (x, 0)); x += t.width + 8
img.save(out)
print("CMP", out, img.size)

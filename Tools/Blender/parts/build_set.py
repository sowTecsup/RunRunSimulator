import sys, os, importlib
sys.path.insert(0, os.path.dirname(__file__))
import part_common as pc
a = pc.args()
names, out_dir, tag = a[0].split(","), a[1], a[2]
views = a[3].split(",") if len(a) > 3 else ["side", "front"]
arm = pc.load_dragon("A")
objs = []
for n in names:
    mod = importlib.import_module("p_" + n)
    built = mod.build(arm)
    for slot in mod.SLOTS:
        pc.hide_baked(slot)
    objs += built
for o in objs:
    if o.name.startswith("Deco_"):
        h = o.name[5:11]
        pc.paint(o, tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)) + (1,))
    elif o.name.startswith("Wing"):
        pc.paint(o, pc.WING_COLOR)
    else:
        pc.paint(o, pc.ACCENT_COLOR)
os.makedirs(out_dir, exist_ok=True)
for v in views:
    pc.setup_render(os.path.join(out_dir, "set_%s_%s.png" % (tag, v)), view=v)
print("SET_DONE", tag, names)

import sys, os, importlib
sys.path.insert(0, os.path.dirname(__file__))
import part_common as pc
a = pc.args()
name, out_dir = a[0], a[1]
views = a[2].split(",") if len(a) > 2 else ["side", "front"]
letter = a[3] if len(a) > 3 else "A"
mod = importlib.import_module("p_" + name)
arm = pc.load_dragon(letter)
objs = mod.build(arm)
for slot in mod.SLOTS:
    pc.hide_baked(slot)
for o in objs:
    if o.name.startswith("Deco_"):
        h = o.name[5:11]
        pc.paint(o, tuple(int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)) + (1,))
    elif o.name.startswith("Wing"):
        pc.paint(o, pc.WING_COLOR)
    else:
        pc.paint(o, pc.ACCENT_COLOR)
os.makedirs(out_dir, exist_ok=True)
pc.export_part(arm, objs, os.path.join(out_dir, "MonchiPart_%s.fbx" % name))
for v in views:
    pc.setup_render(os.path.join(out_dir, "%s_%s.png" % (name, v)), view=v)
print("PART_DONE", name, [o.name for o in objs], sum(len(o.data.vertices) for o in objs))

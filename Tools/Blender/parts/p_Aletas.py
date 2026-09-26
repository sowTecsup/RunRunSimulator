import bmesh
from mathutils import Vector
import part_common as pc

SLOTS = ("Wing",)


def fin(side):
    s = 1 if side == "L" else -1
    root = Vector((0.47 * s, -0.10, 1.06))
    out, back, up = Vector((s, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
    rods = [((out * 0.30 + up * 0.80 + back * 0.25).normalized(), 0.74),
            ((out * 0.45 + up * 0.35 + back * 0.60).normalized(), 0.64),
            ((out * 0.35 + back * 0.90 - up * 0.08).normalized(), 0.50)]
    tips = [root + d * L for d, L in rods]
    outline = [root, tips[0]]
    for a, b in zip(tips, tips[1:]):
        mid = (a + b) / 2
        outline.append(root + (mid - root) * 0.72)
        outline.append(b)
    bm = bmesh.new()
    vs = [bm.verts.new(p) for p in outline]
    for k in range(1, len(vs) - 1):
        bm.faces.new((vs[0], vs[k], vs[k + 1]))
    obj = pc.new_object("Wing_Aletas_%s_mem" % side, bm)
    mod = obj.modifiers.new("solid", "SOLIDIFY")
    mod.thickness = 0.012
    mod.offset = 0
    parts = [obj]
    for i, (d, L) in enumerate(rods):
        pts = [root + d * (L * k / 7) for k in range(8)]
        parts.append(pc.tube("rod%d" % i, pts, [0.024 - 0.016 * k / 7 for k in range(8)], sides=8))
    pc.apply_transforms(obj)
    return pc.join(parts, "Wing_Aletas_%s" % side)


def build(arm):
    objs = [fin("L"), fin("R")]
    for o in objs:
        pc.skin_like(o, arm, "Wing")
    return objs

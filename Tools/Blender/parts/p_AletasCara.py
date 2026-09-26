import bmesh
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)


def fin(side):
    s = 1 if side == "L" else -1
    hit, nor = pc.surface_point((1.5 * s, -0.45, 0.95), (-s, 0, 0))
    root = (hit - nor * 0.03) if hit else Vector((0.50 * s, -0.45, 0.95))
    out, back, up = Vector((s, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
    rods = [((out * 0.22 - back * 0.12 + up * 0.95).normalized(), 0.55),
            ((out * 0.26 + back * 0.62 + up * 0.72).normalized(), 0.58),
            ((out * 0.26 + back * 0.96 + up * 0.08).normalized(), 0.46)]
    tips = [root + d * L for d, L in rods]
    outline = [root, tips[0]]
    for a, b in zip(tips, tips[1:]):
        mid = (a + b) / 2
        outline.append(root + (mid - root) * 0.62)
        outline.append(b)
    bm = bmesh.new()
    vs = [bm.verts.new(p) for p in outline]
    for k in range(1, len(vs) - 1):
        bm.faces.new((vs[0], vs[k], vs[k + 1]))
    obj = pc.new_object("Horn_AletasCara_%s_mem" % side, bm)
    mod = obj.modifiers.new("solid", "SOLIDIFY")
    mod.thickness = 0.012
    mod.offset = 0
    parts = [obj]
    for i, (d, L) in enumerate(rods):
        pts = [root - d * 0.03 + d * ((L + 0.03) * k / 7) for k in range(8)]
        parts.append(pc.tube("rod%d" % i, pts, [0.022 - 0.016 * k / 7 for k in range(8)], sides=8))
    pc.apply_transforms(obj)
    return pc.join(parts, "Horn_AletasCara_%s" % side)


def build(arm):
    objs = [fin("L"), fin("R")]
    for o in objs:
        pc.skin_like(o, arm, "Horn")
    return objs

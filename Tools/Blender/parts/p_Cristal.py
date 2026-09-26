import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)


def crystal(name, base, axis, L, r, roll=0.0, sides=6):
    axis = axis.normalized()
    pts = [base, base + axis * L * 0.22, base + axis * L * 0.48, base + axis * L]
    radii = [r * 0.55, r, r * 0.96, 0.006]
    ref = Vector((0, 0, 1)) if abs(axis.z) < 0.9 else Vector((1, 0, 0))
    side = axis.cross(ref).normalized()
    up = side.cross(axis)
    hint = side * math.cos(roll) + up * math.sin(roll)
    obj = pc.tube(name, pts, radii, sides=sides, side_hint=tuple(hint))
    for m in list(obj.modifiers):
        obj.modifiers.remove(m)
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj


def build(arm):
    root, nrm = pc.head_top(0.0, -0.54)
    center = root - nrm * 0.10
    specs = [
        ((0.05, -0.28, 0.96), 0.64, 0.175, 0.1),
        ((0.02, -0.92, 0.40), 0.58, 0.165, 0.5),
        ((0.18, -0.72, -0.55), 0.46, 0.145, 0.9),
        ((0.80, -0.50, 0.40), 0.46, 0.150, 0.3),
        ((-0.80, -0.50, 0.40), 0.44, 0.148, 0.7),
        ((-0.20, 0.50, 0.84), 0.34, 0.125, 0.2),
    ]
    parts = []
    for i, (d, L, r, roll) in enumerate(specs):
        parts.append(crystal("c%d" % i, center, Vector(d), L + 0.10, r, roll))
    obj = pc.join(parts, "Horn_Cristal")
    pc.skin_like(obj, arm, "Horn")
    return [obj]

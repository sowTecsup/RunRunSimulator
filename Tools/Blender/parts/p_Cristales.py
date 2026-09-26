import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Back",)


def crystal(name, base, axis, L, r, roll=0.0, sides=6):
    axis = axis.normalized()
    pts = [base, base + axis * L * 0.18, base + axis * L * 0.42, base + axis * L]
    radii = [r * 0.8, r, r * 0.96, 0.006]
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
    n = 7
    pairs = (1, 3, 5)
    parts = []
    for i in range(n):
        t = i / (n - 1)
        y = -0.30 + t * 1.30
        p, nrm = pc.surface_point((0, y, 3.0), (0, 0, -1))
        nrm = Vector((0, nrm.y, nrm.z)).normalized()
        size = 0.55 + 0.45 * math.sin(math.pi * min(1.0, t * 1.25 + 0.1))
        L = 0.30 * size + 0.04
        r = 0.155 * size
        axis = (nrm * 0.7 + Vector((0, 0.45, 0.55))).normalized()
        if i in pairs:
            for s in (-1, 1):
                a = (axis + Vector((0.28 * s, 0, 0))).normalized()
                base = p + Vector((0.045 * s, 0, 0)) - a * 0.04
                parts.append(crystal("c%d_%d" % (i, s), base, a, L * 0.85 + 0.04, r * 0.8, 0.3 * i))
        else:
            base = p - axis * 0.04
            parts.append(crystal("c%d" % i, base, axis, L + 0.04, r, 0.3 * i))
    obj = pc.join(parts, "Back_Cristales")
    pc.skin_like(obj, arm, "Back")
    return [obj]

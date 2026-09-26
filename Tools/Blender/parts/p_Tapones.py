import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)


def mallow(name, base, axis, R, H, sink=0.06, bevel=0.3, sides=20, side_hint=None):
    axis = Vector(axis).normalized()
    b = R * bevel
    prof = [(-sink, R * 0.9), (0.0, R), (H - b, R)]
    for a in (25, 50, 72):
        t = math.radians(a)
        prof.append((H - b + b * math.sin(t), R - b + b * math.cos(t)))
    prof.append((H, (R - b) * 0.8))
    prof.append((H, (R - b) * 0.4))
    pts = [Vector(base) + axis * h for h, _ in prof]
    return pc.tube(name, pts, [r for _, r in prof], sides=sides, side_hint=side_hint)


def bun(name, center, axis, R, T, sides=20, side_hint=(1, 0, 0)):
    axis = Vector(axis).normalized()
    b = T * 0.4
    prof = [(-T / 2, (R - b) * 0.5), (-T / 2 + b * 0.1, R - b)]
    for a in (60, 30):
        t = math.radians(a)
        prof.append((-T / 2 + b - b * math.sin(t), R - b + b * math.cos(t)))
    prof += [(-T / 2 + b, R), (T / 2 - b, R)]
    for a in (30, 60):
        t = math.radians(a)
        prof.append((T / 2 - b + b * math.sin(t), R - b + b * math.cos(t)))
    prof += [(T / 2 - b * 0.1, R - b), (T / 2, (R - b) * 0.5)]
    pts = [Vector(center) + axis * h for h, _ in prof]
    return pc.tube(name, pts, [r for _, r in prof], sides=sides, side_hint=side_hint)


def build(arm):
    p, nrm = pc.head_top(0.0, -0.44)
    axis = (nrm + Vector((0, 0, 1.6))).normalized()
    top = mallow("top", p, axis, 0.115, 0.09, sink=0.08, side_hint=(1, 0, 0))
    T = 0.07
    p, nrm = pc.surface_point((1.5, -0.38, 1.05), (-1, 0, 0))
    axis = (nrm + Vector((1, 0, 0))).normalized()
    d = 0.0
    for dy in (-0.12, -0.06, 0.0, 0.06, 0.12):
        for dz in (-0.12, -0.06, 0.0, 0.06, 0.12):
            hp, _ = pc.surface_point((1.5, -0.38 + dy, 1.05 + dz), (-1, 0, 0), "HornA")
            if hp is not None:
                d = max(d, (hp - p).dot(axis))
    c = p + axis * (d + T / 2 + 0.005)
    stem = pc.tube("stemL", [p - axis * 0.04, c], [0.085, 0.085], sides=16)
    left = pc.join([bun("bunL", c, axis, 0.13, T, side_hint=(0, 0, 1)), stem], "bunL")
    obj = pc.join([top, left, pc.mirror_x(left, "bunR")], "Horn_Tapones")
    pc.skin_like(obj, arm, "Horn")
    return [obj]

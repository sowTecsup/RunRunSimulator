import math
from mathutils import Vector, Matrix
import part_common as pc

SLOTS = ("Back",)


def mallow(name, base, axis, R, H, sink=0.05, bevel=0.15, dome=0.004, sides=16, side_hint=None):
    axis = Vector(axis).normalized()
    b = R * bevel
    prof = [(-sink, R * 0.92), (0.0, R), (H - b, R)]
    for a in (25, 50, 72):
        t = math.radians(a)
        prof.append((H - b + b * math.sin(t), R - b + b * math.cos(t)))
    prof.append((H + dome * 0.4, (R - b) * 0.85))
    prof.append((H + dome, (R - b) * 0.45))
    pts = [Vector(base) + axis * h for h, _ in prof]
    radii = [r for _, r in prof]
    return pc.tube(name, pts, radii, sides=sides, side_hint=side_hint)


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
    pieces = []
    n = 9
    y0, y1 = -0.28, 1.06
    ys = [y0 + (y1 - y0) * k / 200 for k in range(201)]
    spine = [pc.surface_point((0, y, 3.0), (0, 0, -1))[0] for y in ys]
    arc = [0.0]
    for a, b in zip(spine, spine[1:]):
        arc.append(arc[-1] + (b - a).length)
    Rs = [0.135 - 0.072 * (i / (n - 1)) ** 0.6 for i in range(n)]
    need = [Rs[i] + Rs[i + 1] for i in range(n - 1)]
    gap = (arc[-1] - sum(need)) / (n - 1)
    targets = [0.0]
    for d in need:
        targets.append(targets[-1] + d + gap)
    for i in range(n):
        t = i / (n - 1)
        k = next((j for j, s in enumerate(arc) if s >= targets[i] - 1e-6), len(arc) - 1)
        side = 1 if i % 2 else -1
        x = 0.018 * side
        p, nrm = pc.surface_point((x, ys[k], 3.0), (0, 0, -1))
        R = Rs[i]
        H = 0.16 - 0.065 * t ** 0.7
        axis = (nrm + Vector((0.08 * side, 0, 0))).normalized()
        pieces.append(mallow("m%d" % i, p, axis, R, H, side_hint=(1, 0, 0)))
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
    pieces += [left, pc.mirror_x(left, "bunR")]
    obj = pc.join(pieces, "Back_Malvaviscos")
    pc.skin_like(obj, arm, "Back")
    return [obj]

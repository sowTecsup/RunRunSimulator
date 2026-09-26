import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Back",)


def fin(name, base, axis, h, half_len, half_thick, n=11, tip=0.006):
    back = Vector((0, 1, 0))
    sink = 0.05
    p0 = base - axis * sink
    pts = pc.bezier(p0, base + axis * h * 0.35, base + axis * h * 0.7 + back * h * 0.12,
                    base + axis * h + back * h * 0.32, n)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        k = max(0.0, (t - 0.12) / 0.88)
        ln = max(tip, half_len * (1 - k) ** 1.35)
        th = max(tip, half_thick * (1 - k) ** 0.9)
        radii.append((th, ln))
    return pc.tube(name, pts, radii, sides=12, side_hint=(1, 0, 0))


def build(arm):
    n = 8
    fins = []
    for i in range(n):
        t = i / (n - 1)
        y = -0.32 + t * 1.42
        p, nrm = pc.surface_point((0, y, 3.0), (0, 0, -1))
        if p is None:
            continue
        h = 0.28 * (1 - 0.42 * t ** 1.2) + 0.03 * math.sin(math.pi * min(1, t * 2.5))
        half_len = 0.16 * (1 - 0.4 * t)
        half_thick = 0.05 * (1 - 0.3 * t)
        axis = (nrm * 0.55 + Vector((0, 0.25, 0.8))).normalized()
        fins.append(fin("fin%d" % i, p, axis, h, half_len, half_thick))
    obj = pc.join(fins, "Back_PuasGruesas")
    pc.skin_like(obj, arm, "Back")
    return [obj]

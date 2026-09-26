import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Back",)


def spike(name, base, axis, h, r0, n=9, tip=0.006):
    back = Vector((0, 1, 0))
    p0 = base - axis * 0.03
    pts = pc.bezier(p0, base + axis * h * 0.35, base + axis * h * 0.7 + back * h * 0.05,
                    base + axis * h + back * h * 0.14, n)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        flare = 1 + 0.25 * max(0.0, 1 - t / 0.2) ** 2
        radii.append(max(tip, r0 * flare * (1 - t) ** 0.8))
    return pc.tube(name, pts, radii, sides=10)


def build(arm):
    n = 7
    spikes = []
    for side in (1, -1):
        for i in range(n):
            t = (i + (0.0 if side > 0 else 0.5)) / (n - 0.5)
            y = -0.12 + t * 1.02
            x = side * (0.075 + 0.02 * t)
            p, nrm = pc.surface_point((x, y, 3.0), (0, 0, -1))
            if p is None:
                continue
            h = 0.13 * (1 - 0.55 * t ** 1.4) + 0.015 * math.sin(math.pi * min(1, t * 2.0))
            r0 = 0.058 * (1 - 0.45 * t)
            axis = (nrm * 0.45 + Vector((side * 0.22, 0.45, 0.75))).normalized()
            spikes.append(spike("s%d_%d" % (side, i), p, axis, h + 0.03, r0))
    obj = pc.join(spikes, "Back_PuasDobles")
    pc.skin_like(obj, arm, "Back")
    return [obj]

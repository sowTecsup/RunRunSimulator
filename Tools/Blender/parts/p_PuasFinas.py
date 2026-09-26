import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Back",)


def spike(name, base, axis, h, r0, n=9, tip=0.006):
    back = Vector((0, 1, 0))
    pts = pc.bezier(base, base + axis * h * 0.35, base + axis * h * 0.7 + back * h * 0.04,
                    base + axis * h + back * h * 0.10, n)
    radii = [max(tip, r0 * (1 - i / (n - 1)) ** 0.95) for i in range(n)]
    return pc.tube(name, pts, radii, sides=12)


def build(arm):
    n = 8
    spikes = []
    for i in range(n):
        t = i / (n - 1)
        y = -0.30 + t * 1.30
        p, nrm = pc.surface_point((0, y, 3.0), (0, 0, -1))
        h = 0.23 * (1 - 0.55 * t ** 1.2) + 0.02 * math.sin(math.pi * min(1, t * 2.5))
        r0 = 0.054 - 0.02 * t
        axis = (nrm * 0.5 + Vector((0, 0.5, 0.75))).normalized()
        base = p - axis * 0.035
        spikes.append(spike("spike%d" % i, base, axis, h + 0.035, r0))
    obj = pc.join(spikes, "Back_PuasFinas")
    pc.skin_like(obj, arm, "Back")
    return [obj]

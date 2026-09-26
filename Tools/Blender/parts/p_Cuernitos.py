import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)


def cone(name, pts, r0, power=0.8, tip=0.006):
    n = len(pts)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        flare = 1 + 0.30 * max(0.0, 1 - t / 0.15) ** 2
        radii.append(max(tip, r0 * flare * (1 - t) ** power))
    return pc.tube(name, pts, radii, sides=16)


def bull_horn(name, x, y, height, r0, out_peak, tip_in, lean_back):
    root, nrm = pc.head_top(x, y)
    root = root - Vector((0, 0, 0.05))
    up, out, back = Vector((0, 0, 1)), Vector((1, 0, 0)), Vector((0, 1, 0))
    pts = pc.bezier(root,
                    root + up * height * 0.25 + out * out_peak * 0.9,
                    root + up * height * 0.85 + out * out_peak * 1.1 + back * lean_back * 0.5,
                    root + up * height + out * (out_peak - tip_in) + back * lean_back, 16)
    return cone(name, pts, r0)


def build(arm):
    right = bull_horn("Horn_Cuernitos_R", 0.23, -0.40, 0.28, 0.125, 0.12, 0.08, 0.05)
    left = pc.mirror_x(right, "Horn_Cuernitos_L")
    for o in (right, left):
        pc.shade_smooth(o)
        pc.skin_like(o, arm, "Horn")
    return [right, left]

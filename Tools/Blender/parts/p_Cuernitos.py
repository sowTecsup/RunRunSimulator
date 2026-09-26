import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)


def cone(name, pts, r0, power=0.65, tip=0.006):
    n = len(pts)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        flare = 1 + 0.30 * max(0.0, 1 - t / 0.15) ** 2
        radii.append(max(tip, r0 * flare * (1 - t) ** power))
    return pc.tube(name, pts, radii, sides=14)


def nub(name, x, y, height, r0, lean_out, lean_back):
    root, nrm = pc.head_top(x, y)
    root = root - Vector((0, 0, 0.04))
    up, out, back = Vector((0, 0, 1)), Vector((1, 0, 0)), Vector((0, 1, 0))
    pts = pc.bezier(root,
                    root + up * height * 0.40 + out * lean_out * 0.2 + back * lean_back * 0.12,
                    root + up * height * 0.80 + out * lean_out * 0.6 + back * lean_back * 0.25,
                    root + up * height + out * lean_out + back * lean_back, 14)
    return cone(name, pts, r0)


def build(arm):
    front = nub("front", 0.22, -0.48, 0.27, 0.11, 0.06, 0.14)
    rear = nub("rear", 0.29, -0.15, 0.21, 0.09, 0.07, 0.12)
    right = pc.join([front, rear], "Horn_Cuernitos_R")
    left = pc.mirror_x(right, "Horn_Cuernitos_L")
    for o in (right, left):
        pc.shade_smooth(o)
        pc.skin_like(o, arm, "Horn")
    return [right, left]

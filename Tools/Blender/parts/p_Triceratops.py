import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)


def horn(name, pts, r0, power=0.9, tip=0.006):
    n = len(pts)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        flare = 1 + 0.35 * max(0.0, 1 - t / 0.12) ** 2
        radii.append(max(tip, r0 * flare * (1 - t) ** power))
    return pc.tube(name, pts, radii, sides=14)


def build(arm):
    root, nrm = pc.head_top(0.30, -0.46)
    root = root - Vector((0.02, 0, 0.11))
    up, out, fwd = Vector((0, 0, 1)), Vector((1, 0, 0)), Vector((0, -1, 0))
    pts = pc.bezier(root,
                    root + out * 0.40 + fwd * 0.30 + up * 0.20,
                    root + out * 0.58 + fwd * 0.30 + up * 1.00,
                    root - out * 0.02 - fwd * 0.10 + up * 1.02, 32)
    right = horn("Horn_Triceratops_R", pts, 0.16, power=0.85)
    left = pc.mirror_x(right, "Horn_Triceratops_L")
    for o in (right, left):
        pc.shade_smooth(o)
        pc.skin_like(o, arm, "Horn")
    return [right, left]

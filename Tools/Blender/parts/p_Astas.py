import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)


def tine(name, pts, r0, flat=1.35, power=0.85, tip=0.006):
    n = len(pts)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        r = max(tip, r0 * (1 - t) ** power)
        radii.append((r * flat, r / flat))
    return pc.tube(name, pts, radii, sides=12, side_hint=(0, 1, 0))


def build(arm):
    root, nrm = pc.head_top(0.2, -0.40)
    root = root - Vector((0, 0, 0.05))
    up, out, fwd = Vector((0, 0, 1)), Vector((1, 0, 0)), Vector((0, -1, 0))
    main = pc.bezier(root, root + up * 0.30 + out * 0.06 + fwd * 0.02,
                     root + up * 0.56 + out * 0.24 - fwd * 0.04, root + up * 0.80 + out * 0.32 + fwd * 0.06, 18)
    parts = [tine("main", main, 0.11, flat=1.6)]
    b = main[6]
    front = pc.bezier(b - out * 0.02, b + fwd * 0.16 + up * 0.04 + out * 0.04,
                      b + fwd * 0.28 + up * 0.14 + out * 0.07, b + fwd * 0.32 + up * 0.30 + out * 0.08, 12)
    parts.append(tine("front", front, 0.075, flat=1.5))
    c = main[2]
    brow = pc.bezier(c - out * 0.02, c + fwd * 0.09 + out * 0.03, c + fwd * 0.16 + up * 0.03 + out * 0.05,
                     c + fwd * 0.19 + up * 0.10 + out * 0.06, 9)
    parts.append(tine("brow", brow, 0.055, flat=1.4))
    right = pc.join(parts, "Horn_Astas_R")
    left = pc.mirror_x(right, "Horn_Astas_L")
    for o in (right, left):
        pc.shade_smooth(o)
        pc.skin_like(o, arm, "Horn")
    return [right, left]

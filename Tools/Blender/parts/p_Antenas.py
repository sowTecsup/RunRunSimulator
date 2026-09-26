import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)


def antenna(name, pts, r0, r_neck=0.04, paddle=0.058, tip=0.006, flat_hint=(0, 0, 1)):
    n = len(pts)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        if t < 0.82:
            r = r_neck + (r0 - r_neck) * (1 - t / 0.82) ** 0.9
            radii.append((r, r))
        else:
            u = (t - 0.82) / 0.18
            w = r_neck + (paddle - r_neck) * math.sin(math.pi * 0.5 * min(1.0, u / 0.55))
            if u > 0.55:
                w = paddle * max(0.0, 1 - min(1.0, (u - 0.55) / 0.45) ** 1.6) ** 0.6
            th = r_neck * (1 - 0.55 * u) * max(0.0, 1 - u) ** 0.35
            radii.append((max(tip, w), max(tip, th)))
    return pc.tube(name, pts, radii, sides=14, side_hint=flat_hint)


def build(arm):
    root, nrm = pc.head_top(0.2, -0.54)
    root = root - Vector((0, 0, 0.08))
    up, out, back = Vector((0, 0, 1)), Vector((1, 0, 0)), Vector((0, 1, 0))
    pts = pc.bezier(root,
                    root + up * 0.44 + out * 0.36 - back * 0.14,
                    root + up * 0.86 + out * 0.80 + back * 0.22,
                    root + up * 0.58 + out * 0.98 + back * 1.00, 36)
    n = len(pts)
    for i in range(n):
        s = max(0.0, (i / (n - 1) - 0.78) / 0.22)
        pts[i] = pts[i] + up * 0.10 * s * s + back * 0.02 * s
    right = antenna("Horn_Antenas_R", pts, 0.125, r_neck=0.05, paddle=0.085)
    left = pc.mirror_x(right, "Horn_Antenas_L")
    for o in (right, left):
        pc.shade_smooth(o)
        pc.skin_like(o, arm, "Horn")
    return [left, right]

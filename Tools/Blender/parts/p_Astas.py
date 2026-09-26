import math
from mathutils import Vector
import part_common as pc
from p_Carnero import catmull

SLOTS = ("Horn",)

UP, OUT, FWD = Vector((0, 0, 1)), Vector((1, 0, 0)), Vector((0, -1, 0))


def blade(name, pts, width, thick, peak=0.3, hold=0.5, tip=0.006, hint=(0, 1, 0)):
    n = len(pts)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        if t < peak:
            f = 0.75 + 0.25 * math.sin(0.5 * math.pi * t / peak)
        elif t < hold:
            f = 1.0
        else:
            f = (1 - ((t - hold) / (1 - hold)) ** 1.6) ** 1.1
        w = max(tip, width * f)
        radii.append((w, max(tip, thick * (0.4 + 0.6 * f))))
    return pc.tube(name, pts, radii, sides=12, side_hint=hint)


def build(arm):
    root, _ = pc.head_top(0.2, -0.36)
    root = root - Vector((0, 0, 0.06))
    keys = [root,
            root + UP * 0.24 + OUT * 0.05 + FWD * 0.04,
            root + UP * 0.50 + OUT * 0.12 - FWD * 0.02,
            root + UP * 0.74 + OUT * 0.17 - FWD * 0.02,
            root + UP * 0.92 + OUT * 0.20 + FWD * 0.05]
    main = catmull(keys, per=6)
    parts = [blade("main", main, 0.12, 0.032)]
    b = main[8] + FWD * 0.03
    tine = pc.bezier(b, b + FWD * 0.14 + OUT * 0.03 + UP * 0.03,
                     b + FWD * 0.25 + OUT * 0.06 + UP * 0.11, b + FWD * 0.30 + OUT * 0.08 + UP * 0.27, 12)
    parts.append(blade("tine", tine, 0.07, 0.024, peak=0.1, hold=0.25, hint=(0, 0, 1)))
    brow_pts = []
    for i in range(10):
        t = i / 9
        y = -0.30 - 0.30 * t
        z = 1.14 - 0.05 * t - 0.10 * t * t
        p, nrm = pc.surface_point((1.5, y, z), (-1, 0, 0))
        brow_pts.append(p + nrm * 0.012)
        brow_nrm = nrm
    radii = []
    for i in range(10):
        t = i / 9
        w = 0.032 * math.sin(math.pi * (0.1 + 0.9 * t)) ** 0.6 + 0.006
        radii.append((0.018, w))
    parts.append(pc.tube("brow", brow_pts, radii, sides=10, side_hint=brow_nrm))
    right = pc.join(parts, "Horn_Astas_R")
    left = pc.mirror_x(right, "Horn_Astas_L")
    for o in (right, left):
        pc.shade_smooth(o)
        pc.skin_like(o, arm, "Horn")
    return [right, left]

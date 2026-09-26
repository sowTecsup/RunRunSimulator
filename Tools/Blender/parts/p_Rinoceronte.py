import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)


def taper(name, pts, r0, flat=1.0, power=0.9, tip=0.006, hold=0.0, flare=0.0):
    n = len(pts)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        u = max(0.0, (t - hold) / (1 - hold))
        r = max(tip, r0 * (1 - u) ** power * (1 + flare * max(0.0, 1 - t / 0.3) ** 2))
        radii.append((r * flat, r / flat))
    return pc.tube(name, pts, radii, sides=16, side_hint=(1, 0, 0))


def build(arm):
    up, fwd = Vector((0, 0, 1)), Vector((0, -1, 0))
    root, nrm = pc.surface_point((0, -3.0, 1.08), (0, 1, 0))
    root = root - fwd * 0.20 - up * 0.13
    main = pc.bezier(root, root + fwd * 0.42 + up * 0.18,
                     root + fwd * 0.74 + up * 0.15, root + fwd * 1.00 + up * 0.38, 26)
    parts = [taper("main", main, 0.165, flat=0.86, power=0.62, hold=0.2, flare=0.1)]
    back, _ = pc.head_top(0, -0.30)
    back = back - Vector((0, 0, 0.05))
    small = pc.bezier(back, back + up * 0.10 + fwd * 0.02, back + up * 0.17 + fwd * 0.07,
                      back + up * 0.22 + fwd * 0.14, 10)
    parts.append(taper("small", small, 0.06, flat=1.1, power=0.8))
    horn = pc.join(parts, "Horn_Rinoceronte")
    pc.shade_smooth(horn)
    pc.skin_like(horn, arm, "Horn")
    return [horn]

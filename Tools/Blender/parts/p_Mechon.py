import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)

LOCKS = (
    (0.00, -0.08, 0.54, 0.38, 0.24, 0.09),
    (-0.07, -0.16, 0.48, 0.33, 0.23, 0.085),
    (0.07, -0.22, 0.44, 0.29, 0.22, 0.08),
    (0.00, -0.30, 0.38, 0.23, 0.21, 0.078),
)


def lock(name, root, reach, rise, width, thick, phase):
    up, fwd = Vector((0, 0, 1)), Vector((0, -1, 0))
    pts = pc.bezier(root - up * 0.06 - fwd * 0.03, root + up * (rise * 0.7) + fwd * (reach * 0.2),
                    root + up * (rise * 1.15) + fwd * (reach * 0.6),
                    root + up * (rise * 0.9) + fwd * reach, 20)
    n = len(pts)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        pts[i] = pts[i] + Vector((0.02 * math.sin(t * 6.0 + phase) * t, 0, -0.05 * max(0.0, t - 0.75) / 0.25 * t + 0.02 * math.sin(t * 9.0 + phase) * t * t))
        w = width * (0.7 + 0.6 * math.sin(math.pi * min(1.0, t * 1.3)) ** 0.7) * (1 - t) ** 0.5
        th = thick * (0.8 + 0.4 * math.sin(math.pi * min(1.0, t * 1.4))) * (1 - t) ** 0.7
        radii.append((max(0.006, w), max(0.006, th)))
    return pc.tube(name, pts, radii, sides=12, side_hint=(1, 0, 0))


def build(arm):
    parts = []
    for i, (x, y, reach, rise, width, thick) in enumerate(LOCKS):
        root, _ = pc.head_top(x, y)
        parts.append(lock("lock%d" % i, root, reach, rise, width, thick, i * 1.7))
    obj = pc.join(parts, "Horn_Mechon")
    pc.shade_smooth(obj)
    pc.skin_like(obj, arm, "Horn")
    return [obj]

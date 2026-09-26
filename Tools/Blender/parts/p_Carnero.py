from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)

KEYS = ((0.24, -0.40, 1.12), (0.28, -0.36, 1.52), (0.36, -0.12, 1.70), (0.47, 0.10, 1.52),
        (0.54, 0.10, 1.20), (0.56, -0.06, 1.00), (0.55, -0.21, 0.93))


def catmull(keys, per=6):
    k = [Vector(p) for p in keys]
    k = [k[0] * 2 - k[1]] + k + [k[-1] * 2 - k[-2]]
    out = []
    for i in range(1, len(k) - 2):
        p0, p1, p2, p3 = k[i - 1], k[i], k[i + 1], k[i + 2]
        for s in range(per):
            t = s / per
            out.append(0.5 * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (3 * p1 - p0 - 3 * p2 + p3) * t ** 3))
    out.append(k[-2])
    return out


def build(arm):
    pts = catmull(KEYS)
    n = len(pts)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        r = 0.125 * (1 - 0.72 * t) + 0.018
        radii.append((r * 1.15, r * 0.9))
    right = pc.tube("Horn_Carnero_R", pts, radii, sides=14, side_hint=(1, 0, 0))
    left = pc.mirror_x(right, "Horn_Carnero_L")
    for o in (right, left):
        pc.skin_like(o, arm, "Horn")
    return [right, left]

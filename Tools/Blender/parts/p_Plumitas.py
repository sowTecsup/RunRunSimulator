import math
from mathutils import Vector
import part_common as pc

SLOTS = ("Wing",)

FEATHERS = ((80, 0.46, 0.100), (58, 0.43, 0.098), (36, 0.38, 0.092), (15, 0.31, 0.082))


def feather(name, root, d, n, L, W):
    back = n.cross(d).normalized()
    if back.y < 0:
        back = -back
    pts = pc.bezier(root - d * 0.05, root + d * (L * 0.35), root + d * (L * 0.7) + back * (L * 0.06),
                    root + d * L + back * (L * 0.14), 14)
    radii = []
    for i in range(len(pts)):
        t = i / (len(pts) - 1)
        s = t ** 0.8
        w = W * math.sin(math.pi * s) ** 0.55 + 0.006
        radii.append((0.022 * (1 - 0.55 * t) + 0.004, w))
    return pc.tube(name, pts, radii, sides=12, side_hint=n)


def wing(side):
    s = 1 if side == "L" else -1
    root = Vector((0.47 * s, -0.08, 1.07))
    out, back, up = Vector((s, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
    parts = []
    for i, (ang, L, W) in enumerate(FEATHERS):
        a = math.radians(ang)
        d = (up * math.sin(a) + back * math.cos(a) + out * 0.5).normalized()
        n = (out - d * out.dot(d)).normalized()
        base = root + n * (0.014 * i) + back * (0.012 * i)
        parts.append(feather("f%d" % i, base, d, n, L, W))
    dm = (up * 0.7 + back * 0.7 + out * 0.5).normalized()
    nm = (out - dm * out.dot(dm)).normalized()
    parts.append(feather("cov", root + nm * 0.07, dm, nm, 0.22, 0.095))
    return pc.join(parts, "Wing_Plumitas_%s" % side)


def build(arm):
    objs = [wing("L"), wing("R")]
    for o in objs:
        pc.skin_like(o, arm, "Wing")
    return objs

import math
from mathutils import Vector
import part_common as pc
from p_Unicornio import RAINBOW

SLOTS = ("Back",)

TIP_Y = 1.03
LENGTH = 0.31
FAN = (
    (-10, -0.022, 1.00),
    (-3, 0.020, 0.98),
    (4, -0.016, 0.94),
    (11, 0.014, 0.88),
    (18, -0.006, 0.78),
)


def tip_root():
    p, _ = pc.surface_point((0, TIP_Y, 3.0), (0, 0, -1))
    q, _ = pc.surface_point((0, TIP_Y, -3.0), (0, 0, 1))
    return Vector((0, TIP_Y, (p.z + q.z) * 0.5))


def lock(name, pts, width, thick):
    radii = []
    n = len(pts)
    for i in range(n):
        t = i / (n - 1)
        w = width * (0.6 + 0.55 * math.sin(math.pi * min(1.0, t * 1.25))) * (1 - t) ** 0.55 + 0.006
        th = thick * (1 - 0.75 * t) ** 1.2 + 0.006
        radii.append((th, w))
    return pc.tube(name, pts, radii, sides=10, side_hint=(1, 0, 0))


def build(arm):
    objs = []
    root = tip_root()
    for i, (elev, dx, k) in enumerate(FAN):
        e = math.radians(elev)
        d = Vector((0, math.cos(e), math.sin(e)))
        up = Vector((0, -math.sin(e), math.cos(e)))
        length = LENGTH * k
        p0 = root + Vector((dx * 0.4, -0.05, -0.025 + 0.013 * i))
        tip = root + Vector((dx, 0.32 - 0.026 * i, 0.06 + 0.043 * i))
        p1 = p0 + d * length * 0.45 - up * length * (0.17 - 0.035 * i)
        p2 = tip + Vector((0, -0.085, -0.06))
        p3 = tip
        pts = pc.bezier(p0, p1, p2, p3, 14)
        objs.append(lock("Deco_%s_borla%d" % (RAINBOW[i], i), pts, 0.098, 0.078))
    for o in objs:
        pc.skin_like(o, arm, "Back")
    return objs

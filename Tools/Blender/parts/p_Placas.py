import math
from mathutils import Vector, Matrix
import part_common as pc

SLOTS = ("Back",)


def gem(name, center, normal, R, T, roll):
    ax = Vector((1, 0, 0))
    pts = [center + ax * x for x in (-T / 2, -T * 0.3, T * 0.3, T / 2)]
    radii = [R * 0.72, R, R, R * 0.72]
    obj = pc.tube(name, pts, radii, sides=6, side_hint=(0, math.sin(roll), math.cos(roll)))
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj


def build(arm):
    plates = []
    n = 7
    for i in range(n):
        t = i / (n - 1)
        y = -0.28 + t * 1.18
        p, nrm = pc.surface_point((0, y, 3.0), (0, 0, -1))
        R = 0.14 - 0.08 * t ** 1.2
        c = p + nrm * R * 0.62 + Vector((0.025 * (1 if i % 2 else -1), 0, 0))
        plates.append(gem("plate%d" % i, c, nrm, R, R * 0.7, 0.35 * (1 if i % 2 else -1) + math.atan2(nrm.y, nrm.z)))
    obj = pc.join(plates, "Back_Placas")
    pc.skin_like(obj, arm, "Back")
    return [obj]

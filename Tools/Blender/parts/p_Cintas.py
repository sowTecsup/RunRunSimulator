import math, bmesh
from mathutils import Vector
import part_common as pc

SLOTS = ("Wing",)

BACK = Vector((0, 1, 0))
UP = Vector((0, 0, 1))
OUT = Vector((1, 0, 0))

WIDTH = 0.15
THICK = 0.018
TIP = 0.006
ROWS, SIDES = 40, 10


def smooth(t):
    t = max(0.0, min(1.0, t))
    return t * t * (3 - 2 * t)


def ribbon(name, s):
    out = OUT * s
    root = Vector((0.44 * s, -0.08, 1.08))
    path = pc.bezier(root, root + out * 0.20 + UP * 0.12 + BACK * 0.08,
                     root + out * 0.30 + BACK * 0.50 + UP * 0.20,
                     root + out * 0.36 + BACK * 1.10 + UP * 0.40, ROWS + 1)
    n = len(path)
    for i in range(n):
        t = i / (n - 1)
        path[i] = path[i] + UP * 0.085 * math.sin(2 * math.pi * 1.4 * t) * smooth(t / 0.25) \
            + out * 0.04 * math.sin(2 * math.pi * 1.4 * t + 1.2) * smooth(t / 0.25)
    bm = bmesh.new()
    rings = []
    for i in range(n):
        t = i / (n - 1)
        p = path[i]
        tan = (path[min(i + 1, n - 1)] - path[max(i - 1, 0)]).normalized()
        side = (UP - tan * UP.dot(tan)).normalized()
        nrm = tan.cross(side).normalized()
        roll = 0.35 * math.sin(2 * math.pi * 1.4 * t + 0.6)
        wdir = side * math.cos(roll) + nrm * math.sin(roll)
        tdir = nrm * math.cos(roll) - side * math.sin(roll)
        w = WIDTH * (0.5 + 0.5 * smooth(t / 0.2)) * (1 - smooth((t - 0.6) / 0.4) ** 1.4)
        w = max(TIP, w)
        th = max(TIP * 0.8, THICK * (1 - 0.6 * t))
        ring = []
        for k in range(SIDES):
            a = 2 * math.pi * k / SIDES
            ring.append(bm.verts.new(p + wdir * math.cos(a) * w + tdir * math.sin(a) * th))
        rings.append(ring)
    for a, b in zip(rings, rings[1:]):
        for k in range(SIDES):
            bm.faces.new((a[k], a[(k + 1) % SIDES], b[(k + 1) % SIDES], b[k]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = pc.new_object(name, bm)
    pc.shade_smooth(obj, 80)
    return obj


def build(arm):
    objs = [ribbon("Wing_Cintas_L", 1), ribbon("Wing_Cintas_R", -1)]
    for o in objs:
        pc.skin_like(o, arm, "Wing")
    return objs

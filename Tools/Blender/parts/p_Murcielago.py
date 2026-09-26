import math
import bmesh
from mathutils import Vector
import part_common as pc

SLOTS = ("Wing",)

ROOT2 = Vector((-0.02, 0.0))
WRIST = Vector((0.08, 0.2))
TIPS = (Vector((0.46, 0.29)), Vector((0.44, 0.1)), Vector((0.3, -0.03)))
TAIL = Vector((0.08, -0.05))
DEPTH = 0.42


def quad(a, b, c, n):
    return [a * (1 - t) ** 2 + c * 2 * t * (1 - t) + b * t * t for t in (i / (n - 1) for i in range(n))]


def outline2d():
    pts = []
    arm_ctrl = (ROOT2 + WRIST) / 2 + Vector((-0.035, 0.0))
    pts += quad(ROOT2, WRIST, arm_ctrl, 6)[:-1]
    lead_ctrl = (WRIST + TIPS[0]) / 2 + Vector((-0.02, 0.04))
    pts += quad(WRIST, TIPS[0], lead_ctrl, 7)[:-1]
    ends = list(TIPS) + [TAIL]
    for a, b in zip(ends, ends[1:]):
        mid = (a + b) / 2
        ctrl = mid + (WRIST - mid) * DEPTH
        pts += quad(a, b, ctrl, 8)[:-1]
    pts += quad(TAIL, ROOT2, (TAIL + ROOT2) / 2, 3)[:-1]
    return pts


def frame(s):
    out, back, up = Vector((s, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
    B = (back * 0.95 + out * 0.25).normalized()
    U = (up * 0.85 + out * 0.45)
    U = (U - B * U.dot(B)).normalized()
    N = B.cross(U).normalized()
    if N.x * s < 0:
        N = -N
    return B, U, N


def mapper(s):
    root = Vector((0.45 * s, -0.08, 1.05))
    B, U, N = frame(s)

    def to3(p, lift=0.0):
        bend = -0.35 * max(p.x, 0.0) ** 2 - 0.25 * max(p.y - 0.1, 0.0) ** 2
        return root + B * p.x + U * p.y + N * (bend + lift)
    return to3, B, U, N


def membrane(name, to3):
    ol = outline2d()
    c2 = Vector((0.19, 0.11))
    rings = 4
    bm = bmesh.new()
    cv = bm.verts.new(to3(c2))
    grid = []
    for k in range(1, rings + 1):
        f = k / rings
        grid.append([bm.verts.new(to3(c2 + (p - c2) * f)) for p in ol])
    n = len(ol)
    for i in range(n):
        bm.faces.new((cv, grid[0][i], grid[0][(i + 1) % n]))
    for a, b in zip(grid, grid[1:]):
        for i in range(n):
            j = (i + 1) % n
            bm.faces.new((a[i], b[i], b[j], a[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = pc.new_object(name, bm)
    for p in obj.data.polygons:
        p.use_smooth = True
    mod = obj.modifiers.new("solid", "SOLIDIFY")
    mod.thickness = 0.016
    mod.offset = 0
    pc.apply_transforms(obj)
    return obj


def rod(name, pts, r0, r1, sides=10, taper=1.0):
    n = len(pts)
    radii = [r1 + (r0 - r1) * (1 - i / (n - 1)) ** taper for i in range(n)]
    radii[-1] = 0.006
    return pc.tube(name, pts, radii, sides=sides)


def knuckle(name, center, r, axis):
    pts, radii = [], []
    for i in range(7):
        a = math.pi * i / 6
        pts.append(center + axis * (-math.cos(a) * r))
        radii.append(max(math.sin(a) * r, 0.004))
    return pc.tube(name, pts, radii, sides=10)


def wing(s):
    to3, B, U, N = mapper(s)
    lift = 0.006
    parts = [membrane("Wing_Murcielago_mem", to3)]
    arm_ctrl = (ROOT2 + WRIST) / 2 + Vector((-0.035, 0.0))
    arm = [to3(p, lift) for p in quad(ROOT2 + Vector((0.0, -0.07)), WRIST, arm_ctrl, 7)]
    arm[0] = arm[0] - N * 0.03
    parts.append(pc.tube("arm", arm, [0.05, 0.048, 0.045, 0.042, 0.04, 0.039, 0.039], sides=12))
    lead_ctrl = (WRIST + TIPS[0]) / 2 + Vector((-0.02, 0.04))
    parts.append(rod("f0", [to3(p, lift) for p in quad(WRIST, TIPS[0], lead_ctrl, 9)], 0.036, 0.012, taper=0.8))
    for i, t in enumerate(TIPS[1:]):
        ctrl = (WRIST + t) / 2 + Vector((0.0, 0.025))
        parts.append(rod("f%d" % (i + 1), [to3(p, lift) for p in quad(WRIST, t, ctrl, 9)], 0.032, 0.011, taper=0.8))
    parts.append(knuckle("kn", to3(WRIST, lift), 0.05, U))
    thumb = [to3(WRIST + Vector((-0.011 * k, 0.012 * k - 0.0012 * k * k)), lift + 0.002 * k) for k in range(6)]
    parts.append(rod("thumb", thumb, 0.03, 0.008, sides=8))
    return pc.join(parts, "Wing_Murcielago_%s" % ("L" if s > 0 else "R"))


def build(arm):
    left = wing(1)
    right = pc.mirror_x(left, "Wing_Murcielago_R")
    objs = [left, right]
    for o in objs:
        pc.skin_like(o, arm, "Wing")
    return objs

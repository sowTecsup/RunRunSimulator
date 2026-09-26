import math, bmesh
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)

RAINBOW = ("F26B6B", "F5A15A", "F5D65A", "8FD16A", "6AB8E8", "A98BE0")


def spiral_band(name, base, axis, length, r0, t0, t1, rings=10, sides=16):
    axis = axis.normalized()
    side = axis.cross(Vector((1, 0, 0))).normalized()
    up = side.cross(axis).normalized()
    bm = bmesh.new()
    loops = []
    for i in range(rings + 1):
        t = t0 + (t1 - t0) * i / rings
        r = r0 * (1 - 0.55 * t) if t < 0.999 else 0.0
        c = base + axis * length * t
        ring = []
        for k in range(sides):
            a = 2 * math.pi * k / sides
            rr = r * (1 + 0.05 * math.sin(2 * a - t * 26)) * (0.93 + 0.07 * math.sin(math.pi * ((t - t0) / (t1 - t0))))
            ring.append(bm.verts.new(c + side * math.cos(a) * rr + up * math.sin(a) * rr))
        loops.append(ring)
    for a, b in zip(loops, loops[1:]):
        for k in range(sides):
            bm.faces.new((a[k], a[(k + 1) % sides], b[(k + 1) % sides], b[k]))
    if t0 == 0:
        bm.faces.new(list(reversed(loops[0])))
    if t1 >= 0.999:
        tip = bm.verts.new(base + axis * (length + r0 * 0.25))
        for k in range(sides):
            bm.faces.new((loops[-1][k], loops[-1][(k + 1) % sides], tip))
    else:
        bm.faces.new(loops[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = pc.new_object(name, bm)
    pc.shade_smooth(obj)
    return obj


def strand(name, pts, width, thick):
    radii = []
    n = len(pts)
    for i in range(n):
        t = i / (n - 1)
        w = width * (0.55 + 0.9 * math.sin(math.pi * min(1, t * 1.15)) ** 0.8) * (1 - t) ** 0.5 + 0.004
        radii.append((w, thick * (1 - 0.6 * t) + 0.003))
    return pc.tube(name, pts, radii, sides=10, side_hint=(1, 0, 0))


def build(arm):
    objs = []
    base, nrm = pc.head_top(0, -0.6)
    axis = Vector((0, -0.55, 0.83)).normalized()
    base = base - axis * 0.03
    n = len(RAINBOW)
    for i, hexc in enumerate(RAINBOW):
        objs.append(spiral_band("Deco_%s_horn%d" % (hexc, i), base, axis, 0.72, 0.1, i / n, (i + 1) / n))
    root, _ = pc.head_top(0, -0.46)
    for i, hexc in enumerate(RAINBOW[:5]):
        h = 0.02 + i * 0.055
        reach = 0.40 + i * 0.03
        pts = pc.bezier(root + Vector((0, 0.03 * i, -0.05)), root + Vector((0, -0.05, h + 0.14)),
                        root + Vector((0, reach * 0.55, h + 0.14)), root + Vector((0, reach, h + 0.17)), 16)
        objs.append(strand("Deco_%s_mane%d" % (hexc, i), pts, 0.16 - i * 0.012, 0.075))
    horn = [o for o in objs if "horn" in o.name]
    mane = [o for o in objs if "mane" in o.name]
    for o in horn:
        pc.skin_like(o, arm, "Horn")
    for o in mane:
        pc.skin_like(o, arm, "Back")
    return objs

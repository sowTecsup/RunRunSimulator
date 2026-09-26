import bmesh
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)


def bipyramid(name, corners, center, half_thick):
    bm = bmesh.new()
    ring = [bm.verts.new(Vector(c)) for c in corners]
    side = Vector((1, 0, 0)) * half_thick
    right = bm.verts.new(Vector(center) + side)
    left = bm.verts.new(Vector(center) - side)
    n = len(ring)
    for k in range(n):
        a, b = ring[k], ring[(k + 1) % n]
        bm.faces.new((a, b, right))
        bm.faces.new((b, a, left))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = pc.new_object(name, bm)
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj


def build(arm):
    kc = Vector((0, -0.62, 1.19))
    kite = [kc + (Vector(v) - kc) * 1.1 for v in ((0, -0.66, 0.84), (0, -0.87, 1.18), (0, -0.58, 1.55), (0, -0.27, 1.22))]
    parts = [bipyramid("kite", kite, kc, 0.17)]
    fin = [Vector((0, -0.10, 1.24)),
           Vector((0, -0.20, 1.44)),
           Vector((0, -0.19, 1.51)),
           Vector((0, -0.33, 1.58)),
           Vector((0, -0.30, 1.65)),
           Vector((0, -0.45, 1.73)),
           Vector((0, -0.44, 1.81)),
           Vector((0, -0.34, 1.85)),
           Vector((0, -0.18, 1.82)),
           Vector((0, 0.00, 1.72)),
           Vector((0, 0.18, 1.56)),
           Vector((0, 0.28, 1.46)),
           Vector((0, 0.08, 1.40)),
           Vector((0, 0.00, 1.24))]
    parts.append(bipyramid("fin", fin, Vector((0, -0.12, 1.58)), 0.04))
    obj = pc.join(parts, "Horn_Cometa")
    pc.skin_like(obj, arm, "Horn")
    return [obj]

import bmesh
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)

OUTLINE = ((-0.06, 0.62), (0.56, 0.06), (0.50, 0.44), (1.0, 0.34), (0.36, 0.92), (0.42, 0.56), (-0.06, 0.78))


def bolt(name, root, fwd, up, L, W, T):
    fwd = fwd.normalized()
    up = (up - fwd * up.dot(fwd)).normalized()
    nrm = fwd.cross(up).normalized()
    v0 = (OUTLINE[0][1] + OUTLINE[-1][1]) / 2
    bm = bmesh.new()
    front = [bm.verts.new(Vector((u * L, (v0 - v) * W, T / 2))) for u, v in OUTLINE]
    back = [bm.verts.new(Vector((u * L, (v0 - v) * W, -T / 2))) for u, v in OUTLINE]
    n = len(OUTLINE)
    caps = [bm.faces.new(front), bm.faces.new(list(reversed(back)))]
    for k in range(n):
        bm.faces.new((front[k], back[k], back[(k + 1) % n], front[(k + 1) % n]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    rim = [e for e in bm.edges if all(abs(abs(v.co.z) - T / 2) < 1e-6 for v in e.verts)]
    bmesh.ops.bevel(bm, geom=rim, offset=T * 0.38, segments=2, profile=0.5, affect="EDGES", clamp_overlap=True)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4], quad_method="BEAUTY", ngon_method="EAR_CLIP")
    for v in bm.verts:
        u, w, z = v.co
        z *= max(0.25, 1 - 0.75 * max(0.0, u / L))
        v.co = root + fwd * u + up * w + nrm * z
    obj = pc.new_object(name, bm)
    pc.shade_smooth(obj, 35)
    return obj


def build(arm):
    p, nrm = pc.surface_point((1.5, -0.18, 2.3), Vector((-1.0, 0, -0.8)))
    root = p - nrm * 0.07
    fwd = Vector((0.28, -1.0, -0.12))
    up = Vector((0.18, 0, 1.0))
    left = bolt("Horn_Rayo_L", root, fwd, up, 1.08, 0.44, 0.085)
    pc.apply_transforms(left)
    right = pc.mirror_x(left, "Horn_Rayo_R")
    for o in (left, right):
        pc.skin_like(o, arm, "Horn")
    return [left, right]

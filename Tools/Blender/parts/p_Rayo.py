import math
import bmesh
from mathutils import Vector, Matrix
import part_common as pc

SLOTS = ("Horn",)

SPINE = ((0.0, 0.0), (0.50, 0.15), (0.42, -0.05), (1.0, 0.04))
HALF = (0.064, 0.064, 0.064, 0.004)


def _offset_line(a, b, h):
    d = (b - a).normalized()
    n = Vector((-d.y, d.x))
    return a + n * h, d


def _intersect(p, d, q, e):
    den = d.x * e.y - d.y * e.x
    if abs(den) < 1e-6:
        return (p + q) / 2
    t = ((q.x - p.x) * e.y - (q.y - p.y) * e.x) / den
    return p + d * t


def _side(pts, halves, sign):
    out = []
    segs = []
    for i in range(len(pts) - 1):
        a, d = _offset_line(pts[i], pts[i + 1], sign * halves[i])
        segs.append((a, d))
    first_n = Vector((-segs[0][1].y, segs[0][1].x))
    out.append(pts[0] + first_n * sign * halves[0])
    for i in range(1, len(pts) - 1):
        out.append(_intersect(segs[i - 1][0], segs[i - 1][1], segs[i][0], segs[i][1]))
    last_d = segs[-1][1]
    out.append(pts[-1] + Vector((-last_d.y, last_d.x)) * sign * halves[-1])
    return out


def outline():
    pts = [Vector(p) for p in SPINE]
    left = _side(pts, HALF, 1)
    right = _side(pts, HALF, -1)
    return left + list(reversed(right))


def bolt(name, root, fwd, up, T):
    fwd = fwd.normalized()
    up = (up - fwd * up.dot(fwd)).normalized()
    nrm = fwd.cross(up).normalized()
    poly = outline()
    L = SPINE[-1][0]
    bm = bmesh.new()
    front = [bm.verts.new(Vector((p.x, p.y, T / 2))) for p in poly]
    back = [bm.verts.new(Vector((p.x, p.y, -T / 2))) for p in poly]
    n = len(poly)
    bm.faces.new(front)
    bm.faces.new(list(reversed(back)))
    for k in range(n):
        bm.faces.new((front[k], back[k], back[(k + 1) % n], front[(k + 1) % n]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    rim = [e for e in bm.edges if all(abs(abs(v.co.z) - T / 2) < 1e-6 for v in e.verts)]
    bmesh.ops.bevel(bm, geom=rim, offset=T * 0.34, segments=2, profile=0.5, affect="EDGES", clamp_overlap=True)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4], quad_method="BEAUTY", ngon_method="EAR_CLIP")
    for v in bm.verts:
        u, w, z = v.co
        z *= 1 - 0.45 * max(0.0, u / L)
        v.co = root + fwd * u + up * w + nrm * z
    obj = pc.new_object(name, bm)
    pc.shade_smooth(obj, 35)
    return obj


def build(arm):
    p, nrm = pc.surface_point((1.5, -0.36, 2.4), Vector((-1.0, 0, -0.95)))
    root = p - nrm * 0.06 - Vector((0, 0, 0.0))
    fwd = Vector((0.7, -1.0, 0.38))
    tilt = math.radians(15)
    up0 = Vector((0, 0, 1.0))
    up = Matrix.Rotation(-tilt, 3, fwd.normalized()) @ up0
    left = bolt("Horn_Rayo_L", root - fwd.normalized() * 0.06, fwd, up, 0.045)
    pc.apply_transforms(left)
    right = pc.mirror_x(left, "Horn_Rayo_R")
    for o in (left, right):
        pc.skin_like(o, arm, "Horn")
    return [left, right]

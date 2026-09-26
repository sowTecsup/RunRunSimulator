import math
import bmesh
from mathutils import Vector
import part_common as pc

SLOTS = ("Back",)

RIM = [(-0.56, 1.05), (-0.54, 1.37), (-0.40, 1.64), (-0.15, 1.81), (0.18, 1.82), (0.48, 1.66),
       (0.77, 1.38), (0.98, 1.00), (1.09, 0.62), (1.07, 0.40)]
INNER = [(-0.30, 1.00), (0.74, 0.42)]
N = 81
J = 9
K = 6
PLEATS = 16


def catmull(pts, n):
    pts = [Vector((0, y, z)) for y, z in pts]
    ext = [pts[0] * 2 - pts[1]] + pts + [pts[-1] * 2 - pts[-2]]
    dense = []
    for i in range(1, len(ext) - 2):
        p0, p1, p2, p3 = ext[i - 1], ext[i], ext[i + 1], ext[i + 2]
        for k in range(20):
            t = k / 20
            dense.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                                + (-p0 + 3 * p1 - 3 * p2 + p3) * t ** 3))
    dense.append(pts[-1])
    acc = [0.0]
    for a, b in zip(dense, dense[1:]):
        acc.append(acc[-1] + (b - a).length)
    out, j = [], 0
    for i in range(n):
        s = acc[-1] * i / (n - 1)
        while j < len(acc) - 2 and acc[j + 1] < s:
            j += 1
        f = (s - acc[j]) / max(acc[j + 1] - acc[j], 1e-9)
        out.append(dense[j].lerp(dense[j + 1], f))
    return out


def build(arm):
    rim = catmull(RIM, N)
    a, b = Vector((0, *INNER[0])), Vector((0, *INNER[1]))
    X = Vector((1, 0, 0))
    bm = bmesh.new()
    loops = []
    for i in range(N):
        u = i / (N - 1)
        base, tip = a.lerp(b, u), rim[i]
        d = (tip - base).normalized()
        wave = math.sin(2 * math.pi * PLEATS * u)
        end = min(u, 1 - u)
        fade = min(1.0, end / 0.04)

        def mid(s):
            return base.lerp(tip, s) + X * (0.014 * s ** 1.3 * wave * fade)

        def half(s):
            return 0.072 - 0.03 * s

        plus = [mid(j / (J - 1)) + X * half(j / (J - 1)) for j in range(J)]
        hr = half(1.0)
        cap = [mid(1.0) + d * hr * math.sin(math.pi * k / K) + X * hr * math.cos(math.pi * k / K) for k in range(1, K)]
        minus = [mid(j / (J - 1)) - X * half(j / (J - 1)) for j in reversed(range(J))]
        loops.append([bm.verts.new(p) for p in plus + cap + minus])
    M = len(loops[0])
    for la, lb in zip(loops, loops[1:]):
        for k in range(M):
            bm.faces.new((la[k], la[(k + 1) % M], lb[(k + 1) % M], lb[k]))
    bm.faces.new(list(reversed(loops[0])))
    bm.faces.new(loops[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = pc.new_object("Back_Abanico", bm)
    pc.shade_smooth(obj, 50)
    pc.skin_like(obj, arm, "Back")
    return [obj]

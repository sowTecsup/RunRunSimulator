import bpy, bmesh, sys, os, math
import numpy as np
from mathutils import Vector, Quaternion
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform

ARGS = sys.argv[sys.argv.index("--") + 1:]
OUT_DIR, FBX_DIR, MARK_IMAGE = ARGS[0], ARGS[1], ARGS[2]
MODE = ARGS[3] if len(ARGS) > 3 else "egg"

SOURCES = {
    "Egg_Shell": "Dragon_body",
    "Egg_Horn_Nubs": "Horn",
    "Egg_Wing_Buds": "Wing_A",
    "Egg_Wing_Frills": "Wing_A",
}


def world_bbox(points):
    lo = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    hi = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return lo, hi


def normalize(p, lo, hi):
    return Vector(((p.x - lo.x) / max(hi.x - lo.x, 1e-6), (p.y - lo.y) / max(hi.y - lo.y, 1e-6), (p.z - lo.z) / max(hi.z - lo.z, 1e-6)))


class Source:
    def __init__(self, ob, exclude=(), marks=None, covers=None):
        bm = bmesh.new()
        bm.from_mesh(ob.data)
        bm.transform(ob.matrix_world)
        dl = bm.verts.layers.deform.active
        names = {g.index: g.name for g in ob.vertex_groups}
        if exclude and dl:
            def limb(v):
                d = v[dl]
                return bool(d) and names[max(d.items(), key=lambda kv: kv[1])[0]].startswith(exclude)
            bmesh.ops.delete(bm, geom=[f for f in bm.faces if any(limb(v) for v in f.verts)], context="FACES")
        if covers is not None:
            hidden = [f for f in bm.faces if covers.find_nearest(f.calc_center_median())[3] < 0.05]
            print("HIDDEN tris removed=%d" % len(hidden))
            bmesh.ops.delete(bm, geom=hidden, context="FACES")
        bmesh.ops.triangulate(bm, faces=bm.faces)
        bm.faces.ensure_lookup_table()
        self.lo, self.hi = world_bbox([v.co for v in bm.verts])
        uv = bm.loops.layers.uv.active
        self.tris, self.uvs = [], []
        for f in bm.faces:
            self.tris.append([normalize(l.vert.co, self.lo, self.hi) for l in f.loops])
            self.uvs.append([Vector((l[uv].uv.x, l[uv].uv.y, 0)) for l in f.loops])
        if marks is not None:
            h, w = marks.shape
            def dark(t, u):
                if sum(p.y for p in t) / 3 > 0.25:
                    return False
                pts = list(u) + [sum(u, Vector()) / 3]
                return min(marks[min(h - 1, max(0, int(p.y * h))), min(w - 1, max(0, int(p.x * w)))] for p in pts) < 0.45
            keep = [i for i in range(len(self.tris)) if not dark(self.tris[i], self.uvs[i])]
            print("NOSTRIL tris removed=%d" % (len(self.tris) - len(keep)))
            self.tris = [self.tris[i] for i in keep]
            self.uvs = [self.uvs[i] for i in keep]
        verts = [p for t in self.tris for p in t]
        self.tree = BVHTree.FromPolygons(verts, [(3 * i, 3 * i + 1, 3 * i + 2) for i in range(len(self.tris))])
        bm.free()
        self.build_islands()

    def build_islands(self, res=512):
        parent = list(range(len(self.uvs)))
        def find(i):
            while parent[i] != i:
                parent[i] = parent[parent[i]]
                i = parent[i]
            return i
        edges = {}
        for i, u in enumerate(self.uvs):
            keys = [(round(p.x, 4), round(p.y, 4)) for p in u]
            for a_ in range(3):
                e = tuple(sorted((keys[a_], keys[(a_ + 1) % 3])))
                if e in edges:
                    parent[find(i)] = find(edges[e])
                else:
                    edges[e] = i
        self.tri_label = [find(i) for i in range(len(self.uvs))]
        lab = np.full((res, res), -1, dtype=np.int64)
        for i, u in enumerate(self.uvs):
            xs = [p.x * res for p in u]
            ys = [p.y * res for p in u]
            x0, x1 = max(0, int(min(xs))), min(res - 1, int(max(xs)) + 1)
            y0, y1 = max(0, int(min(ys))), min(res - 1, int(max(ys)) + 1)
            if x1 < x0 or y1 < y0:
                continue
            gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
            (ax, ay), (bx, by), (cx, cy) = zip(xs, ys)
            den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
            if abs(den) < 1e-9:
                continue
            l1 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den
            l2 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den
            inside = (l1 >= -0.02) & (l2 >= -0.02) & (1 - l1 - l2 >= -0.02)
            sub = lab[y0:y1 + 1, x0:x1 + 1]
            sub[inside] = self.tri_label[i]
        for _ in range(2):
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                shifted = np.roll(np.roll(lab, dy, axis=0), dx, axis=1)
                fill = (lab < 0) & (shifted >= 0)
                lab[fill] = shifted[fill]
        self.labels = lab
        print("ISLANDS=%d coverage=%.2f" % (len(set(self.tri_label)), (lab >= 0).mean()))

    def leaves_island(self, idx, uvs):
        res = self.labels.shape[0]
        want = self.tri_label[idx]
        for u, v in grid_points(uvs):
            if u < 0 or u > 1 or v < 0 or v > 1:
                return True
            if self.labels[min(res - 1, int(v * res)), min(res - 1, int(u * res))] != want:
                return True
        return False

    def uv_at(self, tri_index, p):
        t, u = self.tris[tri_index], self.uvs[tri_index]
        r = barycentric_transform(p, t[0], t[1], t[2], u[0], u[1], u[2])
        return r.x, r.y


def grid_points(uvs):
    pts = []
    for i in range(1, len(uvs) - 1):
        for a_ in range(7):
            for b_ in range(7 - a_):
                wa, wb = a_ / 6, b_ / 6
                pts.append((uvs[0][0] * (1 - wa - wb) + uvs[i][0] * wa + uvs[i + 1][0] * wb,
                            uvs[0][1] * (1 - wa - wb) + uvs[i][1] * wa + uvs[i + 1][1] * wb))
    return pts


def dark(mask, uvs):
    h, w = mask.shape
    for u, v in uvs:
        if u < 0 or u > 1 or v < 0 or v > 1:
            return True
    for i in range(1, len(uvs) - 1):
        (ax, ay), (bx, by), (cx, cy) = [(p[0] * w, p[1] * h) for p in (uvs[0], uvs[i], uvs[i + 1])]
        x0, x1 = max(0, int(min(ax, bx, cx)) - 1), min(w - 1, int(max(ax, bx, cx)) + 1)
        y0, y1 = max(0, int(min(ay, by, cy)) - 1), min(h - 1, int(max(ay, by, cy)) + 1)
        den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(den) < 1e-9:
            continue
        gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        l1 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den
        l2 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den
        inside = (l1 >= -0.05) & (l2 >= -0.05) & (1 - l1 - l2 >= -0.05)
        if inside.any() and mask[y0:y1 + 1, x0:x1 + 1][inside].min() < 0.45:
            return True
    return False

FLOOR = {"egg": 0.25, "slime": 0.3}


def floor(p):
    f = FLOOR[MODE]
    p.z = f + (1 - f) * p.z
    return p


def plain_bottom(target, corner=(0.03, 0.03), size=0.04):
    me = target.data
    uv = me.uv_layers.active.data
    mw = target.matrix_world
    lo, hi = world_bbox([mw @ v.co for v in me.vertices])
    n = 0
    for poly in me.polygons:
        if (mw.to_3x3() @ poly.normal).z > -0.5:
            continue
        n += 1
        for li in poly.loop_indices:
            p = normalize(mw @ me.vertices[me.loops[li].vertex_index].co, lo, hi)
            uv[li].uv = (corner[0] + (p.x - 0.5) * size, corner[1] + (p.y - 0.5) * size)
    print("BOTTOM plain faces=%d" % n)


def seams(target):
    me = target.data
    uv = me.uv_layers.active.data
    mw = target.matrix_world
    at = {}
    for poly in me.polygons:
        for li in poly.loop_indices:
            at.setdefault(me.loops[li].vertex_index, []).append(uv[li].uv.copy())
    bands = {}
    for vi, us in at.items():
        b = min(4, int((mw @ me.vertices[vi].co).z / 0.05))
        split = max((a - c).length for a in us for c in us) > 0.01
        n, k = bands.get(b, (0, 0))
        bands[b] = (n + 1, k + split)
    print("SEAMS " + " ".join("z%.2f:%d/%d" % (b * 0.05, bands[b][1], bands[b][0]) for b in sorted(bands)))


def transfer(target, src, mask=None):
    me = target.data
    mw = target.matrix_world
    lo, hi = world_bbox([mw @ v.co for v in me.vertices])
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    uv = me.uv_layers.active.data
    moved = unresolved = 0
    for poly in me.polygons:
        center = floor(normalize(mw @ poly.center, lo, hi))
        corners = [floor(normalize(mw @ me.vertices[me.loops[li].vertex_index].co, lo, hi)) for li in poly.loop_indices]
        idx = src.tree.find_nearest(center)[2]
        uvs = [src.uv_at(idx, p) for p in corners]
        snout = (center.y < 0.45 and 0.05 < center.z < 0.9) if MODE == "slime" else (center.y < 0.35 and 0.05 < center.z < 0.7)
        bad = (lambda q, t: src.leaves_island(t, q) or dark(mask[1], q) or (snout and dark(mask[0], q))) if mask is not None else None
        if mask is not None and bad(uvs, idx):
            moved += 1
            for k in [0.03 * i for i in range(1, 16)]:
                found = False
                for off in (Vector((0, 0, k)), Vector((0, 0, -k)), Vector((k, 0, 0)), Vector((-k, 0, 0)), Vector((0, k, 0))):
                    idx2 = src.tree.find_nearest(center + off)[2]
                    cand = [src.uv_at(idx2, p + off) for p in corners]
                    if not bad(cand, idx2):
                        uvs, found = cand, True
                        break
                if found:
                    break
            if not found:
                unresolved += 1
        for li, u in zip(poly.loop_indices, uvs):
            uv[li].uv = u
    print("NOSE faces lifted=%d unresolved=%d" % (moved, unresolved))
    if MODE == "slime":
        plain_bottom(target)
    seams(target)


MINI_PARTS = [("A", "HornA", "Egg_Horn_A", 0.55, 0.75, 25), ("B", "HornB", "Egg_Horn_B", 0.55, 0.75, 25),
              ("C", "HornC", "Egg_Horn_C", 0.55, 0.75, 25), ("D", "HornD", "Egg_Horn_D", 0.55, 0.75, 25),
              ("A", "BackA", "Egg_Back_A", 1.0, 1.0, 0), ("D", "BackB", "Egg_Back_B", 1.0, 1.0, 0)]

def strip_constant_marks(ob, faces_dir):
    mask = None
    for fname in sorted(os.listdir(faces_dir)):
        if not fname.lower().endswith(".png"):
            continue
        im = bpy.data.images.load(os.path.join(faces_dir, fname))
        a = np.array(im.pixels[:], dtype=np.float32).reshape(im.size[1], im.size[0], 4)
        dark = (a[:, :, 3] > 0.5) & (a[:, :, :3].mean(axis=2) < 0.4)
        mask = dark if mask is None else (mask & dark)
        bpy.data.images.remove(im)
    h, w = mask.shape
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    uv = bm.loops.layers.uv.active
    doomed = []
    for f in bm.faces:
        us = [l[uv].uv for l in f.loops]
        hit = False
        for i in range(1, len(us) - 1):
            for a_ in range(6):
                for b_ in range(6 - a_):
                    wa, wb = a_ / 5, b_ / 5
                    p = us[0] * (1 - wa - wb) + us[i] * wa + us[i + 1] * wb
                    if mask[min(h - 1, max(0, int(p.y * h))), min(w - 1, max(0, int(p.x * w)))]:
                        hit = True
        if hit:
            doomed.append(f)
    bmesh.ops.delete(bm, geom=doomed, context="FACES")
    bm.to_mesh(ob.data)
    bm.free()
    print("FACE constant-mark polys removed=%d of mask px=%d" % (len(doomed), int(mask.sum())))


def place_rigid(part, body, shell, name, s, lift):
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bm.transform(body.matrix_world)
    body_tree = BVHTree.FromBMesh(bm)
    blo, bhi = world_bbox([v.co for v in bm.verts])
    bm.free()
    shell_tree = BVHTree.FromObject(shell, bpy.context.evaluated_depsgraph_get())
    slo, shi = world_bbox([shell.matrix_world @ v.co for v in shell.data.vertices])
    sdim, bdim = shi - slo, bhi - blo
    center = (slo + shi) / 2
    me = part.data.copy()
    mw = part.matrix_world
    pb = bmesh.new()
    pb.from_mesh(me)
    pb.transform(mw)
    seen, islands = set(), []
    for v in pb.verts:
        if v.index in seen:
            continue
        stack, isl = [v], []
        seen.add(v.index)
        while stack:
            a = stack.pop()
            isl.append(a)
            for e in a.link_edges:
                b = e.other_vert(a)
                if b.index not in seen:
                    seen.add(b.index)
                    stack.append(b)
        islands.append(isl)
    groups = []
    for isl in islands:
        cen = sum((v.co for v in isl), Vector()) / len(isl)
        for grp in groups:
            if (grp[0] - cen).length < 0.25:
                grp[1].extend(isl)
                break
        else:
            groups.append([cen, list(isl)])
    for _, vs in groups:
        ds = []
        for v in vs:
            b, nb, _, _ = body_tree.find_nearest(v.co)
            ds.append((v.co - b).dot(nb))
        lo, hi = min(ds), max(ds)
        root_vs = [v for v, d in zip(vs, ds) if d <= min(0.0, lo + 0.05 * (hi - lo))] or [v for v, d in zip(vs, ds) if d <= lo + 0.05 * (hi - lo)]
        anchor = sum((v.co for v in root_vs), Vector()) / len(root_vs)
        bD, nD, _, _ = body_tree.find_nearest(anchor)
        nrm = Vector(((bD.x - blo.x) / bdim.x, (bD.y - blo.y) / bdim.y, 1 - (1 - (bD.z - blo.z) / bdim.z) * lift))
        e0 = Vector((slo.x + nrm.x * sdim.x, slo.y + nrm.y * sdim.y, slo.z + nrm.z * sdim.z))
        eS, nS, _, _ = shell_tree.ray_cast(center, (e0 - center).normalized())
        if nS.dot(eS - center) < 0:
            nS = -nS
        rot = Quaternion()
        for v in vs:
            v.co = eS + rot @ ((v.co - bD) * s)
        target = sum(1 for d in ds if d < 0) / len(ds)
        base = sum((v.co for v in root_vs), Vector()) / len(root_vs)
        nb = shell_tree.find_nearest(base)[1]
        if nb.dot(base - center) < 0:
            nb = -nb
        orig = [v.co.copy() for v in vs]
        def inside_at(off):
            return sum(1 for p in orig if shell_tree.find_nearest(p + nb * off)[1].dot(p + nb * off - shell_tree.find_nearest(p + nb * off)[0]) < 0) / len(orig)
        lo_o, hi_o = -0.08, 0.08
        for _ in range(18):
            mid_o = (lo_o + hi_o) / 2
            if inside_at(mid_o) > target:
                lo_o = mid_o
            else:
                hi_o = mid_o
        for v, p in zip(vs, orig):
            v.co = p + nb * hi_o
        print("SINK %s target_inside=%.2f got=%.2f" % (name, target, inside_at(hi_o)))
    pb.to_mesh(me)
    pb.free()
    me.materials.clear()
    for m in shell.data.materials:
        me.materials.append(m)
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.parent = shell
    print("RIGID %-8s <- %-6s groups=%d dims=%s" % (name, part.name, len(groups), tuple(round(x, 3) for x in me.vertices and ob.dimensions)))
    return ob

def make_scales(shell, around=6, rows=3, lift=0.0012):
    me = shell.data.copy()
    me.uv_layers.remove(me.uv_layers[0])
    uvl = me.uv_layers.new(name="UVMap")
    for v in me.vertices:
        v.co = v.co + v.normal * lift
    zs = [v.co.z for v in me.vertices]
    z0, z1 = min(zs), max(zs)
    for poly in me.polygons:
        us = []
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            us.append(((math.atan2(co.y, co.x) / (2 * math.pi)) + 0.5) * around)
        if max(us) - min(us) > around / 2:
            us = [u + around if u < around / 2 else u for u in us]
        for li, u in zip(poly.loop_indices, us):
            co = me.vertices[me.loops[li].vertex_index].co
            uvl.data[li].uv = (u, (co.z - z0) / (z1 - z0) * rows)
    mat = bpy.data.materials.new("EggScales")
    me.materials.clear()
    me.materials.append(mat)
    ob = bpy.data.objects.new("Egg_Scales", me)
    bpy.context.collection.objects.link(ob)
    ob.parent = shell
    print("SCALES layer verts=%d" % len(me.vertices))
    return ob

SLIME_PARTS = [("A", "HornA", "Horn_A", 1.0, 0.6, 0), ("B", "HornB", "Horn_B", 1.0, 0.25, 0),
               ("C", "HornC", "Horn_C", 1.0, 0.6, 0), ("D", "HornD", "Horn_D", 1.0, 0.6, 0),
               ("A", "Face", "Face", 1.0, 1.45, 0)]
GROW = {}


def grow(ob, shell, g):
    tree = BVHTree.FromObject(shell, bpy.context.evaluated_depsgraph_get())
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    seen, islands = set(), []
    for v in bm.verts:
        if v.index in seen:
            continue
        stack, isl = [v], []
        seen.add(v.index)
        while stack:
            a = stack.pop()
            isl.append(a)
            for e in a.link_edges:
                b = e.other_vert(a)
                if b.index not in seen:
                    seen.add(b.index)
                    stack.append(b)
        islands.append(isl)
    groups = []
    for isl in islands:
        c = sum((v.co for v in isl), Vector()) / len(isl)
        for grp in groups:
            if (grp[0] - c).length < 0.04:
                grp[1].extend(isl)
                break
        else:
            groups.append([c, list(isl)])
    for _, vs in groups:
        ds = []
        for v in vs:
            loc, n, _, _ = tree.find_nearest(v.co)
            ds.append((v.co - loc).dot(n))
        lo, hi = min(ds), max(ds)
        root = [v.co.copy() for v, d in zip(vs, ds) if d <= lo + 0.25 * (hi - lo)]
        anchor = sum(root, Vector()) / len(root)
        for v in vs:
            v.co = anchor + (v.co - anchor) * g
    bm.to_mesh(ob.data)
    bm.free()
    print("GROW %-8s x%.2f groups=%d dims=%s" % (ob.name, g, len(groups), tuple(round(x, 3) for x in ob.dimensions)))


def soften(ob, iterations):
    mod = ob.modifiers.new("Soft", "SMOOTH")
    mod.factor = 0.8
    mod.iterations = iterations
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    ob.modifiers.remove(mod)
    ob.data = me
    for p in me.polygons:
        p.use_smooth = True


def conform(part, body, shell, name, k=1.0, lift=1.0, shrink=1.0):
    bm = bmesh.new()
    bm.from_mesh(body.data)
    bm.transform(body.matrix_world)
    body_tree = BVHTree.FromBMesh(bm)
    blo, bhi = world_bbox([v.co for v in bm.verts])
    bm.free()
    shell_tree = BVHTree.FromObject(shell, bpy.context.evaluated_depsgraph_get())
    slo, shi = world_bbox([shell.matrix_world @ v.co for v in shell.data.vertices])
    sdim, bdim = shi - slo, bhi - blo
    scale = sum(sdim[i] / bdim[i] for i in range(3)) / 3
    center = (slo + shi) / 2
    me = part.data.copy()
    mw = part.matrix_world
    samples = []
    for v in me.vertices:
        p = mw @ v.co
        b, nb, _, _ = body_tree.find_nearest(p)
        off = p - b
        d = off.dot(nb)
        nrm = Vector(((b.x - blo.x) / bdim.x, (b.y - blo.y) / bdim.y, 1 - (1 - (b.z - blo.z) / bdim.z) * lift))
        samples.append((v, d, off - nb * d, nrm))
    mid = sum((s_[3] for s_ in samples), Vector()) / len(samples)
    for v, d, lateral, nrm in samples:
        nrm = Vector((mid.x + (nrm.x - mid.x) * shrink, nrm.y, mid.z + (nrm.z - mid.z) * shrink))
        e0 = Vector((slo.x + nrm.x * sdim.x, slo.y + nrm.y * sdim.y, slo.z + nrm.z * sdim.z))
        hit, ne, _, _ = shell_tree.ray_cast(center, (e0 - center).normalized())
        if hit is None:
            hit, ne = e0, (e0 - center).normalized()
        if ne.dot(hit - center) < 0:
            ne = -ne
        v.co = hit + ne * (d * scale * k) + lateral * (scale * k * shrink)
    if name != "Face":
        me.materials.clear()
        for m in shell.data.materials:
            me.materials.append(m)
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    ob.parent = shell
    print("MINI %-12s <- %-8s verts=%d dims=%s scale=%.3f" % (name, part.name, len(me.vertices), tuple(round(x, 3) for x in ob.dimensions), scale))
    return ob


def import_new(path):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    return {o.name.split(".")[0]: o for o in bpy.data.objects if o not in before and o.type == "MESH"}


def main():
    for o in [o for o in bpy.data.objects if o.name.startswith("Egg_") and o.name != "Egg_Shell"]:
        bpy.data.objects.remove(o, do_unlink=True)
    shell = bpy.data.objects["Egg_Shell"]
    dragons = {L: import_new(os.path.join(FBX_DIR, "DragonSD_%s.fbx" % L)) for L in "ABCD"}
    body = dragons["A"]["Dragon_body"]
    def load(pid):
        img = bpy.data.images.load(MARK_IMAGE.replace("_00", "_" + pid))
        return np.array(img.pixels[:], dtype=np.float32).reshape(img.size[1], img.size[0], 4)[:, :, 0]
    nose = load("00")
    px = sum(load(pid) for pid in ("01", "09", "16", "25")) / 4.0
    fb = bmesh.new()
    fb.from_mesh(dragons["A"]["Face"].data)
    fb.transform(dragons["A"]["Face"].matrix_world)
    covers = BVHTree.FromBMesh(fb)
    fb.free()
    transfer(shell, Source(body, ("Leg", "Arm", "Tail") if MODE == "egg" else ("Leg", "Arm", "Tail3", "Tail4")), (nose, px))
    keep = {"Egg_Shell"}
    if MODE == "egg":
        keep.add(make_scales(shell).name)
    wanted = {}
    for letter, key, name, k, lift, soft in (SLIME_PARTS if MODE == "slime" else MINI_PARTS):
        if MODE == "slime" and name.startswith("Horn_"):
            ob = place_rigid(dragons[letter][key], body, shell, name, 0.208, lift)
        else:
            ob = conform(dragons[letter][key], body, shell, name, k, lift, 0.5 if (MODE == "slime" and name == "Face") else 1.0)
        if soft:
            soften(ob, soft)
        if MODE == "slime" and name == "Face":
            strip_constant_marks(ob, os.path.join(os.path.dirname(os.path.dirname(MARK_IMAGE)), "Faces"))
        if MODE == "slime" and name in GROW:
            grow(ob, shell, GROW[name])
        keep.add(ob.name)
        wanted[name] = ob
    for o in list(bpy.data.objects):
        if o.name not in keep:
            bpy.data.objects.remove(o, do_unlink=True)
    for name, ob in wanted.items():
        ob.name = name
    base = "MonchiSlime" if MODE == "slime" else "MonchiEgg"
    if MODE == "slime":
        shell.name = "Slime_Body"
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, base + "_Skin.blend"))
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_DIR, base + ".fbx"), use_selection=False,
                             apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True,
                             object_types={"MESH"}, mesh_smooth_type="FACE", path_mode="STRIP")
    print("SKIN_DONE")


main()
import bpy, bmesh, math, sys, os
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT_DIR = ARGS[0] if ARGS else os.path.dirname(__file__)
PATTERN = ARGS[1] if len(ARGS) > 1 else ""
SHAPE = ARGS[2] if len(ARGS) > 2 else "egg"
NAME = "MonchiSlime" if SHAPE == "slime" else "MonchiEgg"

R, H, A = 0.13, 0.16, 0.12
NT, NP = 32, 48
FRONT = -math.pi / 2


def slime_point(t, phi):
    s, c = math.sin(t), math.cos(t)
    hb, ht, t0 = 0.0835, 0.1305, 0.94
    p = 3.0 if c > 0 else 2.1
    r = 0.15 * abs(s) ** (2 / p)
    z = max(0.0, hb - hb / t0 * abs(c) ** (2 / p)) if c > 0 else hb + ht * abs(c) ** (2 / p)
    return Vector((r * math.cos(phi), r * math.sin(phi), z))


def egg_point(t, phi):
    if SHAPE == "slime":
        return slime_point(t, phi)
    r = R * math.sin(t) * (1.0 + A * math.cos(t))
    return Vector((r * math.cos(phi), r * math.sin(phi), H - math.cos(t) * H))


def egg_normal(t, phi):
    e = 1e-4
    p = egg_point(t, phi)
    dt = egg_point(min(t + e, math.pi - 1e-5), phi) - p
    dp = egg_point(t, phi + e) - p
    n = dp.cross(dt).normalized()
    c = p - Vector((0, 0, 0.1 if SHAPE == "slime" else H))
    return n if n.dot(c) > 0 else -n


def material(name, rgba, image=None):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.45
    if image is None:
        bsdf.inputs["Base Color"].default_value = rgba
        return m
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = image
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.inputs[6].default_value = (0.98, 0.93, 0.80, 1)
    mix.inputs[7].default_value = rgba
    nt.links.new(tex.outputs["Color"], mix.inputs[0])
    nt.links.new(mix.outputs[2], bsdf.inputs["Base Color"])
    return m


def to_object(bm, name, mat, parent=None):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    me.materials.append(mat)
    if parent:
        ob.parent = parent
    return ob


def build_shell(mat):
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    bottom = bm.verts.new(egg_point(0, 0))
    top = bm.verts.new(egg_point(math.pi, 0))
    rings = []
    for i in range(1, NT):
        t = math.pi * i / NT
        rings.append([bm.verts.new(egg_point(t, 2 * math.pi * j / NP)) for j in range(NP)])

    def uvset(face, coords):
        for loop, c in zip(face.loops, coords):
            loop[uv].uv = c

    for j in range(NP):
        j2 = (j + 1) % NP
        u0, u1 = j / NP, (j + 1) / NP
        f = bm.faces.new((bottom, rings[0][j2], rings[0][j]))
        uvset(f, ((u0 + 0.5 / NP, 0), (u1, 1 / NT), (u0, 1 / NT)))
        for i in range(NT - 2):
            v0, v1 = (i + 1) / NT, (i + 2) / NT
            f = bm.faces.new((rings[i][j], rings[i][j2], rings[i + 1][j2], rings[i + 1][j]))
            uvset(f, ((u0, v0), (u1, v0), (u1, v1), (u0, v1)))
        f = bm.faces.new((rings[-1][j], rings[-1][j2], top))
        uvset(f, ((u0, (NT - 1) / NT), (u1, (NT - 1) / NT), (u0 + 0.5 / NP, 1)))
    bm.normal_update()
    return to_object(bm, "Egg_Shell", mat)


def tube(bm, base, normal, bend_dir, length, radius_fn, bend=0.0, flat=1.0, sides=12, rings=8, sink=0.012):
    n = normal.normalized()
    b = (bend_dir - n * bend_dir.dot(n)).normalized()
    side = n.cross(b).normalized()
    start = base - n * sink
    prev, verts_rings = None, []
    for k in range(rings + 1):
        s = k / rings
        c = start + _bent(n, b, length, bend, s)
        fwd = (n * math.cos(bend * s) + b * math.sin(bend * s)).normalized()
        u = fwd.cross(side).normalized()
        r = radius_fn(s)
        ring = []
        if r < 1e-5:
            verts_rings.append([bm.verts.new(c)])
            continue
        for j in range(sides):
            a = 2 * math.pi * j / sides
            ring.append(bm.verts.new(c + side * (math.cos(a) * r) + u * (math.sin(a) * r * flat)))
        verts_rings.append(ring)
    for k in range(len(verts_rings) - 1):
        r0, r1 = verts_rings[k], verts_rings[k + 1]
        if len(r1) == 1:
            for j in range(sides):
                bm.faces.new((r0[j], r0[(j + 1) % sides], r1[0]))
        else:
            for j in range(sides):
                j2 = (j + 1) % sides
                bm.faces.new((r0[j], r0[j2], r1[j2], r1[j]))
    bm.faces.new(list(reversed(verts_rings[0])))


def _bent(n, b, length, bend, s):
    if abs(bend) < 1e-4:
        return n * (s * length)
    rad = length / bend
    return n * (math.sin(bend * s) * rad) + b * ((1 - math.cos(bend * s)) * rad)


def cone(tip=0.25, exp=1.0):
    return lambda s: (1 - s) ** exp * (1 - tip) + tip * (1 - s) if s < 1 else 0.0


def dome(s):
    return math.sqrt(max(0.0, 1 - s * s))


def piece(name, mat, parent, parts):
    bm = bmesh.new()
    for p in parts:
        tube(bm, **p)
    bm.normal_update()
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return to_object(bm, name, mat, parent)


def at(t_frac, phi):
    t = math.pi * t_frac
    return egg_point(t, phi), egg_normal(t, phi)


def up():
    return Vector((0, 0, 1))


def back():
    return Vector((math.cos(FRONT + math.pi), math.sin(FRONT + math.pi), 0))


def horn_parts():
    out = []
    for sgn in (-1, 1):
        p, n = at(0.80, FRONT + sgn * 0.62)
        out.append(dict(base=p, normal=n, bend_dir=back() + up() * 0.3, length=0.055,
                        radius_fn=lambda s: 0.020 * (1 - s) ** 1.1 + 0.002, bend=0.9))
    return out


def ridge_parts(kind):
    out = []
    ts = [0.78, 0.68, 0.58, 0.48, 0.38]
    for i, tf in enumerate(ts):
        k = 1.0 - i * 0.13
        p, n = at(tf, FRONT + math.pi)
        if kind == "Spikes":
            out.append(dict(base=p, normal=n, bend_dir=-up(), length=0.046 * k,
                            radius_fn=lambda s, k=k: 0.018 * k * (1 - s) + 0.0015, bend=0.5, sides=10))
        elif kind == "Plates":
            out.append(dict(base=p, normal=n, bend_dir=-up(), length=0.040 * k,
                            radius_fn=lambda s, k=k: 0.027 * k * dome(s), flat=0.35, sides=14))
        elif kind == "Bumps":
            out.append(dict(base=p, normal=n, bend_dir=-up(), length=0.030 * k,
                            radius_fn=lambda s, k=k: 0.021 * k * dome(s), sink=0.010, sides=14))
    if kind == "Fin":
        for i, tf in enumerate([0.74, 0.62, 0.50]):
            p, n = at(tf, FRONT + math.pi)
            out.append(dict(base=p, normal=n, bend_dir=-up(), length=0.052 - i * 0.010,
                            radius_fn=lambda s: 0.030 * (1 - s) ** 0.8 + 0.001, flat=0.22, bend=0.7, sides=14))
    return out


def wing_parts(kind):
    out = []
    for sgn in (-1, 1):
        phi = FRONT + sgn * math.pi / 2
        if kind == "Buds":
            p, n = at(0.47, phi)
            out.append(dict(base=p, normal=n, bend_dir=back() + up() * 0.25, length=0.052,
                            radius_fn=lambda s: 0.026 * math.sin(math.pi * min(1.0, 0.25 + s * 0.75)) + 0.001,
                            flat=0.20, bend=1.9, sides=14))
        elif kind == "Frills":
            for i, off in enumerate((-0.22, 0.0, 0.22)):
                p, n = at(0.55 + off * 0.25, phi + sgn * off * 0.6)
                out.append(dict(base=p, normal=n, bend_dir=back() + up() * (0.4 + off), length=0.030 - abs(off) * 0.02,
                                radius_fn=lambda s: 0.014 * (1 - s) ** 0.7 + 0.001, flat=0.28, bend=1.1, sides=12))
    return out


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    img = bpy.data.images.load(PATTERN) if PATTERN and os.path.exists(PATTERN) else None
    shell_mat = material("EggShell", (0.55, 0.78, 0.95, 1), img)
    detail_mat = material("EggDetail", (0.30, 0.45, 0.75, 1))
    shell = build_shell(shell_mat)
    pieces = [piece("Egg_Horn_Nubs", detail_mat, shell, horn_parts())]
    for kind in ("Spikes", "Plates", "Bumps", "Fin"):
        pieces.append(piece("Egg_Back_" + kind, detail_mat, shell, ridge_parts(kind)))
    for kind in ("Buds", "Frills"):
        pieces.append(piece("Egg_Wing_" + kind, detail_mat, shell, wing_parts(kind)))

    dg = bpy.context.evaluated_depsgraph_get()
    tree = BVHTree.FromObject(shell, dg)
    print("EGG shell verts=%d tris=%d dims=%s" % (len(shell.data.vertices),
          sum(len(p.vertices) - 2 for p in shell.data.polygons), tuple(round(d, 3) for d in shell.dimensions)))
    for ob in pieces:
        inside, peak = 0, 0.0
        for v in ob.data.vertices:
            loc, nrm, idx, dist = tree.find_nearest(v.co)
            d = (v.co - loc).dot(nrm)
            inside += d < 0
            peak = max(peak, d)
        shells = len([1 for _ in ob.data.polygons])
        print("EGG %-18s verts=%4d faces=%4d dims=%s embedded_verts=%d protrude_mm=%.1f" % (ob.name, len(ob.data.vertices), shells,
              tuple(round(d, 3) for d in ob.dimensions), inside, peak * 1000))

    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, NAME + ".blend"))
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_DIR, NAME + ".fbx"), use_selection=False,
                             apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True,
                             object_types={"MESH"}, mesh_smooth_type="FACE", path_mode="STRIP")
    print("EGG_DONE")


main()

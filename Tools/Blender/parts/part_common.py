import bpy, bmesh, os, sys, math
from mathutils import Vector, Matrix

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
FBX_DIR = os.path.join(ROOT, "Assets", "Suriyun", "Dragons_SD", "FBX")

SLOT_SOURCE = {"Horn": "HornA", "Back": "BackA", "Wing": "Wing_A"}
BAKED = ("HornA", "HornB", "HornC", "HornD", "BackA", "BackB", "Wing_A")

BODY_COLOR = (0.93, 0.86, 0.78, 1)
ACCENT_COLOR = (0.95, 0.62, 0.45, 1)
WING_COLOR = (0.98, 0.78, 0.55, 1)


def load_dragon(letter="A"):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(FBX_DIR, "DragonSD_%s.fbx" % letter))
    arm = next(o for o in bpy.data.objects if o.type == "ARMATURE")
    bpy.context.view_layer.update()
    return arm


def mesh(name):
    return bpy.data.objects.get(name)


def bone_head(arm, bone):
    return arm.matrix_world @ arm.data.bones[bone].head_local


def surface_point(origin, direction, target="Dragon_body"):
    obj = mesh(target)
    inv = obj.matrix_world.inverted()
    o = inv @ Vector(origin)
    d = (inv.to_3x3() @ Vector(direction)).normalized()
    hit, loc, nor, _ = obj.ray_cast(o, d)
    if not hit:
        return None, None
    return obj.matrix_world @ loc, (obj.matrix_world.to_3x3() @ nor).normalized()


def head_top(offset_x=0.0, offset_y=-0.35):
    p, n = surface_point((offset_x, offset_y, 3.0), (0, 0, -1))
    return p, n


def new_object(name, bm):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def shade_smooth(obj, angle=60):
    for p in obj.data.polygons:
        p.use_smooth = True
    try:
        mod = obj.modifiers.new("smooth", "SMOOTH_BY_ANGLE")
        mod["Input_1"] = math.radians(angle)
    except Exception:
        pass


def tube(name, points, radii, sides=12, cap=True, twist=0.0, side_hint=None):
    bm = bmesh.new()
    rings = []
    n = len(points)
    prev_side = None
    for i, (p, r) in enumerate(zip(points, radii)):
        p = Vector(p)
        if i == 0:
            t = (Vector(points[1]) - p).normalized()
        elif i == n - 1:
            t = (p - Vector(points[i - 1])).normalized()
        else:
            t = (Vector(points[i + 1]) - Vector(points[i - 1])).normalized()
        if prev_side is None and side_hint is not None:
            h = Vector(side_hint)
            side = (h - t * h.dot(t)).normalized()
        elif prev_side is None:
            ref = Vector((0, 0, 1)) if abs(t.z) < 0.9 else Vector((1, 0, 0))
            side = t.cross(ref).normalized()
        else:
            side = (prev_side - t * prev_side.dot(t)).normalized()
        prev_side = side
        up = side.cross(t).normalized()
        ring = []
        rx, ry = (r, r) if not isinstance(r, (tuple, list)) else r
        for k in range(sides):
            a = 2 * math.pi * k / sides + twist * i
            ring.append(bm.verts.new(p + side * math.cos(a) * rx + up * math.sin(a) * ry))
        rings.append(ring)
    for a, b in zip(rings, rings[1:]):
        for k in range(sides):
            bm.faces.new((a[k], a[(k + 1) % sides], b[(k + 1) % sides], b[k]))
    if cap:
        bm.faces.new(list(reversed(rings[0])))
        bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = new_object(name, bm)
    shade_smooth(obj)
    return obj


def bezier(p0, p1, p2, p3, n):
    out = []
    for i in range(n):
        t = i / (n - 1)
        u = 1 - t
        out.append(Vector(p0) * u ** 3 + Vector(p1) * 3 * u * u * t + Vector(p2) * 3 * u * t * t + Vector(p3) * t ** 3)
    return out


def mirror_x(obj, name=None):
    dup = obj.copy()
    dup.data = obj.data.copy()
    dup.name = name or obj.name + "_R"
    bpy.context.scene.collection.objects.link(dup)
    for v in dup.data.vertices:
        v.co.x = -v.co.x
    dup.data.flip_normals()
    return dup


def join(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    objs[0].name = name
    objs[0].data.name = name
    return objs[0]


def apply_transforms(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for m in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)


def skin_like(obj, arm, slot, rigid_bone=None):
    apply_transforms(obj)
    obj.vertex_groups.clear()
    if rigid_bone:
        g = obj.vertex_groups.new(name=rigid_bone)
        g.add([v.index for v in obj.data.vertices], 1.0, "REPLACE")
    else:
        src = mesh(SLOT_SOURCE[slot])
        for sg in src.vertex_groups:
            obj.vertex_groups.new(name=sg.name)
        mod = obj.modifiers.new("dt", "DATA_TRANSFER")
        mod.object = src
        mod.use_vert_data = True
        mod.data_types_verts = {"VGROUP_WEIGHTS"}
        mod.vert_mapping = "NEAREST"
        mod.layers_vgroup_select_src = "ALL"
        mod.layers_vgroup_select_dst = "NAME"
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm
    obj.parent = arm
    obj.matrix_parent_inverse = arm.matrix_world.inverted()
    return obj


def paint(obj, rgba):
    mat = bpy.data.materials.new(obj.name + "_mat")
    mat.diffuse_color = rgba
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def hide_baked(slot):
    for n in BAKED:
        o = mesh(n)
        if o and n.startswith(SLOT_SOURCE[slot][:4]):
            o.hide_render = True
            o.hide_set(True)


def setup_render(path, res=(600, 600), view="side"):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "MATERIAL"
    sc.display.shading.show_cavity = True
    sc.display.shading.show_object_outline = True
    sc.render.film_transparent = False
    sc.world = sc.world or bpy.data.worlds.new("w")
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.filepath = path
    for o in bpy.data.objects:
        if o.type == "MESH" and not o.data.materials:
            paint(o, BODY_COLOR)
        elif o.type == "MESH" and o.name in ("Dragon_body", "Teech"):
            paint(o, BODY_COLOR)
        elif o.type == "MESH" and o.name == "Face":
            o.hide_render = True
        elif o.type == "MESH" and o.name.startswith("Wing_A"):
            paint(o, WING_COLOR)
        elif o.type == "MESH" and o.name in BAKED:
            paint(o, ACCENT_COLOR)
    cam_data = bpy.data.cameras.new("cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 2.6
    cam = bpy.data.objects.new("cam", cam_data)
    sc.collection.objects.link(cam)
    views = {
        "side": ((6.0, -2.6, 2.3), (0, 0.1, 0.8)),
        "front": ((1.8, -6.5, 2.4), (0, 0, 0.8)),
        "top": ((2.5, -1.5, 7.0), (0, 0.1, 0.9)),
        "back": ((-5.0, 4.5, 3.0), (0, 0.1, 0.8)),
    }
    pos, target = views[view]
    cam.location = pos
    d = (Vector(target) - Vector(pos))
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    sc.camera = cam
    bpy.ops.render.render(write_still=True)


def export_part(arm, objs, path):
    bpy.ops.object.select_all(action="DESELECT")
    arm.select_set(True)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
                             add_leaf_bones=False, bake_anim=False, apply_scale_options="FBX_SCALE_ALL",
                             bake_space_transform=True, mesh_smooth_type="FACE")


def args():
    return sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []

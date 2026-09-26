import bpy, math
from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)

LOBES = (
    (0.00, -0.46, 0.28, 0.85),
    (0.00, -0.12, 0.22, 0.85),
    (0.22, -0.28, 0.19, 0.55),
    (-0.22, -0.28, 0.19, 0.55),
    (0.16, -0.52, 0.17, 0.55),
    (-0.16, -0.52, 0.17, 0.55),
    (0.00, 0.10, 0.17, 0.45),
    (0.20, 0.00, 0.15, 0.40),
    (-0.20, 0.00, 0.15, 0.40),
)

FRONT_FLOOR = 1.10


def lobe_center(x, y, r, lift):
    p, n = pc.surface_point((x, y, 3.0), (0, 0, -1))
    c = p + n * r * lift
    if y < -0.3:
        c.z = max(c.z, FRONT_FLOOR + r * 0.9)
    return c


def build(arm):
    mb = bpy.data.metaballs.new("LanaMeta")
    mb.resolution = 0.045
    mb.render_resolution = 0.045
    mb.threshold = 0.6
    for x, y, r, lift in LOBES:
        r *= 0.95
        el = mb.elements.new()
        el.co = lobe_center(x, y, r, lift)
        el.radius = r * 1.45
        el.stiffness = 2.4
    ob = bpy.data.objects.new("LanaMeta", mb)
    bpy.context.scene.collection.objects.link(ob)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.view_layer.objects.active
    obj.name = "Horn_Lana"
    obj.data.name = "Horn_Lana"
    if len(obj.data.vertices) > 2400:
        mod = obj.modifiers.new("dec", "DECIMATE")
        mod.ratio = 2300 / len(obj.data.vertices)
        bpy.ops.object.modifier_apply(modifier=mod.name)
    pc.shade_smooth(obj)
    pc.skin_like(obj, arm, "Horn")
    return [obj]

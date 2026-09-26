import bpy
from mathutils import Vector
import part_common as pc

SLOTS = ("Back",)

Y0, Y1 = -0.22, 1.02
R0, R1 = 0.125, 0.045
OVERLAP = 0.97
SINK = 0.38
FIELD = 0.608


def spine(n=240):
    ys = [Y0 + (Y1 - Y0) * k / n for k in range(n + 1)]
    pts = [pc.surface_point((0, y, 3.0), (0, 0, -1)) for y in ys]
    arc = [0.0]
    for a, b in zip(pts, pts[1:]):
        arc.append(arc[-1] + (b[0] - a[0]).length)
    return pts, arc


def at(pts, arc, s):
    k = next((j for j, v in enumerate(arc) if v >= s), len(arc) - 1)
    return pts[k]


def build(arm):
    pts, arc = spine()
    bumps = []
    s, r = 0.0, R0
    while s <= arc[-1]:
        t = s / arc[-1]
        r = R0 + (R1 - R0) * t ** 1.15
        bumps.append((s, r))
        nxt = R0 + (R1 - R0) * min(1.0, (s + 2 * r * OVERLAP) / arc[-1]) ** 1.15
        s += (r + nxt) * OVERLAP
    mb = bpy.data.metaballs.new("LomoLanaMeta")
    mb.resolution = 0.02
    mb.render_resolution = 0.02
    mb.threshold = 0.6
    for s, r in bumps:
        p, n = at(pts, arc, s)
        el = mb.elements.new()
        el.co = p + n * r * (1 - 2 * SINK)
        el.type = "ELLIPSOID"
        side = Vector((1, 0, 0))
        fwd = n.cross(side).normalized()
        side = fwd.cross(n).normalized()
        from mathutils import Matrix
        el.rotation = Matrix((side, fwd, n)).transposed().to_quaternion()
        el.radius = r / FIELD
        el.size_x = 1.3
        el.size_y = 1.0
        el.size_z = 0.9
        el.stiffness = 3.0
    ob = bpy.data.objects.new("LomoLanaMeta", mb)
    bpy.context.scene.collection.objects.link(ob)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.view_layer.objects.active
    obj.name = "Back_LomoLana"
    obj.data.name = "Back_LomoLana"
    if len(obj.data.vertices) > 2400:
        mod = obj.modifiers.new("dec", "DECIMATE")
        mod.ratio = 2300 / len(obj.data.vertices)
        bpy.ops.object.modifier_apply(modifier=mod.name)
    pc.shade_smooth(obj)
    pc.skin_like(obj, arm, "Back")
    return [obj]



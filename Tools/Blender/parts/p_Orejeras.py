import bmesh
from mathutils import Vector, Matrix
import part_common as pc
from p_Carnero import catmull

SLOTS = ("Horn",)

KEYS = ((0.10, -0.14, 1.26), (0.18, -0.30, 1.52), (0.38, -0.52, 1.46), (0.50, -0.58, 1.22), (0.53, -0.54, 1.02))


def cup(name, center, out, R, T):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=12, radius=1.0)
    obj = pc.new_object(name, bm)
    obj.scale = (T, R, R)
    obj.location = center
    obj.rotation_euler = out.to_track_quat("X", "Z").to_euler()
    pc.shade_smooth(obj)
    pc.apply_transforms(obj)
    return obj


def build(arm):
    pts = catmull(KEYS)
    n = len(pts)
    radii = [(0.10 - 0.02 * i / (n - 1), 0.055) for i in range(n)]
    band = pc.tube("band", pts, radii, sides=12, side_hint=(0, 1, 0))
    c = cup("cup", Vector((0.55, -0.52, 0.98)), Vector((1, -0.55, 0)).normalized(), 0.19, 0.12)
    right = pc.join([band, c], "Horn_Orejeras_R")
    left = pc.mirror_x(right, "Horn_Orejeras_L")
    for o in (right, left):
        pc.skin_like(o, arm, "Horn")
    return [right, left]

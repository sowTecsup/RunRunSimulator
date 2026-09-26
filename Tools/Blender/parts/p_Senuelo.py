import bmesh
from mathutils import Vector
import part_common as pc
from p_Carnero import catmull

SLOTS = ("Horn",)

BALL = Vector((0.0, -1.02, 1.18))
BALL_R = 0.17


def ball(name, center, r, squash):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=14, radius=1.0)
    obj = pc.new_object(name, bm)
    obj.scale = (r, r, r * squash)
    obj.location = center
    pc.shade_smooth(obj)
    pc.apply_transforms(obj)
    return obj


def build(arm):
    base, _ = pc.head_top(0.0, -0.28)
    keys = (base - Vector((0, 0, 0.06)), base + Vector((0, -0.05, 0.16)), (0.0, -0.58, 1.61),
            (0.0, -0.90, 1.56), (0.0, -1.03, 1.40), BALL + Vector((0, 0, BALL_R * 0.5)))
    pts = catmull(keys)
    n = len(pts)
    radii = [(0.115 - 0.035 * i / (n - 1), 0.1 - 0.03 * i / (n - 1)) for i in range(n)]
    stalk = pc.tube("stalk", pts, radii, sides=12, side_hint=(1, 0, 0))
    bulb = ball("bulb", BALL, BALL_R, 0.85)
    obj = pc.join([stalk, bulb], "Horn_Senuelo")
    pc.skin_like(obj, arm, "Horn")
    return [obj]

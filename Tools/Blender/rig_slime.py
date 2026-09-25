import bpy
import math
import os
import sys
from mathutils import Vector
from mathutils.kdtree import KDTree

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT_DIR = ARGS[0] if ARGS else bpy.path.abspath("//")
FPS = 30
TOP_Z = 0.12
BLEND = (0.07, 0.17)
BASE_R, SIDE_R = 0.095, 0.15


def smooth(a, b, x):
    t = min(1.0, max(0.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def wave(t, cycles=1.0, phase=0.0):
    return math.sin(2 * math.pi * (t * cycles + phase))


def bump(t, a, b):
    if t <= a or t >= b:
        return 0.0
    return math.sin(math.pi * (t - a) / (b - a))


def ease(t, a, b):
    return smooth(a, b, t)


def vol(h):
    return 1.0 / math.sqrt(max(h, 0.05))


def pose(root_loc=(0, 0, 0), root_rot=(0, 0, 0), h=1.0, w=None, body_rot=(0, 0, 0), top_rot=(0, 0, 0), top_loc=(0, 0, 0)):
    return {"root_loc": root_loc, "root_rot": root_rot, "h": h, "w": vol(h) if w is None else w,
            "body_rot": body_rot, "top_rot": top_rot, "top_loc": top_loc}


def deg(x):
    return math.radians(x)


def idle(t):
    return pose(h=1 - 0.035 * (0.5 - 0.5 * math.cos(2 * math.pi * t)),
                top_rot=(0, 0, deg(2.5) * wave(t)))


def seam(t, width):
    d = min(t, 1 - t) / width
    return 0.5 + 0.5 * math.cos(math.pi * d) if d < 1 else 0.0


def move(t):
    squash = seam(t, 0.2)
    air = bump(t, 0.12, 0.88)
    stretch = bump(t, 0.1, 0.5) - 0.5 * bump(t, 0.55, 0.9)
    h = 1 - 0.17 * squash + 0.12 * stretch
    return pose(root_loc=(0, 0.05 * air, 0), h=h,
                root_rot=(deg(3) - deg(7) * bump(t, 0.1, 0.6) + deg(6) * squash, 0, 0),
                top_rot=(deg(8) * squash - deg(6) * air, 0, 0))


def attack(t):
    wind = bump(t, 0.0, 0.45)
    lunge = bump(t, 0.35, 0.75)
    settle = bump(t, 0.7, 1.0)
    h = 1 - 0.24 * wind + 0.14 * lunge - 0.1 * settle
    return pose(root_loc=(0, 0.03 * lunge, -0.03 * wind + 0.12 * lunge), h=h,
                root_rot=(deg(-14) * wind + deg(24) * lunge, 0, 0),
                top_rot=(deg(-16) * wind + deg(18) * lunge, 0, 0))


def jelly(t, hit=0.07, decay=4.5, freq=2.0):
    if t <= 0:
        return 0.0
    if t < hit:
        return -math.sin(0.5 * math.pi * t / hit)
    tt = t - hit
    return -math.exp(-decay * tt) * math.cos(2 * math.pi * freq * tt)


def damage(t):
    body = jelly(t)
    top = jelly(t - 0.035)
    h = 1 - 0.3 * max(0.0, -body) + 0.12 * max(0.0, body)
    return pose(h=h, top_rot=(deg(-42) * top, 0, 0), top_loc=(0, 0, -0.06 * top))


def jump(t):
    ant = bump(t, 0.0, 0.3)
    air = bump(t, 0.25, 0.75)
    land = bump(t, 0.72, 0.9)
    wob = bump(t, 0.85, 1.0) * wave(t, 6)
    up = 0.16 * air
    stretch = bump(t, 0.25, 0.45) - 0.4 * bump(t, 0.45, 0.6)
    h = 1 - 0.25 * ant + 0.18 * stretch - 0.22 * land + 0.04 * wob
    return pose(root_loc=(0, up, 0), h=h, top_rot=(deg(-6) * air, 0, 0))


def happy(t):
    hop = abs(wave(t, 1))
    h = 1 + 0.08 * hop - 0.12 * (1 - hop) ** 4
    return pose(root_loc=(0, 0.03 * hop, 0), h=h,
                root_rot=(0, 0, deg(7) * wave(t, 0.5)),
                top_rot=(0, 0, deg(6) * wave(t, 0.5, 0.15)))


def excited(t):
    hop = abs(wave(t, 1))
    h = 1 + 0.1 * hop - 0.14 * (1 - hop) ** 4
    return pose(root_loc=(0, 0.02 * hop, 0), h=h,
                root_rot=(0, deg(10) * wave(t, 1), 0),
                top_rot=(deg(-5) * hop, 0, deg(6) * wave(t, 2)))


def angry(t):
    puff = ease(t, 0.0, 0.3) * (1 - ease(t, 0.85, 1.0))
    tremble = wave(t, 12) * puff
    return pose(h=1 + 0.08 * puff, w=1 + 0.2 * puff,
                root_rot=(deg(12) * puff, 0, deg(3.5) * tremble),
                top_rot=(deg(10) * puff, 0, deg(3) * tremble))


def yes(t):
    nod = wave(t, 2, -0.25) * 0.5 + 0.5
    return pose(h=1 - 0.1 * nod, top_rot=(deg(26) * nod, 0, 0), root_rot=(deg(7) * nod, 0, 0))


def no(t):
    env = bump(t, 0.0, 1.0)
    return pose(h=1 - 0.04 * env, top_rot=(0, deg(32) * wave(t, 2) * env, deg(4) * wave(t, 2, 0.25) * env),
                root_rot=(0, deg(10) * wave(t, 2, 0.1) * env, 0))


def stun(t):
    a = 2 * math.pi * t
    return pose(h=0.95, root_rot=(deg(6) * math.sin(a), 0, deg(6) * math.cos(a)),
                top_rot=(deg(10) * math.sin(a + 0.6), 0, deg(10) * math.cos(a + 0.6)))


def sick(t):
    breath = 0.5 - 0.5 * math.cos(2 * math.pi * t)
    return pose(h=0.9 - 0.03 * breath, w=1.07 + 0.015 * breath,
                root_rot=(deg(4), 0, deg(3) * wave(t)),
                top_rot=(deg(12) + deg(3) * breath, 0, deg(4) * wave(t, 1, 0.2)))


def eating(t):
    chew = 0.5 - 0.5 * math.cos(2 * math.pi * 2 * t)
    return pose(h=1 - 0.09 * chew, root_rot=(deg(6), 0, deg(3) * wave(t)),
                top_rot=(deg(12) + deg(14) * chew, 0, deg(4) * wave(t, 1, 0.25)))


def die(t):
    shiver = bump(t, 0.0, 0.3) * wave(t, 5)
    melt = ease(t, 0.2, 0.85)
    h = 1 - 0.72 * melt + 0.04 * shiver
    return pose(h=h, w=1 + 0.45 * melt, top_rot=(deg(10) * melt, 0, deg(4) * shiver))


def die2(t):
    air = bump(t, 0.1, 0.55)
    ant = bump(t, 0.0, 0.15)
    fall = ease(t, 0.45, 0.75)
    bounce = bump(t, 0.75, 0.9)
    spin = ease(t, 0.1, 0.6)
    h = 1 - 0.2 * ant + 0.1 * air - 0.15 * bounce - 0.1 * fall
    return pose(root_loc=(0.09 * fall, 0.12 * air + 0.015 * bounce, 0), h=h,
                root_rot=(0, deg(360) * spin, deg(-92) * fall),
                top_rot=(0, 0, deg(-15) * fall))


CLIPS = [("Idle", idle, 48), ("Move", move, 16), ("Attack", attack, 24), ("Damage", damage, 36),
         ("Jump", jump, 30), ("Happy", happy, 24), ("Excited", excited, 12), ("Angry", angry, 36),
         ("Yes", yes, 24), ("No", no, 24), ("Stun", stun, 32), ("Sick", sick, 48),
         ("Eating", eating, 24), ("Die", die, 30), ("Die2", die2, 36)]


def build_rig():
    arm_data = bpy.data.armatures.new("Slime_Rig")
    rig = bpy.data.objects.new("Slime_Rig", arm_data)
    bpy.context.scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    root = arm_data.edit_bones.new("Root")
    root.head, root.tail = (0, 0, 0), (0, 0, 0.05)
    body = arm_data.edit_bones.new("Body")
    body.head, body.tail, body.parent = (0, 0, 0), (0, 0, TOP_Z), root
    top = arm_data.edit_bones.new("Top")
    top.head, top.tail, top.parent = (0, 0, TOP_Z), (0, 0, 0.22), body
    top.use_connect = True
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def top_weight(z):
    return smooth(BLEND[0], BLEND[1], z)


def skin(rig, body_ob, parts):
    mw = body_ob.matrix_world
    kd = KDTree(len(body_ob.data.vertices))
    for v in body_ob.data.vertices:
        kd.insert(mw @ v.co, v.index)
    kd.balance()
    world_z = {v.index: (mw @ v.co).z for v in body_ob.data.vertices}
    for ob in [body_ob] + parts:
        wm = ob.matrix_world.copy()
        ob.parent = None
        ob.matrix_world = wm
    for ob in [body_ob] + parts:
        ob.vertex_groups.clear()
        g_body = ob.vertex_groups.new(name="Body")
        g_top = ob.vertex_groups.new(name="Top")
        for v in ob.data.vertices:
            p = ob.matrix_world @ v.co
            z = p.z if ob is body_ob else world_z[kd.find(p)[1]]
            w = top_weight(z)
            if w < 1:
                g_body.add([v.index], 1 - w, "REPLACE")
            if w > 0:
                g_top.add([v.index], w, "REPLACE")
        mod = ob.modifiers.new("Rig", "ARMATURE")
        mod.object = rig
        wm = ob.matrix_world.copy()
        ob.parent = rig
        ob.matrix_world = wm


def apply_pose(rig, p):
    pb = rig.pose.bones
    rx, _, rz = p["root_rot"]
    tilt = max(abs(rx), abs(rz))
    lift = p["w"] * (BASE_R * math.sin(tilt) + (SIDE_R - BASE_R) * (1 - math.cos(tilt)))
    pb["Root"].location = Vector(p["root_loc"]) + Vector((0, lift, 0))
    pb["Root"].rotation_euler = p["root_rot"]
    pb["Body"].scale = (p["w"], p["h"], p["w"])
    pb["Body"].rotation_euler = p["body_rot"]
    pb["Top"].rotation_euler = p["top_rot"]
    pb["Top"].location = Vector(p["top_loc"])


def bake(rig):
    for b in rig.pose.bones:
        b.rotation_mode = "XYZ"
    rig.animation_data_create()
    for name, fn, frames in CLIPS:
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        rig.animation_data.action = act
        for f in range(frames + 1):
            apply_pose(rig, fn(f / frames))
            for b in rig.pose.bones:
                b.keyframe_insert("location", frame=f)
                b.keyframe_insert("rotation_euler", frame=f)
                b.keyframe_insert("scale", frame=f)
        act.frame_range = (0, frames)
    apply_pose(rig, pose())
    rig.animation_data.action = bpy.data.actions["Idle"]


def main():
    bpy.context.scene.render.fps = FPS
    body_ob = bpy.data.objects["Slime_Body"]
    parts = [o for o in bpy.data.objects if o.parent is body_ob]
    rig = build_rig()
    skin(rig, body_ob, parts)
    bake(rig)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "MonchiSlime_Rig.blend"))
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_DIR, "MonchiSlime.fbx"), use_selection=False,
                             apply_scale_options="FBX_SCALE_ALL", bake_space_transform=True,
                             object_types={"MESH", "ARMATURE"}, mesh_smooth_type="FACE", path_mode="STRIP",
                             add_leaf_bones=False, use_armature_deform_only=True,
                             bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
                             bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0)
    print("RIG_DONE", [c[0] for c in CLIPS])


main()

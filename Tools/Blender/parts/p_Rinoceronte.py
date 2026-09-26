from mathutils import Vector
import part_common as pc

SLOTS = ("Horn",)


def taper(name, pts, r0, flat=1.0, power=0.9, tip=0.006, hold=0.0, flare=0.0):
    n = len(pts)
    radii = []
    for i in range(n):
        t = i / (n - 1)
        u = max(0.0, (t - hold) / (1 - hold))
        r = max(tip, r0 * (1 - u) ** power * (1 + flare * max(0.0, 1 - t / 0.3) ** 2))
        radii.append((r * flat, r / flat))
    return pc.tube(name, pts, radii, sides=16, side_hint=(1, 0, 0))


def build(arm):
    up, fwd, out = Vector((0, 0, 1)), Vector((0, -1, 0)), Vector((1, 0, 0))
    surf, nrm = pc.surface_point((1.5, -0.42, 1.12), (-1, 0, 0))
    root = surf - nrm * 0.07
    pts = pc.bezier(root, root + nrm * 0.14 + fwd * 0.12,
                    root + nrm * 0.16 + out * 0.10 + fwd * 0.50,
                    root + nrm * 0.12 + out * 0.18 + fwd * 0.78 + up * 0.14, 26)
    right = taper("Horn_Rinoceronte_R", pts, 0.15, flat=1.12, power=0.8, hold=0.1, flare=0.2)
    left = pc.mirror_x(right, "Horn_Rinoceronte_L")
    for o in (right, left):
        pc.skin_like(o, arm, "Horn")
    return [right, left]

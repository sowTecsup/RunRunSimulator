import bpy, sys, numpy as np
from collections import deque
ref = sys.argv[-1]
im = bpy.data.images.load(ref); W, H = im.size
a = np.array(im.pixels[:], dtype=np.float32).reshape(H, W, 4)[::-1, :, :3]
bg = np.median(a[5:40, 5:40].reshape(-1, 3), axis=0)
mask = np.abs(a - bg).sum(axis=2) > 0.10
print("REF size", W, H, "bg", bg.round(3))
def blob(seed):
    sy, sx = seed
    seen = np.zeros_like(mask); q = deque([seed]); seen[sy, sx] = True; pts = []
    while q:
        y, x = q.popleft(); pts.append((y, x))
        for dy, dx in ((1,0),(-1,0),(0,1),(0,-1)):
            ny, nx = y + dy, x + dx
            if 0 <= ny < H and 0 <= nx < W and mask[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True; q.append((ny, nx))
    return np.array(pts)
seeds = {"white": (int(H * 0.125), int(W * 0.166)), "orange_r2": (int(H * 0.33), int(W * 0.44)), "grey_r2": (int(H * 0.33), int(W * 0.565)), "mint_r3": (int(H * 0.53), int(W * 0.24))}
for name, sd in seeds.items():
    if not mask[sd]:
        print("SEED miss", name); continue
    pts = blob(sd)
    ys, xs = pts[:, 0], pts[:, 1]
    y0, y1 = ys.min(), ys.max()
    rows = []
    for y in range(y0, y1 + 1):
        r = xs[ys == y]
        rows.append((y, r.min(), r.max()))
    widths = np.array([r[2] - r[1] for r in rows], dtype=float)
    wmax = widths.max()
    bottom = y1
    bh = None
    prof = []
    for f in np.linspace(0.0, 1.0, 21):
        y = int(round(bottom - f * (y1 - y0)))
        w = widths[y - y0] / wmax
        prof.append(round(w, 3))
    widest_y = rows[int(widths.argmax())][0]
    print("PROF %s bbox=(%d..%d, h=%d, w=%d) aspect=%.3f widest_at=%.2f profile=%s" % (name, y0, y1, y1 - y0, wmax, (y1 - y0) / wmax, (bottom - widest_y) / (y1 - y0), prof))

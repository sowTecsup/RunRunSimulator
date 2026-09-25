import bpy, sys, numpy as np

OUT = sys.argv[sys.argv.index("--") + 1]
N = 512
COLS, ROWS = 4, 12
cw, rh = N / COLS, N / ROWS
radius = cw * 0.6

ys, xs = np.mgrid[0:N, 0:N].astype(np.float32) + 0.5
alpha = np.zeros((N, N), dtype=np.float32)
best = np.full((N, N), -1e9, dtype=np.float32)
for row in range(-1, ROWS + 2):
    for col in range(-1, COLS + 2):
        cx = (col + (0.5 if row % 2 else 0.0)) * cw
        cy = row * rh
        for ox in (-N, 0, N):
            for oy in (-N, 0, N):
                dx, dy = xs - (cx + ox), ys - (cy + oy)
                d = np.sqrt(dx * dx + dy * dy)
                mask = d < radius
                upd = mask & ((cy + oy) > best)
                best = np.where(upd, cy + oy, best)
                edge = np.clip(1.0 - np.abs(d - radius) / 3.0, 0, 1)
                shade = np.clip((d / radius) ** 4, 0, 1) * 0.3
                val = np.maximum(edge, shade)
                alpha = np.where(upd, val, alpha)
alpha = np.clip(alpha, 0, 1) * 0.5

img = bpy.data.images.new("EggScales", N, N, alpha=True)
px = np.zeros((N, N, 4), dtype=np.float32)
px[:, :, 3] = alpha
img.pixels.foreach_set(px.ravel())
img.filepath_raw = OUT
img.file_format = "PNG"
img.save()
print("SCALES_DONE alpha_mean=%.3f alpha_max=%.3f" % (alpha.mean(), alpha.max()))

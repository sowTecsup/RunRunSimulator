import bpy, sys, numpy as np
a = sys.argv[sys.argv.index("--") + 1:]
def load(p):
    im = bpy.data.images.load(p); w, h = im.size
    return np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
ref, mine = load(a[0]), load(a[1])
rh, rw = ref.shape[:2]
top = ref[int(rh * (1 - 0.40)):int(rh * (1 - 0.20)), :]
mh, mw = mine.shape[:2]
bot = mine[int(mh * 0.25):int(mh * 0.75), :]
idx = (np.arange(rw) * mw / rw).astype(int)
rows = (np.arange(int(bot.shape[0] * rw / mw)) * mw / rw).astype(int)
bot = bot[rows][:, idx]
out = np.concatenate([bot, top], axis=0)
img = bpy.data.images.new("cmp", out.shape[1], out.shape[0], alpha=True)
img.pixels.foreach_set(out.ravel()); img.filepath_raw = a[2]; img.file_format = "PNG"; img.save()
print("CMP_DONE", out.shape)

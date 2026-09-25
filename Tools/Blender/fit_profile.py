import numpy as np
ref = np.array([0.549, 0.783, 0.869, 0.909, 0.966, 0.977, 0.994, 1.0, 1.0, 1.0, 0.983, 0.966, 0.937, 0.914, 0.88, 0.811, 0.766, 0.686, 0.577, 0.383, 0.011])
hs = np.linspace(0, 1, 21)
def model(hf, pb, pt, t0):
    out = []
    for h in hs:
        if h < hf:
            u = (hf - h) / hf * t0
            out.append((1 - u ** pb) ** (1 / pb))
        else:
            u = min(1.0, (h - hf) / (1 - hf))
            out.append((1 - u ** pt) ** (1 / pt))
    return np.array(out)
best = None
for hf in np.arange(0.30, 0.61, 0.01):
    for pb in np.arange(2.0, 6.01, 0.1):
        for pt in np.arange(1.5, 3.01, 0.05):
            for t0 in np.arange(0.85, 1.001, 0.01):
                e = np.abs(model(hf, pb, pt, t0) - ref).mean()
                if best is None or e < best[0]:
                    best = (e, hf, pb, pt, t0)
e, hf, pb, pt, t0 = best
print("BEST err=%.4f hf=%.2f pb=%.1f pt=%.2f t0=%.2f" % best)
print("MODEL", model(hf, pb, pt, t0).round(3).tolist())
old = model(0.44, 2.3, 2.4, 1.0)
print("V11 err=%.4f" % np.abs(old - ref).mean())

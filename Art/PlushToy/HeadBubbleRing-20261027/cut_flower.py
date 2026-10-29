import sys
from PIL import Image, ImageFilter
import numpy as np
src = sys.argv[1]
im = np.asarray(Image.open(src).convert('RGB')).astype(float)
x0, x1, y0, y1 = 172, 378, 192, 413
reg = im[y0:y1, x0:x1].copy()
H, W = reg.shape[:2]
# background: lavender, estimated per row from the region's left/right borders
rows = np.arange(0, 150)
edge = np.concatenate([reg[:150, :6], reg[:150, -6:]], axis=1).mean(1)   # (150,3)
coef = [np.polyfit(rows, edge[:, k], 1) for k in range(3)]
bg = np.zeros_like(reg)
for k in range(3): bg[..., k] = np.polyval(coef[k], np.arange(H))[:, None]
d = np.sqrt(((reg - bg) ** 2).sum(-1))
a = np.clip((d - 14) / (46 - 14), 0, 1)
# ring membrane: inside the yellow ring keep a faint film
yy, xx = np.indices((H, W))
yellow = (reg[..., 0] > 200) & (reg[..., 1] > 170) & (reg[..., 2] < 120)
ys, xs = np.nonzero(yellow)
cy, cx = ys.mean(), xs.mean()
r = np.sqrt((yy - cy) ** 2 + (xx - cx) ** 2)
ring_r = np.percentile(np.sqrt((ys - cy) ** 2 + (xs - cx) ** 2), 5)
inner = r < ring_r - 1.5
a[inner] = .28
bluehead = (reg[..., 2] > 200) & (reg[..., 0] < 140) & ~inner
a[bluehead] = 0
# unmix the lavender out of soft edges
am = np.maximum(a, 1e-3)[..., None]
col = np.clip((reg - (1 - am) * bg) / am, 0, 255)
col[inner] = reg[inner] * .5 + 255 * .5          # pale film
rgba = np.dstack([col, a * 255]).astype(np.uint8)
out = Image.fromarray(rgba, 'RGBA')
# drop specks: keep the connected flower only
mask = out.getchannel('A').point(lambda v: 255 if v > 60 else 0)
from PIL import ImageDraw
seed = (int(cx), int(cy - ring_r - 4))
m2 = mask.copy(); ImageDraw.floodfill(m2, (int(cx), int(cy + ring_r + 3)), 128)
keep = np.asarray(m2) == 128
keep = np.asarray(Image.fromarray((keep * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))) > 0
arr = np.asarray(out).copy(); arr[..., 3] = np.where(keep | inner, arr[..., 3], 0)
# tuck: extend the stem below the cut so it can hide behind the head
stem = arr[H - 9:H - 1].copy()
row = arr[H - 2, :, 3] > 100
runs, start = [], None
for x, v in enumerate(list(row) + [False]):
    if v and start is None: start = x
    if not v and start is not None: runs.append((start, x)); start = None
s0, s1 = max(runs, key=lambda r: r[1] - r[0])
stem[:, :s0, 3] = 0; stem[:, s1:, 3] = 0
ext = np.concatenate([stem] * 4, axis=0)
arr = np.concatenate([arr, ext], axis=0)
res = Image.fromarray(arr, 'RGBA')
bb = res.getchannel('A').point(lambda v: 255 if v > 8 else 0).getbbox()
res = res.crop(bb)
res.save(sys.argv[2])
info = dict(size=res.size, ringCenter=[round(cx - bb[0], 1), round(cy - bb[1], 1)], ringRadius=round(ring_r, 1),
            stemBottomCut=H - bb[1], tuck=ext.shape[0])
print(info)

#!/usr/bin/env python3
"""R1 v2: deterministic cuts from the battle mockup (no generative repainting).

usage: python3 scripts/r1v2/extract_mockup_v2.py [MOCKUP] [OUT_DIR]
defaults: Art/Concepts/plant-defense-v03/03-cascade-pressure.png -> Art/R1/art-v2/cuts
Coordinates are mockup pixels (941x1672; x0.5739 = 540x960 logical). Only baked digits
are removed (OpenCV Telea inpaint inside the listed boxes). Requires Pillow, NumPy, OpenCV.
"""
import hashlib, json, sys
from pathlib import Path
import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
MOCK = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT/'Art/Concepts/plant-defense-v03/03-cascade-pressure.png'
OUT = Path(sys.argv[2]) if len(sys.argv) > 2 else ROOT/'Art/R1/art-v2/cuts'
cv2.setNumThreads(1)
src = np.asarray(Image.open(MOCK).convert('RGB'))
assert src.shape[:2] == (1672, 941), src.shape
OUT.mkdir(parents=True, exist_ok=True)
records = {}

def save(name, arr, box, note):
    im = Image.fromarray(arr.astype('uint8'), 'RGBA' if arr.shape[2] == 4 else 'RGB')
    p = OUT/f'{name}.png'; im.save(p, optimize=True)
    records[name] = dict(box=list(box), size=list(im.size), note=note,
                         sha256=hashlib.sha256(p.read_bytes()).hexdigest())

def crop(box): x0, y0, x1, y1 = box; return src[y0:y1, x0:x1].copy()

def hsv(a):
    h = cv2.cvtColor(a[:, :, :3].astype('uint8'), cv2.COLOR_RGB2HSV).astype(float)
    return h[:, :, 0] * 2, h[:, :, 1] / 255, h[:, :, 2] / 255

def rounded_alpha(w, h, x0, y0, x1, y1, r, scale=4):
    big = np.zeros((h * scale, w * scale), 'uint8')
    cv2.rectangle(big, (int((x0 + r) * scale), int(y0 * scale)), (int((x1 - r) * scale), int(y1 * scale)), 255, -1)
    cv2.rectangle(big, (int(x0 * scale), int((y0 + r) * scale)), (int(x1 * scale), int((y1 - r) * scale)), 255, -1)
    for cx, cy in ((x0 + r, y0 + r), (x1 - r, y0 + r), (x0 + r, y1 - r), (x1 - r, y1 - r)):
        cv2.circle(big, (int(cx * scale), int(cy * scale)), int(r * scale), 255, -1)
    return cv2.resize(big, (w, h), interpolation=cv2.INTER_AREA)

# 1. Pause: rounded-square wooden button. Alpha is the outline's rounded rectangle.
box = (26, 6, 122, 98); a = crop(box)
alpha = rounded_alpha(a.shape[1], a.shape[0], 5.0, 5.0, 88.5, 86.5, 16)
save('pause', np.dstack([a, alpha]), box, 'rounded-square pause, foliage outside the outline removed')

# 2. Plot (empty slot 4): dirt square with light stone rim; the sprout is inpainted away.
box = (409, 937, 531, 1060); a = crop(box)
h, s, v = hsv(a)
sprout = ((h > 70) & (h < 150) & (s > .25)).astype('uint8')
sp = np.zeros_like(sprout); sp[50:85, 35:90] = sprout[50:85, 35:90]     # only the centre sprout, not rim grass
sp = cv2.dilate(sp * 255, np.ones((3, 3), 'uint8'), iterations=2)
clean = cv2.inpaint(a, sp, 6, cv2.INPAINT_TELEA)
save('plot', clean, box, 'empty plot from mockup slot 4, sprout inpainted')
# 3. Sprout on its own (alpha = green leaves), for empty unlocked plots.
sbox = (448, 988, 494, 1020); s_a = crop(sbox); h, s, v = hsv(s_a)
al = (((h > 70) & (h < 150) & (s > .22)) * 255).astype('uint8')
al = cv2.morphologyEx(al, cv2.MORPH_OPEN, np.ones((2, 2), 'uint8'))
al = cv2.GaussianBlur(al, (3, 3), 0)
save('plot-sprout', np.dstack([s_a, al]), sbox, 'sprout leaves keyed from the dirt')

# 4. Plot number tag, digit removed (runtime text).
box = (424, 1056, 517, 1086); a = crop(box); h, s, v = hsv(a)
dig = np.zeros(a.shape[:2], 'uint8'); dig[3:27, 33:62] = ((v[3:27, 33:62] < .55) * 255).astype('uint8')
dig = cv2.dilate(dig, np.ones((3, 3), 'uint8'), iterations=2)
tag = cv2.inpaint(a, dig, 6, cv2.INPAINT_TELEA)
save('plot-label', tag, box, 'wooden number tag under slot 4, digit 4 inpainted')

# 5. Fence: one tall+short picket period, grass keyed out; tiles horizontally.
box = (476, 856, 557, 926); a = crop(box); h, s, v = hsv(a)
grass = (h > 60) & (h < 160) & (s > .28)
al = np.where(grass, 0, 255).astype('uint8')
al = cv2.morphologyEx(al, cv2.MORPH_OPEN, np.ones((3, 3), 'uint8'))
al = cv2.morphologyEx(al, cv2.MORPH_CLOSE, np.ones((3, 3), 'uint8'))
n, lab, st, _ = cv2.connectedComponentsWithStats((al > 0).astype('uint8'), 8)
keep = np.zeros_like(al)
for i in range(1, n):
    if st[i, 4] > 150: keep[lab == i] = 255
al = cv2.GaussianBlur(keep, (3, 3), 0)
save('fence-tile', np.dstack([a, al]), box, 'white picket fence, short + tall picket with rails at both ends (tiles), grass keyed out')

(OUT/'cuts.json').write_text(json.dumps(dict(
    source=str(MOCK.relative_to(ROOT)) if MOCK.is_relative_to(ROOT) else str(MOCK),
    source_sha256=hashlib.sha256(MOCK.read_bytes()).hexdigest(),
    logical_scale=540 / 941, items=records), ensure_ascii=False, indent=2))
print(json.dumps({k: v['size'] for k, v in records.items()}))

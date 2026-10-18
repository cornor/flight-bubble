"""Format the generated lock bubble and render the requested size preview."""
from pathlib import Path
import hashlib
import json
import re
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

Q = Path(__file__).resolve().parent
D = Q.parent.parent
source = D / '_originals/H16-attempt-02.png'
raw_image = Image.open(source).convert('RGBA')
raw = np.array(raw_image)
alpha = raw[:, :, 3]
n, labels, stats, _ = cv2.connectedComponentsWithStats((alpha >= 16).astype(np.uint8), 8)
k = 1 + int(np.argmax(stats[1:, 4]))
# Remove disconnected extraction flecks outside the generated bubble.
support = cv2.dilate((labels == k).astype(np.uint8), np.ones((3, 3), np.uint8))
raw[support == 0] = 0
raw[raw[:, :, 3] < 3] = 0
x, y, w, h, _ = stats[k]
crop = raw[y:y+h, x:x+w].copy()
# The original circumference differs by only about 1% between its axes.
# Format its full bounding box to a square 230px icon, preserving alpha.
pm = crop.astype(np.float32) / 255
pm[:, :, :3] *= pm[:, :, 3:4]
small = cv2.resize(pm, (230, 230), interpolation=cv2.INTER_AREA)
a = small[:, :, 3:4]
rgb = np.divide(small[:, :, :3], a, out=np.zeros_like(small[:, :, :3]), where=a > 1e-6)
small = np.rint(np.clip(np.concatenate((rgb, a), axis=2), 0, 1)*255).astype(np.uint8)
small[small[:, :, 3] < 2] = 0
out = np.zeros((256, 256, 4), dtype=np.uint8)
out[13:243, 13:243] = small
out[out[:, :, 3] == 0, :3] = 0
sprite = Image.fromarray(out)
sprite.save(D / 'codex-lock.png')

# Native pixel sizes, not display-enlarged thumbnails.
preview = Image.new('RGBA', (480, 160), '#0B1A3A')
for size, center in [(46, (120, 62)), (18, (360, 62))]:
    icon = sprite.resize((size, size), Image.Resampling.LANCZOS)
    preview.alpha_composite(icon, (center[0]-size//2, center[1]-size//2))
draw = ImageDraw.Draw(preview)
font = ImageFont.load_default(size=15)
draw.text((120, 112), '46 px', font=font, fill='#C7DDF7', anchor='mm')
draw.text((360, 112), '18 px', font=font, fill='#C7DDF7', anchor='mm')
preview.convert('RGB').save(D / '_qa/codex-lock-on-dark.png')

contact = Image.new('RGB', (512, 256))
for i, color in enumerate(['#E9F5FF', '#0B1A3A']):
    tile = Image.new('RGBA', (256, 256), color)
    tile.alpha_composite(sprite)
    contact.paste(tile.convert('RGB'), (i*256, 0))
contact.save(Q / 'alpha-on-light-dark.png')

alpha = out[:, :, 3]
yy, xx = np.indices(alpha.shape)
rr = np.hypot(xx-127.5, yy-127.5)
# Broad side film samples avoid the central lock and perimeter highlights.
film = (((xx >= 55) & (xx <= 69)) | ((xx >= 186) & (xx <= 200))) & (yy >= 100) & (yy <= 170)
white = (out[:, :, 0] >= 185) & (out[:, :, 1] >= 200) & (out[:, :, 2] >= 205) & (alpha >= 220) & (rr < 80)
_, wl, ws, _ = cv2.connectedComponentsWithStats(white.astype(np.uint8), 8)
wk = 1 + int(np.argmax(ws[1:, 4]))
wx, wy, ww, wh, _ = ws[wk]

refs = json.loads((Q / 'reference-hashes.json').read_text())
for ref in refs:
    ref['unchanged'] = hashlib.sha256(Path(ref['path']).read_bytes()).hexdigest() == ref['sha256']
assert all(ref['unchanged'] for ref in refs)
assert sprite.size == (256, 256) and sprite.mode == 'RGBA'
assert not alpha[:13].any() and not alpha[243:].any()
assert not alpha[:, :13].any() and not alpha[:, 243:].any()
assert np.mean(alpha[film] < 230) > .9
assert not alpha[rr > 118].any()
log = json.loads((Q / 'generation-log.json').read_text())
log['attempts'][1]['result'] = 'selected; translucent blue film, complete circular lower edge, no base'
log['selected'] = source.name
(Q / 'generation-log.json').write_text(json.dumps(log, indent=2)+'\n')

style = re.search(r'```text\n(.*?)\n```', (D.parent / '\u63d0\u793a\u8bcd.md').read_text(), re.S).group(1)
prompts = []
for i in range(1, 3):
    p = Q / f'H16-attempt-{i:02d}-prompt.txt'
    assert p.read_text().startswith(style)
    prompts.append({'file': str(p), 'fullStylePrefix': True})
audit = {
    'source': str(source), 'sourceSize': list(raw_image.size), 'sourceCrop': [int(x), int(y), int(w), int(h)],
    'delivery': str(D / 'codex-lock.png'), 'size': [256, 256], 'mode': 'RGBA',
    'alphaBounds': sprite.getchannel('A').getbbox(), 'bubbleOccupancy': 230/256,
    'transparentMarginPixels': 13, 'alphaRange': [int(alpha.min()), int(alpha.max())],
    'filmSampleBounds': [[55, 100, 70, 171], [186, 100, 201, 171]],
    'filmAlphaMean': float(alpha[film].mean()), 'filmAlphaMedian': float(np.median(alpha[film])),
    'filmPercentBelow230': float(np.mean(alpha[film] < 230)*100),
    'whiteLockBodyAndShackleBounds': [int(wx), int(wy), int(wx+ww), int(wy+wh)],
    'whiteLockWidthFractionOfBubble': float(ww/230),
    'preview': {'file': str(D / '_qa/codex-lock-on-dark.png'), 'background': '#0B1A3A', 'size': [480, 160], 'nativeIconSizes': [46, 18]},
    'generations': 2, 'maxGenerations': 4, 'selectedAttempt': 2,
    'references': refs, 'prompts': prompts,
    'postprocess': 'remove disconnected alpha flecks, crop full bubble, premultiplied-alpha resize to 230x230 and center in 256x256; retain generated film alpha and RGB; no base removal or recoloring in post'
}
(Q / 'delivery-audit.json').write_text(json.dumps(audit, indent=2)+'\n')
print(json.dumps(audit, indent=2))

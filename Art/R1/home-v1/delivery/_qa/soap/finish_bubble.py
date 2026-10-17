"""Normalize generated alpha/canvas and compose the requested QA previews."""
from pathlib import Path
import json
import cv2
import numpy as np
from PIL import Image, ImageDraw

Q = Path(__file__).resolve().parent
D = Q.parent.parent
H = D.parent
original = D / '_originals/H13a-attempt-03.png'
a = np.array(Image.open(original).convert('RGBA'))
n, labels, stats, _ = cv2.connectedComponentsWithStats((a[:, :, 3] > 64).astype(np.uint8), 8)
k = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
support = cv2.dilate((labels == k).astype(np.uint8), np.ones((5, 5), np.uint8))
a[support == 0] = 0
a[a[:, :, 3] < 3] = 0
im = Image.fromarray(a)
box = im.getchannel('A').getbbox()
im = im.crop(box).convert('RGBa').resize((480, 480), Image.Resampling.LANCZOS).convert('RGBA')
out = Image.new('RGBA', (512, 512))
out.alpha_composite(im, (16, 16))
a = np.array(out)
yy, xx = np.indices((512, 512))
rr = np.sqrt((xx + .5 - 256) ** 2 + (yy + .5 - 256) ** 2)
# Remove only generated alpha outside the requested perfect circular silhouette.
circle_alpha = np.clip(240 - rr, 0, 1)
a[:, :, 3] = np.rint(a[:, :, 3] * circle_alpha).astype(np.uint8)
a[a[:, :, 3] < 2] = 0
a[a[:, :, 3] == 0, :3] = 0
out = Image.fromarray(a)
out.save(D / 'soap-bubble.png')

home = Image.open(H / 'mock/01-home-mock.png').convert('RGBA')
positions = [(460, 205, 180), (680, 400, 120)]
for x, y, size in positions:
    sprite = out.convert('RGBa').resize((size, size), Image.Resampling.LANCZOS).convert('RGBA')
    home.alpha_composite(sprite, (x, y))
home.convert('RGB').save(Q / 'bubble-on-home-only.png')
dark = Image.new('RGBA', (1080, 1920), '#17304E')
dark.alpha_composite(out, (284, 340))
for x, size in [(260, 120), (560, 180)]:
    dark.alpha_composite(out.convert('RGBa').resize((size, size), Image.Resampling.LANCZOS).convert('RGBA'), (x, 1040))
preview = Image.new('RGB', (2160, 1920))
preview.paste(home.convert('RGB'), (0, 0))
preview.paste(dark.convert('RGB'), (1080, 0))
preview.save(D / '_qa/soap-on-home.png')

alpha = a[:, :, 3]
record = {
    'source': str(original.relative_to(H)), 'sourceCrop': box,
    'size': [512, 512], 'mode': 'RGBA', 'alphaBounds': out.getchannel('A').getbbox(),
    'clearFractionInsideCircleAlphaLE16': float((alpha[rr < 240] <= 16).mean()),
    'inner80PercentRadiusTransparentFraction': float((alpha[rr < 192] == 0).mean()),
    'inner80PercentRadiusMeanAlpha': float(alpha[rr < 192].mean()),
    'alphaRange': [int(alpha.min()), int(alpha.max())],
    'homePreviewPlacements': positions,
    'previewLayout': 'left: approved home with added 180px and 120px bubbles; right: dark navy at 512px,120px,180px',
    'postprocess': 'isolated low-alpha speckle cleanup; premultiplied resizing; centered canvas; circular outer alpha trim; no RGB repaint or recoloring'
}
(Q / 'bubble-audit.json').write_text(json.dumps(record, indent=2) + '\n')
print(json.dumps(record, indent=2))

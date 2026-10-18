"""Pack ImageGen frames, normalize their pivots/spread, and render QA."""
from pathlib import Path
import json
import cv2
import numpy as np
from PIL import Image

Q = Path(__file__).resolve().parent
D = Q.parent.parent
source = D / '_originals/H13b-attempt-02.png'
sheet = Image.open(source).convert('RGBA')
assert sheet.size == (1536, 1024)
first_source = D / '_originals/H13b-attempt-01.png'
first_sheet = Image.open(first_source).convert('RGBA')
frames = []
records = []
for i in range(6):
    x, y = i % 3 * 512, i // 3 * 512
    frame_source = first_source if i == 0 else source
    frame_sheet = first_sheet if i == 0 else sheet
    raw = np.array(frame_sheet.crop((x, y, x + 512, y + 512)))
    # Keep generated objects and their immediate antialiased edges, not faint
    # RGB/alpha background haze or isolated extraction speckles.
    n, labels, stats, centers = cv2.connectedComponentsWithStats((raw[:, :, 3] >= 96).astype(np.uint8), 8)
    keep = np.flatnonzero(stats[:, cv2.CC_STAT_AREA] >= 16)
    keep = keep[keep != 0]
    strong = np.isin(labels, keep).astype(np.uint8)
    support = cv2.dilate(strong, np.ones((7, 7), np.uint8))
    raw[support == 0] = 0
    raw[raw[:, :, 3] < 3] = 0
    yy, xx = np.where(strong)
    circle, radius = cv2.minEnclosingCircle(np.column_stack((xx, yy)).astype(np.float32))
    if i == 0:
        # The intact circular film, rather than the first flying droplets,
        # defines the first frame's 200-pixel diameter and pivot.
        k = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
        contours, _ = cv2.findContours((labels == k).astype(np.uint8), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
        center, axes, angle = cv2.fitEllipse(max(contours, key=cv2.contourArea))
        scale = 200 / float(np.mean(axes))
    else:
        center = circle
        scale = [0, 108, 112, 116, 120, 123][i] / radius
    # All frames use a fixed (128,128) pivot. Uniform scaling preserves the
    # generated artwork while making particle spread grow in time.
    transform = np.float32([[scale, 0, 128 - scale * center[0]], [0, scale, 128 - scale * center[1]]])
    rgba = raw.astype(np.float32) / 255
    rgba[:, :, :3] *= rgba[:, :, 3:4]
    warped = cv2.warpAffine(rgba, transform, (256, 256), flags=cv2.INTER_LANCZOS4, borderMode=cv2.BORDER_CONSTANT)
    warped = np.clip(warped, 0, 1)
    alpha = warped[:, :, 3]
    rgb = np.divide(warped[:, :, :3], alpha[:, :, None], out=np.zeros_like(warped[:, :, :3]), where=alpha[:, :, None] > 1e-6)
    fade = [1, 1, 1, 1, .75, .35][i]
    out = np.dstack((rgb, alpha * fade))
    a = np.rint(np.clip(out, 0, 1) * 255).astype(np.uint8)
    a[a[:, :, 3] < 2] = 0
    a[a[:, :, 3] == 0, :3] = 0
    im = Image.fromarray(a)
    im.save(Q / f'pop-frame-{i+1:02d}.png')
    frames.append(im)
    ay, ax = np.where(a[:, :, 3] >= 24)
    distances = np.hypot(ax - 128, ay - 128)
    records.append({
        'frame': i + 1, 'sourceFile': frame_source.name, 'sourceCell': [x, y, x+512, y+512],
        'sourcePivot': list(map(float, center)), 'sourceToDeliveryScale': float(scale),
        'outputPivot': [128, 128], 'alphaFade': fade,
        'alphaBounds': im.getchannel('A').getbbox(),
        'alphaWeightedArea': float(a[:, :, 3].sum() / 255),
        'visiblePixelsAlphaGE24': int(len(ax)),
        'medianRadiusAlphaGE24': float(np.median(distances)),
        'maxRadiusAlphaGE24': float(distances.max()),
        'retainedSourceComponents': int(len(keep))
    })

strip = Image.new('RGBA', (1536, 256))
for i, frame in enumerate(frames):
    strip.alpha_composite(frame, (i * 256, 0))
strip.save(D / 'soap-pop.png')

for color, path in [('#17304E', D / '_qa/soap-pop-frames.png'), ('#D9EFFF', Q / 'pop-frames-on-light.png')]:
    preview = Image.new('RGBA', (1536, 1024), color)
    for i, frame in enumerate(frames):
        enlarged = frame.convert('RGBa').resize((512, 512), Image.Resampling.NEAREST).convert('RGBA')
        preview.alpha_composite(enlarged, (i % 3 * 512, i // 3 * 512))
    preview.convert('RGB').save(path)

bubble = Image.open(D / 'soap-bubble.png').convert('RGBA')
intact = Image.new('RGBA', (256, 256))
small = bubble.convert('RGBa').resize((213, 213), Image.Resampling.LANCZOS).convert('RGBA')
intact.alpha_composite(small, (21, 21))
blank = Image.new('RGBA', (256, 256))
animation = []
for frame in [intact] + frames + [blank]:
    bg = Image.new('RGBA', (256, 256), '#17304E')
    bg.alpha_composite(frame)
    animation.append(bg.convert('RGB'))
animation[0].save(Q / 'soap-pop-preview.gif', save_all=True, append_images=animation[1:], duration=[600, 90, 90, 90, 90, 90, 120, 600], loop=0, disposal=2)

report = {'source': str(source), 'sourceLayout': '3 columns x 2 rows, 512px cells',
          'outputSize': [1536, 256], 'outputLayout': 'six 256x256 cells, left to right',
          'frames': records,
          'postprocess': 'generated alpha cleanup, uniform per-frame pivot/size normalization, premultiplied resampling, late-frame opacity fade, horizontal packing; no RGB repaint'}
(Q / 'pop-audit.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report, indent=2))

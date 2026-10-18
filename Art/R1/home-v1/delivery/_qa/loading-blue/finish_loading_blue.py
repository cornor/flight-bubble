"""Normalize a generated sprite to the required wand anchor and render QA."""
from pathlib import Path
import json
import hashlib
import numpy as np
import cv2
from PIL import Image, ImageDraw, ImageOps

Q = Path(__file__).resolve().parent
D = Q.parent.parent
H = D.parent
SOURCE = D / '_originals/H15-attempt-04.png'

def find_aperture(alpha):
    n, labels, stats, centers = cv2.connectedComponentsWithStats((alpha < 16).astype(np.uint8), 8)
    choices = []
    for k in range(1, n):
        x, y, w, h, area = stats[k]
        if area > 100 and x > alpha.shape[1] * .55 and y > 0 and x+w < alpha.shape[1] and y+h < alpha.shape[0] * .6:
            choices.append(k)
    k = max(choices, key=lambda k: stats[k, 4])
    contours, _ = cv2.findContours((labels == k).astype(np.uint8), cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_NONE)
    contour = max(contours, key=cv2.contourArea)
    center, axes, angle = cv2.fitEllipse(contour)
    return np.array(center), float(np.mean(axes) / 2), contour

def fit_outer_ring(alpha):
    center, inner_radius, contour = find_aperture(alpha)
    yy, xx = np.indices(alpha.shape)
    dx, dy = xx-center[0], yy-center[1]
    radius = np.hypot(dx, dy)
    theta = np.mod(np.rad2deg(np.arctan2(dy, dx)), 360)
    # Exclude the lower-left handle. Use strong perimeter pixels and robustly
    # fit the remaining circular outline, not the ring's inner aperture.
    valid = (alpha >= 128) & (radius > inner_radius * 1.15) & (radius < inner_radius * 2.1 + 2) & ((theta < 75) | (theta > 165))
    points = []
    for lo in range(0, 360, 2):
        ay, ax = np.where(valid & (theta >= lo) & (theta < lo + 2))
        if len(ax):
            k = np.argmax(radius[ay, ax])
            points.append((ax[k], ay[k]))
    points = np.array(points, dtype=np.float64)
    keep = np.ones(len(points), dtype=bool)
    for _ in range(4):
        p = points[keep]
        m = np.column_stack((2*p[:, 0], 2*p[:, 1], np.ones(len(p))))
        fit = np.linalg.lstsq(m, (p*p).sum(axis=1), rcond=None)[0]
        c = fit[:2]
        r = np.sqrt(fit[2] + (c*c).sum())
        residual = np.linalg.norm(points-c, axis=1)-r
        mad = np.median(np.abs(residual-np.median(residual)))
        keep = np.abs(residual) < max(.8, 3*mad)
    return c, float(r), center, inner_radius

raw_image = Image.open(SOURCE).convert('RGBA')
raw = np.array(raw_image)
n, labels, stats, centers = cv2.connectedComponentsWithStats((raw[:, :, 3] > 32).astype(np.uint8), 8)
k = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
support = cv2.dilate((labels == k).astype(np.uint8), np.ones((5, 5), np.uint8))
raw[support == 0] = 0
raw[raw[:, :, 3] < 3] = 0
# Clear extraction speckles enclosed by the empty aperture while retaining
# its generated, antialiased perimeter.
hole_center, hole_radius, hole_contour = find_aperture(raw[:, :, 3])
hole_mask = np.zeros(raw.shape[:2], np.uint8)
cv2.drawContours(hole_mask, [hole_contour], -1, 1, cv2.FILLED)
raw[hole_mask > 0] = 0
source_center, source_radius, _, _ = fit_outer_ring(raw[:, :, 3])
scale = 35 / source_radius
translation = np.array([380., 170.]) - scale * source_center
pm = raw.astype(np.float32) / 255
pm[:, :, :3] *= pm[:, :, 3:4]

best = None
iterations = []
for i in range(5):
    transform = np.float32([[scale, 0, translation[0]], [0, scale, translation[1]]])
    warped = cv2.warpAffine(pm, transform, (512, 512), flags=cv2.INTER_LANCZOS4, borderMode=cv2.BORDER_CONSTANT)
    warped = np.clip(warped, 0, 1)
    alpha = warped[:, :, 3]
    rgb = np.divide(warped[:, :, :3], alpha[:, :, None], out=np.zeros_like(warped[:, :, :3]), where=alpha[:, :, None] > 1e-6)
    out = np.rint(np.clip(np.dstack((rgb, alpha)), 0, 1)*255).astype(np.uint8)
    out[out[:, :, 3] < 2] = 0
    out[out[:, :, 3] == 0, :3] = 0
    center, radius, aperture_center, aperture_radius = fit_outer_ring(out[:, :, 3])
    error = float(np.linalg.norm(center - [380, 170]))
    # Center is the hard requirement; diameter is approximate.
    score = error + .05 * abs(radius - 35)
    iterations.append({'iteration': i, 'center': center.tolist(), 'outerDiameter': 2*radius, 'centerError': error})
    if best is None or score < best[0]:
        best = (score, out.copy(), center, radius, scale, translation.copy(), aperture_center, aperture_radius)
    if error < .04 and abs(radius-35) < .04:
        break
    ratio = 35 / radius
    scale *= ratio
    translation = ratio * (translation - center) + [380, 170]

_, out, center, radius, scale, translation, aperture_center, aperture_radius = best
sprite = Image.fromarray(out)
sprite.save(D / 'loading-blue.png')

# Requested preview: sprite at 1:1 scale on the home background, plus an
# exactly 200px-diameter diagnostic circle centered on the program anchor.
background = Image.open(D / 'background.png').convert('RGB')
bg = ImageOps.fit(background, (512, 512), method=Image.Resampling.LANCZOS, centering=(.5, .5)).convert('RGBA')
bg.alpha_composite(sprite)
guide = Image.new('RGBA', (2048, 2048))
draw = ImageDraw.Draw(guide)
draw.ellipse((1120, 280, 1920, 1080), fill=(147, 227, 255, 60), outline=(255, 255, 255, 195), width=6)
draw.ellipse((1515, 675, 1525, 685), fill=(255, 120, 175, 230))
bg.alpha_composite(guide.resize((512, 512), Image.Resampling.LANCZOS))
bg.convert('RGB').save(D / '_qa/loading-blue-preview.png')

contact = Image.new('RGB', (1024, 512))
for i, color in enumerate(['#E9F5FF', '#172E4E']):
    tile = Image.new('RGBA', (512, 512), color)
    tile.alpha_composite(sprite)
    contact.paste(tile.convert('RGB'), (i*512, 0))
contact.save(Q / 'alpha-on-light-dark.png')

alpha = out[:, :, 3]
yy, xx = np.indices(alpha.shape)
rr = np.hypot(xx-380, yy-170)
inner = rr <= 17
reserved = (xx >= 380) & (yy <= 170) & (rr >= 37) & (rr <= 175)
blue_mask = (out[:, :, 2].astype(int) > out[:, :, 0].astype(int)+35) & (out[:, :, 2].astype(int) > out[:, :, 1].astype(int)+15) & (alpha >= 128)
by, bx = np.where(blue_mask)
body_bounds = [int(bx.min()), int(by.min()), int(bx.max()+1), int(by.max()+1)]
refs = json.loads((Q / 'reference-hashes.json').read_text())
for ref in refs:
    ref['unchanged'] = hashlib.sha256(Path(ref['path']).read_bytes()).hexdigest() == ref['sha256']
assert all(ref['unchanged'] for ref in refs)
assert np.linalg.norm(center-[380,170]) < .25
assert abs(2*radius-70) < .25
assert not alpha[inner].any()
assert not alpha[reserved].any()
assert not alpha[0].any() and not alpha[-1].any() and not alpha[:, 0].any() and not alpha[:, -1].any()
report = {
    'source': str(SOURCE), 'sourceSize': list(raw_image.size), 'outputSize': [512, 512], 'mode': 'RGBA',
    'sourceRingCenter': source_center.tolist(), 'sourceRingOuterDiameter': 2*source_radius,
    'targetRingCenter': [380, 170], 'measuredRingCenter': center.tolist(),
    'ringCenterErrorPixels': float(np.linalg.norm(center-[380,170])), 'measuredRingOuterDiameter': 2*radius,
    'measuredApertureCenter': aperture_center.tolist(), 'measuredApertureDiameter': 2*aperture_radius,
    'uniformScale': scale, 'translation': translation.tolist(),
    'apertureCentralRadius17AlphaMax': int(alpha[inner].max()),
    'upperRightReservedSectorAlphaMax': int(alpha[reserved].max()),
    'spriteAlphaBounds': sprite.getchannel('A').getbbox(), 'blueBodyBounds': body_bounds,
    'alphaRange': [int(alpha.min()), int(alpha.max())],
    'previewCircle': {'center': [380, 170], 'diameter': 200, 'spriteScale': 1, 'onlyInPreview': True},
    'iterations': iterations, 'references': refs,
    'postprocess': 'isolated alpha speckle cleanup and aperture clearing; one uniform similarity transform of generated character and wand together; no local redrawing or recoloring'
}
(Q / 'delivery-audit.json').write_text(json.dumps(report, indent=2)+'\n')
print(json.dumps(report, indent=2))

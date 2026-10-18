"""Finalize generated halo by symmetric compositing and render requested QA."""
from pathlib import Path
import json
import hashlib
import numpy as np
from PIL import Image, ImageOps

Q = Path(__file__).resolve().parent
D = Q.parent.parent
H = D.parent
original = D / '_originals/H14-attempt-01.png'
raw = Image.open(original).convert('RGBA')
# Premultiplied resampling retains smooth transparent colored light.
fitted = raw.convert('RGBa').resize((960, 960), Image.Resampling.LANCZOS).convert('RGBA')
canvas = Image.new('RGBA', (1024, 1024))
canvas.alpha_composite(fitted, (32, 32))
a = np.array(canvas).astype(np.float32) / 255
a[:, :, :3] *= a[:, :, 3:4]
yy, xx = np.indices((1024, 1024), dtype=np.float32)
dx, dy = xx - 511.5, yy - 511.5
radius = np.hypot(dx, dy)

# The join crosses a gap between the generated bubbles. Opposite sides use
# the same generated art rotated by 180 degrees, with a soft light-only join.
angle = np.deg2rad(60)
distance = dx * np.sin(angle) - dy * np.cos(angle)
weight = (.5 + .5 * np.tanh(distance / 12))[:, :, None]
pm = a * weight + a[::-1, ::-1] * (1 - weight)
pm = (pm + pm[::-1, ::-1]) * .5

# Only the already faint exterior glow tapers here; bubbles remain inside.
t = np.clip((radius - 450) / 40, 0, 1)
fade = 1 - t * t * (3 - 2 * t)
pm *= fade[:, :, None]
alpha = pm[:, :, 3]
rgb = np.divide(pm[:, :, :3], alpha[:, :, None], out=np.zeros_like(pm[:, :, :3]), where=alpha[:, :, None] > 1e-6)
out = np.rint(np.clip(np.dstack((rgb, alpha)), 0, 1) * 255).astype(np.uint8)
out[out[:, :, 3] < 2] = 0
out[out[:, :, 3] == 0, :3] = 0
halo = Image.fromarray(out)
halo.save(D / 'celebrate-halo.png')

# The backdrop is a quiet sky-only patch of the approved mock, dimmed by
# exactly the requested navy overlay at 65% opacity. It is not in the asset.
home = Image.open(H / 'mock/01-home-mock.png').convert('RGB')
sky_box = (360, 150, 700, 400)
sky = home.crop(sky_box).resize((1024, 1024), Image.Resampling.LANCZOS)
dark = Image.blend(sky, Image.new('RGB', sky.size, '#0B1A3A'), .65).convert('RGBA')
dark_halo = dark.copy()
dark_halo.alpha_composite(halo)
dark_halo.convert('RGB').save(D / '_qa/halo-on-dark.png')

# Crop only the transparent padding of the supplied blue layer; preserve its
# generated pose and color. Center the visible hero on the halo.
blue = Image.open(D / 'squad-blue.png').convert('RGBA')
blue_bbox = blue.getchannel('A').getbbox()
hero = blue.crop(blue_bbox)
hero_height = 570
hero_width = round(hero.width * hero_height / hero.height)
hero = hero.convert('RGBa').resize((hero_width, hero_height), Image.Resampling.LANCZOS).convert('RGBA')
hero_xy = ((1024 - hero_width) // 2, (1024 - hero_height) // 2)
with_hero = dark_halo.copy()
with_hero.alpha_composite(hero, hero_xy)
with_hero.convert('RGB').save(D / '_qa/halo-with-hero.png')

contact = Image.new('RGB', (2048, 2048))
angles = [0, 45, 90, 135]
for i, deg in enumerate(angles):
    turned = halo.convert('RGBa').rotate(deg, resample=Image.Resampling.BICUBIC).convert('RGBA')
    tile = dark.copy()
    tile.alpha_composite(turned)
    tile.alpha_composite(hero, hero_xy)
    contact.paste(tile.convert('RGB'), (i % 2 * 1024, i // 2 * 1024))
contact.save(Q / 'rotation-check.png')

# Small optional playback preview; the hero stays fixed while the halo turns.
small_dark = dark.resize((320, 320), Image.Resampling.LANCZOS)
small_halo = halo.convert('RGBa').resize((320, 320), Image.Resampling.LANCZOS)
hero_layer = Image.new('RGBA', (1024, 1024))
hero_layer.alpha_composite(hero, hero_xy)
small_hero = hero_layer.convert('RGBa').resize((320, 320), Image.Resampling.LANCZOS).convert('RGBA')
frames = []
for deg in range(0, 360, 10):
    bg = small_dark.copy()
    bg.alpha_composite(small_halo.rotate(deg, resample=Image.Resampling.BICUBIC).convert('RGBA'))
    bg.alpha_composite(small_hero)
    frames.append(bg.convert('RGB'))
frames[0].save(Q / 'halo-rotation-preview.gif', save_all=True, append_images=frames[1:], duration=250, loop=0, disposal=2)

alpha = out[:, :, 3]
rotated = out[::-1, ::-1]
diff = np.abs(out.astype(np.int16) - rotated.astype(np.int16))
total = alpha.sum()
com = [float((xx * alpha).sum() / total), float((yy * alpha).sum() / total)]
corners = [alpha[:128, :128], alpha[:128, -128:], alpha[-128:, :128], alpha[-128:, -128:]]
refs = json.loads((Q / 'reference-hashes.json').read_text())
for item in refs:
    item['unchanged'] = hashlib.sha256(Path(item['path']).read_bytes()).hexdigest() == item['sha256']
assert all(item['unchanged'] for item in refs)
assert int(diff.max()) == 0
assert all(not np.any(corner) for corner in corners)
report = {
    'source': str(original), 'sourceSize': list(raw.size),
    'outputSize': [1024, 1024], 'mode': 'RGBA',
    'alphaBounds': halo.getchannel('A').getbbox(),
    'alphaRange': [int(alpha.min()), int(alpha.max())],
    'alphaMassCenterPixelIndices': com,
    'rotation180MaxChannelDifference': int(diff.max()),
    'rotation180DifferentPixels': int(np.any(diff, axis=2).sum()),
    'corners128x128AlphaMax': [int(corner.max()) for corner in corners],
    'transparentPixelFraction': float((alpha == 0).mean()),
    'partialAlphaPixelFraction': float(((alpha > 0) & (alpha < 255)).mean()),
    'bubbleCountVisual': 12,
    'mask': {'color': '#0B1A3A', 'opacity': .65, 'skyCropFromMock': sky_box},
    'hero': {'source': 'delivery/squad-blue.png', 'sourceAlphaBounds': blue_bbox, 'size': [hero_width, hero_height], 'position': hero_xy},
    'rotationPreviewDegrees': angles,
    'postprocess': 'premultiplied resize; 180-degree symmetric compositing with soft join at 60 degrees through a bubble-free gap; exterior alpha taper; no recoloring or painted new objects',
    'references': refs
}
(Q / 'delivery-audit.json').write_text(json.dumps(report, indent=2) + '\n')
print(json.dumps(report, indent=2))

from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

root = Path(__file__).resolve().parents[1]
master = Image.open(root / 'masters/reach-06.png').convert('RGBA')
# Coordinates are in the final canvas after the ten-pixel upward registration.
# Trace the generated body and paws; the exact pot stays behind.
outline = [
    (0, 0), (0, 154), (43, 154), (43, 158), (43, 163),
    (44, 168), (46, 173), (48, 179), (50, 184), (53, 189),
    (55, 194), (57, 197), (60, 198), (64, 198), (65, 200),
    (68, 202), (72, 202), (75, 200), (78, 200), (81, 201),
    (84, 200), (87, 199), (89, 198), (92, 197), (94, 194),
    (95, 191), (96, 188), (100, 189), (108, 190), (116, 189),
    (121, 188), (128, 186), (132, 187), (135, 190), (136, 194),
    (138, 197), (141, 198), (145, 199), (147, 201), (150, 203),
    (153, 202), (157, 201), (159, 202), (162, 201), (165, 201),
    (169, 198), (171, 195), (174, 191), (178, 184), (181, 179),
    (182, 174), (182, 169), (181, 163), (178, 157), (178, 154),
    (224, 154), (224, 0),
]
mask = Image.new('L', master.size)
ImageDraw.Draw(mask).polygon(
    [(round(x * master.width / 224), round((y + 10) * master.height / 300))
     for x, y in outline], fill=255)
mask = mask.filter(ImageFilter.GaussianBlur(.65))
body_only_mask = mask.copy()
a = np.array(master)
# Preserve the generated side leaves above the rim; the arms naturally cover them.
rgb = a[:, :, :3].astype(np.int16)
native_y = np.indices(a.shape[:2])[0] * 300 / master.height - 10
leaves = ((rgb[:, :, 1] > rgb[:, :, 0] * 1.15) &
          (rgb[:, :, 1] > rgb[:, :, 2] * 1.18) &
          (native_y >= 150) & (native_y < 191))
leaf_matte = Image.fromarray((leaves * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(3))
mask = Image.fromarray(np.maximum(np.array(mask), np.array(leaf_matte)))
a[:, :, 3] = ((a[:, :, 3].astype(np.uint16) *
               np.array(mask).astype(np.uint16) + 127) // 255).astype(np.uint8)
core = Image.fromarray(np.where(a[:, :, 3] > 12, 255, 0).astype(np.uint8)).copy()
ImageDraw.floodfill(core, (master.width // 2, int(master.height * .4)), 128)
core = Image.fromarray(np.where(np.array(core) == 128, 255, 0).astype(np.uint8))
core = core.filter(ImageFilter.MaxFilter(9))
a[np.array(core) == 0] = 0
scaled = Image.fromarray(a).convert('RGBa').resize(
    (224, 300), Image.Resampling.LANCZOS).convert('RGBA')
body = Image.new('RGBA', (224, 300))
body.alpha_composite(scaled, (0, -10))
a = np.array(body)
a[:6] = 0
# Resampling of the side leaves must not modify locked pixels below the rim.
allowed_lower = Image.new('L', (224, 300))
allowed_lower.paste(body_only_mask.resize((224, 300), Image.Resampling.LANCZOS), (0, -10))
outside_body = np.array(allowed_lower) == 0
a[192:][outside_body[192:]] = 0
body = Image.fromarray(a)

plate = Image.open(root / 'pot-plate-224x300.png').convert('RGBA')
base = np.array(plate)
idle = np.array(Image.open(root / 'references/koala-idle.png').convert('RGBA'))
# Restore only the original green leaves above the locked rim, shifted by 44 px.
leaf_source = idle[118:148].copy()
rgb = leaf_source[:, :, :3].astype(np.int16)
leaf_mask = ((rgb[:, :, 1] > rgb[:, :, 0] * 1.15) &
             (rgb[:, :, 1] > rgb[:, :, 2] * 1.18) & (leaf_source[:, :, 3] > 0))
base[162:192][leaf_mask] = leaf_source[leaf_mask]
result = np.array(Image.alpha_composite(Image.fromarray(base), body))
body_alpha = np.array(body)[:, :, 3]
lower = np.indices((300, 224))[0] >= 192
locked = lower & (body_alpha == 0)
original = np.array(plate)
result[locked] = original[locked]
final = Image.fromarray(result)
out = root / 'delivery/creatures/koala/42A9FF/reach.png'
out.parent.mkdir(parents=True, exist_ok=True)
final.save(out)
body.save(root / 'work/reach-v2-body.png')
Image.fromarray(body_alpha).save(root / 'work/reach-v2-body-mask.png')
preview = Image.new('RGBA', (672, 900), (231, 223, 241, 255))
preview.alpha_composite(final.resize(preview.size, Image.Resampling.NEAREST))
preview.save(root / 'work/reach-v2-check-3x.png')

def bounds(mask):
    ys, xs = np.where(mask)
    return [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())]

occluded = lower & (body_alpha > 0)
last_occluded_y = bounds(occluded)[3]
validation = {
    'file': str(out.relative_to(root)),
    'size': list(final.size), 'mode': final.mode,
    'sha256': hashlib.sha256(out.read_bytes()).hexdigest(),
    'generation_count_v2': 2, 'selected_master': 'masters/reach-06.png',
    'pot_source': 'pot-plate-224x300.png', 'pot_rim_y': 192, 'feet_line_y': 256,
    'first_six_rows_alpha_nonzero': int(np.count_nonzero(result[:6, :, 3])),
    'visible_alpha_threshold': 8,
    'visible_bounds_inclusive': bounds(result[:, :, 3] > 8),
    'body_bounds_inclusive': bounds(body_alpha > 8),
    'body_occlusion_below_rim_bounds_inclusive': bounds(occluded),
    'body_occlusion_below_rim_pixels': int(occluded.sum()),
    'locked_lower_region_pixels': int(locked.sum()),
    'locked_lower_region_different_pixels': int(np.any(result[locked] != original[locked], axis=1).sum()),
    'unoccluded_rows_from_y': last_occluded_y + 1,
    'unoccluded_rows_different_pixels': int(np.any(result[last_occluded_y + 1:] != original[last_occluded_y + 1:], axis=2).sum()),
    'source_low_alpha_padding_retained': True,
}
assert final.size == (224, 300) and final.mode == 'RGBA'
assert validation['first_six_rows_alpha_nonzero'] == 0
assert validation['locked_lower_region_different_pixels'] == 0
assert validation['unoccluded_rows_different_pixels'] == 0
assert validation['visible_bounds_inclusive'][3] == 255
(root / 'work/validation-v2.json').write_text(json.dumps(validation, indent=2) + '\n')
print(json.dumps(validation, indent=2))

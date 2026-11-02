#!/usr/bin/env python3
"""Register image_gen back-body layers and restore original foreground pixels.

No fur is drawn procedurally. Creative content is kept in masters; this script
only aligns, resamples, composites supplied foreground pixels and records QA.
"""
import hashlib
import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent
ROSTER = json.loads((ROOT / 'roster.json').read_text())
MASK_PROVENANCE = {e['creatureId']: e for e in json.loads((ROOT / 'qa/source-mask-manifest.json').read_text())}
CFG = {
    'koala': (108, 148), 'lion': (107, 148), 'green': (114, 144),
    'purple': (116, 148), 'red': (110, 152), 'charcoal': (115, 148),
    'slate-blue': (108, 152), 'cyan': (113, 150), 'lavender': (110, 152),
    'berry': (106, 151), 'brown': (110, 154), 'violet': (113, 150),
    'lime': (110, 152), 'mint': (116, 138), 'forest': (122, 142),
    'pink': (115, 154), 'peach': (106, 150), 'pale-mint': (116, 142),
}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def box(alpha):
    # Ignore disconnected single-pixel export specks in reference/master margins.
    mask = (alpha > 32).astype('uint8')
    n, labels, stats, _ = cv2.connectedComponentsWithStats(mask, 8)
    k = 1 + np.argmax(stats[1:, cv2.CC_STAT_AREA])
    x, y, w, h = map(int, stats[k, :4])
    return [x, y, x + w, y + h]


def compose(s):
    cid, hexcode = s[:2]
    refpath = ROOT / 'references' / f'{cid}-{hexcode}-idle.png'
    ref = Image.open(refpath).convert('RGBA')
    original = np.array(ref)
    assert ref.size == (224, 256)
    # These extraction masks were reviewed in the prior 18-character layer task.
    # Reuse only after checking that the source pixels are identical.
    provenance = MASK_PROVENANCE[cid]
    assert sha(refpath) == provenance['referenceSha256'], f'Changed reference: {cid}'
    maskpath = ROOT / 'qa/source-leaf-masks' / f'{cid}-leaf-mask.png'
    assert sha(maskpath) == provenance['maskSha256'], f'Changed mask: {cid}'
    raw_mask = Image.open(maskpath).convert('L')
    mask = np.array(raw_mask) > 0
    mask[148:] = False
    # Include the antialiased fringe without reinstating neighboring facial fur.
    fringe = np.array(raw_mask.filter(ImageFilter.MaxFilter(3))) > 0
    rgb = original[:, :, :3].astype(int)
    green = rgb[:, :, 1] - np.maximum(rgb[:, :, 0], rgb[:, :, 2]) > 8
    yy = np.indices(mask.shape)[0]
    mask |= fringe & green & (yy < 148) & (original[:, :, 3] > 0)
    masterpath = sorted((ROOT / 'masters').glob(f'{cid}-back-*.png'))[-1]
    master = Image.open(masterpath).convert('RGBA')
    sb = box(np.array(master)[:, :, 3])
    alpha = original[:, :, 3].copy()
    alpha[148:] = 0
    rb = box(alpha)
    # Per-character registration compensates for master layers with more hidden
    # lower torso. The extra fur is placed behind the fixed source foreground.
    body_bottom = {'lion': 170, 'berry': 174, 'mint': 170, 'pink': 174, 'lime': 174}.get(cid, 156)
    target = [rb[0], max(6, rb[1]), rb[2], body_bottom]
    if cid == 'forest':
        # Center the round rear skull over the leaves. The front bounds also
        # include the projecting left snout, which is hidden from this angle.
        center = CFG[cid][0]
        target[0], target[2] = center - 72, center + 72
    tile = master.crop(sb).convert('RGBa').resize(
        (target[2] - target[0], target[3] - target[1]), Image.Resampling.LANCZOS
    ).convert('RGBA')
    body = Image.new('RGBA', (224, 256))
    body.paste(tile, (target[0], target[1]))
    result = np.array(body)
    result[mask] = original[mask]

    split, rim = CFG[cid]
    cap = np.zeros(mask.shape, bool)
    for row in range(rim - 4, 148):
        left = np.where(mask[row, :split])[0]
        right = np.where(mask[row, split:])[0] + split
        if len(left) and len(right):
            cap[row, :int(left.max()) + 1] = True
            cap[row, int(right.min()):] = True
    if cid == 'mint':
        # Its turquoise pot rim starts at the sides at y~134 and reaches the
        # center at y142. Restore the curved colored rim, not a horizontal cut
        # through the generated shell or the source's forward-facing shell.
        hsv = cv2.cvtColor(original[:, :, :3], cv2.COLOR_RGB2HSV)
        pot_seed = (hsv[:, :, 0] >= 73) & (hsv[:, :, 0] <= 86) & (hsv[:, :, 2] > 110) & (yy >= 132)
        cap = np.array(Image.fromarray(pot_seed.astype('uint8') * 255).filter(ImageFilter.MaxFilter(3))) > 0
        cap &= (yy >= 132) & (yy < 148)
        cap[142:148] = True
    if cid in ('lion', 'lime'):
        # Mane/quills beside the pot belong to the turned body. They are not
        # part of the foreground rim, even where they share the pot's color.
        xx = np.indices(mask.shape)[1]
        cap &= (xx >= split - 65) & (xx <= split + 65)
    result[cap] = original[cap]
    # Keep generated hidden lower-body pixels inside the original lower outline,
    # avoiding a protruding shell/torso that terminates at the hard pot lock row.
    outside = (yy >= rim - 2) & (original[:, :, 3] < 16)
    if cid == 'mint':
        result[outside] = original[outside]
    if cid == 'charcoal':
        # The source flippers cross the y148 lock at the sides of the pot.
        # Their outer, featureless black fur reads identically from behind;
        # keep these connected contours instead of leaving floating locked tips.
        xx = np.indices(mask.shape)[1]
        flippers = (yy >= 110) & (yy < 158) & ((xx < 49) | (xx >= 183))
        weight = np.clip((yy - 110) / 34., 0, 1)
        weight = weight * weight * (3 - 2 * weight)
        w = weight[:, :, None]
        ra, oa = result[:, :, 3:4] / 255., original[:, :, 3:4] / 255.
        mixed_alpha = ra * (1 - w) + oa * w
        premul = result[:, :, :3] * ra * (1 - w) + original[:, :, :3] * oa * w
        mixed_rgb = np.divide(premul, mixed_alpha, out=np.zeros_like(premul), where=mixed_alpha > 0)
        mixed = np.clip(np.rint(np.concatenate((mixed_rgb, mixed_alpha * 255), axis=2)), 0, 255).astype('uint8')
        result[flippers] = mixed[flippers]
    if cid == 'mint':
        # This reference's front pot begins earlier than the common y=148 line.
        result[144:] = original[144:]
    # Literal RGBA byte lock, including RGB values in fully transparent pixels.
    result[148:] = original[148:]
    result[:6] = 0
    out = Image.fromarray(result)
    folder = ROOT / 'delivery' / 'creatures' / cid / hexcode
    folder.mkdir(parents=True, exist_ok=True)
    path = folder / 'back.png'
    out.save(path)
    actual = np.array(Image.open(path))
    potdiff = int(np.any(actual[148:] != original[148:], axis=2).sum())
    leafdiff = int(np.any(actual[mask] != original[mask], axis=1).sum())
    assert potdiff == leafdiff == 0
    assert actual.shape == (256, 224, 4) and not actual[:6, :, 3].any()
    qa = ROOT / 'qa'
    body.save(qa / f'{cid}-registered-body.png')
    Image.fromarray(mask.astype('uint8') * 255).save(qa / f'{cid}-leaf-lock-mask.png')
    Image.fromarray(cap.astype('uint8') * 255).save(qa / f'{cid}-upper-rim-lock-mask.png')
    frontshape = alpha > 32
    backshape = actual[:, :, 3] > 32
    backshape[148:] = False
    report = {
        'creatureId': cid, 'hex': hexcode, 'size': list(out.size), 'mode': out.mode,
        'reference': str(refpath.relative_to(ROOT)), 'referenceSha256': sha(refpath),
        'master': str(masterpath.relative_to(ROOT)), 'masterSha256': sha(masterpath),
        'output': str(path.relative_to(ROOT)), 'outputSha256': sha(path),
        'sourceBodyBounds': sb, 'registeredBodyBox': target,
        'potLockedFromY': 148, 'potLockedPixelCount': 224 * 108,
        'potRgbaDifferentPixels': potdiff, 'leafLockedPixelCountAbove148': int(mask.sum()),
        'leafRgbaDifferentPixels': leafdiff,
        'upperRimRgbaDifferentPixels': int(np.any(actual[cap] != original[cap], axis=1).sum()),
        'nontransparentPixelsAboveY6': int(np.count_nonzero(actual[:6, :, 3])),
        'referenceUpperBounds': rb, 'outputUpperBounds': box(actual[:148, :, 3]),
        'upperSilhouetteIoU': round(float((frontshape & backshape).sum() / (frontshape | backshape).sum()), 4),
        'footPlacement': 'Original rows y>=148 retained; source footline unchanged.'
    }
    comparison = Image.new('RGB', (448, 280), (222, 217, 234))
    draw = ImageDraw.Draw(comparison)
    for i, (label, im) in enumerate([('FRONT / original', ref), ('BACK / final', out)]):
        draw.text((i * 224 + 8, 6), f'{cid} / {label}', fill=(52, 43, 73))
        comparison.paste(im, (i * 224, 24), im)
    comparison.save(qa / f'{cid}-front-back.png')
    return ref, out, report


def main():
    (ROOT / 'qa').mkdir(exist_ok=True)
    done = [(s, compose(s)) for s in ROSTER if list((ROOT / 'masters').glob(f'{s[0]}-back-*.png'))]
    for n in range(0, len(done), 6):
        batch = done[n:n + 6]
        sheet = Image.new('RGB', (224 * len(batch), 560), (222, 217, 234))
        draw = ImageDraw.Draw(sheet)
        for col, (s, (ref, out, _)) in enumerate(batch):
            x = col * 224
            for y, label, im in [(0, 'FRONT', ref), (280, 'BACK', out)]:
                draw.text((x + 8, y + 5), f'{s[0]} / {label}', fill=(52, 43, 73))
                sheet.paste(im, (x, y + 24), im)
        sheet.save(ROOT / 'qa' / f'batch-{n // 6 + 1}-front-back.png')
    overview = Image.new('RGB', (1344, 280 * ((len(done) + 5) // 6)), (222, 217, 234))
    draw = ImageDraw.Draw(overview)
    for i, (s, (_, out, _)) in enumerate(done):
        x, y = (i % 6) * 224, (i // 6) * 280
        draw.text((x + 8, y + 6), f'{s[0]} / #{s[1]}', fill=(52, 43, 73))
        overview.paste(out, (x, y + 24), out)
    overview.save(ROOT / 'qa' / 'back-lineup.png')
    (ROOT / 'qa' / 'validation.json').write_text(json.dumps({
        'generator': 'built-in image_gen', 'characters': len(done),
        'assets': [item[2] for _, item in done]
    }, ensure_ascii=False, indent=2) + '\n')
    p = ROOT / 'prompts.json'
    log = json.loads(p.read_text())
    selected = {item[2]['master'] for _, item in done}
    for g in log['generations']:
        if (ROOT / g['master']).exists():
            g['status'] = 'selected' if g['master'] in selected else 'superseded'
    log['generation_count'] = len(log['generations'])
    p.write_text(json.dumps(log, ensure_ascii=False, indent=2) + '\n')
    print(json.dumps([{'id': s[0], 'iou': item[2]['upperSilhouetteIoU'], 'potDiff': item[2]['potRgbaDifferentPixels'], 'leafDiff': item[2]['leafRgbaDifferentPixels']} for s, item in done]))


if __name__ == '__main__':
    main()

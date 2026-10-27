"""Register AI-authored upper-body strips for the existing sprite atlas.

Only texture slicing, uniform resampling and anchor registration happen here;
the three poses and their alpha are authored by image_gen. Original pots are
sampled directly at runtime and are never repainted or included in these layers.
"""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageDraw

STATES = ('combat-inhale', 'combat-blow', 'combat-rebound')


def body_bounds(image):
    # Measure the connected silhouette, so faint detached fur pixels do not move
    # the paw baseline. This analytical mask never replaces the artwork's alpha.
    mask = image.getchannel('A').point(lambda a: 255 if a >= 32 else 0)
    seed = (image.width // 2, round(image.height * .6))
    if not mask.getpixel(seed):
        raise ValueError('Missing body at registration seed')
    ImageDraw.floodfill(mask, seed, 128)
    return mask.point(lambda a: 255 if a == 128 else 0).getbbox()


def load(root, out):
    batch = root / 'Art/PlushToy/PadCombat-20261027'
    config = batch / 'mouths.json'
    if not config.exists():
        return []
    mouths = json.loads(config.read_text())
    coverage = json.loads((batch / 'coverage.json').read_text())
    tiles, entries, review = [], [], []
    for char in coverage['characters']:
        hx = char['hex'].lstrip('#')
        path = batch / 'masters' / (hx + '-combat-strip.png')
        if not path.exists() or hx not in mouths:
            raise ValueError('Incomplete combat strip: ' + hx)
        strip = Image.open(path).convert('RGBA')
        if strip.width % 3:
            raise ValueError('Combat strip must have three equal columns: ' + hx)
        cw, ch = strip.width // 3, strip.height
        frames = [strip.crop((i * cw, 0, (i + 1) * cw, ch)) for i in range(3)]
        boxes = [body_bounds(im) for im in frames]
        if not all(boxes):
            raise ValueError('Empty combat frame: ' + hx)
        # One scale for all poses; only the paw baseline is registered per frame.
        # A full canvas fits in 214 px; tall ears/antlers get 194 px of visible height.
        scale = min(214 / cw, 194 / max(b[3] - b[1] for b in boxes))
        sw, sh = round(cw * scale), round(ch * scale)
        sx, sy = sw / cw, sh / ch
        ox = (224 - sw) // 2
        sha = hashlib.sha256(path.read_bytes()).hexdigest()
        # 2026-10-27 19:50: body-to-pot proportion. The strips were drawn without their pots; scale the body about
        # its paw line (registered at y 200) so the widest paw row spans ~1.28x the pot's measured width.
        pot = Image.open(char['source']).convert('RGBA').resize((224, 256), Image.Resampling.LANCZOS).getchannel('A')
        pot_rows = [[x for x in range(224) if pot.getpixel((x, y)) >= 128] for y in range(174, 201)]
        pot_widths = sorted(r[-1] - r[0] + 1 for r in pot_rows if len(r) > 20)
        info = dict(hex=hx, frames=[], fit=1.0)
        source = Path(char['source']).relative_to(root / 'Art/PlushToy')
        rev = dict(hex=hx, name=char['animalName'], source='../../' + source.as_posix(),
                   strip='../masters/' + path.name, slotId=char['slotId'], frames=[])
        for i, (im, box, state) in enumerate(zip(frames, boxes, STATES)):
            oy = 200 - round(box[3] * sy)
            packed = Image.new('RGBA', (224, 224))
            # Premultiplied resampling keeps transparent fur edges free of dark halos.
            # Trim to this connected body's bounds before packing. A neighbouring
            # cel's ear/whisker fragment at a strip boundary must not enter this cel.
            left, upper = round(box[0] * sx), round(box[1] * sy)
            right, lower = round(box[2] * sx), round(box[3] * sy)
            packed.alpha_composite(im.crop(box).convert('RGBa').resize((right-left, lower-upper), Image.Resampling.LANCZOS).convert('RGBA'), (ox+left, oy+upper))
            mx, my = mouths[hx][i]
            anchor = dict(state=state, mouthX=round(ox + mx * cw * sx, 3),
                          mouthY=round(oy + my * ch * sy, 3), top=round(oy + box[1] * sy, 3))
            info['frames'].append(anchor)
            rev['frames'].append(dict(anchor, sx=i * cw+box[0], sy=box[1], sw=box[2]-box[0], sh=box[3]-box[1],
                                      x=ox+left, y=oy+upper, w=right-left, h=lower-upper))
            tiles.append((f'plants/{char["slotId"]}/{hx}/{state}', packed, sha))
            alpha = packed.getchannel('A')
            for y in range(165, 200):
                row = [x for x in range(224) if alpha.getpixel((x, y)) >= 128]
                if row: info['paws'] = max(info.get('paws', 0), row[-1] - row[0] + 1)
        if pot_widths and info.get('paws'):
            info['fit'] = round(min(1.0, 1.28 * pot_widths[len(pot_widths) // 2] / info['paws']), 3)
        info.pop('paws', None)
        entries.append(info)
        review.append(rev)
    out.mkdir(parents=True, exist_ok=True)
    (out / 'pad-combat.json').write_text(json.dumps(dict(entries=entries), ensure_ascii=False, indent=2) + '\n')
    (batch / 'preview/registration.json').write_text(json.dumps(dict(characters=review), ensure_ascii=False, indent=2) + '\n')
    return tiles

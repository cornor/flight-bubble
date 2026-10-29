"""Offline preview of the flower ring on every member, same geometry as GardenSkin.FlowerRing.cs."""
import json, math, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT = Path(__file__).resolve().parents[3]
D = json.loads((ROOT / 'UnityProject/Assets/Flightbeat/Resources/GardenR1/flower-ring.json').read_text())
FLOWER = Image.open(ROOT / 'Art/PlushToy/HeadBubbleRing-20261027/delivery/flower-ring.png').convert('RGBA')
SIZE = 1.0
GEN = ROOT / 'Art/R1/art-v3/generated/program'
BEAD, HI = Image.open(GEN / 'bugs__bead-main.png').convert('RGBA'), Image.open(GEN / 'bugs__bead-fixed.png').convert('RGBA')
colors = json.loads((ROOT / 'Art/skin-colors.json').read_text())['colors']
roots = [ROOT / 'Art/PlushToy' / p / 'delivery' for p in ('PadReach-20261027', 'UniqueAnimals-20261026', 'P2-P4', 'FirstFive', 'P1')]
H = int(sys.argv[1]) if len(sys.argv) > 1 else 150          # drawn canvas height (px) of one member
W = round(H * 224 / 256)

def shot(canvas, color, cx, cy, d, alpha):
    d = max(2, round(d))
    b = BEAD.resize((d, d), Image.LANCZOS)
    tint = Image.new('RGBA', (d, d), color + (255,))
    body = Image.composite(Image.new('RGBA', (d, d)), Image.new('RGBA', (d, d)), b)
    px = Image.new('RGBA', (d, d))
    import numpy as np
    a = np.asarray(b).astype(float) / 255
    t = np.zeros_like(a); t[..., :3] = np.array(color) / 255 * a[..., :3]; t[..., 3] = a[..., 3] * alpha
    canvas.alpha_composite(Image.fromarray((t * 255).astype('uint8')), (round(cx - d / 2), round(cy - d / 2)))
    h = np.asarray(HI.resize((d, d), Image.LANCZOS)).astype(float); h[..., 3] *= alpha
    canvas.alpha_composite(Image.fromarray(h.astype('uint8')), (round(cx - d / 2), round(cy - d / 2)))

def member(hexv, cid, state, pose):
    rel = f'creatures/{cid}/{hexv}/{state}.png'
    p = next((r / rel for r in roots if (r / rel).is_file()), None) or next((r / rel.replace(state, 'idle') for r in roots if (r / rel.replace(state, 'idle')).is_file()))
    body = Image.open(p).convert('RGBA').resize((W, H), Image.LANCZOS)
    top = round(H * .55)
    cv = Image.new('RGBA', (W + 40, H + top), (226, 238, 250, 255))
    ox, oy = 20, top
    crown = next(c for c in D['crowns'] if c['hex'] == hexv)
    k = D['scale'] * W / 224 * SIZE
    sq = pose.get('squash', 0)
    fl = FLOWER.resize((max(1, round(FLOWER.width * k * (1 + sq * .5))), max(1, round(FLOWER.height * k * (1 - sq)))), Image.LANCZOS)
    fl = fl.rotate(-pose.get('sway', 0), resample=Image.BICUBIC, expand=False,
                   center=(D['stemX'] * k, D['stemCut'] * k * (1 - sq)))
    cx, cy = ox + W * crown['x'] / 224, oy + H * crown['y'] / 256
    cv.alpha_composite(fl, (round(cx - D['stemX'] * k * (1 + sq * .5)), round(cy - D['stemCut'] * k * (1 - sq))))
    cv.alpha_composite(body, (ox, oy))
    rx, ry = cx + (D['cx'] - D['stemX']) * k, cy + (D['cy'] - D['stemCut']) * k * (1 - sq)
    rr = D['r'] * k
    color = tuple(int(hexv[i:i + 2], 16) for i in (0, 2, 4))
    if 'bubble' in pose:
        g = pose['bubble']; e = 1 - (1 - g) ** 2
        shot(cv, color, rx, ry - rr * .85 * e, 2 * rr * (.45 + .75 * e), .55 + .4 * g)
    if 'pop' in pose:
        q = pose['pop']
        shot(cv, color, rx, ry - rr * (.85 + 3.2 * q), rr * 2.4 * (1 - .45 * q), 1 - q * q)
    return cv

poses = [('待机', 'idle', {}), ('鼓泡', 'attack', {'bubble': .85, 'squash': -.05}), ('弹出', 'attack', {'pop': .25, 'squash': .1, 'sway': 2})]
tiles = []
for c in colors:
    hexv = c['hex'].lstrip('#').upper()
    tiles.append((c['animalName'], [member(hexv, c['creatureId'], st, ps) for _, st, ps in poses]))
tw, th = tiles[0][1][0].size
cols = 4
rows = math.ceil(len(tiles) / cols)
font = ImageFont.truetype(str(ROOT / 'Art/fonts') + '/' + sorted(p.name for p in (ROOT / 'Art/fonts').iterdir() if p.suffix in ('.ttf', '.otf'))[0], 20)
sheet = Image.new('RGBA', (cols * (3 * tw + 16), rows * (th + 30)), (246, 248, 252, 255))
d = ImageDraw.Draw(sheet)
for i, (name, ims) in enumerate(tiles):
    x0, y0 = (i % cols) * (3 * tw + 16), (i // cols) * (th + 30)
    d.text((x0 + 6, y0 + 4), name, fill=(70, 60, 120), font=font)
    for j, im in enumerate(ims): sheet.alpha_composite(im, (x0 + j * tw, y0 + 28))
out = ROOT / 'Art/PlushToy/HeadBubbleRing-20261027/preview' / f'all-members-{H}.png'
sheet.convert('RGB').save(out)
print(out, sheet.size)

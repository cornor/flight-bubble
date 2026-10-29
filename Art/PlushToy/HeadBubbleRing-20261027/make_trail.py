"""Offline: a member firing at three targets; same rules as GardenSkin.BlownShot / BlownCurve / BlownEase."""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, str(Path(__file__).parent))
import make_preview as M
SOAP = Image.open(M.ROOT / 'Art/R1/home-v1/generated/soap-bubble-halo.png').convert('RGBA')
S = 150 / 64                                   # preview px per logical px
def ss(e): return e * e * (3 - 2 * e)
POPS = [Image.open(M.ROOT / f'Art/R1/home-v1/generated/soap-pop-{i}.png').convert('RGBA') for i in range(6)]
def img(cv, im, x, y, d, a, mul=None):
    b = np.asarray(im.resize((max(2, round(d * S)),) * 2, Image.LANCZOS)).astype(float)
    if mul is not None: b[..., :3] *= np.array(mul)
    b[..., 3] *= a; cv.alpha_composite(Image.fromarray(b.clip(0, 255).astype('uint8')), (round(x - d * S / 2), round(y - d * S / 2)))
def blown(cv, color, x, y, soap, shotd, dist, pop=True):   # GardenSkin.BlownShot (11:47, option C)
    u = (dist - 58) / 14
    if u >= 1: M.shot(cv, color, x, y, shotd * S, 1); return
    e = 0 if u <= 0 else ss(u); mix = .12 + .6 * e
    img(cv, SOAP, x, y, soap + (shotd - soap) * e, .78 * (1 - e), [1 + (c / 255 - 1) * mix for c in color])
    if u <= 0: return
    M.shot(cv, color, x, y, shotd * (.5 + .5 * e) * (1 + .15 * np.sin(e * np.pi)) * S, e)
    if pop:
        r = shotd * (.5 + .5 * u) * S
        ImageDraw.Draw(cv).ellipse([x - r, y - r, x + r, y + r], outline=(255, 255, 255, int(217 * (1 - u))), width=max(1, round((.6 + 1.6 * (1 - u)) * S / 2)))
def curve(f, t, to, wob=0):
    rise = min(110, .55 * np.hypot(to[0] - f[0], to[1] - f[1])); u = 1 - t
    c = (f[0] + wob, f[1] - rise)
    return tuple(f[i] * u * u + c[i] * 2 * u * t + to[i] * t * t for i in range(2))
tiles = []
for hexv in ('42A9FF', 'FF525B', '2D2D2D'):
    c = next(c for c in M.colors if c['hex'].lstrip('#').upper() == hexv)
    body = M.member(hexv, c['creatureId'], 'attack', {})
    for tx in (-120, 0, 120):
        W, Hh = 620, 980
        cv = Image.new('RGBA', (W, Hh), (226, 238, 250, 255))
        bx, by = W // 2 - body.width // 2, Hh - body.height
        cv.alpha_composite(body, (bx, by))
        crown = next(q for q in M.D['crowns'] if q['hex'] == hexv)
        k = M.D['scale'] * M.W / 224
        rx = bx + 20 + M.W * crown['x'] / 224 + (M.D['cx'] - M.D['stemX']) * k
        ry = by + round(M.H * .55) + M.H * crown['y'] / 256 + (M.D['cy'] - M.D['stemCut']) * k - M.D['r'] * k * .4
        f = (0.0, 0.0); to = (tx, -360.0)          # logical px from the ring
        ImageDraw.Draw(cv).ellipse((rx + tx * S - 14, ry - 360 * S - 14, rx + tx * S + 14, ry - 360 * S + 14), outline=(120, 120, 160), width=3)
        color = tuple(int(hexv[i:i + 2], 16) for i in (0, 2, 4))
        for i in range(11, -1, -1):               # 12 shots in flight, evenly spaced in time
            t = i / 11; te = t * (.55 + .45 * t)
            p = curve(f, te, to)
            dist = np.hypot(*p)
            blown(cv, color, rx + p[0] * S, ry + p[1] * S, 12 * 1.7, 12, dist)
        tiles.append(cv)
sheet = Image.new('RGB', (3 * 620, 3 * 980), (226, 238, 250))
for i, t in enumerate(tiles): sheet.paste(t.convert('RGB'), ((i % 3) * 620, (i // 3) * 980))
sheet = sheet.resize((sheet.width // 2, sheet.height // 2), Image.LANCZOS)
sheet.save(Path(__file__).parent / 'preview' / 'trail.png'); print(sheet.size)

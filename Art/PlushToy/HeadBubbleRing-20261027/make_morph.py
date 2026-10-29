"""Offline: three ways for a soap bubble to become the member's solid shot, frame by frame (one shot rising)."""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont
sys.argv = sys.argv[:1]
sys.path.insert(0, str(Path(__file__).parent))
import make_preview as M
G = M.ROOT / 'Art/R1/home-v1/generated'
SOAP = Image.open(G / 'soap-bubble-halo.png').convert('RGBA')
POPS = [Image.open(G / f'soap-pop-{i}.png').convert('RGBA') for i in range(6)]
S = 4.0                                    # preview px per logical px (zoomed in)
def paste(cv, im, x, y, d, a=1.0, tint=None):
    d = max(2, round(d * S)); b = np.asarray(im.resize((d, d), Image.LANCZOS)).astype(float)
    if tint is not None: b[..., :3] *= np.array(tint)
    b[..., 3] *= a; cv.alpha_composite(Image.fromarray(b.clip(0, 255).astype('uint8')), (round(x - d / 2), round(y - d / 2)))
def bead(cv, color, x, y, d, a=1.0): M.shot(cv, color, x, y, d * S, a)
def ss(e): e = min(1, max(0, e)); return e * e * (3 - 2 * e)
SH, SOAPD = 12, 20.4                       # shot size, soap size at the ring (logical px)
def film(c): return [1 + (v / 255 - 1) * .12 for v in c]
def A(cv, c, x, y, dist):                  # now: cross-fade
    e = ss((dist - 36) / 54)
    if e < .999: paste(cv, SOAP, x, y, SOAPD + (SH * 1.08 - SOAPD) * e, .78 * (1 - e), film(c))
    if e > .001: bead(cv, c, x, y, SH * (.35 + .65 * e), min(1, e * 1.6))
def B(cv, c, x, y, dist):                  # a coloured drop inside the bubble; the film bursts, the drop flies on
    burst = 62.0
    if dist < burst:
        core = SH * (.45 + .25 * ss(dist / burst))
        bead(cv, c, x, y, core)
        paste(cv, SOAP, x, y, SOAPD * (1 + .05 * np.sin(dist / 6)), .8, film(c))
    else:
        u = (dist - burst) / 22.0
        bead(cv, c, x, y, SH * (.7 + .3 * ss(u * 1.5)) * (1 + .12 * np.sin(min(1, u) * np.pi)))
        if u < 1: paste(cv, POPS[min(5, int(u * 6))], x, y, SOAPD * 1.25, 1 - .3 * u)
def C(cv, c, x, y, dist):                  # the bubble stays clear, then condenses in a blink with a glint
    k0 = 58.0; u = (dist - k0) / 14.0
    if u < 0: paste(cv, SOAP, x, y, SOAPD, .78, film(c)); return
    if u < 1:
        e = ss(u); paste(cv, SOAP, x, y, SOAPD + (SH - SOAPD) * e, .78 * (1 - e), [1 + (v / 255 - 1) * (.12 + .6 * e) for v in c])
        bead(cv, c, x, y, SH * (.5 + .5 * e) * (1 + .15 * np.sin(e * np.pi)), e)
        ImageDraw.Draw(cv).ellipse([x - SH * S * (.5 + .5 * u), y - SH * S * (.5 + .5 * u), x + SH * S * (.5 + .5 * u), y + SH * S * (.5 + .5 * u)],
                                   outline=(255, 255, 255, int(220 * (1 - u))), width=3)
    else: bead(cv, c, x, y, SH)
font = ImageFont.truetype(str(M.ROOT / 'Art/fonts/NotoSansSC.ttf'), 26)
rows = [('A 现在：交叉淡化', A), ('B 泡泡裹着彩珠，半路破掉', B), ('C 先透明，一眨眼凝结', C)]
dists = [0, 15, 30, 45, 55, 62, 68, 74, 82, 95]
colors = [(0x42, 0xA9, 0xFF), (0xFF, 0x52, 0x5B), (0x2D, 0x2D, 0x2D)]
cw, ch = 130, 150
sheet = Image.new('RGBA', (cw * len(dists) + 20, len(rows) * len(colors) * ch + len(rows) * 46), (226, 238, 250, 255))
d = ImageDraw.Draw(sheet); y0 = 0
for name, fn in rows:
    d.text((10, y0 + 8), name, fill=(70, 60, 120), font=font); y0 += 46
    for c in colors:
        for i, dist in enumerate(dists):
            cell = Image.new('RGBA', (cw, ch), (226, 238, 250, 255)); fn(cell, c, cw / 2, ch / 2, dist)
            sheet.alpha_composite(cell, (10 + i * cw, y0))
        y0 += ch
sheet.convert('RGB').save(Path(__file__).parent / 'preview' / 'morph-options.png'); print(sheet.size)

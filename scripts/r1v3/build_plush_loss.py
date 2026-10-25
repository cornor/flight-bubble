#!/usr/bin/env python3
"""Loss screen: editable gray ribbon/share control, plus registered plush artwork."""
from pathlib import Path
from PIL import Image, ImageDraw
from build_plush_controls import button_mold
from build_plush_result_ribbons import ribbon

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Art/PlushToy/P2-P4/delivery/lose'
MASTER = ROOT / 'Art/PlushToy/Loss-20261024/masters/hero-v2.png'


def share():
    m = button_mold(152, 152, (221, 160, 255), (179, 101, 237), 34)
    mask = Image.new('L', m.im.size)
    d = ImageDraw.Draw(mask)
    points = [(69, 31), (48, 31), (38, 41), (38, 81), (48, 90), (92, 90), (102, 80), (102, 69)]
    d.line([(x * 4, y * 4) for x, y in points], fill=255, width=8 * 4, joint='curve')
    for x, y in (points[0], points[-1]):
        d.ellipse(((x-4)*4, (y-4)*4, (x+4)*4, (y+4)*4), fill=255)
    arrow = [(59, 73), (59, 56), (69, 43), (87, 39), (87, 25), (117, 48),
             (87, 71), (87, 57), (76, 59), (70, 65), (70, 73)]
    d.polygon([(x * 4, y * 4) for x, y in arrow], fill=255)
    m.symbol(mask, (255, 255, 253), (246, 237, 251))
    return m.finish()


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    ribbon().save(OUT / 'ribbon.png')
    share().save(OUT / 'button-share.png')
    if MASTER.exists():
        # Uniform registration only; retain generated fur and transparent pixels.
        im = Image.open(MASTER).convert('RGBA')
        box = im.getchannel('A').point(lambda a: 255 if a > 16 else 0).getbbox()
        if not box:
            raise ValueError('Empty loss illustration')
        im = im.crop(box)
        im.thumbnail((500, 500), Image.Resampling.LANCZOS)
        canvas = Image.new('RGBA', (512, 512))
        canvas.alpha_composite(im, ((512-im.width)//2, (512-im.height)//2))
        canvas.save(OUT / 'hero.png')
    print('Loss assets: gray ribbon, purple share and plush hero')


if __name__ == '__main__':
    main()

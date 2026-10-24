#!/usr/bin/env python3
"""Loss screen: editable gray ribbon/share control, plus registered plush artwork."""
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from build_plush_controls import Mold, button_mold

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Art/PlushToy/P2-P4/delivery/lose'
MASTER = ROOT / 'Art/PlushToy/Loss-20261024/masters/hero-v2.png'


def polygon(m, points):
    distance = np.full_like(m.x, np.inf)
    inside = np.zeros_like(m.x, dtype=bool)
    for (ax, ay), (bx, by) in zip(points, points[1:] + points[:1]):
        dx, dy = bx - ax, by - ay
        t = np.clip(((m.x - ax) * dx + (m.y - ay) * dy) / (dx * dx + dy * dy), 0, 1)
        distance = np.minimum(distance, np.hypot(m.x - ax - t * dx, m.y - ay - t * dy))
        if dy:
            inside ^= ((ay > m.y) != (by > m.y)) & (m.x < ax + (m.y - ay) * dx / dy)
    return np.where(inside, distance, -distance)


def ribbon():
    m = Mold(840, 270)
    tail = [(164, 77), (31, 99), (14, 123), (63, 172), (36, 246), (196, 211), (211, 120)]
    for points in (tail, [(840 - x, y) for x, y in tail]):
        shape = polygon(m, points)
        m.shadow(shape, .26, 2.5, (0, 4))
        m.surface(shape, (143, 154, 174), (109, 122, 148), 5, 1.8, shine=.025)
    for points in ([(118, 153), (207, 210), (206, 151)], [(722, 153), (633, 210), (634, 151)]):
        m.surface(polygon(m, points), (88, 101, 127), (80, 94, 120), 2, .6, shine=0)
    face = m.rect((116, 22, 724, 194), 24)
    m.shadow(face, .3, 2, (0, 4))
    m.surface(m.rect((116, 27, 724, 199), 24), (112, 126, 151), (97, 111, 138), 4, 1, shine=.01)
    m.surface(face, (179, 187, 201), (147, 159, 180), 5, 1.5, shine=.03, bounds=(22, 194))
    return m.finish()


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

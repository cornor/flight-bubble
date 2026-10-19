#!/usr/bin/env python3
"""Temporary pictures for developing and testing the B-set pipeline (plan step 2, "临时图").
They are drawn from the A boards, so no final artwork is needed:

  flip     A board mirrored left-right (mode S: every colour count already matches)
  recolor  A board mirrored and with its colours swapped around (mode P: --auto-sigma recovers it)
  soft     the flip upscaled to 600 px and blurred, like a generated picture (tests quantizing)

  python3 scripts/levels_b/make_temp.py --level 3 [--kind flip|recolor|soft|all]
Pictures go to Art/levels-b/work/_temp/; they are never game resources.
"""
import argparse
import random
import sys
from pathlib import Path

from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
from levelsb_common import SIDE, WORK, file_tag, level_colors, load_a, rgb

TEMP = WORK / '_temp'


def board_rgb(level, cells):
    img = Image.new('RGB', (SIDE, SIDE))
    img.putdata([rgb(level, c) for c in cells])
    return img


def make(game, kind, output_dir=None):
    level, _ = load_a(game)
    flipped = board_rgb(level, level['cells']).transpose(Image.FLIP_LEFT_RIGHT)
    if kind == 'flip':
        img = flipped
    elif kind == 'soft':
        img = flipped.resize((600, 600), Image.NEAREST).filter(ImageFilter.GaussianBlur(2.2))
    elif kind == 'recolor':
        used = level_colors(level)
        shuffled = used[:]
        random.Random(game).shuffle(shuffled)
        swap = dict(zip(used, shuffled))
        cells = [swap[c] for c in level['cells']]
        img = board_rgb(level, cells).transpose(Image.FLIP_LEFT_RIGHT)
    else:
        raise ValueError(kind)
    out = Path(output_dir) if output_dir is not None else TEMP
    out.mkdir(parents=True, exist_ok=True)
    path = out / f'{file_tag(game)}_{kind}.png'
    img.save(path)
    return path


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--level', type=int, nargs='+', required=True)
    ap.add_argument('--kind', choices=['flip', 'recolor', 'soft', 'all'], default='all')
    args = ap.parse_args()
    for game in args.level:
        for kind in (['flip', 'recolor', 'soft'] if args.kind == 'all' else [args.kind]):
            print(make(game, kind))


if __name__ == '__main__':
    main()

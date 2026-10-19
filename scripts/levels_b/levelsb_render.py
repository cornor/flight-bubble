"""Preview drawing for the B-set tools: boards as bubbles in the level's own colours."""
from functools import lru_cache
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from levelsb_common import SIDE, rgb

FONTS = ['/System/Library/Fonts/PingFang.ttc', '/System/Library/Fonts/STHeiti Medium.ttc',
         '/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc', '/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc']


@lru_cache(maxsize=None)
def font(size):
    for f in FONTS:
        if Path(f).exists():
            return ImageFont.truetype(f, size)
    return ImageFont.load_default()


def board_image(cells, level, scale=6, marks=None, bubbles=None):
    """60 x 60 board. scale >= 5 draws round bubbles like the game; `marks` (set of cell indices)
    get a dark ring so refilled cells stand out."""
    bubbles = scale >= 5 if bubbles is None else bubbles
    img = Image.new('RGB', (SIDE * scale, SIDE * scale), (24, 28, 40))
    d = ImageDraw.Draw(img)
    for i, c in enumerate(cells):
        x, y = (i % SIDE) * scale, (i // SIDE) * scale
        col = rgb(level, c)
        if bubbles:
            d.ellipse([x, y, x + scale - 1, y + scale - 1], fill=col)
        else:
            d.rectangle([x, y, x + scale - 1, y + scale - 1], fill=col)
        if marks and i in marks:
            d.ellipse([x + 1, y + 1, x + scale - 2, y + scale - 2], outline=(0, 0, 0), width=max(1, scale // 4))
    return img

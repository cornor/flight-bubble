"""Register the edited old-style arched ribbon masters on the original UI canvas.

Shape and lighting are authored in the ImageGen masters. This import step only
trims transparent padding and scales uniformly; it does not redraw or recolor.
"""
from pathlib import Path
from PIL import Image

MASTER = Path(__file__).resolve().parents[2] / 'Art/PlushToy/ArchedRibbons-20261025/masters'


def ribbon(victory=False):
    image = Image.open(MASTER / ('win.png' if victory else 'lose.png')).convert('RGBA')
    bounds = image.getchannel('A').point(lambda a: 255 if a > 16 else 0).getbbox()
    if not bounds:
        raise ValueError('Empty result ribbon')
    image = image.crop(bounds)
    image.thumbnail((780, 204), Image.Resampling.LANCZOS)
    canvas = Image.new('RGBA', (800, 224))
    canvas.alpha_composite(image, ((800 - image.width) // 2, (224 - image.height) // 2))
    return canvas

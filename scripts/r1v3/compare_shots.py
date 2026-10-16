#!/usr/bin/env python3
"""Review the first four levels against the unchanged source-import captures.

Usage: compare_shots.py PLAY_DIR OUTPUT_PNG [SOURCE_PLAY_DIR]
Frames are from real input runs and are not synchronized pixel-diff fixtures.
"""
import sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
play, out = map(Path, sys.argv[1:3])
source = Path(sys.argv[3]) if len(sys.argv) > 3 else ROOT / 'Builds/R1-evidence/step1-play'
font = ImageFont.truetype(str(ROOT / 'Art/fonts/NotoSansSC.ttf'), 18)
width, gap, label = 270, 12, 32
paths = [[base / f'{i}-2.png' for i in range(4)] for base in (source, play)]
height = 480
for row in paths:
    for path in row:
        if path.exists():
            with Image.open(path) as im:
                height = max(height, round(im.height * width / im.width))
sheet = Image.new('RGB', (4 * (width + gap) + gap, 2 * (height + label + gap) + 48), '#F5F6F8')
draw = ImageDraw.Draw(sheet)
draw.text((gap, 8), '原样导入 / 当前包装：不同输入帧，仅核对 UI 和素材外观', font=font, fill='#202020')
for row, prefix in enumerate(('来源', '当前')):
    for col, path in enumerate(paths[row]):
        x, y = gap + col * (width + gap), 40 + row * (height + label + gap)
        title = f'{prefix} · 总第 {col + 1} 关' + ('' if path.exists() else '（缺截图）')
        draw.text((x, y), title, font=font, fill='#202020')
        if path.exists():
            with Image.open(path) as raw:
                im = raw.convert('RGB')
            im = im.resize((width, round(im.height * width / im.width)), Image.Resampling.LANCZOS)
            sheet.paste(im, (x, y + label))
sheet.save(out)
print(out, sheet.size)

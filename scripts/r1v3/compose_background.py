#!/usr/bin/env python3
"""Option 2 background (user, 2026-10-15 19:58): the item-1 background (sky, floating islands, waterfalls,
castle) with the item-56 stage painted over its lower part, flattened into ONE opaque 1080x1920 image.
Used by build_atlas_v3.py instead of delivery/scene/background.png while it exists; delete it when a
redrawn full background that keeps the islands (item 60) is accepted.

usage: python3 scripts/r1v3/compose_background.py
"""
from pathlib import Path
from PIL import Image
ROOT = Path(__file__).resolve().parents[2]
ORIG = ROOT / 'Art/R1/art-v3/delivery/_originals'
OUT = ROOT / 'Art/R1/art-v3/generated/background-composite.png'
sky = Image.open(ORIG / 'background-v1.png').convert('RGBA')
stage = Image.open(ORIG / 'stage-56.png').convert('RGBA')
assert sky.size == (1080, 1920) and stage.size == (1080, 772), (sky.size, stage.size)
sky.alpha_composite(stage, (0, 1920 - 772))   # stage spans logical y 574-960 (image y 1148-1920)
sky.convert('RGB').save(OUT)
print(OUT.relative_to(ROOT))

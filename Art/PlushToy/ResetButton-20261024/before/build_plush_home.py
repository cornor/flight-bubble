#!/usr/bin/env python3
"""Home CTA: gold modal-style two-line start button; background stays unchanged."""
from pathlib import Path
from PIL import Image, ImageDraw
from build_plush_controls import raised_button
ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Art/PlushToy/P2-P4/delivery/home'

def build():
    OUT.mkdir(parents=True, exist_ok=True)
    raised_button(520,240,(255,231,84),(255,185,13),48).save(OUT/'start-button.png')
    badge = Image.new('RGBA',(280,64))
    ImageDraw.Draw(badge).rounded_rectangle((0,0,279,63),radius=32,fill=(217,145,19,255))
    badge.save(OUT/'level-plate.png')

if __name__ == '__main__': build()

#!/usr/bin/env python3
"""Editable molded UI geometry, rendered at 4x for clean small-screen edges.

The toys remain the approved fur artwork. These lavender supports and controls
share a soft upper-left studio light, rounded bevels and contact shadows.
"""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter


class Mold:
    SCALE = 4

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.y, self.x = np.mgrid[:h*self.SCALE, :w*self.SCALE] / self.SCALE
        self.im = Image.new('RGBA', (w*self.SCALE, h*self.SCALE))

    def rect(self, box, radius):
        l, t, r, b = box
        qx = np.abs(self.x-(l+r)/2)-(r-l)/2+radius
        qy = np.abs(self.y-(t+b)/2)-(b-t)/2+radius
        return radius-np.hypot(np.maximum(qx, 0), np.maximum(qy, 0))-np.minimum(np.maximum(qx, qy), 0)

    def ellipse(self, box):
        l, t, r, b = box
        rx, ry = (r-l)/2, (b-t)/2
        return (1-np.hypot((self.x-(l+r)/2)/rx, (self.y-(t+b)/2)/ry))*min(rx, ry)

    def alpha(self, distance):
        return np.clip(distance*self.SCALE+.5, 0, 1)

    def shadow(self, distance, opacity=.25, blur=2, offset=(0, 3)):
        mask = Image.fromarray(np.uint8(self.alpha(distance)*255*opacity))
        mask = mask.filter(ImageFilter.GaussianBlur(blur*self.SCALE))
        layer = Image.new('RGBA', self.im.size, (86, 49, 144))
        layer.putalpha(mask)
        self.im.alpha_composite(layer, (round(offset[0]*self.SCALE), round(offset[1]*self.SCALE)))

    def surface(self, distance, top, bottom, bevel=3, rise=2, dome=0, shine=.12, bounds=None):
        # A smooth height profile makes a broad bevel instead of painted outlines.
        t = np.clip(distance/bevel, 0, 1)
        height = rise*np.sin(t*np.pi/2) + dome*np.maximum(distance, 0)
        gy, gx = np.gradient(height, 1/self.SCALE)
        normal = np.stack((-gx, -gy, np.ones_like(gx)), axis=-1)
        normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
        light = np.array([-.42, -.58, .7]); light /= np.linalg.norm(light)
        half = light + np.array([0, 0, 1]); half /= np.linalg.norm(half)
        diffuse = np.maximum(0, np.sum(normal*light, axis=-1))
        specular = np.maximum(0, np.sum(normal*half, axis=-1))**28 * shine
        y0, y1 = bounds or (0, self.h)
        v = np.clip((self.y-y0)/(y1-y0), 0, 1)[..., None]
        rgb = (np.array(top)*(1-v)+np.array(bottom)*v) * (.74+.34*diffuse[..., None])
        rgb += specular[..., None]*255
        rgba = np.dstack((np.clip(rgb, 0, 255), self.alpha(distance)*255))
        self.im.alpha_composite(Image.fromarray(np.uint8(rgba), 'RGBA'))

    def finish(self):
        return self.im.resize((self.w, self.h), Image.Resampling.LANCZOS)

    def symbol(self, mask, top, bottom):
        # Short inward distances give code-drawn icons the same rounded relief.
        distance = np.zeros((self.h*self.SCALE, self.w*self.SCALE))
        inset = mask
        for _ in range(12):
            distance += np.asarray(inset)/255/self.SCALE
            inset = inset.filter(ImageFilter.MinFilter(3))
        distance -= .5/self.SCALE
        self.shadow(distance, .38, .9, (0, 2))
        self.surface(distance, top, bottom, 2, 1.4, shine=.08)


def button_mold(w, h, top, bottom, radius=None):
    """Shallow lower lip and close contact shadow, visually matched to the reference.

    Keep the full source dimensions: sliced and prefab buttons share this face,
    centred around 47% of their canvas height, without expanding hit regions.
    These are rendering parameters, not measurements claimed for the reference.
    """
    m = Mold(w, h)
    margin, depth, shadow = max(3, h*.035), h*.045, h*.02
    face_bottom = h-margin-shadow-depth
    radius = min(radius or h*.42, (face_bottom-margin)/2)
    face = m.rect((margin, margin, w-margin, face_bottom), radius)
    base = m.rect((margin, margin+depth, w-margin, face_bottom+depth), radius)
    bottom = np.array(bottom)
    side_top = bottom*.86
    side_bottom = bottom*.77 + np.array([6, 3, 8])
    m.shadow(base, .22, h*.01, (0, h*.01))
    m.surface(base, side_top, side_bottom, max(1, h*.014), max(.5, h*.006), shine=.01)
    m.surface(face, top, bottom, h*.03, h*.010, shine=.045, bounds=(margin, face_bottom))
    return m


def raised_button(w, h, top, bottom, radius=None):
    return button_mold(w, h, top, bottom, radius).finish()


def modal_panel(w, h, radius):
    m = Mold(w, h)
    outer = m.rect((4, 3, w-4, h-12), radius)
    side = m.rect((4, 7, w-4, h-8), radius)
    m.shadow(side, .24, 1.3, (0, 1.5))
    m.surface(side, (195, 168, 227), (167, 136, 203), 2.5, .7, shine=.01)
    m.surface(outer, (235, 219, 255), (204, 177, 235), 3, 1.2, shine=.04)
    face = m.rect((10, 9, w-10, h-18), radius-6)
    m.surface(face, (255, 253, 244), (241, 229, 223), 3.5, .8, shine=.02)
    return m.finish()


def toggle(on):
    colors = ((172, 241, 126), (100, 202, 61)) if on else ((211, 195, 226), (169, 146, 194))
    m = button_mold(120, 64, *colors, 26)
    cx = 88 if on else 32
    knob = m.ellipse((cx-24, 5, cx+24, 53))
    m.shadow(knob, .25, .8, (0, 1.2))
    m.surface(m.ellipse((cx-24, 7, cx+24, 55)), (224, 224, 224), (182, 189, 178), 1.4, .6, shine=.01)
    m.surface(knob, (255, 255, 252), (244, 241, 246), 2.3, .9, dome=.015, shine=.05)
    if on:
        mask = Image.new('L', m.im.size); d = ImageDraw.Draw(mask)
        d.line([(17*4, 27*4), (26*4, 35*4), (42*4, 19*4)], fill=255, width=6*4, joint='curve')
        for x, y in ((17, 27), (26, 35), (42, 19)): d.ellipse(((x-3)*4, (y-3)*4, (x+3)*4, (y+3)*4), fill=255)
        m.symbol(mask, (255, 255, 252), (231, 245, 224))
    return m.finish()


def queue_card():
    m = Mold(192, 152)
    outer = m.rect((4, 3, 188, 145), 24)
    m.shadow(outer, .23, 2, (0, 3))
    m.surface(m.rect((4, 8, 188, 147), 24), (177, 144, 221), (155, 119, 208), 4, 2)
    m.surface(outer, (230, 212, 255), (197, 168, 238), 5, 2.6, shine=.10)
    # Concave bevel: shade on the top/left, a soft light lip on the bottom/right.
    inset = m.rect((12, 11, 180, 136), 18)
    m.surface(inset, (184, 151, 221), (209, 187, 239), 5, -2.4, shine=.03)
    return m.finish()


def platform():
    m = Mold(160, 64)
    m.shadow(m.ellipse((7, 25, 153, 59)), .30, 2.2, (0, 1))
    # The front wall is visibly thicker than the top rim at the final 60 px width.
    m.surface(m.ellipse((4, 16, 156, 58)), (184, 146, 237), (139, 97, 205), 3.5, 2.3, shine=.06, bounds=(16, 58))
    m.surface(m.ellipse((4, 7, 156, 49)), (238, 219, 255), (190, 155, 235), 4, 2.1, shine=.15, bounds=(7, 49))
    m.surface(m.ellipse((13, 12, 147, 44)), (180, 143, 225), (217, 193, 250), 3, -1.7, shine=.05, bounds=(12, 44))
    return m.finish()


def round_control(kind, size=128):
    # Author all symbols on the same 128 px grid, including the battle pause.
    m = Mold(128, 128)
    rim = m.ellipse((8, 5, 120, 115))
    base = m.ellipse((8, 10, 120, 120))
    m.shadow(base, .23, 1.2, (0, 1.2))
    m.surface(base, (173, 133, 225), (144, 101, 199), 2.5, .9, shine=.02)
    m.surface(rim, (238, 215, 255), (185, 145, 240), 3, 1.3, shine=.09)
    m.surface(m.ellipse((12, 9, 116, 111)), (203, 173, 249), (160, 116, 225), 4, 1.0, dome=.04, shine=.08)
    if kind == 'settings':
        dx, dy = m.x-64, m.y-57
        rho, angle = np.hypot(dx, dy), np.arctan2(dy, dx)
        tooth = np.clip((np.cos(angle*8)+.2)/.9, 0, 1)
        tooth = tooth*tooth*(3-2*tooth)
        symbol = np.minimum(27+7*tooth-rho, rho-12)
    elif kind == 'pause':
        symbol = np.maximum(m.rect((40, 30, 54, 83), 7), m.rect((74, 30, 88, 83), 7))
    elif kind == 'lock':
        shackle = m.rect((46, 32, 82, 74), 18)
        hole = m.rect((54, 40, 74, 68), 10)
        symbol = np.maximum(np.minimum(shackle, -hole), m.rect((40, 58, 88, 94), 8))
        key = np.maximum(m.ellipse((59, 67, 69, 77)), m.rect((62, 73, 66, 84), 2))
        symbol = np.minimum(symbol, -key)
    else:
        raise ValueError(kind)
    m.shadow(symbol, .48, 1.3, (0, 2.5))
    m.surface(symbol, (255, 253, 248), (225, 212, 245), 2.3, 2, shine=.12, bounds=(28, 96))
    return m.finish().resize((size, size), Image.Resampling.LANCZOS)


def locked_platform():
    im = Image.new('RGBA', (160, 160))
    im.alpha_composite(platform(), (0, 96))
    im.alpha_composite(round_control('lock', 110), (25, 0))
    return im

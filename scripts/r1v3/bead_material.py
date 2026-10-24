"""Approved B material: tintable sphere + fixed specular light, baked offline.

The 48 px master is prefiltered to 12 px for ordinary formation bubbles, whose
quads are about 9 px wide. Keeping this prefilter avoids harsh undersampling.
Runtime rendering still uses the existing two layers and palette tint.
"""
from functools import lru_cache

import numpy as np
from PIL import Image, ImageDraw, ImageFilter


def _smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def _rgba(rgb, alpha):
    rgba = np.concatenate([rgb, alpha[..., None]], axis=-1)
    return Image.fromarray(np.uint8(np.clip(rgba, 0, 1) * 255 + .5))


def _resize(im, size):
    return im.convert("RGBa").resize(size, Image.Resampling.LANCZOS).convert("RGBA")


@lru_cache(maxsize=3)
def _master(rim):
    px = 48
    n = px * 8
    yy, xx = np.mgrid[:n, :n]
    x = ((xx + .5) / n - .5) / .47
    y = ((yy + .5) / n - .5) / .47
    rho = np.sqrt(x*x + y*y)
    z = np.sqrt(np.maximum(0, 1 - rho*rho))
    alpha = 1 - _smooth(.995, 1.005, rho)
    light = np.array([-.43, -.52, .74])
    light /= np.linalg.norm(light)
    diffuse = np.clip(light[0]*x + light[1]*y + light[2]*z, 0, 1)
    # 2026-10-21 (user: more material and volume): a deeper light-to-shade range, a wider darker rim towards the
    # lower right, a brighter coloured bounce light inside the lower edge (was .66 + .34 d, rim .13-.20, bounce .075).
    value = .62 + .38 * diffuse ** 1.1
    edge = _smooth(.80, .995, rho)
    value *= 1 - edge * (.07 + .08 * np.clip((x+y)*.5 + .2, 0, 1))
    value += .075 * np.exp(-((x-.12)/.50)**2 - ((y-.60)/.20)**2)
    main = _rgba(np.repeat(np.clip(value, 0, 1)[..., None], 3, axis=2), alpha)

    dx, dy = x + .31, y + .43
    u, v = .866*dx - .5*dy, .5*dx + .866*dy
    # A crisper, brighter specular and a clear white glass reflection along the lower rim.
    soft = .14 * np.exp(-((u/.46)**2 + (v/.31)**2) * 1.8)
    core = .50 * (1 - _smooth(.6, 1.1, (u/.17)**2 + (v/.10)**2))
    reflection = .12 * np.exp(-((rho-.82)/.06)**2) * np.exp(-((x-.30)/.42)**2 - ((y-.70)/.30)**2)
    fa = (1 - (1-soft)*(1-core)*(1-reflection)) * alpha
    frgb = np.ones((n, n, 3))
    if rim is not None:
        rim_color = np.array(rim[:3]) / 255
        strength = .48 if rim[:3] == (150, 154, 160) else .80
        rim_alpha = edge * alpha * strength * (rim[3] / 255 if len(rim) > 3 else 1)
        total = fa + rim_alpha*(1-fa)
        frgb = (frgb*fa[..., None] + rim_color*rim_alpha[..., None]*(1-fa[..., None])) / np.maximum(total[..., None], 1e-8)
        fa = total
    fixed = _rgba(frgb, fa)
    return _resize(main, (px, px)), _resize(fixed, (px, px))


def _teaching_mark(px, mark):
    # Preserve the original four teaching symbols, positions and dark backing.
    s = 16
    n = px*s
    r = n*.47
    cx = cy = n/2
    w = r*.3
    mx, my = cx+r*.06, cy+r*.14
    mk = Image.new("RGBA", (n, n))
    draw = ImageDraw.Draw(mk)
    white = (255, 255, 255, 255)
    if mark == "dot":
        draw.ellipse([mx-w, my-w, mx+w, my+w], fill=white)
    elif mark == "tri":
        draw.polygon([(mx, my-w*1.1), (mx+w*1.05, my+w*.8), (mx-w*1.05, my+w*.8)], fill=white)
    elif mark == "square":
        draw.rectangle([mx-w*.85, my-w*.85, mx+w*.85, my+w*.85], fill=white)
    elif mark == "cross":
        draw.line([(mx-w, my-w), (mx+w, my+w)], fill=white, width=int(r*.2))
        draw.line([(mx-w, my+w), (mx+w, my-w)], fill=white, width=int(r*.2))
    else:
        raise ValueError(f"Unknown teaching mark: {mark}")
    shadow = mk.getchannel("A").filter(ImageFilter.MaxFilter(int(s*1.2) | 1))
    result = Image.new("RGBA", (n, n))
    result.paste(Image.new("RGBA", (n, n), (35, 64, 107, 255)), (0, 0), shadow)
    result.alpha_composite(mk)
    return _resize(result, (px, px))


def bead(px, teaching_mark=None, rim=None):
    main, fixed = _master(tuple(rim) if rim is not None else None)
    main, fixed = _resize(main, (px, px)), _resize(fixed, (px, px))
    if teaching_mark is not None:
        fixed.alpha_composite(_teaching_mark(px, teaching_mark))
    return main, fixed

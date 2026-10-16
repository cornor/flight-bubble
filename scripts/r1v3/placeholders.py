"""R1 v3 placeholders: program-drawn stand-ins for every art-AI file that has not been delivered yet.

Each function returns an RGBA image at the DELIVERY size (2x logical), so it goes through the
same scaling as the real file. Real deliveries in Art/R1/art-v3/delivery always win.
Style follows docs/r1/界面规范-v3.md loosely: soft sky, glossy capsules, navy badges.
"""
import math
from PIL import Image, ImageDraw, ImageFilter, ImageFont

SS = 4  # supersampling
NAVY = (31, 53, 87, 255)
OUTLINE = (35, 64, 107, 255)


def _ss(w, h):
    return Image.new('RGBA', (w * SS, h * SS), (0, 0, 0, 0))


def _down(im, w, h):
    return im.resize((w, h), Image.LANCZOS)


def _rgb(hexs):
    return tuple(int(hexs[i:i + 2], 16) for i in (0, 2, 4))


def _shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c[:3])


def _mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def capsule(w, h, fill, rim=None, rim_w=0.06, highlight=True, radius=None):
    im = _ss(w, h); d = ImageDraw.Draw(im); W, H = w * SS, h * SS
    r = (radius * SS) if radius else H / 2
    if rim:
        d.rounded_rectangle([0, 0, W - 1, H - 1], r, fill=rim)
        k = H * rim_w
        d.rounded_rectangle([k, k, W - 1 - k, H - 1 - k], max(1, r - k), fill=fill)
    else:
        d.rounded_rectangle([0, 0, W - 1, H - 1], r, fill=fill)
    if highlight:
        hl = _ss(w, h); dh = ImageDraw.Draw(hl)
        dh.rounded_rectangle([H * .18, H * .12, W - H * .18, H * .42], r * .5, fill=(255, 255, 255, 70))
        im.alpha_composite(hl)
    return _down(im, w, h)


def font(size, weight=700, path=None):
    f = ImageFont.truetype(str(path), size)
    try:
        f.set_variation_by_axes([weight])
    except Exception:
        pass
    return f


def outlined_text(im, xy, text, f, fill=(255, 255, 255, 255), outline=OUTLINE, width=3, anchor='mm'):
    d = ImageDraw.Draw(im)
    d.text(xy, text, font=f, fill=fill, anchor=anchor, stroke_width=width, stroke_fill=outline)


# ---------------- scene
def background(w=1080, h=1920):
    im = Image.new('RGBA', (w, h))
    top, mid, bot = (126, 196, 255), (206, 236, 255), (240, 247, 255)
    px = im.load()
    for y in range(h):
        t = y / h
        c = _mix(top, mid, t / .55) if t < .55 else _mix(mid, bot, (t - .55) / .45)
        for x in range(w):
            px[x, y] = c + (255,)
    clouds = Image.new('RGBA', (w, h), (0, 0, 0, 0)); d = ImageDraw.Draw(clouds)
    def blob(cx, cy, s, a=235):
        for dx, dy, r in ((0, 0, 1), (-.8, .25, .7), (.8, .25, .7), (-.4, -.35, .65), (.45, -.3, .6)):
            rr = s * r; d.ellipse([cx + dx * s - rr, cy + dy * s - rr, cx + dx * s + rr, cy + dy * s + rr], fill=(255, 255, 255, a))
    for cx, cy, s in ((40, 260, 90), (1040, 420, 100), (20, 900, 110), (1060, 1000, 90), (60, 1300, 120), (1020, 1340, 120)):
        blob(cx, cy, s, 200)
    for i in range(9):
        blob(60 + i * 125, 1820 + (i % 2) * 40, 150, 250)
    clouds = clouds.filter(ImageFilter.GaussianBlur(6))
    im.alpha_composite(clouds)
    return im


def ceiling(w=1080, h=80):
    im = _ss(w, h); d = ImageDraw.Draw(im); W, H = w * SS, h * SS
    d.rectangle([0, 0, W, H * .45], fill=(245, 251, 255, 255))
    n = 18
    for i in range(n + 1):
        cx = i * W / n + (W / n / 2 if i % 2 else 0); r = H * (.38 + .08 * (i % 3))
        d.ellipse([cx - r, H * .45 - r * .9, cx + r, H * .45 + r * .9], fill=(245, 251, 255, 255))
    shade = _ss(w, h); ds = ImageDraw.Draw(shade)
    for i in range(n + 1):
        cx = i * W / n + (W / n / 2 if i % 2 else 0); r = H * (.38 + .08 * (i % 3))
        ds.ellipse([cx - r * .9, H * .45 + r * .2, cx + r * .9, H * .45 + r * .9], fill=(190, 220, 245, 120))
    im.alpha_composite(shade.filter(ImageFilter.GaussianBlur(SS * 3)))
    return _down(im, w, h)


def rail(w=1080, h=44):
    im = _ss(w, h); d = ImageDraw.Draw(im); W, H = w * SS, h * SS
    glow = _ss(w, h); dg = ImageDraw.Draw(glow)
    dg.rounded_rectangle([0, H * .25, W, H * .75], H * .25, fill=(190, 245, 255, 150))
    im.alpha_composite(glow.filter(ImageFilter.GaussianBlur(SS * 4)))
    d.rounded_rectangle([0, H * .4, W, H * .6], H * .1, fill=(235, 252, 255, 255))
    for i in range(0, W, SS * 22):
        r = H * .14; d.ellipse([i - r, H * .5 - r, i + r, H * .5 + r], outline=(255, 255, 255, 230), width=SS * 2)
    return _down(im, w, h)


# ---------------- ui
def pause(s=88):
    im = _ss(s, s); d = ImageDraw.Draw(im); S = s * SS
    d.ellipse([0, 0, S - 1, S - 1], fill=(28, 98, 196, 255))
    d.ellipse([S * .06, S * .06, S * .94, S * .94], fill=(54, 140, 245, 255))
    d.ellipse([S * .2, S * .12, S * .55, S * .35], fill=(255, 255, 255, 90))
    for x in (.36, .56):
        d.rounded_rectangle([S * x, S * .3, S * (x + .1), S * .7], S * .04, fill=(255, 255, 255, 255))
    return _down(im, s, s)


def level(w=196, h=72):
    return capsule(w, h, (52, 76, 110, 215), rim=(150, 180, 215, 230), rim_w=.07)


def progress_frame(w=552, h=56):
    return capsule(w, h, (40, 60, 92, 235), rim=(170, 200, 235, 255), rim_w=.12, highlight=False)


def progress_fill(w=528, h=36):
    return capsule(w, h, (76, 214, 100, 255), rim=(46, 160, 70, 255), rim_w=.08)


def pad(w=140, h=70, dim=False):
    im = _ss(w, h); d = ImageDraw.Draw(im); W, H = w * SS, h * SS
    ring = (120, 190, 245, 255) if not dim else (130, 150, 170, 255)
    top = (232, 246, 255, 255) if not dim else (180, 192, 205, 255)
    d.ellipse([0, H * .1, W - 1, H - 1], fill=_shade(ring, .8) + (255,))
    d.ellipse([0, 0, W - 1, H * .88], fill=ring)
    d.ellipse([W * .07, H * .1, W * .93, H * .76], fill=top)
    d.ellipse([W * .2, H * .16, W * .55, H * .34], fill=(255, 255, 255, 140))
    return _down(im, w, h)


def pad_locked(w=140, h=140):
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    im.alpha_composite(pad(w, h // 2, dim=True), (0, h // 2))
    ov = _ss(w, h); d = ImageDraw.Draw(ov); W, H = w * SS, h * SS
    cx, cy, r = W / 2, H * .42, W * .3
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(220, 230, 240, 90))
    for a in range(0, 360, 20):
        a0, a1 = math.radians(a), math.radians(a + 11)
        d.arc([cx - r, cy - r, cx + r, cy + r], a, a + 11, fill=(255, 255, 255, 230), width=SS * 3)
    bw = r * .7
    d.rounded_rectangle([cx - bw / 2, cy - bw * .05, cx + bw / 2, cy + bw * .6], SS * 3, fill=(255, 255, 255, 255))
    d.arc([cx - bw * .3, cy - bw * .55, cx + bw * .3, cy + bw * .15], 180, 360, fill=(255, 255, 255, 255), width=SS * 5)
    d.ellipse([cx - bw * .08, cy + bw * .18, cx + bw * .08, cy + bw * .34], fill=(120, 140, 165, 255))
    im.alpha_composite(_down(ov, w, h))
    return im


def pad_swirl(s=160):
    im = _ss(s, s); S = s * SS; cx = cy = S / 2
    glow = _ss(s, s); dg = ImageDraw.Draw(glow)
    for i in range(int(S * .45), 0, -SS * 2):
        t = i / (S * .45); a = int(150 * (1 - t) ** 1.2)
        dg.ellipse([cx - i, cy - i, cx + i, cy + i], fill=(255, 205, 70, a))
    im.alpha_composite(glow.filter(ImageFilter.GaussianBlur(SS * 4)))
    sp = _ss(s, s); d = ImageDraw.Draw(sp)
    for arm in range(2):
        pts = []
        for k in range(220):
            t = k / 219; ang = t * 3.4 * math.pi + arm * math.pi; rad = S * (.04 + .38 * t)
            pts.append((cx + math.cos(ang) * rad, cy + math.sin(ang) * rad * .98))
        for k in range(len(pts) - 1):
            t = k / len(pts); a = int(255 * (1 - t) ** .7); w = int(SS * (6 - 4 * t))
            d.line([pts[k], pts[k + 1]], fill=(255, 236, 150, a), width=max(1, w))
    im.alpha_composite(sp.filter(ImageFilter.GaussianBlur(SS * .8)))
    dd = ImageDraw.Draw(im)
    for ang, rad in ((.6, .33), (2.1, .4), (3.7, .3), (5.0, .38)):
        x, y, r = cx + math.cos(ang) * S * rad, cy + math.sin(ang) * S * rad, SS * 3
        dd.ellipse([x - r, y - r, x + r, y + r], fill=(255, 245, 200, 220))
    dd.ellipse([cx - SS * 8, cy - SS * 8, cx + SS * 8, cy + SS * 8], fill=(255, 250, 225, 255))
    return _down(im, s, s)


def badge_ammo(w=96, h=36):
    return capsule(w, h, NAVY, rim=(120, 150, 190, 255), rim_w=.09)


def tier_coin(s=40):
    im = _ss(s, s); d = ImageDraw.Draw(im); S = s * SS
    d.ellipse([0, 0, S - 1, S - 1], fill=(205, 140, 20, 255))
    d.ellipse([S * .08, S * .08, S * .92, S * .92], fill=(255, 205, 60, 255))
    d.ellipse([S * .2, S * .14, S * .52, S * .36], fill=(255, 245, 190, 200))
    return _down(im, s, s)


def crown(w=48, h=36):
    im = _ss(w, h); d = ImageDraw.Draw(im); W, H = w * SS, h * SS
    pts = [(W * .08, H * .9), (W * .08, H * .35), (W * .3, H * .6), (W * .5, H * .22), (W * .7, H * .6), (W * .92, H * .35), (W * .92, H * .9)]
    d.polygon(pts, fill=(255, 200, 40, 255), outline=(200, 130, 10, 255))
    for x, y in ((.08, .3), (.5, .17), (.92, .3)):
        r = W * .08; d.ellipse([W * x - r, H * y - r, W * x + r, H * y + r], fill=(255, 245, 210, 255))
    return _down(im, w, h)


def queue_panel(w=192, h=360):
    im = _ss(w, h); d = ImageDraw.Draw(im); W, H = w * SS, h * SS
    r = W * .42
    d.rounded_rectangle([0, 0, W - 1, H - 1], r, fill=(150, 200, 240, 150))
    k = SS * 7
    d.rounded_rectangle([k, k, W - 1 - k, H - 1 - k], r - k, fill=(225, 240, 255, 70))
    d.rounded_rectangle([W * .1, H * .08, W * .2, H * .9], W * .05, fill=(255, 255, 255, 90))
    d.ellipse([W * .12, H * .9, W * .88, H * .99], outline=(190, 230, 255, 200), width=SS * 3)
    return _down(im, w, h)


def queue_corner(s=28):
    im = _ss(s, s); d = ImageDraw.Draw(im); S = s * SS; t = S * .28
    d.rounded_rectangle([0, 0, S, t], t / 2, fill=(255, 255, 255, 255))
    d.rounded_rectangle([0, 0, t, S], t / 2, fill=(255, 255, 255, 255))
    return _down(im, s, s)


def button(kind, label, fontpath, w=310, h=144):
    fills = {'shuffle': ((61, 139, 255), (30, 86, 190)), 'designate': ((255, 196, 50), (205, 120, 20)),
             'bomb': ((165, 96, 255), (100, 50, 190))}
    fill, rimc = fills[kind]
    im = capsule(w, h, fill + (255,), rim=rimc + (255,), rim_w=.07)
    ic = _ss(w, h); d = ImageDraw.Draw(ic); H = h * SS; cx, cy, s = H * .55, H * .5, H * .22
    white = (255, 255, 255, 255)
    if kind == 'shuffle':
        d.line([(cx - s, cy - s * .6), (cx + s, cy + s * .6)], fill=white, width=SS * 7)
        d.line([(cx - s, cy + s * .6), (cx + s, cy - s * .6)], fill=white, width=SS * 7)
        for y, sgn in ((cy + s * .6, 1), (cy - s * .6, -1)):
            d.polygon([(cx + s * 1.25, y), (cx + s * .75, y - s * .35), (cx + s * .75, y + s * .35)], fill=white)
    elif kind == 'designate':
        for ox in (-s * .55, s * .35):
            d.polygon([(cx + ox - s * .5, cy - s * .8), (cx + ox + s * .5, cy), (cx + ox - s * .5, cy + s * .8)], fill=white)
    else:
        d.ellipse([cx - s * .7, cy - s * .7, cx + s * .7, cy + s * .7], outline=white, width=SS * 6)
        for a in range(0, 360, 45):
            x, y = cx + math.cos(math.radians(a)) * s * 1.15, cy + math.sin(math.radians(a)) * s * 1.15
            d.ellipse([x - SS * 5, y - SS * 5, x + SS * 5, y + SS * 5], fill=white)
    im.alpha_composite(_down(ic, w, h))
    f = font(int(h * .3), 800, fontpath)
    outlined_text(im, (w * .62, h * .5), label, f, outline=_shade(rimc, .7) + (255,), width=4)
    return im


def badge_count(s=44):
    im = _ss(s, s); d = ImageDraw.Draw(im); S = s * SS
    d.ellipse([0, 0, S - 1, S - 1], fill=(255, 255, 255, 255))
    d.ellipse([S * .1, S * .1, S * .9, S * .9], fill=NAVY)
    return _down(im, s, s)


# ---------------- second batch stand-ins (bubble/glass style)
def popup_panel(w=120, h=120):
    return capsule(w, h, (246, 250, 255, 250), rim=(80, 130, 200, 255), rim_w=.06, highlight=False, radius=34)


def popup_ribbon(w=760, h=120):
    return capsule(w, h, (61, 139, 255, 255), rim=(30, 86, 190, 255), rim_w=.07)


def button_primary(w=360, h=156):
    return capsule(w, h, (61, 139, 255, 255), rim=(30, 86, 190, 255), rim_w=.07, radius=60)


def button_secondary(w=240, h=144):
    return capsule(w, h, (120, 150, 190, 255), rim=(70, 95, 135, 255), rim_w=.07, radius=40)


def toggle(on, w=120, h=64):
    im = capsule(w, h, ((76, 214, 100) if on else (150, 160, 175)) + (255,), rim=(255, 255, 255, 255), rim_w=.08, highlight=False)
    d = ImageDraw.Draw(im); r = h * .36; cx = w - h * .5 if on else h * .5
    d.ellipse([cx - r, h / 2 - r, cx + r, h / 2 + r], fill=(255, 255, 255, 255))
    return im


def card_frame(w=520, h=720):
    return capsule(w, h, (246, 250, 255, 245), rim=(80, 130, 200, 255), rim_w=.03, highlight=False, radius=60)


def card_rays(s=800):
    im = _ss(s, s); d = ImageDraw.Draw(im); S = s * SS; c = S / 2
    for i in range(16):
        a0 = i * 2 * math.pi / 16; a1 = a0 + math.pi / 22
        d.polygon([(c, c), (c + math.cos(a0) * S, c + math.sin(a0) * S), (c + math.cos(a1) * S, c + math.sin(a1) * S)], fill=(255, 230, 140, 120))
    mask = Image.new('L', (S, S), 0); dm = ImageDraw.Draw(mask)
    for i in range(int(S / 2), 0, -SS * 4):
        dm.ellipse([c - i, c - i, c + i, c + i], fill=int(255 * (1 - i / (S / 2)) ** .6))
    im.putalpha(Image.composite(im.getchannel('A'), Image.new('L', (S, S), 0), mask))
    return _down(im, s, s)


def speech_bubble(w=160, h=160):
    return capsule(w, h, (255, 255, 255, 250), rim=(80, 130, 200, 255), rim_w=.05, highlight=False, radius=40)


def pointer(s=64):
    im = _ss(s, s); d = ImageDraw.Draw(im); S = s * SS
    d.polygon([(S * .3, S * .05), (S * .7, S * .05), (S * .7, S * .45), (S * .9, S * .45), (S * .5, S * .95), (S * .1, S * .45), (S * .3, S * .45)], fill=(255, 205, 60, 255), outline=(200, 130, 10, 255))
    return _down(im, s, s)


def banner(kind, w=580, h=104):
    if kind == 'warning':
        return capsule(w, h, (240, 84, 84, 245), rim=(160, 40, 40, 255), rim_w=.08)
    return capsule(w, h, (52, 76, 110, 225), rim=(150, 180, 215, 240), rim_w=.08)


# ---------------- creatures
FEATURES = {'blue': 'fins', 'orange': 'horns', 'green': 'bumps', 'purple': 'ears', 'yellow': 'antennae',
            'white': 'pill', 'red': 'quiff', 'charcoal': 'spikes'}


def creature(cid, hexs, state, w=224, h=256):
    """Feet at y=212, centered at x=112; body 120-150 tall; nothing below y=212."""
    im = _ss(w, h); d = ImageDraw.Draw(im)
    base = _rgb(hexs)
    col = _shade(base, .82) if state == 'spent' else base
    outline = (205, 212, 222) if cid == 'charcoal' else ((140, 150, 162) if cid == 'white' else _shade(base, .55))
    feat = FEATURES[cid]
    cx, feet = 112 * SS, 212 * SS
    bw, bh = (150, 128) if feat != 'pill' else (128, 138)
    if state == 'attack': bw, bh = bw * 1.08, bh * .9
    if state == 'spent': bw, bh = bw * 1.06, bh * .82
    bw, bh = bw * SS, bh * SS
    tilt = 10 * SS if state == 'spent' else 0
    box = [cx - bw / 2 + tilt, feet - bh, cx + bw / 2 + tilt, feet]
    lw = 5 * SS

    def blob(b, fill):
        if feat == 'pill':
            d.rounded_rectangle(b, (b[2] - b[0]) * .45, fill=fill)
        elif feat == 'quiff':  # inverted heart
            x0, y0, x1, y1 = b; mx = (x0 + x1) / 2; r = (x1 - x0) * .29
            d.ellipse([x0, y0, mx + r * .25, y0 + 2 * r], fill=fill); d.ellipse([mx - r * .25, y0, x1, y0 + 2 * r], fill=fill)
            d.polygon([(x0 + r * .05, y0 + r * 1.2), (x1 - r * .05, y0 + r * 1.2), (mx, y1)], fill=fill)
        elif feat == 'antennae':
            d.rounded_rectangle(b, (b[2] - b[0]) * .3, fill=fill)
        else:
            d.ellipse(b, fill=fill)

    # features behind/around the body
    x0, y0, x1, y1 = box; mx = (x0 + x1) / 2
    if feat == 'horns':
        for sx in (-1, 1):
            hx = mx + sx * bw * .17
            d.polygon([(hx - 12 * SS, y0 + 10 * SS), (hx + 12 * SS, y0 + 10 * SS), (hx + sx * 6 * SS, y0 - 22 * SS)], fill=outline + (255,))
            d.polygon([(hx - 8 * SS, y0 + 10 * SS), (hx + 8 * SS, y0 + 10 * SS), (hx + sx * 5 * SS, y0 - 15 * SS)], fill=_shade(col, 1.08) + (255,))
    if feat == 'antennae':
        for sx in (-1, 1):
            ax = mx + sx * bw * .2; d.line([(ax, y0 + 6 * SS), (ax + sx * 10 * SS, y0 - 26 * SS)], fill=outline + (255,), width=lw)
            d.ellipse([ax + sx * 10 * SS - 9 * SS, y0 - 35 * SS, ax + sx * 10 * SS + 9 * SS, y0 - 17 * SS], fill=col + (255,), outline=outline + (255,), width=lw // 2)
    if feat == 'ears':
        for sx in (-1, 1):
            ex = mx + sx * bw * .3
            d.ellipse([ex - 18 * SS, y0 - 22 * SS, ex + 18 * SS, y0 + 30 * SS], fill=outline + (255,))
            d.ellipse([ex - 13 * SS, y0 - 17 * SS, ex + 13 * SS, y0 + 25 * SS], fill=col + (255,))
    if feat == 'bumps':
        for i, sx in enumerate((-.25, 0, .25)):
            bx = mx + sx * bw; d.ellipse([bx - 20 * SS, y0 - 16 * SS, bx + 20 * SS, y0 + 22 * SS], fill=outline + (255,))
            d.ellipse([bx - 15 * SS, y0 - 11 * SS, bx + 15 * SS, y0 + 17 * SS], fill=col + (255,))
    if feat == 'fins':
        for sx in (-1, 1):
            fx = (x0 if sx < 0 else x1)
            d.polygon([(fx, y0 + bh * .45), (fx + sx * 26 * SS, y0 + bh * .3), (fx + sx * 18 * SS, y0 + bh * .65)], fill=outline + (255,))
    if feat == 'spikes':
        n = 16
        for k in range(n):
            a = math.pi * (1 + k / (n - 1))
            sx0, sy0 = mx + math.cos(a) * bw * .46, (y0 + y1) / 2 + math.sin(a) * bh * .46
            sx1, sy1 = mx + math.cos(a) * bw * .62, (y0 + y1) / 2 + math.sin(a) * bh * .62
            d.line([(sx0, sy0), (sx1, sy1)], fill=outline + (255,), width=lw * 2)
    # body with outline and shading
    blob([x0 - lw, y0 - lw, x1 + lw, y1 + lw], outline + (255,))
    blob(box, col + (255,))
    sh = _ss(w, h); dsh = ImageDraw.Draw(sh)
    dsh.ellipse([x0 + bw * .1, y0 + bh * .55, x1 - bw * .05, y1 + bh * .1], fill=_shade(col, .82) + (110,))
    im.alpha_composite(Image.composite(sh.filter(ImageFilter.GaussianBlur(SS * 6)), Image.new('RGBA', im.size, (0, 0, 0, 0)), im.getchannel('A')))
    d = ImageDraw.Draw(im)
    d.ellipse([x0 + bw * .16, y0 + bh * .12, x0 + bw * .4, y0 + bh * .3], fill=(255, 255, 255, 120))
    if feat == 'quiff':
        d.polygon([(mx - 4 * SS, y0 + 4 * SS), (mx + 18 * SS, y0 - 26 * SS), (mx + 10 * SS, y0 + 8 * SS)], fill=col + (255,), outline=outline + (255,))
    # face
    ey = y0 + bh * (.42 if state != 'spent' else .46); ex = bw * .17
    for sx in (-1, 1):
        x = mx + sx * ex
        if state == 'spent':
            d.chord([x - 14 * SS, ey - 12 * SS, x + 14 * SS, ey + 14 * SS], 0, 180, fill=(255, 255, 255, 255), outline=(30, 30, 40, 255), width=SS * 2)
            d.ellipse([x - 6 * SS, ey + 1 * SS, x + 6 * SS, ey + 11 * SS], fill=(30, 30, 40, 255))
        else:
            d.ellipse([x - 15 * SS, ey - 17 * SS, x + 15 * SS, ey + 17 * SS], fill=(255, 255, 255, 255), outline=(30, 30, 40, 255), width=SS * 2)
            d.ellipse([x - 8 * SS, ey - 6 * SS, x + 8 * SS, ey + 12 * SS], fill=(30, 30, 40, 255))
            d.ellipse([x - 4 * SS, ey - 3 * SS, x + 1 * SS, ey + 2 * SS], fill=(255, 255, 255, 255))
    my = ey + 30 * SS
    if state == 'attack':
        d.ellipse([mx - 14 * SS, my - 10 * SS, mx + 14 * SS, my + 16 * SS], fill=(90, 30, 40, 255), outline=(30, 30, 40, 255), width=SS * 2)
        for sx in (-1, 1):
            cxk = mx + sx * bw * .32; d.ellipse([cxk - 14 * SS, my - 12 * SS, cxk + 14 * SS, my + 8 * SS], fill=(255, 140, 150, 110))
    elif state == 'spent':
        d.arc([mx - 14 * SS, my, mx + 14 * SS, my + 16 * SS], 200, 340, fill=(30, 30, 40, 255), width=SS * 3)
        d.ellipse([x1 - 10 * SS, y0 + 4 * SS, x1 + 4 * SS, y0 + 24 * SS], fill=(140, 200, 255, 220))
    else:
        d.arc([mx - 16 * SS, my - 12 * SS, mx + 16 * SS, my + 10 * SS], 20, 160, fill=(30, 30, 40, 255), width=SS * 3)
    out = _down(im, w, h)
    # hard guarantee: nothing below the feet line
    out.paste((0, 0, 0, 0), (0, 213, w, h))
    return out


def projectile(hexs, s=64):
    im = _ss(s, s); d = ImageDraw.Draw(im); S = s * SS; c = _rgb(hexs)
    d.ellipse([S * .1, S * .1, S * .9, S * .9], fill=c + (70,), outline=c + (255,), width=SS * 5)
    d.ellipse([S * .25, S * .22, S * .45, S * .38], fill=(255, 255, 255, 230))
    return _down(im, s, s)


def round_creature(hexs, state, w=224, h=256):
    """Plain round stand-in, two eyes, navy outline; baseline at y=212."""
    im = _ss(w, h); d = ImageDraw.Draw(im)
    rgb = _rgb(hexs)
    if state == 'spent': rgb = _shade(rgb, .82)
    body_w, body_h = (158, 132) if state == 'attack' else (150, 112) if state == 'spent' else (144, 144)
    box = tuple(int(v * SS) for v in (112-body_w/2, 212-body_h, 112+body_w/2, 212))
    ink = (35, 64, 107)
    d.ellipse(box, fill=rgb, outline=ink, width=6*SS)
    for x in (90, 134):
        y = 212-body_h*.55
        if state == 'spent':
            d.line([(int((x-6)*SS),int(y*SS)),(int((x+6)*SS),int(y*SS))],fill=ink,width=4*SS)
        else:
            d.ellipse(tuple(int(v*SS) for v in (x-6,y-8,x+6,y+8)),fill=ink)
    return _down(im, w, h)

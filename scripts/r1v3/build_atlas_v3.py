#!/usr/bin/env python3
"""R1 v3 (bubble squad) atlas: art-AI deliveries, program placeholders for anything missing,
program-drawn beads / mosaic panel, v2 battle FX frames, and the glyph block.

usage: python3 scripts/r1v3/build_atlas_v3.py [--delivery DIR] [--font PATH]
- Reads finished PNGs from Art/R1/art-v3/delivery (2x logical size); any file not yet delivered
  is replaced by a placeholder from placeholders.py (listed as "placeholder" in the output).
- Stores UI and characters in separate 2048x2048 atlases. The full background has its own
  texture (background.png, 810x1440 = x0.75). Character scale retries down to 0.62 if needed.
- Creatures are full-color per level color under the existing slot names plants/<internal>/<HEX>/<state>
  (docs/r1/界面规范-v3.md section 5 maps creature ids to internal ids).
- Battle FX: each strip comes from art-v3/delivery/fx when delivered, otherwise from the v2 delivery
  (same file names, sizes and frame counts as v2, so the game code is unchanged).
- Glyphs: rebuilt from Noto Sans SC when the character set in code/prefabs changed.
"""
import argparse, hashlib, json, math, os, re, sys
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'scripts/r1v2')); sys.path.insert(0, str(Path(__file__).parent))
from build_fx_atlas import SPECS as FX_SPECS, write_index
# Third batch (art item 79/80): rocket-pop (MAX bubble shot burst, finale pops) and rescue-bubble (revive purge).
# Updated in place so build_fx_atlas.write_index also lists them in fx-atlas.json. Delivered only in art-v3.
FX_SPECS.update({'rocket-pop': 6, 'rescue-bubble': 1})
import placeholders as P

ap = argparse.ArgumentParser()
ap.add_argument('--delivery', default=str(ROOT / 'Art/R1/art-v3/delivery'))
ap.add_argument('--fx-delivery', default=str(ROOT / 'Art/R1/art-v2/delivery'))
ap.add_argument('--out', default=str(ROOT / 'UnityProject/Assets/Flightbeat/Resources/GardenR1'))
ap.add_argument('--generated', default=str(ROOT / 'Art/R1/art-v3/generated'))
ap.add_argument('--code', default=str(ROOT / 'UnityProject/Assets/Flightbeat'))
ap.add_argument('--font', default=str(ROOT / 'Art/fonts/NotoSansSC.ttf'))
args = ap.parse_args()
DEL, FXDEL, OUT, GEN, CODE, FONT = map(Path, (args.delivery, args.fx_delivery, args.out, args.generated, args.code, args.font))
SIZE, CELL, FONT_SIZE = 2048, 40, 32
# 2026-10-19 (build speed): skip the whole run when nothing it reads has changed since the last successful run.
# build.sh calls this twice (before and after the prefabs are regenerated); the second call normally finds nothing
# new. Inputs: the art deliveries, fonts, colours, these scripts (size + mtime), the C# sources and level files, and
# the non-ASCII characters in the prefabs (only their glyphs matter here). R1_ATLAS_FORCE=1 rebuilds anyway.
_CACHE = ROOT / 'Builds/.atlas-cache.json'
def _fingerprint():
    h = hashlib.sha256()
    h.update((ROOT / 'Art/skin-colors.json').read_bytes())   # rewritten by this script each run: hash the content
    files = []
    for r in (DEL, FXDEL, (DEL / '../../home-v1/delivery').resolve(), FONT.parent, ROOT / 'scripts/r1v3', ROOT / 'scripts/r1v2',
              CODE / 'Resources/levels'):
        if r.exists(): files += [p for p in r.rglob('*') if p.is_file() and '__pycache__' not in p.parts and p.name != '.DS_Store']
    for p in sorted(set(files)):
        st = p.stat(); h.update(f'{p}|{st.st_size}|{st.st_mtime_ns}\n'.encode())
    for p in sorted(CODE.rglob('*.cs')):
        if p.name != 'GardenColorCatalog.Generated.cs': h.update(p.read_bytes())
    chars = set()
    for p in CODE.rglob('*.prefab'):
        chars |= set(c for c in re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m[1], 16)), p.read_text(encoding='utf-8')) if ord(c) >= 128)
    h.update(''.join(sorted(chars)).encode())
    return h.hexdigest()
_FP = os.environ.get('R1_ATLAS_FP') or _fingerprint()
os.environ['R1_ATLAS_FP'] = _FP          # kept across the creature-scale re-run (os.execv below)
if os.environ.get('R1_ATLAS_FORCE') != '1' and _CACHE.exists() and all((OUT / n).exists() for n in ('atlas.json', 'atlas.png', 'creatures.png')):
    try: _cached = json.loads(_CACHE.read_text())
    except Exception: _cached = {}
    if _cached.get('fingerprint') == _FP:
        print('atlas unchanged, skipped (R1_ATLAS_FORCE=1 to rebuild)', file=sys.stderr)
        print(_cached.get('summary', '{}'))
        raise SystemExit(0)
FONT_SHA = 'a3041811a78c361b1de50f953c805e0244951c21c5bd412f7232ef0d899af0da'
UI = 0.75
# Creature canvases are stored at the UI scale (224x256 -> 168x192 px, display canvas at most 118 logical px).
# If blink/cheer poses (items 58/59) make the 2048 atlas overflow, the script re-runs itself with a smaller
# creature scale, down to 0.62 (139x159 px); the scale used is printed as creatureScale.
CREATURE = float(os.environ.get('R1_CREATURE_SCALE', UI))

# ---- (slot id, delivery file, scale, placeholder factory)
SLOTS = [
    ('scene/ceiling', 'scene/ceiling.png', UI, P.ceiling),
    ('scene/fence', 'scene/rail.png', UI, P.rail),
    ('scene/stage', 'scene/stage.png', 0.5, None),   # optional painted lower-half stage; program tray when absent
    ('scene/pause', 'ui/pause.png', UI, P.pause),
    ('scene/level', 'ui/level.png', UI, P.level),
    ('scene/progress-frame', 'ui/progress-frame.png', UI, P.progress_frame),
    ('scene/progress-fill', 'ui/progress-fill.png', UI, P.progress_fill),
    ('scene/plot', 'ui/pad.png', UI, P.pad),
    ('scene/plot-locked', 'ui/pad-locked.png', UI, P.pad_locked),
    ('scene/pad-swirl', 'ui/pad-swirl.png', UI, P.pad_swirl),
    ('scene/badge-ammo', 'ui/badge-ammo.png', UI, P.badge_ammo),
    ('scene/tier-coin', 'ui/tier-coin.png', UI, P.tier_coin),
    ('scene/crown', 'ui/crown.png', UI, P.crown),
    ('scene/queue-panel', 'ui/queue-panel.png', UI, P.queue_panel),
    ('scene/queue-corner', 'ui/queue-corner.png', UI, P.queue_corner),
    ('scene/button-shuffle', 'ui/button-shuffle.png', UI, lambda: P.button('shuffle', '重排', FONT)),
    ('scene/button-designate', 'ui/button-designate.png', UI, lambda: P.button('designate', '优先出战', FONT)),
    ('scene/button-water', 'ui/button-bomb.png', UI, lambda: P.button('bomb', '泡泡炸弹', FONT)),
    ('scene/badge', 'ui/badge-count.png', UI, P.badge_count),
    # second batch (v3 file names; placeholders until delivered)
    ('ui/popup-panel', 'ui2/popup-panel.png', UI, P.popup_panel),
    ('ui/popup-ribbon', 'ui2/popup-ribbon.png', UI, P.popup_ribbon),
    ('ui/button-primary', 'ui2/button-primary.png', UI, P.button_primary),
    ('ui/button-secondary', 'ui2/button-secondary.png', UI, P.button_secondary),
    ('ui/toggle-on', 'ui2/toggle-on.png', UI, lambda: P.toggle(True)),
    ('ui/toggle-off', 'ui2/toggle-off.png', UI, lambda: P.toggle(False)),
    # 2026-10-17: card-frame and the gold card-rays are no longer drawn (11:39: the rays read dark and off-theme).
    ('ui/speech-bubble', 'ui2/speech-bubble.png', UI, P.speech_bubble),
    ('ui/banner-warning', 'ui2/banner-warning.png', UI, lambda: P.banner('warning')),
    ('ui/banner-hint', 'ui2/banner-hint.png', UI, lambda: P.banner('hint')),
]
# Color identity is shared by the atlas builder, runtime lookup, and coverage report.
color_file = ROOT / 'Art/skin-colors.json'
color_data = json.loads(color_file.read_text())
for entry in color_data['colors']:
    h, cid, internal = entry['hex'].lstrip('#').upper(), entry['creatureId'], entry['slotId']
    entry['status'] = 'art' if all((DEL / f'creatures/{cid}/{h}/{st}.png').is_file() for st in ['idle','attack','spent']) else 'placeholder'
    for st in ['idle', 'attack', 'spent']:
        SLOTS.append((f'plants/{internal}/{h}/{st}', f'creatures/{cid}/{h}/{st}.png', CREATURE,
                      lambda x=h, state=st: P.round_creature(x, state)))
    for st in ['blink', 'cheer']:
        SLOTS.append((f'plants/{internal}/{h}/{st}', f'creatures/{cid}/{h}/{st}.png', CREATURE, None))
# Mystery reserves reveal neither their palette nor their optional animation.
# Art item 76 delivers only idle: a mystery member never attacks while hidden, so the other states reuse it.
for st in ['idle', 'attack', 'spent']:
    rel = f'creatures/mystery/8C919C/{st}.png'
    if not (DEL / rel).exists() and (DEL / 'creatures/mystery/8C919C/idle.png').exists(): rel = 'creatures/mystery/8C919C/idle.png'
    SLOTS.append((f'plants/color-8c919c/8C919C/{st}', rel, CREATURE, lambda state=st: P.round_creature('8C919C', state)))
# Third batch finale (art items 77/78), optional: the game falls back to the original drawing when absent.
# The Bubble King goes to the creature atlas (plants/ prefix); king-blow falls back to king-ready in code.
# Home key art (Art/R1/home-v1, items H3-H8, H10), optional: the home page keeps its old look until delivered.
# Squad layers are full 1080x1920 frames (trimmed when packed) on the creature atlas; the logo on the main atlas.
HOME_REL = '../../home-v1/delivery/'
for _cid in ['blue', 'orange', 'yellow', 'green', 'purple']:
    SLOTS.append((f'plants/home/squad-{_cid}', f'{HOME_REL}squad-{_cid}.png', 0.9, None))
    SLOTS.append((f'plants/home/squad-{_cid}-blink', f'{HOME_REL}squad-{_cid}-blink.png', 0.9, None))
# 2026-10-17 home soap bubbles: delivery/soap-bubble.png (H13) when delivered; until then the bubble the yellow
# member is blowing in its own layer (already given a soft alpha by the art AI) is cropped out and used.
_HOME = DEL / '../../home-v1'
# 2026-10-17: the H13 bubble is fully clear inside (reads as a hollow ring); a faint pale-rainbow film and a soft
# sheen are laid under it so it reads as a soap bubble. Also applied to the first pop frame (the whole bubble).
import numpy as np
from PIL import ImageFilter as _IF
def add_film(img, strength=1.0):
    img = img.convert('RGBA'); w, h = img.size
    a = np.array(img.getchannel('A'))
    ys, xs = np.nonzero(a > 40)
    cx, cy = (xs.min() + xs.max()) / 2, (ys.min() + ys.max()) / 2
    R = min(xs.max() - xs.min(), ys.max() - ys.min()) / 2 * .96
    Y, X = np.mgrid[0:h, 0:w]
    dx, dy = X - cx, Y - cy
    r = np.sqrt(dx * dx + dy * dy) / R
    ang = (np.arctan2(dy, dx) / (2 * np.pi)) % 1
    hue = (ang + .18 * np.sin(r * np.pi * 2.2) + .1 * np.sin(ang * 6 * np.pi)) % 1
    rgb = np.zeros((h, w, 3))
    hh = hue * 6; i = np.floor(hh).astype(int) % 6; f = hh - np.floor(hh); s = .45; v = 1.0
    p = v * (1 - s); q = v * (1 - s * f); t = v * (1 - s * (1 - f))
    for k, (rr, gg, bb) in enumerate([(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)]):
        m = i == k
        rgb[..., 0] = np.where(m, rr if np.isscalar(rr) else rr, rgb[..., 0])
        rgb[..., 1] = np.where(m, gg if np.isscalar(gg) else gg, rgb[..., 1])
        rgb[..., 2] = np.where(m, bb if np.isscalar(bb) else bb, rgb[..., 2])
    alpha = (.07 + .26 * np.clip(r, 0, 1) ** 3) * strength
    alpha[r > 1] = 0
    rgb = rgb * .55 + .45
    sheen = np.clip(1 - np.sqrt(((X - (cx - R * .35)) / (R * .75)) ** 2 + ((Y - (cy - R * .4)) / (R * .55)) ** 2), 0, 1) ** 1.5 * .22
    alpha = np.maximum(alpha, sheen * (r <= 1)); rgb = rgb * (1 - sheen[..., None]) + sheen[..., None]
    film = np.dstack([rgb * 255, alpha * 255]).clip(0, 255).astype('uint8')
    film = Image.fromarray(film).filter(_IF.GaussianBlur(w / 160))
    out = film.copy(); out.alpha_composite(img); return out

if (_HOME / 'delivery/soap-bubble.png').exists():
    (_HOME / 'generated').mkdir(exist_ok=True)
    add_film(Image.open(_HOME / 'delivery/soap-bubble.png')).save(_HOME / 'generated/soap-bubble-film.png')
    SLOTS.append(('plants/home/soap-bubble', '../../home-v1/generated/soap-bubble-film.png', 0.5, None))
elif (_HOME / 'delivery/squad-yellow.png').exists():
    (_HOME / 'generated').mkdir(exist_ok=True)
    # Bubble centre (884, 718), radius ~74 in the 1080x1920 frame; a soft circular mask drops the face and film.
    from PIL import ImageDraw, ImageFilter, ImageChops
    _y = Image.open(_HOME / 'delivery/squad-yellow.png').convert('RGBA').crop((884 - 84, 718 - 84, 884 + 84, 718 + 84))
    _m = Image.new('L', _y.size, 0); ImageDraw.Draw(_m).ellipse((84 - 77, 84 - 77, 84 + 77, 84 + 77), fill=255)
    _m = _m.filter(ImageFilter.GaussianBlur(1.5))
    _y.putalpha(ImageChops.multiply(_y.getchannel('A'), _m))
    _y.save(_HOME / 'generated/soap-bubble-from-yellow.png')
    SLOTS.append(('plants/home/soap-bubble', '../../home-v1/generated/soap-bubble-from-yellow.png', 1.0, None))
# H13 pop: a horizontal strip of 6 frames, 256 x 256 each.
if (_HOME / 'delivery/soap-pop.png').exists():
    (_HOME / 'generated').mkdir(exist_ok=True)
    _strip = Image.open(_HOME / 'delivery/soap-pop.png').convert('RGBA')
    _n = max(1, _strip.width // _strip.height)
    for _i in range(_n):
        _fr = _strip.crop((_i * _strip.height, 0, (_i + 1) * _strip.height, _strip.height))
        (add_film(_fr) if _i == 0 else _fr).save(_HOME / f'generated/soap-pop-{_i}.png')
        SLOTS.append((f'plants/home/soap-pop/{_i}', f'../../home-v1/generated/soap-pop-{_i}.png', 0.5, None))
# H14 celebration halo behind showcased members (victory screen, codex); optional until delivered.
# 2026-10-17 (user: the light is right, the outer bubbles too big): the bubble-free core is kept and its rays are
# continued outward (clean_halo); the program circles smaller bubbles around it. One of the halo's big bubbles
# (milky iridescent inside, user preferred it to the H13 one) becomes the home/codex soap bubble and pop frame 0.
def clean_halo(img, r0=200.0, r1=430.0):
    """Keep the bubble-free core of the H14 halo (r < r0) and continue its rays outward: beyond r0 every pixel
    takes the colour of the core's edge at the same angle, fading smoothly to nothing at r1."""
    im = np.array(img.convert('RGBA')).astype(np.float32)
    h, w = im.shape[:2]
    Y, X = np.mgrid[0:h, 0:w]
    cx, cy = (w - 1) / 2, (h - 1) / 2
    dx, dy = X - cx, Y - cy
    R = np.sqrt(dx * dx + dy * dy) + 1e-6
    pm = im.copy(); pm[..., :3] *= pm[..., 3:4] / 255
    # sample the ring at r0 (bilinear) along each pixel's angle
    sx = cx + dx / R * r0; sy = cy + dy / R * r0
    x0 = np.floor(sx).astype(int); y0 = np.floor(sy).astype(int); fx = (sx - x0)[..., None]; fy = (sy - y0)[..., None]
    def g(yy, xx): return pm[np.clip(yy, 0, h - 1), np.clip(xx, 0, w - 1)]
    edge = (g(y0, x0) * (1 - fx) * (1 - fy) + g(y0, x0 + 1) * fx * (1 - fy) + g(y0 + 1, x0) * (1 - fx) * fy + g(y0 + 1, x0 + 1) * fx * fy)
    u = np.clip((R - r0) / (r1 - r0), 0, 1); fall = (1 - u) ** 2.2
    ext = edge * fall[..., None]
    t = np.clip((R - (r0 - 14)) / 14, 0, 1)[..., None]   # 14 px blend into the extension
    out = pm * (1 - t) + ext * t
    a = out[..., 3:4]
    rgb = np.where(a > 0, out[..., :3] / np.maximum(a, 1e-3) * 255, 0)
    return Image.fromarray(np.dstack([np.clip(rgb, 0, 255), np.clip(a, 0, 255)]).astype(np.uint8))

# 2026-10-17 12:30 (user): the codex lock showed the battle pad's plate under it. Lift just the lock bubble out of
# pad-locked.png: its bottom sat inside the plate, so the hidden rows are mirrored from the top of the bubble.
def lock_bubble():
    from PIL import ImageDraw as _ID2, ImageChops as _IC2
    src = Image.open(DEL / 'ui/pad-locked.png').convert('RGBA')
    cx, cy, r = 69.5, 45.0, 41.0
    out = src.copy(); px = src.load(); po = out.load()
    for y in range(58, 140):
        sy = int(round(2 * cy - y)); w = 1.0 if y >= 62 else (y - 57) / 4.0   # rows 58-61 cross-fade, no seam
        for x in range(140):
            a = px[x, y]; b = px[x, sy] if 0 <= sy < 140 else (0, 0, 0, 0)
            if y < 62 and 50 <= x <= 90: b = a                         # keep the padlock's own bottom edge
            po[x, y] = tuple(int(round(a[i] * (1 - w) + b[i] * w)) for i in range(4))
    m = Image.new('L', out.size, 0); _ID2.Draw(m).ellipse((cx - r, cy - r, cx + r, cy + r), fill=255)
    out.putalpha(_IC2.multiply(out.getchannel('A'), m.filter(_IF.GaussianBlur(.6))))
    return out.crop((int(cx - r) - 1, int(cy - r) - 1, int(cx + r) + 2, int(cy + r) + 2))
(_HOME / 'generated').mkdir(exist_ok=True)
if (_HOME / 'delivery/codex-lock.png').exists():   # H16, drawn on its own by the art AI
    Image.open(_HOME / 'delivery/codex-lock.png').convert('RGBA').resize((96, 96), Image.LANCZOS).save(_HOME / 'generated/lock-bubble.png')
else:
    lock_bubble().save(_HOME / 'generated/lock-bubble.png')
SLOTS.append(('scene/lock-bubble', '../../home-v1/generated/lock-bubble.png', 0.8, None))

# 2026-10-18 00:14 (user): the selected codex card's own rim turns gold (no extra frame around it). The blue jelly
# rim of ui2/popup-panel.png is recoloured along a luminance ramp (amber shadow -> gold -> pale gold highlights);
# the low-saturation face and white sheen spots are kept, so it is the same card with a gold rim.
def popup_panel_gold():
    im = np.asarray(Image.open(DEL / 'ui2/popup-panel.png').convert('RGBA')).astype(np.float32) / 255
    rgb, a = im[..., :3], im[..., 3:]
    mx, mn = rgb.max(-1), rgb.min(-1)
    sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    t = np.clip((rgb[..., 0] * .299 + rgb[..., 1] * .587 + rgb[..., 2] * .114 - .2) / .75, 0, 1)[..., None]
    dark, mid, light = np.array([.62, .36, .02]), np.array([1., .76, .16]), np.array([1., .97, .72])
    gold = np.where(t < .55, dark + (mid - dark) * (t / .55), mid + (light - mid) * ((t - .55) / .45))
    w = np.clip((sat - .22) / .18, 0, 1)[..., None]
    out = np.concatenate([rgb * (1 - w) + gold * w, a], -1)
    return Image.fromarray((out * 255 + .5).clip(0, 255).astype(np.uint8))
(DEL.parent / 'generated/ui2').mkdir(parents=True, exist_ok=True)
popup_panel_gold().save(DEL.parent / 'generated/ui2/popup-panel-gold.png')
SLOTS.append(('ui/popup-panel-gold', '../generated/ui2/popup-panel-gold.png', UI, None))

# 2026-10-18 00:15 (user): make the guard line itself flow and turn red on warning (no extra band). The rail art is a
# chain of bubble beads repeating about every 130 px; one 130 px period (cut where the seam matches best, edges
# cross-faded) is tiled across the width so the game can scroll it seamlessly. A red recolour of the same strip is
# cross-faded in while the beads close in.
RAIL_PERIOD = 130
def rail_tiled():
    im = np.asarray(Image.open(DEL / 'scene/rail.png').convert('RGBA')).astype(np.float32)
    P = RAIL_PERIOD
    c = min(range(8, im.shape[1] - P - 8), key=lambda c: float(np.abs(im[:, c - 3:c + 3] - im[:, c + P - 3:c + P + 3]).mean()))
    T = im[:, c:c + P].copy()
    for k in range(6):
        w = (6 - k) / 12
        T[:, k] = T[:, k] * (1 - w) + im[:, c + P + k] * w
        T[:, P - 1 - k] = T[:, P - 1 - k] * (1 - w) + im[:, c - 1 - k] * w
    out = np.concatenate([T] * (im.shape[1] // P + 1), 1)[:, :im.shape[1]]
    return Image.fromarray(out.clip(0, 255).astype(np.uint8))
def recolor(img, dark, mid, light, knee=.55):
    im = np.asarray(img).astype(np.float32) / 255
    rgb, a = im[..., :3], im[..., 3:]
    t = np.clip((rgb[..., 0] * .299 + rgb[..., 1] * .587 + rgb[..., 2] * .114 - .2) / .75, 0, 1)[..., None]
    dark, mid, light = map(np.array, (dark, mid, light))
    col = np.where(t < knee, dark + (mid - dark) * (t / knee), mid + (light - mid) * ((t - knee) / (1 - knee)))
    return Image.fromarray((np.concatenate([col, a], -1) * 255 + .5).clip(0, 255).astype(np.uint8))
(DEL.parent / 'generated/scene').mkdir(parents=True, exist_ok=True)
_rail = rail_tiled(); _rail.save(DEL.parent / 'generated/scene/rail-tiled.png')
recolor(_rail, (.50, .02, .06), (.98, .16, .20), (1., .82, .80), knee=.6).save(DEL.parent / 'generated/scene/rail-red.png')
SLOTS[:] = [(sid, '../generated/scene/rail-tiled.png', sc, fb) if sid == 'scene/fence' else (sid, src, sc, fb) for sid, src, sc, fb in SLOTS]
SLOTS.append(('scene/fence-red', '../generated/scene/rail-red.png', UI, None))

if (_HOME / 'delivery/celebrate-halo.png').exists():
    (_HOME / 'generated').mkdir(exist_ok=True)
    _halo = Image.open(_HOME / 'delivery/celebrate-halo.png').convert('RGBA')
    # 12:19 (user): the bubble-free light again; the program draws livelier bubbles around it (BubbleWin).
    clean_halo(_halo).save(_HOME / 'generated/halo-glow.png')
    SLOTS.append(('home/halo', '../../home-v1/generated/halo-glow.png', 0.5, None))
    from PIL import ImageDraw as _ID, ImageChops as _IC
    _b = _halo.crop((512 - 102, 178 - 102, 512 + 102, 178 + 102))
    _m = Image.new('L', _b.size, 0); _ID.Draw(_m).ellipse((6, 6, 198, 198), fill=255); _m = _m.filter(_IF.GaussianBlur(1.2))
    _b.putalpha(_IC.multiply(_b.getchannel('A'), _m))
    _b.save(_HOME / 'generated/soap-bubble-halo.png')
    SLOTS[:] = [e for e in SLOTS if e[0] != 'plants/home/soap-bubble']
    SLOTS.append(('plants/home/soap-bubble', '../../home-v1/generated/soap-bubble-halo.png', 1.0, None))
    if (_HOME / 'generated/soap-pop-0.png').exists():
        _f0 = Image.new('RGBA', (256, 256), (0, 0, 0, 0)); _f0.alpha_composite(_b.resize((200, 200), Image.LANCZOS), (28, 28))
        _f0.save(_HOME / 'generated/soap-pop-0.png')
SLOTS.append(('plants/home/btn-settings', f'{HOME_REL}btn-settings.png', 0.5, None))   # H11, optional until delivered
SLOTS.append(('plants/home/btn-codex', f'{HOME_REL}btn-codex.png', 0.5, None))         # H12
SLOTS.append(('plants/home/logo', f'{HOME_REL}logo.png', 0.8, None))
SLOTS += [('plants/finale/king-ready', 'finale/king-ready.png', UI, None),
          ('plants/finale/king-blow', 'finale/king-blow.png', UI, None),
          ('finale/button', 'finale/button.png', UI, None)]
color_file.write_text(json.dumps(color_data, ensure_ascii=False, indent=2) + '\n')
lookup = ['#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR', 'namespace Flightbeat.Garden {',
          '    public static class GardenColorCatalog {']
for method, field in [('Key', 'slotId'), ('Name', 'displayName')]:
    lookup += [f'        public static string {method}(int rgb) {{', '            switch (rgb) {']
    for e in color_data['colors']:
        lookup.append('                case 0x%s: return %s;' % (e['hex'][1:], json.dumps(e[field], ensure_ascii=False)))
    fallback = '"color-" + rgb.ToString("x6")' if method == 'Key' else '"队员"'
    lookup += ['                default: return ' + fallback + ';', '            }', '        }']
# Debut table (2026-10-16): for each level (0-based, training01 first) the squad members appearing for the first
# time, by character (creatureId), as the hex used in that level. Drives the intro's "新队员" chips and NEW tags.
_order = ['training01', 'training02'] + [f'sector{i:02}' for i in range(1, 261)]
_by_hex = {e['hex'].upper(): e['creatureId'] for e in color_data['colors']}
_seen, _debut = set(), []
for _name in _order:
    _d = json.loads((CODE / 'Resources/levels' / (_name + '.json')).read_text())
    _row = []
    for _q in _d['queue']:
        _h = ''.join(f'{c:02X}' for c in _d['palette'][_q['color'] * 3:_q['color'] * 3 + 3])
        _cid = _by_hex['#' + _h]
        if _cid not in _seen: _seen.add(_cid); _row.append(_h)
    _debut.append(_row)
lookup += ['        static readonly int[] noDebut = new int[0];', '        static readonly int[][] debut = {']
lookup += ['            ' + ('new[] { ' + ', '.join('0x' + h for h in row) + ' },' if row else 'noDebut,') for row in _debut]
lookup += ['        };', '        public static int[] Debut(int level) => level >= 0 && level < debut.Length ? debut[level] : noDebut;']
lookup += ['    }', '}', '#endif']
(CODE / 'Garden/GardenColorCatalog.Generated.cs').write_text('\n'.join(lookup) + '\n')
# Guide "Xiao Lan" portraits: second batch; until then the blue creature's idle / attack / spent art.
GUIDE = {'explain': 'idle', 'happy': 'attack', 'worried': 'spent', 'proud': 'idle'}
for face, st in GUIDE.items():
    SLOTS.append((f'ui/guide-{face}', f'ui2/guide-{face}.png', UI, None, ('creatures/blue/2982F4/' + st + '.png', (lambda s=st: P.creature('blue', '2982F4', s)))))

# ---- program-drawn: beads, mosaic panel, glow, arrow
def bead(px, teaching_mark=None, rim=None):
    s = 16; n = px * s
    main = Image.new('RGBA', (n, n), (0, 0, 0, 0)); fixed = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    dm, df = ImageDraw.Draw(main), ImageDraw.Draw(fixed)
    r = n * .47; cx = cy = n / 2
    for i in range(int(r), 0, -1):          # light top-left, slightly darker lower right (tinted)
        t = i / r
        g = int(255 * (0.78 + 0.22 * (1 - t) ** .6))
        ox, oy = -(1 - t) * r * .25, -(1 - t) * r * .28
        dm.ellipse([cx - i + ox, cy - i + oy, cx + i + ox, cy + i + oy], fill=(g, g, g, 255))
    ring = Image.new('L', (n, n), 0); ImageDraw.Draw(ring).ellipse([cx - r, cy - r, cx + r, cy + r], fill=255)
    inner = ring.filter(ImageFilter.MinFilter(int(s * 1.4) | 1))
    main.putalpha(ring)
    edge = Image.composite(Image.new('L', (n, n), 0), ring, inner)
    main.paste(Image.new('RGBA', (n, n), (158, 158, 158, 255)), (0, 0), edge)   # same-hue rim = main x 0.62
    df.ellipse([cx - r * .58, cy - r * .62, cx - r * .08, cy - r * .2], fill=(255, 255, 255, 220))
    if rim: fixed.paste(Image.new('RGBA', (n, n), rim), (0, 0), edge)
    if teaching_mark:                       # UI spec v3 4.1: blue dot, orange triangle, green square, purple cross
        mk = Image.new('RGBA', (n, n), (0, 0, 0, 0)); dk = ImageDraw.Draw(mk); w = r * .3
        mx, my = cx + r * .06, cy + r * .14
        if teaching_mark == 'dot': dk.ellipse([mx - w, my - w, mx + w, my + w], fill=(255, 255, 255, 255))
        elif teaching_mark == 'tri': dk.polygon([(mx, my - w * 1.1), (mx + w * 1.05, my + w * .8), (mx - w * 1.05, my + w * .8)], fill=(255, 255, 255, 255))
        elif teaching_mark == 'square': dk.rectangle([mx - w * .85, my - w * .85, mx + w * .85, my + w * .85], fill=(255, 255, 255, 255))
        elif teaching_mark == 'cross':
            dk.line([(mx - w, my - w), (mx + w, my + w)], fill=(255, 255, 255, 255), width=int(r * .2))
            dk.line([(mx - w, my + w), (mx + w, my - w)], fill=(255, 255, 255, 255), width=int(r * .2))
        sh = mk.split()[3].filter(ImageFilter.MaxFilter(int(s * 1.2) | 1))
        fixed.paste(Image.new('RGBA', (n, n), (35, 64, 107, 255)), (0, 0), sh); fixed.alpha_composite(mk)
    return main.resize((px, px), Image.LANCZOS), fixed.resize((px, px), Image.LANCZOS)

def field(px=96, radius=24, edge=9):
    """Mosaic panel: exact #8C919C in the middle (UI spec v3 2), soft darker inner edge; drawn 9-sliced."""
    n = px * 4; base = (0x8C, 0x91, 0x9C)
    im = Image.new('RGBA', (n, n), (0, 0, 0, 0))
    ImageDraw.Draw(im).rounded_rectangle([0, 0, n - 1, n - 1], radius * 4, fill=base + (255,))
    shade = Image.new('L', (n, n), 0); ImageDraw.Draw(shade).rounded_rectangle([0, 0, n - 1, n - 1], radius * 4, fill=255)
    core = Image.new('L', (n, n), 0); ImageDraw.Draw(core).rounded_rectangle([edge * 4, edge * 4, n - 1 - edge * 4, n - 1 - edge * 4], max(4, (radius - edge) * 4), fill=255)
    rim = Image.composite(Image.new('L', (n, n), 0), shade, core).filter(ImageFilter.GaussianBlur(10))
    dark = Image.new('RGBA', (n, n), tuple(int(v * .8) for v in base) + (255,))
    im.paste(dark, (0, 0), Image.eval(rim, lambda v: min(255, int(v * .55))))
    im.putalpha(shade)
    out = im.resize((px, px), Image.LANCZOS)
    c = px // 2
    out.paste(base + (255,), (radius, radius, px - radius, px - radius))   # guarantee exact center color
    return out

def glow(px):
    n = px * 4; im = Image.new('L', (n, n), 0); d = ImageDraw.Draw(im)
    for i in range(n // 2, 0, -1):
        t = i / (n / 2); d.ellipse([n / 2 - i, n / 2 - i, n / 2 + i, n / 2 + i], fill=int(255 * min(1, 1.25 * (1 - t) ** 0.9)))
    im = im.filter(ImageFilter.GaussianBlur(n / 40)).resize((px, px), Image.LANCZOS)
    out = Image.new('RGBA', (px, px), (255, 255, 255, 0)); out.putalpha(im); return out

GENERATED = {}
m, f = bead(11); GENERATED['bugs/bead-main'], GENERATED['bugs/bead-fixed'] = m, f
GENERATED['bugs/bead-white-fixed'] = bead(11, rim=(140, 150, 162, 255))[1]
GENERATED['bugs/bead-dark-fixed'] = bead(11, rim=(150, 154, 160, 255))[1]
GENERATED['bugs/bead-teaching-main'] = bead(21)[0]
for key, mark in {'morning-glory': 'dot', 'carrot': 'tri', 'broccoli': 'square', 'eggplant': 'cross'}.items():
    GENERATED[f'bugs/bead-teaching-{key}-fixed'] = bead(21, teaching_mark=mark)[1]
GENERATED['bugs/bead-teaching-fixed'] = bead(21)[1]
# scene/field (the #8C919C mosaic panel) was dropped on 2026-10-15: beads sit on the background sky.
def tray(px=112, radius=28):
    """Lower-half stage behind pads and queues (2026-10-15, matches the mockup's muted blue-gray tray).
    Nine-sliced: vertical gradient in the middle, rounded top corners, soft white rim along the top edge."""
    n = px * 4; top, bottom = (0xB9, 0xCB, 0xE2), (0x9A, 0xB2, 0xD1)
    grad = Image.new('RGBA', (n, n))
    for y in range(n):
        t = y / (n - 1); grad.paste(tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3)) + (235,), (0, y, n, y + 1))
    mask = Image.new('L', (n, n), 0); ImageDraw.Draw(mask).rounded_rectangle([0, 0, n - 1, n + radius * 8], radius * 4, fill=255)
    rim = Image.new('RGBA', (n, n), (0, 0, 0, 0)); ImageDraw.Draw(rim).rounded_rectangle([2, 2, n - 3, n + radius * 8], radius * 4 - 2, outline=(255, 255, 255, 170), width=10)
    grad.alpha_composite(rim.filter(ImageFilter.GaussianBlur(3)))
    grad.putalpha(Image.composite(grad.getchannel('A'), Image.new('L', (n, n), 0), mask))
    return grad.resize((px, px), Image.LANCZOS)
# Item 57 (2026-10-15): the redrawn full background has the stage painted in. The art AI keeps the old file as
# _originals/background-v1.png; when the current background differs from it, no program tray is generated
# (the game draws scene/stage if present, else scene/tray if present, else nothing).
_bg, _bg1 = DEL / 'scene/background.png', DEL / '_originals/background-v1.png'
BACKGROUND_HAS_STAGE = _bg.exists() and _bg1.exists() and _bg.read_bytes() != _bg1.read_bytes()
if not BACKGROUND_HAS_STAGE:
    GENERATED['scene/tray'] = tray()
else:   # the painted stage is part of the background: neither scene/stage nor the program tray is drawn
    SLOTS[:] = [e for e in SLOTS if e[0] != 'scene/stage']
GENERATED['fx/glow'] = glow(96)
_ptr = DEL / 'ui2/pointer.png'   # tutorial arrow: delivered art when present (stored at 48 px like v2)
GENERATED['fx/arrow'] = Image.open(_ptr).convert('RGBA').resize((48, 48), Image.LANCZOS) if _ptr.exists() else P.pointer(48)

# ---- glyph block
text = ''.join(p.read_text(encoding='utf-8') for p in CODE.rglob('*.cs'))
for p in CODE.rglob('*.prefab'):
    text += re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m[1], 16)), p.read_text(encoding='utf-8'))
chars = ''.join(chr(i) for i in range(32, 127)) + ''.join(sorted(set(c for c in text if ord(c) >= 128)))
# Preserve imported glyph pixels and metrics. The source atlas predates the current
# font rasterizer; re-rasterizing all characters changes existing UI line widths.
old = json.loads((ROOT / 'Art/fonts/source-glyphs.json').read_text())
cols = SIZE // CELL
image = Image.new('RGBA', (SIZE, SIZE))
image.alpha_composite(Image.open(ROOT / 'Art/fonts/source-glyphs.png').convert('RGBA'), (0, 0))
missing = ''.join(sorted(set(chars) - set(old['characters'])))
chars = old['characters'] + missing
glyphs = list(old['glyphs']); font_size = old['fontSize']
if missing:
    if hashlib.sha256(FONT.read_bytes()).hexdigest() != FONT_SHA:
        raise SystemExit(f'{FONT}: not the recorded Noto Sans SC file (font-source.json)')
    font = ImageFont.truetype(str(FONT), FONT_SIZE); font.set_variation_by_axes([600])
    draw = ImageDraw.Draw(image)
    for i, c in enumerate(missing, len(old['characters'])):
        x, y = (i % cols) * CELL, (i // cols) * CELL
        left, top, right, bottom = font.getbbox(c, anchor='ls')
        draw.text((x + 2 - left, y + 2 - top), c, font=font, fill='white', anchor='ls')
        glyphs.append(dict(code=ord(c), x=x + 2, y=y + 2, w=right - left, h=bottom - top, advance=font.getlength(c) / FONT_SIZE,
                           bearingX=left / FONT_SIZE, bearingY=top / FONT_SIZE))
cursor_y = ((len(chars) + cols - 1) // cols) * CELL
glyph_mode = 'source-preserved+' + str(len(missing))

# ---- tiles
tiles, placeholder, delivered = [], [], []
for entry in SLOTS:
    sid, rel, sc, make = entry[:4]
    p = DEL / rel
    if p.exists():
        im = Image.open(p).convert('RGBA'); src = hashlib.sha256(p.read_bytes()).hexdigest(); delivered.append(rel)
    else:
        if make is None and len(entry) < 5:    # optional art with no placeholder (scene/stage)
            continue
        if make is None:                       # guide portraits fall back to the blue creature
            alt_rel, alt_make = entry[4]
            q = DEL / alt_rel
            im = Image.open(q).convert('RGBA') if q.exists() else alt_make()
        else:
            im = make()
        src = 'placeholder'; placeholder.append(rel)
        out = ROOT / 'Art/generated/placeholders' / rel; out.parent.mkdir(parents=True, exist_ok=True); im.save(out)
    if sid == 'scene/pad-swirl':               # room to rotate the UVs without leaving the slot (UI spec v3 4.3)
        k = math.ceil(im.width * 1.42); pad = Image.new('RGBA', (k, k), (0, 0, 0, 0))
        # 2026-10-17 16:47 (user: the turning swirl looked crooked): the art's visual centre (alpha centroid) is ~4.6 px
        # right of its canvas centre, so it wobbled as it turned. Paste it so the centroid sits on the slot centre.
        import numpy as _np
        _a = _np.asarray(im.getchannel('A'), dtype=float); _ys, _xs = _np.indices(_a.shape)
        _cx, _cy = (_xs * _a).sum() / _a.sum(), (_ys * _a).sum() / _a.sum()
        pad.alpha_composite(im, (round(k / 2 - _cx), round(k / 2 - _cy))); im = pad
    im = im.resize((max(1, round(im.width * sc)), max(1, round(im.height * sc))), Image.LANCZOS)
    tiles.append((sid, im, src))
for sid, im in GENERATED.items(): tiles.append((sid, im, 'generated'))
BG_SIZE = (810, 1440)
if _bg.exists():
    background = Image.open(_bg).convert('RGB'); bg_src = _bg.name + ':' + hashlib.sha256(_bg.read_bytes()).hexdigest(); delivered.append('scene/background.png')
else:
    background = P.background().convert('RGB'); bg_src = 'placeholder'; placeholder.append('scene/background.png')
background.resize(BG_SIZE, Image.LANCZOS).save(OUT / 'background.png')
# Home background (H2): its own texture, 1080x2340 stored at 0.75 (core 9:16 picture at y 210-2130 of the source).
_home_bg = DEL / '../../home-v1/delivery/background.png'
if _home_bg.exists():
    Image.open(_home_bg).convert('RGB').resize((810, 1755), Image.LANCZOS).save(OUT / 'home-background.png')
    delivered.append('home-v1/background.png')
    _meta = OUT / 'home-background.png.meta'
    if not _meta.exists():                   # same import settings as background.png, own stable guid
        import hashlib, re
        _src = (OUT / 'background.png.meta').read_text(encoding='utf-8')
        _guid = hashlib.md5(b'GardenR1/home-background').hexdigest()
        _meta.write_text(re.sub(r'guid: [0-9a-f]{32}', 'guid: ' + _guid, _src, count=1), encoding='utf-8')
# 2026-10-17 loading page (WebGL template, shown before the game downloads, so kept small: ~300 KB in all).
# Softened home sky, the title logo, Xiao Lan blowing a bubble (H15 when delivered, otherwise its cheer pose) and
# the halo soap bubble that grows with the download progress. Anchor = where the bubble starts, in the hero image.
def export_loading():
    import json as _json
    tpl = ROOT / 'UnityProject/Assets/WebGLTemplates/Flightbeat/TemplateData'
    tpl.mkdir(parents=True, exist_ok=True)
    home = DEL / '../../home-v1'
    bg = home / 'delivery/background.png'
    if bg.exists():
        im = Image.open(bg).convert('RGB').resize((540, 1170), Image.LANCZOS).filter(_IF.GaussianBlur(2.2))
        im.save(tpl / 'loading-bg.jpg', quality=70, optimize=True, progressive=True)
    # 16:11 (user): no game name on the loading page, so no logo image.
    bub = home / 'generated/soap-bubble-halo.png'
    if bub.exists():
        Image.open(bub).convert('RGBA').resize((240, 240), Image.LANCZOS).save(tpl / 'loading-bubble.webp', quality=90, method=6)
    # 2026-10-17 15:45 (user): the squad blows bubbles from the mouth, not with a wand. 16:01 (user: Xiao Lan looked
    # rough): the 1080-frame home layer is only ~370 px wide and was palette-quantized; the page now uses the full
    # resolution H3 original (~1100 px), in full color, as WebP at 600 px wide. No blink frame at this size.
    h3 = Image.open(home / 'delivery/_originals/H3-attempt-01.png').convert('RGBA')
    crop = (17, 5, 1195, 1284)   # H3 body bbox (57,45)-(1155,1244) plus a margin
    h3.crop(crop).resize((600, 651), Image.LANCZOS).save(tpl / 'loading-hero.webp', quality=90, method=6)
    for stale in ('loading-hero.png', 'loading-hero-blink.png', 'loading-logo.png', 'loading-logo.webp', 'loading-bubble.png'):
        for f in (tpl / stale, tpl / (stale + '.meta')):
            if f.exists(): f.unlink()
    # Mouth: (562,1062) in the home frame = (.539,.493) of that layer's body bbox -> H3 pixels -> crop fraction.
    mx, my = 57 + .539 * 1098, 45 + .493 * 1199
    anchor = {'x': round((mx - crop[0]) / (crop[2] - crop[0]), 4), 'y': round((my - crop[1]) / (crop[3] - crop[1]), 4), 'dir': 0.3}
    # 16:33 (user): two more of the squad on the loading page, smaller, behind Xiao Lan: orange (H4) on the left and
    # purple (H7) on the right. Their mouth points are fixed in index.html: orange (.557,.527), purple (.536,.49).
    # 17:24 (user confirmed the redesign): purple is the new art-v3 redesign/purple-A (both ears hang in arcs, smiling).
    for src, name, box in ((home / 'delivery/_originals/H4-attempt-01.png', 'orange', (0, 47, 1204, 1238)),
                           (DEL / 'redesign/purple-A.png', 'purple', (72, 152, 952, 901))):
        o = Image.open(src).convert('RGBA')
        c = o.crop((max(0, box[0] - 30), max(0, box[1] - 30), min(o.width, box[2] + 30), min(o.height, box[3] + 30)))
        c.resize((420, round(420 * c.height / c.width)), Image.LANCZOS).save(tpl / ('loading-' + name + '.webp'), quality=88, method=6)
    for src, dst in (('progress-frame.png', 'loading-bar-frame.png'), ('progress-fill.png', 'loading-bar-fill.png')):
        Image.open(DEL / 'ui' / src).convert('RGBA').quantize(colors=180, method=Image.FASTOCTREE).save(tpl / dst, optimize=True)
    (tpl / 'loading-anchor.js').write_text('window.LOADING_ANCHOR=' + _json.dumps(anchor) + ';\n')
    return sum(f.stat().st_size for f in tpl.iterdir())
try:
    print('loading page assets', export_loading(), 'bytes', file=sys.stderr)
except Exception as _e:   # the atlas must not fail because of the loading page
    print('loading page assets skipped:', _e, file=sys.stderr)
fx_sources = {}
for name, count in FX_SPECS.items():
    path = DEL / 'fx' / (name + '.png')
    if not path.exists(): path = FXDEL / 'fx' / (name + '.png')
    fx_sources[name] = str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path)
    im = Image.open(path).convert('RGBA'); assert im.width % count == 0, path
    cw = im.width // count; sha = hashlib.sha256(path.read_bytes()).hexdigest()
    for i in range(count):
        cell = im.crop((i * cw, 0, (i + 1) * cw, im.height)).convert('RGBa').resize((cw // 2, im.height // 2), Image.Resampling.LANCZOS).convert('RGBA')
        tiles.append((f'fx/{name}/{i}', cell, sha))
(GEN / 'program').mkdir(parents=True, exist_ok=True)
for sid, im in GENERATED.items(): im.save(GEN / 'program' / (sid.replace('/', '__') + '.png'))

trimmed = []
for sid, im, src in tiles:
    fw, fh = im.size
    bb = im.getchannel('A').point(lambda v: 255 if v > 0 else 0).getbbox() or (0, 0, 1, 1)
    if sid.startswith(('bugs/', 'fx/')) or sid in ('scene/background', 'scene/field', 'scene/pad-swirl', 'scene/tray'): bb = (0, 0, fw, fh)
    trimmed.append((sid, im.crop(bb), src, bb[0], bb[1], fw, fh))
tiles = trimmed
tiles.sort(key=lambda t: (-max(t[1].size), -t[1].width * t[1].height, t[0]))
free = [(0, cursor_y, SIZE, SIZE - cursor_y)]
def place(w, h):
    choices = [(min(rw-w,rh-h), max(rw-w,rh-h), y, x) for x,y,rw,rh in free if w<=rw and h<=rh]
    if not choices: return None
    _, _, y, x = min(choices); right, bottom = x+w, y+h
    remaining = []
    for fx,fy,fw,fh in free:
        fr, fb = fx+fw, fy+fh
        if right<=fx or x>=fr or bottom<=fy or y>=fb:
            remaining.append((fx,fy,fw,fh)); continue
        if x>fx: remaining.append((fx,fy,x-fx,fh))
        if right<fr: remaining.append((right,fy,fr-right,fh))
        if y>fy: remaining.append((fx,fy,fw,y-fy))
        if bottom<fb: remaining.append((fx,bottom,fw,fb-bottom))
    remaining = list(dict.fromkeys(remaining))
    free[:] = [r for i,r in enumerate(remaining) if not any(i!=j and q[0]<=r[0] and q[1]<=r[1] and q[0]+q[2]>=r[0]+r[2] and q[1]+q[3]>=r[1]+r[3] for j,q in enumerate(remaining))]
    return x,y
slots, heights = [], []
for atlas_id in [0, 1]:
    atlas_image = image if atlas_id == 0 else Image.new('RGBA', (SIZE, SIZE))
    start_y = cursor_y if atlas_id == 0 else 0
    free[:] = [(0, start_y, SIZE, SIZE - start_y)]
    top = start_y
    for sid, im, src, ox, oy, fw, fh in tiles:
        if int(sid.startswith('plants/')) != atlas_id: continue
        w, h = im.size
        at = place(w + 4, h + 4)
        if at is None:
            if atlas_id == 1 and CREATURE > 0.62 + 1e-6:
                os.environ['R1_CREATURE_SCALE'] = f'{max(0.62, CREATURE - 0.04):.2f}'
                print(f'Creature atlas full at {sid}; retrying at {os.environ["R1_CREATURE_SCALE"]}', file=sys.stderr)
                os.execv(sys.executable, [sys.executable, str(Path(__file__).resolve()), *sys.argv[1:]])
            raise SystemExit(f'Atlas {atlas_id} full at {sid}')
        atlas_image.alpha_composite(im, (at[0] + 2, at[1] + 2)); top = max(top, at[1] + h + 4)
        if sid.startswith('fx/') or sid in ('scene/field', 'scene/tray'):
            px, py = at[0] + 2, at[1] + 2
            atlas_image.paste(im.crop((0,0,w,1)).resize((w,2)),(px,py-2)); atlas_image.paste(im.crop((0,h-1,w,h)).resize((w,2)),(px,py+h))
            atlas_image.paste(im.crop((0,0,1,h)).resize((2,h)),(px-2,py)); atlas_image.paste(im.crop((w-1,0,w,h)).resize((2,h)),(px+w,py))
        slots.append(dict(id=sid, atlas=atlas_id, x=at[0]+2, y=at[1]+2, w=w, h=h, ox=ox, oy=oy, fw=fw, fh=fh))
    heights.append(top)
    atlas_image.save(OUT / ('atlas.png' if atlas_id == 0 else 'creatures.png'))
write_index(slots, SIZE, OUT, GEN)
(OUT / 'atlas.json').write_text(json.dumps(dict(size=SIZE, cell=CELL, fontSize=font_size, characters=chars,
                                                 glyphs=glyphs, slots=slots), ensure_ascii=False))
(GEN / 'atlas-sources.json').write_text(json.dumps({'scene/background (background.png)': bg_src, **{t[0]: t[2] for t in tiles}}, indent=1, ensure_ascii=False))
_summary = json.dumps(dict(slots=len(slots), creatureScale=CREATURE, glyphs=glyph_mode, characters=len(chars), usedHeight=heights, size=SIZE,
                      delivered=len(delivered), placeholders=len(placeholder),
                      fxFromV3=sorted(k for k, v in fx_sources.items() if 'art-v3' in v), backgroundHasStage=BACKGROUND_HAS_STAGE), ensure_ascii=False)
print(_summary)
_CACHE.parent.mkdir(parents=True, exist_ok=True)
_CACHE.write_text(json.dumps(dict(fingerprint=_FP, summary=_summary), ensure_ascii=False))

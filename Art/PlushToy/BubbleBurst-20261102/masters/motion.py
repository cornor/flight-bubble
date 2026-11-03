"""Motion master for the three BubbleBurst studies. Run to export the 2× strips."""
from functools import lru_cache
from math import cos, sin, radians, degrees, pi
from pathlib import Path
import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
CELL = 256
FRAMES = 16
STEP_MS = 20
RANGE = (26, 39, 52)
PROOF_COLOR = (49, 204, 218)
RESAMPLE = Image.Resampling.LANCZOS


def make_textures():
    """Separate an alpha-preserving color film from the reflected window of light."""
    atlas = Image.open(ROOT / 'masters/water-particles.png').convert('RGBA')
    textures = []
    for i in range(3):
        w = atlas.width // 3
        tile = atlas.crop((i*w, 0, (i+1)*w, atlas.height))
        box = tile.getchannel('A').point(lambda p: 255 if p > 16 else 0).getbbox()
        pixels = np.asarray(tile.crop(box), dtype=float) / 255
        rgb, a = pixels[:, :, :3], pixels[:, :, 3]
        h, w = a.shape
        y, x = np.mgrid[0:h, 0:w]
        x, y = (x + .5) / w, (y + .5) / h
        depth = cv2.distanceTransform((a > .025).astype('uint8'), cv2.DIST_L2, 5)
        rim = np.exp(-depth / (.095 * min(w, h)))
        # Keep the body clear. The inside lip carries the hit color.
        film = (a ** .65) * (.57 + .40 * rim)
        tone = np.clip(.99 - .36*y**1.6 - .10*rim + .10*(rgb.mean(axis=2)-.7), .48, 1)
        neutral = np.stack((tone, tone, tone, film), axis=-1)
        # Isolate the compact upper reflection from the texture. Add a faint
        # broken side reflection for readability at the board's 11 px pitch.
        reflected = np.clip((rgb.min(axis=2)-.84)/.15, 0, 1)
        window = reflected**2 * np.clip((.65-y)/.15, 0, 1) * a
        lx = (.32, .69, .28)[i]
        broad = np.exp(-(((x-lx)/.15)**2 + ((y-.245)/.085)**2)*1.5) * a**.5
        lip = np.exp(-((y-.84)/.115)**2 - ((x-.69)/.22)**2) * rim * a
        white = np.ones((h,w,4))
        white[:,:,3] = np.clip(np.maximum(window, broad)*.97 + lip*.45, 0, 1)
        textures.append(tuple(Image.fromarray(np.uint8(np.round(p*255))) for p in (neutral, white)))
    return textures


@lru_cache(None)
def textures():
    return make_textures()


def travel(t):
    # Large first displacement, then a shorter outward move on every sample.
    return 1 - (1 - t) ** 2.45


def opacity(t):
    return max(0, 1 - t**1.65)**1.28


def polar(theta, r):
    return r*cos(theta), r*sin(theta)


def pearl_rays(level, ms):
    t = min(ms/280, 1)
    n = (7, 9, 11)[level]
    for k in range(n):
        theta = .12 + k*2*pi/n + .043*sin(2.3*k)
        ray = RANGE[level] * (1 if k % 2 == 0 else .87)
        radius = 7.5 + (ray-7.5)*travel(t)
        head = (6.2+.58*level + .75*sin(k*2.7))*(1-.35*t)
        x,y = polar(theta,radius)
        yield 0, x,y,head,head*1.02,0,opacity(t)
        for j in range(1, 2 + int((level == 2 or k % 2 == 0))):
            # The beads separate out of the same spoke at different speeds.
            gap = (3.2 + travel(t)*(3.1+1.0*level))*j
            r = radius-gap
            if r > 6 and ms >= 20:
                size = head*(.57 if j == 1 else .32)
                x,y = polar(theta,r)
                yield 0, x,y,size,size,0,opacity(t)*(.91 if j == 1 else .74)


def forks(level, ms):
    n = (4, 5, 6)[level]
    t = min(ms/300, 1)
    for k in range(n):
        theta = radians(-18) + k*2*pi/n + .075*sin(k*3.7)
        reach = RANGE[level]*(.94+.06*cos(2.6*k))
        split = reach*.49
        start = 7.2
        if ms <= 100:
            u = min(ms/60,1)
            r = start + (split-start)*(1-(1-u)**1.45) + max(0,ms-60)*.035
            size = (9.6+level)*(1-.21*u)
            x,y = polar(theta,r)
            yield 1,x,y,size,size*.56,theta,max(0,1-max(0,ms-40)/65)*opacity(t)
        if ms >= 60:
            u = min((ms-60)/240,1)
            for side in (-1,1):
                # Each daughter continues away from the parent, on a new straight ray.
                path = theta + radians(side*(25+level*2+4*sin(k)))
                length = (reach-split)*travel(u)*(1 if side == 1 else .85)
                x = split*cos(theta) + length*cos(path)
                y = split*sin(theta) + length*sin(path)
                size = (8.2 + level*.55) * (1 if side == 1 else .76) * (1-.37*u)
                yield 1,x,y,size,size*(.56+.19*u),path,opacity(t)
            if level > 0 and ms >= 100:
                r = split + (reach-split)*.42*travel(u)
                x,y = polar(theta,r)
                size = (3.8+.35*level)*(1-.30*u)
                yield 0,x,y,size,size,0,opacity(t)*.83


def ripple(level, ms):
    n = (7, 9, 12)[level]
    for ring in range(2):
        delay = ring*40
        if ms < delay:
            continue
        t = min((ms-delay)/260,1)
        for k in range(n):
            theta = radians(-82) + (k+.5*ring)*2*pi/n + .025*sin(k*2)
            start = 7.8 if ring == 0 else 8.2+2.0*level
            end = RANGE[level] * (1 if ring == 0 else .64)
            r = start + (end-start)*travel(t)
            size = (7.6+level*.48)*(1+.10*cos(3*k))*(1-.38*t)
            x,y = polar(theta,r)
            if ring == 0:
                # The tangent faces of the membrane fragments outline the outer shell.
                yield 2,x,y,size,size*(.58+.20*t),theta+pi/2,opacity(t)*.97
            else:
                size *= .61
                yield 0,x,y,size,size,0,opacity(t)*.88


def render_layers(style, level, frame):
    motion = {'A':pearl_rays,'B':forks,'C':ripple}[style]
    ss = 5  # Screen-space supersampling. Output imports at 2 texture pixels/screen px.
    layers = [Image.new('RGBA',(128*ss,128*ss)) for _ in range(2)]
    for tex,x,y,w,h,angle,a in motion(level,frame*STEP_MS):
        if a <= .003:
            continue
        for canvas,part in zip(layers,textures()[tex]):
            s = part.resize((max(1,round(w*ss)),max(1,round(h*ss))),RESAMPLE)
            if angle:
                s = s.rotate(-degrees(angle), Image.Resampling.BICUBIC, expand=True)
            s.putalpha(s.getchannel('A').point(lambda v:round(v*a)))
            canvas.alpha_composite(s,(round((64+x)*ss-s.width/2),round((64+y)*ss-s.height/2)))
    result = [im.resize((CELL,CELL),RESAMPLE) for im in layers]
    result[1] = Image.merge('RGBA', (Image.new('L',(CELL,CELL),255),)*3 + (result[1].getchannel('A'),))
    return result


def tint(layers,color):
    base,shine = layers
    grey = base.getchannel('R')
    rgb = [grey.point(lambda v:round(v*c/255)) for c in color]
    return Image.alpha_composite(Image.merge('RGBA',(*rgb,base.getchannel('A'))), shine)


def export():
    for style in 'ABC':
        folder = ROOT/'delivery'/('方案'+style)
        folder.mkdir(exist_ok=True)
        for level in range(3):
            strips = {name:Image.new('RGBA',(FRAMES*CELL,CELL)) for name in ('film','glint','proof')}
            for f in range(FRAMES):
                layers = render_layers(style,level,f)
                for s,im in zip(strips.values(),[*layers,tint(layers,PROOF_COLOR)]):
                    s.paste(im,(f*CELL,0))
            for name,s in strips.items():
                s.save(folder/f'L{level}-{name}.png')


if __name__ == '__main__':
    export()

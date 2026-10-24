#!/usr/bin/env python3
"""Native UI geometry and lossless registration of approved plush masters for P3/P4.

No character repainting: master cells are cropped, uniformly resized and placed on
the shared canvas. UI uses the same editable capsule geometry as the battle page.
"""
import json, math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from build_plush_ui import capsule
from build_plush_controls import Mold, round_control, button_mold, raised_button, modal_panel, toggle

ROOT = Path(__file__).resolve().parents[2]
BATCH = ROOT / 'Art/PlushToy/P2-P4'
OUT = BATCH / 'delivery'
CREAM = ((255,252,245),(239,222,219))
PURPLE = ((217,194,255),(163,120,237))
GOLD = ((255,247,148),(255,188,54))
INK = (85,48,148,255)

def save(im, name):
    p = OUT / name; p.parent.mkdir(parents=True,exist_ok=True); im.save(p)

def registered(im, width=448, height=512):
    box=im.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
    if not box: raise ValueError('Empty character')
    l,t,r,b=box; k=min(width*194/224/(r-l),height*196/256/(b-t))
    scaled=im.resize((round(im.width*k),round(im.height*k)),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',(width,height))
    canvas.alpha_composite(scaled,(round(width/2-(l+r)*.5*k),round(height*212/256-b*k)))
    return canvas

def home_members():
    sources={}
    for cid in ('blue','orange','yellow','green'):
        root=ROOT/'Art/PlushToy/P1/masters'/cid
        sheet=Image.open(root/'states.png').convert('RGBA');w,h=sheet.size
        frames={'idle':Image.open(root/'idle.png').convert('RGBA'),
                'attack':sheet.crop((0,0,w//2,h//2)), 'blink':sheet.crop((w//2,h//2,w,h))}
        sources[cid]=frames
    sheet=Image.open(ROOT/'Art/PlushToy/FirstFive/masters/purple/sheet.png').convert('RGBA');w,h=sheet.size
    sources['purple']={pose:sheet.crop((round(col*w/3),round(row*h/2),round((col+1)*w/3),round((row+1)*h/2)))
                       for pose,col,row in [('idle',0,0),('attack',1,0),('blink',1,1)]}
    for cid,frames in sources.items():
        for pose,im in frames.items():save(registered(im),f'home/{cid}/{pose}.png')
    # A proud portrait reuses the same blue identity, eyes gently closed in delight.
    save(registered(sources['blue']['blink'],224,256).crop((0,0,224,224)),'ui2/guide-proud.png')

def backgrounds():
    y,x=np.mgrid[0:2340,0:1080];t=y/2340
    glow=np.exp(-(((x-540)/700)**2+((y-1030)/900)**2))
    rgb=np.zeros((2340,1080,3))
    for i,(a,b) in enumerate(zip((192,169,232),(214,195,243))):rgb[:,:,i]=a*(1-t)+b*t+glow*12
    save(Image.fromarray(np.uint8(np.clip(rgb,0,255))),'home/background.png')
    # Album has the same studio palette, a lighter face for dense cards and labels.
    rgb=rgb*.55+np.array([255,248,244])*.45
    save(Image.fromarray(np.uint8(np.clip(rgb,0,255))),'home/codex-background.png')

def special_assets():
    mystery=registered(Image.open(BATCH/'masters/mystery/sheet.png').convert('RGBA'),224,256)
    save(mystery,'creatures/mystery/bubble.png')
    for state in ('idle','attack','spent'):
        save(mystery,f'creatures/mystery/8C919C/{state}.png')
    sheet=Image.open(BATCH/'masters/king/sheet.png').convert('RGBA');w,h=sheet.size
    for i,state in enumerate(('ready','blow')):
        cell=sheet.crop((round(i*w/2),0,round((i+1)*w/2),h))
        save(registered(cell,512,512),f'finale/king-{state}.png')

def icon(name, colors=PURPLE, size=128, glyph_only=False):
    if name=='settings': return round_control('settings',size)
    body=Mold(size,size) if glyph_only else button_mold(size,size,*colors)
    im=Image.new('RGBA',body.im.size);d=ImageDraw.Draw(im)
    k=size*body.SCALE/128
    def line(points,width=8,fill=INK):d.line([(round(x*k),round(y*k)) for x,y in points],fill=fill,width=round(width*k),joint='curve')
    def poly(points,fill=INK):d.polygon([(round(x*k),round(y*k)) for x,y in points],fill=fill)
    if name=='home':
        poly([(28,62),(64,31),(100,62),(90,62),(90,96),(73,96),(73,74),(55,74),(55,96),(38,96),(38,62)])
    elif name=='replay':
        d.arc((31*k,30*k,98*k,98*k),-75,225,fill=INK,width=round(9*k));poly([(23,40),(50,45),(30,64)])
    elif name=='music':
        line([(52,82),(52,39),(89,31),(89,74)],7)
        for cx,cy in [(42,84),(79,76)]:d.ellipse(((cx-12)*k,(cy-9)*k,(cx+12)*k,(cy+9)*k),fill=INK)
    elif name=='sound':
        poly([(27,53),(45,53),(66,35),(66,93),(45,75),(27,75)])
        for off in (0,12):d.arc(((57+off)*k,(39-off)*k,(90+off)*k,(88+off)*k),-65,65,fill=INK,width=round(5*k))
    elif name=='light':
        for a in range(0,360,45):
            r=math.radians(a);line([(64+28*math.cos(r),63+28*math.sin(r)),(64+39*math.cos(r),63+39*math.sin(r))],5)
        d.ellipse((45*k,44*k,83*k,82*k),fill=INK)
    elif name=='pattern':
        for py in (37,67):
            for px in (37,67):d.rounded_rectangle((px*k,py*k,(px+24)*k,(py+24)*k),5*k,fill=INK)
    elif name=='codex':
        d.rounded_rectangle((29*k,29*k,100*k,98*k),8*k,fill=INK)
        d.rounded_rectangle((38*k,35*k,94*k,89*k),4*k,fill=(255,247,229,255))
        line([(38,38),(38,88)],4)
        for cx,cy,r in [(67,64,15),(55,47,7),(79,47,7)]:d.ellipse(((cx-r)*k,(cy-r)*k,(cx+r)*k,(cy+r)*k),fill=(168,129,226,255))
    if glyph_only:
        body.symbol(im.getchannel('A'),(255,196,92),(226,128,27))
        rendered=body.im
        bounds=rendered.getchannel('A').point(lambda a:255 if a>4 else 0).getbbox()
        glyph=rendered.crop(bounds);scale=size*body.SCALE*.88/max(glyph.size)
        glyph=glyph.resize((round(glyph.width*scale),round(glyph.height*scale)),Image.Resampling.LANCZOS)
        centered=Image.new('RGBA',body.im.size);centered.alpha_composite(glyph,((centered.width-glyph.width)//2,(centered.height-glyph.height)//2))
        return centered.resize((size,size),Image.Resampling.LANCZOS)
    if name in ('home','replay'):
        # Their prefab labels occupy y=48..64 of a 76 px button. Reserve that
        # lower band, including outlines, instead of putting the icon behind it.
        glyph=im.resize((round(im.width*.78),round(im.height*.78)),Image.Resampling.LANCZOS)
        im=Image.new('RGBA',body.im.size)
        im.alpha_composite(glyph,((im.width-glyph.width)//2,-body.SCALE))
    # Symbols are sampled at the same 4x resolution as their raised button face.
    # Shift their centre upward to leave the visible lower wall free of artwork.
    shadow=Image.new('RGBA',im.size,(70,36,114))
    shadow.putalpha(im.getchannel('A').filter(ImageFilter.GaussianBlur(body.SCALE)).point(lambda a:round(a*.35)))
    body.im.alpha_composite(shadow,(0,-2*body.SCALE))
    body.im.alpha_composite(im,(0,-4*body.SCALE))
    return body.finish()

def main():
    backgrounds();home_members();special_assets()
    for path,w,h,colors,r in [('ui4/panel-card.png',360,360,CREAM,48),('ui2/popup-panel.png',256,256,CREAM,32),
        ('ui2/button-primary.png',520,104,GOLD,48),('ui2/button-secondary.png',400,96,CREAM,44),
        ('win/button-next.png',416,168,GOLD,78),('win/button-next-blue.png',416,168,PURPLE,78),
        ('finale/button-plate.png',680,172,GOLD,80),('finale/title-plate.png',1040,200,PURPLE,84),
        ('finale/clear-plate.png',1040,220,CREAM,95)]:
        if 'panel' in path: im=modal_panel(w,h,r)
        elif 'button' in path: im=raised_button(w,h,*colors,r)
        else: im=capsule(w,h,*colors,r,4,(185,150,224))
        save(im,path)
    save(icon('home',CREAM,152),'win/button-home.png')
    from build_plush_victory import main as victory_assets
    victory_assets()
    from build_plush_loss import main as loss_assets
    loss_assets()
    for name in ('music','sound','light','pattern'):save(icon(name,CREAM),f'ui4/icon-{name}.png')
    for name in ('settings','codex'):save(icon(name,PURPLE if name=='settings' else CREAM),f'home/btn-{name}.png')
    for on in (True,False):
        save(toggle(on),'ui2/toggle-'+('on' if on else 'off')+'.png')
    manifest={str(p.relative_to(OUT)):list(Image.open(p).size) for p in OUT.rglob('*.png') if 'creatures' not in p.parts}
    (BATCH/'shell-assets.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    print(f'Plush shell: {len(manifest)} assets')

if __name__=='__main__':main()

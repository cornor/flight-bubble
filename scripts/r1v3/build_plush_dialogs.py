#!/usr/bin/env python3
"""Settings/revive reference layout parts: native geometry plus a plush hero."""
import json
from pathlib import Path
from PIL import Image, ImageDraw
from build_plush_controls import Mold, button_mold, raised_button, modal_panel
from build_plush_shell import icon

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Art/PlushToy/P2-P4/delivery/dialog'
GREEN=((190,248,47),(169,231,31))
CYAN=((60,242,235),(24,217,217))
ORANGE=((255,213,75),(255,178,31))
PINK=((255,149,236),(234,95,207))
PAUSE_GREEN=((192,237,134),(133,206,76))
PAUSE_GOLD=((255,247,148),(255,188,54))

def save(im,name):
    OUT.mkdir(parents=True,exist_ok=True);im.save(OUT/(name+'.png'))

def panel():
    m=Mold(360,360)
    side=m.rect((4,7,356,353),48);rim=m.rect((4,3,356,349),48)
    m.shadow(side,.24,1.5,(0,1.5))
    m.surface(side,(28,181,221),(20,144,195),3,1,shine=.01)
    m.surface(rim,(80,224,255),(35,189,235),4,1.4,shine=.07)
    m.surface(m.rect((14,13,346,339),39),(255,255,246),(250,246,233),5,1.1,shine=.02)
    return m.finish()

def row():
    m=Mold(320,96)
    m.surface(m.rect((2,2,318,94),22),(244,232,216),(244,234,221),2,.4,shine=.01)
    m.surface(m.rect((5,5,315,91),19),(255,250,241),(253,247,236),2,-.3,shine=.01)
    return m.finish()

def pause_row():
    # Cream row with a fine lavender edge, matching the level-page card palette.
    m=Mold(320,96)
    rim=m.rect((3,3,317,91),22)
    m.shadow(rim,.15,1.1,(0,1.2))
    m.surface(rim,(244,227,246),(231,211,232),1.5,.3,shine=.01)
    m.surface(m.rect((5,5,315,89),20),(255,252,245),(249,241,230),2,.4,shine=.015)
    return m.finish()

def close():
    m=button_mold(96,96,(255,141,173),(241,80,129),42)
    mask=Image.new('L',m.im.size);d=ImageDraw.Draw(mask)
    for points in (((32,29),(64,61)),((64,29),(32,61))):
        d.line([(x*4,y*4) for x,y in points],fill=255,width=12*4)
        for x,y in points:d.ellipse(((x-6)*4,(y-6)*4,(x+6)*4,(y+6)*4),fill=255)
    m.symbol(mask,(255,255,253),(247,239,245));return m.finish()

def home():
    m=button_mold(152,152,*ORANGE,34)
    mask=Image.new('L',m.im.size);d=ImageDraw.Draw(mask)
    pts=[(41,57),(76,23),(111,57),(101,57),(101,88),(82,88),(82,68),(70,68),(70,88),(51,88),(51,57)]
    d.polygon([(x*4,y*4) for x,y in pts],fill=255)
    m.symbol(mask,(255,255,253),(247,241,226));return m.finish()

def video():
    m=Mold(96,80);mask=Image.new('L',m.im.size);d=ImageDraw.Draw(mask)
    d.rounded_rectangle((8*4,12*4,65*4,67*4),10*4,fill=255)
    d.polygon([(62*4,30*4),(86*4,17*4),(86*4,62*4),(62*4,50*4)],fill=255)
    d.polygon([(28*4,24*4),(49*4,39*4),(28*4,54*4)],fill=0)
    m.symbol(mask,(255,255,253),(243,248,233));return m.finish()

def skip():
    m=button_mold(152,152,*PINK,34)
    mask=Image.new('L',m.im.size);d=ImageDraw.Draw(mask)
    d.polygon([(45*4,29*4),(88*4,57*4),(45*4,86*4)],fill=255)
    d.rounded_rectangle((94*4,29*4,108*4,86*4),4*4,fill=255)
    m.symbol(mask,(255,255,253),(249,238,247))
    # Small video badge overlaps the top-right corner, as in the pause reference.
    m.surface(m.rect((109,3,150,37),9),(255,255,255),(252,242,251),2,.7,shine=.03)
    camera=Image.new('L',m.im.size);d=ImageDraw.Draw(camera)
    d.rounded_rectangle((116*4,11*4,134*4,29*4),3*4,fill=255)
    d.polygon([(132*4,16*4),(143*4,10*4),(143*4,30*4),(132*4,24*4)],fill=255)
    d.polygon([(122*4,15*4),(129*4,20*4),(122*4,25*4)],fill=0)
    m.symbol(camera,(175,69,161),(151,51,139));return m.finish()

def main():
    save(panel(),'panel');save(row(),'row');save(close(),'close');save(home(),'home');save(video(),'video');save(skip(),'skip')
    for name,colors in [('primary',GREEN),('resume',CYAN)]:save(raised_button(400,144,*colors,34),name)
    # Pause and home settings use the approved lavender/cream concept. Separate
    # slots preserve the existing revive and result dialogs' colors and shapes.
    save(modal_panel(360,360,48),'pause-panel')
    save(pause_row(),'pause-row')
    # Reuse the historical win/button-next template (416 x 168, radius 78)
    # for the green variant. The live gold button references win/button-next
    # directly; pause-resume is retained for older layout versions.
    save(raised_button(416,168,*PAUSE_GREEN,78),'pause-retry')
    save(raised_button(400,144,*PAUSE_GOLD,34),'pause-resume')
    for name in ('music','sound','light','pattern'):save(icon(name,size=128,glyph_only=True),'icon-'+name)
    # Crop/register only: preserve the generated plush fibers and alpha, no repaint.
    im=Image.open(ROOT/'Art/PlushToy/Dialogs-20261023/masters/revive-heart.png').convert('RGBA')
    box=im.getchannel('A').point(lambda a:255 if a>32 else 0).getbbox();assert box
    im=im.crop(box);im.thumbnail((376,292),Image.Resampling.LANCZOS)
    canvas=Image.new('RGBA',(400,320));canvas.alpha_composite(im,((400-im.width)//2,(320-im.height)//2))
    save(canvas,'revive-heart')
    print('Reference dialog assets:',len(list(OUT.glob('*.png'))))

if __name__=='__main__':main()

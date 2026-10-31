"""Read-only checks and composited QA previews of the four delivered PNGs."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageOps
import hashlib
import json
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SIZES = {'title.png': (720,200), 'arrow.png': (160,320), 'ribbon.png': (1080,200), 'sparkle.png': (96,96)}
report = {'backgrounds':['#B5D2F9','colored bubble texture cropped from supplied reference'], 'files':[]}
for filename, expected in SIZES.items():
    path = ROOT/'delivery'/filename
    im = Image.open(path).convert('RGBA')
    a = np.asarray(im)
    alpha = a[:,:,3]
    horizontal = np.concatenate([alpha[0],alpha[-1]])
    vertical = np.concatenate([alpha[:,0],alpha[:,-1]])
    assert im.size == expected
    assert alpha.min() == 0 and alpha.max() > 240
    assert horizontal.max() <= 3, (filename, int(horizontal.max()))
    if filename != 'ribbon.png':
        assert vertical.max() <= 3, (filename, int(vertical.max()))
    report['files'].append({'path':'delivery/'+filename,'size':list(im.size),'alpha_range':[int(alpha.min()),int(alpha.max())],
        'top_bottom_max_alpha':int(horizontal.max()),'left_right_max_alpha':int(vertical.max()),
        'semitransparent_pixel_count':int(((alpha>0)&(alpha<255)).sum()),
        'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})

ribbon = np.asarray(Image.open(ROOT/'delivery/ribbon.png').convert('RGBA'))
seam = np.abs(ribbon[:,0].astype(int)-ribbon[:,-1].astype(int))
assert seam.max() == 0
report['ribbon_seam'] = {'left_right_rgba_equal':True,'maximum_channel_difference':int(seam.max()),'preview':'qa/ribbon-three-tiles.png'}
report['wordmark'] = {'verbatim':'复活成功！','character_check':['复','活','成','功','！'],'ocr':'qa/wordmark-ocr.json'}
report['status'] = 'passed'
(ROOT/'qa/verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')

tile = Image.open(ROOT/'qa/bubble-background-tile.png').convert('RGBA')
def background(size, bubbles=False):
    im = Image.new('RGBA',size,'#B5D2F9')
    if bubbles:
        for y in range(0,size[1],tile.height):
            for x in range(0,size[0],tile.width):im.alpha_composite(tile,(x,y))
    return im

sheet = Image.new('RGB',(2240,1050),'#eeeeee')
draw = ImageDraw.Draw(sheet)
for column,bubbles in enumerate([False,True]):
    cell = background((1100,980),bubbles)
    for name,xy in [('title.png',(190,25)),('arrow.png',(340,270)),('sparkle.png',(670,390)),('ribbon.png',(10,720))]:
        cell.alpha_composite(Image.open(ROOT/'delivery'/name).convert('RGBA'),xy)
    sheet.paste(cell.convert('RGB'),(column*1120+10,50))
    draw.text((column*1120+20,16),'Colored bubbles' if bubbles else '#B5D2F9',fill='black')
sheet.save(ROOT/'qa/all-assets-backgrounds.png')

strip = Image.open(ROOT/'delivery/ribbon.png').convert('RGBA')
seams = Image.new('RGB',(3240,560),'#eeeeee')
for row,bubbles in enumerate([False,True]):
    cell = background((3240,240),bubbles)
    for i in range(3):cell.alpha_composite(strip,(i*1080,20))
    seams.paste(cell.convert('RGB'),(0,row*280+20))
    d = ImageDraw.Draw(seams)
    for x in [1080,2160]:
        d.line((x,row*280,x,row*280+18),fill='red',width=2)
        d.text((x+5,row*280+3),'join',fill='black')
seams.save(ROOT/'qa/ribbon-three-tiles.png')

# Review-only static assembly of the independent sprites.
overview = background((1240,900))
title = Image.open(ROOT/'delivery/title.png').convert('RGBA')
arrow = Image.open(ROOT/'delivery/arrow.png').convert('RGBA')
star = Image.open(ROOT/'delivery/sparkle.png').convert('RGBA')
overview.alpha_composite(arrow,(210,70))
overview.alpha_composite(ImageOps.mirror(arrow),(870,70))
overview.alpha_composite(title,(260,330))
for xy in [(200,480),(960,465),(450,565)]:overview.alpha_composite(star,xy)
overview.alpha_composite(strip,(80,660))
overview.convert('RGB').save(ROOT/'qa/overview.png')
print(json.dumps({'status':report['status'],'file_count':len(SIZES),'ribbon_seam':report['ribbon_seam']},ensure_ascii=False))

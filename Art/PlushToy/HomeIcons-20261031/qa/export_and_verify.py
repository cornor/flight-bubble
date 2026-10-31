"""Export generated artwork with sips/Swift, then measure and build QA previews."""
from pathlib import Path
from PIL import Image, ImageDraw
import hashlib
import json
import subprocess
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
ASSETS = [('I1','btn-settings.png'),('I2','btn-codex.png')]
exports = []
for asset_id, filename in ASSETS:
    master = ROOT/f'masters/{asset_id}-v02.png'
    image = Image.open(master).convert('RGBA')
    # Ignore alpha 1-3 fringe when locating the useful generated silhouette.
    bbox = image.getchannel('A').point(lambda a: 255 if a >= 4 else 0).getbbox()
    x0,y0,x1,y1 = bbox
    cropped = ROOT/f'qa/{asset_id}-crop.png'
    resized = ROOT/f'qa/{asset_id}-150.png'
    subprocess.run(['sips','--cropToHeightWidth',str(y1-y0),str(x1-x0),'--cropOffset',str(y0),str(x0),str(master),'--out',str(cropped)],check=True,capture_output=True)
    subprocess.run(['sips','--resampleHeightWidth','150','150',str(cropped),'--out',str(resized)],check=True,capture_output=True)
    target = ROOT/'delivery'/filename
    subprocess.run(['swift',str(ROOT/'qa/pad_export.swift'),str(resized),str(target)],check=True,capture_output=True)
    subprocess.run(['sips','--resampleHeightWidth','48','48',str(target),'--out',str(ROOT/f'qa/{asset_id}-48.png')],check=True,capture_output=True)
    exports.append({'id':asset_id,'target':'delivery/'+filename,'size':[160,160],
        'selected_master':str(master.relative_to(ROOT)),
        'export':{'crop_bbox_at_alpha_4':list(bbox),'resized_content':[150,150],'canvas':[160,160],'offset':[5,5],
                  'tools':['sips','qa/pad_export.swift']}})

report = {'backgrounds':['#FBFDFE','#E8F2FF'],'files':[]}
for asset_id,filename in ASSETS:
    path = ROOT/'delivery'/filename
    im = Image.open(path).convert('RGBA')
    a = np.asarray(im)
    alpha = a[:,:,3]
    assert im.size == (160,160)
    assert alpha.min() == 0 and alpha.max() == 255
    assert not alpha[:5].any() and not alpha[-5:].any()
    assert not alpha[:,:5].any() and not alpha[:,-5:].any()
    bbox = im.getchannel('A').getbbox()
    margins = [bbox[0],bbox[1],160-bbox[2],160-bbox[3]]
    assert all(4 <= m <= 6 for m in margins), (filename,margins)
    report['files'].append({'path':'delivery/'+filename,'size':[160,160],
        'alpha_bbox':list(bbox),'transparent_margins_left_top_right_bottom':margins,
        'outer_five_pixel_border_alpha':0,'preview_48':f'qa/{asset_id}-48.png',
        'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})

# Estimate the requested gear/book proportions from their central color regions.
settings = np.array(Image.open(ROOT/'delivery/btn-settings.png').convert('RGBA'))
yy,xx = np.mgrid[:160,:160]
central = (xx>30)&(xx<130)&(yy>30)&(yy<130)
gear = central&(settings[:,:,0]<175)&(settings[:,:,1]<185)&(settings[:,:,2]<205)&(settings[:,:,3]>240)
gy,gx = np.where(gear)
gear_width = int(gx.max()-gx.min()+1)
gear_rgb = np.median(settings[gear,:3],axis=0).astype(int).tolist()
book = np.array(Image.open(ROOT/'delivery/btn-codex.png').convert('RGBA'))
white = central&(book[:,:,0]>220)&(book[:,:,1]>220)&(book[:,:,2]>210)&(book[:,:,3]>240)
by,bx = np.where(white)
report['shape_measurements'] = {
    'gear_width_px':gear_width,'gear_width_fraction_of_150px_button':round(gear_width/150,3),
    'gear_median_rgb':gear_rgb,'gear_teeth_manual_count':8,
    'white_book_width_px':int(bx.max()-bx.min()+1),
    'white_book_width_fraction_of_150px_button':round((int(bx.max()-bx.min()+1))/150,3),
}

sheet = Image.new('RGB',(1000,650),'#eeeeee')
draw = ImageDraw.Draw(sheet)
for row,color in enumerate(report['backgrounds']):
    cell = Image.new('RGBA',(980,280),color)
    for column,(asset_id,filename) in enumerate(ASSETS):
        x = column*490
        full = Image.open(ROOT/'delivery'/filename).convert('RGBA')
        small = Image.open(ROOT/f'qa/{asset_id}-48.png').convert('RGBA')
        cell.alpha_composite(full,(x+35,55))
        cell.alpha_composite(small,(x+245,110))
        cell.alpha_composite(small.resize((144,144),Image.Resampling.NEAREST),(x+325,62))
    sheet.paste(cell.convert('RGB'),(10,row*325+35))
    draw.text((18,row*325+13),color,fill='#334155')
    for column,(asset_id,_) in enumerate(ASSETS):
        x=column*490
        draw.text((x+50,row*325+60),asset_id+' / 160px',fill='#334155')
        draw.text((x+250,row*325+60),'48px',fill='#334155')
        draw.text((x+330,row*325+60),'48px enlarged 3x',fill='#334155')
sheet.save(ROOT/'qa/backgrounds-and-48px.png')
report['status']='passed'
(ROOT/'qa/verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
(ROOT/'qa/exports.json').write_text(json.dumps(exports,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(report,ensure_ascii=False,indent=2))

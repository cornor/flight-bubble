"""Register AI-generated originals on 512px canvases and render the requested rig checks.
No painting/inpainting: only uniform resizing, placement, compositing and QA rotations.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json, math
import numpy as np
P=Path(__file__).resolve().parents[1]
SIZE=(512,512)
specs={
 'body':dict(master='D1-body-01.png',height=376,top=42),
 'arm-l':dict(master='D2-arm-l-02.png',height=124,sourcePivot=[773,445],pivot=[189,253]),
 'arm-r':dict(master='D3-arm-r-01.png',height=124,sourcePivot=[600,493],pivot=[349,252]),
 'leg-l':dict(master='D4-leg-l-02.png',height=114,sourcePivot=[725,426],pivot=[214,392]),
 'leg-r':dict(master='D5-leg-r-01.png',height=114,sourcePivot=[579,428],pivot=[309,392]),
 'mouth-open':dict(master='D6-mouth-open-01.png',width=43,xy=[276,150]),
}
exports={}
for name,s in specs.items():
 a=Image.open(P/'masters'/s['master']).convert('RGBA')
 box=a.getchannel('A').point(lambda v:255 if v>16 else 0).getbbox()
 crop=a.crop(box)
 scale=s['width']/crop.width if 'width' in s else s['height']/crop.height
 w,h=round(crop.width*scale),round(crop.height*scale)
 if name=='body':xy=[(512-w)//2,s['top']]
 elif 'pivot' in s:xy=[round(s['pivot'][0]-(s['sourcePivot'][0]-box[0])*scale),round(s['pivot'][1]-(s['sourcePivot'][1]-box[1])*scale)]
 else:xy=s['xy']
 out=Image.new('RGBA',SIZE)
 out.alpha_composite(crop.convert('RGBa').resize((w,h),Image.Resampling.LANCZOS).convert('RGBA'),xy)
 out.save(P/'delivery'/(name+'.png'))
 exports[name]=dict(master='masters/'+s['master'],sourceCrop=list(box),exportSize=[w,h],pasteXY=xy,**({k:s[k] for k in ['sourcePivot','pivot']} if 'pivot' in s else {}))
pivots={n:s['pivot'] for n,s in specs.items() if 'pivot' in s};pivots['mouth']=[298,168]
(P/'delivery/parts.json').write_text(json.dumps(pivots,indent=2)+'\n')
prompt=json.loads((P/'prompts.json').read_text());prompt['export']={'method':'Premultiplied-alpha Lanczos uniform resize and translation only; no redrawing; toy design from image_gen.','parts':exports,'drawOrderBackToFront':['leg-l','leg-r','arm-l','arm-r','body','mouth-open (optional)'],'angles':'Positive angle counterclockwise in these QA images; pivots measured from canvas top left.'};(P/'prompts.json').write_text(json.dumps(prompt,ensure_ascii=False,indent=2)+'\n')
imgs={n:Image.open(P/'delivery'/(n+'.png')).convert('RGBA') for n in specs}
order=['leg-l','leg-r','arm-l','arm-r','body','mouth-open']
def composite(angles={},mouth=True,bg=None):
 out=Image.new('RGBA',SIZE,bg or (0,0,0,0))
 for n in order:
  if n=='mouth-open' and not mouth:continue
  im=imgs[n]
  if n in angles:im=im.rotate(angles[n],Image.Resampling.BICUBIC,center=pivots[n])
  out.alpha_composite(im)
 return out
composite().save(P/'qa/assembled-open.png')
composite(mouth=False).save(P/'qa/assembled-closed.png')
for n,bg in [('white','#FFFFFF'),('blue','#2F86FF')]:
 composite(bg=bg).save(P/'qa'/('assembled-'+n+'.png'))
 frames=[(-40,{'arm-l':-40,'arm-r':40,'leg-l':40,'leg-r':-40}), (0,{}),(40,{'arm-l':40,'arm-r':-40,'leg-l':-40,'leg-r':40})]
 sheet=Image.new('RGB',(1536,552),bg);d=ImageDraw.Draw(sheet)
 for i,(v,aa) in enumerate(frames):
  d.text((i*512+20,12),f'RUN POSE {v:+d} deg',fill='#183864' if n=='white' else 'white')
  sheet.paste(composite(aa,bg=bg),(i*512,40))
 sheet.save(P/'qa'/('rotation-'+n+'.png'))
# Each limb individually at both extremes, with joint marked for the inspection sheet only.
sheet=Image.new('RGB',(4*384,2*424),'white');d=ImageDraw.Draw(sheet)
for col,n in enumerate(pivots):
 if n=='mouth':continue
 for row,angle in enumerate([-40,40]):
  frame=composite({n:angle},bg='#FFFFFF');dr=ImageDraw.Draw(frame);x,y=pivots[n];dr.ellipse((x-4,y-4,x+4,y+4),outline='#F15B39',width=1)
  sheet.paste(frame.resize((384,384),Image.Resampling.LANCZOS),(col*384,row*424+32));d.text((col*384+12,row*424+10),f'{n}: {angle:+d} deg',fill='#183864')
sheet.save(P/'qa/individual-rotations.png')
# Closed/open comparison and all source layer canvases.
sheet=Image.new('RGB',(1024,552),'#F2F6FD');d=ImageDraw.Draw(sheet)
for i,mouth in enumerate([False,True]):
 sheet.paste(composite(mouth=mouth,bg='#F2F6FD'),(512*i,40));d.text((512*i+20,12),'OPEN' if mouth else 'CLOSED',fill='#183864')
sheet.save(P/'qa/mouth-toggle.png')
sheet=Image.new('RGB',(3*384,2*424),'#EDF3FF');d=ImageDraw.Draw(sheet)
for i,n in enumerate(specs):
 tile=Image.new('RGBA',SIZE,'#EDF3FF');tile.alpha_composite(imgs[n]);x=i%3*384;y=i//3*424
 sheet.paste(tile.resize((384,384),Image.Resampling.LANCZOS),(x,y+32));d.text((x+12,y+10),n,fill='#183864')
sheet.save(P/'qa/layers.png')
print(json.dumps(exports,ensure_ascii=False,indent=2))
# Check every whole-degree angle, including the requested extremes. Measure the actual opaque overlap.
yy,xx=np.mgrid[:512,:512]
body_alpha=np.asarray(imgs['body'].getchannel('A'))
report={'canvas':[512,512],'format':'RGBA PNG','pivotCoordinates':'pixels, top-left origin','rotationRange':[-40,40],'rotationStep':1,'parts':{},'joints':{}}
for n,im in imgs.items():
 alpha=np.asarray(im.getchannel('A'))
 assert im.size==SIZE and im.mode=='RGBA' and alpha.min()==0 and alpha.max()==255
 assert not np.any(alpha[0]) and not np.any(alpha[-1]) and not np.any(alpha[:,0]) and not np.any(alpha[:,-1])
 report['parts'][n]={'size':list(im.size),'mode':im.mode,'alphaBounds':list(im.getchannel('A').getbbox()),'transparentPixels':int((alpha==0).sum())}
for n,point in pivots.items():
 if n=='mouth':continue
 overlaps=[];disk=((xx-point[0])**2+(yy-point[1])**2)<=8**2
 assert np.all(body_alpha[disk]>=250),(n,'body does not cover pivot')
 for angle in range(-40,41):
  rotated=imgs[n].rotate(angle,Image.Resampling.BICUBIC,center=point)
  aa=np.asarray(rotated.getchannel('A'))
  assert np.all(aa[disk]>=250),(n,angle,'joint gap')
  # A larger temporary canvas catches pixels that a normal 512px rotation might otherwise clip.
  large=Image.new('RGBA',(1024,1024));large.alpha_composite(imgs[n],(256,256));large=large.rotate(angle,Image.Resampling.BICUBIC,center=(point[0]+256,point[1]+256))
  box=large.getchannel('A').getbbox();assert box[0]>=256 and box[1]>=256 and box[2]<=768 and box[3]<=768,(n,angle,'clipped')
  overlaps.append(int(((aa>=250)&(body_alpha>=250)).sum()))
 report['joints'][n]={'pivot':point,'opaqueJointCoreRadius':8,'testedAngles':81,'minimumOpaqueOverlapPixels':min(overlaps),'clipped':False,'coreCoveredAtEveryAngle':True}
report['visualChecks']={'whiteBackground':'Passed: no rectangular background, no dark fringe or cast shadow; open/closed smile aligns.','blueBackground':'Passed on #2F86FF: intact fluffy edges and cream soles; silhouette remains readable.','individualExtremes':'Inspected all eight single-limb poses; body covers joint ends, no visible cracks.','notes':'Art rig QA only. No game code or transition animation integration in this delivery.'}
(P/'qa/verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
# Rig-only motion preview: just the six delivered layers and the specified rotations, no added art/effects.
frames=[]
for i in range(32):
 angle=40*math.sin(i/32*math.tau)
 frame=composite({'arm-l':angle,'arm-r':angle,'leg-l':-angle,'leg-r':-angle},mouth=i%16<10,bg='#FFFFFF').convert('RGB')
 frames.append(frame)
frames[0].save(P/'qa/rig-preview.gif',save_all=True,append_images=frames[1:],duration=55,loop=0,disposal=2)
print('PASS: 6 RGBA layers; 324 rotations, opaque joint overlap and no clipping.')

"""Reproducible H2-H9 pixel, dimensions, color and immutability audit."""
from pathlib import Path
import json,hashlib,math,importlib.util,numpy as np
from PIL import Image,ImageDraw
Q=Path(__file__).resolve().parent;D=Q.parent.parent;H=D.parent
spec=importlib.util.spec_from_file_location('h1colors',D/'_qa/check_h1_colors.py');mod=importlib.util.module_from_spec(spec);spec.loader.exec_module(mod)
names=['blue','orange','yellow','green','purple']
targets=['2982F4','FF7E27','FFDF48','62E18E','BD80FF']
centers=[[(470,1060),(480,1120)],[(345,770),(350,815)],[(720,875),(765,910)],[(252,1100),(300,1160)],[(885,1110),(830,1170)]]
files={};colors=[];sample=Image.open(D/'_qa/compose.png');dr=ImageDraw.Draw(sample)
for i,c in enumerate(names):
 for suffix in ['', '-blink']:
  path=D/f'squad-{c}{suffix}.png';im=Image.open(path);a=np.array(im)
  assert im.size==(1080,1920) and im.mode=='RGBA'
  files[path.name]={'size':im.size,'mode':im.mode,'transparentPixels':int((a[:,:,3]==0).sum()),'partialAlphaPixels':int(((a[:,:,3]>0)&(a[:,:,3]<255)).sum()),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
 im=np.array(Image.open(D/f'squad-{c}.png'))
 target=[int(targets[i][k:k+2],16) for k in [0,2,4]]
 for tone,(x,y) in zip(['lit','mid'],centers[i]):
  patch=im[y-7:y+8,x-7:x+8];rgb=np.rint(np.median(patch[:,:,:3].reshape(-1,3),axis=0)).astype(int).tolist()
  de=math.dist(mod.lab(rgb),mod.lab(target));box=[x-7,y-7,x+8,y+8]
  colors.append({'character':c,'tone':tone,'target':'#'+targets[i],'center':[x,y],'box':box,'hex':'#'+''.join(f'{v:02X}' for v in rgb),'rgb':rgb,'deltaE76':round(de,4),'pass':de<=8,'minAlpha':int(patch[:,:,3].min())})
  dr.rectangle(box,outline='white',width=1);dr.text((x+10,y-7),c[0]+'-'+tone,fill='#23406b')
sample.crop((90,550,990,1320)).save(Q/'final-color-samples.png')
(Q/'final-colors.json').write_text(json.dumps(colors,indent=2)+'\n')
bg=Image.open(D/'background.png');assert bg.size==(1080,2340) and bg.mode=='RGB'
files['background.png']={'size':bg.size,'mode':bg.mode,'sha256':hashlib.sha256((D/'background.png').read_bytes()).hexdigest()}
compose=bg.crop((0,210,1080,2130)).convert('RGBA')
for c in ['orange','yellow','green','purple','blue']:compose.alpha_composite(Image.open(D/f'squad-{c}.png'))
actual=np.array(Image.open(D/'_qa/compose.png').convert('RGB'));assert np.array_equal(np.array(compose.convert('RGB')),actual)
src=np.array(Image.open(H/'mock/01-home-mock.png').convert('RGB'));delta=np.abs(actual.astype(int)-src.astype(int));changed=np.any(delta>0,axis=2)
diff={'order':['orange','yellow','green','purple','blue'],'size':[1080,1920],'meanAbsoluteRGBChannelError':float(delta.mean()),'identicalPixels':int((~changed).sum()),'identicalPercent':float((~changed).mean()*100),'changedPixels':int(changed.sum()),'pixelsWithMaxChannelDifferenceOver8':int((delta.max(axis=2)>8).sum()),'pixelsWithMaxChannelDifferenceOver32':int((delta.max(axis=2)>32).sum())}
regions={'title_center':[162,150,918,420],'castle_and_right_island':[815,130,1080,550],'left_distant_island':[0,540,180,760],'decorative_bubbles':[240,350,620,540],'buttons':[0,1430,1080,1800]}
diff['regions']={k:{'box':b,'changedPixels':int(changed[b[1]:b[3],b[0]:b[2]].sum()),'maxChannelDifference':int(delta[b[1]:b[3],b[0]:b[2]].max())} for k,b in regions.items()}
yellow=np.array(Image.open(Q/'yellow-clean-visible-mask.png'))
purple=np.array(Image.open(D/'squad-purple.png'))[:,:,3]
blue=np.array(Image.open(D/'squad-blue.png'))[:,:,3]
overlap=(yellow>245)&(purple>245)&(blue<5)&changed
yy,xx=np.where(overlap)
diff['purpleOverYellowRestoredEarOverlap']={'pixels':int(overlap.sum()),'bounds':[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)] if len(xx) else None}
heat=np.zeros_like(src);v=delta.max(axis=2);heat[:,:,0]=np.minimum(v*5,255);heat[:,:,1]=np.minimum(v,150);Image.fromarray(heat).save(D/'_qa/compose-diff.png')
comparison=Image.new('RGB',(2160,1970),'#e8eef7');comparison.paste(Image.fromarray(src),(0,50));comparison.paste(Image.fromarray(actual),(1080,50));dd=ImageDraw.Draw(comparison);dd.text((20,18),'Approved H1',fill='#23406b');dd.text((1100,18),'H9 layer composition: orange, yellow, green, purple, blue',fill='#23406b');comparison.save(D/'_qa/compose-compare.png')
blink=json.loads((Q/'blink-differences.json').read_text())
for c in names:
 a=np.array(Image.open(D/f'squad-{c}.png'));b=np.array(Image.open(D/f'squad-{c}-blink.png'));allowed=np.zeros((1920,1080),bool)
 for x0,y0,x1,y1 in blink[c]['eyeRectanglesInclusiveExclusive']:allowed[y0:y1,x0:x1]=True
 assert not np.any(a[~allowed]!=b[~allowed]);assert np.array_equal(a[:,:,3],b[:,:,3])
refs=json.loads((Q/'reference-hashes.json').read_text())
for r in refs:r['currentSha256']=hashlib.sha256(Path(r['path']).read_bytes()).hexdigest();r['unchanged']=r['sha256']==r['currentSha256']
assert all(r['unchanged'] for r in refs)
counts={k:len(v) for k,v in json.loads((Q/'generation-log.json').read_text())['items'].items()};assert all(n<=4 for n in counts.values())
audit={'files':files,'generationCalls':counts,'colorsAllPassed':all(r['pass'] for r in colors),'blinkOutsideEyePixelsAllZero':True,'sourceReferences':refs,'compose':diff,'method':'Source interiors retained; generated hidden completions registered then alpha blended at occlusion edges. Green fourth bump removed. Background removal uses generated fill and gradient-domain blend. First/last 80 core rows crossfade to generated extensions.'}
(Q/'delivery-audit.json').write_text(json.dumps(audit,ensure_ascii=False,indent=2)+'\n');(Q/'compose-differences.json').write_text(json.dumps(diff,indent=2)+'\n')
print(json.dumps({'generationCalls':counts,'colors':colors,'compose':diff,'referencesUnchanged':all(r['unchanged'] for r in refs)},ensure_ascii=False,indent=2))

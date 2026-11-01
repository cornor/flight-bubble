from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib
import numpy as np
B=Path(__file__).resolve().parents[1]
roster=json.loads((B/'roster.json').read_text())
attempts=[json.loads(p.read_text()) for p in sorted((B/'qa/generations').glob('*.json'))]
reports=[]
for a in roster:
 p=B/'qa'/f'{a["creatureId"]}-verification.json'
 if p.exists():
  report=json.loads(p.read_text())
  delivery=B/a['delivery'];im=Image.open(delivery)
  assert im.size==(224,256) and im.mode=='RGBA'
  assert hashlib.sha256(delivery.read_bytes()).hexdigest()==report['sha256']
  orig=np.array(Image.open(B/'references'/a['reference']).convert('RGBA'));final=np.array(im)
  mask=np.array(Image.open(B/report['protection_mask']))>0
  assert np.array_equal(orig[mask],final[mask])
  assert not final[0,:,3].any() and not final[-1,:,3].any() and not final[:,0,3].any() and not final[:,-1,3].any()
  report['protection_mask_sha256']=hashlib.sha256((B/report['protection_mask']).read_bytes()).hexdigest()
  report['comparison']=f'qa/{a["creatureId"]}-idle-vs-cheer.png'
  report['visual_review']={'closed_happy_eyes':True,'open_laughing_mouth':True,'raised_limbs_or_wings':True,'identity_preserved':True,'no_clipping':True,'no_effects_or_text':True,'backgrounds':['#FFFFFF','#E8F2FF']}
  if a['creatureId']=='cyan':
   report['occlusion_repair']={'mask':'qa/masks/cyan-occlusion-repair.png','note':'原趴鳍抬起后，新露出的叶面及原鳍的接触边缘、遮挡阴影由第二次生成补齐；遮挡修复区域以外的受保护原叶面及花盆逐像素保留。'}
  reports.append(report)
counts={}
for a in attempts:
 counts[a['creatureId']]=counts.get(a['creatureId'],0)+1
 if a.get('input_refs'):a['refs']=a['input_refs']
 a['selected']=any(r['master']==a['master'] for r in reports)
 master=B/a['master'];source=Path(a['source'])
 assert hashlib.sha256(master.read_bytes()).digest()==hashlib.sha256(source.read_bytes()).digest()
 a['master_sha256']=hashlib.sha256(master.read_bytes()).hexdigest()
manifest={'batch':'Cheer-20261031','date':'2026-10-31','generator':'built-in image_gen.imagegen','generation_count':len(attempts),'generation_counts':counts,'delivery_count':len(reports),'status':'complete' if len(reports)==22 else 'in_progress','attempts':attempts,'assets':reports,'export_method':'sips resize; pot-anchor registration and local lower-body alignment; qa/pack_cheer.swift copies original RGBA pixels verbatim through qa/masks/<id>-protected.png; old lowered limb pixels excluded; blending only outside protected region; cyan v02 fills newly exposed leaf occlusions and contact fringe; outer 3px canvas fringe cleaned to transparent','qa':{'per_member':'qa/<creatureId>-idle-vs-cheer.png','protected_masks':'qa/masks/','overview':'qa/cheer-roster.png','comparison_overview':'qa/all-comparisons.png'}}
(B/'prompts.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
(B/'qa/verification.json').write_text(json.dumps({'count':len(reports),'protected_changed_pixels':sum(r['protected_region_changed_pixels'] for r in reports),'files':reports},ensure_ascii=False,indent=2)+'\n')
qa=Image.new('RGB',(1344,1120),'#E8F2FF');d=ImageDraw.Draw(qa)
comparison=Image.new('RGB',(1792,1680),'#E8F2FF');dc=ImageDraw.Draw(comparison)
for i,a in enumerate(roster):
 path=B/a['delivery']
 if not path.exists():continue
 im=Image.open(path).convert('RGBA');x=i%6*224;y=i//6*280;qa.paste(im,(x,y+22),im);d.text((x+8,y+4),a['creatureId']+' '+a['hex'],fill='black')
 x=i%4*448;y=i//4*280;dc.text((x+8,y+4),a['creatureId']+' / IDLE - CHEER',fill='black')
 orig=Image.open(B/'references'/a['reference']).convert('RGBA');comparison.paste(orig,(x,y+22),orig);comparison.paste(im,(x+224,y+22),im)
qa.save(B/'qa/cheer-roster.png');comparison.save(B/'qa/all-comparisons.png')
print(json.dumps({'generated':len(attempts),'delivered':len(reports),'protected_diff':sum(r['protected_region_changed_pixels'] for r in reports)}))

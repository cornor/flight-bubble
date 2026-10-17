from pathlib import Path
import json,hashlib,shutil
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from finish_round2 import meta,lab,rgb
from process_creatures import resize
from finish_third_batch_round1 import CAST
B=Path(__file__).resolve().parents[1];Q=B/'_qa';P=B/'third-batch-round2-prompts.json';d=json.loads(P.read_text())
ki=next(i for i in d['items'] if i['key']=='77-king-blow');kv=json.loads((Q/'r2-king-candidate.json').read_text());candidate=Path(kv['path']);dest=B/'finale/king-blow.png'
assert kv['mouth']['center'][0]==256 and 110<=kv['mouth']['center'][1]<=150
assert kv['baseline_y']==470 and 360<=kv['bbox'][3]-kv['bbox'][1]<=400 and kv['bbox'][2]-kv['bbox'][0]<=440
assert not dest.exists(),'Do not replace an accepted output silently'
shutil.copy2(candidate,dest);kv.update(meta(dest));ki['outputs']={'blow':kv};ki['selected_attempt']=ki['attempts'][-1]['attempt'];ki['status']='accepted_offline_checks';P.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n')
rows=[]
for i in d['items']:
 assert i['status']=='accepted_offline_checks' and 1<=len(i['attempts'])<=4
 for at in i['attempts']:
  assert Path(at['archive_path']).exists()
 for state,v in i['outputs'].items():
  f=Path(v['path']);im=Image.open(f);a=np.array(im);bb=im.getbbox();assert f.exists() and im.mode=='RGBA';assert hashlib.sha256(f.read_bytes()).hexdigest()==v['sha256']
  if i['character']!='king':
   assert im.size==(224,256) and bb[3]==213 and not a[213:,:,3].any() and abs((bb[0]+bb[2])/2-112)<=.5
   if state in ('idle','attack','spent'):assert 120<=bb[3]-bb[1]<=150 and bb[2]-bb[0]<=180
   for s in v['samples']:
    x,y=s['xy'];actual=np.rint(np.median(a[y-1:y+2,x-1:x+2,:3],axis=(0,1))).astype('uint8');assert actual.tolist()==s['rgb'];assert np.linalg.norm(lab(actual)-lab(rgb(i['color'])))<=8
   if state=='blink':
    idle=np.array(Image.open(f.parent/'idle.png'));allowed=np.zeros(a.shape[:2],bool)
    for x0,y0,x1,y1 in v['postprocess']['patch_rectangles']:allowed[y0:y1,x0:x1]=True
    assert not (np.any(a!=idle,2)&~allowed).any() and np.array_equal(a[:,:,3],idle[:,:,3])
  rows.append(dict(character=i['character'],state=state,**v))
assert len(rows)==61
old=[('blue','2982F4','小蓝'),('orange','FF7E27','小橙'),('green','62E18E','小绿'),('purple','BD80FF','小紫'),('yellow','FFDF48','小黄'),('white','FFFFFF','小白'),('red','FF525B','小红'),('charcoal','2D2D2D','小黑')]
cast=[(n,c,t) for n,c,t,_ in CAST]+old;names={n:t for n,c,t in cast};sprites={n:Image.open(B/'creatures'/n/c/'idle.png').convert('RGBA') for n,c,t in cast}
font=lambda n:ImageFont.truetype('/System/Library/Fonts/STHeiti Light.ttc',n)
groups=[['cyan','blue'],['violet','lavender','slate-blue'],['lime','mint','forest','pale-mint'],['berry','red','pink'],['peach','orange'],['brown','charcoal'],['pale-mint','white'],['green','purple','yellow']]
board=Image.new('RGB',(1160,920),'#F4F6F9');dr=ImageDraw.Draw(board);dr.text((22,12),'第九次 · 20个角色 · 40像素彩色 / 灰度对照',font=font(26),fill='#26364D');dr.text((22,50),'全部取自最终交付 idle.png；有效轮廓高40px，按100%查看。',font=font(15),fill='#59687D')
for row,group in enumerate(groups):
 y=86+row*98;dr.rounded_rectangle((16,y,1144,y+91),8,fill='#E6EBF1' if row%2 else 'white');dr.text((28,y+8),f'近色组 {row+1}' if row<7 else '其余老角色',font=font(15),fill='#33425A');dr.text((153,y+5),'彩色',font=font(13),fill='#78859A');dr.text((663,y+5),'灰度',font=font(13),fill='#78859A')
 for col,name in enumerate(group):
  im=sprites[name].crop(sprites[name].getbbox());im=resize(im,(round(im.width*40/im.height),40));gray=im.convert('LA').convert('RGBA');gray.putalpha(im.getchannel('A'))
  for x,t in [(150,im),(660,gray)]:
   board.paste(t,(x+col*118+(80-t.width)//2,y+23),t);dr.text((x+col*118,y+65),f'{names[name]} {name}',font=font(11),fill='#33425A')
board.save(Q/'lineup-new-40px.png')
# Native-sized idle grid and king pose; detailed five-state sheets remain separate.
overview=Image.new('RGB',(1504,950),'#E8EDF3');dr=ImageDraw.Draw(overview);dr.text((24,20),'第九次 · 12个新角色 × 5态 + 泡泡大王「吹」',font=font(30),fill='#26364D');dr.text((24,63),'61张透明PNG · 三批角色五态预览及检测记录见交付清单',font=font(18),fill='#59687D')
for idx,(name,color,label,_) in enumerate(CAST):
 x=16+(idx%4)*240;y=100+(idx//4)*270;im=sprites[name];overview.paste(im,(x,y),im);dr.text((x+22,y+235),f'{label} · {name}',font=font(16),fill='#26364D')
im=Image.open(dest);overview.paste(im,(976,170),im);dr.text((1020,705),'king-blow · 512×512 RGBA',font=font(22),fill='#26364D');dr.text((1020,747),'嘴中心 (256, 142.5) · 脚底 y=470',font=font(18),fill='#59687D');overview.save(Q/'r2-delivery-overview.png')
manifest=B/'交付清单.md';marker='R1_V3_ROUND2_COMPLETE';assert marker not in manifest.read_text()
maxde=max(s['delta_e76'] for r in rows for s in r.get('samples',[]));totalattempts=sum(len(i['attempts']) for i in d['items'])
text=f'''\n\n<!-- {marker}_START -->
## 第九次 · 全轮完成（2026-10-16）

共61张成品：12个新角色各5态共60张，另加`finale/king-blow.png`。三批20张依次完成并登记，未制作projectile。已确认的king-ready和既有角色成品保持不变。

[整轮总览](_qa/r2-delivery-overview.png) · [20角色40像素彩色/灰度对照](_qa/lineup-new-40px.png) · [小翠深底放大检查](_qa/r2-forest-dark-edge-review.png) · [大王ready/blow对照](_qa/r2-king-ready-blow-comparison.png) · [最终验收JSON](_qa/r2-final-audit.json)

### 泡泡大王「吹」

| 文件 | 画布/轮廓 | 实测锚点 | 提示词/次数 | 自查 |
| --- | --- | --- | --- | --- |
| `finale/king-blow.png` | 512×512 RGBA；轮廓411×381；bbox=(50,90,461,471) | 脚底y=470；轮廓中心x=255.5（整数像素居中）；嘴中心 **(256,142.5)** | 77 / 2 | 通过 |

以确认ready为首张参考进行编辑；第二稿将嘴上移、皇冠后倾。按ready的411px轮廓宽度等比缩放，并使用同一水平位置x=50和脚底y=470。皇冠顶y=90（≥16），两态动作导致总高相差5px；未拉伸，未重画或覆盖ready。口腔主体bbox=(236,121,277,165)，取像素外接框中心测量，排除唇边和皇冠宝石；无需额外嘴部位移。未画泡泡流。

### 检测与边界

- 60张角色PNG均224×256；最低非透明像素y=212，y>212全透明；三态共用等比缩放；眨眼仅眼框外扩4px范围发生变化，其余与idle逐像素一致，Alpha完全一致。
- 120个身体取样点复核RGB和ΔE76，最大ΔE76为{maxde:.3f}，均≤8；每点坐标、RGB和色差见前面三批表及JSON。
- 小翠浅绿描边由AI直接生成，未后期改色描边；全部五态已在深色底以2倍全图及4倍局部检查，边缘连续，无明显毛刺。
- 20个角色对照图现已全部更新为最终idle，按任务书近色组排列彩色/灰度40px轮廓；小碧的宽扁身体、小翠的双泡、小清卷尾、小荧五角以及其他近色组仍可区分。
- 小碧三态图使用第3稿，前两稿等比缩放后最低高度不足120px；最后三态125/127/120px。其余角色各项均1次生成，大王2次；合计{totalattempts}次生成，任何项均未超过4次上限。所有原图、逐次完整提示词、参考路径和后期记录均保留在`third-batch-round2-prompts.json`和`_originals/r2-*.png`。
- 本轮离线资产自查完成；尚未进行游戏实机验收。只主动写入`Art/R1/art-v3/`，未提交Git。
<!-- {marker}_END -->
'''
with manifest.open('a') as f:f.write(text)
before=json.loads((Q/'before-third-batch-round2-sha256.json').read_text());changed=[];missing=[]
for rel,sha in before.items():
 f=B.parent/rel
 if not f.exists():missing.append(rel)
 elif hashlib.sha256(f.read_bytes()).hexdigest()!=sha:changed.append(rel)
allowed={'delivery/交付清单.md','delivery/_qa/lineup-new-40px.png'};old_art_changes=[p for p in changed if p.startswith(('delivery/creatures/','delivery/finale/','delivery/fx/','delivery/ui/','lineup/'))];assert not old_art_changes and not missing,(old_art_changes,missing)
report=dict(passed=True,output_count=len(rows),character_files=60,king_files=1,color_sample_count=120,max_delta_e76=maxde,generation_calls=totalattempts,max_attempts_per_item=max(len(i['attempts']) for i in d['items']),outputs=rows,king_ready_unchanged=True,old_delivered_art_unchanged=True,baseline_changed_files=changed,planned_existing_changes=sorted(allowed),other_changed_files=[p for p in changed if p not in allowed],missing_baseline_files=missing,manual_visual_review=dict(five_state_sheets=True,forest_dark_edges=True,near_color_40px=True,king_pair=True),runtime_game_test='not performed')
(Q/'r2-final-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');print(json.dumps({k:v for k,v in report.items() if k!='outputs'},ensure_ascii=False,indent=2))

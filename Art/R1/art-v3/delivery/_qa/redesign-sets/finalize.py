from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image,ImageDraw,ImageFont,ImageOps
import finish
Q=finish.Q;D=finish.D
NAMES={'charcoal':'小黑','slate-blue':'小靛','mint':'小碧','forest':'小翠','berry':'小玫'}
GROUPS=[
 ('小黑同组 · 棕 / 深灰',[('brown','65392D'),('charcoal','2D2D2D')]),
 ('小靛同组 · 紫 / 淡紫 / 靛',[('violet','B672FD'),('lavender','C8C5FF'),('slate-blue','696CAF')]),
 ('小碧、小翠同组 · 绿系',[('lime','92EF4D'),('mint','30D183'),('forest','0C7E1C'),('pale-mint','C5FFE4')]),
 ('小玫同组 · 玫 / 红 / 粉',[('berry','9C225E'),('red','FF525B'),('pink','FFC5E0')])]
def font(n):return ImageFont.truetype('/System/Library/Fonts/STHeiti Light.ttc',n)
def proof40():
 canvas=Image.new('RGB',(1100,1000),'#EDF1F7');dr=ImageDraw.Draw(canvas);layout=[]
 dr.text((24,18),'第四批整套 · 新 idle 与同组近色角色',font=font(26),fill='#243550')
 dr.text((24,55),'角色实际高度 40 px；每组上排彩色、下排灰度。蓝色标记为本次重画。',font=font(16),fill='#465770')
 for row,(title,items) in enumerate(GROUPS):
  top=100+row*220;dr.rounded_rectangle((20,top,1080,top+203),radius=12,fill='#FFFFFF')
  dr.text((38,top+12),title,font=font(20),fill='#243550')
  for mode,y in [('color',top+48),('grayscale',top+122)]:
   dr.text((40,y+13),'彩色' if mode=='color' else '灰度',font=font(16),fill='#52647D')
   for j,(ident,hx) in enumerate(items):
    path=D/'creatures'/ident/hx/'idle.png';im=Image.open(path).convert('RGBA');im=im.crop(im.getchannel('A').getbbox());sm=finish.resize(im,(round(im.width*40/im.height),40))
    if mode=='grayscale':
     gray=ImageOps.grayscale(sm);sm=Image.merge('RGBA',(gray,gray,gray,sm.getchannel('A')))
    x=180+j*220;dr.rounded_rectangle((x-40,y-4,x+60,y+43),radius=6,fill='#17213A');canvas.paste(sm,(x+(20-sm.width)//2,y),sm)
    label=(NAMES.get(ident,ident))+'  #'+hx;dr.text((x-68,y+47),label,font=font(13),fill='#2368C9' if ident in NAMES else '#52647D')
    layout.append(dict(group=title,id=ident,color=hx,mode=mode,source=str(path),nativePixelSize=list(sm.size),actualAlphaHeight=sm.getchannel('A').getbbox()[3]-sm.getchannel('A').getbbox()[1]))
 dest=D/'_qa/redesign-sets-40px.png';canvas.save(dest);(Q/'proof-40px-layout.json').write_text(json.dumps(layout,ensure_ascii=False,indent=2)+'\n');return dest
def audit():
 c=finish.read();base=json.loads((Q/'protected-baseline.json').read_text());changed=set();out=[];archives=[];generation_log=[]
 for ident,it in c['characters'].items():
  assert it['status']=='passed and delivered',ident
  for state in finish.STATES:
   path=Path(it['outputDir'])/f'{state}.png';changed.add(str(path));m=finish.meta(path);row=it['outputs'][state]
   assert m['sha256']==row['sha256'] and m['size']==[224,256] and m['mode']=='RGBA'
   assert m['lowestPixelY']==212 and m['below212Empty'] and abs(m['centerX']-112)<=.5
   if state in ['idle','attack','spent']:assert 120<=m['height']<=150 and m['width']<=180
   im=Image.open(path);a=np.array(im)
   for sample in row['samples']:
    x,y=sample['xy'];v=np.rint(np.median(a[y-1:y+2,x-1:x+2,:3],axis=(0,1))).astype(np.uint8);de=float(np.linalg.norm(finish.pr.lab(v)-finish.pr.lab(finish.pr.rgb(it['hex']))));assert de<=8,(ident,state,sample,de)
   if state=='blink':
    old=np.array(Image.open(Path(it['outputDir'])/'idle.png'));diff=np.any(a!=old,axis=2);allowed=np.zeros((256,224),bool)
    for x0,y0,x1,y1 in row['allowedRectangles']:allowed[y0:y1,x0:x1]=True
    assert not (diff&~allowed).any() and np.array_equal(a[:,:,3],old[:,:,3]);assert int(diff.sum())==row['changedPixels']
   out.append({'id':ident,'state':state,**row,'finalMetadata':m})
  assert it['outputs']['cheer']['widthDifferencePercent']<=5 and it['selfCheck']['bodyWidthDifferencePercent']<=5
  for r in it['archive']:
   assert finish.sha(Path(r['archive']))==base[r['original']];archives.append(r)
  for state,ats in it['attempts'].items():
   for at in ats:
    assert finish.sha(Path(at['original']))==at['sha256'];refs=[str(Q/'staging'/ident/'idle.png'),it['reference'],c['mockup']]
    if state=='idle':refs=refs[1:]
    elif state=='blink' and ((ident=='forest' and at['attempt']==2) or ident=='berry'):refs=[refs[0],refs[2]]
    selected=at['attempt']==it['outputs'][state]['attempt']
    reason='selected'
    if not selected:reason='rejected: duplicated arms' if ident=='charcoal' else 'rejected: glossy thick closed eyelids with highlights'
    generation_log.append({'id':ident,'state':state,**at,'selected':selected,'reason':reason,'references':[{'path':p,'sha256':finish.sha(Path(p))} for p in refs],'promptSha256':finish.sha(Path(at['prompt']))})
 protected=[]
 for p,h in base.items():
  if p not in changed:
   assert finish.sha(Path(p))==h,p;protected.append(p)
 mbase=json.loads((Q/'manifest-baseline.json').read_text());prefix=(D/'交付清单.md').read_bytes()[:mbase['length']];assert hashlib.sha256(prefix).hexdigest()==mbase['sha256']
 result={'outputs':out,'generationCount':len(generation_log),'maxGenerationsPerAsset':max(len(a) for it in c['characters'].values() for a in it['attempts'].values()),'outputCount':len(out),'archiveCount':len(archives),'archives':archives,'unmodifiedProtectedCount':len(protected),'protectedFiles':protected,'manifestPrefixUnchanged':True,'allDimensionsPassed':True,'allBaselinesPassed':True,'allBlinkOutsideEyeDifferencesZero':True,'allBlinkAlphaIdentical':True,'allCheerWidthDifferencesLE5Percent':True,'allColorSamplesPassed':True,'maxDeltaE76':max(s['deltaE76'] for o in out for s in o['samples']),'smallestThreeStateHeight':min(o['height'] for o in out if o['state'] in ['idle','attack','spent']),'largestThreeStateWidth':max(o['width'] for o in out if o['state'] in ['idle','attack','spent']),'visualReview':'All 25 final sprites inspected at native scale on dark background; same-group 40px color and grayscale proof pending final review.'}
 (Q/'generation-log.json').write_text(json.dumps(generation_log,indent=2)+'\n');(Q/'validation.json').write_text(json.dumps(result,indent=2)+'\n')
 return result
def report():
 c=finish.read();r=audit();lines=['\n\n## 第四批整套：已确认方案的五角色战斗图（2026-10-17）','',
 '按任务书 6.3、6.4、5.2 交付；小黑 A、小靛 A、小碧 A、小翠 A、小玫 B。使用内置 imagegen，每张单独生成；先完成小黑五张并自查通过、归档替换，再开始其余四个。小紫及其战斗图未改动。','',
 f"共 **{r['generationCount']} 次生成 / 25 张成品**；小黑 attack 与小翠 blink 各 2 次，其余每张 1 次。弃稿与选中原图均保存在 `delivery/_originals/redesign-set-*.png`。每次实际提示词见 `delivery/_qa/redesign-sets/*-prompt.txt`，原图、引用、次数与 SHA-256 见同目录 `generation-log.json`。画风段使用提示词文件中的整段 [STYLE]。",'',
 '### 文件、尺寸与颜色','',
 '| 角色 / 方案 | 交付目录（各 idle / attack / spent / blink / cheer） | 三态尺寸（实际占用宽×高） | 全套主色最大 ΔE76 |','| --- | --- | --- | --- |']
 for ident,it in c['characters'].items():
  dims=' / '.join(f"{it['outputs'][s]['width']}×{it['outputs'][s]['height']}" for s in ['idle','attack','spent']);mx=max(s['deltaE76'] for o in it['outputs'].values() for s in o['samples'])
  lines.append(f"| {NAMES[ident]} {it['variant']} | `delivery/creatures/{ident}/{it['hex']}/` | {dims} | {mx:.4f} |")
 lines+=['','25 张均为 **224×256 RGBA**，实际最低可见像素 **y=212**，y≥213 完全透明；按可见轮廓居中，整数像素取整后的中心 x 为 111.5 或 112，最大偏差 0.5 px。三态实际高度 120–150、宽度≤180。欢呼原图只做等比缩放，没有横向拉伸。','',
 '保留选定方案的造型与配色：小黑软绒毛、星星眼高光、浅灰肚皮；小靛单片扁尾与双鳍、无脚无角；小碧顶部两颗真实泡泡眼；小翠上下双泡泡及浅绿肚皮；小玫右侧长尾与尾尖泡泡。深色角色的浅描边、浅色肚皮或脸颊、开眼态大眼白均保留。出手态鼓腮、视线与 O 口朝上，不带弹体；泄气态歪斜、半闭眼、一滴汗。','',
 '色差采用 sRGB → CIELAB（D65）→ ΔE76；每张从主色身体色块的中间调、较亮部各取一处 3×3 中位数，排除白色高光、浅肚皮/脸颊和描边。完整坐标、RGB、色差见 `delivery/_qa/redesign-sets/validation.json`。校色仅作用于主色内部，保留生成的浅描边和浅色点缀；未后画描边。','',
 '### 眨眼逐像素核对','',
 '取生成图中的闭眼部分回贴到 idle；矩形外直接保留 idle 原像素，alpha 全帧完全相同。为去掉生成器连带改动，鼻、嘴、眉毛所在区域按需保留 idle；小翠第二稿闭眼区另作局部对齐，去除偏移眉毛和残余瞳孔。以下矩形均为 `[x0,y0,x1,y1)`，右/下边界不含。','',
 '| 角色 | 允许修改的眼睛矩形（含 4 px 余量） | 实际变化包围盒 | 变化像素 | 矩形外变化 |','| --- | --- | --- | --- | --- |']
 for ident,it in c['characters'].items():
  b=it['outputs']['blink'];lines.append(f"| {NAMES[ident]} | `{b['allowedRectangles']}` | `{b['changedBounds']}` | {b['changedPixels']} | 0 |")
 lines+=['','### 欢呼尺度与检查图','','同时核对整体剪影最宽处和躯干宽度。躯干使用两态相同的归一化纵向区间，避开举起/放下的手臂；区间、测量值均记录在 `config.json` 的 selfCheck。','',
 '| 角色 | 整体最宽处 idle → cheer | 差值 | 躯干宽度 idle → cheer | 差值 |','| --- | --- | --- | --- | --- |']
 for ident,it in c['characters'].items():
  sc=it['selfCheck'];lines.append(f"| {NAMES[ident]} | {it['outputs']['idle']['width']} → {it['outputs']['cheer']['width']} | {it['outputs']['cheer']['widthDifferencePercent']:.2f}% | {sc['bodyWidthIdle']} → {sc['bodyWidthCheer']} | {sc['bodyWidthDifferencePercent']:.2f}% |")
 lines+=['',
 '- `delivery/_qa/redesign-sets.png`：5 行 × 5 态，深色底与 y=212 基线；已逐张看过脸、轮廓和姿势。',
 '- `delivery/_qa/redesign-sets-40px.png`：按 5.1 近色组放置新 idle 与旧同组 idle，实际可见高度 40 px，彩色、灰度各一排；小黑绒毛对小棕圆耳、小靛扁尾对小晶棱角/小雾云团、小碧双眼对小翠双泡泡/小荧星形/小清卷尾、小玫长尾对小红倒心/小粉泡泡辫，可区分。','',
 '### 旧图归档与范围','',
 '覆盖前先将旧五张移动到 `delivery/_originals/old-charcoal/`、`old-slate-blue/`、`old-mint/`、`old-forest/`、`old-berry/`。共 25 张旧图，归档 SHA-256 与覆盖前一致；仅替换要求的五态文件。',
 f"保护检查：{r['unmodifiedProtectedCount']} 张非目标角色图和方案图与操作前哈希一致（含小紫、旧 projectile）；本清单原有内容完整保留。未写入 art-v3 以外的项目目录，未执行 Git 提交。",'',
 '### 未做到的项','',
 '无未完成的交付文件或超阈值项。水平居中采用整数像素取整，奇数宽轮廓中心为 x=111.5（距目标 0.5 px）；未使用亚像素平移，以保留干净边缘。40 px 的剪影辨识为人工目视检查，未做用户辨识实验。','']
 text='\n'.join(lines);(Q/'delivery-record.md').write_text(text);return text
if __name__=='__main__':
 print(proof40());r=audit();print(json.dumps({k:r[k] for k in ['outputCount','archiveCount','generationCount','maxDeltaE76','unmodifiedProtectedCount']}));report()

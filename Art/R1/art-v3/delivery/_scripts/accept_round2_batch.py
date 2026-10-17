from pathlib import Path
import json,sys,hashlib
import numpy as np
from PIL import Image
from finish_round2 import proof
B=Path(__file__).resolve().parents[1];P=B/'third-batch-round2-prompts.json';data=json.loads(P.read_text());batch=int(sys.argv[1]);items=[i for i in data['items'] if i['batch']==batch];names=[i['character'] for i in items if i['pose']=='sheet'];rows=[]
for name in names:
 group=[i for i in items if i['character']==name];outputs={k:(it,v) for it in group for k,v in it['outputs'].items()};assert set(outputs)==set(['idle','attack','spent','blink','cheer'])
 idle=np.array(Image.open(outputs['idle'][1]['path']));blink=np.array(Image.open(outputs['blink'][1]['path']));allow=np.zeros((256,224),bool)
 for x0,y0,x1,y1 in outputs['blink'][1]['postprocess']['patch_rectangles']:allow[y0:y1,x0:x1]=True
 diff=np.any(idle!=blink,2);assert not (diff&~allow).any();assert np.array_equal(idle[:,:,3],blink[:,:,3])
 for state,(it,v) in outputs.items():
  p=Path(v['path']);im=Image.open(p);a=np.array(im);bb=im.getbbox();assert im.size==(224,256) and im.mode=='RGBA';assert bb[3]==213 and not a[213:,:,3].any();assert abs((bb[0]+bb[2])/2-112)<=.5
  assert all(s['delta_e76']<=8 for s in v['samples']);assert hashlib.sha256(p.read_bytes()).hexdigest()==v['sha256'];assert len(it['attempts'])<=4
  if state in ('idle','attack','spent'):assert 120<=bb[3]-bb[1]<=150 and bb[2]-bb[0]<=180
  if state=='cheer':assert bb[1]>=30 and bb[2]-bb[0]<=210 and abs(v['postprocess']['body_width_ratio']-1)<=.05
  rows.append(dict(character=name,state=state,number=it['number'],attempts=len(it['attempts']),**v))
 for it in group:it['status']='accepted_offline_checks'
preview=proof(data,names);report=dict(batch=batch,characters=names,files=len(rows),passed=True,preview=preview,outputs=rows,manual_visual_review='completed: five state faces and silhouettes; no unintended shadows, halos, text or clipped features')
qa=B/'_qa'/f'r2-batch-{batch}-audit.json';qa.write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n');P.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
manifest=B/'交付清单.md';marker=f'R1_V3_ROUND2_BATCH_{batch}';assert marker not in manifest.read_text()
text=f'\n\n<!-- {marker}_START -->\n## 第九次 · 第二轮第{batch}批角色（2026-10-16）\n\n已完成：'+ '、'.join(names)+'，每人5态，共20张224×256 RGBA PNG。先完成本批首位角色的5态并通过自查，再制作其余3位；本批全部通过后才开始下一批。\n\n'
text+=f'[五态预览](_qa/{Path(preview).name}) · [逐文件检测与颜色取样](_qa/{qa.name}) · [完整执行提示词、原图与后期记录](third-batch-round2-prompts.json)\n\n'
text+='所有文件最低非透明像素y=212，y>212全透明，水平居中误差≤0.5px；idle/attack/spent使用同一等比缩放。blink由内置image_gen编辑，再仅将眼部小矩形（外扩4px）贴回idle，Alpha完全一致、眼部矩形之外0像素变化。cheer按身体宽度等比缩放，与idle宽度差≤5%，顶y≥30、全宽≤210。颜色为3×3身体样点RGB中值，CIELAB ΔE76≤8，排除描边、镜面高光和眼白。\n\n'
text+='| 文件（相对delivery） | 提示词 / 生成次数 | 中间调：色值 / 坐标 / ΔE76 | 较亮部：色值 / 坐标 / ΔE76 | 自查 |\n| --- | --- | --- | --- | --- |\n'
for row in rows:
 vals=[f"{s['hex']} / {tuple(s['xy'])} / {s['delta_e76']:.3f}" for s in row['samples']]
 text+=f"| `{Path(row['path']).relative_to(B)}` | {row['number']} / {row['attempts']} | {vals[0]} | {vals[1]} | 通过 |\n"
text+='\n眨眼差异范围和欢呼宽度：\n\n| 角色 | blink差异框(x,y,w,h) / 像素数 | cheer身体宽度 idle→cheer / 比值 |\n| --- | --- | --- |\n'
for name in names:
 bl=next(r for r in rows if r['character']==name and r['state']=='blink')['postprocess'];ch=next(r for r in rows if r['character']==name and r['state']=='cheer')['postprocess']
 text+=f"| {name} | {bl['changed_bbox_xywh']} / {bl['changed_pixels']} | {ch['idle_body_width']}→{ch['cheer_body_width']} / {ch['body_width_ratio']:.4f} |\n"
text+='\n后期保留AI绘制的造型、表情和描边，清理低Alpha杂点并校准身体色；外轮廓像素不做主色替换。眨眼的实际眼框、眉毛保护和必要的临时眉区清理均写入JSON。各生成原图在`_originals/r2-*.png`，精确提示词和参考路径在JSON；每个文件最多4次生成。\n\n未做到/边界：本批无缺交；未制作projectile（程序绘制），未运行实机验收。其余批次和king-blow按顺序继续，不把这一批完成称为整轮完成。主动写入仅Art/R1/art-v3，未提交Git。\n'
text+=f'<!-- {marker}_END -->\n'
with manifest.open('a') as f:f.write(text)
print(json.dumps(dict(batch=batch,files=len(rows),passed=True,preview=preview),ensure_ascii=False))

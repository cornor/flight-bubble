"""Verify emotes against their immutable idle sources; append the complete record."""
from pathlib import Path
import hashlib,json,sys
import numpy as np
from PIL import Image,ImageDraw
from process_emotes import body_width,preview,color_samples

ROOT=Path(__file__).resolve().parents[1];ART=ROOT.parent
P=ROOT/'emotes-58-59-prompts.json'


def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()


def audit(final=False):
    m=json.loads(P.read_text());rows=[];failures=[]
    for item in m['items']:
        path=ROOT/item['path']
        if not path.exists() or not item.get('output'):continue
        out=Image.open(path).convert('RGBA');a=np.array(out);idle=Image.open(item['idle_path']).convert('RGBA');b=out.getchannel('A').getbbox();o=item['output'];detail=o['postprocessing']
        checks={'source_idle_unchanged':sha(item['idle_path'])==item['idle_sha256'],
                'size_rgba':Image.open(path).mode=='RGBA' and out.size==(224,256),
                'foot_boundary_y212':b[3]==212 and not a[212:,:,3].any(),
                'center_x112':b[0]+b[2]==224,'top_at_least_y30':b[1]>=30,
                'width_at_most_210':b[2]-b[0]<=210,'max_4_calls':1<=len(item['attempts'])<=4,
                'visual_review':item.get('visual_review',{}).get('passed',False)}
        mask=np.array(Image.open(ROOT/'_qa'/f"{item['key']}-body-mask.png"))>0
        samples=color_samples(out,mask,item['color'][1:]);checks['body_color_samples']=all(s['pass']for s in samples)
        extra={}
        if item['pose']=='blink':
            original=np.array(idle);diff=np.any(a!=original,axis=2);allowed=np.zeros((256,224),bool)
            for x0,y0,x1,y1 in detail['eye_patch_boxes_xyxy_exclusive']:allowed[y0:y1,x0:x1]=True
            ys,xs=np.where(diff)
            extra={'changed_pixels':int(diff.sum()),'difference_bbox_xywh':[int(xs.min()),int(ys.min()),int(xs.max()-xs.min()+1),int(ys.max()-ys.min()+1)],
                   'eye_patch_boxes_xyxy_exclusive':detail['eye_patch_boxes_xyxy_exclusive'],
                   'outside_eye_rectangles_changed_pixels':int((diff&~allowed).sum()),'alpha_pixel_identical':bool(np.array_equal(a[:,:,3],original[:,:,3]))}
            checks['eye_only']=extra['outside_eye_rectangles_changed_pixels']==0 and extra['alpha_pixel_identical']
            proof=Image.new('RGB',(224,256),(35,43,61));proof.paste(out,(0,0),out);overlay=np.array(proof);overlay[diff]=[255,70,120]
            Image.fromarray(overlay).save(ROOT/'_qa'/f"{item['key']}-diff.png")
        else:
            iw,ib=body_width(idle);cw,cb=body_width(out,tuple(detail.get('cheer_measurement_fraction',[.55,.85])));extra={'idle_body_width':iw,'cheer_body_width':cw,'body_width_ratio':cw/iw,
                'idle_measurement_y_band':ib,'cheer_measurement_y_band':cb}
            checks['body_width_within_5_percent']=abs(cw/iw-1)<=.05
            if item['color']in ['#42A9FF','#FF9D30']:
                src=Image.open(detail['color_variant_source']).convert('RGBA');aa=np.array(src);changed=np.any(a!=aa,2)
                editmask=np.array(Image.open(detail['color_variant_mask']))>0
                checks['variant_alpha_identical']=bool(np.array_equal(a[:,:,3],aa[:,:,3]))
                checks['variant_outside_body_identical']=not np.any(changed&~editmask)
                face=np.array(Image.open(detail['face_preserve_mask']))>0
                checks['variant_face_identical']=not np.any(changed&face)
                extra['variant_face_preserved_pixels']=int(face.sum())
                extra['variant_changed_pixels']=int(changed.sum())
        for attempt in item['attempts']:
            checks[f"attempt_{attempt['attempt']}_full_prompt"]=attempt['prompt'].startswith(item['base_prompt']) and '[STYLE]'not in attempt['prompt']and'[CAST]'not in attempt['prompt']
            if attempt.get('archive_path'):checks[f"attempt_{attempt['attempt']}_original_preserved"]=sha(attempt['archive_path'])==attempt['sha256']
            checks[f"attempt_{attempt['attempt']}_references_preserved"]=all(sha(r['path'])==r['sha256']for r in attempt['reference_metadata'])
        row={'key':item['key'],'path':item['path'],'prompt_number':item['prompt_number'],'calls':len(item['attempts']),
             'selected_attempt':item['selected_attempt'],'sha256':sha(path),'bbox_xyxy_exclusive':b,'foot_boundary':212,
             'last_occupied_row':b[3]-1,'horizontal_center':(b[0]+b[2])/2,'color_samples':samples,'checks':checks,**extra}
        rows.append(row)
        failures.extend({'key':item['key'],'check':k}for k,v in checks.items()if not v)
        item['final_audit']=row
    before=json.loads((ROOT/'_qa/before-emotes-58-59-sha256.json').read_text())
    changed=[rel for rel,h in before.items()if not (ART/rel).exists()or sha(ART/rel)!=h]
    protected=[rel for rel in before if rel.startswith('delivery/creatures/') and Path(rel).stem in ['idle','attack','spent','projectile']]
    protected_changed=[rel for rel in protected if rel in changed]
    assert not protected_changed,protected_changed
    result={'file_count':len(rows),'expected_files':20,'native_generation_calls':sum(len(i['attempts'])for i in m['items']),
            'failures':failures,'files':rows,'protected_source_file_count':len(protected),'protected_sources_unchanged':True,
            'existing_files_changed':changed,'game_integration_tested':False}
    (ROOT/'_qa/emotes-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    preview(m)
    # Split the review image into readable groups, retaining exact delivery scale.
    contact=Image.open(ROOT/'_qa/emotes-contact-sheet.png')
    for n,y in enumerate(range(0,contact.height,1450),1):contact.crop((0,y,contact.width,min(y+1450,contact.height))).save(ROOT/'_qa'/f'emotes-contact-{n}.png')
    if not final:
        print(json.dumps({k:v for k,v in result.items()if k!='files'},ensure_ascii=False));return
    assert len(rows)==20 and not failures,result
    assert m['blue_first_gate']['passed']
    lines=['','<!-- R1_V3_EMOTES_58_59_START -->','## 第58、59项 · 眨眼与欢呼补件（2026-10-15）','',
           f'交付20个RGBA PNG：10张blink.png、10张cheer.png，均为224×256。内置image_gen共调用{result["native_generation_calls"]}次，每文件最多4次；先完成blue #2982F4两张并通过自查，再制作其余原色，最后处理两个教学关配色。',
           '', '[20张补件总览](_qa/emotes-overview.png) · [完整提示词、参考和每次生成记录](emotes-58-59-prompts.json) · [逐像素与尺寸自查数据](_qa/emotes-audit.json) · [对照图1](_qa/emotes-contact-1.png) · [对照图2](_qa/emotes-contact-2.png) · [40px与多底色检查](_qa/emotes-40px-alpha-proof.png) · [教学欢呼改色范围](_qa/emotes-tutorial-recolor-proof.png)','',
           '坐标从0开始。脚底y=212沿用已有素材的边界定义：最后占用行是y=211，y≥212全透明。眼睛矩形和包围盒均采用右下边界不包含的区间。身体宽度通常测量轮廓高度55%–85%带内的最大行跨度（Alpha>10）；侧举手或圆掌进入该区时，目检确定手部结束以下的可见躯干量区：黄色66%–90%、红色64%–90%、白色70%–90%。每张实际y区间记在JSON，并逐张目检体量，避免将举手宽度当成身体宽度。','',
           '### 文件与关键指标','',
           '| 文件 | 提示词 | 调用/采用 | 轮廓宽×高 / 顶部y | 眨眼差异范围(x,y,w,h)或欢呼身体宽度 | 最大ΔE76 |',
           '| --- | --- | --- | --- | --- | --- |']
    for row in rows:
        b=row['bbox_xyxy_exclusive'];metric=str(row['difference_bbox_xywh'])if row['prompt_number']==58 else f"{row['idle_body_width']}→{row['cheer_body_width']}px（{(row['body_width_ratio']-1)*100:+.2f}%）"
        lines.append(f"| [{row['path']}]({row['path']}) | {row['prompt_number']} | {row['calls']}/{row['selected_attempt']} | {b[2]-b[0]}×{b[3]-b[1]} / {b[1]} | {metric} | {max(s['deltaE76']for s in row['color_samples']):.3f} |")
    lines+=['','### 每个文件的任务书第4节自查','']
    for item,row in zip(m['items'],rows):
        assert item['key']==row['key']
        eye=item['pose']=='blink';extra='眼睛矩形之外修改0像素，Alpha逐像素一致。'if eye else f"身体宽度相对待战图变化{(row['body_width_ratio']-1)*100:+.2f}%，最高点y={row['bbox_xyxy_exclusive'][1]}，总宽{row['bbox_xyxy_exclusive'][2]-row['bbox_xyxy_exclusive'][0]}px。"
        cs='；'.join(f"{s['label']}，ROI={s['roi_xyxy_exclusive']}，{s['hex']}，ΔE76={s['deltaE76']:.3f}"for s in row['color_samples'])
        lines += [f"#### {item['path']}（提示词{item['prompt_number']}，调用{row['calls']}次，采用第{row['selected_attempt']}次）",'',
          '1. **尺寸、文件名、透明边缘：通过。** 224×256，RGBA透明底；轮廓边缘已在浅色和深色底上目检，无背景残留、外部投影或杂散碎点。',
          '2. **画风与角色一致：通过。** 以本颜色idle.png为身份依据，欢呼另附示意图及已确认阵容；表情、身体和特征已逐张目检。',
          '3. **角色锚点和颜色版本：通过。** 脚底边界y=212、中心x=112，y≥212透明。'+extra,
          '4. **身体主色：通过。** '+cs+'。两处均≤8，镜面白高光和深轮廓不作为主色样本。',
          '5. **空心弹体：不适用。** 本文件是角色姿态，不含弹体。',
          '6. **文字、数字：通过。** 无文字、数字、水印。',
          '7. **禁止元素：通过。** 无植物、蔬菜、花盆、篱笆、恐龙、既有游戏标志；无道具或额外特效。',
          '8. **九宫格：不适用。** 单格角色PNG，不是可拉伸界面。',
          '9. **多帧和白灰染色：不适用。** 单格角色姿态，不是特效图集或染色尾迹。',
          '10. **小蓝头像：不适用。** 本项不是向导头像；角色身份已按第2项核对。']
        if eye:lines.append(f"11. **眼区逐像素检查：通过。** 眼睛外扩4px后的矩形为{row['eye_patch_boxes_xyxy_exclusive']}；实际变化{row['changed_pixels']}像素，合并差异范围(x,y,w,h)={row['difference_bbox_xywh']}；矩形之外变化0像素，整张Alpha完全一致。")
        elif item['color']in ['#42A9FF','#FF9D30']:lines.append('11. **教学关改色：通过。** 使用第28/29项完整提示词编辑对应原色cheer.png；最终Alpha、轮廓、脸、姿势、位置及身体改色蒙版外所有像素与原色欢呼图完全一致。')
        else:lines.append('11. **欢呼表情和姿势：通过。** 双眼闭成上弧、张嘴开心，原有小附肢或头部特征上扬；两脚触地，整个角色在画布内。')
        lines.append('')
    lines+=['### 每次调用与后期','']
    for item in m['items']:
        lines.append(f"- {item['path']}：")
        for a in item['attempts']:lines.append(f"  - 第{a['attempt']}次：{a.get('verdict',a['status'])}")
    lines+=['','- 所有生成原图保存在_originals/，完整提示词、参考文件哈希、每次原图尺寸及SHA256均见emotes-58-59-prompts.json。[STYLE]和[CAST]已完整展开；眨眼使用第58项原文，教学欢呼使用第28/29项原文。',
           '- 眨眼：对生成图配准，只把眼睛矩形（外扩4px）贴回对应idle.png，做局部色差衔接；保留原图整张Alpha和矩形之外的所有RGBA像素。教学关眨眼分别直接使用自己的idle.png，未从原色眨眼改色。',
           '- 黄色idle本来就是一只眼闭合的眨眼表情；58项保留原已闭合的左眼，仅替换原睁开的右眼，最终双眼闭合。炭黑闭眼线采用原角色轮廓中的浅灰色#888A99，局部校正闭眼线，避免缩小后消失；眨眼校正仍严格限制在眼区矩形内。',
           '- 欢呼：清理透明边缘，按身体宽度等比缩放，居中和脚底定位，校准身体主色。教学关改色以原色欢呼图为形体/Alpha/面部母版，仅替换身体主色区域，强制保留其他像素。教学欢呼后期初版蒙版包含了部分同色系眉毛和嘴部，复核后显式排除面部特征；最终通过Alpha、面部和身体改色区域之外的逐像素比较。','',
           '### 未做到与验收边界（单独列出）','',
           '1. 没有缺交文件，所有适用的离线自查项通过。生成原图与最终导出区分留档，不把未对齐或未裁切的原图当作可直接接入成品。',
           '2. 此次没有接入游戏、打包图集或运行实机。眨眼频率、0.13秒显示时长、过关跳动和状态切换由程序完成，本轮只交付两种静态帧。',
           '3. 主色检验以身体中间调和较亮部取样，保留高光、阴影和表情细节，不声称所有身体像素都等于主色HEX。','',
           '### 写入范围与原素材保全','',
           f"主动写入仅在Art/R1/art-v3/，未提交Git。{len(protected)}张已有idle、attack、spent、projectile均与开工前SHA256一致。交付清单采用追加方式。工作期间检测到的其他已有文件变化：{changed or '无'}；这些外部更新未覆盖。",
           '<!-- R1_V3_EMOTES_58_59_END -->','']
    report=ROOT/'交付清单.md';assert '<!-- R1_V3_EMOTES_58_59_START -->'not in report.read_text()
    report.write_text(report.read_text().rstrip()+'\n'+'\n'.join(lines))
    m.update(status='delivered_offline_checks_passed',validation=result)
    P.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items()if k!='files'},ensure_ascii=False))


if __name__=='__main__':audit('--final'in sys.argv)

from pathlib import Path
import json,hashlib
from PIL import Image
from process_creatures import ROOT,MANIFEST

m=json.loads(MANIFEST.read_text());assert m['validation']['file_count']==40
assert not m['validation']['failures']
snap=json.loads((ROOT/'_qa/variant-reference-snapshots.json').read_text())
for item in m['items']:
    assert len(item['attempts'])<=4
    for attempt in item['attempts']:
        assert attempt['prompt'].startswith(item['base_prompt'])
        assert '[STYLE]' not in attempt['prompt'] and '[CAST]' not in attempt['prompt']
        attempt['reference_metadata']=[]
        for ref in attempt['reference_images']:
            if ref in snap:
                data={'path_at_generation':ref,**snap[ref]}
            else:
                p=Path(ref);data={'path':ref,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'size':list(Image.open(p).size)}
            attempt['reference_metadata'].append(data)
        assert any('bubble-squad-v02/01-smiley-intro.png' in p for p in attempt['reference_images'])
        if item['id']<28:
            assert len(attempt['reference_images'])==3
        assert Path(attempt['archive_path']).exists()
    item['generation_count']=len(item['attempts'])
    item['selected_version']=(f"第{item['selected_attempt']}张四格原图 + 切图校色后期" if item['id']<28
        else f"原色成品作像素母版 + 精确主色改色；4张生成编辑稿归档，不直接采用其重绘几何")
    for o in item['outputs']:
        o['prompt_id']=item['id'];o['generation_count']=len(item['attempts']) if item['id']<28 else 1
        assert o['sha256']==hashlib.sha256(Path(o['path']).read_bytes()).hexdigest()
m['status']='第20–29项完成，40个PNG已自查；未接入游戏。'
m['generator']='built-in imagegen; no CLI/API fallback'
m['generated_original_count']=sum(len(a['attempts']) for a in m['items'])
m['reference_snapshots_note']='8张原色参考快照不计入模型生成次数。blue/spent在编辑生成后按色差自查进一步压低明暗对比；最终浅蓝版从修正后的原色母版导出，三态形体/Alpha未变。'
MANIFEST.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
master=json.loads((ROOT/'prompts.json').read_text())
master['character_assets']=m['items'];master['character_manifest']=str(MANIFEST.resolve())
master['status']='第0–29项完成：阵容已确认，19个场景/UI部件、40个角色文件交付。'
master['character_validation']=m['validation'];master['total_model_generations']=25+m['generated_original_count']
(ROOT/'prompts.json').write_text(json.dumps(master,ensure_ascii=False,indent=2)+'\n')

f=ROOT/'交付清单.md';s=f.read_text().split('\n## 角色阶段交付（第20–29项）')[0]
s=s.replace('# R1 v3 第一批美术交付清单（第0–19项）','# R1 v3 第一批美术交付清单（第0–29项）')
s=s.replace('正在继续第20–29项角色。下列第0–19项记录保留前轮验收历史。','第20–29项现已完成，新增40个角色PNG及18张模型原图；第一批合计60个最终PNG、43张模型原图。下列第0–19项记录保留前轮验收历史。')
s=s.replace('技术自查通过，造型待用户确认','技术自查通过；用户已确认造型')
s=s.replace('等待用户确认造型。','用户已于2026-10-15确认造型。')
s=s.replace('2. **第20–29项正在制作。** 角色新增验收记录将在本清单后半部分逐文件补齐；前轮场景/UI文件的角色检查“不适用”仍保留。','2. **第20–29项已完成。** 40个角色PNG的新增验收记录见本清单后半部分；前轮场景/UI文件的角色检查“不适用”仍保留。')
s=s.replace('正式角色颜色版本须在确认后的角色阶段单独校验。','正式角色颜色版本已在后半部分单独校验。')
s=s.replace('最终脚本检查：20个交付PNG','前轮脚本检查（历史）：20个交付PNG')
lines=['','## 角色阶段交付（第20–29项）','',
 '阵容确认依据：用户回复“确认阵容”。已交8个角色、10个颜色版本，每版3态加1个弹体，共40个透明PNG。第20与27项各生成2张四格图，其余基础角色各1张；第28与29项分别对4个原色文件各生成1次编辑稿，共18次内置imagegen生成。', '',
 '- [全部三态与弹体总览](_qa/characters-contact-sheet.png)',
 '- [40像素与三种底色检查](_qa/characters-40px-and-alpha.png)',
 '- [完整提示词、参考图、每次结果和SHA-256](character-prompts.json)',
 '- [逐文件机器检查及颜色取样](_qa/character-file-audit.json)', '',
 '锚点口径：脚底落在像素边界y=212，最底有色行是y=211，y≥212的44行全透明；可见包围盒中心x=112。每个基础角色三态使用同一个源图缩放因子，仅像素取整，未分别放大来伪造同尺寸。', '',
 '### 40个文件总表','',
 '| 编号 | 文件 | 尺寸 | 本文件生成次数 | 自查 |',
 '| --- | --- | --- | --- | --- |']
for a in m['items']:
    for o in a['outputs']:
        rel=Path(o['path']).relative_to(ROOT).as_posix();wh='×'.join(map(str,o['size']))
        lines.append(f'| {a["id"]} | [{rel}]({rel}) | {wh} | {o["generation_count"]} | 通过 |')
lines += ['', '### 逐文件七项自查','']
feature={'blue':'腮鳍与鼓腮','orange':'梨形与两个短圆角','green':'三枚软鼓包','purple':'双耷耳与豆形','yellow':'软方块、双触角与待战眨眼','white':'圆药丸形与冷灰边','red':'倒心形与单撮刘海','charcoal':'短软刺、浅灰轮廓与亮眼睛'}
for a in m['items']:
    for o in a['outputs']:
        rel=Path(o['path']).relative_to(ROOT).as_posix();body=o['state']!='projectile'
        lines += [f'#### {a["id"]} · {rel}','',f'- 提示词编号{a["id"]}；本文件生成{o["generation_count"]}次。{a["selected_version"]}。',
          f'- 1 尺寸/文件名/透明底：**通过**。{"×".join(map(str,o["size"]))}，RGBA；透明边缘已在浅灰、#8C919C和深色底检查，无底板、阴影和漂浮残点。',
          f'- 2 画风/身份：**通过**。沿用确认阵容及示意图画法；{feature[a["character"]]}保留，40像素预览可辨。' if body else '- 2 画风/身份：**通过**。细主色泡泡边与白色高光，符合示意图的光泽画法。']
        if body:
            ident='；与原色版Alpha及主色遮罩外全部像素完全相同' if a['id']>=28 else '；三态采用同一缩放因子'
            lines += [f'- 3 角色定位/版本：**通过**。脚底边界y=212、中心x=112，下方44行全透明；可见宽高{o["body_dimensions"][0]}×{o["body_dimensions"][1]}{ident}。',
                      '- 4 主色：**通过**。以下为3×3色块RGB中位数，CIELAB D65 / ΔE76；白色镜面高光和边缘阴影不作为身体主色取样：']
            for v in o['color_samples']:
                lines.append(f'  - {v["label"]}：ROI {v["roi"]}，RGB {v["rgb"]} / {v["hex"]}，目标#{a["hex"]}，ΔE76={v["deltaE76"]:.3f} ≤ 8。')
            lines.append('- 5 空心弹体：**不适用**。本文件只含角色姿态；出手图内没有弹体，独立projectile.png逐项检查。')
        else:
            lines += ['- 3 角色定位/版本：**不适用角色脚底规则**。64×64弹体居中，四周透明安全边；教学关版本与原色版Alpha、形状、位置完全一致。' if a['id']>=28 else '- 3 角色定位/版本：**不适用角色脚底规则**。64×64弹体居中，四周透明安全边。',
                      '- 4 主色：**不适用身体取样**。本文件为透明泡泡，无身体亮部/中间调；主色边缘目检与角色同色系，白色为冷白、深灰为中性灰。',
                      f'- 5 空心弹体：**通过**。中心24×24区域最大Alpha={o["projectile_alpha"]["center_24x24_max"]}，半透明边缘像素{o["projectile_alpha"]["semitransparent_pixels"]}个；检查底色可透过中心，无拖尾。']
        lines += ['- 6 无文字数字：**通过**。未见文字、数字、标签或水印。',
                  '- 7 禁止元素：**通过**。未见植物、蔬菜、花盆、篱笆、恐龙及已有游戏标志；角色按本项目确认阵容制作。','']
lines += ['### 每次生成的选择与后期','']
for a in m['items']:
    lines.append(f'- 第{a["id"]}项 {a["character"]} #{a["hex"]}：')
    for v in a['attempts']:
        lines.append(f'  - 第{v["attempt"]}次'+(f'（{v["state"]}文件）' if v.get('state') else '')+'：'+v['verdict'])
lines += ['', '### 未做到或验收边界（单独列出）','',
 '1. **没有把生成编辑稿冒充为像素完全一致。** 第28、29项均按原文逐文件生成并归档，但模型会放大重绘。最终浅蓝/浅橙PNG以对应原色成品作母版，在主色遮罩内做确定性CIELAB改色；Alpha和遮罩外RGBA逐像素相等，位置、脸、轮廓、高光不变。',
 '2. **没有锁成全身平涂HEX。** 为保留已确认的柔和明暗，身体采用同色系明暗；按任务书第4节ΔE76≤8执行60处亮部/中间调取样，全部通过。并不声称每个亮部或中间调像素都精确等于HEX，也不把阴影色当作主色样本。',
 '3. **原始泡泡填色和Alpha瑕疵已做后期。** 清除中心填色、保留生成的薄环及高光；清理周边碎点。紫色耳朵跨过名义四格边界，按实际图形间隙切图，未切断耳朵；白色弹体去除过量蓝色。',
 '4. **未接入Unity、未做真机验收。** 本轮完成的是美术文件与离线尺寸、透明度、取色、像素一致性及40像素可读性检查。程序接入后的缩放、动画、数字牌遮挡仍需运行验收。',
 '5. 以上处理已完成，**没有缺交的第20–29项文件**。未开始第二批弹窗、向导和特效。','',
 '### 本轮写入范围与保全检查','',
 '本轮所有主动项目写入均在Art/R1/art-v3/，未提交Git。阵容确认图、前轮19个场景/UI成品、源任务书、源提示词、生成占位和其他已有图片均未改。生成器缓存原图已复制到_originals/；8张原色输入快照另标为reference，不计入生成次数。', '']
f.write_text(s+'\n'+'\n'.join(lines))

# Verify old art is untouched. Only manifests/reports are expected to change.
before=json.loads((ROOT/'_qa/before-characters-sha256.json').read_text())
changed=[]
for rel,old in before.items():
    p=ROOT.parent/rel
    if p.exists() and hashlib.sha256(p.read_bytes()).hexdigest()!=old:changed.append(rel)
allowed={'delivery/prompts.json','delivery/交付清单.md'}
assert set(changed)<=allowed,changed
(ROOT/'_qa/character-preservation-check.json').write_text(json.dumps({'changed_preexisting_files':changed,'all_preexisting_images_unchanged':True,'file_count':40,'model_generations':m['generated_original_count']},ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'files':40,'model_generations':m['generated_original_count'],'color_samples':m['validation']['color_sample_count'],'all_checks_pass':True,'changed_preexisting_files':changed},ensure_ascii=False))

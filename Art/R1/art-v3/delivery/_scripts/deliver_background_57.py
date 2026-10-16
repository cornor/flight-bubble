"""Archive superseded assets and append item 57's measured delivery record."""
from pathlib import Path
import json
import re
import shutil
from PIL import Image
from check_background_57 import sha, check

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT.parent
manifest=ROOT/'background-57-prompts.json'
m=json.loads(manifest.read_text())
q=json.loads((ROOT/'_qa/background-57-checks.json').read_text())
post=json.loads((ROOT/'_qa/background-57-postprocess.json').read_text())
candidate=ROOT/'_qa/background-57-candidate.png'
target=ROOT/'scene/background.png'
stage=ROOT/'scene/stage.png'
stage_archive=ROOT/'_originals/stage-56.png'
report=ROOT/'交付清单.md'
marker='<!-- R1_V3_BACKGROUND_57_START -->'
assert marker not in report.read_text()
assert all(q['checks'].values()),q['checks']
assert sha(candidate)==q['sha256']
assert sha(target)==m['backup_sha256']==sha(m['backup_path'])
assert sha(stage)==m['stage_source_sha256']
assert not stage_archive.exists()
assert len(m['attempts'])==3 and len(m['attempts'])<=m['max_generations']
source=(ART/'美术AI提示词.md').read_text()
style=re.search(r'## 通用画风段.*?```text\n(.*?)\n```',source,re.S).group(1)
body=re.search(r'### 57\..*?```text\n(.*?)\n```',source,re.S).group(1)
assert body.replace('[STYLE]',style)==m['base_prompt']
for r in m['reference_metadata']:
    assert sha(r['path'])==r['sha256']
for t in m['attempts']:
    assert t['prompt'].startswith(m['base_prompt']) and '[STYLE]' not in t['prompt']
    assert t['reference_images']==m['reference_images'] and len(t['reference_images'])==4
    assert sha(t['archive_path'])==t['sha256']
    t['reference_metadata']=m['reference_metadata']
m['attempts'][2]['status']='selected_finished_and_passed'
m['attempts'][2]['verdict']='选中并完成后期：原生边饰已缩小且保持圆润；微调顶部与托板上沿位置，校准天空和灰蓝面板，淡化边云并清理保护区；最终尺寸、取色和留白检查通过。底云和泡泡没有横向压缩。'

before=json.loads((ROOT/'_qa/before-background-57-sha256.json').read_text())
external=[rel for rel,h in before.items() if not (ART/rel).exists() or sha(ART/rel)!=h]
assert set(external)<= {'美术AI提示词.md','一次性出图指令.md','generated/atlas-sources.json'},external

# User explicitly requests replacement first, then relocation of the old stage.
shutil.copyfile(candidate,target)
stage.rename(stage_archive)
assert sha(target)==q['sha256']
assert sha(stage_archive)==m['stage_source_sha256'] and not stage.exists()
q=check(target)
q.update(selected_attempt=3,generation_call_count=3,generated_image_count=3,
         visual_checks_passed=True,visual_review='Reviewed final full image and protected-area overlay.',
         background_backup={'path':m['backup_path'],'sha256':sha(m['backup_path']),'byte_identical':True},
         retired_stage={'path':str(stage_archive),'sha256':sha(stage_archive),'byte_identical':True,'source_absent':True},
         previous_file_count=len(before),external_changes_observed_before_delivery=external)

checks=[
('尺寸、文件名、不透明度','通过','scene/background.png，1080×1920，RGB PNG，整张不透明；背景是任务书透明底要求的例外。'),
('画风与示意图一致','通过','采用云端泡泡世界的柔和明暗和明亮高光；天空、城堡、彩虹、边云和托板来自同一次完整背景生成，无角色。'),
('角色脚底、三态、颜色版本','不适用','本项是背景，不含角色；无脚底行、动作帧或角色颜色版本。'),
('角色身体主色ΔE76','不适用','无角色身体。背景另取中线四点和旧图逐点比较，ΔE76均为0；结果见下表。'),
('空心半透明弹体','不适用','不是弹体；侧边彩色小泡泡是背景装饰，不交付为战斗弹体。'),
('文字和数字','通过','没有文字、数字、水印；没有预画角色、出战位、玻璃管、按钮、泡泡阵或护栏。'),
('禁止元素','通过','未见植物、蔬菜、花盆、篱笆、恐龙、既有游戏角色或标志。'),
('九宫格边框与拉伸','不适用','固定1080×1920全屏背景，不是九宫格界面部件。'),
('多帧、灰度染色特效','不适用','单张彩色背景，没有帧切分、帧数或白灰染色要求。'),
('小蓝头像身份','不适用','本图不含小蓝头像或其他角色。'),
('泡泡阵留白区','通过','检查矩形[81,184,999,1095)，覆盖中间85%宽、9.6%–57%高；目检无云、图案、小岛、瀑布。逐行RGB完全一致，纵向相邻像素各通道最大变化1/255，只有平缓纵向蓝色渐变。'),
('天空颜色与明暗','通过','四个指定高度的中线像素与background-v1.png完全一致；9.6%处采用旧图实际#56AAFE，与表中约值#5CAEFD的ΔE76为3.2888，其余三点也与表中值一致。'),
('托板留白区','通过','检查矩形[108,1190,972,1863)，覆盖中间80%宽、62%–97%高；无云、泡泡、彩虹、图案、文字或界面。逐行RGB完全一致，纵向相邻通道最大变化1/255。上部#B4C8E0，下部#9DB4D2。'),
('上下分区与过渡','通过','小城堡、彩虹和顶部云团收在顶部约9%；托板微白上沿位于57%–60%过渡带，60%以下为完整托板；天空和托板是一张不透明画面，无另贴旧托板。'),
('边缘装饰与形体','通过','白、浅蓝云沿左右及底边；粉、蓝、黄、紫小泡泡和角部小彩虹保留。第三版原生缩小边饰，最终未作横向压缩，泡泡和底云保持圆润，保护区内没有装饰。'),
('提示词、四张参考、次数','通过','第57项完整原文和[STYLE]整段均保留；每次都附四张指定参考的稳定副本，共调用3次、生成3张，未超4次。原图和完整提示词均留档。'),
('原背景备份','通过','动手前已备份到_originals/background-v1.png；SHA256与覆盖前背景一致，校色始终以该副本为准。'),
('托板归档','通过','覆盖新背景后，把scene/stage.png移动到_originals/stage-56.png，文件内容哈希不变，原交付路径已不存在。前节第56项记录为历史记录，以本节归档路径为准。'),
]
m.update(status='delivered_offline_checks_passed',selected_version=3,generation_count=3,generation_call_count=3,
         output_path=str(target),output_size=[1080,1920],output_mode='RGB',sha256=sha(target),
         postprocessing=post,technical_check=q,
         self_check=[{'item':i,'name':n,'result':s,'reason':r}for i,(n,s,r)in enumerate(checks,1)])
manifest.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
(ROOT/'_qa/background-57-checks.json').write_text(json.dumps(q,ensure_ascii=False,indent=2)+'\n')

lines=['',marker,'## 第57项 · 完整背景重画（2026-10-15，取代第1项背景和第56项托板）','',
'交付：[scene/background.png](scene/background.png)，1080×1920，不透明RGB PNG。上半部天空和下半部托板已绘在同一张图中。内置image_gen共生成3次，采用第3版并完成后期；最终离线自查通过。',
'',
'旧背景已预先备份为[_originals/background-v1.png](_originals/background-v1.png)。旧托板已从scene/stage.png移至[_originals/stage-56.png](_originals/stage-56.png)，不再单独交付托板。前节第56项的成品路径为历史记录，以本节归档位置为准。','',
'[完整提示词与每次生成记录](background-57-prompts.json) · [成品预览](_qa/background-57-preview.png) · [保护区检查图](_qa/background-57-protected-areas.png) · [像素与归档检查数据](_qa/background-57-checks.json)','',
'检查图中的红框是天空保护区，绿框是托板保护区，黄线是60%高度；这些辅助线不在交付PNG中。','',
'### 逐项自查','']
for i,(n,s,r)in enumerate(checks,1):lines.append(f'{i}. **{n}：{s}。** {r}')
lines+=['','### 天空取色（坐标从0开始）','',
'| 高度 / 坐标 | 提示词表中约值 | 覆盖前背景 | 最终背景 | ΔE76相对旧图 |',
'| --- | --- | --- | --- | --- |']
for height,s in zip(['9.6% / (540,184)','30% / (540,576)','50% / (540,960)','57% / (540,1094)'],q['sky_samples']):
    lines.append(f"| {height} | {s['table_hex']} | {s['original_hex']} | {s['actual_hex']} | {s['delta_e76_to_original']:.4f} |")
lines+=['','9.6%处的旧图实际值与提示词表内近似值略有差异。本次优先遵循“上半部天空的颜色和明暗必须保持”，保留旧图实测值；没有改动实际发送的提示词色值。','',
'### 每次生成结果','']
for t in m['attempts']:lines.append(f"- 第{t['attempt']}次：{t['verdict']} 原图：[_originals/57-background-attempt-{t['attempt']:02d}.png](_originals/57-background-attempt-{t['attempt']:02d}.png)。")
lines+=['','### 后期与归档','',
'- 原图均为941×1672，已保留未经处理的PNG。选中图精确缩放到1080×1920，微调顶部云团和托板上沿的纵向位置；最终没有横向压缩边饰，底部云朵保持原有比例。',
'- 按旧背景中线取色校准天空；清理两个保护区内的残余纹理及装饰侵入，羽化只放在保护区之外。面板核心统一为#B4C8E0至#9DB4D2的平缓灰蓝渐变。',
'- 装饰画面来自第三次完整生成；没有把旧背景或第56项托板的图形贴入最终背景。57-reference-*.png和background-v1.png是稳定参考，不计入生成次数。',
f"- 最终背景SHA256：{sha(target)}。",
f"- background-v1.png SHA256：{sha(m['backup_path'])}。",
f"- stage-56.png SHA256：{sha(stage_archive)}，与移动前stage.png相同。",'',
'### 未做到与验收边界（单独列出）','',
'1. 没有缺交文件或适用自查未通过项。第1、2版未采用，原因已逐次列出；不把未处理的生成原图当作合格成品。',
'2. 原生生成尺寸不是1080×1920，已按任务书由美术侧完成精确尺寸后期；原图不冒充最终交付图。',
'3. 本轮没有修改程序、重新打包或运行游戏。已按要求移走单独托板，但运行中的旧图集或旧构建是否仍叠加旧托板，需要程序接入后验证；未声称已完成实机替换。','',
'### 写入范围与共享文件保全','',
f'主动项目写入均在Art/R1/art-v3/，没有提交Git。开始前记录{len(before)}个已有文件的SHA256；本次主动变更的已有文件仅为scene/background.png、移动scene/stage.png，以及追加交付清单。生成期间观察到美术AI提示词.md、一次性出图指令.md、generated/atlas-sources.json被外部更新，均未覆盖；重新核对[STYLE]与第57项原文仍与本次完整提示词一致。其余原有素材未改。',
'<!-- R1_V3_BACKGROUND_57_END -->','']
report.write_text(report.read_text().rstrip()+'\n'+'\n'.join(lines))
print(json.dumps({'delivered':str(target),'sha256':sha(target),'selected_attempt':3,'calls':3,
                  'stage_archived':str(stage_archive),'report_appended':True},ensure_ascii=False))

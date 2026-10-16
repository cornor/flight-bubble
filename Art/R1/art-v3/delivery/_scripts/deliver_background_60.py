"""Deliver the reviewed background and append its complete offline QA record."""
from pathlib import Path
import json,hashlib,shutil,re

ROOT=Path(__file__).resolve().parents[1];ART=ROOT.parent
P=ROOT/'background-60-prompts.json'


def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()


m=json.loads(P.read_text());selected=m['attempts'][3]
assert sha(ROOT/'scene/background.png')==m['backup']['sha256']
assert sha(m['backup']['path'])==m['backup']['sha256']
text=(ART/'美术AI提示词.md').read_text()
style=re.search(r'## 通用画风段.*?```text\n(.*?)\n```',text,re.S).group(1)
base=re.search(r'### 60\..*?```text\n(.*?)\n```',text,re.S).group(1).replace('[STYLE]',style)
assert base==m['base_prompt']
reference_changes=[]
for ref in m['reference_metadata']:
    current=sha(ref['path'])
    if current!=ref['sha256']:
        reference_changes.append({'path':ref['path'],'at_start':ref['sha256'],'at_delivery':current})
assert all('/Builds/R1-evidence/play-v3/3-2.png'in x['path']for x in reference_changes)
m['reference_verification']={'metadata_capture':'task start; paths attached to every successful request',
    'externally_changed_during_task':reference_changes,
    'note':'The gameplay screenshot changed externally during generation. Re-inspected at delivery: UI positions unchanged; no rewrite of the external file. Actual bytes read by each native call cannot be reconstructed from the current file.'}
for a in m['attempts']:
    if a.get('archive_path'):assert sha(a['archive_path'])==a['native_sha256']
visual={'sky_clear':True,'islands_and_waterfalls':True,'distant_cottages':True,'upward_haze':True,
        'cloud_wrapped_stage_no_frame':True,'stage_clear':True,'castle_and_rainbow':True,
        'style':True,'no_ui_or_text':True,'one_small_star':True}
selected.update(status='selected_after_postprocessing',visual_checks=visual,
    verdict='采用第3张成功出图（第4次工具请求）。完成连续分区缩放和托板轻度调色后，尺寸、景物、留白、云边、取色与整体目检均通过。',
    raw_layout_review='原图造型通过，但浮岛后方云顶仍略高，左下云尖接近留白区；最终用连续布局重采样收进指定区域，没有以平面色块覆盖景物。')
(ROOT/'_qa/background-60-attempt-04-raw-checks.json').write_text(json.dumps(selected['measurements'],ensure_ascii=False,indent=2)+'\n')
shutil.copy2(ROOT/'_qa/background-60-candidate.png',ROOT/'scene/background.png')
shutil.copy2(ROOT/'_qa/background-60-attempt-04-regions.png',ROOT/'_qa/background-60-protected-areas.png')
shutil.copy2(ROOT/'_qa/background-60-attempt-04-preview.png',ROOT/'_qa/background-60-preview.png')
check=json.loads((ROOT/'_qa/background-60-checks.json').read_text())
check.update(path=str(ROOT/'scene/background.png'),visual_checks=visual,backup_matches_pre_task=True,
    reference_verification=m['reference_verification'],ceiling_baked_in=False)
check.pop('visual_review_pending',None)
check['sky_max_deltaE76_to_v1']=max(s['deltaE76_to_v1']for s in check['samples'][:3])
check['stage_max_deltaE76_to_target']=max(s['deltaE76_to_target']for s in check['samples'][3:])
assert all(check['checks'].values())and all(visual.values())
(ROOT/'_qa/background-60-checks.json').write_text(json.dumps(check,ensure_ascii=False,indent=2)+'\n')
m.update(status='delivered_background_offline_checks_passed',selected_attempt=4,selected_generation=3,
         tool_requests=4,generated_images=3,validation=check,
         output={'path':str(ROOT/'scene/background.png'),'size':[1080,1920],'mode':'RGB','sha256':sha(ROOT/'scene/background.png')})
lines=['','<!-- R1_V3_BACKGROUND_60_START -->','## 第60项 · 浮岛带与云包边完整背景（2026-10-15）','',
 '已覆盖 `scene/background.png`：1080×1920、RGB、不透明。开工前将第57项成品逐字节备份到 `_originals/background-57.png`。内置image_gen请求4次，其中3次成功出图、1次在出图前因参考数量限制被拒绝；采用第4次请求（第3张原图），未超过最多4次生成。','',
 '[成品](scene/background.png) · [示意图并排对比](_qa/background-60-comparison.png) · [叠加现有云顶棚的对比](_qa/background-60-comparison-with-ceiling.png) · [顶部放大](_qa/background-60-top-with-ceiling.png) · [留白边界](_qa/background-60-protected-areas.png) · [完整提示词及每次结果](background-60-prompts.json) · [检查数据](_qa/background-60-checks.json) · [后期参数](_qa/background-60-postprocess.json)','',
 '### 第60项逐项自查','',
 '1. **尺寸、不透明、路径：通过。** 1080×1920 RGB；转为RGBA核验，Alpha全部255。',
 '2. **泡泡阵留白：通过。** x=81–998、y=184–902（中央85%宽、9.6%–47%高）无云、浮岛、房子和图案；保留柔和天空渐变及原画细微色阶。该区域最大相邻像素通道变化3。',
 '3. **浮岛、瀑布、远处房子：通过。** 47%–59%带内，左右各一座带瀑布、草地、小树、小房子的浮岛，中间数座朦胧小浮岛与蘑菇顶房子；向上变淡、向下接入云层。主要清楚细节位于两侧与带内下半段。',
 '4. **托板云包边：通过。** 白/浅蓝云自然包住灰蓝托板四周，无硬边方框、直角或面板外框。保留左下短彩虹、少量光泽泡泡及左侧一颗小星星；按留白要求收窄外围装饰。',
 '5. **托板留白：通过。** x=108–971、y=1190–1862（中央80%宽、62%–97%高）没有云、装饰或界面图案；保留原画柔和光照。最大相邻像素通道变化4。',
 f"6. **天空、城堡和彩虹：通过。** 天空三个样点对v1最大ΔE76为{check['sky_max_deltaE76_to_v1']:.3f}；保留右上城堡与彩虹设计。属于同设计重绘，不声称装饰逐像素相同。",
 '7. **并排对比：通过。** 保存纯背景对比和额外的现有云顶棚叠加预览。成品无角色、泡泡阵、护栏、出战位、管道、按钮、文字或水印。','',
 '### 取色记录','',
 '各点取5×5区域RGB中值。天空对照v1和第60项色值；托板对照第60项近似色值。','',
 '| 区域 / 像素坐标 | 成品 | 目标 | ΔE76对目标 | v1 | ΔE76对v1 |',
 '| --- | --- | --- | --- | --- | --- |']
for k,s in enumerate(check['samples']):
    v1='#'+''.join(f'{v:02X}'for v in s['v1_rgb'])
    lines.append(f"| {'天空'if k<3 else'托板'} {s['xy']} | {s['hex']} | {s['target_hex']} | {s['deltaE76_to_target']:.3f} | {v1 if k<3 else'不适用'} | {s['deltaE76_to_v1']if k<3 else'不适用'} |")
lines+=['','### 任务书第4节自查','',
 '1. **尺寸、文件名、底色：通过。** 背景为允许的不透明项，边缘无透明缝。',
 '2. **画风：通过。** 与示意图的柔和卡通云海、浮岛、光泽泡泡材质一致；本文件不含角色。',
 '3. **角色锚点、三态：不适用。** 没有角色。',
 '4. **角色身体主色：不适用。** 背景色取样另列上表。',
 '5. **空心弹体：不适用。** 没有弹体。',
 '6. **文字数字：通过。** 成品没有文字、数字、水印；仅自查图有说明文字。',
 '7. **禁止元素：通过。** 没有植物角色、蔬菜、花盆、篱笆、恐龙或既有游戏标志；浮岛草地和小树为第60项明确要求。',
 '8. **九宫格：不适用。** 整屏背景。',
 '9. **多帧与白灰特效：不适用。** 不是特效图集。',
 '10. **小蓝头像：不适用。** 不含头像。','',
 '### 每次请求结果','']
for a in m['attempts']:
    lines.append(f"- 请求{a['attempt']}：{a.get('verdict',a['status'])}")
    if a.get('archive_path'):
        name=Path(a['archive_path']).name;lines.append(f'  - 原图：[_originals/{name}](_originals/{name})。')
lines+=['','三张成功出图均附五张指定参考，[STYLE]与第60项原文完整展开。三张生成原图保留未加工文件与SHA256，调用完整提示词和追加定位说明均在JSON中。',
 '后期保留AI原绘画，连续重采样调整天空/浮岛的高度分配、收紧侧云和底部云边，并对灰蓝托板轻度调色。没有以程序图形代画岛屿或云，没有用纯色矩形遮盖景物。原图和成品分别留档；映射与调色参数见后期JSON。','',
 '### 顶部云顶棚（按用户最终确认保持独立）','',
 '云顶棚是已有独立素材 `scene/ceiling.png`，当前程序以540×960逻辑坐标 x=0、y=76、w=540、h=40 叠加；离线预览换算为1080×1920坐标 x=0、y=152、w=1080、h=80。用户确认它是后加层后，明确要求忽略顶部疑问并继续原计划。因此本次没有重画顶棚、改程序位置或把它烘焙到背景；叠加对比仅帮助查看最终层次。','',
 '### 未做到与验收边界（单独列出）','',
 '1. 没有缺交的第60项背景文件，指定背景区域自查均通过。',
 '2. 本轮未接入游戏、重打图集或运行实机；带云顶棚的图片是按现有布局合成的离线预览。',
 '3. 不声称背景与参考逐像素一致；为满足两块留白区，浮岛高度及外围云边经过布局缩放。','',
 '### 保全、参考更新与写入范围','',
 f"- 第57项备份SHA256：`{m['backup']['sha256']}`。",
 f"- 第60项成品SHA256：`{m['output']['sha256']}`。",
 '- 生成期间实机截图 `Builds/R1-evidence/play-v3/3-2.png` 被外部更新；交付时重新查看，界面覆盖位置一致，未写入或恢复该外部文件。JSON记录开工与交付两个哈希，无法追溯工具各次读取瞬间的截图字节。其余四张指定参考未变。',
 '- 主动项目写入仅在Art/R1/art-v3/；既有文件仅覆盖背景、追加交付清单。现有云顶棚和其他素材未修改；没有提交Git。',
 '<!-- R1_V3_BACKGROUND_60_END -->','']
report=ROOT/'交付清单.md';old=report.read_text();assert '<!-- R1_V3_BACKGROUND_60_START -->'not in old
report.write_text(old.rstrip()+'\n'+'\n'.join(lines))
P.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
before=json.loads((ROOT/'_qa/before-background-60-sha256.json').read_text())
changed=[rel for rel,h in before.items()if not(ART/rel).exists()or sha(ART/rel)!=h]
assert set(changed)=={'delivery/scene/background.png','delivery/交付清单.md'},changed
print(json.dumps({'delivered':m['output'],'successful_generations':3,'tool_requests':4,'backup_verified':True,
                  'existing_files_changed':changed,'all_background_checks_passed':True},ensure_ascii=False))

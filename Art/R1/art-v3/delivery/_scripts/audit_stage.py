"""Record the stage supplement's concrete checks without touching other assets."""
from pathlib import Path
import json,hashlib
import numpy as np
from PIL import Image
from process_assets import meta

ROOT=Path(__file__).resolve().parents[1]
p=ROOT/'stage-prompts.json';m=json.loads(p.read_text())
im=Image.open(ROOT/'scene/stage.png');a=np.array(im);c=a[39:680,108:972]
assert im.mode=='RGBA'and im.size==(1080,772)
assert not a[:24,:,3].any()
assert (c[:,:,3]==230).all()
assert (c[:,:,:3].min(2)>232).sum()==0
assert np.ptp(c[:,:,:3],axis=1).max()<=1
assert a[39,540,:3].tolist()==[180,200,224]
assert a[679,540,:3].tolist()==[157,180,210]
assert len(m['attempts'])<=4
generated=[x for x in m['attempts']if x.get('archive_path')]
assert len(generated)==2
for t in m['attempts']:
    assert t['prompt'].startswith(m['base_prompt'])
    assert '[STYLE]'not in t['prompt']
    assert len(t['reference_images'])==3
    if t.get('archive_path'):assert hashlib.sha256(Path(t['archive_path']).read_bytes()).hexdigest()==t['sha256']
    t['reference_metadata']=m['reference_metadata']if t['attempt']==1 else m['reference_snapshots']if t['attempt']==3 else []
    if t['attempt']==3:t['verdict']='选中并完成后期：云朵、泡泡和彩虹保留；边饰压入外侧，中央磨砂面统一明暗与色值，顶部24px清空；指定留白区及尺寸检查通过。'
before=json.loads((ROOT/'_qa/before-stage-sha256.json').read_text())
changed=[rel for rel,h in before.items()if not (ROOT.parent/rel).exists()or hashlib.sha256((ROOT.parent/rel).read_bytes()).hexdigest()!=h]
assert not changed,changed
checks=[
('尺寸、文件名和透明边缘','通过','scene/stage.png，1080×772，RGBA。原生Alpha清理后以预乘Alpha缩放；无不透明底板、黑白残边、杂散碎点或外部投影。'),
('画风与参考','通过','使用所列三张参考；云朵、圆泡泡、轻微玻璃高光沿用示意图，灰蓝底面与交付背景叠加已目检。实机截图仅作为布局参考。'),
('角色脚底、三态和颜色版本','不适用','此文件是场景托板，不含角色；不套用y=212脚底和三态规则。'),
('角色身体主色ΔE76','不适用','无角色身体。托板颜色另按第56项检验：上部(540,39)为#B4C8E0，下部(540,679)为#9DB4D2。'),
('空心半透明弹体','不适用','此文件不是弹体；磨砂面统一Alpha=230/255，属于轻微半透明面板。'),
('文字和数字','通过','不含文字、数字、水印，也未预画出战位、角色、玻璃管或按钮。'),
('禁止元素','通过','未见植物、蔬菜、花盆、篱笆、恐龙、既有游戏标志；没有旧版本的星形装饰和绿色泡泡。'),
('九宫格','不适用','本文件按1080×772整图铺放；不是九宫格部件，未改变使用方式。'),
('多帧特效和可染色灰度','不适用','本文件为单张彩色场景层，没有帧切分或程序染色要求。'),
('小蓝头像身份','不适用','此文件无向导或角色。'),
('中央80%宽、5%–88%高留白','通过','保护区为x=108–971、y=39–679（右下边界采用排他区间[108,39,972,680)）。目检无云、泡泡、彩虹、图案和文字；同一行RGB任一通道的最大差值仅1/255，只有平缓纵向渐变；保护区Alpha恒为230。'),
('顶部3%透明','通过','顶部24行（y=0–23）全部RGBA=0，覆盖3%×772=23.16px；微白顶沿位于保护区上方。'),
('边饰分布和底色','通过','白/浅蓝云仅沿左右及底部，粉、蓝、黄、紫圆泡泡位于边缘，一个小彩虹收在左下角；颜色与中央留白已按最终图检查。')
]
technical={
'file':meta(ROOT/'scene/stage.png'),'prompt_number':56,
'generation_call_count':len(m['attempts']),'generated_image_count':len(generated),
'failed_calls_without_image':1,'selected_call':3,
'protected_rect_xyxy_exclusive':[108,39,972,680],
'protected_alpha_min':int(c[:,:,3].min()),'protected_alpha_max':int(c[:,:,3].max()),
'protected_max_row_channel_range':int(np.ptp(c[:,:,:3],axis=1).max()),
'protected_max_row_channel_std':float(c[:,:,:3].std(axis=1).max()),
'protected_bright_cloud_pixel_count':int((c[:,:,:3].min(2)>232).sum()),
'top_transparent_rows':24,'top_band_nonzero_alpha_pixels':int((a[:24,:,3]>0).sum()),
'panel_color_samples':[{'xy':[540,39],'rgba':a[39,540].tolist(),'target':'#B4C8E0'},{'xy':[540,679],'rgba':a[679,540].tolist(),'target':'#9DB4D2'}],
'previous_file_count':len(before),'previous_changed_files_before_append':changed,
'game_integration_tested':False}
m.update(status='delivered_offline_checks_passed',file_metadata=technical['file'],generation_count=len(generated),generation_call_count=len(m['attempts']),self_check=[{'item':i+1,'name':n,'result':s,'reason':r}for i,(n,s,r)in enumerate(checks)],technical_check=technical)
p.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
(ROOT/'_qa/stage-checks.json').write_text(json.dumps(technical,ensure_ascii=False,indent=2)+'\n')
lines=['','<!-- R1_V3_STAGE_56_START -->','## 第56项补件 · 下半部托板（2026-10-15）','',
'交付：[scene/stage.png](scene/stage.png)，1080×772，RGBA透明底。提示词编号56；内置image_gen共调用3次，其中1次因参考截图正在重建而未产图，实际生成2张，采用第3次调用的图。含失败调用也没有超过最多4次的限制。','',
'[STYLE]已替换为源文件中的完整画风段；第56项原文逐字保留，布局修正追加在后。完整提示词、参考图信息、每次结果、后期步骤和SHA256见[stage-prompts.json](stage-prompts.json)。','',
'- [透明底预览](_qa/stage-checker.png)',
'- [留白边界校验](_qa/stage-protected-area.png)（红框仅在检查图中，不在交付PNG里）',
'- [叠加现有背景的预览](_qa/stage-on-background.png)（离线合成，非实机截图）',
'- [尺寸、透明度和颜色检查数据](_qa/stage-checks.json)','',
'### 逐项自查','']
for i,(name,result,reason)in enumerate(checks,1):lines.append(f'{i}. **{name}：{result}。** {reason}')
lines+=['','### 每次调用结果','']
for t in m['attempts']:lines.append(f'- 第{t["attempt"]}次：{t["verdict"]}')
lines+=['','### 原图与后期','',
'- 原图完整保存为_originals/56-stage-attempt-01.png和_originals/56-stage-attempt-03.png。第2次没有生成原图，不虚构文件。三个56-reference-*.png是稳定参考快照，不计入生成数量。',
'- 处理内容：透明边缘清理、空边裁切、精确缩放，顶部24px透明带；把生成图两侧边饰收进外侧104px；对灰蓝磨砂面统一明暗和透明度，使中央留白没有装饰和显著纹理。云朵、泡泡、彩虹与高光来自选中的AI生成图。',
'- 面板上部目标#B4C8E0，下部目标#9DB4D2；中央Alpha230。显示在不同背景上时会因半透明混合略有不同。',
'',
'### 未做到与验收边界（单独列出）','',
'1. 没有缺交文件或适用自查未通过项。',
'2. 生成原图的角部装饰仍越过留白边界，已在后期收紧边饰并消除中间面板的杂乱明暗；交付文件通过留白区检查，不将原图直接当成合格成品。',
'3. 本任务没有执行游戏接入或打包；背景叠加图仅作离线检查，角色、玻璃管、数字、按钮的最终运行叠加需程序接入后验收。',
'4. 实机参考截图在工作期间曾短暂消失并更新；第1次使用初始版本，第3次使用同一路径恢复后的版本及稳定快照，两版哈希均记录。没有修改Builds目录。',
'',
'### 写入范围','',
f'本任务写入均在Art/R1/art-v3/，没有提交Git。追加本节前，对开始时记录的{len(before)}个已有文件做SHA256比对，未发现已有文件变更。本次只新增托板及其原图、参考快照、提示词记录、检查图和处理脚本，并追加本交付清单。',
'<!-- R1_V3_STAGE_56_END -->','']
report=ROOT/'交付清单.md';text=report.read_text();assert '<!-- R1_V3_STAGE_56_START -->'not in text
report.write_text(text.rstrip()+'\n'+'\n'.join(lines))
print(json.dumps({'path':str((ROOT/'scene/stage.png').resolve()),'size':list(im.size),'calls':len(m['attempts']),'generated_images':len(generated),'central_area':'pass','top_24px_alpha':'pass','sha256':technical['file']['sha256']},ensure_ascii=False))


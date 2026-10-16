"""Write item-by-item production QA and provenance for items 0–19 only."""
from pathlib import Path
import hashlib,json,re
import numpy as np
from PIL import Image
from process_assets import meta
R=Path(__file__).resolve().parents[1];ROOT=R.parent
P=json.loads((R/'prompts.json').read_text())
C=json.loads((R/'_qa/lineup-color-samples.json').read_text())
ALL_SAMPLES=[s for c in C for s in c['samples']]
assert all(s['delta_e76']<=8 for s in ALL_SAMPLES)
names=['尺寸/路径/透明边缘','画风与角色身份','角色画布/三态/颜色版本','主色ΔE76≤8','空心半透明弹体','文字与数字','禁用元素与原创']
specific={
0:'8个角色两行：蓝橙绿紫 / 黄白红深灰；绿色3鼓包、紫色双耷耳、黄色触角、深灰软刺和浅描边可辨。每行脚底已对齐，40像素高预览已查看。等待用户确认造型。',
1:'中央平静天空；城堡和彩虹在右上；仅两侧保留草岛及小瀑布，后期把岛屿带移到58–71.5%高度；无UI、角色、台面或文字。',
2:'厚泡沫云带，白到浅蓝、下沿蓬松。清理原图透明噪点，左右端点做预乘Alpha过渡；首尾像素一致。',
3:'浅青白发光泡沫链，保持透明柔光。左右端点连续，贴图外无不透明底板；柔光为该项明确要求。',
4:'圆形蓝泡按钮，中央两条白色圆头竖线；无额外文字。',
5:'选第2次的深蓝灰细边空胶囊；保留圆头，中央空白，后期Alpha乘0.86形成半透明底。',
6:'空进度条框，蓝色柔亮边与深色凹槽；无填充、百分比。与填充条按38%/100%裁切组合检查过。',
7:'绿色亮面条，圆头保形，中段可横向裁切。去除首轮原图多余外发光后导出。',
8:'浅蓝泡圈台面，斜上方扁椭圆，中心浅色；宽高比2:1。',
9:'使用第9项生成的虚线锁泡；下半部复用第8项的同尺寸台面几何并压暗去饱和，位于x0/y70、140×70；锁泡位于上方。',
10:'选第2次：正俯视圆形金色细漩涡、中心亮、少量圆光点。后期降低浓雾Alpha，边缘柔和衰减至0；已查看压扁到0.45贴台面的效果。',
11:'选第2次深海军蓝空白数字牌，细浅蓝边；两端各18像素保形，中段可拉伸。',
12:'空白金色圆币，有凸起边沿和高光；没有烘焙Ⅰ/Ⅱ等文字。',
13:'三个圆头尖端的金色小皇冠，尖端有泡珠；无MAX文字。',
14:'空玻璃管，侧边反光和底环保留；上下装饰收在32像素边框内，中央Alpha中位数约27/255。已查看192×360及九宫格288×540效果。',
15:'左上朝向的等长L形折角；所有可见像素RGB均为255,255,255，保留抗锯齿Alpha，供程序着色。',
16:'蓝胶囊、左侧交叉箭头、右侧“重排”；在310×144及155×72下逐字查看。',
17:'黄橙胶囊、左侧白色双箭头、右侧“优先出战”；在310×144及155×72下逐字查看。',
18:'紫胶囊、左侧爆泡图标、右侧“泡泡炸弹”；在310×144及155×72下逐字查看。',
19:'深海军蓝圆角标，白边与小高光；中心空白，无数量。'
}
first_fail={
0:'未选：绿色背部多于3个鼓包，橙色角偏长偏尖；主色需校准。',
1:'未选：中间多画了浮岛链及额外房屋/树木，不符合中间留空。',
5:'未选：外框过亮、偏电蓝，和安静的深蓝灰关卡牌不一致。',
10:'未选：黄色雾团偏密，外侧大星芒过多。',
11:'未选：青色外圈过亮、过厚，接近按钮而非小数字牌。'
}
qa_data=[]
for a in P['assets']:
 i=a['id'];path=Path(a['output_path']);m=meta(path);a['file_metadata']=m
 if i>0:assert [m['width'],m['height']]==a['requested_size']
 if i in [0,1]:assert m['mode']=='RGB'
 else:assert m['mode']=='RGBA' and m['alpha_min']==0
 selected=next(x for x in a['attempts'] if x['attempt']==a['selected_attempt'])
 a['prompt']=selected['prompt'];a['attempt_count']=len(a['attempts']);a['selected_version']=Path(selected['archive_path']).name
 assert 1<=len(a['attempts'])<=4
 for at in a['attempts']:
  assert at['prompt'].startswith(a['base_prompt'])
  assert at['referenced_image_paths']==a['referenced_image_paths']
  assert hashlib.sha256(Path(at['archive_path']).read_bytes()).hexdigest()==at['source_metadata']['sha256']
  at['review']={'result':'选中，已完成后期自查' if at['attempt']==a['selected_attempt'] else '未选中',
                'reason':specific[i] if at['attempt']==a['selected_attempt'] else first_fail.get(i,'后续版本更符合要求')}
 c1=('通过',f"{m['width']}×{m['height']}；路径正确。"+('第0项专门要求浅灰底，采用RGB横图，底色#EEF1F5。' if i==0 else '背景按要求不透明RGB。' if i==1 else 'RGBA，存在完全透明像素；在棋盘格和灰底查看边缘，无残留矩形底。'+('保留本项指定的柔光。' if i in [3,10,14] else '非效果部件已清理多余外发光/透明碎点。')))
 c2=('通过',specific[i])
 c3=('不适用','本项为阵容确认图，不是224×256单株；两行脚底分别对齐至y454/y880。三态、y212锚点和两种颜色版本须在确认后第20–29项另做。') if i==0 else ('不适用','本项无角色，角色画布、三态和颜色版本不适用；本轮未生成20–29项。')
 c4=('通过',f"8角色各2处身体区域，共16处9×9中位取样；最大ΔE76={max(s['delta_e76'] for s in ALL_SAMPLES):.3f}≤8，坐标和色值见下表。") if i==0 else ('通过','所有Alpha>0像素的RGB均为#FFFFFF；纯白折角已脚本核对。') if i==15 else ('不适用','本项是场景/UI，不是角色身体；任务书未给其角色主色取样门槛，不用背景或高光冒充身体取样。')
 c5=('不适用','本轮没有角色projectile.png；本项不是弹体，未把该检查记成已验证的弹体通过。')
 if i in [16,17,18]:
  label={16:'重排',17:'优先出战',18:'泡泡炸弹'}[i]
  c6=('通过',f'仅有指定固定按钮名“{label}”，逐字核对；无数字或其他文字。')
 else:c6=('通过','没有文字、字母、数字、关卡名、百分比或等级字样。暂停/锁头/皇冠等是图形符号。')
 if i==1:
  c7=('通过（按本项具体场景要求）','背景保留任务书/提示词明确要求的草岛、城堡和彩虹、边角装饰泡；未出现植物角色、蔬菜、花盆、篱笆或恐龙。未见可识别的已有游戏角色或标志。草岛例外与文字冲突说明见末节。')
 else:c7=('通过','未见植物、蔬菜、花盆、篱笆、恐龙，未见可识别的已有游戏角色或标志；素材沿用项目自有参考形象。')
 a['checks']=[{'item':j+1,'name':names[j],'result':v[0],'reason':v[1]} for j,v in enumerate([c1,c2,c3,c4,c5,c6,c7])]
 a['status']='awaiting_user_lineup_confirmation' if i==0 else 'delivered_after_postprocess'
 qa_data.append({'id':i,'output_path':str(path),'metadata':m,'checks':a['checks']})
# Direct assertions, independent of visual judgments.
for i in [2,3]:
 ar=np.array(Image.open(P['assets'][i]['output_path']).convert('RGBA'));assert np.array_equal(ar[:,0],ar[:,-1])
ar=np.array(Image.open(P['assets'][15]['output_path']).convert('RGBA'));assert np.all(ar[ar[:,:,3]>0,:3]==255)
ar=np.array(Image.open(P['assets'][10]['output_path']).convert('RGBA'));assert not any([ar[0,:,3].max(),ar[-1,:,3].max(),ar[:,0,3].max(),ar[:,-1,3].max()])
assert not (R/'creatures').exists()
for path,digest in P['sources'].items():assert hashlib.sha256(Path(path).read_bytes()).hexdigest()==digest,path
for rel,digest in P['existing_art_v3_hashes'].items():assert hashlib.sha256((ROOT/rel).read_bytes()).hexdigest()==digest,rel
for a in P['assets']:
 for path,digest in a['reference_sha256'].items():assert hashlib.sha256(Path(path).read_bytes()).hexdigest()==digest,path
P['status']='items_0_to_19_delivered_awaiting_lineup_approval'
P['validation']={'final_files':20,'scene_and_ui_files':19,'original_generation_files':sum(len(a['attempts']) for a in P['assets']),
                 'all_delivery_sizes_exact':True,'rgba_sprite_files':18,'lineup_gray_background_exception':True,
                 'all_original_inputs_unchanged':True,'every_prompt_preserves_expanded_base_verbatim':True,
                 'all_reference_paths_and_hashes_verified':True,'max_delta_e76_lineup_samples':max(s['delta_e76'] for s in ALL_SAMPLES),
                 'queue_nine_slice_proof_viewed':True,'characters_40px_proof_viewed':True,'items_20_to_29_generated':False,'git_commit_performed':False}
(R/'prompts.json').write_text(json.dumps(P,ensure_ascii=False,indent=2)+'\n')
(R/'_qa/file-audit.json').write_text(json.dumps(qa_data,ensure_ascii=False,indent=2)+'\n')

md=['# R1 v3 第一批美术交付清单（第0–19项）','','日期：2026-10-15','',
'本轮交付阵容确认图1张、场景3张、界面部件16张，共20个最终PNG；共生成25张原图。第0项等待用户确认，第20–29项未开始。','',
'第1–19项已按指定路径与尺寸完成透明底/裁切/缩放等后期。以下逐文件覆盖任务书第4节全部7项；不适用的检查说明原因，没有当作已经做过的角色/弹体验收。','',
'## 入口与记录','',
'- [阵容确认图](../lineup/01-lineup.png)',
'- [全部素材总览](_qa/contact-sheet.png)',
'- [40像素角色轮廓检查](_qa/lineup-40px.png)',
'- [玻璃管九宫格、台面漩涡、按钮实际尺寸检查](_qa/ui-proof.png)',
'- [完整提示词及生成记录](prompts.json)：[STYLE]/[CAST]已整段展开，保留25次实际完整提示词、参考图、选中版本、尺寸、SHA-256、后期操作。',
'- [_originals/](_originals/)：生成器原始文件，未做裁切/缩放/调色。',
'- [_qa/file-audit.json](_qa/file-audit.json)：逐文件尺寸、RGBA、透明像素与检查结论。','',
'## 文件总表','',
'| 提示词编号 | 文件 | 最终尺寸 | 生成次数 | 选中原图 | 自查结论 |',
'| --- | --- | --- | --- | --- | --- |']
for a in P['assets']:
 rel='../lineup/01-lineup.png' if a['id']==0 else a['relative_path']
 m=a['file_metadata'];conclusion='技术自查通过，造型待用户确认' if a['id']==0 else '适用检查通过；见下方逐项说明'
 md.append(f"| {a['id']} | [{a['relative_path']}]({rel}) | {m['width']}×{m['height']} | {a['attempt_count']} | 第{a['selected_attempt']}次 | {conclusion} |")
md+=['','## 每个文件的7项自查','']
for a in P['assets']:
 md += [f"### {a['id']}. {a['title']}",'',f"文件：`{a['relative_path']}`；提示词编号{a['id']}；生成{a['attempt_count']}次，选第{a['selected_attempt']}次。",'',
        '| 自查项 | 结论 | 原因/证据 |','| --- | --- | --- |']
 for c in a['checks']:md.append(f"| {c['item']}. {c['name']} | {c['result']} | {c['reason']} |")
 md += ['',f"SHA-256：`{a['file_metadata']['sha256']}`。",'']
md += ['## 阵容主色取样','',
'在最终1536×1024阵容图的角色身体上，每色取2个无遮挡9×9区域，用RGB中位数转CIELAB(D65)计算相对规定HEX的ΔE76；阈值8。下表保留亮面/中间调区域坐标，不用眼睛或白色高光替代主色。白色角色的一处白面为纯白。原图色值、校色前坐标和最终位置均保存在[_qa/lineup-color-samples.json](_qa/lineup-color-samples.json)。','',
'校色采用角色分区与身体色相遮罩，在CIELAB中校准调色板并保留明暗；保留白高光、五官与轮廓。颜色表证明所列区域达标，不把渐变/阴影说成全图逐像素等于HEX。','',
'| 角色 | 要求HEX | 最终取样矩形[x0,y0,x1,y1) | RGB中位数 | HEX | ΔE76 | 结果 |',
'| --- | --- | --- | --- | --- | --- | --- |']
for c in C:
 for s in c['samples']:md.append(f"| {c['character']} | {c['target_hex']} | {s['roi_xyxy']} | {s['rgb']} | {s['hex']} | {s['delta_e76']:.3f} | {s['result']} |")
md += ['','## 重出及后期记录','',
'| 项目 | 次数与结果 |','| --- | --- |']
for i in [0,1,5,10,11]:
 md.append(f"| {i} | 第1次：{first_fail[i]} 第2次选中：{specific[i]} |")
md += ['',
'其余项目各生成1次，问题均属于本次明确要求由美术AI完成的尺寸、透明边缘、发光强度或部件定位后期，处理后复查。后期迭代没有虚报为新的模型生成次数。','',
'- 原图由内置imagegen生成；每次都附本项目v02笑脸图。第0项另附v01云上乐园，第10项另附指定空位漩涡参考。',
'- 透明原图保留原始Alpha；非特效部件清理漂浮碎点与不应有的外发光，再用预乘Alpha重采样，避免白边/黑边。',
'- 空胶囊、进度条、数字牌采用圆头保形的三段缩放；数字牌两端各18像素。',
'- 锁定位的底座复用普通台面的几何，只压暗去饱和，位置固定在140×140画布的下半部；上半部使用独立生成的锁泡。',
'- 玻璃管装饰压入四边32像素，中央更透明；已查看192×360与288×540九宫格效果。该检查是离线PNG检查，尚未接入Unity。',
'- 金色漩涡保留原图细圈与圆光点，降低浓雾，加入透明安全边；文件为正俯视圆形，离线检查时再压扁到0.45。',
'- 阵容图平整为#EEF1F5底，角色各列居中、两行脚底分别对齐。背景的边缘岛屿通过连续纵向重采样移到指定高度带；没有在游戏工程绘制新元素。',
'',
'## 未完成、未覆盖与任务书冲突说明（单独列出）','',
'1. **第0项的造型尚未获得用户确认。** 本轮只交阵容图供确认，技术自查不能代替用户审美确认。',
'2. **第20–29项按用户指令未开始。** 因此224×256角色的y212脚底锚点、三态一致、双颜色版本像素一致、64×64空心弹体均未验收；在每个文件的对应自查项明确写“不适用”。',
'3. **背景与透明规则的具体例外：** 第0项明确要求浅灰底，故阵容图为不透明RGB；背景为不透明RGB，其余18个部件为透明RGBA。护栏、漩涡及玻璃管底环保留各项明确指定的柔光，不能同时按通用“无光晕”删除。',
'4. **背景提示词存在通用/具体要求冲突：** 通用段禁止植物，背景专用段却要求grassy sky-islands；开头说NO bubbles，后文又要求少量边缘/角落装饰泡。本轮按具体场景条目保留草岛和少量边角泡泡，移除首轮多画的中央浮岛链、树木与额外房屋。若把“没有植物/泡泡”解读为草岛和装饰泡也一律禁止，本背景不能同时满足那种字面口径；这里未隐瞒该例外。',
'5. **主色未做“每一个亮部/中间调像素都完全等于HEX”的平涂锁色。** 本轮为保留示意图画法，按第4节ΔE76≤8标准校色并记录16处抽样，全部通过；并未声称明暗渐变或所有像素与目标HEX完全相等。正式角色颜色版本须在确认后的角色阶段单独校验。',
'6. **未接入游戏、未做真机运行验收。** 本轮只交图片；底板#8C919C、泡泡阵、动态数字、文本、旋转/呼吸、按钮按下态均留给程序，不混入美术PNG。',
'',
'## 写入范围与验证','',
'所有主动写入的项目文件均在Art/R1/art-v3/；生成服务缓存按工具默认保存，原图已复制到delivery/_originals/。原有任务书、提示词、颜色校验文件、界面规范和3张参考图均逐一校验SHA-256未变。未修改其他项目目录，未提交Git。','',
'最终脚本检查：20个交付PNG、19个指定场景/UI路径、18个透明RGBA部件、全部交付尺寸精确、每项生成次数≤4、所有完整提示词保留原文展开前缀、参考图顺序与哈希正确、25张原图归档完整、两个横贯素材左右端点一致、纯白折角RGB正确、漩涡四边Alpha为0、未生成creatures/。','']
(R/'交付清单.md').write_text('\n'.join(md))
print(json.dumps(P['validation'],ensure_ascii=False))


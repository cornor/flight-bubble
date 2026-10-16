"""Audit finished R1 v3 batch-two files and append the required per-file delivery checks."""
from pathlib import Path
import json,hashlib
import numpy as np,cv2
from PIL import Image,ImageDraw
from process_second_batch import ROOT,MANIFEST,meta,contact

NOTES={
30:'白到极浅蓝空白玻璃面板；深蓝外缘，圆角完整。',
31:'蓝色胶囊标题条，中部无内容，端部少量小泡泡。',
32:'亮蓝主按钮，圆润高光，中间空白。',
33:'灰蓝次按钮，比主按钮更沉静，中间空白。',
34:'绿色胶囊轨道，白色圆钮在右。',
35:'灰色胶囊轨道，白色圆钮在左，与开状态同尺寸和形状。',
36:'竖向玻璃卡，上部至少70%留白，下部浅色名牌，无角色、文字。',
37:'约16束柔和金光与5颗小泡泡，中心发散，向外渐隐。',
38:'讲解：张口说话，右侧小鳍抬起；胸像朝右。',
39:'开心：弧形闭眼、笑口、鼓腮，2颗小星光。',
40:'着急：眉毛上扬、小张口、1滴汗。',
41:'得意：半闭眼、笑口，侧鳍叉腰。',
42:'白色圆角对话框，细蓝边，没有尾巴，内部空白。',
43:'金色圆润箭头，朝正下方，橙褐边缘与泡泡高光。',
44:'珊瑚红胶囊告急横幅，无字。',
45:'半透明深蓝胶囊提示条，无字。',
46:'竖直泡泡尾迹，上密下疏、向下淡出；中性灰可染色。',
47:'6帧从紧凑泡沫炸开，到泡泡散开，末帧淡出。',
48:'6帧金色光圈扩张，白金泡泡飞散，第2–3帧星光达到峰值，第5–6帧淡出。',
49:'竖直连续泡泡光流，上下边缘像素相等，可连续平铺；中性灰可染色。',
50:'4帧泡泡簇从小到大，末帧最亮；中性灰可染色。',
51:'横贯画布的白/浅蓝泡沫气浪，两侧与外缘柔和渐隐。',
52:'向上涌的白/浅青泡沫浪，上缘泡沫与泡泡，下部透明淡出。',
53:'单颗彩虹紫色肥皂泡，内含1颗星光，中心可透出底色；没有水壶或卫星泡泡。',
54:'8帧由大泡泡破裂到泡沫圈扩张、小泡泡散开，第8帧接近消失。',
55:'浅青虚线椭圆，宽高约1.6，线上5颗小泡泡，中部透明。'
}
CHECK_NAMES=['尺寸/文件名/透明边缘','画风/角色身份','角色脚底/三态/颜色版本','身体主色 ΔE76','空心半透明弹体','文字和数字','禁止元素','九宫格边距/拉伸/留白','特效帧数/等宽/灰度','小蓝身份与构图']
COMMON=[
str(ROOT.parents[3]/'Art/Concepts/bubble-squad-v02/01-smiley-intro.png'),
str(ROOT.parents[3]/'Builds/R1-evidence/play-v3-p2/3-2.png')]
TARGET=cv2.cvtColor(np.array([[[41,130,244]]],dtype='float32')/255,cv2.COLOR_RGB2LAB)[0,0]

def sample(a,x,y):
    rgb=a[y,x,:3];lab=cv2.cvtColor(rgb.reshape(1,1,3).astype('float32')/255,cv2.COLOR_RGB2LAB)[0,0]
    return {'xy':[x,y],'rgb':rgb.tolist(),'hex':'#'+''.join(f'{v:02X}'for v in rgb),'alpha':int(a[y,x,3]),'delta_e76':round(float(np.linalg.norm(lab-TARGET)),3)}

def audit(a):
    p=ROOT/a['path'];im=Image.open(p);ar=np.array(im);ident=a['id'];tech={}
    assert im.mode=='RGBA' and list(im.size)==a['requested_size'],(ident,'mode/size')
    assert ar[:,:,3].min()==0 and ar[:,:,3].max()>0,(ident,'alpha')
    assert len(a['attempts'])<=4 and 1<=len(a['attempts']),(ident,'attempt count')
    for attempt in a['attempts']:
        assert attempt['prompt'].startswith(a['base_prompt']),(ident,'base prompt not intact')
        assert '[STYLE]'not in attempt['prompt'] and '[FX]'not in attempt['prompt']
        assert all(f in attempt['reference_images']for f in COMMON),(ident,'common refs')
        for ref in a['reference_images']:assert ref in attempt['reference_images']
        original=Path(attempt['archive_path']);assert hashlib.sha256(original.read_bytes()).hexdigest()==attempt['sha256']
    tech['size_mode_name_alpha']='通过';tech['reference_and_prompt_integrity']='通过'
    tech['alpha_mean']=round(float(ar[:,:,3].mean()),4)
    if ident>=46:
        old=ROOT.parents[1]/'art-v2'/'delivery'/a['path'];old_im=Image.open(old)
        assert old_im.size==im.size
        tech['v2_same_size']=True;tech['v2_alpha_mean']=round(float(np.array(old_im.convert('RGBA'))[:,:,3].mean()),4)
    if ident in [46,49,50]:
        visible=ar[:,:,3]>0;rgb=ar[:,:,:3][visible]
        assert np.all(rgb[:,0]==rgb[:,1])and np.all(rgb[:,1]==rgb[:,2]),(ident,'not neutral')
        assert rgb.min()>=185,(ident,'not light gray')
        tech['grayscale']={'colored_visible_pixels':0,'minimum_rgb':int(rgb.min()),'maximum_rgb':int(rgb.max())}
    if ident==49:
        diff=np.abs(ar[0].astype(int)-ar[-1].astype(int));assert diff.max()==0
        tech['vertical_seam_max_channel_error']=0
    if a['frames']>1:
        n=a['frames'];fw=im.width//n;tech['frames']=[]
        for k in range(n):
            alpha=ar[:,k*fw:(k+1)*fw,3];ys,xs=np.where(alpha>2)
            bbox=[int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1)]
            delta=[(bbox[0]+bbox[2]-fw)/2,(bbox[1]+bbox[3]-im.height)/2]
            edges=int(max(alpha[0].max(),alpha[-1].max(),alpha[:,0].max(),alpha[:,-1].max()))
            assert edges==0 and max(map(abs,delta))<=1,(ident,k,'frame position',delta,edges)
            tech['frames'].append({'index':k+1,'size':[fw,im.height],'bbox_alpha_gt_2':bbox,'center_error_px':delta,'edge_alpha_max':edges,'alpha_mean':round(float(alpha.mean()),4)})
        if ident==50:assert all(tech['frames'][k]['alpha_mean']<tech['frames'][k+1]['alpha_mean']for k in range(n-1))
    if 38<=ident<=41:
        tech['body_color_samples']={'bright':sample(ar,60,80),'midtone':sample(ar,60,150)}
        assert all(v['delta_e76']<=8 for v in tech['body_color_samples'].values())
        tech['portrait_viewport']=[60,145,1254,1179];tech['portrait_position']=[8,23,224,194]
    if ident in [53,55]:
        h,w=ar.shape[:2];region=ar[h//2-12:h//2+12,w//2-12:w//2+12,3]
        tech['center_24x24_alpha']={'mean':round(float(region.mean()),3),'max':int(region.max())}
        assert region.mean()<60,(ident,'center too opaque')
    # Visual judgments were made on final checkerboard, dark-frame, and stretch proofs.
    c=[]
    c.append(('通过',f'{im.width}×{im.height} RGBA；文件名与任务书一致，原生Alpha经清理和预乘Alpha缩放，透明背景成立。'+('保留提示词要求的特效柔光，外部非预期底色已清除。'if ident>=46 or ident==37 else '浅灰与深色底检查未见底板或杂散残点。')))
    c.append(('通过',NOTES[ident]+' 圆润高光和色彩与两张公共参考匹配。'))
    c.append(('不适用','本文件为胸像，没有脚底、全身三态或第二颜色版本；按240×240共同视窗定位。'if 38<=ident<=41 else '本文件为界面或特效，不是224×256全身角色三态；不套用y=212脚底规则。'))
    if 38<=ident<=41:
        ss=tech['body_color_samples'];parts=[]
        for label,key in [('亮部','bright'),('中间调','midtone')]:
            v=ss[key];parts.append(f'{label} {tuple(v["xy"])}={v["hex"]}，ΔE76={v["delta_e76"]:.3f}')
        c.append(('通过','目标#2982F4；'+'；'.join(parts)+'。固定坐标取样，不取白色镜面高光或深色阴影。'))
    else:c.append(('不适用','本文件没有角色身体主色；按本项界面/特效配色检查，不将背景、光晕或白色高光作为身体取样。'))
    if ident==53:
        center=tech['center_24x24_alpha'];c.append(('通过',f'大泡泡是半透明空心材质，彩虹边和白高光保留；中心24×24平均Alpha={center["mean"]}、最大={center["max"]}，能透出底色。'))
    else:c.append(('不适用','本项不是第一批projectile弹体。'+('特效泡泡已保留透明和渐隐层次。'if ident>=46 else '界面面板和头像按其内容保留实心表面。')))
    c.append(('通过','目检无文字、数字、标签、水印；界面空白区域没有预烘焙文案。'))
    c.append(('通过','未见植物、蔬菜、花盆、篱笆、恐龙、旧版水壶或已有游戏角色和标志。'))
    if a['slice_margin']:
        margin=a['slice_margin'];sides='四边'if ident in[30,42]else'左右'
        c.append(('通过',f'{sides}{margin}px固定区容纳圆角端部；已按相同切片边距做离线拉伸预览，边框连续、中间无文字，可拉伸。见second-batch-nine-slice.png。'))
    else:c.append(('不适用','本项按原尺寸缩放或逐帧读取，不是九宫格素材。界面该留空的区域已留空。'))
    if ident>=46:
        text=f'{a["frames"]}帧，与v2一致；尺寸不增大。'
        if a['frames']>1:
            errors=[abs(v)for f in tech['frames']for v in f['center_error_px']]
            text+=f'每格{im.width//a["frames"]}×{im.height}，等宽单行；Alpha>2边界中心误差最大{max(errors)}px，各格四边Alpha=0，没有跨格。'
        if ident in[46,49,50]:text+=f'所有可见像素R=G=B，RGB范围{tech["grayscale"]["minimum_rgb"]}–{tech["grayscale"]["maximum_rgb"]}，无彩色像素。'
        if ident==49:text+='首行与末行RGBA逐像素相同，支持上下平铺。'
        if ident==50:text+='四格可见面积和平均Alpha依次增大，符合新提示词顺序。'
        c.append(('通过',text))
    else:c.append(('不适用','本项为单张界面PNG，不是多帧特效或程序染色光流。'))
    if 38<=ident<=41:c.append(('通过','对照蓝色idle确认同一蓝色圆身体、头顶小撮、双鼓腮和小鳍。四张使用相同源视窗、缩放、落点；镜头均朝右，表情不同，未变成全身像。'))
    else:c.append(('不适用','本文件不是小蓝头像。'))
    a['self_check']=[{'item':i+1,'name':CHECK_NAMES[i],'result':s,'reason':r}for i,(s,r)in enumerate(c)]
    a['technical_check']=tech;a['status']='delivered_offline_checks_passed';a['file_metadata']=meta(p)
    return tech

def previews(m):
    # The same finished files in a readable UI board and FX board.
    for lo,hi,name,cols,cw,ch in [(30,45,'second-batch-ui-preview.png',4,350,280),(46,55,'second-batch-fx-preview.png',2,650,260)]:
        items=[x for x in m['items']if lo<=x['id']<=hi];out=Image.new('RGB',(cols*cw,((len(items)+cols-1)//cols)*ch),'#EBF0F7');d=ImageDraw.Draw(out)
        for j,a in enumerate(items):
            x=j%cols*cw;y=j//cols*ch;d.text((x+14,y+10),str(a['id'])+' '+Path(a['path']).name+' '+str(a['requested_size'][0])+'x'+str(a['requested_size'][1]),fill='#24354C')
            for yy in range(y+34,y+ch-10,12):
                for xx in range(x+10,x+cw-10,12):
                    d.rectangle((xx,yy,min(xx+11,x+cw-11),min(yy+11,y+ch-11)),fill='#A7B4C7'if((xx-x)//12+(yy-y)//12)%2 else'#C5D0DF')
            im=Image.open(ROOT/a['path']);im.thumbnail((cw-40,ch-58),Image.Resampling.LANCZOS);out.paste(im,(x+(cw-im.width)//2,y+34+(ch-44-im.height)//2),im)
        out.save(ROOT/'_qa'/name)

def main():
    m=json.loads(MANIFEST.read_text());assert len(m['items'])==26
    metrics={}
    for a in m['items']:metrics[str(a['id'])]=audit(a)
    before=json.loads((ROOT/'_qa/before-second-batch-sha256.json').read_text());changed=[]
    for rel,digest in before.items():
        p=ROOT.parent/rel
        if not p.exists()or hashlib.sha256(p.read_bytes()).hexdigest()!=digest:changed.append(rel)
    # Shared-workspace edits observed during generation; this task did not write them.
    observed_external={'美术AI任务书.md','generated/atlas-sources.json','generated/program/fx__arrow.png'}
    assert set(changed)<=observed_external|{'delivery/交付清单.md'},changed
    atlas=json.loads((ROOT.parent/'generated/atlas-sources.json').read_text());stale=[]
    for a in m['items']:
        stem=Path(a['path']).stem
        keys=['ui/'+stem]if a['id']<46 else ['fx/'+stem+'/'+str(k)for k in range(a['frames'])]
        if any(atlas.get(k)!=a['file_metadata']['sha256']for k in keys):stale.append({'path':a['path'],'keys':keys,'current_sha256':a['file_metadata']['sha256'],'atlas_recorded_sha256':[atlas.get(k)for k in keys]})
    m['summary']={'delivered_files':26,'ui_files':16,'fx_files':10,'total_generations':sum(len(a['attempts'])for a in m['items']),'maximum_generations_for_one_item':max(len(a['attempts'])for a in m['items']),'all_offline_checks_passed':True,'game_integration_tested':False,'previous_files_snapshot_count':len(before),'observed_concurrent_changes':[p for p in changed if p in observed_external],'stale_generated_atlas_assets':stale}
    MANIFEST.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
    (ROOT/'_qa/second-batch-checks.json').write_text(json.dumps({'summary':m['summary'],'assets':metrics},ensure_ascii=False,indent=2)+'\n')
    previews(m);contact(m)
    lines=['','<!-- R1_V3_SECOND_BATCH_START -->','## 第二批交付 · 第30–55项（2026-10-15）','',
    '本轮按任务书第2节与第4节交付16张界面、10张特效，共26个RGBA PNG。所有最终尺寸、文件名和帧数与任务书一致，没有放大交付尺寸。第一批接入游戏后的截图已逐项作为参考。',
    '',
    f'共生成{m["summary"]["total_generations"]}次，单项最多{m["summary"]["maximum_generations_for_one_item"]}次，没有超过4次。所有生成原图逐次存于_originals/；完整提示词、参考图及其SHA256、生成次数、选中版本、后期处理和最终SHA256见[second-batch-prompts.json](second-batch-prompts.json)。[STYLE]、[FX]已整段展开，逐项原文提示词均保留在实际提示词开头；生产约束和重出修正追加在后。',
    '',
    '- [界面预览](_qa/second-batch-ui-preview.png)',
    '- [特效预览](_qa/second-batch-fx-preview.png)',
    '- [多帧展开检查](_qa/second-batch-frames.png)',
    '- [九宫格拉伸检查](_qa/second-batch-nine-slice.png)',
    '- [机器检查数据](_qa/second-batch-checks.json)',
    '',
    '### 文件与次数','',
    '| 提示词编号 | 文件 | 尺寸 | 帧数 | 生成次数 | 选中 | 自查结论 |',
    '| --- | --- | --- | --- | --- | --- | --- |']
    for a in m['items']:
        lines.append(f'| {a["id"]} | [{a["path"]}]({a["path"]}) | {a["requested_size"][0]}×{a["requested_size"][1]} | {a["frames"]} | {len(a["attempts"])} | 第{a["selected_attempt"]}次 | 适用项通过；不适用项逐条说明 |')
    lines+=['','### 按任务书第4节逐文件自查','',
    '以下每个文件均列出全部10项。“不适用”明确给出原因；本批头像为胸像，因此不套用第一批全身角色的脚底规则。第1项“无光晕”按无意外抠图底色理解；第37项及特效提示词明确要求的柔光予以保留。']
    for a in m['items']:
        lines+=['',f'#### {a["id"]} · {a["path"]}','',f'提示词编号{a["id"]}；生成{len(a["attempts"])}次，采用第{a["selected_attempt"]}次。']
        for c in a['self_check']:lines.append(f'- {c["item"]} {c["name"]}：**{c["result"]}**。{c["reason"]}')
    lines+=['','### 每次生成的结果','']
    for a in m['items']:
        lines.append(f'- 第{a["id"]}项 {a["path"]}：')
        for t in a['attempts']:lines.append(f'  - 第{t["attempt"]}次：{t["verdict"]}')
    lines+=['','### 未做到与验收边界（单独列出）','',
    '1. **没有缺交文件，未发现离线自查未通过项。** 26个最终PNG已齐，所有适用的尺寸、透明度、色差、灰度、分格、居中和拉伸检查通过。',
    '2. **生成原图并非可直接入图集的最终文件。** 已执行透明边缘清理、预乘Alpha缩放、帧切分与居中重排、渐隐与灰度校正。合成爆发第3次原图的六组间距不均，按Alpha透明谷切分后重排为6个192×192格；原图全部保留，没有把原图版式当作已经通过。',
    '3. **角色保留明暗，不声称所有身体像素等于HEX。** 小蓝四头像做同一蓝色Lab校正；用同一组固定亮部/中间调坐标检查，8个样本ΔE76均≤8。白色镜面高光、深色轮廓和阴影不作为身体主色样本。',
    '4. **旧版beam-tip顺序与新提示词不同。** v2参考为由大到小；按本次提示词改为由小到大，保持256×64、4帧及中性灰。其他多帧仍沿用原帧数和横排布局。',
    '5. **本任务未执行游戏接入、图集打包或真机验收。** 本轮只生成交付目录里的美术文件及离线检查资料，没有改程序或其他目录。实机叠加亮度、动画时长、图集是否能排入仍需程序接入后确认；未声称完成运行验收。',
    f'6. **共享目录里的图集记录读取过中间稿，需要重新打包。** 交付时只读比对generated/atlas-sources.json，发现{len(stale)}项仍为旧哈希、占位或缺失：'+', '.join(x['path']for x in stale)+'。本轮未改这些生成记录；最终PNG的SHA256以second-batch-prompts.json为准。',
    '',
    '### 写入范围与保全','',
    f'主动项目写入仅在Art/R1/art-v3/，没有提交Git。开始前记录了{len(before)}个已有文件的SHA256。第一批成品与源提示词保持原样。本任务未修改任务书、generated/atlas-sources.json和generated/program/fx__arrow.png，但检查发现它们在生成期间发生了外部更新，已保留；重新读取任务书第2节、第4节确认本批规格仍一致。其余已有文件除本次追加的交付清单外未变。生成器缓存原图复制归档后保留。',
    '<!-- R1_V3_SECOND_BATCH_END -->','']
    report=ROOT/'交付清单.md';old=report.read_text();marker='<!-- R1_V3_SECOND_BATCH_START -->'
    assert marker not in old,'Refuse accidental duplicate append'
    report.write_text(old.rstrip()+'\n'+'\n'.join(lines))
    print(json.dumps(m['summary'],ensure_ascii=False,indent=2))

if __name__=='__main__':main()


const fs=require('fs'),path=require('path'),crypto=require('crypto'),sharp=require('sharp');
const out=path.resolve(__dirname,'..'),recordPath=path.join(out,'generation-20261021-buttons.json');
const rec=JSON.parse(fs.readFileSync(recordPath,'utf8'));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
(async()=>{
 const selected=[];
 for(const spec of rec.specs){
  const variant=2,candidate=path.join(out,'button-'+spec.id+'-blank-02.png'),target=path.join(out,'button-'+spec.id+'-blank.png');
  fs.copyFileSync(candidate,target);
  selected.push({id:spec.id,variant,candidate,path:target,reason:'在原图保留区完全一致的前提下，选择补色区较平滑、与周围果冻渐变衔接自然的 02。'});
 }
 const width=1120,height=440,cols=[110,450,790],rows=[76,268],tiles=[],labels=[];
 rec.specs.forEach((s,i)=>labels.push('<text x="'+(cols[i]+155)+'" y="35" text-anchor="middle">'+s.label+'</text>'));
 ['原图','去字版'].forEach((t,i)=>labels.push('<text x="14" y="'+(rows[i]+78)+'">'+t+'</text>'));
 for(let row=0;row<2;row++)for(let col=0;col<3;col++)tiles.push({input:row===0?rec.specs[col].input:selected[col].path,left:cols[col],top:rows[row]});
 tiles.push({input:Buffer.from('<svg width="'+width+'" height="'+height+'"><style>text{font-family:PingFang SC,sans-serif;font-size:19px;fill:#303846}</style>'+labels.join('')+'</svg>'),left:0,top:0});
 await sharp({create:{width,height,channels:4,background:'#e9edf1'}}).composite(tiles).png().toFile(path.join(out,'comparison.png'));
 const handoff=[
  '道具按钮无字底图交付 · 2026-10-21',
  '',
  '依据：docs/r1/提示条UI-切图与改动清单.md 第 10.4 节。',
  '工具：内置 imagegen 图片编辑；三张原图分别独立编辑两次。',
  '编辑：仅移除中文文字和字的描边/阴影，保留左侧白色图标、轮廓、原有高光和透明边。',
  '',
  '可直接替换的选定版（均选候选 02，310×144、RGBA）：',
  '  button-shuffle-blank.png   蓝色重排',
  '  button-designate-blank.png 橙黄色优先出战',
  '  button-bomb-blank.png      紫色泡泡炸弹',
  '',
  '每张独立候选：同名前缀加 -01.png、-02.png，共 6 张。',
  'comparison.png：原图在上、选定去字版在下，三按钮并排。',
  'candidates.png：原图、候选 01、候选 02 三行对照。',
  '',
  '生成输出为 1840×855；交付时仅将 AI 修补表面归一到原图坐标，取文字区补色。',
  '使用边界色差的调和插值与 4 像素内向羽化消除局部接缝。',
  '原图画布从未缩放；原图 Alpha 通道全部复用，文字修补区外 RGBA 像素全部复用。',
  '修补区 [x0,y0,x1,y1)，单位为原图像素：',
  '  shuffle  [155,31,276,113]',
  '  designate [115,39,286,110]',
  '  bomb      [117,40,287,110]',
  '',
  '验收：6 张候选均为 310×144 RGBA，Alpha 差异像素为 0，修补区外差异像素为 0。',
  '三张输入原图 SHA-256 与生成前一致；最终选定版为候选 02 的逐字节副本。',
  '已目视检查无文字、描边残影和矩形补色接缝；图标与外轮廓位置不变。',
  '',
  'generation-20261021-buttons.json：完整提示词、输入原图、生成输出、交付文件 SHA-256。',
  '_originals/：六次 AI 编辑的原始输出，保留不改。',
  '_qa/pixel-audit.json：逐像素检查结果。',
  '_qa/finish-buttons.cjs 与 _qa/package-buttons.cjs：交付整理脚本。',
  '',
  '本次仅在 buttons-v1/ 保存出图交付，没有修改游戏或图集，也没有覆盖 delivery/ui/ 原图。',
  '程序接入由后续任务处理。'
 ].join('\n')+'\n';
 fs.writeFileSync(path.join(out,'handoff.txt'),handoff);
 rec.selected=selected;
 rec.postprocess={runtime:'Node.js + sharp',script:'_qa/finish-buttons.cjs',method:'Normalize only the AI edit to 310x144; transfer RGB only within each text region; match boundary colors with harmonic correction and 4px inward feather; preserve every original alpha value and every RGBA pixel outside the text region.',originalCanvasResampled:false,alpha:'copied bit-exact from original',regions:rec.specs.map(s=>({id:s.id,xyxy:s.region}))};
 const files=[...rec.candidates.map(c=>c.path),...selected.map(s=>s.path),path.join(out,'comparison.png'),path.join(out,'candidates.png'),path.join(out,'handoff.txt'),path.join(out,'_qa','pixel-audit.json')];
 rec.outputs=[];
 for(const p of files){
  const x={path:p,sha256:sha(p),bytes:fs.statSync(p).size};
  if(p.endsWith('.png')){const m=await sharp(p).metadata();Object.assign(x,{size:[m.width,m.height],channels:m.channels,hasAlpha:m.hasAlpha});}
  rec.outputs.push(x);
 }
 rec.validation={visualReview:'passed: no remaining text or letter shadow; fills blend continuously; original icons and contour retained',originalHashesUnchanged:rec.specs.every(s=>sha(s.input)===s.inputSha256),allCandidates310x144RGBA:rec.outputs.filter(x=>/blank-\d\d\.png$/.test(x.path)).every(x=>x.size[0]===310&&x.size[1]===144&&x.channels===4&&x.hasAlpha),allAlphaValuesUnchanged:rec.candidates.every(x=>x.alphaChangedPixels===0),allPixelsOutsideTextRegionsUnchanged:rec.candidates.every(x=>x.outsideTextRegionChangedPixels===0),selectedAreExactCandidateCopies:selected.every(s=>sha(s.path)===sha(s.candidate)),candidatePairsDistinct:rec.specs.every(s=>sha(path.join(out,'button-'+s.id+'-blank-01.png'))!==sha(path.join(out,'button-'+s.id+'-blank-02.png')))};
 if(Object.values(rec.validation).some(x=>x===false))throw new Error(JSON.stringify(rec.validation));
 rec.status='completed';rec.completedAt=new Date().toISOString();
 fs.writeFileSync(recordPath,JSON.stringify(rec,null,2)+'\n');
 console.log(JSON.stringify({selected:selected.map(s=>path.basename(s.path)),validation:rec.validation,outputCount:rec.outputs.length},null,2));
})().catch(e=>{console.error(e);process.exit(1)});


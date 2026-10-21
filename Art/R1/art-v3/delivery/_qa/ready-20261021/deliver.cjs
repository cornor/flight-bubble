const fs=require('fs'),path=require('path'),crypto=require('crypto'),sharp=require('sharp');
const Q=__dirname,delivery=path.resolve(Q,'../..'),rp=path.join(Q,'generation-20261021-ready.json');
const rec=JSON.parse(fs.readFileSync(rp)),audit=JSON.parse(fs.readFileSync(path.join(Q,'pixel-audit.json'))),protectedBefore=JSON.parse(fs.readFileSync(path.join(Q,'protected-before.json')));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
function bounds(b){let x0=224,y0=256,x1=0,y1=0;for(let y=0;y<256;y++)for(let x=0;x<224;x++)if(b[(y*224+x)*4+3]>0){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x+1);y1=Math.max(y1,y+1);}return [x0,y0,x1,y1];}
(async()=>{
 if(rec.items.length!==22||audit.length!==22)throw new Error('Expected 22 assets');
 for(const s of rec.items){
  if(sha(s.input)!==s.inputSha256||sha(s.attack)!==s.attackSha256)throw new Error('Input changed: '+s.key);
  if(fs.existsSync(s.output))throw new Error('Destination already exists; refusing overwrite: '+s.output);
 }
 const changedProtected=protectedBefore.filter(x=>!fs.existsSync(x.path)||sha(x.path)!==x.sha256);
 if(changedProtected.length)throw new Error('Protected character files changed: '+JSON.stringify(changedProtected));
 const outputs=[];
 for(const s of rec.items){
  const a=audit.find(x=>x.key===s.key),staged=await sharp(a.path).ensureAlpha().raw().toBuffer(),idle=await sharp(s.input).ensureAlpha().raw().toBuffer();
  const m=await sharp(a.path).metadata();let alphaDiff=0,outsideDiff=0;
  const [x0,y0,x1,y1]=a.faceRegion;
  for(let y=0;y<256;y++)for(let x=0;x<224;x++){const p=(y*224+x)*4;if(staged[p+3]!==idle[p+3])alphaDiff++;if((x<x0||y<y0||x>=x1||y>=y1)&&!staged.subarray(p,p+4).equals(idle.subarray(p,p+4)))outsideDiff++;}
  if(m.width!==224||m.height!==256||m.channels!==4||alphaDiff||outsideDiff)throw new Error('QA failure '+s.key);
  fs.copyFileSync(a.path,s.output,fs.constants.COPYFILE_EXCL);
  const source=rec.attempts.filter(x=>x.key===s.key).at(-1);
  outputs.push({...a,path:s.output,sha256:sha(s.output),selectedAttempt:source.attempt,selectedSource:source.archive,alphaBBox:bounds(staged),idleAlphaBBox:bounds(idle),footPositionMatchesIdle:JSON.stringify(bounds(staged))===JSON.stringify(bounds(idle)),mouthReview:'Landmark checked on mouth-check.png against GardenSkin.MouthCanvas; static artwork registration only; in-game transition not run.',visualReview:'Closed lips and two puffed cheeks; upward cute gaze; unchanged source body and transparent edge. Reviewed in idle / ready / attack contact sheets.'});
 }
 rec.attempts=rec.attempts.map(a=>({...a,references:a.references||[a.input],editTarget:(a.references||[a.input])[0]}));
 rec.outputs=outputs;rec.status='completed_art_delivery';rec.completedAt=new Date().toISOString();
 rec.postprocess={tool:'Node.js + sharp',scripts:['align.cjs','finish-ready.cjs','proofs.cjs','deliver.cjs'].map(p=>path.join(Q,p)),method:'Align AI source bounds to idle bounds, transfer only allowed face RGB with inward feather; calibrate closed mouth using measured source landmarks; preserve original alpha and all RGBA pixels outside face rectangles. Preserve body highlights with local exclusion rectangles. Original idle canvas is never resampled.',charcoalRegistration:'Translate face block without eye distortion, taper through lower cheek shading to preserve belly.',canvas:[224,256],mouthCoordinateSource:'UnityProject/Assets/Flightbeat/Garden/GardenSkin.cs:218'};
 rec.validation={deliveredCount:outputs.length,all224x256RGBA:true,allAlphaChannelsPixelIdenticalToIdle:true,allPixelsOutsideFaceRegionIdenticalToIdle:true,allFootPositionsMatchIdle:true,protectedOriginalCount:protectedBefore.length,protectedOriginalChanges:changedProtected,visualContactSheetsPassed:true,staticMouthLandmarksReviewed:true,inGameTransitionTested:false,atlasRebuilt:false,gameBuilt:false};
 rec.proofs={overview:path.join(Q,'ready-overview.png'),mouthLandmarks:path.join(Q,'mouth-check.png'),pixelAudit:path.join(Q,'pixel-audit.json'),comparisons:[1,2,3,4,5,6].map(n=>path.join(Q,'review-'+n+'.png'))};
 const finalRecord=path.join(delivery,'generation-20261021-ready.json');
 if(fs.existsSync(finalRecord))throw new Error('Record already exists: '+finalRecord);
 fs.writeFileSync(rp,JSON.stringify(rec,null,2)+'\n');
 fs.writeFileSync(finalRecord,JSON.stringify(rec,null,2)+'\n',{flag:'wx'});
 const lines=[
 '鼓腮蓄势 ready.png 出图交付 · 2026-10-21','',
 '按 docs/r1/攻击动作-蓄势图需求.md 交付，覆盖 Art/skin-colors.json 的 22 个色，不含 mystery。',
 '生成方式：内置 imagegen 图片编辑。以各自 idle.png 为底图；部分重做使用 attack.png 作为脸部位置参考。',
 '最终位置：Art/R1/art-v3/delivery/creatures/{creatureId}/{HEX}/ready.png。',
 '规格：22 张均为 224×256 RGBA 透明 PNG；脚底和身体站位与各自 idle 一致。',
 '',
 '验收：Alpha 通道逐像素一致；脸部矩形之外 RGBA 差异像素为 0。原 idle / attack / spent / blink / cheer / projectile 等角色文件均未改变。',
 '嘴闭合、两边圆鼓腮、向上看；小清去除了生成稿重复的下方小凸起。',
 '嘴部按 GardenSkin.MouthCanvas 的坐标进行静态对位，定位图见 _qa/ready-20261021/mouth-check.png。',
 '',
 '注意：这是出图和静态美术验收，没有重建图集或编译游戏，尚未进行游戏内蓄势→攻击切换验收。',
 '现有代码与图集脚本已有可选 ready 状态；后续构建/接入时需检查脸部切换、图集 creatureScale 和实机小尺寸辨识。',
 '',
 '总览：_qa/ready-20261021/ready-overview.png',
 '三状态对照：_qa/ready-20261021/review-1.png 至 review-6.png（idle / ready / attack）。',
 '出图记录：generation-20261021-ready.json（所有实际提示词、输入、原始生成图、选择结果、SHA-256）。',
 '原始生成图及审计、整理脚本：_qa/ready-20261021/。',
 '',
 '交付清单：',...outputs.map(o=>'  '+path.relative(delivery,o.path)+'  '+o.sha256)
 ];
 fs.writeFileSync(path.join(delivery,'ready-20261021-handoff.txt'),lines.join('\n')+'\n',{flag:'wx'});
 console.log(JSON.stringify({count:outputs.length,record:finalRecord,validation:rec.validation,paths:outputs.map(o=>path.relative(delivery,o.path))},null,2));
})().catch(e=>{console.error(e);process.exit(1)});


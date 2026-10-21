const sharp = require('sharp');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const root = path.resolve(__dirname, '../../../../../..');
const delivery = path.join(root, 'Art/R1/art-v3/delivery');
const input = JSON.parse(fs.readFileSync(path.join(__dirname, 'request.json')));
const final = path.join(root, input.finalPath);
const hash = p => crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
async function bounds(p, threshold) {
  const {data,info}=await sharp(p).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let x0=info.width,y0=info.height,x1=-1,y1=-1, count=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>=threshold) {
    x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);count++;
  }
  return {left:x0,top:y0,width:x1-x0+1,height:y1-y0+1,right:x1+1,bottom:y1+1,count};
}
(async()=>{
  if(fs.existsSync(final))throw new Error('Final already exists; refusing overwrite');
  const protectedPaths=[...input.styleInspected,'Art/R1/art-v3/delivery/creatures/mystery/8C919C/idle.png'];
  const protectedBefore=Object.fromEntries(protectedPaths.map(p=>[p,hash(path.join(root,p))]));
  const raw=path.join(__dirname,'original.png');
  fs.copyFileSync(input.rawGeneratedPath,raw);
  const originalMeta=await sharp(raw).metadata();
  const box=await bounds(raw,16);
  const pad=6;
  const crop={left:box.left-pad,top:box.top-pad,width:box.width+pad*2,height:box.height+pad*2};
  const scaled=await sharp(raw).extract(crop).resize({width:152,kernel:'lanczos3'}).png().toBuffer();
  const scaledBox=await bounds(scaled,16);
  const left=Math.round(112-(scaledBox.left+scaledBox.right)/2);
  const top=212-scaledBox.bottom;
  await sharp({create:{width:224,height:256,channels:4,background:{r:0,g:0,b:0,alpha:0}}})
    .composite([{input:scaled,left,top}]).png().toFile(final);
  const finalMeta=await sharp(final).metadata();
  const alphaBounds={};
  for(const threshold of [1,16,64,128,240])alphaBounds[threshold]=await bounds(final,threshold);
  if(finalMeta.width!==224||finalMeta.height!==256||finalMeta.channels!==4||!finalMeta.hasAlpha)throw new Error('Wrong final format');
  if(alphaBounds[16].width<148||alphaBounds[16].width>152||alphaBounds[16].bottom!==212)throw new Error('Wrong placement');
  const protectedAfter=Object.fromEntries(protectedPaths.map(p=>[p,hash(path.join(root,p))]));
  if(JSON.stringify(protectedBefore)!==JSON.stringify(protectedAfter))throw new Error('Reference assets changed');
  const previewPanels=[];
  for(const bg of ['#F2F7FC','#24334B']){
    previewPanels.push(await sharp(final).flatten({background:bg}).resize(448,512,{kernel:'nearest'}).png().toBuffer());
  }
  await sharp({create:{width:896,height:512,channels:3,background:'#F2F7FC'}})
    .composite([{input:previewPanels[0],left:0,top:0},{input:previewPanels[1],left:448,top:0}])
    .png().toFile(path.join(__dirname,'transparency-review.png'));
  const record={...input,rawSavedPath:path.relative(root,raw),rawSha256:hash(raw),
    sourceDimensions:[originalMeta.width,originalMeta.height],postprocess:{operation:'Crop transparent margins with 6 source-pixel padding; proportional Lanczos3 resize; position on transparent canvas. No repainting.',crop,scaledWidth:152,position:{left,top}},
    final:{width:finalMeta.width,height:finalMeta.height,channels:finalMeta.channels,hasAlpha:finalMeta.hasAlpha,sha256:hash(final),alphaBounds},
    protectedBefore,protectedAfter,staticQa:{questionMarks:4,questionMarksOnly:true,noFaceOrLimbs:true,noShadow:true,singleDarkVioletOutline:true},gameIntegration:'Existing optional atlas slot confirmed by reading scripts/r1v3/build_atlas_v3.py. No code changes or build.'};
  fs.writeFileSync(path.join(delivery,'generation-20261021-mystery-bubble.json'),JSON.stringify(record,null,2)+'\n');
  fs.writeFileSync(path.join(delivery,'mystery-bubble-20261021-handoff.txt'),
    '隐藏角色彩虹问号泡泡 · 2026-10-21\n生成方式：内置 imagegen，全新生成 1 张。\n交付：creatures/mystery/bubble.png，224×256 RGBA 透明 PNG。\n泡泡可见主体约 150 px 宽，水平居中，底边 y≈212；4 个白色问号、深紫蓝描边、左上双高光、下沿反光，无脸和肢体、无底座和落地阴影。\n只做透明边距裁剪、等比缩放和画布站位整理，保留生成图透明边缘。原始生成图、提示词和浅深背景检查图位于 _qa/mystery-bubble-20261021/。\n图集脚本已有 plants/mystery-bubble 槽，白描边由游戏自动生成。\n本次未改代码、未重建图集或编译游戏，游戏内效果待下次构建检查。\n出图记录（含完整提示词、尺寸、站位及 SHA-256）：generation-20261021-mystery-bubble.json。\n');
  console.log(JSON.stringify({final,sha256:hash(final),alphaBounds,protectedUnchanged:true},null,2));
})().catch(e=>{console.error(e);process.exit(1);});


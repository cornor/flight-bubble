const sharp=require('sharp'),fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../../../../..'),W=192,H=360;
const clamp=(v,a,b)=>Math.min(b,Math.max(a,v));
const smooth=(a,b,x)=>{const t=clamp((x-a)/(b-a),0,1);return t*t*(3-2*t);};
function median(a){a.sort((x,y)=>x-y);return a[Math.floor(a.length/2)];}
async function bounds(input,t=32){const {data,info}=await sharp(input).ensureAlpha().raw().toBuffer({resolveWithObject:true});let x0=info.width,y0=info.height,x1=-1,y1=-1;for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>=t){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);}return {left:x0,top:y0,width:x1-x0+1,height:y1-y0+1,right:x1+1,bottom:y1+1};}
(async()=>{
 const source=path.join(root,'Art/R1/art-v3/delivery/_originals/queue-groove-attempt-03.png');
 const b=await bounds(source),pad=4,sw=b.width+2*pad,sh=Math.round(sw/2),cw=188,ch=86;
 const crops={top:{left:b.left-pad,top:b.top-pad,width:sw,height:sh},bottom:{left:b.left-pad,top:b.bottom+pad-sh,width:sw,height:sh},middle:{left:b.left-pad,top:Math.round((b.top+b.bottom)/2)-32,width:sw,height:64}};
 const top=await sharp(source).extract(crops.top).resize(cw,ch,{fit:'fill',kernel:'lanczos3'}).ensureAlpha().raw().toBuffer();
 const bottom=await sharp(source).extract(crops.bottom).resize(cw,ch,{fit:'fill',kernel:'lanczos3'}).ensureAlpha().raw().toBuffer();
 const mid=await sharp(source).extract(crops.middle).resize(cw,64,{fit:'fill',kernel:'lanczos3'}).ensureAlpha().raw().toBuffer();
 const row=Buffer.alloc(cw*4);
 for(let x=0;x<cw;x++)for(let c=0;c<4;c++){const values=[];for(let y=0;y<64;y++)values.push(mid[(y*cw+x)*4+c]);row[x*4+c]=median(values);}
 const med=[0,1,2,3].map(c=>median(Array.from({length:80},(_,j)=>row[(54+j)*4+c])));
 const target=[161,183,213],offset=target.map((v,c)=>v-med[c]),alphaScale=173/med[3];
 const out=Buffer.alloc(W*H*4);
 function finishPixel(p,y){
  const brightness=(p[0]+p[1]+p[2])/3,whiteProtection=1-smooth(200,245,brightness);
  const lightLip=y>=272?smooth(205,240,brightness):0;
  const factor=alphaScale*(1-lightLip*(1-102/173));
  const a=Math.round(p[3]*factor);
  if(a<2)return [0,0,0,0];
  return [clamp(Math.round(p[0]+offset[0]*whiteProtection),0,255),clamp(Math.round(p[1]+offset[1]*whiteProtection),0,255),clamp(Math.round(p[2]+offset[2]*whiteProtection),0,255),a];
 }
 for(let y=0;y<H;y++)for(let x=0;x<cw;x++){
  let p;
  if(y<2||y>=358)continue;
  if(y<88){const sy=y-2,i=(sy*cw+x)*4;p=[...top.subarray(i,i+4)];const t=smooth(79,87,y);for(let c=0;c<4;c++)p[c]=p[c]*(1-t)+row[x*4+c]*t;}
  else if(y<272)p=[...row.subarray(x*4,x*4+4)];
  else {const sy=y-272,i=(sy*cw+x)*4;p=[...bottom.subarray(i,i+4)];const t=smooth(272,280,y);for(let c=0;c<4;c++)p[c]=row[x*4+c]*(1-t)+p[c]*t;}
  const v=finishPixel(p,y);for(let c=0;c<4;c++)out[(y*W+x+2)*4+c]=v[c];
 }
 // Retain the connected painted silhouette and remove isolated alpha specks outside it.
 const seen=new Uint8Array(W*H),components=[];
 for(let seed=0;seed<W*H;seed++)if(!seen[seed]&&out[seed*4+3]>0){
  const cells=[seed];seen[seed]=1;
  for(let h=0;h<cells.length;h++){const at=cells[h],x=at%W,y=Math.floor(at/W);for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){const nx=x+dx,ny=y+dy;if(nx<0||nx>=W||ny<0||ny>=H)continue;const j=ny*W+nx;if(!seen[j]&&out[j*4+3]>0){seen[j]=1;cells.push(j);}}}
  components.push(cells);
 }
 components.sort((a,b)=>b.length-a.length);let removedSpecks=0;
 for(const component of components.slice(1))for(const j of component){out.fill(0,j*4,j*4+4);removedSpecks++;}
 const candidate=path.join(__dirname,'queue-groove-candidate.png');await sharp(out,{raw:{width:W,height:H,channels:4}}).png().toFile(candidate);
 const pw=186,ph=344,cap=Math.round(88*pw/W),middleHeight=ph-cap*2;
 const previewTop=await sharp(candidate).extract({left:0,top:0,width:W,height:88}).resize(pw,cap).png().toBuffer();
 const previewMiddle=await sharp(candidate).extract({left:0,top:88,width:W,height:1}).resize(pw,middleHeight,{fit:'fill',kernel:'nearest'}).png().toBuffer();
 const previewBottom=await sharp(candidate).extract({left:0,top:272,width:W,height:88}).resize(pw,cap).png().toBuffer();
 const previewSprite=await sharp({create:{width:pw,height:ph,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input:previewTop,left:0,top:0},{input:previewMiddle,left:0,top:cap},{input:previewBottom,left:0,top:cap+middleHeight}]).png().toBuffer();
 const background=path.join(root,'Art/R1/art-v3/delivery/scene/background.png'),positions=[72,322,572,822].map(x=>({left:x,top:248}));
 const preview=path.join(root,'Art/R1/ui-calm-design-20261021/13-排队凹槽-预览.png');
 await sharp(background).extract({left:0,top:1152,width:1080,height:768}).composite(positions.map(p=>({input:previewSprite,...p}))).removeAlpha().png().toFile(preview);
 const flat=await sharp(candidate).flatten({background:'#AAC1DD'}).resize(384,720,{kernel:'nearest'}).png().toBuffer();
 await sharp({create:{width:768,height:720,channels:3,background:'#AAC1DD'}}).composite([{input:flat,left:0,top:0},{input:await sharp(candidate).flatten({background:'#536579'}).resize(384,720,{kernel:'nearest'}).png().toBuffer(),left:384,top:0}]).png().toFile(path.join(__dirname,'material-review.png'));
 const stretch=await sharp({create:{width:192,height:600,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([
 {input:await sharp(candidate).extract({left:0,top:0,width:192,height:88}).png().toBuffer(),left:0,top:0},
 {input:await sharp(candidate).extract({left:0,top:88,width:192,height:1}).resize(192,424,{fit:'fill',kernel:'nearest'}).png().toBuffer(),left:0,top:88},
 {input:await sharp(candidate).extract({left:0,top:272,width:192,height:88}).png().toBuffer(),left:0,top:512}]).png().toFile(path.join(__dirname,'stretch-check-600.png'));
 fs.writeFileSync(path.join(__dirname,'postprocessing.json'),JSON.stringify({source,sourceBounds:b,sourceCrops:crops,capRows:88,uniformRows:[88,271],method:'Resample generated end caps into 88-pixel head/tail zones; median source strip supplies a single repeated center row; soften cap-to-center joins within the caps. Calibrate muted blue-gray RGB and face alpha, protect pale lip. Remove disconnected alpha specks only.',removedDisconnectedAlphaPixels:removedSpecks,centerOriginalRgba:med,centerTargetRgb:target,rgbOffsets:offset,alphaScale,centerTargetAlpha:173,brightestBottomLipTargetAlpha:102,output:candidate,preview:{path:preview,stageCrop:{left:0,top:1152,width:1080,height:768},spriteSize:[pw,ph],nineSliceCaps:cap,positions}},null,2)+'\n');
 console.log(JSON.stringify({candidate,preview,bounds:await bounds(candidate,1),centerOriginalRgba:med,rgbOffsets:offset,centerTargetAlpha:173,previewCaps:cap}));
})().catch(e=>{console.error(e);process.exit(1);});

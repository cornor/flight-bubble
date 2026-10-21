const fs=require('fs'),path=require('path'),sharp=require('sharp'),crypto=require('crypto');
const Q=__dirname,items=JSON.parse(fs.readFileSync(path.join(Q,'alignment.json')));
const rawMouth=[[668,716],[671,698],[656,740],[653,735],[723,766],[631,729],[625,765],[651,711],[617,748],[607,712],[667,732],[622,640],[637,708],[485,827],[668,724],[607,689],[625,799],[596,581],[621,600],[643,804],[658,677],[646,613]];
const newTop={blue:92,orange:90,green:84,purple:97,yellow:102,white:86,red:116,charcoal:94,'slate-blue':94,cyan:84,lavender:103,berry:111,brown:93,violet:94,lime:99,mint:78,forest:86,pink:100,peach:91,'pale-mint':80};
const preserves={green:[[115,82,134,94]],mint:[[97,98,132,109]],pink:[[109,98,133,109]],forest:[[75,78,92,91]],'pale-mint':[[99,69,124,83]]};
const faceBottom={green:191,purple:191,red:191,charcoal:157,'slate-blue':159,cyan:166,berry:190,brown:194,violet:174,lime:187,mint:151,forest:144,pink:191,peach:183,'pale-mint':174};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex'),smooth=x=>x*x*(3-2*x);
function pixel(b,x,y,c){x=Math.max(0,Math.min(222.9999,x));y=Math.max(0,Math.min(254.9999,y));const ix=Math.floor(x),iy=Math.floor(y),u=x-ix,v=y-iy,p=(iy*224+ix)*4;return b[p+c]*(1-u)*(1-v)+b[p+4+c]*u*(1-v)+b[p+224*4+c]*(1-u)*v+b[p+225*4+c]*u*v;}
(async()=>{
 const audits=[];
 for(let n=0;n<items.length;n++){
  const s=items[n],o=await sharp(s.input).ensureAlpha().raw().toBuffer(),a=await sharp(s.aligned).ensureAlpha().raw().toBuffer(),result=Buffer.from(o);
  const [rx0,ry0,rx1,ry1]=s.rawBBox,[ix0,iy0,ix1,iy1]=s.alignedBBox;
  const source=[ix0+(rawMouth[n][0]-rx0)*(ix1-ix0)/(rx1-rx0),iy0+(rawMouth[n][1]-ry0)*(iy1-iy0)/(ry1-ry0)];
  const [mx,my]=s.mouth,dx=source[0]-mx,dy=source[1]-my;
  const region=[...s.faceRegion];region[1]=s.creatureId==='red'?101:s.creatureId==='lavender'?93:newTop[s.creatureId];
  if(faceBottom[s.creatureId])region[3]=faceBottom[s.creatureId];
  if(s.creatureId==='charcoal'){region[2]=166;}
  const [x0,y0,x1,y1]=region,mask=Buffer.alloc(224*256*4),diff=Buffer.alloc(224*256*4);
  const radX=Math.max(18,Math.abs(dx)*3+6),radY=Math.max(18,Math.abs(dy)*3+6);
  for(let y=y0;y<y1;y++)for(let x=x0;x<x1;x++){
   const p=(y*224+x)*4;
   if(o[p+3]<240)continue;
   let edge=6;
   for(let yy=Math.max(0,y-6);yy<=Math.min(255,y+6);yy++)for(let xx=Math.max(0,x-6);xx<=Math.min(223,x+6);xx++)if(o[(yy*224+xx)*4+3]<220)edge=Math.min(edge,Math.hypot(xx-x,yy-y));
   if(edge<=3)continue;
   if((preserves[s.creatureId]||[]).some(r=>x>=r[0]&&x<r[2]&&y>=r[1]&&y<r[3]))continue;
   const d=Math.min(x-x0,y-y0,x1-1-x,y1-1-y),blend=smooth(Math.min(1,d/5))*smooth(Math.min(1,(edge-3)/3));
   // Small residual mouth registration; body and far-away facial features are fixed.
   const r2=((x-mx)/radX)**2+((y-my)/radY)**2,w=r2<1?(1-r2)**2:0;
   const uniform=s.creatureId==='charcoal';
   const coreEnd=my+7,end=y1-4,wy=y<=coreEnd?1:Math.max(0,(end-y)/(end-coreEnd));
   const sx=x+dx*(uniform?1:w),sy=y+dy*(uniform?wy:w);
   if(pixel(a,sx,sy,3)<220)continue;
   for(let c=0;c<3;c++)result[p+c]=Math.round(o[p+c]*(1-blend)+pixel(a,sx,sy,c)*blend);
   mask[p]=mask[p+1]=mask[p+2]=Math.round(255*blend);mask[p+3]=255;
  }
  let changed=0,outside=0,alphaChanged=0,bbox=[224,256,0,0];
  for(let y=0;y<256;y++)for(let x=0;x<224;x++){
   const p=(y*224+x)*4;
   if(result[p+3]!==o[p+3])alphaChanged++;
   if(!result.subarray(p,p+4).equals(o.subarray(p,p+4))){
    changed++;bbox=[Math.min(bbox[0],x),Math.min(bbox[1],y),Math.max(bbox[2],x+1),Math.max(bbox[3],y+1)];
    if(x<x0||y<y0||x>=x1||y>=y1)outside++;
    diff[p]=255;diff[p+1]=50;diff[p+2]=200;diff[p+3]=255;
   }
  }
  const final=path.join(Q,'staging',s.key+'-ready.png');
  const raw={width:224,height:256,channels:4};
  await sharp(result,{raw}).png().toFile(final);
  await sharp(mask,{raw}).png().toFile(path.join(Q,'staging',s.key+'-face-mask.png'));
  await sharp(diff,{raw}).png().toFile(path.join(Q,'staging',s.key+'-diff.png'));
  const audit={key:s.key,path:final,sha256:sha(final),size:[224,256],mode:'RGBA',faceRegion:region,preserveRectangles:preserves[s.creatureId]||[],changedPixels:changed,changedBBox:bbox,outsideFaceChangedPixels:outside,alphaChangedPixels:alphaChanged,sourceMouth:source,mouthTarget:s.mouth,mouthCalibration:{method:'local smooth inverse resampling of AI face only; original body untouched',rawMouth:rawMouth[n],displacement:[dx,dy],supportRadii:[radX,radY]},inputUnchanged:sha(s.input)===s.inputSha256};
  if(alphaChanged||outside||!changed||!audit.inputUnchanged)throw new Error(JSON.stringify(audit));
  audits.push(audit);
 }
 fs.writeFileSync(path.join(Q,'pixel-audit.json'),JSON.stringify(audits,null,2)+'\n');
 for(let start=0;start<items.length;start+=4){
  const part=items.slice(start,start+4),W=734,H=part.length*266+32,layers=[],texts=[];
  for(let i=0;i<part.length;i++){const s=part[i],y=32+i*266;texts.push('<text x="10" y="'+(y+14)+'">'+s.key+' | idle / ready / attack</text>');
   [s.input,path.join(Q,'staging',s.key+'-ready.png'),s.attack].forEach((p,j)=>layers.push({input:p,left:10+j*240,top:y+10}));
  }
  layers.push({input:Buffer.from('<svg width="'+W+'" height="'+H+'"><style>text{font:14px sans-serif;fill:#27364c}</style>'+texts.join('')+'</svg>'),left:0,top:0});
  await sharp({create:{width:W,height:H,channels:4,background:'#e7edf2'}}).composite(layers).png().toFile(path.join(Q,'review-'+(1+start/4)+'.png'));
 }
 console.log(JSON.stringify(audits.map(s=>({key:s.key,sourceMouth:s.sourceMouth.map(v=>+v.toFixed(1)),target:s.mouthTarget,alpha:s.alphaChangedPixels,outside:s.outsideFaceChangedPixels}))));
})().catch(e=>{console.error(e);process.exit(1)});


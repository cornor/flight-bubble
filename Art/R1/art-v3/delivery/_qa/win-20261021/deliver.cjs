const sharp=require('sharp'),fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../../../../..');
const request=JSON.parse(fs.readFileSync(path.join(__dirname,'request.json')));
const delivery=path.join(root,'Art/R1/art-v3/delivery');
const out=path.join(root,request.destination);
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
async function bounds(input,threshold) {
 const {data,info}=await sharp(input).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let x0=info.width,y0=info.height,x1=-1,y1=-1,count=0;
 for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>=threshold){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x);y1=Math.max(y1,y);count++;}
 return {left:x0,top:y0,width:x1-x0+1,height:y1-y0+1,right:x1+1,bottom:y1+1,count};
}
(async()=>{
 const finals=request.specs.map(s=>path.join(out,s.key+'.png'));
 for(const p of finals)if(fs.existsSync(p))throw new Error('Refusing overwrite: '+p);
 fs.mkdirSync(out,{recursive:true});fs.mkdirSync(path.join(__dirname,'originals'),{recursive:true});
 const before=Object.fromEntries(request.styleInspected.map(p=>[p,hash(path.join(root,p))]));
 const records=[];
 for(const spec of request.specs){
  const raw=path.join(__dirname,'originals',spec.key+'.png');fs.copyFileSync(spec.raw,raw);
  const meta=await sharp(raw).metadata();const b=await bounds(raw,16);
  const pad=6;
  const crop={left:Math.max(0,b.left-pad),top:Math.max(0,b.top-pad)};
  crop.width=Math.min(meta.width,b.right+pad)-crop.left;crop.height=Math.min(meta.height,b.bottom+pad)-crop.top;
  const margin=spec.key==='ribbon'?8:spec.key==='icon-clock'?6:4;
  const contentWidth=spec.width-2*margin,contentHeight=spec.height-2*margin;
  const sprite=await sharp(raw).extract(crop).resize(contentWidth,contentHeight,{fit:'fill',kernel:'lanczos3'}).png().toBuffer();
  const final=path.join(out,spec.key+'.png');
  await sharp({create:{width:spec.width,height:spec.height,channels:4,background:{r:0,g:0,b:0,alpha:0}}}).composite([{input:sprite,left:margin,top:margin}]).png().toFile(final);
  const fm=await sharp(final).metadata(),alphaBounds={};for(const t of [1,16,64,128])alphaBounds[t]=await bounds(final,t);
  if(fm.width!==spec.width||fm.height!==spec.height||fm.channels!==4||!fm.hasAlpha)throw new Error('Invalid output format');
  const box=alphaBounds[1];
  if(box.left<=0||box.top<=0||box.right>=spec.width||box.bottom>=spec.height)throw new Error('Nontransparent canvas border');
  records.push({...spec,result:undefined,rawSavedPath:path.relative(root,raw),rawSha256:hash(raw),sourceSize:[meta.width,meta.height],
    finalPath:path.relative(root,final),finalSha256:hash(final),format:{width:fm.width,height:fm.height,channels:fm.channels,hasAlpha:fm.hasAlpha},
    normalization:{crop,contentWidth,contentHeight,margin,method:'Lanczos3 resize to asset bounds and composite on transparent exact-size canvas; generated artwork and alpha retained, no painted content added.'},alphaBounds});
 }
 const placements=[{key:'ribbon',x:112,y:18},{key:'button-home',x:36,y:284},{key:'button-replay',x:220,y:284},{key:'button-next',x:404,y:276},{key:'icon-clock',x:856,y:296}];
 const labels=placements.map(p=>'<text x="'+(p.x+request.specs.find(s=>s.key===p.key).width/2)+'" y="'+(p.key==='ribbon'?260:464)+'" text-anchor="middle">'+p.key+'.png</text>').join('');
 for(const [name,bg,fg] of [['overview','#F3F7FD','#4A5670'],['dark-review','#253046','#DCE7FF']]){
  const labelSvg=Buffer.from('<svg width="1024" height="488" xmlns="http://www.w3.org/2000/svg"><g font-family="sans-serif" font-size="16" fill="'+fg+'">'+labels+'</g></svg>');
  await sharp({create:{width:1024,height:488,channels:3,background:bg}}).composite([...placements.map(p=>({input:path.join(out,p.key+'.png'),left:p.x,top:p.y})),{input:labelSvg,left:0,top:0}]).png().toFile(path.join(__dirname,name+'.png'));
 }
 const after=Object.fromEntries(request.styleInspected.map(p=>[p,hash(path.join(root,p))]));
 if(JSON.stringify(before)!==JSON.stringify(after))throw new Error('Style reference changed');
 const record={date:request.date,mode:request.mode,requirement:request.requirement,assets:records,styleReferenceHashesBefore:before,styleReferenceHashesAfter:after,
   staticQa:{exactDimensions:true,rgbaTransparency:true,transparentCanvasBorders:true,visualReview:'pending'},integration:{atlasSlots:request.specs.map(s=>'plants/win/'+s.key),existingSlotsConfirmed:true,atlasRebuilt:false,gameBuilt:false,inGameTested:false}};
 fs.writeFileSync(path.join(delivery,'generation-20261021-win.json'),JSON.stringify(record,null,2)+'\n');
 console.log(JSON.stringify(records.map(r=>({key:r.key,size:[r.format.width,r.format.height],bounds:r.alphaBounds[16],sha256:r.finalSha256})),null,2));
})().catch(e=>{console.error(e);process.exit(1);});


const fs=require('fs'),path=require('path'),sharp=require('sharp'),crypto=require('crypto');
const qa=__dirname, del=path.resolve(qa,'../..'), out=path.join(del,'ui4');
const selected=JSON.parse(fs.readFileSync(path.join(qa,'selected.json'))), generations=JSON.parse(fs.readFileSync(path.join(qa,'generations.json')));
const logs=[];
function bbox(data,w,h,threshold){
 let l=w,t=h,r=0,b=0;
 for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(data[(y*w+x)*4+3]>=threshold){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x+1);b=Math.max(b,y+1);}
 return {left:l,top:t,width:r-l,height:b-t};
}
(async()=>{
 fs.mkdirSync(out,{recursive:true});
 for(const item of generations)fs.copyFileSync(item.raw,path.join(del,'_originals','popup-'+item.name+'-attempt-01.png'));
 for(const s of selected){
  if(s.attempt===2)fs.copyFileSync(s.raw,path.join(del,'_originals','popup-'+s.name+'-attempt-02.png'));
  const {data,info}=await sharp(s.raw).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const box=bbox(data,info.width,info.height,8),pad=s.name==='panel-card'?0:7;
  const crop={left:Math.max(0,box.left-pad),top:Math.max(0,box.top-pad),width:Math.min(info.width,box.left+box.width+pad)-Math.max(0,box.left-pad),height:Math.min(info.height,box.top+box.height+pad)-Math.max(0,box.top-pad)};
  const panel=s.name==='panel-card',size=panel?356:116;
  const resized=await sharp(s.raw).extract(crop).resize(size,size,{fit:panel?'fill':'inside',kernel:'lanczos3'}).ensureAlpha().toBuffer();
  const md=await sharp(resized).metadata(),left=Math.floor((s.width-md.width)/2),top=Math.floor((s.height-md.height)/2);
  const result=await sharp({create:{width:s.width,height:s.height,channels:4,background:'#00000000'}}).composite([{input:resized,left,top}]).png().toBuffer();
  fs.writeFileSync(path.join(out,s.name+'.png'),result);
  logs.push({name:s.name,attempt:s.attempt,sourceSize:[info.width,info.height],crop,resized:[md.width,md.height],placement:[left,top],finalSize:[s.width,s.height],sha256:crypto.createHash('sha256').update(result).digest('hex'),operation:'Crop around alpha >= 8 with padding for icons; Lanczos resize; center on exact transparent canvas. Preserve generated colors and alpha.'});
 }
 fs.writeFileSync(path.join(qa,'postprocessing.json'),JSON.stringify(logs,null,2)+'\n');
 console.log(JSON.stringify(logs.map(({name,finalSize,sha256})=>({name,finalSize,sha256})),null,2));
})().catch(e=>{console.error(e);process.exit(1)});


const fs=require('fs'),sharp=require('sharp'),path=require('path');
const Q=__dirname,D=JSON.parse(fs.readFileSync(path.join(Q,'generation-20261021-ready.json')));
function bbox(b,w,h,t=64){let x0=w,y0=h,x1=0,y1=0;for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(b[(y*w+x)*4+3]>=t){x0=Math.min(x0,x);y0=Math.min(y0,y);x1=Math.max(x1,x+1);y1=Math.max(y1,y+1);}return [x0,y0,x1,y1];}
(async()=>{
 const data=[];
 for(const s of D.items){
  const a=D.attempts.filter(a=>a.key===s.key).at(-1);if(!a)continue;
  const orig=await sharp(s.input).ensureAlpha().raw().toBuffer();
  const raw=await sharp(a.archive).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  const rb=bbox(raw.data,raw.info.width,raw.info.height),ib=bbox(orig,224,256);
  const png=await sharp(a.archive).extract({left:rb[0],top:rb[1],width:rb[2]-rb[0],height:rb[3]-rb[1]}).resize(ib[2]-ib[0],ib[3]-ib[1]).png().toBuffer();
  const dest=path.join(Q,'staging',s.key+'-aligned.png');
  await sharp({create:{width:224,height:256,channels:4,background:'#00000000'}}).composite([{input:png,left:ib[0],top:ib[1]}]).png().toFile(dest);
  data.push({...s,aligned:dest,rawBBox:rb,alignedBBox:ib});
 }
 fs.writeFileSync(path.join(Q,'alignment.json'),JSON.stringify(data,null,2)+'\n');
 for(let start=0;start<data.length;start+=4){
  const part=data.slice(start,start+4),W=734,H=part.length*266+32,layers=[],texts=[];
  for(let i=0;i<part.length;i++){const s=part[i],y=32+i*266;texts.push('<text x="10" y="'+(y+14)+'">'+s.key+' | idle / AI aligned / attack | mouth '+s.mouth+'</text>');
   [s.input,s.aligned,s.attack].forEach((p,j)=>layers.push({input:p,left:10+j*240,top:y+10}));
  }
  layers.push({input:Buffer.from('<svg width="'+W+'" height="'+H+'"><style>text{font:14px sans-serif;fill:#27364c}</style>'+texts.join('')+'</svg>'),left:0,top:0});
  await sharp({create:{width:W,height:H,channels:4,background:'#e7edf2'}}).composite(layers).png().toFile(path.join(Q,'aligned-review-'+(1+start/4)+'.png'));
 }
 console.log(data.map(s=>({key:s.key,bbox:s.rawBBox})));
})().catch(e=>{console.error(e);process.exit(1)});


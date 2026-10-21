const fs=require('fs'),path=require('path'),crypto=require('crypto'),sharp=require('sharp');
const out=path.resolve(__dirname,'..'),recordPath=path.join(out,'generation-20261021-buttons.json');
const rec=JSON.parse(fs.readFileSync(recordPath,'utf8'));
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const smooth=t=>t*t*(3-2*t);
// Match the AI fill to the unchanged source at the patch boundary.
// The harmonic correction preserves the AI surface gradients without a rectangular color seam.
function boundaryMatch(orig,patch,region){
 const [x0,y0,x1,y1]=region,w=x1-x0,h=y1-y0,cor=new Float64Array(w*h*3);
 for(let y=0;y<h;y++)for(let x=0;x<w;x++)if(x===0||x===w-1||y===0||y===h-1){
  const p=((y+y0)*310+x+x0)*4,k=(y*w+x)*3;
  for(let c=0;c<3;c++)cor[k+c]=orig[p+c]-patch[p+c];
 }
 let iterations=0,residual=0;
 for(;iterations<2000;iterations++){
  residual=0;
  for(let y=1;y<h-1;y++)for(let x=1;x<w-1;x++){
   const k=(y*w+x)*3;
   for(let c=0;c<3;c++){
    const delta=1.85*((cor[k+c-3]+cor[k+c+3]+cor[k+c-w*3]+cor[k+c+w*3])/4-cor[k+c]);
    cor[k+c]+=delta; residual=Math.max(residual,Math.abs(delta));
   }
  }
  if(residual<0.0001)break;
 }
 return {cor,w,iterations,residual};
}
(async()=>{
 const audit=[];
 for(const gen of rec.generations){
  const spec=rec.specs.find(s=>s.id===gen.id),[x0,y0,x1,y1]=spec.region;
  const orig=await sharp(spec.input).ensureAlpha().raw().toBuffer();
  const patch=await sharp(gen.rawOutput).resize(310,144,{fit:'fill',kernel:'lanczos3'}).ensureAlpha().raw().toBuffer();
  const data=Buffer.from(orig),feather=4,match=boundaryMatch(orig,patch,spec.region);
  for(let y=y0;y<y1;y++)for(let x=x0;x<x1;x++){
   const p=(y*310+x)*4,d=Math.min(x-x0,y-y0,x1-1-x,y1-1-y);
   const w=smooth(Math.min(1,d/feather));
   for(let c=0;c<3;c++){
    const matched=Math.max(0,Math.min(255,patch[p+c]+match.cor[((y-y0)*match.w+x-x0)*3+c]));
    data[p+c]=Math.round(orig[p+c]*(1-w)+matched*w);
   }
  }
  const file=path.join(out,'button-'+gen.id+'-blank-0'+gen.variant+'.png');
  await sharp(data,{raw:{width:310,height:144,channels:4}}).png().toFile(file);
  const actual=await sharp(file).ensureAlpha().raw().toBuffer();
  let alphaChanged=0,outsideChanged=0,changed=0;
  for(let y=0;y<144;y++)for(let x=0;x<310;x++){
   const p=(y*310+x)*4;
   if(actual[p+3]!==orig[p+3])alphaChanged++;
   if(!actual.subarray(p,p+4).equals(orig.subarray(p,p+4))){
    changed++;
    if(x<x0||x>=x1||y<y0||y>=y1)outsideChanged++;
   }
  }
  const check={id:gen.id,variant:gen.variant,path:file,size:[310,144],mode:'RGBA',sha256:sha(file),
   textPatchRegion:spec.region,featherPixels:feather,boundaryColorMatch:{method:'harmonic boundary correction',iterations:match.iterations,residual:match.residual},alphaChangedPixels:alphaChanged,
   outsideTextRegionChangedPixels:outsideChanged,totalChangedPixels:changed,
   originalSha256:sha(spec.input),originalHashUnchanged:sha(spec.input)===spec.inputSha256};
  if(alphaChanged||outsideChanged||!check.originalHashUnchanged||!changed)throw new Error(JSON.stringify(check));
  audit.push(check);
 }
 fs.writeFileSync(path.join(out,'_qa','pixel-audit.json'),JSON.stringify(audit,null,2)+'\n');
 rec.candidates=audit;
 fs.writeFileSync(recordPath,JSON.stringify(rec,null,2)+'\n');
 const width=1120,height=640,cols=[110,450,790],rows=[76,268,460];
 const tiles=[],labels=[];
 rec.specs.forEach((s,i)=>labels.push('<text x="'+(cols[i]+155)+'" y="35" text-anchor="middle">'+s.label+'</text>'));
 ['原图','候选 01','候选 02'].forEach((t,i)=>labels.push('<text x="14" y="'+(rows[i]+78)+'">'+t+'</text>'));
 for(let row=0;row<3;row++)for(let col=0;col<3;col++){
  const s=rec.specs[col],input=row===0?s.input:path.join(out,'button-'+s.id+'-blank-0'+row+'.png');
  tiles.push({input,left:cols[col],top:rows[row]});
 }
 const label=Buffer.from('<svg width="'+width+'" height="'+height+'"><style>text{font-family:PingFang SC,sans-serif;font-size:19px;fill:#303846}</style>'+labels.join('')+'</svg>');
 tiles.push({input:label,top:0,left:0});
 await sharp({create:{width,height,channels:4,background:'#e9edf1'}}).composite(tiles).png().toFile(path.join(out,'candidates.png'));
 console.log(JSON.stringify(audit.map(({id,variant,alphaChangedPixels,outsideTextRegionChangedPixels,totalChangedPixels,originalHashUnchanged})=>({id,variant,alphaChangedPixels,outsideTextRegionChangedPixels,totalChangedPixels,originalHashUnchanged})),null,2));
})().catch(e=>{console.error(e);process.exit(1)});


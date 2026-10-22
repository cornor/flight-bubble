const fs=require('fs'),path=require('path'),sharp=require('sharp'),crypto=require('crypto');
const qa=__dirname,del=path.resolve(qa,'../..'),req=JSON.parse(fs.readFileSync(path.join(qa,'request.json'))),s=req.attempts.find(x=>x.selected);
(async()=>{
 for(const a of req.attempts)fs.copyFileSync(a.raw,path.join(del,'_originals','tap-hand-attempt-0'+a.attempt+'-20261022.png'));
 const {data,info}=await sharp(s.raw).ensureAlpha().raw().toBuffer({resolveWithObject:true});
 let l=info.width,t=info.height,r=0,b=0;
 for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>=16){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x+1);b=Math.max(b,y+1);}
 const crop={left:l-4,top:t-4,width:r-l+8,height:b-t+8};
 const art=await sharp(s.raw).extract(crop).resize(116,116,{fit:'fill',kernel:'lanczos3'}).png().toBuffer();
 const candidate=await sharp({create:{width:128,height:128,channels:4,background:'#00000000'}}).composite([{input:art,left:6,top:6}]).png().toBuffer();
 fs.writeFileSync(path.join(qa,'bar-icon-tap-candidate.png'),candidate);
 const b4=path.join(del,'_originals/tap-with-bubble-20261022.png');
 const small=await sharp(candidate).resize(30,30).png().toBuffer();
 fs.writeFileSync(path.join(qa,'tap-30px.png'),small);
 const svg=Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="640" height="260"><style>text{font-family:PingFang SC,Arial,sans-serif;fill:#355072}</style><text x="24" y="35" font-size="23">点击图标 · 去蓝球</text><text x="28" y="225" font-size="17">原图 128 px</text><text x="220" y="225" font-size="17">新图 128 px</text><text x="430" y="225" font-size="17">30 px 实际显示</text></svg>');
 const dark=Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="70" height="70"><rect width="70" height="70" rx="10" fill="#233959"/></svg>');
 await sharp({create:{width:640,height:260,channels:4,background:'#EFF4FA'}}).composite([{input:b4,left:24,top:70},{input:candidate,left:216,top:70},{input:small,left:430,top:116},{input:dark,left:500,top:96},{input:small,left:520,top:116},{input:svg,left:0,top:0}]).png().toFile(path.join(qa,'comparison.png'));
 fs.writeFileSync(path.join(qa,'export.json'),JSON.stringify({selected_attempt:2,source_size:[info.width,info.height],crop,content_box:[6,6,122,122],final_size:[128,128],operation:'Crop transparent padding; normalize artwork to 116x116; place centered on 128x128 RGBA. Preserve source colors and alpha.',sha256:crypto.createHash('sha256').update(candidate).digest('hex')},null,2)+'\n');
 console.log('Candidate and 30px comparison exported.');
})().catch(e=>{console.error(e);process.exit(1)});


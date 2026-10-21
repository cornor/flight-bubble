const fs=require('fs'),sharp=require('sharp'),path=require('path');
const Q=__dirname,items=JSON.parse(fs.readFileSync(path.join(Q,'alignment.json'))),audit=JSON.parse(fs.readFileSync(path.join(Q,'pixel-audit.json')));
(async()=>{
 const W=1200,H=1130,layers=[],texts=['<text x="24" y="38" font-size="25">鼓腮蓄势 · 22 色角色</text>','<text x="24" y="65" font-size="15">224 × 256 · ready.png · 身体轮廓及透明通道与 idle 完全一致</text>'];
 const mouthLayers=[],mouthTexts=[];
 for(let i=0;i<items.length;i++){
  const s=items[i],p=audit[i].path,x=8+(i%5)*240,y=88+Math.floor(i/5)*207;
  layers.push({input:await sharp(p).extract({left:0,top:56,width:224,height:164}).png().toBuffer(),left:x,top:y});
  texts.push('<text x="'+(x+112)+'" y="'+(y+190)+'" text-anchor="middle" font-size="15">'+s.displayName+' · '+s.hex+'</text>');
  const [mx,my]=s.mouth,cx=12+(i%5)*184,cy=30+Math.floor(i/5)*178;
  mouthLayers.push({input:await sharp(p).extract({left:mx-20,top:my-17,width:40,height:34}).resize(160,136,{kernel:'nearest'}).png().toBuffer(),left:cx,top:cy});
  mouthTexts.push('<text x="'+cx+'" y="'+(cy-7)+'" font-size="12">'+s.key+'</text>');
  const ax=cx+80,ay=cy+68;
  mouthTexts.push('<path d="M '+(ax-14)+' '+ay+' h 8 M '+(ax+6)+' '+ay+' h 8 M '+ax+' '+(ay-14)+' v 8 M '+ax+' '+(ay+6)+' v 8" stroke="#ee3b48" fill="none"/>');
 }
 layers.push({input:Buffer.from('<svg width="'+W+'" height="'+H+'"><style>text{font-family:PingFang SC,sans-serif;fill:#25364b}</style>'+texts.join('')+'</svg>'),left:0,top:0});
 await sharp({create:{width:W,height:H,channels:4,background:'#e8eef3'}}).composite(layers).png().toFile(path.join(Q,'ready-overview.png'));
 const MW=930,MH=910;mouthLayers.push({input:Buffer.from('<svg width="'+MW+'" height="'+MH+'"><style>text{font-family:sans-serif;fill:#25364b}</style>'+mouthTexts.join('')+'</svg>'),left:0,top:0});
 await sharp({create:{width:MW,height:MH,channels:4,background:'#e8eef3'}}).composite(mouthLayers).png().toFile(path.join(Q,'mouth-check.png'));
 console.log('Overview and mouth-position proof saved.');
})().catch(e=>{console.error(e);process.exit(1)});


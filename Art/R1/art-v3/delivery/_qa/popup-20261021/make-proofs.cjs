const fs=require('fs'),path=require('path'),sharp=require('sharp');
const qa=__dirname,del=path.resolve(qa,'../..'),out=path.join(del,'ui4');
const names=['music','sound','light','pattern'],labels=['音乐','音效','光效','新版关卡图案'];
function svg(w,h,body){return Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="'+w+'" height="'+h+'"><style>text{font-family:PingFang SC,Heiti SC,Arial,sans-serif}</style>'+body+'</svg>');}
function text(x,y,s,size=20,fill='#29466A'){return '<text x="'+x+'" y="'+y+'" font-size="'+size+'" fill="'+fill+'">'+s+'</text>';}
async function sliced(w,h,destCap=24){
 const input=await sharp(path.join(out,'panel-card.png')).resize(270,270).png().toBuffer();
 const coords=[0,36,234,270],dx=[0,destCap,w-destCap,w],dy=[0,destCap,h-destCap,h],parts=[];
 for(let iy=0;iy<3;iy++)for(let ix=0;ix<3;ix++){
  parts.push({input:await sharp(input).extract({left:coords[ix],top:coords[iy],width:coords[ix+1]-coords[ix],height:coords[iy+1]-coords[iy]}).resize(dx[ix+1]-dx[ix],dy[iy+1]-dy[iy],{fit:'fill'}).png().toBuffer(),left:dx[ix],top:dy[iy]});
 }
 return sharp({create:{width:w,height:h,channels:4,background:'#00000000'}}).composite(parts).png().toBuffer();
}
(async()=>{
 const comps=[{input:path.join(out,'panel-card.png'),left:32,top:104}];
 let typography=text(32,48,'弹框与设置图标',30)+text(32,78,'透明 PNG · 内置 imagegen · 2026-10-21',16,'#6D829C')+text(32,492,'panel-card.png · 360 × 360',18)+text(32,520,'奶白卡片 / 淡蓝细边 / 柔和投影',16,'#6D829C')+text(444,124,'图标原尺寸 128 × 128',18)+text(444,345,'缩小到游戏内的 32 × 32',18);
 for(let i=0;i<4;i++){
  const x=444+i*160,p=path.join(out,'icon-'+names[i]+'.png');
  comps.push({input:p,left:x,top:150});
  typography+=text(x,310,labels[i],18);
  const small=await sharp(p).resize(64,64).resize(32,32).png().toBuffer();
  comps.push({input:small,left:x+48,top:370});
  comps.push({input:svg(128,72,'<rect width="128" height="72" rx="12" fill="#273C60"/>'),left:x,top:428});
  comps.push({input:small,left:x+48,top:448});
 }
 typography+=text(444,530,'浅色与深色底对照（下排图标仍为 32 px）',16,'#6D829C');
 comps.push({input:svg(1120,570,typography),left:0,top:0});
 await sharp({create:{width:1120,height:570,channels:4,background:'#EFF3F8'}}).composite(comps).png().toFile(path.join(qa,'overview.png'));
 const p1=await sliced(440,402),p2=await sliced(440,370);
 fs.writeFileSync(path.join(qa,'panel-stretched-440x402.png'),p1);fs.writeFileSync(path.join(qa,'panel-stretched-440x370.png'),p2);
 const parts=[{input:p1,left:28,top:102},{input:p2,left:492,top:102}];
 let cap=text(28,42,'九宫格拉伸预览',26,'#F5F9FF')+text(28,72,'按现有代码：入图集 270 × 270，四边切 36 px，显示端头 24 px',16,'#B8CBE4');
 cap+=text(60,154,'设置图标在白卡片上',24)+text(522,154,'较矮弹框 · 留白检查',24);
 for(let i=0;i<4;i++){
  const small=await sharp(path.join(out,'icon-'+names[i]+'.png')).resize(64,64).resize(32,32).png().toBuffer();
  parts.push({input:small,left:66,top:184+i*62});cap+=text(116,209+i*62,labels[i],21);
 }
 cap+=text(28,540,'440 × 402 · 素材效果预览',18,'#C6D6EC')+text(492,540,'440 × 370 · 素材效果预览',18,'#C6D6EC');
 parts.push({input:svg(960,574,cap),left:0,top:0});
 await sharp({create:{width:960,height:574,channels:4,background:'#243656'}}).composite(parts).png().toFile(path.join(qa,'nine-slice-review.png'));
 console.log('Created overview.png, nine-slice-review.png, two transparent stretch proofs.');
})().catch(e=>{console.error(e);process.exit(1)});


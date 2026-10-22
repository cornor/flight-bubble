const fs=require('fs'),path=require('path'),sharp=require('sharp'),crypto=require('crypto');
const dir=__dirname,root=path.resolve(dir,'../../..'),items=JSON.parse(fs.readFileSync(path.join(dir,'generations.json')));
const shot=path.join(root,'Builds/R1-evidence/lose-ui-web-20261022-two-buttons/battle-new-icons.png');
const fontfile=path.join(root,'Art/fonts/ChillRoundF/ChillRoundFBold.ttf');
const notes={A:'去掉大外框；单层细条；数字收在条内。',B:'奶白底板与轻凹槽；和新弹框统一。',C:'蓝紫填充与固定泡泡读数；主题感更强。'};
function svg(w,h,s){return Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="'+w+'" height="'+h+'"><style>text{font-family:PingFang SC,Arial,sans-serif}</style>'+s+'</svg>')}
function tx(x,y,s,size=22,c='#183D68',weight=400){return '<text x="'+x+'" y="'+y+'" font-size="'+size+'" fill="'+c+'" font-weight="'+weight+'">'+s+'</text>'}
async function glyph(text,size,color){return sharp({text:{text:'<span foreground="'+color+'">'+text+'</span>',fontfile,font:'ChillRoundF Bold '+size,rgba:true,dpi:72}}).png().toBuffer();}
(async()=>{
 const bg=await sharp(path.join(root,'UnityProject/Assets/Flightbeat/Resources/GardenR1/background.png')).resize(1080,1920).extract({left:352,top:24,width:680,height:92}).png().toBuffer();
 const base=await sharp(shot).composite([{input:bg,left:352,top:24}]).png().toBuffer();
 fs.writeFileSync(path.join(dir,'_qa/current-source.png'),fs.readFileSync(shot));
 const baseHash=crypto.createHash('sha256').update(fs.readFileSync(shot)).digest('hex'),logs=[];
 for(const s of items){
  fs.copyFileSync(s.raw,path.join(dir,'_originals',s.file+'.png'));
  const {data,info}=await sharp(s.raw).ensureAlpha().raw().toBuffer({resolveWithObject:true});
  let l=info.width,t=info.height,r=0,b=0;
  for(let y=0;y<info.height;y++)for(let x=0;x<info.width;x++)if(data[(y*info.width+x)*4+3]>=16){l=Math.min(l,x);t=Math.min(t,y);r=Math.max(r,x+1);b=Math.max(b,y+1);}
  const crop={left:l-4,top:t-4,width:r-l+8,height:b-t+8};
  const art=await sharp(s.raw).extract(crop).resize(s.render[0],s.render[1],{fit:'fill',kernel:'lanczos3'}).png().toBuffer();
  fs.writeFileSync(path.join(dir,s.file+'-concept.png'),art);
  const y=s.id==='A'?44:s.id==='B'?32:30,parts=[{input:art,left:360,top:y}];
  const label=await glyph('68%',36,'#163E6D'),m=await sharp(label).metadata();
  const lx=Math.round((s.id==='B'?958:950)-m.width/2),ly=Math.round(69-m.height/2);
  if(s.id==='A'){
   const shadow=await glyph('68%',36,'#FFFFFF');
   for(const [dx,dy] of [[-2,0],[2,0],[0,-2],[0,2],[-1,-1],[1,-1],[-1,1],[1,1]])parts.push({input:shadow,left:lx+dx,top:ly+dy});
  }
  parts.push({input:label,left:lx,top:ly});
  const full=await sharp(base).composite(parts).png().toBuffer();
  fs.writeFileSync(path.join(dir,s.file+'-in-game.png'),full);
  const header=await sharp(full).extract({left:0,top:0,width:1080,height:230}).png().toBuffer();
  fs.writeFileSync(path.join(dir,s.file+'-header.png'),header);
  logs.push({id:s.id,name:s.name,sourceSize:[info.width,info.height],crop,render:s.render,position:[360,y],label:'68%',labelPosition:[lx,ly],file:s.file+'-in-game.png',sha256:crypto.createHash('sha256').update(full).digest('hex')});
 }
 const boardParts=[],headerHeight=230,row=300;
 let titles=tx(20,40,'进度条 · 当前版与 3 个新方向',30,'#163E6D',600)+tx(20,70,'同一张游戏截图合成预览｜当前版保留实拍 0%；新设计均以 68% 示意',17,'#627C9B');
 const current=await sharp(shot).extract({left:0,top:0,width:1080,height:230}).png().toBuffer();
 const variants=[{id:'current',title:'当前版',note:'最新游戏截图：外框高光偏重，空槽与背景都偏蓝。',image:current},...items.map(s=>({id:s.id,title:s.id+' · '+s.name,note:notes[s.id],image:fs.readFileSync(path.join(dir,s.file+'-header.png'))}))];
 for(let i=0;i<4;i++){
  const top=90+i*row;
  titles+=tx(20,top+30,variants[i].title,24,'#163E6D',600)+tx(i===0?160:280,top+28,variants[i].note,17,'#627C9B');
  boardParts.push({input:variants[i].image,left:20,top:top+48});
 }
 boardParts.push({input:svg(1120,1300,titles),left:0,top:0});
 await sharp({create:{width:1120,height:1300,channels:4,background:'#F0F4FA'}}).composite(boardParts).png().toFile(path.join(dir,'comparison-header.png'));
 const fullParts=[];let fullTitles='';
 for(let i=0;i<items.length;i++){
  const s=items[i];
  fullParts.push({input:await sharp(path.join(dir,s.file+'-in-game.png')).resize(540,960).png().toBuffer(),left:i*560+10,top:92});
  fullTitles+=tx(i*560+20,38,s.id+' · '+s.name,27,'#163E6D',600)+tx(i*560+20,69,notes[s.id],18,'#627C9B');
 }
 fullParts.push({input:svg(1680,1070,fullTitles),left:0,top:0});
 await sharp({create:{width:1680,height:1070,channels:4,background:'#F0F4FA'}}).composite(fullParts).png().toFile(path.join(dir,'comparison-full.png'));
 fs.writeFileSync(path.join(dir,'_qa/export.json'),JSON.stringify({originalScreenshot:shot,originalScreenshotSha256:baseHash,compositeNote:'New imagegen meter, live-style text overlaid using game font; current background texture restores only old meter bounding rectangle. Concept preview, not live game capture. Fill proportions are illustrative.',restoredRectangle:[352,24,680,92],variants:logs},null,2)+'\n');
 console.log('Saved 3 transparent concepts, 3 full-size in-context previews, header and full comparisons.');
})().catch(e=>{console.error(e);process.exit(1)});


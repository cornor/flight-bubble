import fs from 'node:fs';
import {connect} from '../../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL;
fs.mkdirSync(out,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms)),errors=[],checks=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT),onEvent:e=>{if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(f,label){for(let n=0;n<180;n++){let m;try{m=await sample();}catch{}if(m&&f(m))return m;await sleep(500);}throw Error(label);}
async function tap(x,y,space='battle'){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y};let py=y;if('${space}'==='modal'||'${space}'==='home')py=y+(h-960)/2;else if('${space}'==='battle'){const a=[0,60,114,574,647,688,721,855,880,960],b=[0,54,70,444,562,600,661,843,880,960],at=i=>3+b[i]+(h-966)*Math.max(0,Math.min(1,(b[i]-60)/396));let i=0;while(i<a.length-2&&y>a[i+1])i++;py=at(i)+(at(i+1)-at(i))*(y-a[i])/(a[i+1]-a[i]);}return {x:r.left+${x}*s,y:r.top+py*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await sleep(70);await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});await sleep(300);
}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}

async function open(level){
 await c.call('Page.navigate',{url:base+'?set=A&level='+level});await sleep(1200);
 await until(m=>m.level===level-1&&!m.home&&!m.transition&&m.phase===1,'Ready '+level);await sleep(1800);
}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 await c.call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:2,mobile:false});
 await open(3);await shot('third-ready');
 await tap(82,730);await until(m=>m.phase===2&&m.fleet>0,'Deploy');
 await sleep(1800);await tap(497,875);await until(m=>m.selection===2,'Bomb aim');
 await tap(220,350);await shot('bomb-aim');
 await tap(497,875);await until(m=>m.selection===0,'Bomb cancel');await shot('bomb-cancel');
 await tap(497,875);await until(m=>m.selection===2,'Bomb reopen');await tap(300,420);
 await tap(270,774);await until(m=>m.selection===0&&!m.paused,'Bomb confirm');await sleep(200);await shot('bomb-impact');
 await sleep(1500);await shot('bomb-fall');checks.push('Bomb aim, cancel, confirm and falling after explosion');
 let won=false,deploys=0;const started=Date.now();
 while(Date.now()-started<180000){
  const m=await sample();fs.appendFileSync(out+'/samples.jsonl',JSON.stringify(m)+'\n');
  if(m.phase===4){won=true;await sleep(1500);await shot('third-win');break;}
  if(m.phase===3||m.phase===5)throw Error('Unexpected loss/revive');
  if(m.finaleGuide){await tap(270,760);}
  else if(m.finaleReady){await tap(270,759);}
  else if(!m.paused&&!m.transition){
   const q=m.lanes.map((l,i)=>({...l,i})).filter(l=>l.color>=0);
   const chosen=q.find(l=>l.merge&&l.exposed)||q.find(l=>l.merge)||q.find(l=>l.exposed&&!m.fleetColors.includes(l.color))||q.find(l=>l.exposed)||(m.fleet<5?q[0]:null);
   if(chosen){await tap(20+(chosen.i+.5)*500/m.lanes.length,730);deploys++;}
  }
  if(deploys===4)await shot('third-playing');
  await sleep(400);
 }
 if(!won)throw Error('Did not finish level');checks.push('Third level complete through real queue clicks: '+deploys);
 await open(4);let before=await sample();await tap(82,730);await until(m=>m.phase===2,'Stream-level deployment');await sleep(4000);await shot('fourth-stream');
 let after=await sample();if(after.remaining>=before.remaining)throw Error('Stream-level shooting did not remove beads');checks.push('Streamed fourth-level combat');
 await tap(34,28,'screen');await until(m=>m.settings,'Pause');await shot('paused');
 await tap(100,676,'modal');await until(m=>m.home&&!m.transition,'Home');await shot('home-board-hidden');checks.push('Pause and home lifecycle');
 await open(3);await c.call('Emulation.setDeviceMetricsOverride',{width:768,height:1024,deviceScaleFactor:2,mobile:false});await sleep(1600);await shot('resized-tablet');
 await c.call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:2,mobile:false});await sleep(1600);await shot('resized-phone');
 await tap(82,730);await until(m=>m.phase===2,'Resize preserves input');checks.push('Live resize and deployment');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors},null,2));console.log(JSON.stringify({passed:true,checks}));
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

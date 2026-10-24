// P1 acceptance through real pointer input and read-only game telemetry.
import fs from 'node:fs';
import { connect } from '../../tools/browser/cdp.mjs';
const out=process.argv[2];
if(!out||fs.existsSync(out))throw Error('Fresh evidence directory required');
fs.mkdirSync(out,{recursive:true});
const events=[],checks=[],pause=ms=>new Promise(r=>setTimeout(r,ms));
const c=await connect({port:Number(process.env.R1_CDP_PORT||9344),onEvent:e=>{
 if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))events.push(e);
}});
const base=process.env.R1_URL||'http://127.0.0.1:8804/';
const sample=()=>c.evaluate('window.gardenR1');
async function until(predicate,label,seconds=15){
 for(let i=0;i<seconds*2;i++){let m;try{m=await sample();}catch{}if(m&&predicate(m))return m;await pause(500);}
 throw Error(label);
}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
async function tap(x,y,modal=false){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,e=h-966,y=${y},py=${modal}?y+(h-960)/2:y+3+e*Math.max(0,Math.min(1,(y-60)/396));return {x:r.left+${x}*s,y:r.top+py*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});
 await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await pause(70);
 await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});
 fs.appendFileSync(out+'/actions.jsonl',JSON.stringify({x,y,modal,...p,at:Date.now()})+'\n');
}
async function open(level,width=540,height=960){
 await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});
 await c.call('Page.navigate',{url:base+'?set=A&level='+level});
 await until(m=>m.level===level-1&&!m.home&&!m.transition&&m.phase!==0,'Level failed to open',90);
 await pause(1500); const m=await sample();if(!m.plushTheme)throw Error('Plush theme not active');
 return m;
}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 const initial=await open(3);await shot('level-3-ready');
 await tap(36,30);await until(m=>m.settings&&m.paused,'Pause failed');await shot('level-3-settings');
 const muted=(await sample()).musicMuted;await tap(400,318,true);await until(m=>m.musicMuted!==muted,'Music toggle failed');
 await tap(400,318,true);await until(m=>m.musicMuted===muted,'Music restore failed');
 await tap(270,779,true);await until(m=>!m.settings&&!m.paused,'Resume failed');checks.push('pause / music toggle / resume');
 await tap(82,650);await until(m=>m.phase===2&&m.fleet>0,'Initial deployment failed');checks.push('first card deployment');
 await tap(104,914);await pause(1600);await shot('level-3-shuffle');
 await tap(270,914);await until(m=>m.selection===1,'Priority selection failed');await shot('level-3-priority');
 await tap(82,820);await until(m=>m.selection===0&&!m.paused,'Priority rear-card hit failed');checks.push('priority rear-card selection');
 await tap(434,914);await until(m=>m.selection===2,'Bomb aiming failed');
 await tap(270,300);await pause(500);await shot('level-3-bomb-aim');
 await tap(270,740);await until(m=>m.selection===0,'Bomb confirmation failed');checks.push('bomb target / confirm');
 let won=false,deploys=0,battleShot=false;const start=Date.now();
 while(Date.now()-start<240000){
  const m=await sample();fs.appendFileSync(out+'/samples.jsonl',JSON.stringify(m)+'\n');
  if(m.phase===4){won=true;await pause(1600);await shot('level-3-win');break;}
  if(m.phase===5)throw Error('Sample level lost');
  if(m.phase===3){await shot('level-3-revive');throw Error('Unexpected revive');}
  if(m.finaleReady)await tap(270,738);
  else if(!m.paused&&!m.transition){
   const candidates=m.lanes.map((l,i)=>({...l,i})).filter(l=>l.color>=0);
   const chosen=candidates.find(l=>l.merge&&l.exposed)||candidates.find(l=>l.merge)||candidates.find(l=>l.exposed&&!m.fleetColors.includes(l.color))||candidates.find(l=>l.exposed)||(m.fleet<5?candidates[0]:null);
   if(chosen){await tap(20+(chosen.i+.5)*500/m.lanes.length,650);deploys++;}
  }
  if(!battleShot&&m.phase===2&&m.fleet>=3){await shot('level-3-battle');battleShot=true;}
  await pause(750);
 }
 if(!won)throw Error('Sample level did not finish');checks.push('level 3 victory using real queue clicks');
 const tall=await open(3,390,844);await shot('level-3-tall');await tap(82,650);
 await until(m=>m.phase===2&&m.fleet>0,'Tall-screen deployment failed');checks.push('390 x 844 queue tap');
 const dense=await open(7);await shot('five-lanes-eight-slots');
 if(dense.lanes.length!==5||dense.maximumSlots!==8)throw Error('Density fixture mismatch');
 await tap(70,650);await until(m=>m.phase===2&&m.fleet>0,'Five-lane deployment failed');await shot('five-lanes-deployed');checks.push('five lanes / eight slots');
 if(events.length)throw Error('Browser error');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,deploys,initial,tall,dense,events},null,2));
 console.log(JSON.stringify({passed:true,checks,deploys}));
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,events,last:await sample().catch(()=>null)},null,2));throw e;}
finally{c.close();}

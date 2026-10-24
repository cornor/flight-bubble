// P2–P4 regression: real pointers, published level URLs and read-only telemetry.
import fs from 'node:fs';
import {connect} from '../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL||'http://127.0.0.1:8804/';
if(!out||fs.existsSync(out))throw Error('Fresh evidence directory required');
fs.mkdirSync(out,{recursive:true});
const events=[],checks=[],results=[],pause=ms=>new Promise(r=>setTimeout(r,ms));
const c=await connect({port:Number(process.env.R1_CDP_PORT||9344),onEvent:e=>{
 if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))events.push(e);
}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(predicate,label,seconds=30){
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
 await c.call('Page.navigate',{url:base+(level?'?set=A&level='+level:'?set=A')});
 await until(m=>level?m.level===level-1&&!m.home&&!m.transition&&m.phase>0:m.home,'Page failed to open',90);
 await pause(1500);return sample();
}
async function play(level){
 const start=Date.now();let maxTier=0,maxFleet=0,revives=0,finale=false;
 while(Date.now()-start<240000){
  const m=await sample();fs.appendFileSync(out+'/samples.jsonl',JSON.stringify(m)+'\n');
  maxTier=Math.max(maxTier,m.maxTier);maxFleet=Math.max(maxFleet,m.fleet);
  if(m.phase===4){await pause(1600);await shot(`level-${level}-win`);results.push({level,won:true,maxTier,maxFleet,revives,finale});return;}
  if(m.phase===5)throw Error(`Level ${level} lost`);
  if(m.phase===3){await shot(`level-${level}-revive`);await tap(270,700,true);await until(m=>m.phase===2,'Revive failed');revives++;}
  else if(m.finaleGuide)await tap(270,740);
  else if(m.finaleReady){
   await shot(`level-${level}-finale-ready`);await tap(270,738);await pause(1100);await shot(`level-${level}-king-ready`);
   await pause(700);await shot(`level-${level}-king-blow`);finale=true;
  }else if(!m.paused&&!m.transition){
   const lanes=m.lanes.map((l,i)=>({...l,i})).filter(l=>l.color>=0);
   const l=lanes.find(l=>l.merge&&l.exposed)||lanes.find(l=>l.merge)||lanes.find(l=>l.exposed&&!m.fleetColors.includes(l.color))||lanes.find(l=>l.exposed)||(m.fleet<m.unlockedSlots?lanes[0]:null);
   if(l)await tap(20+(l.i+.5)*500/m.lanes.length,650);
  }
  if(m.maxTier===3&&!fs.existsSync(`${out}/max-tier.png`))await shot('max-tier');
  await pause(1100);
 }
 throw Error(`Level ${level} timed out`);
}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 // A fresh home has no Battle object, so gameplay telemetry starts only after one
 // published level has opened. Return through the real pause/home button first.
 await open(3);await tap(36,30);await until(m=>m.settings,'Initial pause failed');
 await tap(98,676,true);await until(m=>m.home&&!m.transition,'Initial home failed');await shot('home');
 await tap(35,30,true);await until(m=>m.settings,'Home settings failed');await shot('home-settings');
 const initial=await sample();await tap(270,418,true);await until(m=>m.soundMuted!==initial.soundMuted,'Sound toggle failed');await shot('home-settings-off');
 await tap(270,418,true);await until(m=>m.soundMuted===initial.soundMuted,'Sound restore failed');
 await tap(270,690,true);await until(m=>!m.settings,'Home settings close failed');checks.push('home settings / sound toggle');
 await tap(498,438,true);await until(m=>m.codexOpen,'Codex open failed');await shot('codex-locked');
 await tap(470,852,true);await until(m=>m.codexSelection===19,'Codex last card failed');await shot('codex-last-card');
 await tap(500,44,true);await until(m=>!m.codexOpen,'Codex return failed');checks.push('codex locked card / selection / close');
 await tap(270,790,true);await until(m=>!m.home&&!m.transition,'Home start failed');await shot('tutorial-start');checks.push('home start into tutorial');
 await open(7);await shot('sector05-ready');
 const dense=await sample();if(dense.lanes.length!==5||dense.maximumSlots!==8)throw Error('Sector05 fixture mismatch');
 await tap(433,550);await pause(1500);await tap(498,550);await until(m=>m.unlockedSlots===8,'Both extra bays failed');await shot('sector05-eight-open');
 await tap(36,30);await until(m=>m.settings&&m.paused,'Pause failed');await shot('pause');
 const b=await sample();await pause(1200);if(Math.abs((await sample()).battleTime-b.battleTime)>.03)throw Error('Pause did not freeze game');
 await tap(270,779,true);await until(m=>!m.settings&&!m.paused,'Resume failed');checks.push('eight bays / pause / resume');
 await tap(70,650);await until(m=>m.fleet>0,'Deployment failed');
 await tap(104,914);await pause(1500);await shot('shuffle');
 await tap(270,914);await until(m=>m.selection===1,'Priority failed');await shot('priority');
 await tap(70,820);await until(m=>m.selection===0,'Priority rear card failed');
 await tap(434,914);await until(m=>m.selection===2,'Bomb aiming failed');await tap(270,300);await pause(300);await shot('bomb-aim');
 await tap(270,740);await until(m=>m.selection===0,'Bomb confirm failed');checks.push('shuffle / rear-card priority / bomb');
 await play(7);checks.push('sector05 complete / merge / finale / victory');
 await tap(108,782,true);await until(m=>m.home&&!m.transition,'Victory home failed');await shot('home-after-win');
 await tap(498,438,true);await until(m=>m.codexOpen,'Codex reopen failed');await shot('codex-unlocked');checks.push('victory home / new member codex');
 await open(11);const hidden=await sample();if(!hidden.hiddenReserve)throw Error('Hidden fixture mismatch');await shot('mystery-bags');
 await tap(332,650);await pause(1500);await shot('mystery-revealed');checks.push('hidden identity / front reveal');
 await open(7,390,844);await shot('sector05-tall');await tap(70,650);await until(m=>m.fleet>0,'Tall deployment failed');
 await tap(36,30);await until(m=>m.settings,'Tall pause failed');await shot('pause-tall');await tap(98,676,true);await until(m=>m.home&&!m.transition,'Pause home failed');await pause(600);
 await shot('home-tall');await tap(35,30);await until(m=>m.settings,'Tall home settings failed');await shot('home-settings-tall');
 await tap(270,690,true);await until(m=>!m.settings,'Tall settings close failed');
 await tap(498,438,true);await until(m=>m.codexOpen,'Tall codex failed');await shot('codex-tall');checks.push('390x844 gameplay / pause / home / settings / codex');
 await c.call('Page.navigate',{url:base});
 let loaded=false;for(let i=0;i<120;i++){if(await c.evaluate('document.querySelector("#loading")?.classList.contains("out")||!document.querySelector("#loading")')){loaded=true;break;}await pause(500);}
 if(!loaded)throw Error('Default URL loading failed');await pause(1200);await shot('default-home-tall');checks.push('default URL load / home');
 if(events.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,results,events},null,2));console.log('PLUSH_FULL_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,results,events,last:await sample().catch(()=>null)},null,2));throw e;}
finally{c.close();}

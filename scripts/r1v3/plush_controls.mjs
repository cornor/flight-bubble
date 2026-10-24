// Focused UI acceptance: actual pointers, eight pads and two display proportions.
import fs from 'node:fs';
import {connect} from '../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL||'http://127.0.0.1:8804/';
if(!out||fs.existsSync(out))throw Error('Fresh evidence directory required');
fs.mkdirSync(out,{recursive:true});
const checks=[],errors=[],pause=ms=>new Promise(r=>setTimeout(r,ms));
const c=await connect({port:Number(process.env.R1_CDP_PORT||9344),onEvent:e=>{
 if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);
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
}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 for(const [width,height,label] of [[540,960,'standard'],[390,844,'tall']]){
  await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});
  await c.call('Page.navigate',{url:base+'?set=A&level=7'});
  await until(m=>m.level===6&&!m.home&&!m.transition&&m.phase===1,'Level 7 ready',90);await pause(1200);
  await shot(`${label}-ready`);
  const before=await sample();if(before.lanes.length!==5||before.maximumSlots!==8)throw Error('Unexpected slot fixture');
  await tap(433,550);await until(m=>m.unlockedSlots===7,'First bay unlock');await pause(1400);await shot(`${label}-seven-open`);
  await tap(498,550);await until(m=>m.unlockedSlots===8,'Second bay unlock');await pause(1400);
  await tap(70,650);await until(m=>m.fleet>0,'First deployment');await pause(800);
  await tap(170,650);await pause(800);await shot(`${label}-occupied`);
  checks.push(`${width}x${height}: five queue lanes / both slot unlocks / deployment`);
  await tap(104,914);await pause(1200);await shot(`${label}-shuffle`);
  await tap(270,914);await until(m=>m.selection===1,'Priority button');await shot(`${label}-priority`);
  await tap(70,820);await until(m=>m.selection===0,'Priority queue selection');
  await tap(434,914);await until(m=>m.selection===2,'Bomb button');await tap(270,300);await shot(`${label}-bomb-confirm`);
  await tap(270,740);await until(m=>m.selection===0,'Bomb confirm');
  checks.push(`${width}x${height}: shuffle / priority / bomb aiming and confirmation`);
  await tap(36,30);await until(m=>m.settings&&m.paused,'Battle pause');await shot(`${label}-pause`);
  const stopped=await sample();await pause(1000);if(Math.abs((await sample()).battleTime-stopped.battleTime)>.03)throw Error('Pause time advanced');
  await tap(270,779,true);await until(m=>!m.settings&&!m.paused,'Continue game');
  await tap(36,30);await until(m=>m.settings,'Reopen pause');
  await tap(270,677,true);await until(m=>!m.settings&&!m.transition&&m.phase===1,'Restart from new action row');await shot(`${label}-retry`);
  await tap(36,30);await until(m=>m.settings,'Pause after restart');
  await tap(98,676,true);await until(m=>m.home&&!m.transition,'Return home');await pause(700);await shot(`${label}-home`);
  // The gear is anchored to the safe top edge, unlike centered modal controls.
  await tap(35,30);await until(m=>m.settings,'Settings gear');await shot(`${label}-settings`);
  for(const [key,y] of [['musicMuted',318],['soundMuted',416],['lightEnabled',514]]){
   const initial=await sample();await tap(400,y,true);await until(m=>m[key]!==initial[key],key+' toggle');await shot(`${label}-${key}-changed`);
   await tap(400,y,true);await until(m=>m[key]===initial[key],key+' restore');
  }
  await tap(482,215,true);await until(m=>!m.settings,'Settings close cross');
  await tap(35,30);await until(m=>m.settings,'Settings reopen');
  await tap(270,690,true);await until(m=>!m.settings,'Settings done');
  checks.push(`${width}x${height}: pause freeze / continue / restart / home / all three toggles and restore / close cross / done`);
 }
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors},null,2));console.log('PLUSH_CONTROLS_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}
finally{c.close();}

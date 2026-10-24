// Final reference-layout review with real pointers and naturally triggered revival.
import fs from 'node:fs';
import {connect} from '../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL||'http://127.0.0.1:8804/';
if(!out||fs.existsSync(out))throw Error('Fresh evidence required');fs.mkdirSync(out,{recursive:true});
const pause=ms=>new Promise(r=>setTimeout(r,ms)),errors=[],checks=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT||9344),onEvent:e=>{if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(fn,label,seconds=90){for(let i=0;i<seconds*2;i++){let m;try{m=await sample();}catch{}if(m&&fn(m))return m;await pause(500);}throw Error(label);}
async function shot(n){fs.writeFileSync(`${out}/${n}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
async function tap(x,y,modal=false){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y};return {x:r.left+${x}*s,y:r.top+(${modal}?y+(h-960)/2:y+3+(h-966)*Math.max(0,Math.min(1,(y-60)/396)))*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await pause(70);await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});
}
async function viewport(width,height){await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});}
async function level(){await c.call('Page.navigate',{url:base+'?set=A&level=7'});await until(m=>m.level===6&&!m.home&&!m.transition&&m.phase===1,'Level ready');await pause(1000);}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 for(const [width,height,label] of [[540,960,'standard'],[390,844,'tall']]){
  await viewport(width,height);await level();await shot(`${label}-battle`);
  await tap(36,30);await until(m=>m.settings,'Pause');await shot(`${label}-pause`);
  await tap(98,676,true);await until(m=>m.home&&!m.transition,'Home');await pause(600);
  await tap(50,72);await until(m=>m.settings,'Home settings');await shot(`${label}-settings`);
  const initial=await sample();await tap(400,416,true);await until(m=>m.soundMuted!==initial.soundMuted,'Sound off');await shot(`${label}-settings-off`);
  await tap(400,416,true);await until(m=>m.soundMuted===initial.soundMuted,'Sound restore');
  await tap(482,215,true);await until(m=>!m.settings,'Settings cross');checks.push(`${width}x${height} settings / sound toggle / close / battle buttons`);
 }
 await viewport(540,960);await level();await tap(170,650);await until(m=>m.phase===3,'Natural revive offer',180);await shot('revive');
 const offer=await sample();await viewport(390,844);await pause(1000);await shot('revive-tall');
 await tap(270,700,true);const revived=await until(m=>m.phase===2,'Revive action');
 if(revived.unlockedSlots!==offer.unlockedSlots+1)throw Error('Revive slot reward');
 await pause(1500);if((await sample()).selection===2){await tap(270,740);await until(m=>m.selection===0,'Reward bomb confirm');}
 checks.push('revive heading and plush hero on both sizes / ad action / extra slot / reward bomb');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors},null,2));console.log('PLUSH_DIALOGS_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}
finally{c.close();}

import fs from 'node:fs';
import {connect} from '../../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL;
fs.mkdirSync(out,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms)),errors=[],checks=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT),onEvent:e=>{if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(f,label){for(let n=0;n<120;n++){let m;try{m=await sample();}catch{}if(m&&f(m))return m;await sleep(500);}throw Error(label);}
async function tap(x,y,modal=false){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y};return {x:r.left+${x}*s,y:r.top+(${modal}?y+(h-960)/2:y+3+(h-966)*Math.max(0,Math.min(1,(y-60)/396)))*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await sleep(70);await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});await sleep(300);
}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
async function viewport(width,height){await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});await sleep(1100);}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 await viewport(540,960);
 await c.call('Page.navigate',{url:base+'?set=A&level=7'});await until(m=>m.level===6&&!m.home&&!m.transition&&m.phase===1,'Ready');
 await tap(170,650);await until(m=>m.phase===3,'Natural revive offer');
 await tap(482,232,true);const lost=await until(m=>m.phase===5,'Decline produces normal loss');await sleep(1900);await shot('standard-loss');
 for(const [w,h,label] of [[540,960,'standard'],[390,844,'tall']]){
  await viewport(w,h);await shot(label+'-loss');await tap(442,751,true);await sleep(1200);
  const still=await sample();if(still.phase!==5||still.level!==lost.level||still.wins!==lost.wins||still.dayAds!==lost.dayAds||still.home||still.transition)throw Error('Share placeholder changed game state');
  checks.push(`${w}x${h}: gray ribbon/plush hero/home/retry/share layout, share has no action`);
 }
 await tap(98,751,true);await until(m=>m.home&&!m.transition,'Loss home');await shot('loss-home');checks.push('Loss home button returns to the home page');
 await c.call('Page.navigate',{url:base+'?set=A&level=7'});await until(m=>m.level===6&&!m.home&&!m.transition&&m.phase===1,'Ready again');
 await tap(170,650);await until(m=>m.phase===3,'Second revive offer');await tap(482,232,true);await until(m=>m.phase===5,'Second loss');await sleep(1400);
 await tap(270,751,true);await until(m=>m.level===6&&m.phase===1&&!m.transition&&!m.home,'Retry same level');await shot('retry');
 checks.push('Retry starts the same level through the existing restart action');
 await tap(36,30);await until(m=>m.settings,'Pause');await tap(442,676,true);await until(m=>m.phase===4&&!m.settings,'Victory regression');await sleep(2000);await shot('victory-regression');
 await tap(108,782,true);await until(m=>m.home&&!m.transition,'Victory home');checks.push('Victory retains its red ribbon and original home/next/replay row');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors,adMode:'Existing test provider; no real video playback tested.'},null,2));console.log('LOSS_LAYOUT_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

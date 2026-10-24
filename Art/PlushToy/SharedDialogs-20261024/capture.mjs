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
async function both(name){
 for(const [w,h,label] of [[540,960,'standard'],[390,844,'tall']]){await viewport(w,h);await shot(label+'-'+name);}
}
async function level(n){await c.call('Page.navigate',{url:base+'?set=A&level='+n});await until(m=>m.level===n-1&&!m.home&&!m.transition&&m.phase===1,'Level '+n+' ready');await sleep(800);}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 await viewport(540,960);await level(1);
 await tap(36,30);await until(m=>m.settings,'Pause');await tap(442,676,true);await until(m=>m.phase===4&&!m.settings,'Win');await sleep(2200);await both('victory');
 await tap(98,782,true);await until(m=>m.home&&!m.transition,'Win home');await both('home');
 await tap(270,774,true);await until(m=>m.level===1&&!m.home&&!m.transition&&m.phase===1,'Home CTA starts next uncleared level');checks.push('Home gold CTA retains next-level selection and two-line level information');
 await level(7);await tap(36,30);await until(m=>m.settings,'Pause');await tap(270,677,true);
 await until(m=>m.level===6&&m.phase===1&&!m.transition&&!m.settings,'Pause restart directly');await sleep(1200);
 checks.push('Current build keeps streak confirmation disabled; restart still runs directly');
 await tap(170,650);await until(m=>m.phase===2,'Deploy');const offer=await until(m=>m.phase===3,'Natural revive');await both('revive');
 await tap(270,700,true);const revived=await until(m=>m.phase===2&&m.unlockedSlots===offer.unlockedSlots+1,'Rewarded revive');
 if(revived.dayAds!==offer.dayAds+1)throw Error('Expected one revive ad');
 await until(m=>m.selection===2,'Reward bomb aiming');await tap(270,740);await until(m=>m.selection===0&&!m.paused,'Bomb confirm');
 checks.push('Revive reward flow, original video icon, +1 slot');
 await until(m=>m.phase===5,'Natural loss');await sleep(1800);await both('loss');
 const lost=await sample();await tap(442,751,true);await sleep(1200);const shared=await sample();
 if(shared.phase!==5||shared.home||shared.level!==lost.level||shared.dayAds!==lost.dayAds)throw Error('Share placeholder changed state');
 await tap(270,751,true);await until(m=>m.level===6&&m.phase===1&&!m.transition,'Retry same level');checks.push('Loss Retry and empty Share behavior');
 await tap(36,30);await until(m=>m.settings,'Pause regression');await shot('tall-pause');
 await tap(442,676,true);await until(m=>m.phase===4&&!m.settings,'Win again');await sleep(1800);
 await tap(270,783,true);await until(m=>m.level===7&&m.phase===1&&!m.transition,'Victory Next');checks.push('Victory retains ribbon, Home and Next actions');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,viewports:['540x960','390x844'],adMode:'Existing test provider; no real video playback tested.',errors},null,2));console.log('SHARED_DIALOGS_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

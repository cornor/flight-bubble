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
 await viewport(540,960);await level(7);
 await tap(170,650);await until(m=>m.phase===3,'Natural revive offer');await tap(482,232,true);await until(m=>m.phase===5,'Close declines revive directly');await sleep(1800);await both('loss');
 const lost=await sample();await tap(442,751,true);await sleep(1200);const share=await sample();
 if(share.phase!==5||share.home||share.level!==lost.level||share.dayAds!==lost.dayAds)throw Error('Share placeholder changed state');
 await tap(270,751,true);await until(m=>m.level===6&&m.phase===1&&!m.transition,'Retry same level');await sleep(1200);checks.push('Revive close, loss Retry and empty Share behavior');
 await tap(36,30);await until(m=>m.settings,'Pause regression');await shot('tall-pause');
 await tap(442,676,true);await until(m=>m.phase===4&&!m.settings,'Win again');await sleep(1800);
 await tap(270,783,true);await until(m=>m.level===7&&m.phase===1&&!m.transition,'Victory Next');checks.push('Victory Next enters the following level');
 if(errors.length)throw Error('Browser errors');fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,viewports:['540x960','390x844'],adMode:'Existing test provider; no real video playback tested.',errors},null,2));console.log('REMAINING_DIALOGS_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

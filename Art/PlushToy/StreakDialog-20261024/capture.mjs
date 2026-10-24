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
 await tap(36,30);await until(m=>m.settings,'Pause');await tap(442,676,true);await until(m=>m.phase===4&&!m.settings,'Win establishes streak');await sleep(2200);
 await tap(432,782,true);await until(m=>m.level===6&&m.phase===1&&!m.transition,'Replay');
 await tap(36,30);await until(m=>m.settings,'Pause');await shot('pause-style');
 await tap(270,677,true);await sleep(700);await shot('standard-continue');
 await viewport(390,844);await shot('tall-continue');
 await tap(270,533,true);await sleep(700);await shot('returned-pause');
 await tap(270,779,true);await until(m=>!m.settings&&!m.paused&&m.phase===1,'Continue challenge returns via pause without restarting');
 checks.push('Ordinary streak confirmation: Continue returns to pause and resumes the current level');
 await tap(170,650);await until(m=>m.phase===2,'Combat');const before=await until(m=>m.phase===3,'Natural revive offer');
 await tap(482,232,true);await sleep(700);await shot('tall-revive');
 await viewport(540,960);await shot('standard-revive');
 await tap(270,533,true);const after=await until(m=>m.phase===2&&m.unlockedSlots===before.unlockedSlots+1,'Revive confirmation main action');
 if(after.dayAds!==before.dayAds+1)throw Error('Revive must request one rewarded ad');
 checks.push('Revive confirmation: primary action follows the existing rewarded-ad flow and unlocks one slot');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,viewports:['540x960','390x844'],adMode:'Existing test provider; no real video playback tested.',errors},null,2));console.log('STREAK_DIALOG_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

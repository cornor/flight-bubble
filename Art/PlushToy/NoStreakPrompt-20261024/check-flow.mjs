import fs from 'node:fs';
import {connect} from '../../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL;
fs.mkdirSync(out,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms)),errors=[],checks=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT),onEvent:e=>{if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(f,label){for(let n=0;n<180;n++){let m;try{m=await sample();}catch{}if(m&&f(m))return m;await sleep(500);}throw Error(label);}
async function tap(x,y,space='battle'){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y};let py=y;if('${space}'==='modal'||'${space}'==='home')py=y+(h-960)/2;else if('${space}'==='battle')py=y+3+(h-966)*Math.max(0,Math.min(1,(y-60)/396));return {x:r.left+${x}*s,y:r.top+py*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await sleep(70);await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});await sleep(300);
}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 await c.call('Emulation.setDeviceMetricsOverride',{width:540,height:960,deviceScaleFactor:1,mobile:false});
 async function winAndNext(level){
  await c.call('Page.navigate',{url:base+'?set=A&level='+level});
  await until(m=>m.level===level-1&&!m.home&&!m.transition&&m.phase===1,'Ready');
  await tap(36,30);await until(m=>m.settings,'Pause');
  await tap(442,676,'modal');await until(m=>m.phase===4&&m.rewardSettled,'Win creates streak');await sleep(1800);
  await tap(270,783,'modal');await until(m=>m.level===level&&!m.transition&&m.phase===1,'Next with streak');
 }
 await winAndNext(3);
 await tap(36,30);await until(m=>m.settings,'Pause next level');await tap(98,676,'modal');
 await until(m=>m.home&&!m.transition&&!m.settings,'Exit must directly return home');await shot('exit-direct-home');
 checks.push('Win level 3, enter level 4, pause and return home with one tap, no streak confirmation');
 await winAndNext(6);
 await tap(170,650);await until(m=>m.phase===3,'Natural revive offer');await shot('normal-revive-retained');
 await tap(482,232,'modal');await until(m=>m.phase===5,'Decline must directly lose');await sleep(1800);await shot('decline-direct-loss');
 checks.push('Win level 6, enter level 7, natural revive retained; decline directly reaches loss without streak confirmation');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors},null,2));console.log('NO_STREAK_PROMPT_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

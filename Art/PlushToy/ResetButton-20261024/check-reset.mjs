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
 await c.call('Page.navigate',{url:base+'?set=A&level=3'});
 await until(m=>m.level===2&&!m.home&&!m.transition&&m.phase===1,'Ready');
 await tap(36,30);await until(m=>m.settings,'Pause');
 await tap(442,676,'modal');await until(m=>m.phase===4&&m.rewardSettled,'Win fixture');await sleep(1800);
 await tap(108,782,'modal');await until(m=>m.home&&!m.transition,'Return home');
 await tap(50,72,'screen');await until(m=>m.settings,'Home settings');await sleep(500);await shot('standard-settings');
 const before=await sample();if(before.unlockedLevel<4)throw Error('Progress fixture missing');
 await tap(270,594,'modal');await shot('standard-confirm');
 if((await sample()).unlockedLevel!==before.unlockedLevel)throw Error('First tap must not clear progress');
 await sleep(3400);await tap(270,594,'modal');
 if((await sample()).unlockedLevel!==before.unlockedLevel)throw Error('Expired confirmation must not clear progress');
 await tap(270,594,'modal');await until(m=>m.unlockedLevel===1,'Second tap resets progress');await shot('standard-cleared');
 checks.push('Visible reset button: first tap arms, 3-second expiry re-arms, second tap clears progress');
 await sleep(1800);await c.call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:false});await sleep(1000);await shot('tall-settings');
 await tap(270,594,'modal');await shot('tall-confirm');
 await tap(270,690,'modal');await until(m=>!m.settings,'Done remains clickable');
 await tap(270,800,'home');await until(m=>!m.home&&!m.transition&&m.level===0,'Reset starts first level');await shot('reset-level-one');
 checks.push('390x844: reset button and confirmation visible, Done closes settings, start opens level 1');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors},null,2));console.log('RESET_BUTTON_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

import fs from 'node:fs';
import {connect} from '../../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL;
fs.mkdirSync(out,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms)),errors=[],checks=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT),onEvent:e=>{if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(f,label){for(let n=0;n<180;n++){let m;try{m=await sample();}catch{}if(m&&f(m))return m;await sleep(500);}throw Error(label);}
async function tap(x,y,space='battle'){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y};let py=y;if('${space}'==='modal'||'${space}'==='home')py=y+(h-960)/2;else if('${space}'==='battle'){const a=[0,60,114,574,647,688,721,855,880,960],b=[0,54,70,444,562,600,661,843,880,960],at=i=>3+b[i]+(h-966)*Math.max(0,Math.min(1,(b[i]-60)/396));let i=0;while(i<a.length-2&&y>a[i+1])i++;py=at(i)+(at(i+1)-at(i))*(y-a[i])/(a[i+1]-a[i]);}return {x:r.left+${x}*s,y:r.top+py*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await sleep(70);await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});await sleep(300);
}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
const settingsMatch=(m,music,sound,light)=>m.musicMuted===music&&m.soundMuted===sound&&m.lightEnabled===light;
async function openLevel(level=6){
 await c.call('Page.navigate',{url:base+'?set=A&level='+level});await sleep(1200);
 return until(m=>m.level===level-1&&!m.home&&!m.transition&&m.phase===1,'Ready level '+level);
}
async function reload(){await c.call('Page.reload',{ignoreCache:true});await sleep(1200);return until(m=>m.level===5&&!m.home&&!m.transition&&m.phase===1,'Reload ready');}
async function toggle(index){await tap(400,318+98*index,'modal');}
async function settings(){await tap(34,28,'screen');await until(m=>m.settings,'Settings open');}
async function resume(){await tap(270,779,'modal');await until(m=>!m.settings&&!m.paused,'Settings close');}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 await c.call('Emulation.setDeviceMetricsOverride',{width:540,height:960,deviceScaleFactor:1,mobile:false});
 let m=await openLevel();
 if(!settingsMatch(m,false,false,true))throw Error('Fresh settings defaults');
 await settings();
 for(let i=0;i<3;i++)await toggle(i);
 await until(m=>settingsMatch(m,true,true,false),'All settings off');
 m=await reload();if(!settingsMatch(m,true,true,false))throw Error('Off settings lost on refresh');
 await settings();await shot('settings-all-off-after-refresh');
 await toggle(0);await toggle(2);await until(m=>settingsMatch(m,false,true,true),'Mixed preferences');
 m=await reload();if(!settingsMatch(m,false,true,true))throw Error('Mixed settings lost on refresh');
 await settings();await shot('settings-mixed-after-refresh');
 await tap(100,676,'modal');await until(m=>m.home&&!m.transition,'Home');
 await tap(50,72,'screen');await until(m=>m.home&&m.settings,'Home settings');
 await toggle(1);await until(m=>settingsMatch(m,false,false,true),'Restore sound from home');
 m=await reload();if(!settingsMatch(m,false,false,true))throw Error('Home setting lost on refresh');
 checks.push('Fresh defaults, all-off refresh, mixed refresh, home settings refresh all retained');
 console.log(checks.at(-1));
 for(const [name,width,height] of [['standard',540,960],['tall',390,844]]){
  await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});
  await openLevel();await sleep(600);await shot(name+'-ready');
  await tap(82,730);await until(m=>m.phase===2,'Battle playing');
  await tap(333,875);await until(m=>m.selection===1,'Priority outer badge hit');
  await tap(333,875);await until(m=>m.selection===0,'Priority outer badge cancel');
  await tap(497,875);await until(m=>m.selection===2,'Bomb outer badge hit');await shot(name+'-cancel-badge');
  await tap(497,875);await until(m=>m.selection===0,'Bomb outer badge cancel');
  await shot(name+'-playing');
  if(name==='standard')fs.writeFileSync(`${out}/buttons.png`,Buffer.from((await c.call('Page.captureScreenshot',{clip:{x:20,y:857,width:510,height:96,scale:2}})).data,'base64'));
  checks.push(name+': badges outside button remain clickable, count/cancel render, locks and shuffle inspected');
 }
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors},null,2));console.log('UI_DETAIL_SETTINGS_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

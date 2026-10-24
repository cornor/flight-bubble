// Rewarded skip acceptance uses real pointer events and read-only runtime metrics.
import fs from 'node:fs';
import {connect} from '../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL||'http://127.0.0.1:8804/';
if(!out||fs.existsSync(out))throw Error('Fresh evidence directory required');
fs.mkdirSync(out,{recursive:true});
const checks=[],errors=[],states={},pause=ms=>new Promise(r=>setTimeout(r,ms));
const c=await connect({port:Number(process.env.R1_CDP_PORT||9344),onEvent:e=>{
 if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);
}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(fn,label,seconds=60){for(let i=0;i<seconds*2;i++){let m;try{m=await sample();}catch{}if(m&&fn(m))return m;await pause(500);}throw Error(label);}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
async function tap(x,y,modal=false){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y};return {x:r.left+${x}*s,y:r.top+(${modal}?y+(h-960)/2:y+3+(h-966)*Math.max(0,Math.min(1,(y-60)/396)))*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});
 await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await pause(70);
 await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});
}
async function win(before,label){
 await tap(442,676,true);await tap(442,676,true);
 const won=await until(m=>m.phase===4&&m.rewardSettled&&!m.settings&&!m.paused,'Skip victory');
 if(won.level!==before.level||won.remaining!==0||won.wins!==before.wins+1||won.dayAds!==before.dayAds+1)throw Error('Skip must settle current level exactly once');
 if(won.unlockedLevel<before.level+2)throw Error('Next level not unlocked');
 states[label]={before,won};await pause(2000);await shot(label+'-win');
 const settled=await sample();if(settled.wins!==won.wins||settled.dayAds!==won.dayAds)throw Error('Duplicate settlement');
 return settled;
}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 for(const [width,height,label] of (process.env.R1_SKIP_LESSON_ONLY ? [] : [[540,960,'standard'],[390,844,'tall']])){
  await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});
  await c.call('Page.navigate',{url:base+'?set=A&level=3'});
  await until(m=>m.level===2&&!m.home&&!m.transition&&m.phase===1,'Third level ready',90);await pause(600);
  await tap(36,30);await until(m=>m.settings&&m.paused,'Pause page');await shot(label+'-pause');
  const before=await sample();await tap(270,779,true);const resumed=await until(m=>!m.settings&&!m.paused,'Continue without skipping');
  if(resumed.phase!==1||resumed.wins!==before.wins||resumed.dayAds!==before.dayAds)throw Error('Continue must not skip');
  await tap(36,30);await until(m=>m.settings,'Pause again');await win(before,label+'-ready');
  await tap(270,783,true);await until(m=>m.level===3&&m.phase===1&&!m.transition,'Victory next level');
  await tap(70,650);await until(m=>m.phase===2&&m.fleet>0,'Fourth level combat');await pause(600);
  await tap(36,30);await until(m=>m.settings&&m.paused,'Pause during combat');
  const playing=await sample();const won=await win(playing,label+'-playing');
  checks.push(`${width}x${height}: pause-only skip / resume unchanged / ready and playing skip / one ad reward and one win / next level unlocked`);
  await tap(108,782,true);await until(m=>m.home&&!m.transition,'Win returns home');await pause(600);
  await tap(35,30);await until(m=>m.home&&m.settings,'Home settings');await shot(label+'-home-settings');
  await tap(442,676,true);await pause(1200);
  const home=await sample();if(!home.home||home.wins!==won.wins||home.dayAds!==won.dayAds)throw Error('Home settings exposed a skip action');
  await c.call('Page.navigate',{url:base+'?set=A&level=3&verify=persistence'});
  const restored=await until(m=>!m.home&&!m.transition&&m.phase===1&&m.level===2&&m.wins===won.wins,'Persisted win',90);
  if(restored.unlockedLevel<5)throw Error('Unlock was not saved');
  checks.push(`${width}x${height}: home settings has no skip / win and level unlock survive reload`);
 }
 await c.call('Emulation.setDeviceMetricsOverride',{width:540,height:960,deviceScaleFactor:1,mobile:false});
 await c.call('Page.navigate',{url:base+'?set=A&level=1'});
 await until(m=>m.level===0&&!m.home&&!m.transition&&m.phase===1,'Teaching level ready',90);
 await tap(36,30);await until(m=>m.settings&&m.paused,'Teaching pause');await shot('teaching-pause');
 await win(await sample(),'teaching');
 await pause(2500);if((await sample()).level!==0)throw Error('Teaching win must wait on its victory dialog');
 await tap(270,783,true);await until(m=>m.level===1&&m.phase===1&&!m.transition,'Teaching next level');
 checks.push('Teaching level: rewarded skip opens the normal victory dialog, waits for Next, then enters level 2');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,adMode:'Existing test provider immediately invokes reward; no real video playback tested.',checks,errors,states},null,2));
 console.log('PLUSH_SKIP_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null),states},null,2));throw e;}
finally{c.close();}

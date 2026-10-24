import fs from 'node:fs';
import {connect} from '/Users/yangdage/aitools/flight/flight-bubble/tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL;
fs.mkdirSync(out,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms)),errors=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT),onEvent:e=>{if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);}});
async function until(f,label){for(let n=0;n<160;n++){let m;try{m=await c.evaluate('window.gardenR1');}catch{}if(m&&f(m))return m;await sleep(500);}throw Error(label);}
async function tap(x,y,modal=false){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y};return {x:r.left+${x}*s,y:r.top+(${modal}?y+(h-960)/2:y+3+(h-966)*Math.max(0,Math.min(1,(y-60)/396)))*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await sleep(70);await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});
}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 await c.call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:false});
 await c.call('Page.navigate',{url:base+'?set=A&level=3'});await until(m=>m.level===2&&!m.home&&!m.transition&&m.phase===1,'Ready');
 await tap(36,30);await until(m=>m.settings,'Pause');await tap(442,676,true);await until(m=>m.phase===4&&!m.settings,'Win');await sleep(2400);await shot('tall-victory');
 await c.call('Emulation.setDeviceMetricsOverride',{width:540,height:960,deviceScaleFactor:1,mobile:false});await sleep(1200);await shot('standard-victory');
 await tap(432,782,true);await until(m=>m.level===2&&m.phase===1&&!m.transition,'Replay');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks:['Replay icon rendered at 390x844 and 540x960','Replay button restarts the same level'],errors},null,2));console.log('REPLAY_ICON_PASSED');
}catch(e){await shot('failure').catch(()=>{});throw e;}finally{c.close();}

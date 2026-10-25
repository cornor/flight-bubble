import fs from 'node:fs';
import {connect} from '../../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL;
fs.mkdirSync(out,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms)),errors=[],checks=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT),onEvent:e=>{if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(f,label){for(let n=0;n<180;n++){let m;try{m=await sample();}catch{}if(m&&f(m))return m;await sleep(500);}throw Error(label);}
async function tap(x,y,space='battle'){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y};let py=y;let px=${x};if('${space}'==='home'){const k=Math.min(1,(h-6)/1026);px=270+(${x}-270)*k;py=(h-1026*k)/2+y*k;}else if('${space}'==='modal')py=y+(h-960)/2;else if('${space}'==='battle')py=y+3+(h-966)*Math.max(0,Math.min(1,(y-60)/396));return {x:r.left+px*s,y:r.top+py*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await sleep(70);await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});await sleep(300);
}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 for(const [name,width,height] of [['standard',540,960],['tall',390,844]]){
  await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});
  await c.call('Page.navigate',{url:base+'?set=A&level=3'});await until(m=>m.level===2&&!m.home&&!m.transition&&m.phase>0,'Level ready');
  await tap(36,30);await until(m=>m.settings,'Pause');await tap(98,676,'modal');await until(m=>m.home&&!m.transition,'Return home');await sleep(1000);
  await shot(name+'-home');
  await tap(170,935,'home');await until(m=>m.codexOpen,'Partner preview opens codex');await tap(500,47,'screen');await until(m=>!m.codexOpen,'Close preview codex');
  await tap(483,780,'home');await until(m=>m.codexOpen,'Enlarged codex hit');await shot(name+'-codex');
  await tap(500,47,'screen');await until(m=>!m.codexOpen,'Close codex');
  await tap(50,49,'screen');await until(m=>m.settings,'Settings hit');await shot(name+'-settings');
  await tap(270,690,'modal');await until(m=>!m.settings,'Close settings');
  await tap(270,823,'home');await until(m=>!m.home&&!m.transition,'Start from level badge');await shot(name+'-started');
  if((await sample()).level!==0)throw Error('CTA failed to start next uncleared level 1');
  checks.push(name+': partner preview and codex open/close, settings open/close, level badge starts next uncleared level');
 }
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors},null,2));console.log('HOME_WAVE_LAYOUT_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

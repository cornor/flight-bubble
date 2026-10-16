// Read-only telemetry plus real mouse/touch input; level selection is URL-only.
import fs from 'node:fs';
import { connect } from '../../tools/browser/cdp.mjs';
const out=process.argv[2];
if(!out||fs.existsSync(out))throw Error('A fresh evidence directory is required');
fs.mkdirSync(out,{recursive:true});
const pause=ms=>new Promise(r=>setTimeout(r,ms));
const levels=(process.env.R1_LEVELS||'5,10,30,100,200,262').split(',').map(Number);
const seconds=Number(process.env.R1_SECONDS||60);
const events=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT||9342),onEvent:e=>{
 if(['Runtime.exceptionThrown','Log.entryAdded'].includes(e.method)){events.push(e);fs.appendFileSync(out+'/browser-events.jsonl',JSON.stringify(e)+'\n');}
}});
await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
await c.call('Emulation.setDeviceMetricsOverride',{width:540,height:Number(process.env.R1_HEIGHT||960),deviceScaleFactor:Number(process.env.R1_DPR||1),mobile:false});
await c.call('Emulation.setCPUThrottlingRate',{rate:Number(process.env.R1_CPU_RATE||1)});
await c.call('Emulation.setTouchEmulationEnabled',{enabled:process.env.R1_TOUCH==='1',maxTouchPoints:1});
async function tap(x,y){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,t=(h-506)/460,y=${y},py=y<=114?3+y:y<574?3+114+(y-114)*t:3+t*460+y-460;return {x:r.left+${x}*s,y:r.top+py*s}})()`);
 if(process.env.R1_TOUCH==='1'){
  await c.call('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{...p,id:0}]});await pause(80);await c.call('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
 }else{
  await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});
  await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await pause(65);
  await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});
 }
 fs.appendFileSync(out+'/actions.jsonl',JSON.stringify({level:currentLevel,x,y,...p,at:Date.now()})+'\n');
}
async function screenshot(name){fs.writeFileSync(out+'/'+name+'.png',Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
const results=[];let currentLevel=0;
try{
 for(const level of levels){
  currentLevel=level;
  await c.call('Page.navigate',{url:(process.env.R1_URL||'http://127.0.0.1:8802/')+'?level='+level});
  let initial;
  for(let i=0;i<90;i++){
   try{initial=await c.evaluate('window.gardenR1');}catch{}
   if(initial&&!initial.home&&initial.level===level-1&&initial.phase!==0)break;
   if(i===89)throw Error('Level did not open: '+level);
   await pause(500);
  }
  await pause(1800);await screenshot('level-'+level+'-start');
  const start=Date.now();let first=0,lastShot=0,lastSample,actions=0;
  while(Date.now()-start<(seconds+90)*1000){
   const m=await c.evaluate('window.gardenR1');lastSample=m;
   fs.appendFileSync(out+'/samples.jsonl',JSON.stringify({requested:level,...m})+'\n');
   if(m.home||m.level!==level-1)throw Error('Unexpected level/home during sample: '+level);
   if(m.phase===2&&!first)first=Date.now();
   if(first&&Date.now()-first>=seconds*1000)break;
   if(first&&Date.now()-lastShot>15000){await screenshot('level-'+level+'-'+Math.floor((Date.now()-first)/1000)+'s');lastShot=Date.now();}
   if(m.phase===4||m.phase===5){if(seconds===0)break;throw Error('Ended before requested sample duration: '+level);}
   if(m.phase===3)await tap(270,645);
   else if(m.finaleReady)await tap(270,759);
   else if(!m.paused&&!m.transition&&[1,2].includes(m.phase)){
    const candidates=m.lanes.map((l,i)=>({...l,i})).filter(l=>l.color>=0);
    let chosen=candidates.find(l=>l.merge&&l.exposed)||candidates.find(l=>l.merge)||candidates.find(l=>l.exposed&&!m.fleetColors.includes(l.color))||candidates.find(l=>l.exposed);
    if(!chosen&&m.fleet<5)chosen=candidates[0];
    if(chosen){await tap(20+(chosen.i+.5)*500/m.lanes.length,725);actions++;}
   }
   if(seconds===0)break;
   await pause(600);
  }
  await screenshot('level-'+level+'-final');
  const final=await c.evaluate('window.gardenR1');
  const result={level,initial,final,actions,observedSeconds:first?(Date.now()-first)/1000:0};
  if(seconds>0&&result.observedSeconds<seconds)throw Error('Insufficient active observation: '+level);
  results.push(result);fs.writeFileSync(out+'/result.json',JSON.stringify({complete:false,results},null,2));
  console.log(JSON.stringify({level,actions,observedSeconds:result.observedSeconds,remaining:final.remaining,phase:final.phase}));
 }
 const errors=events.filter(e=>e.method==='Runtime.exceptionThrown'||e.params?.entry?.level==='error');
 const environment=await c.evaluate(`(()=>{const cv=document.querySelector('canvas'),gl=cv.getContext('webgl2');return {viewport:[innerWidth,innerHeight],dpr:devicePixelRatio,maxFragmentSamplers:gl.getParameter(gl.MAX_TEXTURE_IMAGE_UNITS),userAgent:navigator.userAgent}})()`);
 fs.writeFileSync(out+'/result.json',JSON.stringify({complete:errors.length===0,results,errors,environment,cpuThrottle:Number(process.env.R1_CPU_RATE||1)},null,2));
 if(errors.length)throw Error('Browser errors: '+errors.length);
}finally{c.close();}

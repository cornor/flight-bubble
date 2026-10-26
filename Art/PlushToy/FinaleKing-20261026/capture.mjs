// Finale visual regression: real queue/advert/launch clicks, read-only telemetry.
import fs from 'node:fs';
import {connect} from '../../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL;
if(fs.existsSync(out))throw Error('Fresh evidence directory required');
fs.mkdirSync(out,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms)),errors=[],checks=[],frames=[];
const frameDir=process.env.R1_FRAME_DIR||'/tmp/flight-bubble/finale-king-20261026/screencast-final';fs.mkdirSync(frameDir,{recursive:true});
const c=await connect({port:Number(process.env.R1_CDP_PORT),onEvent:e=>{if(e.method==='Page.screencastFrame'){const name=String(frames.length).padStart(6,'0')+'.jpg';fs.writeFileSync(frameDir+'/'+name,Buffer.from(e.params.data,'base64'));frames.push({name,t:e.params.metadata.timestamp});c.call('Page.screencastFrameAck',{sessionId:e.params.sessionId}).catch(()=>{});}if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(f,label,seconds=90){for(let n=0;n<seconds*2;n++){let m;try{m=await sample();}catch{}if(m&&f(m))return m;await sleep(500);}throw Error(label);}
async function tap(x,y,modal=false){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y};return {x:r.left+${x}*s,y:r.top+(${modal}?y+(h-960)/2:y+3+(h-966)*Math.max(0,Math.min(1,(y-60)/396)))*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await sleep(70);await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});
 fs.appendFileSync(out+'/actions.jsonl',JSON.stringify({x,y,modal,at:Date.now()})+'\n');
}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
async function viewport(width,height){await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});await sleep(800);}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 await viewport(540,960);await c.call('Page.navigate',{url:base+'?set=A&level=7'});
 await until(m=>m.level===6&&!m.home&&!m.transition&&m.phase===1,'Level 7 ready');await sleep(1200);
 const start=Date.now();let launched=false,revives=0;
 while(Date.now()-start<240000){
  const m=await sample();fs.appendFileSync(out+'/samples.jsonl',JSON.stringify(m)+'\n');
  if(m.phase===5)throw Error('Battle lost');
  if(m.phase===4){if(!launched)throw Error('Victory before finale');await sleep(1600);await shot('victory');checks.push('Finale clears remaining bubbles and reaches victory');break;}
  if(m.finaleGuide||m.finaleReady){
   await sleep(3000);await shot('standard-finale-ready');
   if((await sample()).finaleGuide){await viewport(390,844);await shot('tall-finale-ready');await viewport(540,960);}
   await c.call('Page.startScreencast',{format:'jpeg',quality:90,maxWidth:540,maxHeight:960,everyNthFrame:1});
   await tap(270,759);launched=true;const at=Date.now();
   for(const [ms,name] of [[700,'king-appearing'],[950,'king-ready'],[1370,'king-inhale'],[1480,'king-full'],[1850,'king-blow'],[2350,'rainbow-release'],[2700,'rainbow-sweep'],[3450,'clear-banner'],[4200,'clear-banner-late']]){await sleep(Math.max(0,at+ms-Date.now()));await shot(name);}
   await c.call('Page.stopScreencast');await sleep(150);fs.writeFileSync(out+'/frames.json',JSON.stringify(frames,null,2));
   checks.push('Fixed-pot furry king launches the original merge / rainbow sweep / clear sequence');
  }else if(m.phase===3){await tap(270,700,true);await until(m=>m.phase===2,'Revive');revives++;await until(m=>m.selection===2,'Reward bomb targeting',15);await tap(270,740);}
  else if(m.selection===2){await tap(270,740);}
  else if(!m.paused&&!m.transition){
   const lanes=m.lanes.map((l,i)=>({...l,i})).filter(l=>l.color>=0);
   const l=lanes.find(l=>l.merge&&l.exposed)||lanes.find(l=>l.merge)||lanes.find(l=>l.exposed&&!m.fleetColors.includes(l.color))||lanes.find(l=>l.exposed)||(m.fleet<m.unlockedSlots?lanes[0]:null);
   if(l)await tap(20+(l.i+.5)*500/m.lanes.length,650);
  }
  await sleep(750);
 }
 const last=await sample();if(!launched||last.phase!==4)throw Error('Finale completion timed out');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,revives,last,errors},null,2));console.log('FINALE_KING_VISUAL_FLOW_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

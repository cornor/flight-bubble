// Front-five acceptance: real pointer input, URL entry, and read-only game telemetry.
import fs from 'node:fs';
import { connect } from '../../tools/browser/cdp.mjs';
const out=process.argv[2];
if(!out||fs.existsSync(out))throw Error('Fresh evidence directory required');
fs.mkdirSync(out,{recursive:true});
const pause=ms=>new Promise(r=>setTimeout(r,ms)),events=[],results=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT||9344),onEvent:e=>{
 if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))events.push(e);
}});
const sample=()=>c.evaluate('window.gardenR1'),base=process.env.R1_URL||'http://127.0.0.1:8804/';
async function until(predicate,label,seconds=25){
 for(let i=0;i<seconds*2;i++){let m;try{m=await sample();}catch{}if(m&&predicate(m))return m;await pause(500);}
 throw Error(label);
}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
async function tap(x,y,modal=false){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,e=h-966,y=${y},py=${modal}?y+(h-960)/2:y+3+e*Math.max(0,Math.min(1,(y-60)/396));return {x:r.left+${x}*s,y:r.top+py*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});
 await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await pause(65);
 await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});
 fs.appendFileSync(out+'/actions.jsonl',JSON.stringify({x,y,modal,...p,at:Date.now()})+'\n');
}
async function settle(level){
 const m=await until(m=>m.level===level-1&&!m.home&&!m.transition&&m.phase!==0,`Level ${level} failed to open`,90);
 if(!m.plushTheme)throw Error('Plush theme inactive');
 await pause(1500);return sample();
}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 await c.call('Emulation.setDeviceMetricsOverride',{width:540,height:960,deviceScaleFactor:1,mobile:false});
 await c.call('Page.navigate',{url:base+'?set=A&level=1'});
 for(let level=1;level<=5;level++){
  const initial=await settle(level);await shot(`level-${level}-ready`);
  // Formal levels 4/5 expose two optional bays. Exercise their real click/ad path.
  if(level>=4){
   await tap(433,550);await pause(1500);await tap(498,550);await pause(1500);
   await shot(`level-${level}-unlocked`);
  }
  const start=Date.now(),steps=new Set();let won=false,deploys=0,revives=0,battleShot=false,maxFleet=0,maxTier=0;
  while(Date.now()-start<240000){
   const m=await sample();fs.appendFileSync(out+'/samples.jsonl',JSON.stringify(m)+'\n');
   maxFleet=Math.max(maxFleet,m.fleet);maxTier=Math.max(maxTier,m.maxTier);
   if(!steps.has(m.step)){steps.add(m.step);if(level<=2)await shot(`level-${level}-tutorial-${m.step}`);}
   if(m.phase===4){won=true;await pause(1600);await shot(`level-${level}-win`);break;}
   if(m.phase===5)throw Error(`Level ${level} lost`);
   if(m.phase===3){
    if(++revives>2)throw Error(`Level ${level} repeated revive`);
    await shot(`level-${level}-revive-${revives}`);await tap(270,700,true);
    await until(m=>m.phase===2,`Level ${level} revive button failed`);
   }
   else if(m.finaleGuide)await tap(270,740);
   else if(m.finaleReady)await tap(270,738);
   else if(!m.paused&&!m.transition){
    const candidates=m.lanes.map((l,i)=>({...l,i})).filter(l=>l.color>=0);
    const chosen=candidates.find(l=>l.merge&&l.exposed)||candidates.find(l=>l.merge)||candidates.find(l=>l.exposed&&!m.fleetColors.includes(l.color))||candidates.find(l=>l.exposed)||(m.fleet<5?candidates[0]:null);
    if(chosen){await tap(20+(chosen.i+.5)*500/m.lanes.length,650);deploys++;}
   }
   if(!battleShot&&m.phase===2&&m.fleet>=3){await shot(`level-${level}-battle`);battleShot=true;}
   // Metrics are sampled once a second. A faster loop can click the same stale lane twice,
   // accidentally deploying the next color before noticing that the queue advanced.
   await pause(1100);
  }
  if(!won)throw Error(`Level ${level} timed out`);
  const final=await sample();results.push({level,initial,final,deploys,revives,maxFleet,maxTier,steps:[...steps]});
  fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,inProgress:true,results,events},null,2));
  console.log(JSON.stringify({level,won,deploys,revives,maxFleet,maxTier,steps:[...steps]}));
  if(level<5){await tap(270,783,true);await settle(level+1);}
 }
 for(const level of [1,5]){
  await c.call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:false});
  await c.call('Page.navigate',{url:base+'?set=A&level='+level});await settle(level);
  await shot(`level-${level}-tall`);await tap(82,650);
  await until(m=>m.phase===2&&m.fleet>0,`Tall level ${level} deployment failed`);
 }
 if(events.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,results,events,tallLevels:[1,5],nextTransitions:4},null,2));
 console.log('FIRST_FIVE_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),results,events,last:await sample().catch(()=>null)},null,2));throw e;}
finally{c.close();}

// Natural victory for actual combo/time copy, then real-pointer victory-button checks.
import fs from 'node:fs';
import {connect} from '../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL||'http://127.0.0.1:8804/';
if(!out||fs.existsSync(out))throw Error('Fresh evidence directory required');fs.mkdirSync(out,{recursive:true});
const checks=[],errors=[],pause=ms=>new Promise(r=>setTimeout(r,ms));
const c=await connect({port:Number(process.env.R1_CDP_PORT||9344),onEvent:e=>{
 if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);
}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(fn,label,seconds=90){for(let i=0;i<seconds*2;i++){let m;try{m=await sample();}catch{}if(m&&fn(m))return m;await pause(500);}throw Error(label);}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
async function tap(x,y,modal=false){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y};return {x:r.left+${x}*s,y:r.top+(${modal}?y+(h-960)/2:y+3+(h-966)*Math.max(0,Math.min(1,(y-60)/396)))*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});
 await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await pause(70);
 await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});
}
async function viewport(width,height){await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});}
async function open(){await c.call('Page.navigate',{url:base+'?set=A&level=3'});await until(m=>m.level===2&&!m.home&&!m.transition&&m.phase===1,'Level 3 ready');}
async function skip(){await tap(36,30);await until(m=>m.settings&&m.paused,'Pause for skip');await tap(442,676,true);await until(m=>m.phase===4&&!m.settings,'Rewarded win');await pause(2200);}
let natural;
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 await viewport(540,960);await open();
 let won=false;
 for(let i=0;i<210;i++){
  const m=await sample();
  if(m.phase===4){won=true;break;}
  if(m.phase===5)throw Error('Natural battle lost');
  if(m.phase===3){await tap(270,700,true);await until(m=>m.phase===2,'Revive');}
  else if(m.selection===2)await tap(270,740);
  else if(m.finaleGuide||m.finaleReady)await tap(270,738);
  else if(!m.paused&&!m.transition){
   const lanes=m.lanes.map((v,i)=>({...v,i})).filter(v=>v.color>=0);
   const lane=lanes.find(v=>v.merge&&v.exposed)||lanes.find(v=>v.merge)||lanes.find(v=>v.exposed&&!m.fleetColors.includes(v.color))||lanes.find(v=>v.exposed)||(m.fleet<5?lanes[0]:null);
   if(lane)await tap(20+(lane.i+.5)*500/m.lanes.length,650);
  }
  await pause(1100);
 }
 if(!won)throw Error('Natural win timed out');
 await pause(2400);natural=await sample();await shot('standard-natural-win');
 if(natural.battleTime<=0||natural.remaining!==0||!natural.rewardSettled)throw Error('Natural results not settled');
 await viewport(390,844);await pause(1200);await shot('tall-natural-win');
 checks.push('Natural level 3 victory: restored red ribbon, award labels and nonzero combo/time reviewed at 540x960 and 390x844');
 await tap(432,782,true);await until(m=>m.level===2&&m.phase===1&&!m.transition,'Tall victory replay');
 await skip();await shot('tall-replayed-win');
 await tap(108,782,true);await until(m=>m.home&&!m.transition,'Tall victory home');await shot('tall-home');
 checks.push('390x844: Replay restarts the same level; Home returns to the home page');
 await viewport(540,960);await open();await skip();await shot('standard-replayed-win');
 await tap(270,783,true);await until(m=>m.level===3&&m.phase===1&&!m.transition,'Standard victory next');await shot('standard-next-level');
 checks.push('540x960: Next enters level 4; repeated-clear partner label fits');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors,natural},null,2));console.log('PLUSH_VICTORY_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}
finally{c.close();}

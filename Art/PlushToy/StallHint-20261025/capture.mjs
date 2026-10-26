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
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 await c.call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:false});
 await c.call('Page.navigate',{url:base+'?set=A&level=6'});
 await until(m=>m.level===5&&!m.home&&!m.transition&&m.phase===1,'Level 6 ready');
 for(let n=0;n<100&&await c.evaluate('!!document.getElementById("loading")');n++)await sleep(200);
 await sleep(700);
 const steps=[];
 for(let n=0;n<28;n++){
  const m=await sample();
  if(m.phase===3||m.phase===5)throw Error('Guard breached before full bay capture');
  if(m.fleet===m.unlockedSlots){await sleep(1500);break;}
  const lane=m.lanes.findIndex(l=>l.color>=0&&!l.exposed&&!l.merge);
  const fallback=m.lanes.findIndex(l=>l.color>=0&&!l.exposed);
  const pick=lane>=0?lane:fallback;
  if(pick<0)throw Error('No blocked queue head available');
  steps.push({fleet:m.fleet,colors:m.fleetColors,pick,head:m.lanes[pick]});
  await tap(20+(pick+.5)*500/m.lanes.length,730);
  await sleep(850);
 }
 const full=await sample();
 if(full.phase!==2||full.fleet!==full.unlockedSlots)throw Error('Bays did not reach full');
 await shot('phone-390x844');
 for(const [width,height] of [[360,800],[540,960]]){
  await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});await sleep(500);
  await shot('phone-'+width+'x'+height);
 }
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,evidence:'Real queue input; naturally fill slots with blocked-color members. Inspect screenshots for stall hint.',full,steps,errors},null,2));
 console.log('STALL_HINT_CAPTURE_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/failure.json',JSON.stringify({error:String(e),last:await sample().catch(()=>null),errors},null,2));throw e;}finally{c.close();}

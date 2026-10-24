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
 for(const [name,width,height] of [['standard',540,960],['tall',390,844]]){
  await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});
  await c.call('Page.navigate',{url:base+'?set=A&level=7'});await until(m=>m.level===6&&!m.home&&!m.transition&&m.phase===1,'Ready');await sleep(1000);
  await shot(name+'-locked');
  const before=await sample();await tap(513,615);await until(m=>m.unlockedSlots===before.unlockedSlots+1,'Video corner unlock');await sleep(700);await shot(name+'-unlocked');
  const after=await sample();if(after.dayAds!==before.dayAds+1)throw Error('Exactly one rewarded unlock expected');
  checks.push(name+': Git glass lock visible, upper-right video icon clickable, one rewarded unlock');
 }
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors},null,2));console.log('SLOT_UNLOCK_RESTORE_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

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
 await c.call('Emulation.setDeviceMetricsOverride',{width:540,height:960,deviceScaleFactor:1,mobile:false});
 await c.call('Page.navigate',{url:base+'?set=A&level=6'});
 await until(m=>m.level===5&&!m.home&&!m.transition&&m.phase===1,'Level 6 ready');
 for(let n=0;n<100&&await c.evaluate('!!document.getElementById("loading")');n++)await sleep(200);
 await sleep(700);
 const before=await sample(),lane=before.lanes.findIndex(l=>l.color>=0&&!l.exposed);
 if(lane<0)throw Error('No waiting colour available');
 await shot('before-revive-slots');
 await tap(20+(lane+.5)*500/before.lanes.length,730);
 const offer=await until(m=>m.phase===3,'Natural revive offer');
 await sleep(400);await shot('revive-offer');
 await tap(270,700,'modal');
 const revived=await until(m=>m.phase===2&&m.unlockedSlots===offer.unlockedSlots+1,'Rewarded revive action');
 if(revived.maximumSlots!==offer.maximumSlots)throw Error('Revival appended a bay instead of opening the first locked bay');
 await shot('after-revive-slots');
 await until(m=>m.selection===2,'Revive bomb aim');
 await tap(270,774);
 await until(m=>m.selection===0&&!m.paused,'Revive bomb confirm');
 await shot('after-bomb-slots');
 const last=revived.maximumSlots-1;
 await tap(42+456*last/(revived.maximumSlots-1),620);
 const allOpen=await until(m=>m.unlockedSlots===revived.unlockedSlots+1,'Remaining locked bay ad unlock');
 if(allOpen.unlockedSlots!==allOpen.maximumSlots)throw Error('Unexpected extra locked bay after final ad unlock');
 await shot('all-slots-open');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks:['Normal level 6 queue input and natural revive offer','Revive adds one usable slot without expanding locked bay row','Revive bomb aim and confirmation work','Remaining final ad bay remains unlockable'],before,offer,revived,allOpen,errors},null,2));
 console.log('ORDERED_REVIVE_WEB_PASSED');
}catch(e){await shot('failure').catch(()=>{});throw e;}finally{c.close();}

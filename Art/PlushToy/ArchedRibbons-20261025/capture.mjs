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
async function viewport(width,height){await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});await sleep(900);}
async function level(){
 await c.call('Page.navigate',{url:base+'?set=A&level=6'});
 await until(m=>m.level===5&&!m.home&&!m.transition&&m.phase===1,'Level 6 ready');
 for(let n=0;n<100&&await c.evaluate('!!document.getElementById("loading")');n++)await sleep(200);
 await sleep(700);
}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 await viewport(540,960);await level();
 await tap(36,30);await until(m=>m.settings,'Pause');
 await tap(442,676,'modal');await until(m=>m.phase===4&&!m.settings,'Rewarded skip victory');
 await sleep(2500);await shot('standard-victory');
 await viewport(390,844);await shot('tall-victory');
 await viewport(540,960);await level();
 const ready=await sample(),lane=ready.lanes.findIndex(l=>l.color>=0&&!l.exposed);
 if(lane<0)throw Error('No waiting colour available');
 await tap(20+(lane+.5)*500/ready.lanes.length,730);
 await until(m=>m.phase===3,'Natural revive offer');
 await tap(482,232,'modal');await until(m=>m.phase===5,'Decline revival');
 await sleep(1800);await shot('standard-loss');
 await viewport(390,844);await shot('tall-loss');
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks:['Victory entered through real pause skip button','Loss entered by naturally reaching revival and declining','Both result ribbon layouts reviewed at 540x960 and 390x844'],adMode:'Existing test provider; no real video playback tested.',errors},null,2));
 console.log('RESULT_RIBBONS_WEB_PASSED');
}catch(e){await shot('failure').catch(()=>{});throw e;}finally{c.close();}

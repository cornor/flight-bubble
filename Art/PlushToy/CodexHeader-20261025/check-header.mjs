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
 for(const [name,width,height] of [['standard',540,960],['wechat-phone',390,758],['tall-phone',390,844]]){
  await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});
  await c.call('Page.navigate',{url:base+'?set=A&level=6'});await sleep(1000);
  await until(m=>m.level===5&&!m.home&&!m.transition&&m.phase===1,'Ready');
  await tap(34,28,'screen');await until(m=>m.settings,'Settings');
  await tap(100,676,'modal');await until(m=>m.home&&!m.transition,'Home');
  await tap(485,773,'home');await until(m=>m.codexOpen,'Album opens');await sleep(700);await shot(name+'-album');
  await tap(370,860,'home');await until(m=>m.codexSelection===18,'Frog card selection');await sleep(500);await shot(name+'-frog');
  await tap(500,47,'screen');await until(m=>!m.codexOpen&&m.home,'Close at top safe area');
  await tap(485,773,'home');await until(m=>m.codexOpen,'Album reopens');
  await c.call('Emulation.setDeviceMetricsOverride',{width,height:height+60,deviceScaleFactor:1,mobile:false});await sleep(600);await shot(name+'-resized');
  await tap(500,47,'screen');await until(m=>!m.codexOpen&&m.home,'Close remains anchored after viewport resize');
  checks.push(name+': header anchored, frog card accessible, close works before and after viewport resize');
 }
 if(errors.length)throw Error('Browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors},null,2));console.log('CODEX_HEADER_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}finally{c.close();}

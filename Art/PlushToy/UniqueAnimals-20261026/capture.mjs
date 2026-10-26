// Actual WebGL input smoke; telemetry is read-only and uses an isolated browser profile.
import fs from 'node:fs';
import {connect} from '../../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL;
if (!out || fs.existsSync(out)) throw Error('Fresh evidence directory required');
fs.mkdirSync(out,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms)),errors=[],results=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT),onEvent:e=>{
 if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);
}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(f,label,seconds=90){
 for(let n=0;n<seconds*2;n++){let m;try{m=await sample();}catch{}if(m&&f(m))return m;await sleep(500);}
 throw Error(label);
}
async function tap(x,y){
 const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y},a=[0,60,114,574,647,688,721,855,880,960],b=[0,54,70,444,562,600,661,843,880,960],at=i=>3+b[i]+(h-966)*Math.max(0,Math.min(1,(b[i]-60)/396));let i=0;while(i<a.length-2&&y>a[i+1])i++;const py=at(i)+(at(i+1)-at(i))*(y-a[i])/(a[i+1]-a[i]);return {x:r.left+${x}*s,y:r.top+py*s};})()`);
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});
 await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await sleep(70);
 await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});
 fs.appendFileSync(out+'/actions.jsonl',JSON.stringify({x,y,...p,at:Date.now()})+'\n');
}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');
 for(const [level,width,height] of [[1,540,960],[2,540,960],[5,540,960],[13,390,844]]){
  await c.call('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});
  await c.call('Page.navigate',{url:base+'?set=A&level='+level});
  await until(m=>m.level===level-1&&!m.home&&!m.transition&&m.phase===1,`Level ${level} ready`);
  for(let n=0;n<100&&await c.evaluate('!!document.getElementById("loading")');n++)await sleep(200);
  await sleep(900);
  const before=await sample();
  if(!before.plushTheme)throw Error('Plush theme inactive');
  await shot(`level-${level}-ready-${width}x${height}`);
  // Prefer a currently exposed color, with the ordinary first member as a tutorial fallback.
  let lane=before.lanes.findIndex(l=>l.color>=0&&l.exposed);
  if(lane<0)lane=before.lanes.findIndex(l=>l.color>=0);
  if(lane<0)throw Error('No waiting member');
  await tap(20+(lane+.5)*500/before.lanes.length,730);
  await until(m=>m.phase===2&&m.fleet>0,`Level ${level} deployment`,20);
  const after=await until(m=>m.remaining<before.remaining,`Level ${level} shooting`,20);
  await shot(`level-${level}-battle-${width}x${height}`);
  results.push({level,width,height,lane,before,after});
  console.log(JSON.stringify({level,width,height,remainingBefore:before.remaining,remainingAfter:after.remaining,fleetColors:after.fleetColors}));
 }
 if(errors.length)throw Error('Browser runtime errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,results,errors},null,2));
 console.log('UNIQUE_ANIMALS_WEBGL_PASSED');
}catch(e){
 await shot('failure').catch(()=>{});
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),results,errors,last:await sample().catch(()=>null)},null,2));
 throw e;
}finally{c.close();}

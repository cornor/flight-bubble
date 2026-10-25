import fs from 'node:fs';
import {connect} from '../../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL;
fs.mkdirSync(out,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms)),errors=[],results=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT),onEvent:e=>{if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(f){for(let n=0;n<180;n++){let m;try{m=await sample();}catch{}if(m&&f(m))return m;await sleep(500);}throw Error('Ready timeout');}
const metrics=async()=>Object.fromEntries((await c.call('Performance.getMetrics')).metrics.map(x=>[x.name,x.value]));
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');await c.call('Performance.enable');
 await c.call('Page.addScriptToEvaluateOnNewDocument',{source:`(()=>{
 const p=window.perfProbe={active:false,frames:[],draws:[],indices:[],uploads:[],dc:0,ic:0,bytes:0};
 const gl=WebGL2RenderingContext.prototype,draw=gl.drawElements,sub=gl.bufferSubData,data=gl.bufferData;
 gl.drawElements=function(mode,count,...rest){if(p.active){p.dc++;p.ic+=count;}return draw.call(this,mode,count,...rest);};
 // WebGL2 receives a view of the whole WASM heap plus an element range.
 // Count that range, not the backing heap's byteLength.
 const byteRange=(view,offset=0,length=0)=>{if(!view)return 0;const size=view.BYTES_PER_ELEMENT||1;return length>0?length*size:Math.max(0,view.byteLength-offset*size);};
 gl.bufferSubData=function(...args){if(p.active)p.bytes+=byteRange(args[2],args[3],args[4]);return sub.apply(this,args);};
 gl.bufferData=function(...args){if(p.active)p.bytes+=typeof args[1]==='number'?args[1]:byteRange(args[1],args[3],args[4]);return data.apply(this,args);};
 let prev=0;function frame(t){if(p.active&&prev){p.frames.push(t-prev);p.draws.push(p.dc);p.indices.push(p.ic);p.uploads.push(p.bytes);}prev=t;p.dc=p.ic=p.bytes=0;requestAnimationFrame(frame);}requestAnimationFrame(frame);
})();`});
 const cases=process.env.R1_PERF_CASES?JSON.parse(process.env.R1_PERF_CASES):process.env.R1_PERF_QUICK?[[2,1,2],[3,1,2]]:[[2,1,2],[3,1,2],[2,4,2],[3,4,2],[3,4,1]];
 for(const [level,rate,dpr] of cases){
  await c.call('Emulation.setCPUThrottlingRate',{rate});
  await c.call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:dpr,mobile:false});
  await c.call('Page.navigate',{url:base+'?set=A&level='+level});await sleep(1000);
  const initial=await until(m=>m.level===level-1&&!m.home&&!m.transition&&m.phase===1);await sleep(1500);
  const before=await metrics();const begin=Date.now();
  await c.evaluate('perfProbe.active=true');await sleep(12000);await c.evaluate('perfProbe.active=false');
  const after=await metrics();
  const data=await c.evaluate(`(()=>{const p=perfProbe,a=p.frames.slice(1).sort((a,b)=>a-b),mean=v=>v.reduce((a,b)=>a+b,0)/Math.max(1,v.length),cv=document.querySelector('canvas');return {frames:a.length,rafFps:1000/mean(a),p50:a[Math.floor(a.length*.5)],p95:a[Math.floor(a.length*.95)],p99:a[Math.floor(a.length*.99)],drawCalls:mean(p.draws),indices:mean(p.indices),uploadBytesPerFrame:mean(p.uploads),backing:[cv.width,cv.height],heapUsed:performance.memory?.usedJSHeapSize};})()`);
  const row={level,rate,dpr,initial,final:await sample(),...data,taskMsPerFrame:(after.TaskDuration-before.TaskDuration)*1000/data.frames,scriptMsPerFrame:(after.ScriptDuration-before.ScriptDuration)*1000/data.frames,wallSeconds:(Date.now()-begin)/1000};
  results.push(row);fs.writeFileSync(out+'/result.json',JSON.stringify({results,errors},null,2));
  fs.writeFileSync(`${out}/level-${level}-cpu${rate}-dpr${dpr}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));
  console.log(JSON.stringify({level,rate,dpr,visible:row.final.visibleBugs,fps:data.rafFps,p95:data.p95,taskMs:row.taskMsPerFrame,drawCalls:data.drawCalls,indices:data.indices,uploadMiB:data.uploadBytesPerFrame/1048576}));
 }
 await c.call('Emulation.setCPUThrottlingRate',{rate:1});await c.call('Profiler.enable');await c.call('Profiler.setSamplingInterval',{interval:1000});await c.call('Profiler.start');await sleep(10000);
 const profile=(await c.call('Profiler.stop')).profile;fs.writeFileSync(out+'/level3-ready.cpuprofile',JSON.stringify(profile));
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:errors.length===0,results,errors},null,2));
}finally{c.close();}

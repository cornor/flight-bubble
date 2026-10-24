// Loading acceptance uses real HTML, network requests and Unity startup.
// Intercept only the Unity loader request to inspect the first frame/failure state.
import fs from 'node:fs';
import {connect} from '../../../tools/browser/cdp.mjs';
const out=process.argv[2],base=process.env.R1_URL;
if(fs.existsSync(out))throw Error('Fresh evidence directory required');fs.mkdirSync(out,{recursive:true});
const sleep=ms=>new Promise(r=>setTimeout(r,ms)),errors=[],expectedFailures=[],held=[],checks=[],layouts=[],progress=[];
let mode='hold',c;
c=await connect({port:Number(process.env.R1_CDP_PORT),onEvent:e=>{
 if(e.method==='Fetch.requestPaused'){
  const p=e.params;if(mode==='hold')held.push(p);
  else c.call(mode==='fail'?'Fetch.failRequest':'Fetch.continueRequest',{requestId:p.requestId,...(mode==='fail'?{errorReason:'Failed'}:{})}).catch(error=>errors.push(String(error)));
 }
 if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))(mode==='fail'?expectedFailures:errors).push(e);
}});
async function until(f,label,seconds=60){for(let i=0;i<seconds*5;i++){let r;try{r=await f();}catch{}if(r)return r;await sleep(200);}throw Error(label);}
async function shot(name){fs.writeFileSync(`${out}/${name}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
const text=()=>c.evaluate('document.querySelector("#lg-text")?.textContent');
const loadedArt=()=>c.evaluate('(()=>{const im=document.querySelector("#lg-art img");return im?.complete&&im.naturalWidth>0;})()');
async function viewport(w,h){await c.call('Emulation.setDeviceMetricsOverride',{width:w,height:h,deviceScaleFactor:1,mobile:false});await until(loadedArt,'Artwork must load');await sleep(500);}
async function layout(name){
 const l=await c.evaluate(`(()=>{const rect=id=>{const r=document.getElementById(id).getBoundingClientRect();return {x:r.x,y:r.y,width:r.width,height:r.height,right:r.right,bottom:r.bottom};};return {stage:rect('stage'),bar:rect('lg-bar'),label:rect('lg-text'),health:rect('lg-health'),healthTitle:document.querySelector('#lg-health h2').textContent,healthLines:[...document.querySelectorAll('#lg-health p')].map(p=>p.textContent),image:document.querySelector('#lg-art img').currentSrc,progress:document.querySelector('#lg-bar').getAttribute('aria-valuenow')};})()`);
 if(l.health.bottom>l.stage.bottom+.5||l.health.x<l.stage.x||l.health.right>l.stage.right+.5||l.bar.bottom>=l.health.y||l.label.bottom>l.bar.y)throw Error(name+' layout overlaps');
 if(l.healthLines.join('')!=='抵制不良游戏，拒绝盗版游戏。注意自我保护，谨防受骗上当。适度游戏益脑，沉迷游戏伤身。合理安排时间，享受健康生活。')throw Error('Health text mismatch');
 layouts.push({name,...l});await shot(name);
}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');await c.call('Network.enable');await c.call('Network.setCacheDisabled',{cacheDisabled:true});
 await c.call('Fetch.enable',{patterns:[{urlPattern:'*/Build/*.loader.js*',requestStage:'Request'}]});
 await c.call('Emulation.setDeviceMetricsOverride',{width:540,height:960,deviceScaleFactor:1,mobile:false});
 await c.call('Page.navigate',{url:base+'?set=A&level=3'});await until(()=>held.length>0,'Hold Unity loader');await until(loadedArt,'Initial artwork');await sleep(800);
 await layout('standard-loading');await sleep(2000);if((await text())!=='加载中… 0%')throw Error('Loading progress advances without download');
 for(const [w,h,name] of [[390,844,'tall-loading'],[320,568,'small-loading']]){await viewport(w,h);await layout(name);}
 checks.push('Complete health advisory; no layout overlap at 540x960, 390x844, 320x568; correct responsive artwork');
 await viewport(390,844);await c.call('Network.emulateNetworkConditions',{offline:false,latency:20,downloadThroughput:2000000,uploadThroughput:1000000});
 mode='running';for(const p of held)await c.call('Fetch.continueRequest',{requestId:p.requestId});await c.call('Fetch.disable');
 let midShot=false,finished=false;const begin=Date.now();
 while(Date.now()-begin<120000){
  const p=await c.evaluate('document.querySelector("#lg-bar")?.getAttribute("aria-valuenow")??null');
  if(p===null){finished=true;break;}const n=Number(p);progress.push(n);
  if(n>5&&n<95&&!midShot){await shot('actual-download-progress');midShot=true;}
  if(n===100&&!fs.existsSync(out+'/loading-complete.png'))await shot('loading-complete');
  await sleep(150);
 }
 if(!finished||!midShot)throw Error('Real loading sequence incomplete');
 if(progress.some((n,i)=>i&&n<progress[i-1]))throw Error('Progress regressed');
 await until(()=>c.evaluate('window.gardenR1?.level===2&&window.gardenR1?.phase===1'),'Unity level 3 ready');await shot('game-ready');
 checks.push('Actual Unity download gives monotonic progress; loading disappears after successful startup');
 await c.call('Network.emulateNetworkConditions',{offline:false,latency:0,downloadThroughput:-1,uploadThroughput:-1});
 mode='fail';await c.call('Fetch.enable',{patterns:[{urlPattern:'*/Build/*.loader.js*',requestStage:'Request'}]});
 await c.call('Page.navigate',{url:base+'?set=A&level=3&loading-check=failure'});await until(async()=>String(await text()).includes('加载失败'),'Failure feedback');await until(loadedArt,'Failure artwork');await sleep(700);await layout('load-failure');
 checks.push('Failed loader shows refresh message while artwork and health advisory remain visible');
 if(errors.length)throw Error('Unexpected browser errors');
 fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,layouts,progress,expectedFailures:expectedFailures.length,errors},null,2));console.log('LOADING_A_ACCEPTANCE_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,layouts,progress,errors},null,2));throw e;}finally{c.close();}

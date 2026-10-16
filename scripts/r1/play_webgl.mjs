// R1 end-to-end smoke: real browser mouse/touch events only. No model state writes.
// Run with an isolated Chrome on --remote-debugging-port=9342 and local server 8802.
import fs from 'node:fs';
import { connect } from '../../tools/browser/cdp.mjs';
const out=process.argv[2]||'Builds/R1-evidence/play-v1';
if(fs.existsSync(out)) throw Error('Fresh output directory required');
fs.mkdirSync(out,{recursive:true});
const c=await connect({port:Number(process.env.R1_CDP_PORT||9342),onEvent:e=>{
  if(['Runtime.exceptionThrown','Log.entryAdded'].includes(e.method))fs.appendFileSync(out+'/browser-events.jsonl',JSON.stringify(e)+'\n');
}});
await c.call('Page.enable'); await c.call('Runtime.enable'); await c.call('Log.clear'); await c.call('Log.enable');
await c.call('Emulation.setDeviceMetricsOverride',{width:540,height:Number(process.env.R1_HEIGHT||960),deviceScaleFactor:Number(process.env.R1_DPR||1),mobile:false});
await c.call('Emulation.setCPUThrottlingRate',{rate:Number(process.env.R1_CPU_RATE||1)});
await c.call('Emulation.setTouchEmulationEnabled',{enabled:process.env.R1_TOUCH==='1',maxTouchPoints:1});
const pause=ms=>new Promise(r=>setTimeout(r,ms));
// Unity captures DPR at startup. Navigate after device configuration and allow
// the first rendered frame to establish the input hit boxes before interacting.
if(process.env.R1_URL)await c.call('Page.navigate',{url:process.env.R1_URL});
for(let i=0;i<90;i++){
  if(await c.evaluate('!!window.gardenR1 && !!document.querySelector("canvas")'))break;
  if(i===89)throw Error('WebGL did not become ready');
  await pause(1000);
}
await pause(1500);
// Prefab modals use a centered 540x960 RectTransform; battle/header keep ScreenLayout.
// Opt in for v2 prefab builds; v1 replay coordinates remain available.
const prefabUi=process.env.R1_PREFAB_UI==='1';
async function tap(x,y,modal=false){
  const mapped=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),scale=Math.min(r.width/540,r.height/960),height=r.height/scale,stretch=(height-506)/460,ly=${y},py=${modal&&prefabUi?'true':'false'}?ly+(height-960)/2:ly<=114?3+ly:ly<574?3+114+(ly-114)*stretch:3+stretch*460+ly-460;return {x:r.left+${x}*scale,y:r.top+py*scale}})()`);
  x=mapped.x;y=mapped.y;
  fs.appendFileSync(out+'/actions.jsonl',JSON.stringify({type:process.env.R1_TOUCH==='1'?'touch':'mouse',x,y,at:Date.now()})+'\n');
  if(process.env.R1_TOUCH==='1'){
    await c.call('Input.dispatchTouchEvent',{type:'touchStart',touchPoints:[{x,y,id:0}]});
    await pause(80);
    await c.call('Input.dispatchTouchEvent',{type:'touchEnd',touchPoints:[]});
    return;
  }
  await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',x,y});
  await c.call('Input.dispatchMouseEvent',{type:'mousePressed',x,y,button:'left',buttons:1,clickCount:1});
  await pause(65);
  await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',x,y,button:'left',buttons:0,clickCount:1});
}
const wins=new Set(), snaps=new Set(), uiChecks=[], colorShots={}; let last='',attempts=0, pauseChecked=false, homeChecked=false, homeRequested=false;
try{
  for(let i=0;i<1000;i++){
    const m=await c.evaluate('window.gardenR1');
    if(!m){await pause(1000);continue;}
    if(m.home && !m.transition){
      fs.writeFileSync(out+'/home.png',Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));
      if(!homeChecked){
        await tap(270,662);await pause(1200);
        const before=await c.evaluate('window.gardenR1');
        if(!before.settings)throw Error('Home settings did not open');
        fs.writeFileSync(out+'/home-settings.png',Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));
        for(const [y,key] of [[360,'musicMuted'],[418,'soundMuted'],[476,'lightEnabled']]){
          const old=(await c.evaluate('window.gardenR1'))[key];
          await tap(270,y,true);await pause(1200);
          if((await c.evaluate('window.gardenR1'))[key]===old)throw Error('Home toggle did not change: '+key);
          await tap(270,y,true);await pause(1200);
          if((await c.evaluate('window.gardenR1'))[key]!==old)throw Error('Home toggle did not restore: '+key);
        }
        await tap(270,543,true);await pause(1200);
        if((await c.evaluate('window.gardenR1')).settings)throw Error('Home settings did not close');
        homeChecked=true;uiChecks.push('home/settings/all three toggles/close passed');
      }
      await tap(270,568);await pause(1200);
      const started=await c.evaluate('window.gardenR1');
      if(started.home || (homeRequested && started.level!==3))throw Error('Home start did not enter first uncleared level');
      uiChecks.push('home start enters first uncleared level');continue;
    }
    if(!pauseChecked&&m.level===0&&m.phase===1&&!m.transition){
      await tap(36,30);await pause(1200);
      const paused=await c.evaluate('window.gardenR1');
      if(!paused.settings||!paused.paused)throw Error('Pause UI did not pause');
      fs.writeFileSync(out+'/pause.png',Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));
      await tap(270,543,true);await pause(1200);
      const resumed=await c.evaluate('window.gardenR1');
      if(resumed.paused||resumed.settings)throw Error('Continue UI did not resume');
      uiChecks.push('pause/resume via UI passed');pauseChecked=true;continue;
    }
    fs.appendFileSync(out+'/samples.jsonl',JSON.stringify(m)+'\n');
    if(process.env.R1_COLOR_SHOTS==='1' && m.phase===2 && !m.paused && !m.transition && !m.finaleReady && m.remaining>m.total*.2){
      const colors=new Set(m.fleetColors.concat(m.lanes.map(l=>l.color)).filter(c=>c>=0));
      const score=colors.size*10+Math.min(m.fleet,4);
      if(!colorShots[m.level]||score>colorShots[m.level].score){
        const name=`battle-${m.level+1}-colors${colors.size}-fleet${m.fleet}.png`;
        fs.writeFileSync(`${out}/${name}`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));
        colorShots[m.level]={file:name,score,colors:[...colors],sample:m};
      }
    }
    // MAX (tier 3) attack review: first 3 frames with a tier-3 ship on the pads (field added in v3; absent before).
    if(m.maxTier>=3&&m.phase===2&&!m.paused&&!m.transition&&(snaps.maxShots||0)<3){
      snaps.maxShots=(snaps.maxShots||0)+1;
      fs.writeFileSync(`${out}/max-${m.level+1}-${snaps.maxShots}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));
    }
    const tag=`${m.level}-${m.phase}`;
    if(!snaps.has(tag)){
      fs.writeFileSync(`${out}/${tag}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));snaps.add(tag);
    }
    if(tag!==last){console.log(JSON.stringify(m));last=tag;}
    if(m.phase===4){
      wins.add(m.level);
      if(m.level===3){console.log('FOUR_LEVELS_WON');break;}
      if(m.level===2 && !homeChecked){
        if(!homeRequested){await pause(1600);await tap(270,760,true);homeRequested=true;}
        await pause(1100);continue;
      }
      if(m.level>=2){await pause(1600);await tap(270,680,true);}
    } else if(m.phase===3){await tap(270,645);}
    else if(m.home){ await pause(1000);continue; }
    else if(m.finaleReady){await tap(270,759);}
    else if(!m.paused&&!m.transition&&[1,2].includes(m.phase)){
      const candidates=m.lanes.map((l,i)=>({...l,i})).filter(l=>l.color>=0);
      let chosen=candidates.find(l=>l.merge&&l.exposed)||candidates.find(l=>l.merge)||candidates.find(l=>l.exposed&&!m.fleetColors.includes(l.color))||candidates.find(l=>l.exposed);
      if(!chosen&&m.fleet<5)chosen=candidates[0];
      if(chosen){await tap(20+(chosen.i+.5)*500/m.lanes.length,725);attempts++;}
    }
    await pause(1050);
  }
  const history=await c.evaluate('window.gardenR1History');
  const environment=await c.evaluate(`(()=>{const cv=document.querySelector('canvas'),gl=cv.getContext('webgl2')||cv.getContext('webgl'),ext=gl.getExtension('WEBGL_debug_renderer_info');return {userAgent:navigator.userAgent,renderer:ext?gl.getParameter(ext.UNMASKED_RENDERER_WEBGL):'',viewport:[innerWidth,innerHeight],canvasRect:[cv.clientWidth,cv.clientHeight],backing:[cv.width,cv.height],dpr:devicePixelRatio,cores:navigator.hardwareConcurrency}})()`);
  const result={environment,uiChecks,colorShots,prefabUi,touch:process.env.R1_TOUCH==='1',cpuThrottle:Number(process.env.R1_CPU_RATE||1),wins:[...wins],attempts,final:await c.evaluate('window.gardenR1'),history};
  fs.writeFileSync(out+'/result.json',JSON.stringify(result,null,2));
  console.log(JSON.stringify({wins:result.wins,attempts,final:result.final}));
  if(result.wins.length!==4||result.final.level!==3||result.final.phase!==4)throw Error('Did not win all four levels');
  if(!pauseChecked)throw Error('Pause/resume was not exercised');
  if(!homeChecked)throw Error('Home/settings/progression were not exercised');
}finally{c.close();}

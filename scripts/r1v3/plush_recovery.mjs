// Let normal board pressure trigger revive/loss; never force game state.
import fs from 'node:fs';
import {connect} from '../../tools/browser/cdp.mjs';
const out=process.argv[2];if(!out||fs.existsSync(out))throw Error('Fresh evidence required');fs.mkdirSync(out,{recursive:true});
const pause=ms=>new Promise(r=>setTimeout(r,ms)),errors=[],checks=[];
const c=await connect({port:Number(process.env.R1_CDP_PORT||9346),onEvent:e=>{if(e.method==='Runtime.exceptionThrown'||(e.method==='Log.entryAdded'&&e.params.entry.level==='error'))errors.push(e);}});
const sample=()=>c.evaluate('window.gardenR1');
async function until(fn,label,seconds=180){for(let i=0;i<seconds;i++){let m;try{m=await sample();}catch{}if(m&&fn(m))return m;await pause(1000);}throw Error(label);}
async function shot(n){fs.writeFileSync(`${out}/${n}.png`,Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));}
async function tap(x,y,modal=false){const p=await c.evaluate(`(()=>{const r=document.querySelector('canvas').getBoundingClientRect(),s=Math.min(r.width/540,r.height/960),h=r.height/s,y=${y};return {x:r.left+${x}*s,y:r.top+(${modal}?y+(h-960)/2:y+3+(h-966)*Math.max(0,Math.min(1,(y-60)/396)))*s};})()`);await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...p});await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...p,button:'left',buttons:1,clickCount:1});await pause(70);await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...p,button:'left',buttons:0,clickCount:1});}
try{
 await c.call('Page.enable');await c.call('Runtime.enable');await c.call('Log.enable');await c.call('Emulation.setDeviceMetricsOverride',{width:540,height:960,deviceScaleFactor:1,mobile:false});
 await c.call('Page.navigate',{url:(process.env.R1_URL||'http://127.0.0.1:8806/')+'?set=A&level=7'});
 await until(m=>m.level===6&&!m.home&&!m.transition&&m.phase===1,'Level ready',90);await pause(1500);
 await tap(170,650);await until(m=>m.phase===2,'Deploy',15);
 const offer=await until(m=>m.phase===3,'Normal pressure did not offer revival');await shot('revive');
 await tap(270,700,true);const revived=await until(m=>m.phase===2,'Revive action',20);await pause(1200);await shot('revived');if((await sample()).selection===2){await tap(270,740);await until(m=>m.selection===0&&!m.paused,'Revive bomb confirm',15);}checks.push('revive / extra slot / bomb reward');
 if(revived.unlockedSlots!==offer.unlockedSlots+1)throw Error('Revive slot reward mismatch');
 await until(m=>m.phase===5,'Normal pressure did not produce loss');await pause(2200);await shot('lose');checks.push('loss presentation');
 await tap(270,751,true);await until(m=>m.phase===1&&!m.transition,'Retry action',30);await shot('retry');checks.push('retry');
 await tap(170,650);await until(m=>m.phase===3,'Second revive offer');await shot('decline-offer');
 await c.call('Emulation.setDeviceMetricsOverride',{width:390,height:844,deviceScaleFactor:1,mobile:false});await pause(1200);await shot('revive-tall');
 await tap(482,232,true);await until(m=>m.phase===5,'Decline via close cross',20);await pause(1800);await shot('decline-loss');checks.push('390x844 revive / close cross declines revival');
 const lost=await sample();await tap(442,751,true);await pause(1200);const share=await sample();
 if(share.phase!==5||share.level!==lost.level||share.home||share.dayAds!==lost.dayAds)throw Error('Share placeholder must not change game state');
 await tap(98,751,true);await until(m=>m.home&&!m.transition,'Loss home',30);await shot('loss-home');checks.push('share placeholder / loss home');
 if(errors.length)throw Error('Browser errors');fs.writeFileSync(out+'/result.json',JSON.stringify({passed:true,checks,errors,offer,revived},null,2));console.log('PLUSH_RECOVERY_PASSED');
}catch(e){await shot('failure').catch(()=>{});fs.writeFileSync(out+'/result.json',JSON.stringify({passed:false,error:String(e),checks,errors,last:await sample().catch(()=>null)},null,2));throw e;}
finally{c.close();}

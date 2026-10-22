import fs from 'node:fs';import path from 'node:path';import os from 'node:os';import {spawn} from 'node:child_process';
import {connect} from '../../../tools/browser/cdp.mjs';
const dir=path.dirname(new URL(import.meta.url).pathname),qa=path.join(dir,'_qa'),profile=fs.mkdtempSync(path.join(os.tmpdir(),'r1-codex-design-')),sleep=ms=>new Promise(r=>setTimeout(r,ms));
let c,chrome;
try{
 chrome=spawn('/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',['--headless=new','--remote-debugging-port=0','--user-data-dir='+profile,'--no-first-run','--no-default-browser-check','--use-angle=swiftshader','--enable-unsafe-swiftshader','http://127.0.0.1:8792/?level=7'],{stdio:'ignore'});
 for(let i=0;i<100&&!fs.existsSync(path.join(profile,'DevToolsActivePort'));i++)await sleep(100);
 const port=Number(fs.readFileSync(path.join(profile,'DevToolsActivePort'),'utf8').split('\n')[0]);
 for(let i=0;i<20;i++){try{c=await connect({port,path:'/?level=7'});break;}catch{await sleep(150);}}
 if(!c)throw Error('Could not connect isolated preview Chrome');
 await c.call('Page.enable');await c.call('Runtime.enable');
 await c.call('Emulation.setDeviceMetricsOverride',{width:540,height:960,deviceScaleFactor:2,mobile:false});
 let state;for(let i=0;i<100;i++){try{state=await c.evaluate('window.gardenR1');}catch{} if(state)break;await sleep(400);}
 if(!state)throw Error('Game not ready');await sleep(1500);
 async function tap(x,y){await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',x,y});await sleep(80);await c.call('Input.dispatchMouseEvent',{type:'mousePressed',x,y,button:'left',buttons:1,clickCount:1});await sleep(70);await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',x,y,button:'left',buttons:0,clickCount:1});await sleep(700);}
 if(!state.home){await tap(34,35);fs.writeFileSync(path.join(qa,'pause-before-home.png'),Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));await tap(122,586);await sleep(500);state=await c.evaluate('window.gardenR1');}
 if(!state.home)throw Error('Normal home navigation did not reach home');
 fs.writeFileSync(path.join(qa,'current-home.png'),Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));
 const r=await c.evaluate('(()=>{const r=document.querySelector("canvas").getBoundingClientRect();return {x:r.left+500*r.width/540,y:r.top+438*r.height/960}})()');
 await c.call('Input.dispatchMouseEvent',{type:'mouseMoved',...r});
 await c.call('Input.dispatchMouseEvent',{type:'mousePressed',...r,button:'left',buttons:1,clickCount:1});await sleep(70);
 await c.call('Input.dispatchMouseEvent',{type:'mouseReleased',...r,button:'left',buttons:0,clickCount:1});await sleep(1500);
 fs.writeFileSync(path.join(qa,'current-codex.png'),Buffer.from((await c.call('Page.captureScreenshot')).data,'base64'));
 fs.writeFileSync(path.join(qa,'capture-record.json'),JSON.stringify({source:'Isolated headless Chrome, local existing WebGL build, fresh profile; normal input only; no game-state injection',before:state,click:r,after:await c.evaluate('window.gardenR1'),buildTime:await c.evaluate('document.querySelector("meta[name=build-time]")?.content')},null,2)+'\n');
 console.log('Captured current-codex.png using normal input in a fresh isolated profile.');
}finally{if(c)c.close();if(chrome){chrome.kill('SIGTERM');await sleep(500);}fs.rmSync(profile,{recursive:true,force:true});}


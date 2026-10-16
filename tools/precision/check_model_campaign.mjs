import fs from 'node:fs';
import zlib from 'node:zlib';
import crypto from 'node:crypto';
import readline from 'node:readline';
import assert from 'node:assert/strict';
import path from 'node:path';
import {spawn} from 'node:child_process';
const [input, output, waitFlag] = process.argv.slice(2), sha = b => crypto.createHash('sha256').update(b).digest('hex');
const campaign = JSON.parse(fs.readFileSync(`${input}/campaign.json`));
const report = { passed: true, cases: [], scope: campaign.mode==='specials'
  ? 'Frozen nonwinning model scripts in Unity Mono, with actual candidate actions driven by the declared source script, per-frame state and seven array hashes, controller/checkpoint fields and saved profiles. Scripted cases do not tick Pilot; any legacy victory control remains separate. No Awake/PlayerLoop, tutorial UI reachability, graphics, audio or natural progression claim.'
  : 'Frozen original Battle/controller model adapter, in Unity Mono; actual candidate Pilot decisions, per-frame state, seven original array hashes, controller state and saved profile fields. No Awake, PlayerLoop, visuals, sound or normal progression claim.' };
async function* rows(file, hash) {
  const source=fs.createReadStream(file);
  let child, decoded, completed, stderr='';
  if(file.endsWith('.zst')) {
    child=spawn('zstd',['-dc'],{stdio:['pipe','pipe','pipe']});
    completed=new Promise((resolve,reject)=>{child.once('error',reject);child.once('close',code=>resolve(code));});
    child.stderr.on('data',b=>stderr+=b.toString());
    source.pipe(child.stdin);decoded=child.stdout;
  } else {assert.ok(file.endsWith('.gz'));decoded=source.pipe(zlib.createGunzip());}
  if(hash)decoded.on('data',b=>hash.update(b));
  const reader=readline.createInterface({input:decoded,crlfDelay:Infinity});
  try {
    for await (const line of reader) if(line)yield JSON.parse(line);
    if(completed)assert.equal(await completed,0,stderr);
  } finally {source.destroy();reader.close();if(child&&child.exitCode===null)child.kill();}
}
async function fileHash(file){const h=crypto.createHash('sha256');for await(const b of fs.createReadStream(file))h.update(b);return h.digest('hex');}
const buffer = new ArrayBuffer(4), view = new DataView(buffer);
function number(bits) {view.setInt32(0,bits,true);const value=view.getFloat32(0,true);return Object.is(value,-0)?'-0':value;}
for (const c of campaign.cases) {
  if(waitFlag==='--wait') {
    const deadline=Date.now()+1800000;
    while(!fs.existsSync(`${output}/${c.name}/report.json`)) {
      if(Date.now()>deadline)throw Error(`Timed out waiting for completed Unity case ${c.name}`);
      await new Promise(resolve=>setTimeout(resolve,3000));
    }
  }
  const source = JSON.parse(fs.readFileSync(`${input}/${c.name}/source.json`));
  for (const file of [...source.files,...source.prepared])assert.equal(await fileHash(file.path),file.sha256,file.path);
  const state=JSON.parse(fs.readFileSync(`${output}/${c.name}/report.json`));
  const trace=source.files.find(f=>/trace\.jsonl\.(gz|zst)$/.test(f.path)).path, traceHash=crypto.createHash('sha256');
  const arrayFiles=['gz','zst'].map(ext=>`${output}/${c.name}/arrays.jsonl.${ext}`).filter(file=>fs.existsSync(file));
  assert.equal(arrayFiles.length,1,`${c.name}: expected exactly one candidate array stream`);
  const arrayFile=arrayFiles[0], original=rows(trace,traceHash), candidate=rows(arrayFile);
  let frames=0, hashes=0, failureCount=0;const failures=[];
  for await(const expected of original){
    const next=await candidate.next();assert.ok(!next.done,`${c.name}: candidate truncated at ${frames}`);const actual=next.value;
    assert.equal(actual.frame,expected.frame);
    for(const key of ['Alive','sand','x','y','flow','position','fronts']){
      const values=actual[key].map(v=>key==='Alive'?!!v:['x','y','flow'].includes(key)?number(v):v);
      const actualHash=sha(Buffer.from(JSON.stringify(values))), expectedHash=expected.battle[key].sha256;
      if(actualHash!==expectedHash){failureCount++;if(failures.length<30)failures.push({frame:frames,key,actual:actualHash,expected:expectedHash});}
      hashes++;
    }
    frames++;
  }
  assert.ok((await candidate.next()).done);assert.equal(traceHash.digest('hex'),source.uncompressedTraceSha256);
  assert.equal(frames,source.frames);
  const row={name:c.name,passed:state.passed&&failureCount===0,frames,arrayHashes:hashes,failureCount,failures,state,
    sourceManifestSha256:await fileHash(`${input}/${c.name}/source.json`),arraysFile:arrayFile,arraysSha256:await fileHash(arrayFile)};
  fs.writeFileSync(`${output}/${c.name}/combined.json`,JSON.stringify(row,null,2)+'\n',{flag:'wx'});
  report.cases.push(row);report.passed&&=row.passed;
  console.log(JSON.stringify({name:c.name,passed:row.passed,frames,arrayHashes:hashes,stateFailures:state.failureCount,arrayFailures:failureCount}));
}
fs.writeFileSync(`${output}/combined.json`,JSON.stringify(report,null,2)+'\n',{flag:'wx'});
if(!report.passed)process.exitCode=1;

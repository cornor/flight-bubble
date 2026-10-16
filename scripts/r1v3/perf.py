#!/usr/bin/env python3
"""Measure a dense four-lane level and a five-lane level, rejecting concurrent test workloads."""
import datetime, json, os, re, subprocess, sys, time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
out=Path(sys.argv[1]).resolve()
def snapshot(allow_own=False):
    rows=subprocess.check_output(['ps','-axo','pid=,args='],text=True).splitlines()
    busy=[]
    for row in rows:
        if re.match(r'\s*\d+ /Applications/Unity/Hub/Editor/.*/MacOS/Unity -batchmode',row): busy.append(row.strip())
        elif re.match(r'\s*\d+ \S*node .*check_model_campaign\.mjs(?: |$)',row): busy.append(row.strip())
        elif re.match(r'\s*\d+ /Applications/Google Chrome.app/Contents/MacOS/Google Chrome .*--headless',row):
            if not (allow_own and '--user-data-dir=/tmp/flight-bubble/' in row): busy.append(row.strip())
    return dict(at=datetime.datetime.now().astimezone().isoformat(),otherWorkloads=busy)
pre=snapshot()
if pre['otherWorkloads']: raise SystemExit('Wait for Unity/headless Chrome/regression checker to finish: '+json.dumps(pre))
out.mkdir(parents=True)
env=dict(os.environ,R1_HEIGHT='1170',R1_DPR='1.5',R1_CPU_RATE='4',R1_TOUCH='1',R1_LEVELS='4,7',R1_SECONDS='65',R1_INPUT_INTERVAL='1050')
observations=[pre]
with (out/'sample.log').open('w') as log:
    child=subprocess.Popen(['bash','scripts/r1v3/sample.sh',str(out/'play')],cwd=ROOT,env=env,stdout=log,stderr=subprocess.STDOUT)
    while child.poll() is None:
        observations.append(snapshot(True)); time.sleep(3)
observations.append(snapshot(True))
(out/'isolation.json').write_text(json.dumps(observations,indent=2)+'\n')
isolated=all(not x['otherWorkloads'] for x in observations)
result=dict(passed=False,isolated=isolated,sampleExitCode=child.returncode,criteria=dict(denseSeconds=60,meanFps=55,p95ms=25),levels=[])
if child.returncode==0:
    samples=json.loads((out/'play/result.json').read_text())
    result['environment']=samples['environment'];result['cpuThrottle']=samples['cpuThrottle'];result['touch']=samples['touch']
    for row in samples['results']:
        m=row['final'];result['levels'].append(dict(level=row['level'],lanes=len(m['lanes']),observedSeconds=row['observedSeconds'],frames=m['frames'],seconds=m['wallSeconds'],fps=m['fps'],p95ms=m['p95ms'],denseFrames=m['denseFrames'],denseSeconds=m['denseSeconds'],denseFps=m['denseFps'],denseP95ms=m['denseP95ms']))
    fourth=result['levels'][0]
    result['passed']=isolated and samples['complete'] and fourth['level']==4 and fourth['denseSeconds']>=60 and fourth['denseFps']>=55 and fourth['denseP95ms']<=25 and result['levels'][1]['lanes']==5
(out/'report.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result));raise SystemExit(0 if result['passed'] else 1)

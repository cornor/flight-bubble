from pathlib import Path
import sys, json, shutil, hashlib

Q=Path(__file__).resolve().parent
D=Q.parent.parent
key, attempt_text, source = sys.argv[1:]
attempt=int(attempt_text)
log=json.loads((Q/'generation-log.json').read_text())
asset=next(a for a in log['assets'] if a['key']==key)
assert 1<=attempt<=4
assert not any(a['attempt']==attempt for a in asset['attempts'])
destination=D/f"_originals/redesign-{asset['number']}-{key}-attempt-{attempt:02d}.png"
assert not destination.exists()
shutil.copy2(source,destination)
entry={'attempt':attempt,'original':str(destination),'source':source,'sha256':hashlib.sha256(destination.read_bytes()).hexdigest(),'prompt':str(Q/f"{asset['number']}-{key}-attempt-{attempt:02d}-prompt.txt"),'status':'pending review'}
asset['attempts'].append(entry)
(Q/'generation-log.json').write_text(json.dumps(log,indent=2)+'\n')
print(json.dumps({'key':key,**entry}))

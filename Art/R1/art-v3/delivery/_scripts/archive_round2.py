from pathlib import Path
import sys,json,hashlib,shutil
B=Path(__file__).resolve().parents[1]
record=json.loads(sys.argv[1]);p=B/'third-batch-round2-prompts.json';data=json.loads(p.read_text());item=next(x for x in data['items'] if x['key']==record['key'])
dest=B/'_originals'/f"r2-{record['key']}-attempt-{record['attempt']:02}.png"
assert not dest.exists(),dest
shutil.copy2(record['source'],dest);record['archive_path']=str(dest);record['sha256']=hashlib.sha256(dest.read_bytes()).hexdigest();record['executed_prompt']=item['prompt']+record['extra_prompt'];record.setdefault('reference_paths',item['reference_paths']);item['attempts'].append(record)
p.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n');print(dest)

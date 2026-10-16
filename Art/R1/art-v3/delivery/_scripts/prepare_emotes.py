"""Prepare one native generation request per requested emote, preserving source prompts."""
from pathlib import Path
import json,sys,hashlib
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
P=ROOT/'emotes-58-59-prompts.json'
m=json.loads(P.read_text()); requests=[]
for key in sys.argv[1:]:
    item=next(x for x in m['items']if x['key']==key)
    attempt=len(item['attempts'])+1
    assert attempt<=4
    assert hashlib.sha256(Path(item['idle_path']).read_bytes()).hexdigest()==item['idle_sha256']
    refs=item['reference_images'].copy()
    name,hx,pose=item['character'],item['color'],item['pose']
    if pose=='blink':
        extra=f'Production constraint: the attached image is the exact {name} {hx} idle sprite and the edit target. Preserve its 224x256 canvas and exact placement, no zoom or recentering. Close BOTH eyes into simple relaxed curved lines; remove the entire white eyeballs, pupils and eye highlights, replacing those areas with matching body shading. Keep the mouth, eyebrows, cheek forms, body highlights and all other anatomy unchanged. Output ONE full sprite, transparent background.'
    elif hx in ['#42A9FF','#FF9D30']:
        source_hex='2982F4'if name=='blue'else'FF7E27'
        target=ROOT/f'creatures/{name}/{source_hex}/cheer.png'
        assert target.exists()
        refs=[str(target)]+refs
        extra=f'Image 1 is the exact approved cheer sprite to recolor. Image 2 is this color version\'s idle sprite as color reference; images 3 and 4 are style/identity references only. Preserve the source cheer silhouette, alpha, face, highlights, eyes, pose, canvas and framing. Recolor the body only to {hx}. One transparent 224x256 sprite.'
    else:
        extra=f'Generate ONLY {name}, main body color {hx}. Image 1 is the authoritative character identity and proportions; image 2 is style only; image 3 is the approved cast reference, do not draw other members. The first image overrides generic cast descriptions if they differ. Retain this exact face design and distinctive anatomy. Show BOTH eyes fully closed as happy upward arches, a wide open joyful mouth, existing appendages raised, both feet planted, no new limbs or props. Single full-body sprite on transparent 224x256 canvas. Keep the plump body as broad as in the idle sprite; allow a small upward stretch but do not make it skinny or change species. Whole silhouette fits x=7..216 and y=30..211, bottom boundary y=212. Body width must stay within 5 percent of the idle body width. No frame, labels, effects or floor.'
    if item.get('retry_notes'):extra+='\n'+item['retry_notes']
    req={'key':key,'attempt':attempt,'prompt':item['base_prompt']+'\n\n'+extra,
         'reference_images':refs,'transparent_background':True,'status':'requested',
         'reference_metadata':[{'path':r,'sha256':hashlib.sha256(Path(r).read_bytes()).hexdigest(),'size':list(Image.open(r).size)}for r in refs]}
    item['attempts'].append(req.copy());item['status']='generating';requests.append(req)
P.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(requests,ensure_ascii=False))

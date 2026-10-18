from pathlib import Path
import json,re,hashlib,sys,shutil
Q=Path(__file__).resolve().parent
D=Q.parent.parent
A=D.parent
P=A.parents[2]
STATES=['idle','attack','spent','blink','cheer']
CHARS={
 'charcoal':('2D2D2D','A','a fluffy round charcoal soot sprite, a perimeter of soft curved fur tufts (NOT needles or round grape lobes), short arms and tiny feet, large WHITE eye sclera with star catchlights, pale gray oval belly, pale gray single outline'),
 'slate-blue':('696CAF','A','a slate lavender seal pup with a teardrop body, two shoulder flippers as arms, ONE flat paddle tail emerging behind on the LEFT; NO feet, NO horn, NO cheek fins, NO forked fish tail; large white eyes, softly paler same-hue belly and cheeks, pale lavender outline'),
 'mint':('30D183','A','a mint green frog-like wide round creature, two big round EYE DOMES at the top containing actual white eyeballs and pupils (not ears), a wide mouth, short arms and feet, pale mint belly and cheek patches'),
 'forest':('0C7E1C','A','a forest green creature made from TWO ROUND BUBBLES stacked vertically, smaller head above a larger body with a clearly pinched neck; large white eyes on upper bubble, pale green belly on lower bubble, two short arms and feet, clean pale green single outline; NO ring, NO pear silhouette'),
 'berry':('9C225E','B','a wine-magenta round creature with two small arms and feet, ONE elegant long curved tail growing from its RIGHT SIDE and sweeping upward to a round bubble at its tip; no hair, no bun, no horns, no bottom ghost wisp; large white eyes, paler pink cheek accents and subtle belly, pale pink outline')}

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def setup():
 assert not (Q/'config.json').exists(),'already prepared'
 style=re.search(r'## 通用画风段.*?```text\n(.*?)\n```',(A/'美术AI提示词.md').read_text(),re.S).group(1)
 mock=P/'Art/Concepts/bubble-squad-v02/01-smiley-intro.png'
 assert mock.exists(),mock
 config={'style':style,'mockup':str(mock),'characters':{},'states':STATES}
 for ident,(hx,variant,desc) in CHARS.items():
  config['characters'][ident]={'id':ident,'hex':hx,'variant':variant,'description':desc,'reference':str(D/'redesign'/f'{ident}-{variant}.png'),'outputDir':str(D/'creatures'/ident/hx),'attempts':{}}
 baseline={str(p):sha(p) for p in (D/'creatures').rglob('*.png')}
 baseline.update({str(p):sha(p) for p in (D/'redesign').glob('*.png')})
 (Q/'protected-baseline.json').write_text(json.dumps(baseline,indent=2)+'\n')
 m=D/'交付清单.md';(Q/'manifest-baseline.json').write_text(json.dumps({'length':len(m.read_bytes()),'sha256':sha(m)},indent=2)+'\n')
 (Q/'config.json').write_text(json.dumps(config,indent=2)+'\n')

def prompt(ident,state,attempt=1):
 c=json.loads((Q/'config.json').read_text());it=c['characters'][ident]
 pose={
 'idle':'READY pose: standing firmly at rest, alert but friendly, looking slightly UP, arms or flippers relaxed slightly out from the sides, small closed smile. NOT celebrating. Keep the recognizable silhouette from the approved design.',
 'attack':'ATTACK pose: cheeks visibly puffed with air, head tilted back, small round O mouth on upper face pointing UP to exhale. Eyes look upward. Body slightly compressed vertically with elastic energy, arms kept close. No projectile, no bubbles, no breath stream.',
 'spent':'SPENT pose after running out of shots: visibly deflated, sagging and leaning gently to one side, both eyes half closed with tired lids, small tired mouth, exactly ONE small sweat drop by the temple. Body slightly dulled but still clearly its main color. Keep height within 85–100 percent of ready, not flattened into a puddle.',
 'cheer':'CELEBRATING a victory: BOTH eyes closed into happy upward arcs, a big open happy smile, cheeks a little rosier, body stretched slightly upward with joy, little arms/flippers raised up HIGH. Feet/body bottom still touching the same baseline, NOT jumping. Keep main torso diameter and whole silhouette maximum width within 5 percent of the ready sprite; raise limbs vertically rather than spreading wide.',
 'blink':'Close the character\'s eyes ONLY: replace each open eye with a short gently curved closed-eye line in the character\'s outline color, a relaxed happy blink, at the same position and same size as the open eye. Remove eye highlights and white sclera, replacing eye interior with matching body color. Keep EVERYTHING else pixel-identical: silhouette, color, lighting, outline, mouth, cheeks, eyebrows, limbs, pose, position, size, alpha and transparent background. Do not move the eyebrows.'}[state]
 staged=Q/'staging'/ident/'idle.png'
 if state=='blink':
  refs=[str(staged),it['reference'],c['mockup']]
  framing='Image 1 is the exact edit target idle sprite, 224x256. Image 2 is identity/color reference. Image 3 is style only. Preserve image 1 exactly outside the eye rectangles. Output the SAME framing, margins, character position, scale and proportions. Do not use image 2 pose.'
 else:
  refs=[it['reference'],c['mockup']]
  framing='Image 1 is the APPROVED character design: preserve this identity and all distinguishing features. Image 2 is style only, NOT character identity. Make ONE full-body sprite, centered horizontally on a square transparent canvas. Character lowest point around y=900 on a 1024x1024 canvas; occupy about 720 pixels in height. No cropping. Preserve body proportions, glossy finish and clear face.'
  if staged.exists() and state!='idle':
   refs=[str(staged),it['reference'],c['mockup']]
   framing='Image 1 is this character\'s READY battle sprite, the pose/scale reference. Image 2 is APPROVED character design for identity and details. Image 3 is style only. Draw ONE pose of exactly the same creature. Keep torso diameter and head-to-body ratio as in image 1. Use a square transparent canvas with the character centered, full-body, baseline around y=900 on a 1024x1024 canvas, about 720 pixels tall. No cropping.'
 text=c['style']+'\n\nUse case: identity-preserve. Game battle sprite, transparent PNG.\n'+framing+'\n\nCharacter: '+it['description']+'. Main color #'+it['hex']+'. Lit and midtone body paint must match this hex (CIELAB DeltaE76 <=8); shadows only darken without shifting hue. Preserve large white eyes in open-eye poses, pale belly/cheeks, pale clean outline for dark-background readability. Do not make the whole body pale.\n\n'+pose+'\n\nONE character and ONE pose only. No effects, extra detached bubbles, projectiles, confetti, stars outside eyes, trails, motion lines, pads, ground, ground shadows or glow. No text, letters, numbers, watermark. Actual transparent background, clean anti-aliased edges.\n'
 path=Q/f'{ident}-{state}-attempt-{attempt:02d}-prompt.txt';path.write_text(text)
 return {'id':ident,'state':state,'attempt':attempt,'prompt':text,'promptPath':str(path),'refs':refs}

def register(ident,state,attempt,source):
 c=json.loads((Q/'config.json').read_text());it=c['characters'][ident]
 dest=D/'_originals'/f'redesign-set-{ident}-{state}-attempt-{attempt:02d}.png'
 assert not dest.exists();shutil.copy2(source,dest)
 it['attempts'].setdefault(state,[]).append({'attempt':attempt,'original':str(dest),'source':source,'sha256':sha(dest),'prompt':str(Q/f'{ident}-{state}-attempt-{attempt:02d}-prompt.txt')})
 (Q/'config.json').write_text(json.dumps(c,indent=2)+'\n');return str(dest)

if __name__=='__main__':
 if sys.argv[1]=='setup':setup()
 elif sys.argv[1]=='prompt':print(json.dumps(prompt(sys.argv[2],sys.argv[3],int(sys.argv[4]) if len(sys.argv)>4 else 1)))
 elif sys.argv[1]=='register':print(register(sys.argv[2],sys.argv[3],int(sys.argv[4]),sys.argv[5]))

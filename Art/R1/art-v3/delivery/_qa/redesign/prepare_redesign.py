from pathlib import Path
import re, json, hashlib

ROOT = Path(__file__).resolve().parents[6]
ART = ROOT / 'Art/R1/art-v3'
Q = ART / 'delivery/_qa/redesign'
if (Q / 'generation-log.json').exists():
    raise SystemExit('Generation records already exist; refusing to reset prompts or history.')
Q.mkdir(parents=True, exist_ok=True)
(ART / 'delivery/redesign').mkdir(parents=True, exist_ok=True)
text = (ART / '\u7f8e\u672fAI\u63d0\u793a\u8bcd.md').read_text()
style_section = text.split('## \u901a\u7528\u753b\u98ce\u6bb5', 1)[1]
style = re.search(r'```text\n(.*?)\n```', style_section, re.S).group(1)
section = re.split(r'^## 14\.', text, maxsplit=1, flags=re.M)[1]
template = re.search(r'```text\n(.*?)\n```', section, re.S).group(1)
extras = {
    'charcoal-A': 'Keep the body round and plump with SOFT rounded fluffy tufts, not sharp triangular needles, not a virus. No round ears or horns. Both short arms raised, large open white eyes with tiny star-shaped catchlights. Pale gray belly is an accent, the main body remains #2D2D2D. An original bubble creature, no resemblance to existing film soot characters.',
    'charcoal-B': 'One SHORT soft upright curled smoke wisp centered atop the head, not a horn and not a wide ocean-wave crest. Smooth round body, no fur, spikes, ears, or horns. Both stubby arms raised, relaxed sleepy open eyes with generous visible whites and a small joyful half-smile. Keep charcoal #2D2D2D dominant.',
    'slate-blue-A': 'A visibly SEAL-like teardrop torso, upper body narrow and lower body plump; two arm flippers raised separately at the shoulders, one broad FLAT paddle tail behind and visible to one side. No round cheek fins, no fish tail fork, no horn, no ears, no whiskers. Face looks forward; no extra tiny legs that hide the seal silhouette. Keep muted slate #696CAF, not vivid blue or purple.',
    'slate-blue-B': 'Make the large spiral bubble SHELL clearly visible beside and behind the forward-facing head/body, dominating one side of the silhouette. Eyes on the FACE, NO antennae, no eye-stalks, no horns. Both little arms raised in celebration. The head/body and shell are all #696CAF with pale cool-blue edge accents; no brown shell.',
    'mint-A': 'The two top domes are the FROG\'S ACTUAL EYES: each dome visibly contains a large white eyeball and a pupil, with a wide smiling mouth below. They are not empty pigtail ornaments or bear ears. Wide round glossy body with both short arms raised; no fly, tongue extended, plant or lily pad. Keep #30D183.',
    'mint-B': 'The wave crest is BROAD and low, flowing across most of the top of the head, with a sideways curling breaking-wave lip. It is not a small upright smoke curl, horn, pair of ears, or spiky flame. Round plump dumpling body and two arms raised, eyes on the face below the broad crest. Keep #30D183.',
    'forest-A': 'TWO clearly separate spherical masses in a snowman-like silhouette, with a visible narrower neck: smaller HEAD sphere over a larger BODY sphere. Put the large bright white eyes and happy brave smile on the HEAD sphere, and a pale green belly accent on the lower sphere. Both arms raised. No blended pear silhouette. Main body remains deep #0C7E1C; light green rim is a single clean contour, not a second outer halo.',
    'forest-B': 'The waist ring consists of small round bubbles attached around the equator of ONE central round body; it is an intrinsic bubble feature, no fabric, clothing, accessory straps or separate held object. Preserve a clear round head/body above the horizontal bubble ring, bright white eyes, cheerful brave smile, both arms up. No stacked second head sphere, no back humps. Main body #0C7E1C; single light-green outline, no halo.',
    'berry-A': 'Exactly TWO SMALL adjacent round bubble lobes touch each other at the TOP CENTER of the tall egg-shaped body, like a compact bubble bow. Do not put them far apart at the upper sides: those would copy the pink pigtail creature. No stem, no flame, no leaf, no bangs. Proud raised eyebrow, large white eyes and smile, both arms raised. Main body rich wine-magenta #9C225E, no purple hue shift.',
    'berry-B': 'A round body WITH two little feet and two arms raised. One LONG elegant slender tail extends from the SIDE and curves outward and upward, ending in a small round bubble. The tail must be clearly separate from the round body, not a tapered bottom wisp replacing the feet. No head bun, ears, horns or flame. Proud graceful happy face, main color #9C225E throughout.',
    'purple-A': 'This is the existing PURPLE character, not a new species. Preserve the bean-shaped body and TWO LONG HOLLOW EAR LOOPS, one on each side, drooping in symmetric rounded arcs. The central holes of the loops remain visible. Both little hands raised separately below the ears. For this variant the mouth MUST be CLOSED in a gentle happy smile (overrides the generic big smile), not a large open mouth. No ear sticking straight up. Use high-resolution blue reference only for detail and glossy lighting, never blue anatomy.',
    'purple-B': 'This is the existing PURPLE bean-shaped character. TWO LONG HOLLOW EAR LOOPS with visible open centers: one hangs down and the other bends playfully in a soft sideways/downward arc, NOT sticking vertically straight up. Both little hands raised separately below the ears. ONE eye winks, the other has a large white area. A SMALL open smile, not a huge mouth. Use the high-resolution blue reference only for detail and glossy lighting, never blue anatomy.'
}
assets=[]
mock = ROOT / 'Art/Concepts/bubble-squad-v02/01-smiley-intro.png'
lineup = ART / 'lineup/02-lineup-new.png'
blue_hd = ROOT / 'Art/R1/home-v1/delivery/_originals/H3-attempt-01.png'
for line in section.splitlines():
    if not re.match(r'\| 8[1-6] \|',line):
        continue
    cells=[x.strip() for x in line.strip('|').split('|')]
    number, ident, variant, feature, mood=cells
    ident, hexcode=ident.split()
    key=f'{ident}-{variant}'
    old=ART / f'delivery/creatures/{ident}/{hexcode[1:]}/cheer.png'
    refs=[mock,old,lineup] if ident!='purple' else [mock,old,blue_hd]
    prompt=template.replace('[STYLE]',style).replace('{HEX}',hexcode).replace('{FEATURE}',feature).replace('{MOOD}',mood)
    prompt+='\n\nDelivery framing: one character only on a 1024x1024 transparent RGBA square. Character horizontally centered x=512. Full silhouette about 740-780px high, lowest feet/tail point at y=900, top about y=140. All appendages fully inside the frame; no detached effects or sparkles, no shadows, no ground, no glow, no lettering or A/B label. Maintain clear silhouette at 40px high. Body lit/mid regions must stay within Delta E76 <= 8 of '+hexcode+'; restrict lighter paint to belly, cheeks, highlights and the single clean lighter contour.\n'
    prompt+=f'\nVariant-specific priorities: {extras[key]}\n'
    prompt+='\nReference roles: image 1 is the mandatory battle mockup and overall style reference. Image 2 is the current cheer sprite; '
    prompt+=('preserve the purple identity and ears, use image 3 high-resolution blue for quality and lighting only.' if ident=='purple' else 'use it ONLY for main color and family rendering style, deliberately replace its old shape with this new design. Image 3 is the approved new-character lineup for family style and avoiding duplicate silhouettes.')
    path=Q / f'{number}-{key}-attempt-01-prompt.txt';path.write_text(prompt+'\n')
    assets.append({'number':int(number),'id':ident,'hex':hexcode,'variant':variant,'key':key,'feature':feature,'mood':mood,'old':str(old),'references':list(map(str,refs)),'prompt':str(path),'output':str(ART/f'delivery/redesign/{key}.png'),'attempts':[]})
assert len(assets)==12
(Q/'config.json').write_text(json.dumps(assets,indent=2)+'\n')
protected=set([mock,lineup,blue_hd])
protected.update((ART/'delivery/creatures').rglob('*.png'))
baseline=[{'path':str(p),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in sorted(protected)]
(Q/'protected-input-hashes.json').write_text(json.dumps(baseline,indent=2)+'\n')
manifest=ART/'delivery/\u4ea4\u4ed8\u6e05\u5355.md';b=manifest.read_bytes()
(Q/'manifest-prefix-sha256.txt').write_text(hashlib.sha256(b).hexdigest()+'\n')
(Q/'manifest-prefix-size.txt').write_text(str(len(b))+'\n')
(Q/'generation-log.json').write_text(json.dumps({'task':'redesign-81-86','maxPerAsset':4,'assets':assets},indent=2)+'\n')
print(json.dumps({'assets':[a['key'] for a in assets],'protectedFiles':len(baseline),'root':str(ROOT)},indent=2))

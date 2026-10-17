"""Authorized logo resize, alpha cleanup input, local color grading and QA composites."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageChops
import json, colorsys, statistics, math
from check_h1_colors import lab

Q = Path(__file__).resolve().parent
H = Q.parent.parent
PATCHES = json.loads((Q/'H10-color-patches.json').read_text())

def rgb_from_lab(vals):
    l,a,b=vals; fy=(l+16)/116; fx=fy+a/500; fz=fy-b/200
    def f(v): return v**3 if v>6/29 else 3*(6/29)**2*(v-4/29)
    x,y,z=.95047*f(fx),f(fy),1.08883*f(fz)
    rgb=(3.2404542*x-1.5371385*y-.4985314*z,-.969266*x+1.8760108*y+.041556*z,.0556434*x-.2040259*y+1.0572252*z)
    def gamma(v): return 12.92*v if v<=.0031308 else 1.055*max(0,v)**(1/2.4)-.055
    return tuple(round(max(0,min(1,gamma(v)))*255) for v in rgb)

def smooth(v,lo,hi):
    t=max(0,min(1,(v-lo)/(hi-lo))); return t*t*(3-2*t)

def measure(im, offset=(0,0)):
    rows=[]
    for name,conf in PATCHES.items():
        target=tuple(int(conf['target'][i:i+2],16) for i in [0,2,4])
        for tone in ['lit','mid']:
            x,y=conf[tone];x+=offset[0];y+=offset[1];box=[x-4,y-4,x+5,y+5]
            pix=list(im.crop(box).getdata());rgb=tuple(round(statistics.median([p[c] for p in pix])) for c in range(3))
            de=math.dist(lab(rgb),lab(target))
            rows.append({'character':name,'tone':tone,'target':'#'+conf['target'],'center':[x,y],'box':box,'rgb':list(rgb),'hex':'#'+''.join(f'{v:02X}' for v in rgb),'deltaE76':round(de,4),'pass':de<=8})
    return {'method':'9x9 unfiltered median RGB patches; sRGB CIELAB D65; DeltaE76 vs specified hex. White specular reflections and navy outlines excluded by patch location.','passed':all(r['pass'] for r in rows),'samples':rows}

src=Image.open(Q/'H10-alpha-clean.png').convert('RGBA')
before=measure(src)
(Q/'H10-before-color-colors.json').write_text(json.dumps(before,ensure_ascii=False,indent=2)+'\n')
samples={(s['character'],s['tone']):s for s in before['samples']}
dst=src.copy();p=src.load();o=dst.load()
areas={
    'blue':((65,96,288,316),(.51,.68),.35),
    'orange':((285,96,505,316),(.035,.13),.35),
    'yellow':((503,96,717,316),(.105,.19),.30),
}
counts={}
for name,(box,hues,smin) in areas.items():
    target=tuple(int(PATCHES[name]['target'][i:i+2],16) for i in [0,2,4]);tl,ta,tb=lab(target)
    ml,ma,mb=lab(samples[name,'mid']['rgb']);ll,la,lb=lab(samples[name,'lit']['rgb']);count=0
    for y in range(box[1],box[3]):
        for x in range(box[0],box[2]):
            old=p[x,y];h,s,v=colorsys.rgb_to_hsv(*(k/255 for k in old[:3]))
            if old[3]<32 or not hues[0]<=h<=hues[1] or s<smin or v<.50:continue
            if name=='blue' and (195<=x<240 and y<125 or x<80 and 231<=y<287):continue
            L,A,B=lab(old[:3])
            # Continuous mapping retains smooth inflated shading even when
            # samples have almost equal lightness; no piecewise tonal bands.
            center_l,center_a,center_b=(ml+ll)/2,(ma+la)/2,(mb+lb)/2
            graded=rgb_from_lab((tl+.8*(L-center_l),ta+.7*(A-center_a),tb+.7*(B-center_b)))
            weight=smooth(s,smin,min(.7,smin+.18))*smooth(v,.50,.70)
            value=tuple(round(a+(b-a)*weight) for a,b in zip(old[:3],graded))+(old[3],)
            if value!=old:o[x,y]=value;count+=1
    counts[name]=count

# Center the complete transparent artwork without distorting glyphs.
box=dst.getchannel('A').getbbox();x=(1000-(box[2]-box[0]))//2;y=(400-(box[3]-box[1]))//2
offset=(x-box[0],y-box[1]);final=Image.new('RGBA',(1000,400));final.paste(dst.crop(box),(x,y))
out=H/'delivery/logo.png';assert not out.exists(),out;final.save(out)
result=measure(final,offset);result['image']=str(out)
(Q/'H10-final-colors.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
(Q/'H10-color-grading.json').write_text(json.dumps({'input':str(Q/'H10-alpha-clean.png'),'output':str(out),'changedPixels':counts,'greenUnchanged':True,'alphaPreservedDuringColorGrading':True,'placementTranslation':offset,'alphaBoundingBox':final.getchannel('A').getbbox(),'method':'Hue-limited body color grading in Lab; dark navy and white reflections excluded. No stroke painting, removal or addition.'},indent=2)+'\n')

# Required homepage preview: image width860 = logical430 at scale2.
mock=Image.open(H/'mock/01-home-mock.png').convert('RGBA')
shown=final.resize((860,344),Image.Resampling.LANCZOS)
mock.alpha_composite(shown,(110,113));mock.convert('RGB').save(Q/'logo-on-mock.png')
small=final.resize((300,120),Image.Resampling.LANCZOS)
smallbg=Image.new('RGBA',(360,170),'#75B9F4');smallbg.alpha_composite(small,(30,25));smallbg.convert('RGB').save(Q/'logo-readability-300.png')
for name,color in [('navy','#11284D'),('white','#FFFFFF'),('sky','#75B9F4')]:
    bg=Image.new('RGBA',final.size,color);bg.alpha_composite(final);bg.convert('RGB').save(Q/f'logo-on-{name}.png')

# Direct overlay and enlarged character panels. Registration is translation
# only: reverse final canvas-centering, then shift reference down8px to match
# the generated baseline. No stroke-wise warping or substitution is performed.
ref=Image.open(H/'reference/logo-glyph-reference.png').convert('RGB')
lines=Image.new('RGBA',ref.size);lp=lines.load();rp=ref.load()
for yy in range(ref.height):
    for xx in range(ref.width):
        r,g,b=rp[xx,yy]
        if max(r,g,b)<180:lp[xx,yy]=(225,0,150,155)
registered=Image.new('RGBA',final.size);registered.paste(src,(0,0))
# The analyzed image is the FINAL logo, undoing only the global canvas offset.
registered=Image.new('RGBA',final.size);registered.paste(final,(-offset[0],-offset[1]))
blend=Image.new('RGBA',final.size,'#EEF1F5');blend.alpha_composite(registered);blend.alpha_composite(lines,(0,8))
sheet=Image.new('RGB',(1200,1140),'#F8FAFC');draw=ImageDraw.Draw(sheet)
draw.text((25,18),'REFERENCE NAVY OUTLINES -> MAGENTA | FINAL LOGO UNDERLAY | TRANSLATION ONLY, NO GLYPH WARP',fill='#233B60')
sheet.paste(blend.convert('RGB'),(100,42))
crops=[(55,85,285,330),(280,85,505,330),(495,85,718,330),(710,85,935,330)]
for i,crop in enumerate(crops):
    x=i*300
    draw.text((x+15,465),f'{i+1}  REFERENCE',fill='#233B60')
    a=ref.crop(crop);a.thumbnail((275,275),Image.Resampling.LANCZOS);sheet.paste(a,(x+12,490))
    draw.text((x+15,780),f'{i+1}  OVERLAY (magenta = reference)',fill='#233B60')
    a=blend.crop(crop).convert('RGB');a.thumbnail((275,275),Image.Resampling.LANCZOS);sheet.paste(a,(x+12,805))
sheet.save(Q/'logo-glyph-check.png')
(Q/'H10-overlay-method.json').write_text(json.dumps({'finalGlobalTranslation':offset,'registration':'Final translated by inverse of canvas centering; reference contour translated8px down. No scaling, rotation, or nonrigid warp.','referenceContour':'RGB maximum channel<180, drawn magenta alpha155','columns':['泡 (blue)','泡 (orange)','小 (yellow)','队 (green)'],'previewCanvasPlacement':[110,113,860,344],'previewVisibleAlphaBounds':[110+round(v*.86) if i%2==0 else 113+round(v*.86) for i,v in enumerate(final.getchannel('A').getbbox())]},ensure_ascii=False,indent=2)+'\n')
print(json.dumps({'colorCheck':result,'centeringOffset':offset,'finalAlphaBox':final.getchannel('A').getbbox()},ensure_ascii=False))

"""Finish generated sprite geometry/alpha for the requested production sizes."""
from pathlib import Path
import json
import numpy as np,cv2
from PIL import Image,ImageDraw
from process_assets import meta,resize_alpha,trim_alpha,clean_sprite_alpha,contacts
R=Path(__file__).resolve().parents[1];P=json.loads((R/'prompts.json').read_text())

def source(a):
    at=next(t for t in a['attempts'] if t['attempt']==a['selected_attempt'])
    return Image.open(at['archive_path']).convert('RGBA')

def save(a,im,operation):
    ar=np.array(im.convert('RGBA'));ar[ar[:,:,3]==0,:3]=0
    Image.fromarray(ar).save(a['output_path'])
    a['postprocess']=[op for op in a['postprocess'] if op.get('stage')!='final_ui']
    operation['stage']='final_ui';a['postprocess'].append(operation)
    a['file_metadata']=meta(Path(a['output_path']))

def capsule(im,size):
    im,_=clean_sprite_alpha(im,4);im,box=trim_alpha(im)
    sw,sh=im.size;tw,th=size;cap=min(sh//2,sw//3);dc=th//2
    out=Image.new('RGBA',size)
    out.paste(resize_alpha(im.crop((0,0,cap,sh)),(dc,th)),(0,0))
    out.paste(resize_alpha(im.crop((cap,0,sw-cap,sh)),(tw-2*dc,th)),(dc,0))
    out.paste(resize_alpha(im.crop((sw-cap,0,sw,sh)),(dc,th)),(tw-dc,0))
    return out,box

for ident in [5,6,7,11]:
    a=P['assets'][ident];im,box=capsule(source(a),tuple(a['requested_size']))
    if ident==5:
        ar=np.array(im);ar[:,:,3]=np.rint(ar[:,:,3]*0.86).astype('uint8');im=Image.fromarray(ar)
    save(a,im,{'method':'preserve round end-cap proportions; stretch only blank central band; source-alpha edge cleanup','crop_box':box,'end_cap_pixels':a['requested_size'][1]//2,'level_opacity_multiplier':0.86 if ident==5 else None})

# Preserve the EXACT unlocked-pad base geometry at x0/y70 in the locked variant.
a=P['assets'][9];orig=source(a)
bubble_box=(255,130,1004,833);bubble=orig.crop(bubble_box)
ar=np.array(bubble);bh,bw=ar.shape[:2];yy,xx=np.indices((bh,bw));rr=((xx-(bw-1)/2)/(bw/2))**2+((yy-(bh-1)/2)/(bh/2))**2
edge=np.clip((1-rr)*min(bw,bh)/3,0,1)
ar[:,:,3]=np.minimum(ar[:,:,3],np.rint(edge*255).astype('uint8'))
# Only the white dashed border / padlock retains full opacity; glass remains translucent.
white=(ar[:,:,:3].min(2)>210)
ar[:,:,3]=np.minimum(ar[:,:,3],np.where(white,255,160).astype('uint8'))
bubble=resize_alpha(Image.fromarray(ar),(84,84))
pad=np.array(Image.open(P['assets'][8]['output_path']).convert('RGBA'))
rgb=pad[:,:,:3].astype(float);gray=rgb.mean(2,keepdims=True)
pad[:,:,:3]=np.rint((rgb*0.48+gray*0.52)*0.81).astype('uint8')
locked=Image.new('RGBA',(140,140));locked.alpha_composite(Image.fromarray(pad),(0,70));locked.alpha_composite(bubble,(28,4))
save(a,locked,{'method':'extract generated lock bubble; composite above exact dimmed/desaturated unlocked-pad geometry','bubble_source_box':bubble_box,'bubble_destination':[28,4,84,84],'base_destination':[0,70,140,70],'base_source':'ui/pad.png'})

# Airy golden effect: retain generated coils and dots, reduce dense color haze.
a=P['assets'][10];im=source(a);im,box=trim_alpha(im);im=resize_alpha(im,(152,152))
ar=np.array(im);rgb=ar[:,:,:3].astype(float)
coil=np.clip((rgb[:,:,2]-65)/185,0,1)**1.4
ar[:,:,3]=np.rint(ar[:,:,3]*(0.10+0.90*coil)).astype('uint8')
yy,xx=np.indices((152,152));radius=np.sqrt((xx-75.5)**2+(yy-75.5)**2)/76
fade=np.clip((1-radius)/0.13,0,1);fade=fade*fade*(3-2*fade)
ar[:,:,3]=np.rint(ar[:,:,3]*fade).astype('uint8');ar[ar[:,:,3]<2,3]=0
swirl=Image.new('RGBA',(160,160));swirl.alpha_composite(Image.fromarray(ar),(4,4))
save(a,swirl,{'method':'retain generated gold spiral and glints; attenuate dense gold haze; circular soft alpha falloff; 4px transparent safety margin','crop_box':box,'shape':'overhead circular','edge_alpha':0})

# Put all rounded top/bottom details inside the specified 32px nine-slice borders.
a=P['assets'][14];im=source(a);im,_=clean_sprite_alpha(im,4);im,box=trim_alpha(im);sw,sh=im.size
top=int(sh*0.15);bottom=int(sh*0.14)
panel=Image.new('RGBA',(192,360))
panel.paste(resize_alpha(im.crop((0,0,sw,top)),(192,32)),(0,0))
panel.paste(resize_alpha(im.crop((0,top,sw,sh-bottom)),(192,296)),(0,32))
panel.paste(resize_alpha(im.crop((0,sh-bottom,sw,sh)),(192,32)),(0,328))
ar=np.array(panel);yy,xx=np.indices((360,192));dist=np.minimum.reduce([xx,191-xx,yy,359-yy]).astype(float)
f=np.clip((dist-10)/22,0,1);f=f*f*(3-2*f)
ar[:,:,3]=np.rint(ar[:,:,3]*(1-0.87*f)).astype('uint8')
save(a,Image.fromarray(ar),{'method':'clean original alpha; fit rounded caps and base ring within 32px borders; retain generated glass highlights and reduce central fill to mostly transparent','crop_box':box,'nine_slice_borders':[32,32,32,32],'central_alpha_multiplier':0.13})

# Make the two horizontal edge-to-edge bands continuous at their endpoints.
for ident in [2,3]:
    a=P['assets'][ident];im=Image.open(a['output_path']).convert('RGBA');ar=np.array(im).astype(float)
    width=ar.shape[1];kmax=12
    prem=ar.copy();prem[:,:,:3]*=prem[:,:,3:4]/255
    src=prem.copy()
    for k in range(kmax):
        weight=0.5*(1-k/kmax)
        prem[:,k]=(1-weight)*src[:,k]+weight*src[:,width-1-k]
        prem[:,width-1-k]=(1-weight)*src[:,width-1-k]+weight*src[:,k]
    denom=np.maximum(prem[:,:,3:4]/255,1/255);prem[:,:,:3]/=denom
    prem=np.clip(np.rint(prem),0,255).astype('uint8')
    save(a,Image.fromarray(prem),{'method':'12px premultiplied-alpha endpoint blend for continuous horizontal repeat; source artwork retained','left_right_endpoint_pixels_equal':True})

(R/'prompts.json').write_text(json.dumps(P,ensure_ascii=False,indent=2)+'\n')
print(contacts(P['assets']))


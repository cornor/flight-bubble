from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter
import numpy as np
root=Path(__file__).resolve().parents[1]
master=Image.open(root/'masters/reach-04.png').convert('RGBA')
# Hand-inspected body/chin silhouette; exclude all generated leaves and pot.
left=[(0,0),(0,132),(48,136),(56,139),(61,140),(62,141),(63,142),(64,143),(65,144),(66,145),(67,146),(68,147),(69,148),(70,149),(72,150),(74,151),(76,152),(78,153),(80,154),(83,155),(85,156),(87,157),(91,158),(94,159),(96,160),(106,161),(114,161.5)]
right=[(124,160),(128,159),(131,158),(133,157),(135,156),(138,155),(140,154),(144,153),(148,152),(152,151),(154,150),(156,149),(158,148),(160,147),(162,146),(163,145),(164,144),(165,143),(167,142),(173,140),(177,136),(224,132),(224,0)]
mask=Image.new('L',master.size)
ImageDraw.Draw(mask).polygon([(round(x*master.width/224),round(y*master.height/256)) for x,y in left+right],fill=255)
a=np.array(master);a[:,:,3]=((a[:,:,3].astype(np.uint16)*np.array(mask).astype(np.uint16)+127)//255).astype(np.uint8)
# Preserve antialiasing around the connected body; remove isolated background speckles.
core=Image.fromarray(np.where(a[:,:,3]>12,255,0).astype(np.uint8)).copy()
ImageDraw.floodfill(core,(master.width//2,int(master.height*.43)),128)
core=Image.fromarray(np.where(np.array(core)==128,255,0).astype(np.uint8)).filter(ImageFilter.MaxFilter(9))
a[np.array(core)==0]=0
body=Image.fromarray(a).convert('RGBa').resize((224,256),Image.Resampling.LANCZOS).convert('RGBA')
a=np.array(body);a[:6]=0;body=Image.fromarray(a)
base=Image.open(root/'references/koala-idle.png').convert('RGBA');base_a=np.array(base);base_a[:122]=0
result=Image.alpha_composite(Image.fromarray(base_a),body)
a=np.array(result);original=np.array(base);body_a=np.array(body)[:,:,3]
locked=np.zeros((256,224),bool);locked[148:]=True;locked &= body_a==0
a[locked]=original[locked]
result=Image.fromarray(a)
out=root/'delivery/creatures/koala/42A9FF/reach.png';out.parent.mkdir(parents=True,exist_ok=True);result.save(out)
body.save(root/'work/reach-body.png');Image.fromarray(body_a).save(root/'work/reach-body-mask.png')
result.resize((672,768),Image.Resampling.NEAREST).save(root/'work/reach-check-3x.png')
print('saved',out)
print('non-occluded lower-region differing pixels',int(np.any(a[locked]!=original[locked],axis=1).sum()))

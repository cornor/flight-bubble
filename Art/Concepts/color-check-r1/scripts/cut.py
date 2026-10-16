import numpy as np
from PIL import Image
from scipy import ndimage
im=Image.open('/mnt/user-data/uploads/Flightbeat/Art/Concepts/plant-characters-v05/lineup-battle-style-v05-fix01.png').convert('RGB')
W,H=im.size; s=W/2000
centers=[150,395,650,880,1130,1385,1630,1860]
ids=['morningglory','carrot','broccoli','eggplant','sunflower','cabbage','strawberry','truffle']
bounds=[0]+[ (centers[i]+centers[i+1])/2 for i in range(7)]+[2000]
a=np.asarray(im).astype(float)/255
mx=a.max(-1); mn=a.min(-1); v=mx; sat=np.where(mx>0,(mx-mn)/np.maximum(mx,1e-6),0)
r,g,b=a[...,0],a[...,1],a[...,2]
hue=np.degrees(np.arctan2(np.sqrt(3)*(g-b),2*r-g-b))%360
grass=(hue>60)&(hue<125)&(sat>0.40)&(v>0.40)
for i,pid in enumerate(ids):
    x0=int(bounds[i]*s)+4; x1=int(bounds[i+1]*s)-4; y0=int(25*s); y1=int(428*s)
    sub=grass[y0:y1,x0:x1]
    lab,n=ndimage.label(sub)
    border=set(np.unique(np.concatenate([lab[0],lab[-1],lab[:,0],lab[:,-1]])))-{0}
    bg=np.isin(lab,list(border))
    bg=ndimage.binary_opening(bg,iterations=1)
    fg=~bg
    # keep largest fg component (plant+pot), fill holes
    lab2,n2=ndimage.label(fg); sizes=ndimage.sum(fg,lab2,range(1,n2+1)); keep=lab2==(1+int(np.argmax(sizes)))
    keep=ndimage.binary_fill_holes(keep)
    # also drop the pot shadow (dark green semi) - fine
    rgba=np.dstack([np.asarray(im)[y0:y1,x0:x1],(keep*255).astype(np.uint8)])
    ys,xs=np.where(keep); crop=rgba[ys.min():ys.max()+1,xs.min():xs.max()+1]
    Image.fromarray(crop,'RGBA').save(f'plant_{pid}.png'); print(pid,crop.shape)

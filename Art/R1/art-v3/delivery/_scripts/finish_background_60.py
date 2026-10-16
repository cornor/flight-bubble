"""Layout-resample the AI painting without replacing its scenery or cloud edges."""
from pathlib import Path
import json,hashlib
import numpy as np,cv2
from PIL import Image
from check_background_60 import inspect,comparison

ROOT=Path(__file__).resolve().parents[1]
P=ROOT/'background-60-prompts.json'
m=json.loads(P.read_text());a=m['attempts'][3]
source=ROOT/'_qa/background-60-attempt-04.png'
src=np.array(Image.open(source).convert('RGB'))
yy,xx=np.mgrid[:1920,:1080].astype('float32')
# Continuous piecewise resampling: open sky expands, the island band moves down,
# and the lower cloud fringe compresses to the very bottom. No pixel painting.
dest_y=[0,184,850,903,960,1133,1190,1750,1863,1919]
source_y=[0,184,800,850,925,1133,1190,1745,1818,1919]
ymap=np.interp(yy,dest_y,source_y).astype('float32')
dest_x=[0,80,108,540,972,1000,1079]
source_x=[0,125,165,540,915,955,1079]
xstage=np.interp(xx,dest_x,source_x).astype('float32')
t=np.clip((yy-1133)/57,0,1);t=t*t*(3-2*t)
xmap=xx*(1-t)+xstage*t
out=cv2.remap(src,xmap,ymap,cv2.INTER_LANCZOS4,borderMode=cv2.BORDER_REPLICATE)
# Gentle color grade on the blue-gray floor, not a flat-color replacement.
# Preserve cloud whites and colored corner ornaments by excluding their colors.
f=out.astype('float32');hsv=cv2.cvtColor(out,cv2.COLOR_RGB2HSV)
floor=(yy>=1133)&(hsv[:,:,0]>=96)&(hsv[:,:,0]<=120)&(hsv[:,:,1]<=115)&(f[:,:,0]<205)&(f[:,:,2]<247)
weight=cv2.GaussianBlur(floor.astype('float32'),(0,0),8)
v=np.clip((yy-1133)/57,0,1);v=v*v*(3-2*v)
weight*=v
f+=weight[:,:,None]*np.array([8.,-1.,-15.],np.float32)
out=np.clip(np.rint(f),0,255).astype('uint8')
path=ROOT/'_qa/background-60-candidate.png';Image.fromarray(out).save(path)
detail={'source':str(source),'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
        'method':'continuous layout resampling of the native AI painting; no scenery removed or drawn, no rectangular replacement panels',
        'vertical_destination_to_source':list(zip(dest_y,source_y)),
        'lower_stage_horizontal_destination_to_source':list(zip(dest_x,source_x)),
        'horizontal_blend_y':[1133,1190],'interpolation':'Lanczos4',
        'floor_color_grade_rgb_offset':[8,-1,-15],
        'floor_grade_mask':'blue-gray low-saturation floor pixels; 8px soft transition, cloud whites and colored ornaments excluded',
        'final_path':str(path),'final_sha256':hashlib.sha256(path.read_bytes()).hexdigest()}
(ROOT/'_qa/background-60-postprocess.json').write_text(json.dumps(detail,ensure_ascii=False,indent=2)+'\n')
a['postprocessing_detail']=detail;a['finished_candidate_path']=str(path)
result=inspect(path,4)
(ROOT/'_qa/background-60-checks.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
a['finished_measurements']=result
P.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
comparison(path)
print(json.dumps(result,ensure_ascii=False))

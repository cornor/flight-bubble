import json,pathlib,numpy as np,cv2
from PIL import Image
r=pathlib.Path('/Users/yangdage/aitools/Flightbeat/Art/R1/art-v3')
im=np.array(Image.open(r/'lineup/01-lineup.png').convert('RGB'))
orig=np.array(Image.open(r/'delivery/_originals/00-01-lineup-attempt-02.png').convert('RGB'))
checks=[('blue','#2982F4',[(220,400),(130,350)]),('orange','#FF7E27',[(570,400),(500,350)]),('green','#62E18E',[(1000,365),(1020,250)]),('purple','#BD80FF',[(1320,390),(1210,320)]),('yellow','#FFDF48',[(220,820),(110,750)]),('white','#FFFFFF',[(590,830),(520,760)]),('red','#FF525B',[(960,800),(1040,750)]),('charcoal','#2D2D2D',[(1335,830),(1240,795)])]
out=[]
def lab(c):return cv2.cvtColor((np.array(c,dtype='float32')/255).reshape(1,1,3),cv2.COLOR_RGB2LAB)[0,0]
offsets=json.loads((r/'delivery/prompts.json').read_text())['assets'][0]['lineup_alignment']['offsets']
for idx,(name,hx,pts) in enumerate(checks):
 t=[int(hx[j:j+2],16) for j in [1,3,5]]; target=lab(t)
 row={'character':name,'target_hex':hx,'target_rgb':t,'threshold_delta_e76':8,'samples':[]}
 for x,y in pts:
  ox,oy=x,y; x+=offsets[idx]['dx'];y+=offsets[idx]['dy']
  rgb=np.rint(np.median(im[y-4:y+5,x-4:x+5].reshape(-1,3),0)).astype('uint8')
  old=np.rint(np.median(orig[oy-4:oy+5,ox-4:ox+5].reshape(-1,3),0)).astype('uint8')
  de=float(np.linalg.norm(lab(rgb)-target))
  row['samples'].append({'roi_xyxy':[x-4,y-4,x+5,y+5],'source_roi_before_alignment':[ox-4,oy-4,ox+5,oy+5],'method':'9x9 RGB median','before_rgb':old.tolist(),'rgb':rgb.tolist(),'hex':'#'+''.join(f'{k:02X}' for k in rgb),'delta_e76':round(de,3),'result':'通过' if de<=8 else '未通过'})
 out.append(row)
(r/'delivery/_qa/lineup-color-samples.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(out,ensure_ascii=False))

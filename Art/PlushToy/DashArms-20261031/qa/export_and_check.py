from pathlib import Path
from PIL import Image, ImageDraw
import json,math,subprocess,hashlib
import numpy as np

B=Path(__file__).resolve().parents[1]
Q=B/'qa';D=B/'delivery';runner=Q/'raster-tools'
body=B/'references/body.png';mouth=B.parent/'DashTransition-20261031/delivery/mouth-open.png'
def run(args):subprocess.run([str(x) for x in args],check=True,capture_output=True)
run(['swiftc',Q/'raster_tools.swift','-o',runner])
specs=[{'id':'H1','part':'arm-l','master':'H1-arm-l-v01.png','anchor':[950,450],'paw_center':[365,865],'pivot':[198,240],'bearing':150,'sign':1},
       {'id':'H2','part':'arm-r','master':'H2-arm-r-v01.png','anchor':[330,455],'paw_center':[950,880],'pivot':[314,240],'bearing':30,'sign':-1}]
reports=[]
for s in specs:
 master=B/'masters'/s['master'];im=Image.open(master).convert('RGBA');p=np.array(im);yy,xx=np.where(p[:,:,3]>=16)
 ax,ay=s['anchor'];rad=float(np.hypot(xx-ax,yy-ay).max());scale=86/rad
 vx,vy=np.subtract(s['paw_center'],s['anchor']);theta=s['bearing']-math.degrees(math.atan2(vy,vx))
 small=Q/(s['part']+'-working.png');run(['sips','--resampleHeightWidth','192','192',master,'--out',small]);ratio=192/im.width
 target=D/(s['part']+'.png');px,py=s['pivot']
 run([runner,'transform',small,target,ax*ratio,ay*ratio,px,py,scale/ratio,theta])
 a=np.array(Image.open(target));yy,xx=np.where(a[:,:,3]>=16);r=np.hypot(xx-px,yy-py)
 ygrid,xgrid=np.indices((512,512));core=(xgrid-px)**2+(ygrid-py)**2<=10**2
 assert int(a[:,:,3][core].min())>=250
 body_alpha=np.array(Image.open(body))[:,:,3]
 assert body_alpha[py,px]>=245 and float((body_alpha[core]>=128).mean())>=.95
 assert not a[0,:,3].any() and not a[-1,:,3].any() and not a[:,0,3].any() and not a[:,-1,3].any()
 sweep=[]
 # Check every nontransparent pixel, including faint fur edges, at each degree.
 fy,fx=np.where(a[:,:,3]>0)
 for angle in range(101):
  t=math.radians(angle*s['sign']);dx=fx-px;dy=fy-py
  rx=px+dx*math.cos(t)-dy*math.sin(t);ry=py+dx*math.sin(t)+dy*math.cos(t)
  bounds=[float(rx.min()),float(ry.min()),float(rx.max()),float(ry.max())]
  assert min(bounds[:2])>2 and max(bounds[2:])<509
  sweep.append({'angle':angle,'bounds':bounds})
 for angle in [0,50,100]:
  run([runner,'transform',target,Q/(s['part']+f'-{angle:03}.png'),px,py,px,py,1,angle*s['sign']])
 report={**s,'size':[512,512],'mode':'RGBA','scale_from_master':scale,'registration_degrees_clockwise':theta,'visible_pivot_to_tip_px':round(float(r.max()),2),'visible_bbox':[int(xx.min()),int(yy.min()),int(xx.max()+1),int(yy.max()+1)],'pivot_core_radius_px':10,'pivot_core_min_alpha':int(a[:,:,3][core].min()),'continuous_rotation_radius_bound_px':round(float(np.hypot(fx-px,fy-py).max()),2),'outer_edge_alpha':0,'clipped_angles':[],'angle_sweep':sweep,'sha256':hashlib.sha256(target.read_bytes()).hexdigest()}
 reports.append(report)
(D/'parts.json').write_text(json.dumps({s['part']:s['pivot'] for s in specs},indent=2)+'\n')
for angle in [0,50,100]:
 l=Q/f'arm-l-{angle:03}.png';r=Q/f'arm-r-{angle:03}.png'
 for name,bg in [('transparent','transparent'),('white','FFFFFF'),('blue','2F86FF')]:
  run([runner,'composite',Q/f'assembled-{angle:03}-{name}.png',bg,body,mouth,l,r])
sheet=Image.new('RGB',(1536,1080),'#eeeeee');draw=ImageDraw.Draw(sheet)
for row,name in enumerate(['white','blue']):
 for col,angle in enumerate([0,50,100]):
  x=512*col;y=540*row;sheet.paste(Image.open(Q/f'assembled-{angle:03}-{name}.png').convert('RGB'),(x,y+28));draw.text((x+16,y+8),f'{angle} deg / {name} / body > mouth > arm-l > arm-r',fill='black')
sheet.save(Q/'rotation-contact-sheet.png')
individual=Image.new('RGB',(1536,1080),'#eeeeee');d=ImageDraw.Draw(individual)
for row,s in enumerate(specs):
 for col,angle in enumerate([0,50,100]):
  x=512*col;y=540*row;cell=Image.new('RGBA',(512,512),'#2F86FF' if row else '#FFFFFF');cell.alpha_composite(Image.open(Q/(s['part']+f'-{angle:03}.png')));individual.paste(cell.convert('RGB'),(x,y+28));d.text((x+16,y+8),s['part']+f' / {angle} deg / pivot {s["pivot"]}',fill='black')
individual.save(Q/'individual-rotations.png')
frames=[Image.open(Q/f'assembled-{angle:03}-blue.png').convert('RGB') for angle in [0,50,100,50]]
frames[0].save(Q/'rotation-preview.gif',save_all=True,append_images=frames[1:],duration=[500,200,500,200],loop=0)
(Q/'verification.json').write_text(json.dumps({'parts':reports,'composite_order':['body','mouth-open','arm-l','arm-r'],'backgrounds':['#FFFFFF','#2F86FF'],'check_angles':[0,50,100],'sweep_degrees':[0,100],'sweep_step':1,'continuous_bound':'All nontransparent pixels lie within the recorded pivot radius; each full radius circle stays inside the 512 canvas, including angles between sample degrees.'},indent=2)+'\n')
print(json.dumps([{k:v for k,v in s.items() if k in ['part','pivot','visible_pivot_to_tip_px','visible_bbox','continuous_rotation_radius_bound_px','pivot_core_min_alpha']} for s in reports],indent=2))

"""Compose current/A/B, near-color groups and dark-background proofs."""
from pathlib import Path
import json
import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageOps
from finish_redesign import resize_rgba

Q=Path(__file__).resolve().parent
D=Q.parent.parent
ART=D.parent
FONT='/System/Library/Fonts/STHeiti Light.ttc'
def font(size):return ImageFont.truetype(FONT,size)
names={'charcoal':'小黑','slate-blue':'小靛','mint':'小碧','forest':'小翠','berry':'小玫','purple':'小紫','brown':'小棕','violet':'小晶','lavender':'小雾','cyan':'小冰','blue':'小蓝','green':'小绿','lime':'小荧','pale-mint':'小清','red':'小红','pink':'小粉','peach':'小霞','orange':'小橙','white':'小白','yellow':'小黄'}
colors={'charcoal':'2D2D2D','slate-blue':'696CAF','mint':'30D183','forest':'0C7E1C','berry':'9C225E','purple':'BD80FF','brown':'65392D','violet':'B672FD','lavender':'C8C5FF','cyan':'46D9FF','blue':'2982F4','green':'62E18E','lime':'92EF4D','pale-mint':'C5FFE4','red':'FF525B','pink':'FFC5E0','peach':'FFCAA9','orange':'FF7E27','white':'FFFFFF','yellow':'FFDF48'}
ids=['charcoal','slate-blue','mint','forest','berry','purple']

def clean_crop(path):
    im=Image.open(path).convert('RGBA');a=np.array(im)
    n,l,s,_=cv2.connectedComponentsWithStats((a[:,:,3]>24).astype(np.uint8),8)
    k=1+int(np.argmax(s[1:,4]));m=cv2.dilate((l==k).astype(np.uint8),np.ones((3,3),np.uint8))
    a[m==0]=0;a[a[:,:,3]<3]=0
    im=Image.fromarray(a);return im.crop(im.getchannel('A').getbbox())

def sprite(ident,variant=None,height=250):
    p=D/'redesign'/f'{ident}-{variant}.png' if variant else D/f'creatures/{ident}/{colors[ident]}/cheer.png'
    im=clean_crop(p);return resize_rgba(im,(round(im.width*height/im.height),height))

# Required lineup: exactly current cheer | A | B, same visible height per row.
canvas=Image.new('RGBA',(1500,2160),'#EEF1F5');dr=ImageDraw.Draw(canvas)
dr.text((48,28),'六位伙伴 · 重画方案',font=font(34),fill='#172E4E')
dr.text((48,76),'仅供选方案 · 角色主色不变 · 本轮不替换现有角色文件',font=font(18),fill='#596B82')
centers=[420,830,1240]
for x,label in zip(centers,['现在的 cheer','方案 A','方案 B']):dr.text((x,123),label,font=font(24),fill='#25476D',anchor='mm')
for row,ident in enumerate(ids):
    top=160+row*328
    dr.rounded_rectangle((24,top,1476,top+314),radius=18,fill='#F9FBFD' if row%2==0 else '#E7EDF4')
    dr.text((48,top+104),names[ident],font=font(30),fill='#1A3558')
    dr.text((48,top+148),ident,font=font(15),fill='#536B85')
    dr.text((48,top+175),'#'+colors[ident],font=font(16),fill='#536B85')
    for x,v in zip(centers,[None,'A','B']):
        im=sprite(ident,v,260);canvas.alpha_composite(im,(x-im.width//2,top+24))
canvas.convert('RGB').save(ART/'lineup/03-redesign.png')

# Dark checks: each of the four dark characters in both variants.
dark_ids=['charcoal','slate-blue','forest','berry']
dark=Image.new('RGBA',(1600,880),'#0B1A3A');dd=ImageDraw.Draw(dark)
dd.text((32,20),'深色角色 · 深蓝背景检查  #0B1A3A',font=font(26),fill='#EAF2FF')
for row,v in enumerate(['A','B']):
    for col,ident in enumerate(dark_ids):
        im=sprite(ident,v,310);cx=200+col*400;y=80+row*400
        dark.alpha_composite(im,(cx-im.width//2,y))
        dd.text((cx,y+338),names[ident]+' '+v,font=font(21),fill='#D7E7FF',anchor='mm')
dark.convert('RGB').save(Q/'dark-background-check.png')

# All seven comparison groups from brief 5.1, with the proposed variants.
# Purple is absent from that table: add it explicitly to the purple/slate row.
groups=[
 ('天蓝 / 小蓝',[('cyan',None),('blue',None)]),
 ('紫色 / 灰蓝',[('violet',None),('lavender',None),('slate-blue',None),('slate-blue','A'),('slate-blue','B'),('purple',None),('purple','A'),('purple','B')]),
 ('绿色同场组',[('lime',None),('mint',None),('mint','A'),('mint','B'),('forest',None),('forest','A'),('forest','B'),('pale-mint',None),('green',None)]),
 ('玫红 / 红 / 粉',[('berry',None),('berry','A'),('berry','B'),('red',None),('pink',None)]),
 ('浅橙 / 小橙',[('peach',None),('orange',None)]),
 ('棕色 / 小黑',[('brown',None),('charcoal',None),('charcoal','A'),('charcoal','B')]),
 ('浅绿 / 小白',[('pale-mint',None),('white',None)])]
proof=Image.new('RGBA',(1440,1210),'#EEF1F5');pd=ImageDraw.Draw(proof)
pd.text((28,22),'近色同场 · 40 像素轮廓对照',font=font(28),fill='#173455')
pd.text((28,62),'每个可见角色高 40 px；现稿均用 cheer。灰度版保留明暗，不重新绘制。',font=font(16),fill='#526A82')
pd.text((28,91),'补充：小紫加入紫色组，小绿加入绿色组，检查相近造型。',font=font(16),fill='#526A82')
pd.text((500,135),'彩色',font=font(22),fill='#173455',anchor='mm')
pd.text((1120,135),'灰度',font=font(22),fill='#173455',anchor='mm')
records=[]
for row,(label,members) in enumerate(groups):
    top=165+row*145
    pd.rounded_rectangle((16,top,1424,top+132),radius=12,fill='#FFFFFF' if row%2==0 else '#E4EBF3')
    pd.text((28,top+44),label,font=font(20),fill='#203C5B')
    for col,(ident,v) in enumerate(members):
        im=sprite(ident,v,40);tag=names[ident]+(' '+v if v else ' 现')
        for gray,start in [(False,205),(True,825)]:
            cx=start+col*65+30
            shown=im.copy()
            if gray:
                gray_im=ImageOps.grayscale(im.convert('RGB'));shown=Image.merge('RGBA',(gray_im,gray_im,gray_im,im.getchannel('A')))
            proof.alpha_composite(shown,(cx-im.width//2,top+24))
            pd.text((cx,top+87),tag,font=font(12),fill='#314D68',anchor='mm')
        records.append({'group':label,'id':ident,'variant':v,'visibleHeight':40,'width':im.width,'colorAndGrayscale':True})
proof.convert('RGB').save(D/'_qa/redesign-40px.png')
(Q/'proof-layout.json').write_text(json.dumps({'lineup':{'file':'lineup/03-redesign.png','size':[1500,2160],'order':['current cheer','A','B'],'visibleCharacterHeight':260},'darkBackground':{'file':'delivery/_qa/redesign/dark-background-check.png','color':'#0B1A3A','size':[1600,880]},'fortyPixel':{'file':'delivery/_qa/redesign-40px.png','size':[1440,1210],'records':records}},ensure_ascii=False,indent=2)+'\n')
print('Created current/A/B lineup, 40px color/grayscale group proof, and dark-background proof.')

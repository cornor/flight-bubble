# 重跑：python3 cut.py && python3 make_sheets.py（需先把关卡 JSON 和阵容图路径改成本机路径）
from compose import *
BG='#A39581'
names={'training01':'教学 1','training02':'教学 2','sector01':'第 3 关','sector02':'第 4 关'}
shots=[]
for i,(n,t) in enumerate(names.items()):
    im=screen(n,BG,title=t+'（真实尺寸 2x）'); im.save(f'out/screen-{i+1}-{n}.png'); shots.append(im)
W=shots[0].width; H=shots[0].height; g=40
c=Image.new('RGB',(4*W+5*g,H+2*g+120),'#F3F1E8'); dr=ImageDraw.Draw(c)
dr.text((g,30),'R1 前 4 关真实尺寸校验（540×960 逻辑分辨率，按 2 倍渲染；虫区背景：暖灰土 #A39581；植物与虫子为暂定画法）',font=ImageFont.truetype(FONT,40),fill='#2B2620')
for i,s in enumerate(shots): c.paste(s,(g+i*(W+g),120+g))
c.resize((c.width//2,c.height//2),Image.LANCZOS).save('out/01-前4关总览.png')
bgs=[('A 鲜绿草地','#8AC761'),('B 灰绿','#A9B39C'),('C 暖灰土（推荐）','#A39581'),('D 深橄榄','#5E6A55'),('E 中灰绿','#7E8A72'),('F 暖褐土','#7A6A58')]
allc=['#2982F4','#FF7E27','#FFDF48','#FFFFFF','#FF525B','#2D2D2D','#42A9FF','#62E18E','#FF9D30','#BD80FF']
tiles=[]
for lbl,b in bgs:
    s4=screen('sector02',b).crop((100*S,215*S,445*S,535*S)); t1=screen('training01',b).crop((140*S,290*S,410*S,515*S))
    out=[]
    for col in allc:
        dn=de2000(rgb2lab(hex2rgb(col)),rgb2lab(hex2rgb(b)))
        dw=min(de2000(rgb2lab(simulate(hex2rgb(col),k)),rgb2lab(simulate(hex2rgb(b),k))) for k in ['protan','deutan','tritan'])
        out.append((dn,dw,col))
    mn=min(out); mw=min(out,key=lambda x:x[1])
    tile=Image.new('RGB',(s4.width+t1.width+60,max(s4.height,t1.height)+150),'#F3F1E8'); d2=ImageDraw.Draw(tile)
    d2.text((20,10),f'{lbl} {b}',font=ImageFont.truetype(FONT,44),fill='#2B2620')
    d2.text((20,70),f'与虫色最小色差 {mn[0]:.0f}（{mn[2]}）；色弱下最小 {mw[1]:.0f}（{mw[2]}）',font=ImageFont.truetype(FONT,30),fill='#5A5048')
    tile.paste(s4,(20,140)); tile.paste(t1,(s4.width+40,140)); tiles.append(tile)
tw,th=tiles[0].width,tiles[0].height
c=Image.new('RGB',(2*tw+60,3*th+40),'#F3F1E8')
for i,t in enumerate(tiles): c.paste(t,(20+(i%2)*(tw+20),20+(i//2)*th))
c.resize((c.width//2,c.height//2),Image.LANCZOS).save('out/02-虫区背景候选.png')
kinds=[('正常','normal'),('红色弱','protan'),('绿色弱','deutan'),('蓝黄色弱','tritan'),('灰度','gray')]
rows=[]
for n,crop in [('sector02',(100*S,215*S,445*S,875*S)),('training02',(140*S,280*S,410*S,875*S))]:
    base=screen(n,BG).crop(crop); rows.append((names[n],[(lbl,sim_image(base,k)) for lbl,k in kinds]))
cw=max(r[1][0][1].width for r in rows); ch=[r[1][0][1].height for r in rows]
c=Image.new('RGB',(5*(cw+20)+20,sum(ch)+2*130+40),'#F3F1E8'); dr=ImageDraw.Draw(c); y=20
for (nm,row),h in zip(rows,ch):
    for j,(lbl,im) in enumerate(row):
        x=20+j*(cw+20); dr.text((x,y),f'{nm} · {lbl}',font=ImageFont.truetype(FONT,40),fill='#2B2620'); c.paste(im,(x,y+70))
    y+=h+130
c.resize((c.width//2,c.height//2),Image.LANCZOS).save('out/03-色弱模拟.png')

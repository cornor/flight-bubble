"""Small-scale and background-contrast review; does not change deliverables."""
from pathlib import Path
import json
from PIL import Image,ImageDraw
from process_creatures import resize
ROOT=Path(__file__).resolve().parents[1]
m=json.loads((ROOT/'emotes-58-59-prompts.json').read_text())
names=[(n,h)for n,h in m['order']if (ROOT/f'creatures/{n}/{h}/blink.png').exists()]
canvas=Image.new('RGB',(1020,110*len(names)+35),(235,240,246));d=ImageDraw.Draw(canvas)
d.text((10,10),'Idle / blink / cheer at 40px idle height; same display scale for each character',fill=(20,35,60))
for row,(name,hx)in enumerate(names):
    y=35+row*110;folder=ROOT/f'creatures/{name}/{hx}';idle=Image.open(folder/'idle.png');box=idle.getchannel('A').getbbox();scale=40/(box[3]-box[1])
    d.text((10,y+37),f'{name} #{hx}',fill=(20,35,60))
    for k,bg in enumerate([(240,243,248),(166,187,215),(38,46,64)]):
        x=165+k*280;d.rectangle((x,y+2,x+270,y+104),fill=bg)
        for n,pose in enumerate(['idle','blink','cheer']):
            file=folder/f'{pose}.png'
            if not file.exists():continue
            im=Image.open(file);s=resize(im,(round(224*scale),round(256*scale)))
            canvas.paste(s,(x+8+n*86,y+84-round(212*scale)),s)
canvas.save(ROOT/'_qa/emotes-40px-alpha-proof.png')
print(str(ROOT/'_qa/emotes-40px-alpha-proof.png'))

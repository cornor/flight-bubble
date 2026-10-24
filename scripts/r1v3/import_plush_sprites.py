#!/usr/bin/env python3
"""Import ImageGen masters/sheets into the existing 224x256 sprite contract.

Only sprite-sheet cell extraction, uniform scaling and baseline registration are
performed. Artwork, colors and alpha are not repainted or background-removed.
"""
import argparse, json
from pathlib import Path
from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
P1=ROOT/'Art/PlushToy/P1'
COLORS={'orange':'FF7E27','pink':'FFC5E0','green':'62E18E','blue':'2982F4','white':'FFFFFF','yellow':'FFDF48'}

def register(im):
    # Low-alpha antialiased fibers remain in the image; the opaque core defines registration.
    box=im.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
    if not box: raise ValueError('Empty sprite')
    l,t,r,b=box
    scale=min(194/(r-l),196/(b-t))
    full=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS)
    x=round(112-(l+r)*.5*scale);y=round(212-b*scale)
    canvas=Image.new('RGBA',(224,256));canvas.alpha_composite(full,(x,y))
    return canvas,{'coreBounds':box,'scale':scale,'offset':[x,y]}

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--batch',type=Path,help='Batch directory containing production.json and masters/<id>/sheet.png')
    args=parser.parse_args()
    report={}
    if args.batch:
        batch=args.batch.resolve()
        production=json.loads((batch/'production.json').read_text())
        for job in production['jobs']:
            im=Image.open(batch/'masters'/job['id']/'sheet.png').convert('RGBA')
            cols,rows=job['grid'];w,h=im.size
            dest=batch/'delivery/creatures'/job['creatureId']/job['hex'];dest.mkdir(parents=True,exist_ok=True)
            report[job['id']]={}
            for n,state in enumerate(job['states']):
                col,row=n%cols,n//cols
                cuts=job.get('rowCuts',[round(n*h/rows) for n in range(rows+1)])
                xs=job.get('columnCuts',[round(n*w/cols) for n in range(cols+1)])
                cell=im.crop((xs[col],cuts[row],xs[col+1],cuts[row+1]))
                core=cell.getchannel('A').point(lambda a:255 if a>64 else 0).getbbox()
                if not core or min(core[0],core[1],cell.width-core[2],cell.height-core[3])<1:
                    raise ValueError(f'Clipped or empty source cell: {job["id"]}/{state}: {core}')
                canvas,meta=register(cell);canvas.save(dest/(state+'.png'))
                report[job['id']][state]=meta
        for portrait in production.get('portraits',[]):
            source=batch/'delivery'/portrait['source']
            dest=batch/'delivery'/portrait['destination'];dest.parent.mkdir(parents=True,exist_ok=True)
            # Only remove the empty badge strip, keeping the complete fluffy toy at square aspect.
            Image.open(source).crop((0,0,224,224)).save(dest)
        (batch/'sprite-import.json').write_text(json.dumps(report,indent=2)+'\n')
        print({k:list(v) for k,v in report.items()})
        return
    for cid,hx in COLORS.items():
        master=P1/'masters'/cid;idle=master/'idle.png';sheet=master/'states.png'
        if not idle.exists():continue
        frames={'idle':Image.open(idle).convert('RGBA')}
        if sheet.exists():
            im=Image.open(sheet).convert('RGBA');w,h=im.size
            for n,state in enumerate(('attack','spent','ready','blink')):
                col,row=n%2,n//2
                frames[state]=im.crop((round(col*w/2),round(row*h/2),round((col+1)*w/2),round((row+1)*h/2)))
        dest=P1/'delivery/creatures'/cid/hx;dest.mkdir(parents=True,exist_ok=True)
        report[cid]={}
        for state,im in frames.items():
            canvas,meta=register(im);canvas.save(dest/(state+'.png'));report[cid][state]=meta
    (P1/'sprite-import.json').write_text(json.dumps(report,indent=2)+'\n')
    print({k:list(v) for k,v in report.items()})

if __name__=='__main__':main()

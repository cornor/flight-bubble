#!/usr/bin/env python3
"""Victory native UI: shared result ribbon, warm award cards and pink replay."""
import math
from pathlib import Path
import numpy as np
from build_plush_controls import Mold, button_mold
from build_plush_result_ribbons import ribbon

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Art/PlushToy/P2-P4/delivery/win'

def award(w,h,gold=False):
    m=Mold(w,h);r=min(22,h*.28)
    outer=m.rect((3,3,w-3,h-7),r)
    m.shadow(outer,.19,1.4,(0,2))
    m.surface(outer,(255,225,143),(215,156,62),2,.65,shine=.035)
    m.surface(m.rect((6,6,w-6,h-10),r-3),
              (255,243,188) if gold else (255,254,244),
              (255,218,116) if gold else (255,242,215),2,.6,shine=.025)
    return m.finish()

def replay():
    m=button_mold(152,152,(255,149,236),(234,95,207),34)
    # A continuous, constant-width return arrow. Its head meets the top of the
    # circle tangentially, instead of floating beside a diagonal arc endpoint.
    dx,dy=m.x-78,m.y-58
    arc=np.minimum(5.5-np.abs(np.hypot(dx,dy)-27),np.maximum(dx,dy))
    arc=np.maximum(arc,5.5-np.hypot(dx+27,dy))
    vertices=[(52,31),(79,17),(79,45)]
    edges=[]
    for (ax,ay),(bx,by) in zip(vertices,vertices[1:]+vertices[:1]):
        ex,ey=bx-ax,by-ay
        edges.append((ex*(m.y-ay)-ey*(m.x-ax))/math.hypot(ex,ey))
    symbol=np.maximum(arc,np.minimum.reduce(edges))
    m.shadow(symbol,.28,.9,(0,1.5))
    m.surface(symbol,(255,255,253),(249,238,247),2,1.2,shine=.05)
    return m.finish()

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    ribbon(victory=True).save(OUT/'ribbon.png')
    replay().save(OUT/'button-replay.png')
    for name,w,h,gold in [('badge-mvp',240,80,True),('badge-partner',320,80,False),('stat-card',384,128,False)]:
        award(w,h,gold).save(OUT/(name+'.png'))
    print('Victory assets: soft red result ribbon, pink replay and three warm award panels')

if __name__=='__main__': main()

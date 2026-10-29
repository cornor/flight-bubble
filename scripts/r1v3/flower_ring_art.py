"""Flower bubble ring on the pad members' heads (2026-10-28 user: use concept B 花朵泡泡环 as the battle look).

One shared sprite (Art/PlushToy/HeadBubbleRing-20261027/delivery/flower-ring.png, cut from concept B by
cut_flower.py) is registered in the creature atlas as plants/fx/flower-ring. For every plush idle tile the crown
(top of the head at the head's centre column) is measured here, so the runtime can plant the flower there:
Resources/GardenR1/flower-ring.json.

Sprite anchors (sprite pixels): ring centre and radius; `stemCut` is the row that sat on the koala's crown in the
concept (rows below it are a tuck that hides behind the head). Size: in the concept the flower is 181 px wide on a
koala whose head with ears is 463 px wide; the same ratio is kept against the koala's head in the game, and the
same flower size is used for every member (one shared accessory).
"""
import hashlib
import json
import re
from pathlib import Path
import numpy as np
from PIL import Image

ART = Path('Art/PlushToy/HeadBubbleRing-20261027/delivery/flower-ring.png')
RING = dict(cx=87.4, cy=84.6, r=45.6, stemCut=214)
CONCEPT_FLOWER_W, CONCEPT_HEAD_W = 181, 463
KOALA = '42A9FF'
NECK = 150 / 256


def head_metrics(im):
    """Crown (x, y) and head width in 224x256 canvas units for one idle frame."""
    fw, fh = im.size
    a = np.asarray(im.getchannel('A')) >= 128
    neck = int(round(fh * NECK))
    head = a[:neck]
    ys, xs = np.nonzero(head[int(fh * .2):])
    if len(xs) == 0:
        return None
    cx = float(np.median(xs))                       # head mass centre (ignores a trunk or one ear)
    band = max(2, int(fw * .03))
    cols = head[:, max(0, int(cx) - band):int(cx) + band + 1]
    rows = np.flatnonzero(cols.any(1))
    crown = float(rows.min()) if len(rows) else 0.0
    widths = [int(np.ptp(np.flatnonzero(r))) + 1 for r in head if r.sum() > 3]
    return dict(x=round(cx / fw * 224, 2), y=round(crown / fh * 256, 2), headW=round(max(widths) / fw * 224, 2))


def load(root, out, tiles):
    path = root / ART
    if not path.exists():
        return []
    crowns, koala = [], None
    for sid, im, _ in tiles:
        m = re.fullmatch(r'plants/[^/]+/([0-9A-Fa-f]{6})/idle', sid)
        if not m:
            continue
        hm = head_metrics(im)
        if hm is None:
            continue
        if m.group(1).upper() == '8C919C':
            continue                                # hidden (mystery) members show no flower
        crowns.append(dict(hex=m.group(1).upper(), **{k: hm[k] for k in ('x', 'y')}))
        if m.group(1).upper() == KOALA:
            koala = hm
    flower = Image.open(path).convert('RGBA')
    alpha = np.asarray(flower.getchannel('A'))
    stem = np.flatnonzero(alpha[RING['stemCut'] - 2] > 100)          # the stem where it met the koala's crown
    stem_x = round(float(stem.min() + stem.max() + 1) / 2, 1) if len(stem) else RING['cx']
    head_w = koala['headW'] if koala else 200.0
    scale = round(head_w * CONCEPT_FLOWER_W / CONCEPT_HEAD_W / flower.width, 4)   # canvas units per sprite px
    info = dict(sprite='plants/fx/flower-ring', w=flower.width, h=flower.height, scale=scale, stemX=stem_x, **RING, crowns=crowns)
    out.mkdir(parents=True, exist_ok=True)
    (out / 'flower-ring.json').write_text(json.dumps(info, ensure_ascii=False, indent=1) + '\n')
    return [('plants/fx/flower-ring', flower, hashlib.sha256(path.read_bytes()).hexdigest())]

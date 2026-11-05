"""Teaching levels 1-2 (training01/02) -> B draft. 2026-11-03 (user: give levels 1-2 new pictures in our theme).

  python3 scripts/levels_b/tutorial_convert.py --level 1 --image 第1关-戴蝴蝶结的小水母-01.png --title 戴蝴蝶结的小水母 [--out DIR]

These boards are small silhouettes (24 x 20 / 24 x 21, `shapedFormation`, empty cells = -1) with no stream, so the
60 x 60 converter does not apply. Steps:
1. Map every pixel to the nearest level colour or to white (= empty), crop to the subject.
2. Fit the subject into the grid keeping its aspect, bottom-aligned (the bottom row is the front) and centred, by
   majority vote per cell; the scale is chosen so the number of filled cells is as close as possible to A's.
3. The picture is kept as drawn (2026-11-04 user decision: the bubble count per colour may differ from A, the
   picture matters most): each queue member keeps its lane, colour and order, and its colour's ammo is re-split over
   that colour's members in proportion to A's ammo, so ammo = bubbles again.
4. Checks for the lesson: the bottom row must contain colour 0 (the first members are colour 0 and the first lesson
   step is "tap the blue member"); report the first row (from the bottom) of every colour against A.
Writes <out>/<resource>.json, a preview <resource>.png and report.json.
"""
import argparse, json, sys
from collections import Counter
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
from levelsb_common import a_level_path
ROOT = Path(__file__).resolve().parents[2]
LEVELS = ROOT / 'UnityProject/Assets/Flightbeat/Resources/levels'
RES = {1: 'training01', 2: 'training02'}

def palette(level):
    p = level['palette']; return [tuple(p[i:i + 3]) for i in range(0, len(p), 3)]

def classify(img, cols):
    a = np.asarray(img.convert('RGB')).astype(int)
    ref = np.array([(255, 255, 255)] + [cols[c] for c in range(len(cols))])
    d = ((a[:, :, None, :] - ref[None, None]) ** 2).sum(-1)
    return d.argmin(-1) - 1                      # -1 = white/empty, else colour index

def fit(lab, W, H, scale):
    ys, xs = np.nonzero(lab >= 0)
    sub = lab[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    h, w = sub.shape
    cell = max(w / (W * scale), h / (H * scale))            # source pixels per grid cell
    gw, gh = min(W, int(round(w / cell))), min(H, int(round(h / cell)))
    grid = -np.ones((H, W), int)
    x0 = (W - gw) // 2; y0 = H - gh
    for gy in range(gh):
        for gx in range(gw):
            blk = sub[int(gy * cell):max(int(gy * cell) + 1, int((gy + 1) * cell)), int(gx * cell):max(int(gx * cell) + 1, int((gx + 1) * cell))]
            vals, cnt = np.unique(blk, return_counts=True)
            filled = cnt[vals >= 0].sum()
            if filled * 2 >= blk.size:            # mostly subject -> its most common colour
                fv, fc = vals[vals >= 0], cnt[vals >= 0]
                grid[y0 + gy, x0 + gx] = int(fv[fc.argmax()])
    return grid

def nb(g, y, x):
    H, W = g.shape
    return [(y + dy, x + dx) for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)) if 0 <= y + dy < H and 0 <= x + dx < W]

def resplit_ammo(queue, counts):
    """2026-11-04: every colour's ammo total becomes its new bubble count, split over that colour's members
    in proportion to A's ammo (largest remainder, at least 1 each); lane, colour and order stay A's."""
    q = [dict(e) for e in queue]
    for c in {e['color'] for e in q}:
        idx = [i for i, e in enumerate(q) if e['color'] == c]
        old = [q[i]['ammo'] for i in idx]; n = counts.get(c, 0)
        if n < len(idx):
            raise ValueError(f'颜色 {c} 只有 {n} 颗，少于它的 {len(idx)} 个队员')
        raw = [n * o / sum(old) for o in old]
        new = [max(1, int(r)) for r in raw]
        for k in sorted(range(len(idx)), key=lambda k: raw[k] - int(raw[k]), reverse=True):
            if sum(new) >= n: break
            new[k] += 1
        while sum(new) > n:
            k = max(range(len(new)), key=lambda k: new[k]); new[k] -= 1
        for i, a in zip(idx, new): q[i]['ammo'] = a
    return q


def first_rows(cells, W, H):
    f = {}
    for r in range(H):
        for c in range(W):
            v = cells[r * W + c]
            if v >= 0: f[v] = min(f.get(v, 99), H - r)
    return f

def main():
    ap = argparse.ArgumentParser(); ap.add_argument('--level', type=int, required=True, choices=(1, 2))
    ap.add_argument('--image', required=True); ap.add_argument('--title', required=True); ap.add_argument('--out')
    a = ap.parse_args()
    res = RES[a.level]; A = json.loads(a_level_path(res).read_text())   # 11-05: originals live in Art/levels-a/
    W, H = A['columns'], A['rows']; cols = palette(A)
    target = dict(Counter(c for c in A['cells'] if c >= 0))
    lab = classify(Image.open(a.image), cols)
    used = set(np.unique(lab).tolist()) - {-1}
    if used - set(target):
        print('图里有本关没有的颜色：', sorted(used - set(target))); return 1
    best = None
    for s in np.arange(.70, 1.001, .01):
        gr = fit(lab, W, H, s); n = int((gr >= 0).sum())
        if best is None or abs(n - sum(target.values())) < best[0]: best = (abs(n - sum(target.values())), s, gr)
    _, scale, grid = best
    before = Counter(int(v) for v in grid.flatten() if v >= 0)
    g = grid.copy()
    changed = 0
    B = json.loads(json.dumps(A)); B['cells'] = [int(v) for v in g.flatten()]; B['title'] = a.title
    newc = dict(Counter(int(v) for v in g.flatten() if v >= 0))
    if set(newc) != set(target):
        print('图里的颜色和本关不一致：', sorted(newc), '应为', sorted(target)); return 1
    try:
        B['queue'] = resplit_ammo(A['queue'], newc)
    except ValueError as e:
        print(e); return 1
    B['shades'] = [0] * (W * H)   # sourceImage stays A's, like the other B drafts (the new picture is in the manifest)
    out = Path(a.out) if a.out else ROOT / f'Art/levels-b/work/第{a.level:03d}关_{res}'
    out.mkdir(parents=True, exist_ok=True)
    (out / f'{res}.json').write_text(json.dumps(B, ensure_ascii=False, separators=(',', ':')))
    fa, fb = first_rows(A['cells'], W, H), first_rows(B['cells'], W, H)
    rep = dict(level=a.level, resource=res, title=a.title, image=str(a.image), scale=round(float(scale), 2),
               quantized={int(k): v for k, v in before.items()}, target=target, changedCells=changed,
               counts={int(k): v for k, v in newc.items()},
               ammo=[e['ammo'] for e in B['queue']], ammoA=[e['ammo'] for e in A['queue']],
               bottomRowHasColour0=0 in [B['cells'][(H - 1) * W + c] for c in range(W)],
               firstRowA={int(k): v for k, v in fa.items()}, firstRowB={int(k): v for k, v in fb.items()})
    (out / 'report.json').write_text(json.dumps(rep, ensure_ascii=False, indent=1))
    # preview: A | B
    k = 16; im = Image.new('RGB', (W * k * 2 + 30, H * k + 20), (30, 30, 40)); d = ImageDraw.Draw(im)
    for i, cells in enumerate((A['cells'], B['cells'])):
        for r in range(H):
            for c in range(W):
                v = cells[r * W + c]; x0 = 10 + i * (W * k + 10) + c * k; y0 = 10 + r * k
                d.rectangle([x0, y0, x0 + k - 2, y0 + k - 2], fill=cols[v] if v >= 0 else (45, 45, 58))
                if i == 1 and g[r, c] != grid[r, c]: d.rectangle([x0, y0, x0 + k - 2, y0 + k - 2], outline=(0, 0, 0), width=2)
    im.save(out / f'{res}.png')
    print(json.dumps(rep, ensure_ascii=False))
    return 0

if __name__ == '__main__':
    sys.exit(main())

"""Teaching levels 1-2 (training01/02) -> B draft. 2026-11-03 (user: give levels 1-2 new pictures in our theme).

  python3 scripts/levels_b/tutorial_convert.py --level 1 --image 第1关-戴蝴蝶结的小水母-01.png --title 戴蝴蝶结的小水母 [--out DIR]

These boards are small silhouettes (24 x 20 / 24 x 21, `shapedFormation`, empty cells = -1) with no stream, so the
60 x 60 converter does not apply. Steps:
1. Map every pixel to the nearest level colour or to white (= empty), crop to the subject.
2. Fit the subject into the grid keeping its aspect, bottom-aligned (the bottom row is the front) and centred, by
   majority vote per cell; the scale is chosen so the number of filled cells is as close as possible to A's.
3. Exact counts: grow/shrink along the silhouette until the filled total equals A's, then recolour along colour
   borders until every colour has A's count (the queue, ammo and lesson stay exactly A's).
4. Checks for the lesson: the bottom row must contain colour 0 (the first members are colour 0 and the first lesson
   step is "tap the blue member"); report the first row (from the bottom) of every colour against A.
Writes <out>/<resource>.json, a preview <resource>.png and report.json.
"""
import argparse, json, sys
from collections import Counter
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
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

def exact(grid, target):
    g = grid.copy(); H, W = g.shape; total = sum(target.values())
    # 1. total filled cells: add next to the silhouette (colour of the most common neighbour) / remove boundary cells
    for _ in range(2000):
        n = int((g >= 0).sum())
        if n == total: break
        if n < total:
            cand = [(y, x) for y in range(H) for x in range(W) if g[y, x] < 0 and sum(g[q] >= 0 for q in nb(g, y, x)) >= 2]
            cand = cand or [(y, x) for y in range(H) for x in range(W) if g[y, x] < 0 and any(g[q] >= 0 for q in nb(g, y, x))]
            y, x = max(cand, key=lambda p: (sum(g[q] >= 0 for q in nb(g, *p)), p[0]))
            c = Counter(int(g[q]) for q in nb(g, y, x) if g[q] >= 0).most_common(1)[0][0]; g[y, x] = c
        else:
            cand = [(y, x) for y in range(H) for x in range(W) if g[y, x] >= 0 and sum(g[q] >= 0 for q in nb(g, y, x)) <= 2]
            y, x = min(cand, key=lambda p: (sum(g[q] >= 0 for q in nb(g, *p)), -p[0]))   # thinnest, higher first
            g[y, x] = -1
    # 2. per colour: recolour a surplus cell that touches a deficit colour (keeps parts connected)
    for _ in range(5000):
        have = Counter(int(v) for v in g.flatten() if v >= 0)
        sur = [c for c in target if have[c] > target[c]]; dfc = [c for c in target if have[c] < target[c]]
        if not sur and not dfc: break
        best = None
        for y in range(H):
            for x in range(W):
                a = int(g[y, x])
                if a not in sur: continue
                for q in nb(g, y, x):
                    b = int(g[q])
                    if b in dfc:
                        score = sum(int(g[r]) == b for r in nb(g, y, x)) * 2 - sum(int(g[r]) == a for r in nb(g, y, x))
                        if best is None or score > best[0]: best = (score, y, x, b)
        if best is None:
            # No shared border: relay through a colour m that borders both (a -> m here, then m -> b next round), so a
            # recoloured cell always sits next to its own colour (11-03: two orange cells had landed in the green bow).
            a, b = sur[0], dfc[0]
            def touches(c1, c2):
                return any(int(g[q]) == c2 for y in range(H) for x in range(W) if g[y, x] == c1 for q in nb(g, y, x))
            relay = [m for m in target if m not in (a, b) and touches(a, m) and touches(m, b)]
            if relay:
                m = relay[0]
                y, x = max(((y, x) for y in range(H) for x in range(W) if g[y, x] == a and any(int(g[q]) == m for q in nb(g, y, x))),
                           key=lambda p: sum(int(g[q]) == m for q in nb(g, *p)))
                g[y, x] = m; continue
            ps = [(y, x) for y in range(H) for x in range(W) if g[y, x] == a]
            qs = [(y, x) for y in range(H) for x in range(W) if g[y, x] == b] or ps
            y, x = min(ps, key=lambda p: min(abs(p[0] - q[0]) + abs(p[1] - q[1]) for q in qs))
            g[y, x] = b; continue
        _, y, x, b = best; g[y, x] = b
    return g

def grow_shrink(grid, target):
    """2026-11-03 (level 2's picture 04: its colour shares differ a lot from A's, and recolouring along colour borders
    ate an orange stripe): a short colour grows on its outer edge (empty cells next to it) and a long one is trimmed on
    its outer edge, so stripes, masts and faces inside the picture stay as drawn. Only colour 0 may grow into the
    bottom row. Then exact() settles any remainder."""
    g = grid.copy(); H, W = g.shape
    for _ in range(3000):
        have = Counter(int(v) for v in g.flatten() if v >= 0)
        dfc = [c for c in target if have[c] < target[c]]; sur = [c for c in target if have[c] > target[c]]
        moved = False
        for d in dfc:
            cand = [(y, x) for y in range(H) for x in range(W) if g[y, x] < 0 and (d == 0 or y < H - 1)
                    and any(int(g[q]) == d for q in nb(g, y, x))]
            if cand:
                y, x = max(cand, key=lambda p: (sum(int(g[q]) == d for q in nb(g, *p)), -sum(int(g[q]) not in (-1, d) for q in nb(g, *p))))
                g[y, x] = d; moved = True; break
        for c in sur:
            cand = [(y, x) for y in range(H) for x in range(W) if g[y, x] == c and any(g[q] < 0 for q in nb(g, y, x))
                    or (g[y, x] == c and (y in (0, H - 1) or x in (0, W - 1)))]
            if cand:
                y, x = min(cand, key=lambda p: sum(int(g[q]) == c for q in nb(g, *p)))
                g[y, x] = -1; moved = True; break
        if not moved or (not dfc and not sur):
            break
    return g


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
    ap.add_argument('--grow', action='store_true', help='grow/trim colours on the outer edge first (pictures whose colour shares differ from A)')
    a = ap.parse_args()
    res = RES[a.level]; A = json.loads((LEVELS / f'{res}.json').read_text())
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
    g = exact(grow_shrink(grid, target) if a.grow else grid, target)
    changed = int((g != grid).sum())
    B = json.loads(json.dumps(A)); B['cells'] = [int(v) for v in g.flatten()]; B['title'] = a.title
    B['shades'] = [0] * (W * H)   # sourceImage stays A's, like the other B drafts (the new picture is in the manifest)
    out = Path(a.out) if a.out else ROOT / f'Art/levels-b/work/第{a.level:03d}关_{res}'
    out.mkdir(parents=True, exist_ok=True)
    (out / f'{res}.json').write_text(json.dumps(B, ensure_ascii=False, separators=(',', ':')))
    fa, fb = first_rows(A['cells'], W, H), first_rows(B['cells'], W, H)
    rep = dict(level=a.level, resource=res, title=a.title, image=str(a.image), scale=round(float(scale), 2),
               quantized={int(k): v for k, v in before.items()}, target=target, changedCells=changed,
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

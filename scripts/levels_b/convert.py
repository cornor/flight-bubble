#!/usr/bin/env python3
"""Plan step 2: picture -> B-set level (draft).

  python3 scripts/levels_b/convert.py --level 3 --image my.png [--sigma 3:10,10:3 | --auto-sigma]
                                      [--title 新关名] [--resample block|box] [--fit crop|pad]

1. Square the picture (centre crop or pad) and quantize it to the level's colours (Lab nearest);
   by default every 10 x 10 block of a 600 px copy votes for its cell (keeps pixel-art edges crisp).
2. Optional colour permutation (mode P): --sigma A:B pairs, or --auto-sigma to pick the one whose
   targets best match the picture's colour amounts.
3. Refill: cells are changed only along colour borders until every colour has exactly its target
   count (plan 3.2); the preview rings every changed cell.
4. Validate against the A level (plan 3.1) and write Art/levels-b/work/<第NNN关_sectorXX>/:
   sectorXX.json, <tag>.png (A | quantized | B), <tag>.report.json; the manifest entry is 'draft'.
Exit code 0 = valid and within refill limits, 2 = refill limits exceeded (redraw or try another
permutation), 1 = error.
"""
import argparse
import json
import sys
from datetime import datetime
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).resolve().parent))
from levelsb_common import (CHANGED_PER_COLOR, CHANGED_TOTAL, MIN_PER_COLOR, ROOT, SIDE, WORK, apply_sigma, cells_sha256,
                            check_sigma, counts, dumps, file_sha256, file_tag, label, level_colors, load_a,
                            load_manifest, name, resource, rgb, save_manifest, validate)
from levelsb_render import board_image, font



def lab(arr):
    """sRGB (0-255, ...x3) -> CIE Lab."""
    a = arr.astype(np.float64) / 255.0
    a = np.where(a > .04045, ((a + .055) / 1.055) ** 2.4, a / 12.92)
    m = np.array([[.4124, .3576, .1805], [.2126, .7152, .0722], [.0193, .1192, .9505]])
    xyz = a @ m.T / np.array([.95047, 1.0, 1.08883])
    f = np.where(xyz > .008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


def load_picture(path, fit):
    img = Image.open(path)
    img = img.convert('RGBA')
    bg = Image.new('RGBA', img.size, (255, 255, 255, 255))
    img = Image.alpha_composite(bg, img).convert('RGB')
    w, h = img.size
    if w != h:
        if fit == 'pad':
            side = max(w, h)
            corner = img.getpixel((0, 0))
            sq = Image.new('RGB', (side, side), corner)
            sq.paste(img, ((side - w) // 2, (side - h) // 2))
            img = sq
        else:
            side = min(w, h)
            img = img.crop(((w - side) // 2, (h - side) // 2, (w - side) // 2 + side, (h - side) // 2 + side))
    return img


def quantize(img, palette_idx, level, resample):
    """Returns (grid 60x60 of colour indices, Lab distance of every cell to every candidate colour)."""
    pal = np.array([rgb(level, c) for c in palette_idx])
    pal_lab = lab(pal)
    if resample == 'box' or img.size[0] == SIDE:
        small = np.asarray(img.resize((SIDE, SIDE), Image.BOX))
        dist = np.linalg.norm(lab(small)[:, :, None, :] - pal_lab[None, None], axis=-1)
        return np.array(palette_idx)[dist.argmin(-1)], dist
    big = np.asarray(img.resize((SIDE * 10, SIDE * 10), Image.LANCZOS))
    dist_big = np.linalg.norm(lab(big)[:, :, None, :] - pal_lab[None, None], axis=-1)
    pick = dist_big.argmin(-1).reshape(SIDE, 10, SIDE, 10).transpose(0, 2, 1, 3).reshape(SIDE, SIDE, 100)
    votes = np.stack([(pick == k).sum(-1) for k in range(len(palette_idx))], -1)
    mean_dist = dist_big.reshape(SIDE, 10, SIDE, 10, -1).mean((1, 3))
    # Majority vote; ties go to the colour that is closer on average.
    grid = np.array(palette_idx)[(votes * 1000 - mean_dist).argmax(-1)]
    return grid, mean_dist


def quantize_big(img, palette_idx, level):
    """Per-pixel nearest colour (index into palette_idx) and Lab distances on a 600 x 600 copy."""
    pal_lab = lab(np.array([rgb(level, c) for c in palette_idx]))
    # Small sources are already pixel grids (e.g. 60 x 60): enlarge without smoothing so edges stay exact.
    method = Image.NEAREST if min(img.size) <= 300 else Image.LANCZOS
    big = np.asarray(img.resize((SIDE * 10, SIDE * 10), method))
    dist = np.linalg.norm(lab(big)[:, :, None, :] - pal_lab[None, None], axis=-1).astype(np.float32)
    return dist.argmin(-1), dist


def border_colour(pick):
    edge = np.concatenate([pick[0], pick[-1], pick[:, 0], pick[:, -1]])
    return int(np.bincount(edge).argmax())


SAMPLES = 4   # 4 x 4 sample points per cell


def render(pick, dist, palette_idx, scale=1.0, dx=0.0, dy=0.0, mirror=False):
    """60 x 60 grid of the picture scaled about the centre by `scale`, moved by (dx, dy) cells and optionally
    mirrored; area outside the picture takes the border (background) colour. Each cell is a majority vote of
    4 x 4 samples, ties to the closer colour. Returns (grid of colour indices, per-cell mean distances)."""
    k = len(palette_idx)
    bg = border_colour(pick)
    off = (np.arange(SAMPLES) + .5) / SAMPLES
    u = (np.arange(SIDE)[:, None] + off[None, :]).reshape(-1)          # cell units, SIDE*SAMPLES
    sx = ((u - SIDE / 2 - dx) / scale + SIDE / 2) * 10
    sy = ((u - SIDE / 2 - dy) / scale + SIDE / 2) * 10
    if mirror:
        sx = SIDE * 10 - sx
    ix, iy = np.floor(sx).astype(int), np.floor(sy).astype(int)
    inside = (iy[:, None] >= 0) & (iy[:, None] < SIDE * 10) & (ix[None, :] >= 0) & (ix[None, :] < SIDE * 10)
    cy, cx = np.clip(iy, 0, SIDE * 10 - 1), np.clip(ix, 0, SIDE * 10 - 1)
    p = np.where(inside, pick[cy[:, None], cx[None, :]], bg)
    d = dist[cy[:, None], cx[None, :]]
    bgd = np.full(k, 99.0, np.float32); bgd[bg] = 0
    d = np.where(inside[..., None], d, bgd)
    p = p.reshape(SIDE, SAMPLES, SIDE, SAMPLES).transpose(0, 2, 1, 3).reshape(SIDE, SIDE, -1)
    d = d.reshape(SIDE, SAMPLES, SIDE, SAMPLES, k).mean((1, 3))
    votes = np.stack([(p == j).sum(-1) for j in range(k)], -1)
    return np.array(palette_idx)[(votes * 1000 - d).argmax(-1)], d


def auto_sigma(level, hist):
    """Plan 3.1 mode P. Colours ranked by how much of the picture they cover are paired with A's
    colours ranked by board count (optimal for the summed count difference)."""
    used = level_colors(level)
    board = counts(level['cells'])
    by_target = sorted(used, key=lambda c: (-board.get(c, 0), c))
    by_picture = sorted(used, key=lambda c: (-hist.get(c, 0), c))
    return {a: b for a, b in zip(by_target, by_picture)}


def small_parts(mask, limit):
    """Cells of `mask` that belong to 4-connected parts smaller than `limit` (eyes, highlights...)."""
    seen = np.zeros(mask.shape, bool)
    small = np.zeros(mask.shape, bool)
    for y0, x0 in zip(*np.nonzero(mask)):
        if seen[y0, x0]:
            continue
        stack, part = [(y0, x0)], []
        seen[y0, x0] = True
        while stack:
            y, x = stack.pop()
            part.append((y, x))
            for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= ny < SIDE and 0 <= nx < SIDE and mask[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    stack.append((ny, nx))
        if len(part) < limit:
            for y, x in part:
                small[y, x] = True
    return small


def neighbours(mask, eight):
    m = mask.astype(np.int16)
    p = np.pad(m, 1)
    s = p[:-2, 1:-1] + p[2:, 1:-1] + p[1:-1, :-2] + p[1:-1, 2:]
    if eight:
        s = s + p[:-2, :-2] + p[:-2, 2:] + p[2:, :-2] + p[2:, 2:]
    return s


def refill(grid, target, dist, palette_idx):
    """Plan 3.2: move single cells along colour borders until counts match `target` exactly.
    A surplus colour reaches a deficit colour through the shortest chain of touching colours, so
    middle colours keep their counts. Each move takes the cell most surrounded by the new colour.
    18:30: moves prefer the outline between subject and background (+3) and avoid small parts such as
    eyes and highlights (-8 for parts under 15 cells)."""
    g = grid.copy()
    bg = max(palette_idx, key=lambda c: int((grid == c).sum()))
    protected = np.zeros(grid.shape, bool)   # small parts of the picture as drawn (eyes, highlights...)
    for c in palette_idx:
        protected |= small_parts(grid == c, 15)
    col_pos = {c: k for k, c in enumerate(palette_idx)}
    for _ in range(SIDE * SIDE * 4):
        have = {c: int((g == c).sum()) for c in palette_idx}
        diff = {c: have[c] - target.get(c, 0) for c in palette_idx}
        if all(v == 0 for v in diff.values()):
            return g
        surplus = [c for c in palette_idx if diff[c] > 0]
        deficit = [c for c in palette_idx if diff[c] < 0]
        # Colour adjacency graph (4-neighbour).
        adj = {c: set() for c in palette_idx}
        for a in palette_idx:
            if have[a] == 0:
                continue
            near = neighbours(g == a, False) > 0
            for b in palette_idx:
                if b != a and (near & (g == b)).any():
                    adj[b].add(a)          # a b-cell touches a: b can become a
        path = None
        frontier = [[s] for s in surplus]
        seen = set(surplus)
        while frontier and path is None:
            nxt = []
            for p in frontier:
                for b in sorted(adj[p[-1]]):
                    if b in seen:
                        continue
                    if b in deficit:
                        path = p + [b]
                        break
                    seen.add(b)
                    nxt.append(p + [b])
                if path:
                    break
            frontier = nxt
        if path is None:
            # A deficit colour touches nothing reachable (e.g. absent from the picture): seed it on
            # the surplus cell whose picture colour is closest.
            t = deficit[0]
            cand = np.isin(g, surplus)
            score = np.where(cand, -dist[..., col_pos[t]], -np.inf)
            y, x = np.unravel_index(score.argmax(), score.shape)
            g[y, x] = t
            continue
        for a, b in zip(path[:-1], path[1:]):
            cand = (g == a) & (neighbours(g == b, False) > 0)
            score = neighbours(g == b, True) * 4.0 - neighbours(g == a, True) - dist[..., col_pos[b]] * .01
            if a == bg or b == bg:
                score = score + 3.0
            else:
                score = score + 3.0 * (neighbours(g == bg, False) > 0)
            score = score - 8.0 * protected
            score = np.where(cand, score, -np.inf)
            y, x = np.unravel_index(score.argmax(), score.shape)
            g[y, x] = b
    raise RuntimeError('补齐没有收敛')


SCALES = (.80, .85, .90, .95, 1.0, 1.05, 1.10)
SHIFTS = (-3, 0, 3)


def search(img, a, used, modes, groups=2):
    """18:30: try the picture at several sizes / positions / mirrored and with or without a colour permutation.
    Sizes x modes are ranked by the count gap (a lower bound of refill moves); for the best `groups` of them
    every position and mirror is refilled. Choice: fewest refill moves, then the closest front-row colour mix
    to A (rows 10 and 20 from the defence line). 18:45: the "ideal player" replay turned out chaotic (swapping
    20 adjacent cells, invisible, moved its result by up to 35 %), so it is only reported, not used to choose."""
    import structure
    pick, dist = quantize_big(img, used, a)
    board = counts(a['cells'])
    a_layers = structure.layers(a)
    a_sim = structure.simulate(a)
    renders, gaps = {}, {}
    for scale in SCALES:
        for dx in SHIFTS:
            for dy in SHIFTS:
                for mirror in (False, True):
                    grid, d = render(pick, dist, used, scale, dx, dy, mirror)
                    hist = counts(grid.flatten().tolist())
                    renders[(scale, dx, dy, mirror)] = (grid, d, hist)
                    for mode in modes:
                        sigma = auto_sigma(a, hist) if mode == 'P' else {c: c for c in used}
                        target = {sigma[c]: n for c, n in board.items()}
                        gap = sum(abs(hist.get(c, 0) - target.get(c, 0)) for c in used) // 2
                        key = (scale, mode)
                        gaps[key] = min(gaps.get(key, 10 ** 9), gap)
    best = sorted(gaps, key=lambda k: (gaps[k], abs(k[0] - 1)))[:groups]
    tried = []
    for scale, mode in best:
        for (sc, dx, dy, mirror), (grid, d, hist) in renders.items():
            if sc != scale:
                continue
            sigma = auto_sigma(a, hist) if mode == 'P' else {c: c for c in used}
            identity = all(k == v for k, v in sigma.items())
            base = a if identity else apply_sigma(a, sigma)
            final = refill(grid, counts(base['cells']), d, used)
            b = json.loads(json.dumps(base)); b['cells'] = final.flatten().tolist()
            b_layers = structure.layers(b)
            mix = round(sum(structure.layer_distance(a_layers[k], b_layers[k]) for k in (10, 20)) / 2, 3)
            tried.append(dict(transform=dict(scale=scale, dx=dx, dy=dy, mirror=mirror, mode=mode), gap=gaps[(scale, mode)],
                              changes=int((final != grid).sum()), mix=mix, b=b, sigma=sigma, grid=grid, dist=d, final=final))
    plain = lambda t: (abs(t['scale'] - 1) > 1e-6) + (t['dx'] != 0) + (t['dy'] != 0) + t['mirror']
    tried.sort(key=lambda r: (r['changes'] // 10, r['mix'], plain(r['transform'])))
    for r in tried[:5]:   # the replay is only reported (chaotic), so only for the shortlist
        r['structure'], r['structureNote'] = structure.judge(a_sim, structure.simulate(r['b']))
    for r in tried[5:]:
        r['structure'], r['structureNote'] = '', ''
    return tried[0], tried


def preview(a, quant_cells, b, changed, tag, title_a, title_b, stats, path):
    s = 6
    w = SIDE * s
    img = Image.new('RGB', (w * 3 + 80, w + 120 + 22 * len(stats)), (28, 32, 46))
    d = ImageDraw.Draw(img)
    heads = [f'A 原关  {title_a}', '量化后（补齐前）', f'B 新关  {title_b}  （黑圈 = 补齐改动 {len(changed)} 格）']
    for k, (cells, marks) in enumerate([(a['cells'], None), (quant_cells, None), (b['cells'], changed)]):
        x = 20 + k * (w + 20)
        d.text((x, 12), heads[k], font=font(16), fill=(255, 229, 138))
        img.paste(board_image(cells, b if k else a, s, marks), (x, 40))
    y = w + 56
    d.text((20, y), f'{tag}', font=font(16), fill=(235, 238, 245))
    y += 28
    d.text((20, y), '颜色        目标格数   量化后   补齐后   改动', font=font(15), fill=(190, 198, 215))
    for row in stats:
        y += 22
        d.rectangle([20, y + 2, 36, y + 16], fill=rgb(b, row['index']))
        d.text((44, y), f'{row["name"]:<6}  {row["target"]:>8}  {row["quantized"]:>8}  {row["final"]:>8}  {row["moved"]:>6}'
               + ('  ⚠' if row['over'] else ''), font=font(15), fill=(235, 238, 245))
    img.save(path)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--level', type=int, required=True, help='游戏关号（3–262）')
    ap.add_argument('--image', required=True)
    g = ap.add_mutually_exclusive_group()
    g.add_argument('--sigma', help='颜色置换，A索引:B索引，逗号分隔，例如 3:10,10:3')
    g.add_argument('--auto-sigma', action='store_true', help='按图中各色多少自动选置换')
    ap.add_argument('--title', help='B 关名（默认沿用 A 关名）')
    ap.add_argument('--resample', choices=['block', 'box'], default='block')
    ap.add_argument('--fit', choices=['crop', 'pad'], default='crop')
    ap.add_argument('--out', help='输出目录（默认 Art/levels-b/work/<关>）')
    ap.add_argument('--no-manifest', action='store_true', help='仅输出文件，不写制作清单（隔离测试用）')
    ap.add_argument('--no-search', action='store_true', help='不自动尝试缩放/平移/翻转/换色，按原图直接转换')
    args = ap.parse_args()

    game, res, tag = args.level, resource(args.level), file_tag(args.level)
    a, a_raw = load_a(game)
    used = level_colors(a)
    img = load_picture(args.image, args.fit)

    transform, tried = None, []
    if not args.no_search and not args.sigma:
        modes = ('P',) if args.auto_sigma else ('S', 'P')
        choice, tried = search(img, a, used, modes)
        grid, dist, transform = choice['grid'], choice['dist'], choice['transform']
        hist = counts(grid.flatten().tolist())
        sigma = choice['sigma']
        for r in tried[:5]:
            t = r['transform']
            print(f'  候选：缩放 {t["scale"]:.2f} 平移 ({t["dx"]:+d},{t["dy"]:+d}) {"翻转 " if t["mirror"] else ""}模式 {t["mode"]}'
                  f' → 改动 {r["changes"]} 格，前排差异 {r["mix"]:.2f}，理想打法{r["structure"]}')
        print(f'  共试 {len(tried)} 种摆放')
    else:
        # Quantize to every colour of the level first; the permutation decides which ones the board needs.
        grid, dist = quantize(img, used, a, args.resample)
        hist = counts(grid.flatten().tolist())
    if transform is not None:
        pass
    elif args.auto_sigma:
        sigma = auto_sigma(a, hist)
    elif args.sigma:
        sigma = {int(x): int(y) for x, y in (p.split(':') for p in args.sigma.split(','))}
        for c in used:
            sigma.setdefault(c, c)
    else:
        sigma = {c: c for c in used}
    check_sigma(a, sigma)
    identity = all(k == v for k, v in sigma.items())
    mode = 'S' if identity else 'P'
    base = a if identity else apply_sigma(a, sigma)
    target = counts(base['cells'])

    final = tried[0]['final'] if transform is not None else refill(grid, target, dist, used)
    quant_cells = grid.flatten().tolist()
    cells = final.flatten().tolist()
    changed = {i for i, (q, f) in enumerate(zip(quant_cells, cells)) if q != f}

    b = json.loads(json.dumps(base))
    b['cells'] = cells
    if args.title:
        b['title'] = args.title
    problems = validate(a, b, mode, None if identity else sigma)
    if problems:
        print(f'{label(game)} 校验失败：', *problems, sep='\n  ')
        return 1

    stats, over = [], False
    for c in sorted(set(target) | set(hist), key=lambda c: (-target.get(c, 0), c)):
        moved = sum(1 for i in changed if quant_cells[i] == c or cells[i] == c)
        limit = max(CHANGED_PER_COLOR * target.get(c, 0), MIN_PER_COLOR)
        row = dict(index=c, name=name(b, c), target=target.get(c, 0), quantized=hist.get(c, 0),
                   final=cells.count(c), moved=moved, over=moved > limit)
        over |= row['over']
        stats.append(row)
    over |= len(changed) > CHANGED_TOTAL

    out = Path(args.out) if args.out else WORK / tag
    out.mkdir(parents=True, exist_ok=True)
    data = dumps(b, a_raw)
    (out / f'{res}.json').write_bytes(data)
    preview(a, quant_cells, b, changed, label(game), a.get('title', ''), b.get('title', ''), stats, out / f'{tag}.png')
    src = Path(args.image).read_bytes()
    report = dict(game=game, resource=res, mode=mode, sigma={str(k): v for k, v in sigma.items() if k != v},
                  sourceImage=str(Path(args.image).resolve()), sourceSha256=file_sha256(src),
                  fileSha256=file_sha256(data), cellsSha256=cells_sha256(cells),
                  changedCells=len(changed), withinLimits=not over, colors=stats, transform=transform,
                  candidates=[dict(transform=r['transform'], changes=r['changes'], mix=r['mix'], structure=r['structure'],
                                   structureNote=r['structureNote']) for r in tried],
                  converted=datetime.now().isoformat(timespec='seconds'))
    (out / f'{tag}.report.json').write_text(json.dumps(report, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')

    if not args.no_manifest:
        manifest = load_manifest()
        entry = manifest['levels'].get(res, {})
        if entry.get('status') == 'approved' and entry.get('fileSha256') != report['fileSha256']:
            print(f'注意：{label(game)} 原为 approved，内容已变，状态退回 draft')
        entry.update({k: report[k] for k in ('game', 'resource', 'mode', 'sigma', 'sourceImage', 'sourceSha256',
                                             'fileSha256', 'cellsSha256', 'changedCells', 'withinLimits')})
        entry['title'] = b.get('title', '')
        entry['draft'] = str((out / f'{res}.json').relative_to(ROOT)) if out.is_relative_to(ROOT) else str(out / f'{res}.json')
        entry['status'] = 'draft'
        manifest['levels'][res] = entry
        save_manifest(manifest)

    verdict = '通过' if not over else '补齐改动超限，建议重画或换置换'
    where = '' if transform is None else (f'（缩放 {transform["scale"]:.2f}，平移 {transform["dx"]:+d},{transform["dy"]:+d}'
                                          + ('，左右翻转' if transform['mirror'] else '') + '）')
    print(f'{label(game)}  模式 {mode}{where}  补齐改动 {len(changed)} 格  {verdict}\n  → {out}')
    return 0 if not over else 2


if __name__ == '__main__':
    sys.exit(main())

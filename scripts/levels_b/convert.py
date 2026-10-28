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
from levelsb_common import (invert, full_map, debut_colors, allowed_colors, CHANGED_PER_COLOR, CHANGED_TOTAL, MIN_PER_COLOR, ROOT, SIDE, WORK, apply_sigma, cells_sha256,
                            check_sigma, counts, dumps, file_sha256, file_tag, label, level_colors, load_a,
                            load_manifest, name, resource, rgb, save_manifest, validate)
from levelsb_render import board_image, font
from conversion_guards import (candidate_key, crop_safe, foreground_bounds, load_constraints,
                               protection_mask, refill_budget)



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


def auto_sigma(level, hist, colours=None):
    """Plan 3.1 mode P (colours = the level's own) or R (colours = those chosen from the picture). Colours ranked by
    how much of the picture they cover are paired with A's colours ranked by board count (optimal for the summed
    count difference)."""
    used = level_colors(level)
    colours = list(colours or used)
    board = counts(level['cells'])
    on_board = sorted((c for c in used if board.get(c, 0)), key=lambda c: (-board[c], c))
    by_picture = sorted(colours, key=lambda c: (-hist.get(c, 0), c))
    sigma = dict(zip(on_board, by_picture))
    # Colours only in the sand stream are not drawn: keep them as they are whenever that stays one-to-one.
    taken = set(sigma.values())
    spare = [c for c in by_picture if c not in taken]
    for c in sorted(set(used) - set(on_board)):
        sigma[c] = c if c not in taken and (c in colours or c not in set(by_picture)) else spare.pop(0)
        taken.add(sigma[c])
        if sigma[c] in spare:
            spare.remove(sigma[c])
    return sigma


def choose_recolor(img, a, game):
    """Mode R (2026-10-26 23:31 user decision): pick as many colours as the A level uses from every member that has
    debuted by this level, by how much of the picture each covers; this level's debut members are always kept."""
    from levelsb_common import allowed_colors, debut_colors
    allowed = sorted(allowed_colors(game, a))
    pick, _ = quantize_big(img, allowed, a)
    cover = np.bincount(pick.ravel(), minlength=len(allowed))
    board = counts(a['cells'])
    k = sum(1 for c in level_colors(a) if board.get(c, 0))   # colours drawn on the board
    must = sorted(c for c in debut_colors(game, a) if board.get(c, 0))
    rest = [allowed[i] for i in np.argsort(-cover, kind='stable') if allowed[i] not in must]
    chosen = must + rest[:k - len(must)]
    # Stream-only colours keep their own member when it is not drawn on the board (see auto_sigma).
    for c in level_colors(a):
        if not board.get(c, 0):
            chosen.append(c if c not in chosen else next(x for x in allowed if x not in chosen))
    share = {allowed[i]: float(cover[i]) / cover.sum() for i in range(len(allowed))}
    return sorted(chosen), share


def balance_assignment(level, hist, colours, pool=(), must=()):
    """Stream balance (2026-10-27 12:20 user decision): which A colour each picture colour stands for. The picture
    looks the same under any choice; pick the one-to-one map that needs the fewest board changes (a picture colour
    may not exceed its new colour's total beads) and then the fewest stream recolourings. Exact bitmask DP.
    A colours left without a picture colour (the picture draws fewer colours) take spare labels from `pool`
    (members already debuted) and live only in the stream. Colours only in A's stream keep their member if they can."""
    used = level_colors(level)
    board = counts(level['cells'])
    total = counts(list(level['cells']) + list(level.get('stream') or []))
    roles = sorted((c for c in used if board.get(c, 0)), key=lambda c: (-board[c], c))
    stream_only = sorted(set(used) - set(roles))
    drawn = [c for c in sorted(colours, key=lambda c: (-hist.get(c, 0), c)) if hist.get(c, 0)]
    pics = list(dict.fromkeys([c for c in must if c in colours] + drawn))[:len(roles)]
    pad = [c for c in sorted(set(colours) | set(pool)) if c not in pics and c not in stream_only]
    pics += pad[:len(roles) - len(pics)]
    if len(pics) < len(roles):
        raise ValueError('可用颜色不够，无法一一对应')
    k = len(roles)
    cost = [[(max(0, hist.get(p, 0) - total[r]), abs(hist.get(p, 0) - board[r])) for p in pics] for r in roles]
    best = {0: ((0, 0), [])}
    for i in range(k):
        nxt = {}
        for mask, (c, picks) in best.items():
            for j in range(len(pics)):
                if mask >> j & 1:
                    continue
                cand = (c[0] + cost[i][j][0], c[1] + cost[i][j][1])
                m2 = mask | 1 << j
                if m2 not in nxt or cand < nxt[m2][0]:
                    nxt[m2] = (cand, picks + [j])
        best = nxt
    (_, picks) = min(best.values(), key=lambda v: v[0])
    sigma = {r: pics[j] for r, j in zip(roles, picks)}
    taken = set(sigma.values())
    spare = [c for c in sorted(set(colours) | set(pool)) if c not in taken]
    for c in stream_only:
        sigma[c] = c if c not in taken else spare.pop(0)
        taken.add(sigma[c])
        if sigma[c] in spare:
            spare.remove(sigma[c])
    return sigma


def balance_colours(img, a, game, pic_colours, resample):
    """Stream balance: if a picture colour cannot fit any A colour's total, try drawing one or two fewer colours
    (the smallest non-debut ones merge into their nearest colour). Keeps the full set unless dropping saves board
    changes; returns (colours, grid, dist, hist)."""
    board = counts(a['cells'])
    k = sum(1 for c in level_colors(a) if board.get(c, 0))
    must = set(c for c in debut_colors(game, a) if board.get(c, 0))
    pool = sorted(allowed_colors(game, a))
    # Only the k picked colours paint the picture; labels for stream-only colours come from `pool` later.
    stream_only = set(c for c in level_colors(a) if not board.get(c, 0))
    drawable = [c for c in pic_colours if c not in stream_only or c in must]
    grid, dist = quantize(img, drawable, a, resample)
    hist = counts(grid.flatten().tolist())
    ranked = sorted(drawable, key=lambda c: -hist.get(c, 0))
    options = []
    for drop in range(0, 3):
        keep = ranked[:k - drop] + [c for c in ranked[k - drop:] if c in must]
        cols = sorted(set(keep))
        g2, d2 = (grid, dist) if drop == 0 and len(cols) == len(drawable) else quantize(img, cols, a, resample)
        h2 = counts(g2.flatten().tolist())
        sigma = balance_assignment(a, h2, cols, pool, must)
        base = apply_sigma(a, sigma)
        total = counts(list(base['cells']) + list(base.get('stream') or []))
        excess = sum(max(0, n - total.get(c, 0)) for c, n in h2.items())
        options.append((excess + 60 * drop, drop, cols, g2, d2, h2))
    _, drop, cols, grid, dist, hist = min(options, key=lambda o: o[:2])
    return cols, grid, dist, hist, drop, pool, sorted(must)


def balance_targets(base, hist):
    """Board target per colour: the picture's own count, except a colour may not exceed its total beads; any excess
    goes to the colours with the most room (a small refill on the board)."""
    total = counts(list(base['cells']) + list(base.get('stream') or []))
    target = {c: min(hist.get(c, 0), total[c]) for c in total}
    excess = sum(hist.values()) - sum(target.values())
    got = {c: 0 for c in total}
    while excess > 0:
        # Spread the excess so no receiving colour exceeds its own per-colour refill limit if avoidable.
        head = lambda c: max(CHANGED_PER_COLOR * (target[c] + 1), MIN_PER_COLOR) - got[c]
        open_ = [c for c in total if total[c] > target[c]]
        if not open_:
            raise ValueError('沙流容量不足，无法保持各色总数')
        c = max(open_, key=lambda c: (head(c), total[c] - target[c], -c))
        target[c] += 1; got[c] += 1; excess -= 1
    return {c: n for c, n in target.items() if n}, total


def rebalance_stream(stream, offsets, need, place='even'):
    """Recolour stream beads so that colour c appears need[c] times. Length and the per-column split stay.
    place='even': beads to change are spread evenly over columns and depths (key = depth fraction within its
    column, then column); place='deep': the deepest beads of each surplus colour change, so the early and middle
    game keeps A's stream and only the last part differs."""
    stream = list(stream)
    have = counts(stream)
    key = {}
    for col in range(len(offsets) - 1):
        lo, hi = offsets[col], offsets[col + 1]
        for i in range(lo, hi):
            key[i] = ((i - lo + .5) / max(1, hi - lo), col)
    picked = []
    for c in sorted(have):
        drop = have[c] - need.get(c, 0)
        if drop <= 0:
            continue
        pos = sorted((i for i, x in enumerate(stream) if x == c), key=lambda i: key[i])
        picked += pos[-drop:] if place == 'deep' else [pos[int((j + .5) * len(pos) / drop)] for j in range(drop)]
    seq = sorted(((j + .5) / (need[c] - have.get(c, 0)), c) for c in need if need[c] > have.get(c, 0)
                 for j in range(need[c] - have.get(c, 0)))
    if len(seq) != len(picked):
        raise ValueError('沙流重配数量不一致')
    for i, (_, c) in zip(sorted(picked, key=lambda i: key[i]), seq):
        stream[i] = c
    return stream


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


class RefillBlocked(ValueError):
    """No exact refill remains within the protected/editable regions."""


def refill(grid, target, dist, palette_idx, protected=None, recolor_from=None, enforce_budget=False):
    """Plan 3.2: move single cells along colour borders until counts match `target` exactly.
    A surplus colour reaches a deficit colour through the shortest chain of touching colours, so
    middle colours keep their counts. Each move takes the cell most surrounded by the new colour.
    Small components and thin silhouette tips are locked, as are explicit source-coordinate regions.
    Each cell can change at most once: intermediate colours cannot churn through already adjusted cells.
    If this cannot satisfy the exact counts, fail instead of silently damaging protected parts."""
    g = grid.copy()
    bg = max(palette_idx, key=lambda c: int((grid == c).sum()))
    protected = np.zeros(grid.shape, bool) if protected is None else protected.copy()
    for c in palette_idx:
        protected |= small_parts(grid == c, 15)
    foreground = grid != bg
    protected |= foreground & (neighbours(foreground, False) <= 2)
    allowed = {int(c): np.isin(grid, sources) for c, sources in (recolor_from or {}).items()}
    for c in palette_idx:
        allowed.setdefault(c, np.ones(grid.shape, bool))
    col_pos = {c: k for k, c in enumerate(palette_idx)}
    spent = {c: 0 for c in palette_idx}
    moves = 0
    for _ in range(SIDE * SIDE * 4):
        have = {c: int((g == c).sum()) for c in palette_idx}
        diff = {c: have[c] - target.get(c, 0) for c in palette_idx}
        if all(v == 0 for v in diff.values()):
            return g

        def affordable(path):
            # Reserve the unavoidable remaining count correction as well as this path's moves.
            # A balanced intermediate colour spends two edits, despite having no net count change.
            if not enforce_budget:
                return True
            remaining = dict(diff)
            added = {c: 0 for c in palette_idx}
            for a, b in zip(path[:-1], path[1:]):
                remaining[a] -= 1; remaining[b] += 1
                added[a] += 1; added[b] += 1
            return (moves + max(0, len(path) - 1) + sum(abs(v) for v in remaining.values()) // 2 <= CHANGED_TOTAL
                    and all(spent[c] + added[c] + abs(remaining[c]) <=
                            max(CHANGED_PER_COLOR * target.get(c, 0), MIN_PER_COLOR) for c in palette_idx))

        if not affordable([]):
            raise RefillBlocked('剩余补齐格数已超出原有逐色或总数门槛')
        surplus = sorted((c for c in palette_idx if diff[c] > 0), key=lambda c: (-diff[c], c))
        deficit = [c for c in palette_idx if diff[c] < 0]
        editable = ~protected & (g == grid)
        # Colour adjacency graph (4-neighbour).
        adj = {c: set() for c in palette_idx}
        for a in palette_idx:
            if have[a] == 0:
                continue
            near = neighbours(g == a, False) > 0
            for b in palette_idx:
                if b != a and (near & (g == b) & editable & allowed[a]).any():
                    adj[b].add(a)          # a b-cell touches a: b can become a
        path = None
        frontier = [[s] for s in surplus]
        seen = set(surplus)
        while frontier and path is None:
            nxt = []
            for p in frontier:
                for b in sorted(adj[p[-1]]):
                    if b in (p if enforce_budget else seen) or not affordable(p + [b]):
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
            # Only an absent colour may be seeded away from a boundary. Existing colours must stay
            # on their borders; a blocked boundary is a real constraint failure.
            absent = [c for c in deficit if have[c] == 0]
            if not absent:
                raise RefillBlocked('受保护区域之外没有可补齐的颜色边界')
            t = absent[0]
            cand = np.isin(g, [s for s in surplus if affordable([s, t])]) & editable & allowed[t]
            if not cand.any():
                raise RefillBlocked('缺失颜色没有可用的补齐位置')
            score = np.where(cand, -dist[..., col_pos[t]], -np.inf)
            y, x = np.unravel_index(score.argmax(), score.shape)
            spent[int(g[y, x])] += 1; spent[t] += 1; moves += 1
            g[y, x] = t
            continue
        for a, b in zip(path[:-1], path[1:]):
            cand = (g == a) & (neighbours(g == b, False) > 0) & ~protected & (g == grid) & allowed[b]
            if not cand.any():
                raise RefillBlocked('颜色路径被保护区域阻断')
            score = neighbours(g == b, True) * 4.0 - neighbours(g == a, True) - dist[..., col_pos[b]] * .01
            if a == bg or b == bg:
                score = score + 3.0
            else:
                score = score + 3.0 * (neighbours(g == bg, False) > 0)
            score = np.where(cand, score, -np.inf)
            y, x = np.unravel_index(score.argmax(), score.shape)
            g[y, x] = b
            spent[a] += 1; spent[b] += 1; moves += 1
    raise RuntimeError('补齐没有收敛')


def refill_candidate(grid, target, dist, palette_idx, protected=None, recolor_from=None):
    """Keep existing valid conversions; retry invalid/blocked ones while reserving the same hard budgets."""
    try:
        final = refill(grid, target, dist, palette_idx, protected, recolor_from)
    except RefillBlocked:
        return refill(grid, target, dist, palette_idx, protected, recolor_from, enforce_budget=True), True
    if not refill_budget(grid, final, target)['withinLimits']:
        try:
            return refill(grid, target, dist, palette_idx, protected, recolor_from, enforce_budget=True), True
        except RefillBlocked:
            pass  # Retain the failed diagnostic, never promote it to a passing candidate.
    return final, False


SCALES = (.80, .85, .90, .95, 1.0, 1.05, 1.10)
SHIFTS = (-3, 0, 3)


def search(img, a, used, modes, groups=2, constraints=None, fine=False, fixed_transform=None):
    """Try crop-safe transforms, initially the best `groups` sizes per mode, expanding if needed.
    All refill budgets must pass before ranking by exact changes (D10/D20 only reported since 2026-10-26 23:31;
    difficulty is judged by the headless simulation and queue tuning). `used` = the colours the picture may use.
    The simplified player replay remains diagnostic only. This greedy search does not prove that an
    unselected or infeasible picture has no possible solution."""
    import structure
    constraints = constraints or {}
    pick, dist = quantize_big(img, used, a)
    bounds = foreground_bounds(pick, border_colour(pick), constraints)
    board = counts(a['cells'])
    a_layers = structure.layers(a)
    a_sim = structure.simulate(a)
    renders, gaps = {}, {}
    scales = tuple(round(x / 200, 3) for x in range(160, 221)) if fine else SCALES
    shifts = tuple(range(-3, 4)) if fine else SHIFTS
    xshifts, yshifts, mirrors = shifts, shifts, (False, True)
    if fixed_transform is not None:
        sc, dx, dy, mirror = fixed_transform
        if not (.8 <= sc <= 1.1 and dx in range(-3, 4) and dy in range(-3, 4) and mirror in (0, 1)):
            raise ValueError('固定取景仍须在缩放0.8–1.1、整数平移±3格、镜像0/1范围内')
        scales, xshifts, yshifts, mirrors = (sc,), (int(dx),), (int(dy),), (bool(mirror),)
    rejected_crop, rejected_refill, rejected_lower_bound = 0, 0, 0
    for scale in scales:
        for dx in xshifts:
            for dy in yshifts:
                for mirror in mirrors:
                    if not crop_safe(bounds, scale, dx, dy, mirror):
                        rejected_crop += 1
                        continue
                    grid, d = render(pick, dist, used, scale, dx, dy, mirror)
                    hist = counts(grid.flatten().tolist())
                    renders[(scale, dx, dy, mirror)] = (grid, d, hist)
                    for mode in modes:
                        sigma = auto_sigma(a, hist, used) if mode in ('P', 'R') else {c: c for c in used}
                        target = {sigma[c]: n for c, n in board.items()}
                        gap = sum(abs(hist.get(c, 0) - target.get(c, 0)) for c in used) // 2
                        key = (scale, mode)
                        gaps[key] = min(gaps.get(key, 10 ** 9), gap)
    # 2026-10-20: the best `groups` sizes for EACH mode. Taking them across modes let a P permutation with a smaller
    # count gap crowd out S even when S keeps the colours where they belong (第 9 关 -06: P failed D_20, S was not tried).
    ordered = [k for mode in modes for k in sorted((k for k in gaps if k[1] == mode),
                                                 key=lambda k: (gaps[k], abs(k[0] - 1)))]
    best = [k for mode in modes for k in [k for k in ordered if k[1] == mode][:groups]]
    tried = []
    # Start small, then expand every remaining scale if the initial groups have no fully passing result.
    for scale, mode in best + [k for k in ordered if k not in best]:
        if (scale, mode) not in best and any(r['budget']['withinLimits'] for r in tried):
            break
        for (sc, dx, dy, mirror), (grid, d, hist) in renders.items():
            if sc != scale:
                continue
            sigma = auto_sigma(a, hist, used) if mode in ('P', 'R') else {c: c for c in used}
            identity = all(k == v for k, v in sigma.items())
            base = a if identity else apply_sigma(a, sigma)
            target = counts(base['cells'])
            # Outside the initial diagnostic shortlist, don't spend time refilling a layout whose
            # unavoidable count difference already exceeds a hard budget.
            if (scale, mode) not in best:
                delta = {c: abs(hist.get(c, 0) - target.get(c, 0)) for c in used}
                if (sum(delta.values()) // 2 > CHANGED_TOTAL
                        or any(delta[c] > max(CHANGED_PER_COLOR * target.get(c, 0), MIN_PER_COLOR) for c in used)):
                    rejected_lower_bound += 1
                    continue
            try:
                final, budget_retry = refill_candidate(grid, target, d, used,
                               protection_mask(constraints, sc, dx, dy, mirror), constraints.get('recolorFrom'))
            except RefillBlocked:
                rejected_refill += 1
                continue
            b = json.loads(json.dumps(base)); b['cells'] = final.flatten().tolist()
            # 22:05: compare the front mix in A's colour roles (undo the permutation), or a P candidate that merely
            # swaps two colours would look "different" while being structurally the same level.
            b_layers = structure.layers(b if identity else apply_sigma(b, invert(sigma)))
            exact = structure.front_mix(a_layers, b_layers)
            tried.append(dict(transform=dict(scale=scale, dx=dx, dy=dy, mirror=mirror, mode=mode), gap=gaps[(scale, mode)],
                              changes=int((final != grid).sum()), mix=round(float(exact), 4), mixExact=exact,
                              passes=structure.mix_passes(a_layers, b_layers), budget=refill_budget(grid, final, target),
                              b=b, sigma=sigma, grid=grid, dist=d, final=final, budgetRetry=budget_retry))
    if not tried:
        raise RefillBlocked(f'当前搜索未找到满足主体保护的精确补齐方案（裁切拒绝 {rejected_crop}，'
                            f'补齐受阻 {rejected_refill}，格数下界超限 {rejected_lower_bound}）')
    tried.sort(key=candidate_key)
    tried[0]['searchDiagnostics'] = dict(cropRejected=rejected_crop, refillRejected=rejected_refill,
                                        lowerBoundRejected=rejected_lower_bound, foregroundBounds=bounds, fine=fine)
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
    ap.add_argument('--stream-balance', action='store_true', help='画面照原图转换，各色多出/缺少的数量到沙流里换色补回（总数、排队、弹量不变；10-27 用户确认）')
    ap.add_argument('--stream-place', choices=['even', 'deep'], default='even', help='沙流补偿时换色泡泡的位置：均匀分散 / 集中在各列最深处')
    ap.add_argument('--recolor', action='store_true', help='换色模式 R：从到本关为止已登场的角色颜色里，按画面挑出与原关同样多的颜色（保留本关新角色）')
    ap.add_argument('--title', help='B 关名（默认沿用 A 关名）')
    ap.add_argument('--resample', choices=['block', 'box'], default='block')
    ap.add_argument('--fit', choices=['crop', 'pad'], default='crop')
    ap.add_argument('--out', help='输出目录（默认 Art/levels-b/work/<关>）')
    ap.add_argument('--no-manifest', action='store_true', help='仅输出文件，不写制作清单（隔离测试用）')
    ap.add_argument('--no-search', action='store_true', help='不自动尝试缩放/平移/翻转/换色，按原图直接转换')
    ap.add_argument('--only-s', action='store_true', help='搜索时只用模式 S（不换色），写修改意见时用，颜色名就是画面里的颜色')
    ap.add_argument('--constraints', type=Path, help='绑定源图哈希的保护/可编辑矩形 JSON，坐标按源图 60×60')
    ap.add_argument('--fine-search', action='store_true', help='缩放步长 0.005、平移步长 1 格；无合格结果时扩大搜索')
    ap.add_argument('--transform', nargs=4, type=float, metavar=('SCALE', 'DX', 'DY', 'MIRROR'),
                    help='复现指定取景（镜像0/1），仍执行裁切、主体保护、补齐与结构门槛')
    args = ap.parse_args()
    if args.transform is not None and (args.no_search or args.sigma or args.fine_search):
        ap.error('--transform 不与 --no-search、--sigma 或 --fine-search 混用')
    if args.recolor and (args.only_s or args.constraints and json.loads(Path(args.constraints).read_text()).get('recolorFrom')):
        ap.error('--recolor 不与 --only-s 或 recolorFrom 同用')

    game, res, tag = args.level, resource(args.level), file_tag(args.level)
    a, a_raw = load_a(game)
    used = level_colors(a)
    img = load_picture(args.image, args.fit)
    constraints = load_constraints(args.constraints, args.image)
    if any(int(c) not in used or any(s not in used for s in sources)
           for c, sources in constraints.get('recolorFrom', {}).items()):
        ap.error('recolorFrom 只能使用本关的颜色索引')
    with Image.open(args.image) as source:
        source_size = source.size
    if constraints and source_size[0] != source_size[1]:
        ap.error('保护配置仅支持正方形源图，避免裁切/补边改变坐标')

    transform, tried = None, []
    pic_colours, cover = used, None
    if args.recolor:
        pic_colours, cover = choose_recolor(img, a, game)
        print('  换色：画面选用 ' + '、'.join(f'{name(a, c)}{cover[c]:.0%}' for c in sorted(pic_colours, key=lambda c: -cover[c])))
    elif args.sigma:
        pic_colours = sorted(full_map(a, {int(x): int(y) for x, y in (p.split(':') for p in args.sigma.split(','))}).values())
    stream_changed = None
    if args.stream_balance:
        # The picture as drawn: no zoom/shift search, no exact-count refill beyond what the totals force.
        pic_colours, grid, dist, hist, dropped, pool, must = balance_colours(img, a, game, pic_colours, args.resample)
        if dropped:
            print(f'  沙流补偿：画面少用 {dropped} 种颜色（并入最接近的颜色），以免超出该色总数')
        sigma = balance_assignment(a, hist, pic_colours, pool, must)
        identity = all(k == v for k, v in sigma.items())
        mode = 'S' if identity else 'P' if set(sigma.values()) == set(used) else 'R'
        check_sigma(a, sigma, game, recolor=mode == 'R')
        base = a if identity else apply_sigma(a, sigma)
        target, total = balance_targets(base, hist)
        final, budget_retry = (grid, False) if target == {c: n for c, n in hist.items() if n} else refill_candidate(
            grid, target, dist, sorted(set(pic_colours) | set(target)), protection_mask(constraints), constraints.get('recolorFrom'))
        need = {c: total[c] - target.get(c, 0) for c in total}
        new_stream = rebalance_stream(base['stream'], base['streamOffsets'], need, args.stream_place)
        stream_changed = sum(x != y for x, y in zip(new_stream, base['stream']))
        base = json.loads(json.dumps(base)); base['stream'] = new_stream
    elif not args.no_search and not args.sigma:
        modes = ('R',) if args.recolor else ('P',) if args.auto_sigma else ('S',) if args.only_s else ('S', 'P')
        choice, tried = search(img, a, pic_colours, modes, constraints=constraints, fine=args.fine_search,
                               fixed_transform=args.transform)
        grid, dist, transform = choice['grid'], choice['dist'], choice['transform']
        hist = counts(grid.flatten().tolist())
        sigma = choice['sigma']
        for r in tried[:5]:
            t = r['transform']
            print(f'  候选：缩放 {t["scale"]:.3f} 平移 ({t["dx"]:+d},{t["dy"]:+d}) {"翻转 " if t["mirror"] else ""}模式 {t["mode"]}'
                  f' → 改动 {r["changes"]} 格，前排差异 {r["mix"]:.2f}，理想打法{r["structure"]}')
        print(f'  共试 {len(tried)} 种摆放')
    else:
        # Quantize to every colour of the level first; the permutation decides which ones the board needs.
        grid, dist = quantize(img, pic_colours, a, args.resample)
        hist = counts(grid.flatten().tolist())
    if transform is not None or args.stream_balance:
        pass
    elif args.auto_sigma or args.recolor:
        sigma = auto_sigma(a, hist, pic_colours)
    elif args.sigma:
        sigma = {int(x): int(y) for x, y in (p.split(':') for p in args.sigma.split(','))}
        for c in used:
            sigma.setdefault(c, c)
    else:
        sigma = {c: c for c in used}
    if not args.stream_balance:
        identity = all(k == v for k, v in sigma.items())
        mode = 'S' if identity else 'P' if set(sigma.values()) == set(used) else 'R'
        check_sigma(a, sigma, game, recolor=mode == 'R')
        base = a if identity else apply_sigma(a, sigma)
        target = counts(base['cells'])

    if args.stream_balance:
        pass
    elif transform is not None:
        final, budget_retry = tried[0]['final'], tried[0]['budgetRetry']
    else:
        final, budget_retry = refill_candidate(grid, target, dist, pic_colours,
            protection_mask(constraints), constraints.get('recolorFrom'))
    quant_cells = grid.flatten().tolist()
    cells = final.flatten().tolist()
    changed = {i for i, (q, f) in enumerate(zip(quant_cells, cells)) if q != f}

    b = json.loads(json.dumps(base))
    b['cells'] = cells
    import re
    named = re.match(r'第\d+关-(.+)-\d+$', Path(args.image).stem)
    if args.title or named:   # 2026-10-27: default to the theme in the picture's file name
        b['title'] = args.title or named.group(1)
    problems = validate(a, b, mode, None if identity else sigma, game, stream_balance=args.stream_balance)
    if problems:
        print(f'{label(game)} 校验失败：', *problems, sep='\n  ')
        return 1
    import structure
    in_a = b if identity else apply_sigma(b, invert(sigma))
    mix_pass = structure.mix_passes(structure.layers(a), structure.layers(in_a))

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
                  changedCells=len(changed), withinLimits=not over, colors=stats, transform=transform, budgetRetry=budget_retry,
                  mixPass=mix_pass, conversionPass=not over, mixGated=False,
                  streamBalance=dict(changed=stream_changed, length=len(b.get('stream') or []), place=args.stream_place) if args.stream_balance else None,
                  recolor=dict(chosen=pic_colours, pictureShare={str(c): round(v, 4) for c, v in cover.items()}) if cover else None,
                  protection=dict(constraints=constraints, subjectCropSafe=True, sourceFit=args.fit,
                                  sourceCropped=source_size[0] != source_size[1] and args.fit == 'crop',
                                  policy='small-parts-thin-tips-and-explicit-regions-v1'),
                  searchDiagnostics=tried[0].get('searchDiagnostics') if tried else None,
                  candidates=[dict(transform=r['transform'], changes=r['changes'], mix=r['mix'], structure=r['structure'],
                                   structureNote=r['structureNote'], budget=r['budget'], mixPass=r['passes'],
                                   budgetRetry=r['budgetRetry']) for r in tried],
                  converted=datetime.now().isoformat(timespec='seconds'))
    (out / f'{tag}.report.json').write_text(json.dumps(report, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')

    if not args.no_manifest:
        manifest = load_manifest()
        entry = manifest['levels'].get(res, {})
        if entry.get('status') == 'approved' and entry.get('fileSha256') != report['fileSha256']:
            print(f'注意：{label(game)} 原为 approved，内容已变，状态退回 draft')
        entry.update({k: report[k] for k in ('game', 'resource', 'mode', 'sigma', 'sourceImage', 'sourceSha256',
                                             'fileSha256', 'cellsSha256', 'changedCells', 'withinLimits')})
        if report['streamBalance']:
            entry['streamBalance'] = report['streamBalance']
        else:
            entry.pop('streamBalance', None)
        entry.pop('queueTune', None)
        entry['title'] = b.get('title', '')
        entry['draft'] = str((out / f'{res}.json').relative_to(ROOT)) if out.is_relative_to(ROOT) else str(out / f'{res}.json')
        entry['status'] = 'draft'
        manifest['levels'][res] = entry
        save_manifest(manifest)

    verdict = '通过' if not over else '补齐改动超限'
    if not mix_pass:
        verdict += '（下10/20排差异超0.20，仅记录）'
    where = '' if transform is None else (f'（缩放 {transform["scale"]:.3f}，平移 {transform["dx"]:+d},{transform["dy"]:+d}'
                                          + ('，左右翻转' if transform['mirror'] else '') + '）')
    print(f'{label(game)}  模式 {mode}{where}  补齐改动 {len(changed)} 格  {verdict}\n  → {out}')
    return 0 if not over else 2


if __name__ == '__main__':
    try:
        sys.exit(main())
    except (RefillBlocked, ValueError) as e:
        print(f'转换未完成：{e}', file=sys.stderr)
        sys.exit(1)

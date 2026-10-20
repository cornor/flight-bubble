"""Shared conversion budgets and source-coordinate protection. No level or source image writes."""
import json
from pathlib import Path

import numpy as np

from levelsb_common import CHANGED_PER_COLOR, CHANGED_TOTAL, MIN_PER_COLOR, SIDE, file_sha256


def refill_budget(grid, final, target):
    changed = grid != final
    colors = []
    for c in sorted(set(target) | set(np.unique(grid).tolist())):
        moved = int((changed & ((grid == c) | (final == c))).sum())
        limit = max(CHANGED_PER_COLOR * target.get(c, 0), MIN_PER_COLOR)
        colors.append(dict(index=c, moved=moved, limit=limit, over=moved > limit))
    total = int(changed.sum())
    return dict(changedCells=total, colors=colors,
                withinLimits=total <= CHANGED_TOTAL and not any(c['over'] for c in colors))


def candidate_key(r):
    """Every hard gate precedes aesthetic tie breakers; never prefer an invalid candidate over a valid one."""
    budget = r['budget']
    from structure import MIX_LIMIT
    violations = sum(c['over'] for c in budget['colors']) + (r['changes'] > CHANGED_TOTAL) + (not r['passes'])
    excess = (sum(max(0., c['moved'] / c['limit'] - 1) for c in budget['colors'])
              + max(0., r['changes'] / CHANGED_TOTAL - 1) + max(0., r['mixExact'] / MIX_LIMIT - 1))
    return (not (r['passes'] and budget['withinLimits']),
            violations, excess, r['changes'], r['mixExact'],
            abs(r['transform']['scale'] - 1),
            abs(r['transform']['dx']) + abs(r['transform']['dy']), r['transform']['mirror'])


def load_constraints(path, image):
    if path is None:
        return {}
    raw = Path(path).read_bytes()
    data = json.loads(raw)
    allowed = {'sourceSha256', 'protectRects', 'editableRects', 'subjectRects', 'recolorFrom', 'description'}
    if set(data) - allowed:
        raise ValueError(f'未知保护配置字段：{sorted(set(data) - allowed)}')
    if data.get('sourceSha256') != file_sha256(Path(image).read_bytes()):
        raise ValueError('保护配置的 sourceSha256 与图片不一致')
    if 'recolorFrom' in data:
        rules = data['recolorFrom']
        if not isinstance(rules, dict) or any(
                not isinstance(k, str) or not k.isdigit() or not isinstance(v, list) or not v
                or any(type(c) is not int or c < 0 for c in v) for k, v in rules.items()):
            raise ValueError('recolorFrom 必须是 {"目标颜色索引": [允许改成该色的原颜色索引]}')
    for key in ('protectRects', 'editableRects', 'subjectRects'):
        if key in data and not isinstance(data[key], list):
            raise ValueError(f'{key} 必须是矩形列表')
        if key == 'subjectRects' and key in data and not data[key]:
            raise ValueError('subjectRects 不得为空，不能关闭裁切保护')
        for rect in data.get(key, []):
            if (not isinstance(rect, list) or len(rect) != 4
                    or not all(type(v) in (int, float) and np.isfinite(v) for v in rect)
                    or not (0 <= rect[0] < rect[2] <= SIDE and 0 <= rect[1] < rect[3] <= SIDE)):
                raise ValueError(f'{key} 必须是源图 60×60 坐标中的 [左,上,右,下] 半开矩形')
    return {**data, 'path': str(Path(path).resolve()), 'sha256': file_sha256(raw)}


def protection_mask(constraints, scale=1., dx=0, dy=0, mirror=False):
    """Any sample touching protection locks a whole cell; editable cells must be entirely inside allowed areas."""
    samples = 4
    u = (np.arange(SIDE)[:, None] + (np.arange(samples) + .5) / samples).reshape(-1)
    sx = (u - SIDE / 2 - dx) / scale + SIDE / 2
    sy = (u - SIDE / 2 - dy) / scale + SIDE / 2
    if mirror:
        sx = SIDE - sx

    def covered(rects):
        mask = np.zeros((SIDE * samples, SIDE * samples), bool)
        for x0, y0, x1, y1 in rects:
            mask |= ((sy[:, None] >= y0) & (sy[:, None] < y1)
                     & (sx[None, :] >= x0) & (sx[None, :] < x1))
        return mask.reshape(SIDE, samples, SIDE, samples).transpose(0, 2, 1, 3).reshape(SIDE, SIDE, -1)

    locked = covered(constraints.get('protectRects', [])).any(-1)
    if 'editableRects' in constraints:
        locked |= ~covered(constraints['editableRects']).all(-1)
    return locked


def foreground_bounds(pick, background, constraints=None):
    """Conservative crop guard: every source pixel other than the majority border colour belongs to the picture."""
    foreground = pick != background
    if constraints and 'subjectRects' in constraints:
        # Explicit subjects permit scenery (sea/shore/clouds) to reach beyond the frame.
        u = (np.arange(pick.shape[0]) + .5) * SIDE / pick.shape[0]
        subject = np.zeros(pick.shape, bool)
        for x0, y0, x1, y1 in constraints['subjectRects']:
            subject |= ((u[:, None] >= y0) & (u[:, None] < y1)
                        & (u[None, :] >= x0) & (u[None, :] < x1))
        foreground &= subject
        if not foreground.any():
            raise ValueError('subjectRects 没有覆盖前景，不能关闭裁切保护')
    y, x = np.nonzero(foreground)
    if not len(x):
        return None
    unit = SIDE / pick.shape[0]
    return (x.min() * unit, y.min() * unit, (x.max() + 1) * unit, (y.max() + 1) * unit)


def crop_safe(bounds, scale=1., dx=0, dy=0, mirror=False):
    if bounds is None:
        return True
    x0, y0, x1, y1 = bounds
    if mirror:
        x0, x1 = SIDE - x1, SIDE - x0
    x0, x1 = (x0 - SIDE / 2) * scale + SIDE / 2 + dx, (x1 - SIDE / 2) * scale + SIDE / 2 + dx
    y0, y1 = (y0 - SIDE / 2) * scale + SIDE / 2 + dy, (y1 - SIDE / 2) * scale + SIDE / 2 + dy
    return bool(min(x0, y0) >= -1e-8 and max(x1, y1) <= SIDE + 1e-8)

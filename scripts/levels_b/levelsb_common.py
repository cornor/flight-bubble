"""Shared helpers for the B level set (docs/r1/关卡第二套-实施计划.md).

Game level numbers are what the player sees (levelIndex + 1). Levels 1-2 are the lessons;
game level N >= 3 is resource sector{N-2:02d}. Every report names both.
"""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LEVELS_A = ROOT / 'UnityProject/Assets/Flightbeat/Resources/levels'
WORK = ROOT / 'Art/levels-b/work'
MANIFEST = ROOT / 'Art/levels-b/manifest.json'
FIRST, LAST = 3, 262          # game levels covered by the B set
SIDE = 60                     # every sector board is 60 x 60
# Plan 3.2 refill limits. 18:50 calibrated on the first 12 generated pictures: at 15 % per colour (at least
# 10 cells) and 150 cells in all, the fish and fox that read fine pass and the turtle whose bow had to be smeared
# over its head fails; the first 10 % / 4 / 120 rejected every picture, including clean ones.
CHANGED_PER_COLOR = .15
CHANGED_TOTAL = 150
MIN_PER_COLOR = 10

# Names shown for each palette colour (Garden/GardenColorCatalog.Generated.cs, Name()).
NAMES = {
    0x2982F4: '小蓝', 0x42A9FF: '小蓝', 0xFF7E27: '小橙', 0xFF9D30: '小橙', 0x62E18E: '小绿',
    0xBD80FF: '小紫', 0xFFDF48: '小黄', 0xFFFFFF: '小白', 0xFF525B: '小红', 0x2D2D2D: '小黑',
    0x696CAF: '小靛', 0x46D9FF: '小冰', 0xC8C5FF: '小雾', 0x9C225E: '小玫', 0x65392D: '小棕',
    0xB672FD: '小晶', 0x92EF4D: '小荧', 0x30D183: '小碧', 0x0C7E1C: '小翠', 0xFFC5E0: '小粉',
    0xFFCAA9: '小霞', 0xC5FFE4: '小清',
}


def resource(game_level):
    if not FIRST <= game_level <= LAST:
        raise SystemExit(f'第 {game_level} 关不在 B 套范围（第 {FIRST}–{LAST} 关）')
    return f'sector{game_level - 2:02d}'


def label(game_level):
    return f'第 {game_level} 关（{resource(game_level)}）'


def file_tag(game_level):
    return f'第{game_level:03d}关_{resource(game_level)}'


def load_a(game_level):
    path = LEVELS_A / f'{resource(game_level)}.json'
    raw = path.read_bytes()
    return json.loads(raw), raw


def dumps(level, like_raw):
    """Same layout as the source files: compact, key order kept, trailing newline; titles escaped
    or raw UTF-8 exactly as the A file does it."""
    ascii_titles = b'\\u' in like_raw[:400]
    return json.dumps(level, ensure_ascii=ascii_titles, separators=(',', ':')).encode('utf-8') + b'\n'


def file_sha256(data):
    return hashlib.sha256(data).hexdigest()


def cells_sha256(cells):
    """Plan 3rd-review P2-1: decimal integers joined by ',' with no spaces, UTF-8."""
    return hashlib.sha256(','.join(str(int(c)) for c in cells).encode('utf-8')).hexdigest()


def rgb(level, index):
    p = level['palette']
    return (p[index * 3], p[index * 3 + 1], p[index * 3 + 2])


def rgb_int(level, index):
    r, g, b = rgb(level, index)
    return (r << 16) | (g << 8) | b


def name(level, index):
    return NAMES.get(rgb_int(level, index), f'#{rgb_int(level, index):06X}')


def counts(values):
    out = {}
    for v in values:
        out[v] = out.get(v, 0) + 1
    return out


def level_colors(level):
    """Colour indices used anywhere in the level (board or stream), sorted."""
    return sorted(set(level['cells']) | set(level.get('stream') or []))


def apply_sigma(level, sigma):
    """Plan 3.1 mode P: one permutation applied to cells, stream and queue[].color together.
    queue[].other holds numeric parameters (5/10/15/20), not colours, so it is left alone."""
    out = json.loads(json.dumps(level))
    m = lambda c: sigma.get(c, c)
    out['cells'] = [m(c) for c in out['cells']]
    if out.get('stream'):
        out['stream'] = [m(c) for c in out['stream']]
    for entry in out['queue']:
        entry['color'] = m(entry['color'])
    return out


def invert(sigma):
    return {v: k for k, v in sigma.items()}


def check_sigma(level, sigma):
    used = set(level_colors(level))
    if set(sigma) != set(sigma.values()):
        raise ValueError('置换必须是一一对应（每个颜色只出现一次）')
    extra = (set(sigma) | set(sigma.values())) - used
    if extra:
        raise ValueError(f'置换里有本关没有的颜色索引：{sorted(extra)}')


def validate(a, b, mode, sigma=None):
    """Plan 3.1. Returns a list of problems (empty = valid)."""
    problems = []
    if mode not in ('S', 'P'):
        return [f'未知模式 {mode}']
    sigma = sigma or {}
    if mode == 'S' and any(k != v for k, v in sigma.items()):
        problems.append('模式 S 不允许颜色置换')
    if mode == 'P':
        try:
            check_sigma(a, sigma)
        except ValueError as e:
            problems.append(str(e))
            return problems
    restored = apply_sigma(b, invert(sigma)) if mode == 'P' else b
    cells = restored.get('cells') or []
    if len(cells) != SIDE * SIDE or restored.get('columns') != SIDE or restored.get('rows') != SIDE:
        problems.append('棋盘必须是 60 × 60')
    if any((not isinstance(c, int)) or c < 0 or c >= len(a['palette']) // 3 for c in cells):
        problems.append('有非法颜色索引')
    if counts(cells) != counts(a['cells']):
        problems.append('棋盘各色格子数与 A 关不一致')
    # Field by field, except cells and title.
    keys = set(a) | set(restored)
    for k in sorted(keys - {'cells', 'title'}):
        if a.get(k) != restored.get(k):
            problems.append(f'字段 {k} 与 A 关不一致')
    offsets, stream = b.get('streamOffsets') or [], b.get('stream') or []
    if stream and (len(offsets) != SIDE + 1 or offsets[-1] != len(stream)):
        problems.append('streamOffsets 长度或末项不对')
    ammo = {}
    for entry in b['queue']:
        ammo[entry['color']] = ammo.get(entry['color'], 0) + entry['ammo']
    beads = counts(b['cells'])
    for c, n in counts(stream).items():
        beads[c] = beads.get(c, 0) + n
    if ammo != beads:
        problems.append('各色弹量守恒不成立（棋盘 + 沙流 ≠ 子弹）')
    return problems


def load_manifest():
    if MANIFEST.exists():
        return json.loads(MANIFEST.read_text(encoding='utf-8'))
    return {'version': 1, 'levels': {}}


def save_manifest(manifest):
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    manifest['levels'] = dict(sorted(manifest['levels'].items()))
    MANIFEST.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def record_check(game, check, verdict, evidence, file_sha, extra=None):
    """Store one step-4 check result on the manifest entry; the entry becomes 'evaluated' only when both the
    bot coarse screen ('bot') and the structural check ('structure') passed on the current file."""
    from datetime import datetime
    manifest = load_manifest()
    entry = manifest['levels'].get(resource(game))
    if not entry or entry.get('fileSha256') != file_sha:
        return False
    checks = entry.setdefault('checks', {})
    checks[check] = dict(verdict=verdict, fileSha256=file_sha, evidence=evidence,
                         at=datetime.now().isoformat(timespec='seconds'), **(extra or {}))
    ok = entry.get('withinLimits') is True and all(checks.get(k, {}).get('verdict') == '通过' and checks[k].get('fileSha256') == file_sha for k in ('bot', 'structure'))
    if ok and entry.get('status') == 'draft':
        entry['status'] = 'evaluated'
    if not ok and entry.get('status') == 'evaluated':
        entry['status'] = 'draft'
    save_manifest(manifest)
    return True


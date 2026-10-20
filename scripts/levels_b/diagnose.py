"""Format optional, read-only Unity battle traces. This does not change any evaluation verdict."""
from collections import Counter
from pathlib import Path


def stall(run):
    return next((s for s in (run.get('trace') or {}).get('events', []) if s['reason'] == 'first-stall-hint'), None)


def colors(values):
    return '、'.join(f'{c}×{n}' for c, n in sorted(Counter(values).items())) or '无'


def snapshot_lines(s):
    return [
        f"- {s['reason']}：{s['seconds']:.2f}s，剩余 {s['remaining']}，空位 {s['freePads']}，"
        f"游戏卡住计时 {s['blockedTime']:.2f}s，距护栏 {s['distance']:.2f}",
        '- 上阵（颜色:子弹）：' + ('、'.join(f"{f['color']}:{f['ammo']}" for f in s['fleet']) or '无'),
        '- 前排颜色（含已被子弹预订的目标）：' + colors(c for c in s['frontColors'] if c >= 0),
        '- 队首（列/颜色/子弹/暴露列数/可出战/符合机器人最后空位条件）：'
        + '；'.join(f"{h['lane']}/{h['color']}/{h['ammo']}/{h['exposedColumns']}/"
                   f"{h['deployable']}/{h['lastPadEligible']}" for h in s['heads'] if h['queued']),
    ]


def write_report(results, out):
    out = Path(out)
    grouped = {}
    for r in results:
        grouped.setdefault((r['game'], r['strategy']), {}).setdefault(r['seed'], {})[r['name'].split('-')[1]] = r
    lines = ['# 对局诊断（观察记录，不改变粗筛判定）', '',
             '颜色为关卡索引，队列列号从 0 起。首次卡住取游戏 `Stalled` 判定；'
             '暴露列数直接读取前排，适用于本评估的空装备、无穿透条件。'
             '日志不调用会改变内部状态的目标查询，不额外消耗随机数。', '',
             '完整记录见 `results.json`：开局前 20 次决策、首次卡住/复活/终局前最多 16 次决策及事件快照。'
             '`first-bot-wait-with-space` 只描述有空位但机器人未选择队员的时刻，不计入游戏卡住时间。', '',
             '| 关 | 打法 | 种子 | A 结果/首次提示 | B 结果/首次提示 |',
             '|---|---|---|---|---|']
    for (game, strategy), seeds in sorted(grouped.items()):
        for seed, pair in sorted(seeds.items()):
            def detail(side):
                r = pair[side]; s = stall(r)
                return r['outcome'] + (' / %.2fs' % s['seconds'] if s else ' / 未触发')
            lines.append(f'| {game} | {strategy} | {seed} | {detail("A")} | {detail("B")} |')
    for (game, strategy), seeds in sorted(grouped.items()):
        paired = [(seed, pair) for seed, pair in sorted(seeds.items()) if set(pair) == {'A', 'B'}]
        seed, pair = next(((seed, pair) for seed, pair in paired if pair['A']['outcome'] != pair['B']['outcome']), paired[0])
        lines += ['', f'## 第 {game} 关，打法 {strategy}，种子 {seed}', '',
                  '优先展示同种子输赢不同的一对；若无则展示第一对。完整种子见上表。']
        for side in ('A', 'B'):
            r = pair[side]; trace = r.get('trace') or {}
            lines += ['', f'### {side}：{r["outcome"]}，清除 {r["progress"]:.1%}', '']
            events = trace.get('events', [])
            for s in events:
                lines += snapshot_lines(s)
            recent = (trace.get('beforeFirstStall') or trace.get('beforeRevive') or trace.get('beforeEnd') or [])[-6:]
            if recent:
                period = '首次卡住' if trace.get('beforeFirstStall') else '复活' if trace.get('beforeRevive') else '终局'
                lines += ['', f'{period}前最后 6 次决策：', '',
                          '| 秒 | 选择列 | 犹豫 | 出战成功 | 空位 | 上阵颜色 |',
                          '|---|---|---|---|---|---|']
                lines += [f"| {s['seconds']:.2f} | {s['chosenLane']} | {s['hesitated']} | {s['deployed']} | "
                          f"{s['freePads']} | {colors(f['color'] for f in s['fleet'])} |" for s in recent]
    (out / 'diagnostics.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')

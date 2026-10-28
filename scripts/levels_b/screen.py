#!/usr/bin/env python3
"""Screen generated pictures for B levels: convert each candidate (without touching the manifest), judge it the
same way as the real pipeline, and write a report an artist can act on.

  python3 scripts/levels_b/screen.py --level 9 Art/levels-b/source/第9关-*-0[5-8].png
  python3 scripts/levels_b/screen.py --level 9 --level 15 --all-new     every picture of those levels not screened yet
  options: --out DIR (default tools/work/levels-b/screen-<time>/), --jobs 6,
           --only-s (mode S only: every colour name is the picture's own colour; use it to write edit requests)

For every candidate:
  * refill result: changed cells, within limits or not, and which colours went over (convert.py rules);
  * front mix in A's colour roles: D_1 / D_3 / D_10 / D_20 and the pass rule (structure.mix_passes, D_10 and D_20
    each <= 0.20); lowest-row shifts over 5 rows (reported only);
  * per colour, in the PICTURE's colours (mode P already applied): how many cells the picture has vs needs
    ("画面多/少 N 格", 36 cells = 1 % of the picture), and what the bottom 10 / 20 rows should hold vs hold now.
A candidate is 可用 when it is within the refill limits AND passes the front mix. Pick among 可用 ones by looking at
sheet.png (the board, and the board in A's colour roles, which is what the difficulty sees), then make it a draft
with convert.py (without --no-manifest). Nothing here writes the manifest, Resources or the source pictures.
"""
import argparse
import ast
import glob
import json
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import structure as st
from levelsb_common import ROOT, SIDE, apply_sigma, invert, label, load_a, name

CELLS_PER_PERCENT = SIDE * SIDE // 100


def rel(p):
    p = Path(p).resolve()
    return p.relative_to(ROOT) if p.is_relative_to(ROOT) else p


def short(level, c):
    return name(level, c).replace('小', '')


def convert(game, image, out, only_s=False, constraints=None, fine=False):
    run = subprocess.run([sys.executable, str(HERE / 'convert.py'), '--level', str(game), '--image', str(image),
                          '--out', str(out), '--no-manifest'] + (['--only-s'] if only_s else [])
                         + (['--constraints', str(constraints)] if constraints else [])
                         + (['--fine-search'] if fine else []), capture_output=True, text=True)
    reps = list(out.glob('*.report.json'))
    if run.returncode not in (0, 2) or not reps:
        return dict(error=(run.stdout + run.stderr).strip()[-400:])
    rep = json.loads(reps[0].read_text())
    board = json.loads(next(p for p in out.glob('sector*.json')).read_text())
    return dict(report=rep, board=board, preview=next(iter(out.glob('第*.png')), None))


def comp(layer, level):
    return ' '.join(f'{short(level, c)}{round(100 * float(v))}' for c, v in sorted(layer.items(), key=lambda kv: -kv[1]) if v >= .01)


def judge(game, image, res):
    a, _ = load_a(game)
    rep, b = res['report'], res['board']
    sigma = {int(k): v for k, v in (rep.get('sigma') or {}).items()}
    in_a = apply_sigma(b, invert(sigma)) if sigma else b
    la, lb = st.layers(a), st.layers(in_a)
    want = st.layers(apply_sigma(a, sigma)) if sigma else la   # what B should hold, in the picture's colours
    have = st.layers(b)
    colors = rep['colors'] if isinstance(rep['colors'], list) else ast.literal_eval(rep['colors'])
    usable = rep['withinLimits']   # 2026-10-26 23:31: D10/D20 reported only
    return dict(
        image=str(Path(image).resolve().relative_to(ROOT)) if Path(image).resolve().is_relative_to(ROOT) else str(image),
        game=game, mode=rep['mode'], sigma={short(a, int(k)) + '→' + short(a, v): 1 for k, v in sigma.items() if int(k) != v},
        transform=rep['transform'], changed=rep['changedCells'], within=rep['withinLimits'],
        over=[f"{c['name'].replace('小', '')} {c['moved']}" for c in colors if c['over']],
        amounts=[(c['name'].replace('小', ''), c['quantized'] - c['target']) for c in colors],
        D={k: round(float(st.layer_distance(la[k], lb[k])), 3) for k in st.LAYERS},
        mixPass=st.mix_passes(la, lb),
        shifts={short(a, c): v for c, v in st.lowest_shifts(a, in_a).items()},
        bottom={k: (comp(want[k], b), comp(have[k], b)) for k in (10, 20)},
        usable=usable, board=b, inA=in_a, a=a, preview=res.get('preview'))


def sheet(rows, path):
    from PIL import Image, ImageDraw
    from levelsb_render import board_image, font
    S, W = 4, SIDE * 4
    img = Image.new('RGB', (3 * (W + 12) + 12, len(rows) * (W + 44) + 10), (250, 250, 250))
    d = ImageDraw.Draw(img)
    for i, r in enumerate(rows):
        y = 10 + i * (W + 44)
        img.paste(board_image(r['a']['cells'], r['a'], S), (12, y + 26))
        d.text((12, y + 2), f"第{r['game']}关 原关", fill=(0, 0, 0), font=font(16))
        img.paste(board_image(r['board']['cells'], r['board'], S), (24 + W, y + 26))
        tag = Path(r['image']).stem.split('-')[-1]
        d.text((24 + W, y + 2), f"-{tag} {r['mode']} 改{r['changed']} {'可用' if r['usable'] else '不可用'}",
               fill=(0, 120, 0) if r['usable'] else (200, 0, 0), font=font(16))
        img.paste(board_image(r['inA']['cells'], r['a'], S), (36 + 2 * W, y + 26))
        d.text((36 + 2 * W, y + 2), '按原关颜色角色看', fill=(0, 0, 0), font=font(16))
    img.save(path)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--level', type=int, action='append', required=True)
    ap.add_argument('images', nargs='*')
    ap.add_argument('--all-new', action='store_true', help='every picture of the levels in Art/levels-b/source')
    ap.add_argument('--out', type=Path)
    ap.add_argument('--jobs', type=int, default=6)
    ap.add_argument('--only-s', action='store_true', help='只用模式 S：颜色名就是画面里的颜色，写修改意见时用')
    ap.add_argument('--constraints-dir', type=Path, help='按源图文件名（去掉 .png，加 .json）查找保护配置')
    ap.add_argument('--fine-search', action='store_true')
    args = ap.parse_args()
    jobs = []
    for game in args.level:
        pics = args.images or ([] if not args.all_new else sorted(glob.glob(str(ROOT / f'Art/levels-b/source/第{game}关-*.png'))))
        for p in pics:
            if Path(p).name.startswith(f'第{game}关-'):
                jobs.append((game, Path(p).resolve()))
    if not jobs:
        ap.error('没有图片：给出图片路径，或用 --all-new')
    out = (args.out or ROOT / 'tools/work/levels-b' / f'screen-{datetime.now():%Y%m%d-%H%M%S}').resolve()
    out.mkdir(parents=True, exist_ok=False)
    with ThreadPoolExecutor(args.jobs) as pool:
        def worker(j):
            config = args.constraints_dir / (j[1].stem + '.json') if args.constraints_dir else None
            return j, convert(j[0], j[1], out / 'convert' / f'{j[0]}-{j[1].stem}', args.only_s,
                              config if config and config.exists() else None, args.fine_search)
        results = list(pool.map(worker, jobs))
    rows, lines = [], [f'# B 关候选图筛查（{datetime.now():%Y-%m-%d %H:%M}）', '',
                       '可用 = 补齐不超限（10-26 起下 10/20 排差异仅记录）。「画面多/少」按画面自己的颜色说（模式 P 已换好），36 格 ≈ 画面 1%。', '']
    errors = []
    for (game, image), res in results:
        if 'error' in res:
            errors.append(dict(game=game, image=str(image), error=res['error']))
            lines += [f'## {image.name}：转换失败', '', '```', res['error'], '```', '']
            continue
        r = judge(game, image, res); rows.append(r)
        t = r['transform']
        lines += [f"## {image.name}：{'**可用**' if r['usable'] else '不可用'}", '',
                  f"- 转换：模式 {r['mode']}{('（' + '、'.join(r['sigma']) + '）') if r['sigma'] else ''}，缩放 {t['scale']}，平移 {t['dx']:+d},{t['dy']:+d}{'，左右镜像' if t['mirror'] else ''}",
                  f"- 补齐：改动 {r['changed']} 格，{'不超限' if r['within'] else '超限：' + '、'.join(r['over'])}",
                  f"- 前排差异（判 10、20）：下 1/3/10/20 排 {r['D'][1]}/{r['D'][3]}/{r['D'][10]}/{r['D'][20]}，{'通过' if r['mixPass'] else '超差'}",
                  '- 颜色多少（画面比需要）：' + ('；'.join(f"{n} {'多' if v > 0 else '少'} {abs(v)} 格" for n, v in r['amounts'] if abs(v) >= 5) or '都接近'),
                  f"- 下 10 排 应有：{r['bottom'][10][0]}｜实际：{r['bottom'][10][1]}",
                  f"- 下 20 排 应有：{r['bottom'][20][0]}｜实际：{r['bottom'][20][1]}",
                  '- 最低排偏移 >5（仅参考，原关颜色角色，A→B）：' + ('、'.join(f'{c} {x}→{y}' for c, (x, y) in r['shifts'].items()) or '无'),
                  f"- 转换预览：`{rel(res['preview']) if res['preview'] else '—'}`", '']
    sheet(rows, out / 'sheet.png')
    summary = ['| 关 | 图 | 可用 | 模式 | 改动 | D10 / D20 |', '|---|---|---|---|---|---|'] + [
        f"| {r['game']} | {Path(r['image']).name} | {'是' if r['usable'] else '否'} | {r['mode']} | {r['changed']}{'' if r['within'] else ' 超限'} | {r['D'][10]} / {r['D'][20]} |" for r in rows]
    (out / 'report.md').write_text('\n'.join(lines[:4] + summary + [''] + lines[4:]) + '\n', encoding='utf-8')
    (out / 'report.json').write_text(json.dumps([{k: v for k, v in r.items() if k not in ('board', 'inA', 'a', 'preview')} for r in rows],
                                                ensure_ascii=False, indent=1) + '\n', encoding='utf-8')
    (out / 'errors.json').write_text(json.dumps(errors, ensure_ascii=False, indent=1) + '\n', encoding='utf-8')
    for r in rows:
        print(f"{label(r['game'])} {Path(r['image']).name}：{'可用' if r['usable'] else '不可用'}  改动 {r['changed']}{'' if r['within'] else '（超限）'}  D10/D20 {r['D'][10]}/{r['D'][20]}")
    print(f'报告：{out / "report.md"}\n对照图：{out / "sheet.png"}')
    return 0


if __name__ == '__main__':
    sys.exit(main())

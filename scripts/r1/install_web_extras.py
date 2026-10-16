#!/usr/bin/env python3
"""Add a local, opt-in read-only performance HUD to a completed R1 build."""
import sys
from pathlib import Path
root=Path(__file__).resolve().parents[2]
build=Path(sys.argv[1])
art='-art-' in build.name
bubble='bubble' in build.name   # R1 v3 reskin (scripts/r1v3/build.sh)
name='泡泡小队' if bubble else '植物守卫菜园'
label='R1 试玩版' if bubble else 'R1 第一版美术' if art else 'R1 灰盒'
port=8785 if bubble else 8784 if art else 8782
p=build/'index.html'
s=p.read_text().replace('<title>Flightbeat</title>',f'<title>{name} · {label}</title>').replace('aria-label="Flightbeat 游戏画面"',f'aria-label="{name}游戏画面"').replace('<strong>Flightbeat</strong>',f'<strong>{name} · {label}</strong>')
if 'r1-perf.js' not in s:s=s.replace('</body>','<script src="r1-perf.js"></script>\n</body>')
if 'rel="icon"' not in s:s=s.replace('</head>','<link rel="icon" href="data:,">\n</head>')
p.write_text(s)
(build/'r1-perf.js').write_text((root/'scripts/r1/perf.js').read_text())

(build/'LICENSE-NotoSansSC.txt').write_text((root/'UnityProject/Assets/Flightbeat/Resources/GardenR1/OFL.txt').read_text())
(build/'打开说明.txt').write_text(f'{name} {label}。不要直接双击 index.html。\n在本目录运行 python3 -m http.server {port} --bind 0.0.0.0，然后访问 http://127.0.0.1:{port}/ 。\n同一局域网的设备用电脑的局域网 IP 替换 127.0.0.1。网址加 ?perf=1 可导出只读性能记录。\n前四关使用新包装，主页和终结技保留原版。第 5 关后仍是原版外观。\n')

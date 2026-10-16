"""用真实关卡数据合成泡泡包装的真实比例参考图（540x960 逻辑坐标，按 2 倍输出 1080x1920）。

坐标全部来自 docs/r1/界面规范-v2.md 第 2、3 节：
- 圆珠位置：EnemyPoint，x 以 270 为中心放大 1.565 倍，y 以 562 为中心放大 1.31 倍
- 防线 y=562；出战位中心 x = 42 + i*456/(格数-1)；跑道 x 12-528 每条宽 129；队列行心 725/775/825
- 正式关 60 列，横向间距 5.5*1.565=8.6，纵向间距 5.0*1.31=6.55 逻辑像素

用法：python3 render_real_scale.py <levels目录> <输出目录> sector03 sector07 sector08
"""
import json, sys, os
from PIL import Image, ImageDraw, ImageFont

S = 2  # 输出倍率
BEAD_D = 6.4  # 圆珠直径（逻辑像素）；纵向间距 6.55，再大就上下相碰

def font(size):
    for p in ["/System/Library/Fonts/PingFang.ttc", "/System/Library/Fonts/STHeiti Medium.ttc",
              "/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc",
              "/usr/share/fonts/opentype/noto/NotoSerifCJK-Bold.ttc"]:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                pass
    return ImageFont.load_default()

def ex(x): return 270 + (x - 270) * 1.565
def ey(y): return 562 + (y - 562) * 1.31

def palette(d):
    p = d["palette"]; return [tuple(p[i:i+3]) for i in range(0, len(p), 3)]

def shade(c, k): return tuple(max(0, min(255, int(v * k))) for v in c)

def render(level_path, out_path, slots_filled=5):
    d = json.load(open(level_path))
    pal = palette(d); cols, rows = d["columns"], d["rows"]
    img = Image.new("RGB", (540 * S, 960 * S), (214, 228, 238))
    dr = ImageDraw.Draw(img)
    f_small, f_mid = font(9 * S), font(12 * S)
    # 虫区（泡泡区）可见范围
    dr.rectangle([10 * S, 92 * S, 530 * S, 556 * S], fill=(196, 206, 216))
    r = BEAD_D / 2
    for i, ci in enumerate(d["cells"]):
        if ci is None or ci < 0 or ci >= len(pal):
            continue
        row, col = divmod(i, cols)
        x = ex(d["gridX"] + col * d["pitchX"]); y = ey(d["gridY"] + row * d["pitchY"])
        if y < 92 or y > 556:
            continue
        c = pal[ci]
        dr.ellipse([(x - r) * S, (y - r) * S, (x + r) * S, (y + r) * S], fill=c,
                   outline=shade(c, 0.72) if c != (255, 255, 255) else (150, 160, 172), width=1)
        dr.ellipse([(x - r * 0.55) * S, (y - r * 0.6) * S, (x - r * 0.05) * S, (y - r * 0.1) * S],
                   fill=tuple(min(255, v + 70) for v in c))
    # 防线
    dr.line([0, 562 * S, 540 * S, 562 * S], fill=(90, 170, 220), width=3 * S)
    dr.text((14 * S, 566 * S), "防线 y=562", fill=(40, 90, 130), font=f_small)
    # 出战位
    n = d["initialSlots"] if d.get("initialSlots") else 7
    lim = d.get("slotLimit", n)
    for i in range(lim):
        cx = 42 + i * 456 / (lim - 1)
        w = 70 if lim <= 7 else 61
        locked = i >= n
        dr.rounded_rectangle([(cx - w / 2) * S, 592 * S, (cx + w / 2) * S, 668 * S], radius=10 * S,
                             outline=(120, 130, 150), width=2,
                             fill=(180, 186, 196) if locked else (226, 234, 242))
        dr.text(((cx - 4) * S, 670 * S), "锁" if locked else str(i + 1), fill=(90, 100, 120), font=f_small, anchor="ma")
    # 队列：真实数据前 3 排，每个单位一个数字
    q = d["queue"]
    for lane in range(d["laneCount"]):
        left = 12 + lane * 129
        dr.rounded_rectangle([(left + 5) * S, 696 * S, (left + 124) * S, 862 * S], radius=12 * S,
                             outline=(130, 150, 170), width=2, fill=(200, 214, 228))
        units = sorted([u for u in q if u["lane"] == lane], key=lambda u: u["order"])[:3]
        for k, u in enumerate(units):
            cy = [725, 775, 825][k]; cx = left + 64.5
            c = pal[u["color"]]
            rr = 17 if k == 0 else 15
            dr.ellipse([(cx - rr) * S, (cy - rr - 4) * S, (cx + rr) * S, (cy + rr - 4) * S], fill=c,
                       outline=shade(c, 0.6), width=2)
            t = str(u["ammo"])
            dr.text((cx * S, (cy + 16) * S), t, fill=(30, 30, 40), font=f_small, anchor="mm")
    # 底部三个按钮
    for x0, x1, t in [(27, 182, "重排"), (190, 350, "优先出战"), (356, 512, "泡泡炸弹")]:
        dr.rounded_rectangle([x0 * S, 880 * S, x1 * S, 952 * S], radius=16 * S, outline=(120, 130, 150), width=2,
                             fill=(226, 234, 242))
        dr.text(((x0 + x1) / 2 * S, 916 * S), t, fill=(70, 80, 100), font=f_mid, anchor="mm")
    # 顶栏
    dr.text((80 * S, 20 * S), f"{os.path.basename(level_path)[:-5]}  {d['title']}  60 列 / 珠径 {BEAD_D}",
            fill=(60, 70, 90), font=f_mid)
    img.save(out_path)
    return d

if __name__ == "__main__":
    lv, out = sys.argv[1], sys.argv[2]
    for name in sys.argv[3:]:
        d = render(os.path.join(lv, name + ".json"), os.path.join(out, f"real-scale-{name}.png"))
        q = d["queue"]; pal = palette(d)
        print(name, d["title"], "lanes", d["laneCount"], "slots", d.get("initialSlots"), "/", d.get("slotLimit"))
        for lane in range(d["laneCount"]):
            us = sorted([u for u in q if u["lane"] == lane], key=lambda u: u["order"])[:3]
            print("  lane", lane, [("#%02X%02X%02X" % pal[u["color"]], u["ammo"]) for u in us])

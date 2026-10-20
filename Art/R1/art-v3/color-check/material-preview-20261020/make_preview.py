"""Offline material comparison. Reads game inputs; writes only beside this script."""
from pathlib import Path
import hashlib
import json
import math

import numpy as np
from PIL import Image, ImageDraw, ImageFont

OUT = Path(__file__).resolve().parent
ROOT = next(p for p in OUT.parents if (p / "UnityProject").is_dir())
RES = ROOT / "UnityProject/Assets/Flightbeat/Resources/GardenR1"
LEVEL = ROOT / "Art/levels-b/work/第004关_sector02/sector02.json"
INPUTS = [
    RES / "atlas.png", RES / "atlas.json", RES / "background.png", LEVEL,
    ROOT / "scripts/r1v3/build_atlas_v3.py",
    ROOT / "UnityProject/Assets/Flightbeat/Garden/PixelWingGame.Garden.cs",
    ROOT / "UnityProject/Assets/Flightbeat/Game/ScreenLayout.cs",
]
before = {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in INPUTS}
atlas = Image.open(RES / "atlas.png").convert("RGBA")
slots = {s["id"]: s for s in json.loads((RES / "atlas.json").read_text())["slots"]}
level = json.loads(LEVEL.read_text())
palette = [tuple(level["palette"][i:i + 3]) for i in range(0, len(level["palette"]), 3)]
font_path = str(ROOT / "Art/fonts/NotoSansSC.ttf")


def font(size):
    f = ImageFont.truetype(font_path, size)
    try:
        axes = f.get_variation_axes()
        f.set_variation_by_axes([500 if a["name"] == b"Weight" else a["default"] for a in axes])
    except OSError:
        pass
    return f


def extract(name):
    s = slots["bugs/" + name]
    return atlas.crop((s["x"], s["y"], s["x"] + s["w"], s["y"] + s["h"]))


current_main = extract("bead-main")
current_fixed = {
    "normal": extract("bead-fixed"),
    "white": extract("bead-white-fixed"),
    "dark": extract("bead-dark-fixed"),
}


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def to_image(rgb, alpha):
    rgba = np.concatenate([rgb, alpha[..., None]], axis=-1)
    return Image.fromarray(np.uint8(np.clip(rgba, 0, 1) * 255 + .5))


def proposed_layers(px=48, flavor="normal"):
    """Same two-layer design: tintable sphere plus fixed light/edge overlay."""
    n = px * 8
    yy, xx = np.mgrid[:n, :n]
    x = ((xx + .5) / n - .5) / .47
    y = ((yy + .5) / n - .5) / .47
    rho = np.sqrt(x*x + y*y)
    z = np.sqrt(np.maximum(0, 1 - rho*rho))
    alpha = 1 - smooth(.995, 1.005, rho)
    light = np.array([-.43, -.52, .74])
    light /= np.linalg.norm(light)
    diffuse = np.clip(light[0]*x + light[1]*y + light[2]*z, 0, 1)
    value = .66 + .34 * diffuse
    edge = smooth(.87, .995, rho)
    value *= 1 - edge * (.13 + .07 * np.clip((x+y)*.5, 0, 1))
    # A restrained transmitted-color lift at the bottom, inside the silhouette.
    value += .075 * np.exp(-((x-.10)/.56)**2 - ((y-.57)/.25)**2)
    main = to_image(np.repeat(np.clip(value, 0, 1)[..., None], 3, axis=2), alpha)

    # Broad soft light plus a smaller, oblique oval highlight.
    dx, dy = x + .31, y + .43
    u, v = .866*dx - .5*dy, .5*dx + .866*dy
    soft = .19 * np.exp(-((u/.43)**2 + (v/.29)**2) * 1.8)
    core = .88 * (1 - smooth(.65, 1.15, (u/.19)**2 + (v/.115)**2))
    # Short low-intensity inner reflection, not a complete ring.
    reflection = .11 * np.exp(-((rho-.80)/.065)**2) * np.exp(-((x-.38)/.40)**2 - ((y-.66)/.28)**2)
    fa = (1 - (1-soft)*(1-core)*(1-reflection)) * alpha
    frgb = np.ones((n, n, 3))
    if flavor in ("white", "dark"):
        rim_color = np.array((140, 150, 162) if flavor == "white" else (150, 154, 160)) / 255
        rim_alpha = edge * alpha * (.80 if flavor == "white" else .48)
        # Compose rim behind the existing specular overlay, retaining one edge.
        total = fa + rim_alpha*(1-fa)
        frgb = (frgb*fa[..., None] + rim_color*rim_alpha[..., None]*(1-fa[..., None])) / np.maximum(total[..., None], 1e-8)
        fa = total
    fixed = to_image(frgb, fa)
    resize = lambda im: im.convert("RGBa").resize((px, px), Image.Resampling.LANCZOS).convert("RGBA")
    return resize(main), resize(fixed)


proposed = {k: proposed_layers(flavor=k) for k in ("normal", "white", "dark")}
proposed_small = {
    k: tuple(im.convert("RGBa").resize((12, 12), Image.Resampling.LANCZOS).convert("RGBA") for im in pair)
    for k, pair in proposed.items()
}
current_main.save(OUT / "A-main-11.png")
current_fixed["normal"].save(OUT / "A-highlight-11.png")
proposed["normal"][0].save(OUT / "B-main-48.png")
for k, pair in proposed.items():
    pair[1].save(OUT / f"B-highlight-{k}-48.png")
proposed_small["normal"][0].save(OUT / "B-main-small-12.png")
for k, pair in proposed_small.items():
    pair[1].save(OUT / f"B-highlight-{k}-small-12.png")


def layers(version, color, small=False):
    kind = "white" if color == (255, 255, 255) else "dark" if color == (45, 45, 45) else "normal"
    main, fixed = (current_main, current_fixed[kind]) if version == "A" else (proposed_small if small else proposed)[kind]
    a = np.array(main).astype(np.float32)
    a[..., :3] *= np.array(color) / 255
    return Image.fromarray(np.uint8(np.clip(a, 0, 255))), fixed


def stamp(dest, layer, x, y, w, h):
    """Bilinear texture sampling at pixel centers, including fractional positions."""
    x0, y0 = math.floor(x), math.floor(y)
    x1, y1 = math.ceil(x+w), math.ceil(y+h)
    lw, lh = layer.size
    patch = layer.transform(
        (x1-x0, y1-y0), Image.Transform.AFFINE,
        (lw/w, 0, (x0-x)*lw/w, 0, lh/h, (y0-y)*lh/h),
        Image.Resampling.BILINEAR,
    )
    dest.alpha_composite(patch, (x0, y0))


def bead(dest, version, color, x, y, w, h):
    for layer in layers(version, color, small=w < 20):
        stamp(dest, layer, x-w/2, y-h/2, w, h)


def field(version):
    # 540 logical pixels, 60 columns. Formation height uses the current pitch
    # and overlap formula. Fixed top offset only, to show the complete picture.
    bg = Image.open(RES / "background.png").convert("RGBA").resize((540, 960), Image.Resampling.LANCZOS)
    im = bg.crop((0, 128, 540, 548))
    pitch_x, pitch_y = level["pitchX"]*1.56497705, level["pitchY"]*1.31
    bw = pitch_x*1.05/.94
    bh = min(bw, pitch_y*1.3/.94)
    count = 0
    for i, ci in enumerate(level["cells"]):
        if ci is None or ci < 0:
            continue
        row, col = divmod(i, level["columns"])
        x = 270 + (level["gridX"] + col*level["pitchX"] - 270)*1.56497705
        y = 13 + row*pitch_y
        bead(im, version, palette[ci], x, y, bw, bh)
        count += 1
    return im.convert("RGB"), count, (pitch_x, pitch_y, bw, bh)


fields = {}
for version in ("A", "B"):
    fields[version], count, geometry = field(version)
    fields[version].save(OUT / f"{version}-field-540.png")

W, H = 1160, 1170
sheet = Image.new("RGB", (W, H), "#F3F6FB")
d = ImageDraw.Draw(sheet)
ink, muted = "#1D3150", "#64748B"
d.text((28, 22), "泡泡质感 · 当前效果 / 增强立体感", font=font(29), fill=ink)
d.text((28, 66), "同一关卡、同一配色、同一排列间距。先看单颗，再看整幅图案。", font=font(16), fill=muted)
colors = [(41,130,244), (255,223,72), (255,126,39), (12,126,28), (255,255,255), (45,45,45)]
names = ["蓝色", "黄色", "橙色", "深绿", "白色", "炭黑"]
for v, left in (("A", 28), ("B", 592)):
    d.rounded_rectangle((left, 102, left+540, 365), radius=14, fill="white")
    d.text((left+18, 115), "A  当前贴图" if v == "A" else "B  增强立体感", font=font(23), fill=ink)
    d.text((left+18, 150), "现有 11×11 图集，双线性放大" if v == "A" else "48×48 设计纹理；下方实尺寸采用 12×12 预滤波版", font=font(13), fill=muted)
    panel = Image.new("RGBA", (540, 185), "#E8F0F9")
    pd = ImageDraw.Draw(panel)
    for j, (c, name) in enumerate(zip(colors, names)):
        cx = 45 + j*90
        bead(panel, v, c, cx, 58, 72, 72)
        pd.text((cx, 104), name, font=font(14), fill=ink, anchor="mm")
        pd.text((cx, 126), "#%02X%02X%02X" % c, font=font(10), fill=muted, anchor="mm")
        bead(panel, v, c, cx, 155, geometry[2], geometry[3])
    sheet.paste(panel.convert("RGB"), (left, 175))

d.text((28, 389), "真实排列 · 540 逻辑像素宽 / 60 列", font=font(23), fill=ink)
d.text((28, 425), "完整泡泡阵按 1:1 像素放置；聊天缩略图可能缩放，请打开原图查看。", font=font(14), fill=muted)
for v, left in (("A", 28), ("B", 592)):
    sheet.paste(fields[v], (left, 456))
    d.text((left+8, 884), "A  当前效果" if v == "A" else "B  增强立体感", font=font(17), fill=ink)

d.text((28, 930), "同一局部 · 3 倍放大", font=font(21), fill=ink)
crop_box = (178, 82, 358, 134)
for v, left in (("A", 28), ("B", 592)):
    detail = fields[v].crop(crop_box).resize((540,156), Image.Resampling.NEAREST)
    sheet.paste(detail, (left, 970))
    detail.save(OUT / f"{v}-detail-3x.png")
d.text((28, 1140), "离线材质预览 · 非游戏实机截图 · 当前图集与游戏资源未替换", font=font(13), fill=muted)
sheet.save(OUT / "comparison.png")

# Separate enlarged row for convenient inspection in the chat.
sheet.crop((0, 96, W, 371)).save(OUT / "single-bubbles.png")

after = {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in INPUTS}
assert before == after, "Read-only inputs unexpectedly changed"
record = {
    "preview_only": True,
    "date": "2026-10-20",
    "method": "Procedural offline comparison; no image generation, no game build",
    "baseline": "Actual bead layers cropped from current GardenR1 atlas",
    "candidate": "48px design master; 12px prefiltered two-layer sprite for the 9px game quads. Small specular oval and short faint inner reflection.",
    "candidate_large_preview": "48px master sampled for 72px enlarged illustration",
    "candidate_actual_size_preview": "12px prefiltered layers sampled with same bilinear method as baseline; intended as a separate small-sprite atlas entry",
    "level": str(LEVEL.relative_to(ROOT)),
    "title": level["title"], "cells_drawn_each": count,
    "pitch_x": geometry[0], "pitch_y": geometry[1],
    "quad_width": geometry[2], "quad_height": geometry[3],
    "formation_top": 13, "screen_layout_stretch": 1,
    "sampling": "Pillow bilinear approximation; not Unity GPU validation",
    "input_sha256": before, "inputs_unchanged": before == after,
    "outputs": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.glob("*.png")},
}
(OUT / "preview-record.json").write_text(json.dumps(record, ensure_ascii=False, indent=2))
(OUT / "index.html").write_text("""<!doctype html><html lang="zh-CN"><meta charset="utf-8">
<title>泡泡质感对比</title><meta name="viewport" content="width=device-width,initial-scale=1">
<style>body{margin:0;background:#f3f6fb;color:#1d3150;font:16px system-ui}
main{padding:24px;margin:auto;max-width:1160px}h1{font-size:26px}p{color:#64748b;line-height:1.7}
.scroll{overflow-x:auto}img{display:block}.pair{display:flex;gap:24px;width:1104px}
figure{margin:0}figcaption{font-weight:600;padding:16px 0}
a{color:#2763c3}.full{width:1160px;height:auto}</style>
<main><h1>泡泡质感：当前 / 增强立体感</h1>
<p>离线程序绘制预览。原尺寸区域保持 540 像素宽，窄屏可横向滚动。两边采用相同关卡数据、色值、间距和叠压顺序。</p>
<p><a href="comparison.png">打开完整对比图</a> · <a href="preview-record.json">预览来源与记录</a></p>
<div class="scroll"><img class="full" src="single-bubbles.png" alt="单颗泡泡对比"></div>
<h2>真实大小的泡泡阵</h2><div class="scroll"><div class="pair">
<figure><figcaption>A 当前贴图</figcaption><img width="540" height="420" src="A-field-540.png"></figure>
<figure><figcaption>B 增强立体感</figcaption><img width="540" height="420" src="B-field-540.png"></figure>
</div></div><h2>同一区域，放大 3 倍</h2><div class="scroll"><div class="pair">
<img width="540" height="156" src="A-detail-3x.png"><img width="540" height="156" src="B-detail-3x.png">
</div></div><p>这不是实机截图；预览使用双线性采样近似展示贴图效果。尚未替换游戏资源。</p></main></html>""")
print(json.dumps({"output": str(OUT / "comparison.png"), "cells": count, "geometry": geometry, "inputs_unchanged": before == after}, ensure_ascii=False))

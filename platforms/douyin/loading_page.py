"""把泡泡小队的加载页套到抖音导出的 tt-minigame 上。

抖音版不用 WebGL 模板（UnityProject/Assets/WebGLTemplates/Flightbeat/index.html）的网页加载页，
而是 TTSDK 自带的加载页（粉色背景 + Unity 图标）。这里换成和 WebGL 加载页一样的画面：
气泡背景 + 「泡泡小队」字标 + 进度条的紫色底框 + 健康游戏忠告（一张图，loading/loading-background.jpg），
加上绿色进度和状态文字（TTSDK loadingPageConfig，按背景图的缩放对准底框）。TTSDK 加载页只能放静态背景、进度条和一行文字，
字标的浮动动画做不了。

    python3 platforms/douyin/loading_page.py Builds/DouyinPreview/tt-minigame

preview.command 导出后会自动执行；用 TTSDK 面板正式导出后，也对导出的 tt-minigame 目录执行一次。可重复执行。
"""
import shutil
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
IMAGE = "images/flightbeat-loading.jpg"
MARK = "// 泡泡小队加载页"
ANCHOR = "const gameManager = new UnityManager("
# The background (720 x 1560) is laid over the screen like CSS "cover"; the bar's frame and groove are painted in it
# (groove x 92-628, y 1204-1252), so TTSDK's bar is placed on the groove with the same mapping. TTSDK draws only the
# fill in barConfig backgroundColor and a faint white lane, which could not be seen on the light page (2026-11-08 22:06).
PATCH = MARK + """（platforms/douyin/loading_page.py，与 WebGL 加载页一致）：背景图里已有字标、进度条底框和健康游戏忠告。
  (function (c, w, h) {
    var k = Math.max(w / 720, h / 1560), ox = (w - 720 * k) / 2, oy = (h - 1560 * k) / 2;
    c.designWidth = 0; c.designHeight = 0;
    c.materialConfig.backgroundImage = '""" + IMAGE + """';
    c.iconConfig.visible = false;
    var t = c.textConfig;
    t.firstStartText = '首次加载请耐心等待'; t.downloadingText = ['加载中…'];
    t.compilingText = '准备中…'; t.initText = '准备中…'; t.completeText = '加载完成，出发！';
    var barH = Math.round(48 * k), barB = Math.round(h - (oy + 1252 * k));
    c.barConfig.style = Object.assign({}, c.barConfig.style, { width: Math.round(536 * k), height: barH,
      padding: Math.max(2, Math.round(4 * k)), bottom: barB, backgroundColor: '#2fc23e' });
    t.style = Object.assign({}, t.style, { width: Math.round(w * .8), height: Math.round(w * .05),
      bottom: Math.round(h - (oy + 1196 * k)) + Math.round(w * .02), fontSize: Math.round(w * .04), color: '#3f248a' });
  })(managerConfig.loadingPageConfig, info.screenWidth, info.screenHeight);

  """


def apply(game):
    game = Path(game)
    script = game / "game.js"
    text = script.read_text(encoding="utf-8")
    if text.count(ANCHOR) != 1:
        raise ValueError(f"{script} 里找不到 TTSDK 的 UnityManager 创建语句，加载页没有替换")
    if MARK in text:   # an earlier version of the patch: replace it
        text = text[:text.index(MARK)] + text[text.index(ANCHOR):]
    script.write_text(text.replace(ANCHOR, PATCH + ANCHOR), encoding="utf-8")
    (game / "images").mkdir(exist_ok=True)
    shutil.copyfile(HERE / "loading" / "loading-background.jpg", game / IMAGE)


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    try:
        apply(sys.argv[1])
    except (OSError, ValueError) as error:
        sys.exit(f"加载页替换失败：{error}")
    print("已替换为泡泡小队加载页。")

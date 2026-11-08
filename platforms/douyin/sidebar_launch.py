"""侧边栏复访：在导出的 tt-minigame/game.js 里，引擎加载前就开始监听 tt.onShow。

抖音要求游戏一启动就注册 onShow，并按最近一次进入判断玩家是否从首页侧边栏进来（scene 为 021036，
或 launch_from 为 homepage 且 location 为 sidebar_card）；监听太晚会导致领奖失败。
结果放在 GameGlobal.flightbeatSidebar.fromSidebar，游戏里由 Plugins/WebGL/FlightbeatSidebar.jslib 读取
（PixelWingGame.Sidebar：主页「侧边栏」按钮、每日奖励弹窗）。没打这个补丁时，jslib 会在第一次调用时自己开始监听。

    python3 platforms/douyin/sidebar_launch.py Builds/DouyinPreview/tt-minigame

preview.command 导出后会自动执行；用 TTSDK 面板正式导出后，也对导出的 tt-minigame 目录执行一次。可重复执行。
"""
import sys
from pathlib import Path

MARK = "// 泡泡小队侧边栏复访"
ANCHOR = "function main() {"
PATCH = MARK + """（platforms/douyin/sidebar_launch.py）：启动时就监听 onShow，按最近一次进入判断是否来自首页侧边栏。
(function () {
  var s = GameGlobal.flightbeatSidebar || (GameGlobal.flightbeatSidebar = { fromSidebar: false });
  s.listening = true;
  function mark(o) {
    o = o || {};
    s.fromSidebar = o.scene === '021036' || (o.launch_from === 'homepage' && o.location === 'sidebar_card');
  }
  try { mark(tt.getLaunchOptionsSync()); } catch (e) {}
  try { tt.onShow(mark); } catch (e) {}
})();

"""


def apply(game):
    script = Path(game) / "game.js"
    text = script.read_text(encoding="utf-8")
    if MARK in text:
        return
    if text.count(ANCHOR) != 1:
        raise ValueError(f"{script} 里找不到 main 函数，侧边栏监听没有加上")
    script.write_text(text.replace(ANCHOR, PATCH + ANCHOR), encoding="utf-8")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    try:
        apply(sys.argv[1])
    except (OSError, ValueError) as error:
        sys.exit(f"侧边栏监听添加失败：{error}")
    print("已加上侧边栏复访的启动监听。")

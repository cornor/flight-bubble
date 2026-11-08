# 泡泡小队 · 抖音小游戏激励视频

抖音版本沿用游戏的 Unity 2022.3.62f3 和 WebGL 方案。卡槽、道具、复活、体力和跳关共用一个激励视频广告位。每次观看只对应玩家刚刚点击的奖励，完整观看后发放一次。玩家提前关闭、无广告填充或请求超时，均可回到游戏重试。

## 准备工程

本机已准备好 `Builds/DouyinUnity`。在另一份工作区或需要同步最新游戏源码时，在仓库根目录运行下面的命令，输出路径需尚未存在。也可以通过 `--ad-unit-id` 传入正式广告位 ID。

```sh
python3 platforms/douyin/prepare.py --output Builds/DouyinUnity
```

脚本复制游戏工程并仅在副本应用广告接入。官方 BGDT CP 安装工具固定在 3.0.271，下载后校验 SHA-256；离线准备时可用 `--bgdt` 指定同一份官方 unitypackage。游戏运行时使用 TTSDK，最低允许 6.9.6（2026-11-02 发布）。[官方安装说明](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/game-engine/rd-to-SCgame/unity-game-access/install-connect/install-sdk) · [TTSDK 发版记录](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/game-engine/rd-to-SCgame/unity-game-access/update-record/sc_changelog)

## 日常一键同步与导出

首次配置完成后，关闭占用 `DouyinUnity` 工程的 Unity 编辑器，在 Finder 双击同目录的 `preview.command`，或在仓库根目录执行：

```sh
./platforms/douyin/preview.command
```

命令从主工程 `UnityProject` 同步整个 `Assets`、`Packages` 和 `ProjectSettings`，包含未提交的新增、改名与删除；保留专属 SDK、小游戏配置和广告位，并重新应用抖音适配。美术原稿需先按游戏原有流程导入 Unity；关卡沿用常规 WebGL 的正式资源。导出成功后，在抖音开发者工具已有的 **FlightBubble** 窗口点「编译」。日志在 `Builds/DouyinPreview.build.log`。

只想更新抖音 Unity 工程时，加 `--sync-only`。同步时发现抖音适配补丁冲突，命令会在临时目录报错，便于根据新的主工程代码调整适配。

## 加载页

抖音版不走 WebGL 模板里的网页加载页，用的是 TTSDK 自带的加载页（默认是粉色背景加 Unity 图标）。`loading_page.py` 把它换成和 WebGL 加载页一样的画面：气泡背景、「泡泡小队」字标和健康游戏忠告合成一张背景图（`loading/loading-background.jpg`），再配紫色进度条和状态文字。TTSDK 加载页只支持静态背景、进度条和一行文字，字标的浮动动画做不了。

`preview.command` 导出后会自动执行。用 TTSDK 面板正式导出后，也要对导出的 `tt-minigame` 目录执行一次（可重复执行）：

```sh
python3 platforms/douyin/loading_page.py <导出目录>/tt-minigame
```

换 WebGL 加载页的背景或字标后，需要重新合成 `loading/loading-background.jpg`。

## 侧边栏复访（必接审核项）

抖音只在支持侧边栏的客户端上，在主页「伙伴图鉴」下方显示「侧边栏」按钮（`tt.checkScene`）。点开是每日奖励弹窗：

- 不是从侧边栏进来的：显示步骤和「去首页侧边栏」按钮，点了会关掉弹窗、跳转到抖音首页侧边栏（`tt.navigateToScene`）。
- 从侧边栏进来、今天还没领：按钮带红点，回到主页时弹窗自动打开，点「立即领奖」得到重排、优先出战、泡泡炸弹各 1 个。
- 今天已领：提示明天再来。领取日期存在 PlayerPrefs，重置关卡进度不会重复发放。

是否从侧边栏进入按最近一次 `tt.onShow` 判断（scene 为 `021036`，或 `launch_from` 为 `homepage` 且 `location` 为 `sidebar_card`）。平台要求一启动就监听，所以 `sidebar_launch.py` 会在导出的 `game.js` 里引擎加载前加上监听；`preview.command` 导出后自动执行，正式导出后也要执行一次：

```sh
python3 platforms/douyin/sidebar_launch.py <导出目录>/tt-minigame
```

提审前在测试环境走一遍：从侧边栏进入 → 主页弹窗自动打开 → 领奖 → 道具到账。浏览器里可用 `?sidebar=1`（从侧边栏进入）或 `?sidebar=0`（支持但不是从侧边栏进入）模拟。

## 配置真实 SDK 与广告位

1. 用 Unity 2022.3.62f3 打开生成的抖音工程，在 Build Settings 选择 WebGL 并切换平台。打开工程内的 BGDT，使用开发者账号登录，确认右上角显示 `cp`，在 TTSDK 中选择 6.9.6 或更新的正式版安装。
2. 在抖音小游戏后台为这个 Unity 小游戏开通「商业化 → 流量主」，创建**激励视频**广告位。把广告位 ID 单独写入工程的 `Assets/Resources/DouyinRewardedAd.txt`。[广告位开通与 API](https://developer.open-douyin.com/docs/resource/zh-CN/mini-game/develop/api/c-api/ads/rewardedvideoad)
3. 在 TTSDK 官方导出面板选择 WebGL、竖屏和新包体格式（关闭 IsOld Build Format），保存配置。导出配置位于 `Assets/Editor/StarkBuilderSetting.asset`。[导出配置字段](https://partner.open-douyin.com/docs/resource/zh-CN/mini-game/develop/guide/game-engine/rd-to-SCgame/unity-game-access/packaging-release/starkbuildersetting-asset-doc)
4. 执行 Unity 菜单「泡泡小队 → 抖音 → 安装 TTSDK 后启用广告」。它会核对官方包名和版本，并把泡泡小队的 AppID `tt6b371d8af54872c407` 写入官方配置。等待脚本重新编译后可在 TTSDK 面板导出。构建时会核对 AppID、广告位 ID、WebGL 和新包体格式。

## 验证

- 已导出的本地试玩工程在 `Builds/DouyinPreview/tt-minigame`。在抖音开发者工具选择「小游戏 → 导入」，选中包含 `project.config.json` 的这一层目录，点击「编译」即可回到游戏。批处理试玩导出参数 `-flightbeatDouyinLocalPreview` 允许广告位暂空，此时点广告入口会返回失败。常规导出仍要求正式广告位 ID。
- 离线回调检查在 `tests/AdCallbacks.cs`，使用模拟平台对象和实际广告适配代码。它们覆盖重复点击、完整观看、提前关闭、无填充、异常、取消、超时、重试和过期回调；测试文件只供离线运行。
- 真机联调时，先在锁定卡槽上完整看完一次，确认只解锁刚选的卡槽；再从道具或复活入口提前关闭，确认奖励未发、游戏恢复且入口可以重试。最后验证广告加载失败、切后台返回以及连续请求两个不同奖励的情况。

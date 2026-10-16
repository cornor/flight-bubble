# 战斗 UI Prefab 维护说明

版本 1.0 · 2026-10-14 · 适用于当前 `reskin/r1-v2` 前四关。

## 改界面从哪里进

Unity 的 Project 面板打开 `Assets/Flightbeat/Resources/GardenR1/UI/`，双击 `GardenBattlePage.prefab` 可以看组合结构。五个区域是独立的嵌套 Prefab：

| Prefab | 可编辑内容 |
| --- | --- |
| `GardenBattleBackground` | 背景图片、底色；按顶部、战场、底部三段适配长屏 |
| `GardenBattleHeader` | 暂停按钮、关卡牌、标题、进度条与百分比 |
| `GardenBattleControls` | 三个道具按钮、数量角标、取消选择提示、教学底部提示 |
| `GardenPauseDialog` | 暂停面板、音乐/音效/光效开关、继续、重试、返回主页 |
| `GardenResultDialog` | 胜败结算、奖励、统计、下一关/重试、返回主页、教学完成提示 |

选中具体子节点，用 Rect 工具或 Inspector 的 `RectTransform` 改位置、尺寸、锚点；图片用标准 `Image` 的 Source Image 和 Color 调整。编辑完保存 Prefab，打开 `Assets/Flightbeat/Scenes/PixelWing.unity` 后 Play 即可使用。运行时自动加载，不必手工往场景再放一份。

组合 Prefab 默认隐藏暂停与结算；直接打开对应的子 Prefab 编辑面板。`Settings`/`Paused` 和 `Mission result`/`Lesson complete` 是不同显示状态，编辑时可切换显隐预览；游戏运行时按状态控制显隐。

## 文字、按钮和动态数据

- 文字用 `GardenUiText`，沿用当前 Noto Sans SC 字形图集。可编辑 Text、Alternate Text、字号、颜色、描边、居中方式和 RectTransform。它是独立 UI 节点，可以拖动。
- `{0}`、`{1}` 是动态占位符，例如 `第 {0} 关`、`最高连击 {0}  用时 {1} 秒`。可改周围文案，保留需要的数据占位符；编辑状态显示模板，游戏里填入实际数据。
- Alternate Text 用于同一控件的另一状态，例如胜利/失败、开/关。奖励数字、库存、进度和可见状态由 `GardenBattlePage` 与 `PixelWingGame.GardenUi` 绑定现有游戏状态。
- 开关的两张状态图、结算的两张头像在主 Prefab 根节点 `GardenBattlePage` 的 Toggle On/Off、Happy/Worried Portrait 字段中替换；运行时会根据状态选图。进度条的 Fill Amount 由游戏进度驱动。
- 三个道具按钮的固定名称仍在现有图片里，换名称需要换按钮图；数量角标与取消提示是独立文字。
- 带 `GardenUiAction` 的节点就是点击区域。移动、缩放这个父节点，图片与点击区域一起变化；只移动其图片子节点会只改外观。Action Id 对应原有操作，不要为排版修改它。
- 弹窗的 `Modal input shield` 是遮罩和输入阻挡层，保留它才能防止点穿。按钮仍由原游戏输入流程处理，不再叠加一套 Button.onClick。
- `RefreshGardenUi` 每帧运行。绑定文字请用 `Present(bool)`、`Present(bool,int)`、`Present(bool,int,int)`、`Present(bool,string)` 这几个重载：数值没变就直接返回，不装箱、不格式化，稳定帧零分配。`params object[]` 版本只留给少用的场合。

## 自动适配与绘制分工

程序只调整 `Top safe band`、`Bottom safe band` 和背景三段这些适配容器；请编辑容器内部的具体控件。每帧绑定数据不会重置具体控件的位置、尺寸或字号；选中道具会在图片自身的位置上叠加 Inspector 配置的按下偏移。

背景在战斗绘制下方，固定控件在战斗 HUD 上方；暂停、结算再往上；复活、连胜确认、体力、过场保留原有高层覆盖关系。组合 Prefab 中的 Canvas 排序承担这个顺序。

虫群、植物、弹体、篱笆、种植格、队列、教学指引、终结技及其余专项覆盖层仍由现有批量绘制处理。这次没有把每个战斗对象改成 GameObject，也没有修改战斗、经济、教学或关卡数据。

## 换素材与增加新字

`Assets/Flightbeat/Garden/UI/Sprites/` 是从现有图集提取的 Sprite 资源，引用表在同目录上一层的 `sprite-catalog.json`。也可以给 Image 指定自己导入的 Sprite。

图集重打包时，导入钩子与 WebGL 构建会刷新派生 Sprite 的裁切数据，保留 GUID 和 Prefab 布局。创建工具只供首次生成使用，已有主 Prefab 时会拒绝覆盖。日常维护直接编辑 Prefab。

目前字库按项目用字打包。已有字符可直接用；新增字库以外的汉字，需要先用 `scripts/r1/build_atlas.py <NotoSansSC字体路径>` 更新字形，再运行 `scripts/r1v2/build_atlas_v2.py` 恢复 v2 图集。两个脚本都读取 Prefab 文案。WebGL 构建会检查缺字并明确报错，避免发布后文字缺失。

## 本次验证

- Unity 2022.3.62f3 命令行编译和实际 Play 模式检查通过：52 项。
- 验证了 540×960、540×1170；移动暂停按钮后旧位置不再命中，新位置可用；暂停继续、开关、道具选择与取消、单次扣除、失败重试、胜利下一关、回主页均通过。
- 检查了教学、正式关、暂停、喷壶选取、胜败结算截图。失败/胜利使用显式 UI 状态夹具验证展示与跳转，不把它们记作通关回放。
- 前四关及相关道具、复活的 9 条冻结回放通过：9,093 帧、63,651 次数组哈希核对零差异；基础规则检查 64,541 项通过；战斗、经济、关卡与教学源文件未改动。原完整代表集提前结束，不记为全量通过。
- 顺带修正保留绘制的九宫格弹窗和教学气泡在长屏上的接缝：整体按现有 ScreenLayout 映射，再切分面板；不改变按钮和游戏逻辑。
- 测试使用独立临时经济档案，实际 `pw.fleet.*` 存档不写入；音效偏好在测试前备份、结束后恢复。
- 复查（2026-10-14，未提交状态）：完整回归 `v2-prefab` 39 条路线全过、基础规则 64,541 项通过，`check_source` 逻辑与数据零改动；构建 `Builds/WebGL/GardenR1-prefab-p1` 在 540×960 无头浏览器真实打通前四关（含暂停、优先栽种金角、教学发光、真实结算数据），540×1170 与 900×960 宽屏截图正常；已发布到 garden-r1-v2，旧版在 garden-r1-v2.prev。
- 证据：`Builds/UI-prefab-evidence/play-final/report.json`、同目录 11 张 PNG；逻辑证据存于 `../Flightbeat-archive/replication/Tools/work/garden-ui-scoped-20261014-model/combined.json`。

## 本地 WebGL 包

本次包位于 `Builds/WebGL/GardenR1-ui-prefabs-final/`，使用现有 9:16 包装和 DPR 上限 1.5，没有平台对接变化。从仓库根目录运行：

```sh
python3 -m http.server 8794 --bind 127.0.0.1 --directory Builds/WebGL/GardenR1-ui-prefabs-final
```

然后访问 `http://127.0.0.1:8794/`。编辑 Prefab 后需要重新构建才能更新浏览器里的游戏；Unity Play 会直接加载保存后的资源。日常 v2 构建入口仍是 `scripts/r1v2/build.sh /全新输出目录`。

Unity 命令行构建成功，0 错误；2 条既有编译警告来自 `CombatFeedback.launchAge` 未使用。最终包在浏览器实测了 540×960 与 540×1170，教学气泡、顶栏、底栏和暂停布局正常，暂停/继续与种植操作有效。控制台无错误，保留已有的 IndexedDB 手动同步弃用提示。截图与控制台记录见 `Builds/UI-prefab-evidence/webgl-final-*`。这次没有重复做四关浏览器通关或性能全量验收。

## 命令行复查

命令行 UI 检查（输出目录必须全新）：

```sh
/Applications/Unity/Hub/Editor/2022.3.62f3/Unity.app/Contents/MacOS/Unity \
  -batchmode -projectPath "$PWD/UnityProject" \
  -executeMethod Flightbeat.Editor.GardenBattleUiChecks.Run \
  --idle-test --garden-ui-checks "$PWD/Builds/UI-prefab-evidence/新批次" \
  -logFile "$PWD/Builds/UI-prefab-evidence/新批次.log"
```

此检查自行进入、退出 Play 模式并结束 Unity，不加 `-quit` 或 `-nographics`。

本次固定 UI 改动的逻辑抽样使用 `bash scripts/r1/regress.sh 新批次 battle-ui`：前四关和相关道具、失败、复活共 9 条冻结回放。省略最后一个参数仍是原来的完整代表集，不改变长期回归入口。

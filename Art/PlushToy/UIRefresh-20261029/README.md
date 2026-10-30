# 界面优化（浅色主题 + 新图）美术需求

2026-10-29 21:07 用户确认：
1. **浅色主题全局换**：战斗页、主页、图鉴、设置、暂停、结算都从紫色改成白色或浅蓝渐变，主按钮改为蓝色。
2. 角色不重画，只改名（程序做，和美术无关）。
3. 程序先做不需要新图的部分；美术同时按本文件出图。

需求来源：《泡泡小队界面优化.xlsx》。每一项的当前截图和参考图都放在 `references/` 里，文件名带条目号，例如 `item07-win-ref.png`。**参考图只代表方向，要用我们自己的毛绒画风来画**，不要照抄。

## 通用要求

- 画风：与现有角色一致，写实毛绒质感，柔和的右上方光，3D 圆润。界面件干净、明亮，不要厚重描边。
- 主色板：主蓝 `#2F7BFF`（渐变到 `#1E5FE0`），浅蓝底 `#E8F2FF`～`#CFE4FF`，白 `#FFFFFF`，强调黄 `#FFC93C`，警示红 `#FF4D5A`，深字蓝 `#1D3A7A`。
- 除了 N1 和 N6 两张字标，**图里一律不要文字**，按钮上的字由程序写。
- 交付透明 PNG（背景图除外），尺寸按表格。9 宫格件四角圆角要完整，中间可拉伸区域不能有图案。
- 交付到本目录的 `delivery/` 下，路径见表格。生成原图放 `masters/`，提示词和生成次数记入 `prompts.json`。交付后在本文件末尾写自查：尺寸、透明边、在白底和浅蓝底上各看一次。

## T：浅色主题件（优先，程序换主题要用）

| # | 交付文件 | 尺寸 | 内容 | 参考 |
| --- | --- | --- | --- | --- |
| T1 | `delivery/theme/battle-background.png` | 810×1440 | 战斗页背景：上方白色，往下渐变到浅蓝；有极淡的泡泡和小四角星暗纹，不透明度低于 8%，不能抢棋盘 | item09-light-battle-ref、item16-rail-ref |
| T2 | `delivery/theme/home-background.png` | 1080×2340 | 主页背景：白底，画面中上部有几圈很淡的浅蓝同心光环（衬在主角身后） | item04-home-ref |
| T3 | `delivery/theme/codex-background.png` | 810×1755 | 图鉴背景：白底，顶部有淡蓝同心光环，越往下越干净 | item18-codex-ref |
| T4 | `delivery/theme/board-frame.png` | 192×192（9 宫格，圆角约 32） | 棋盘框：白色磨砂玻璃边框，内底是很浅的灰蓝 `#E9F1FC` | item09-light-battle-ref |
| T5 | `delivery/theme/queue-cell.png` | 144×114 | 排队格：白色半透明圆角卡片，1–2 px 淡蓝描边，轻微内阴影 | item16-rail-ref |
| T6 | `delivery/theme/pad.png` | 120×48 | 出战位底座：浅灰蓝椭圆小托盘，有一点厚度和高光 | item16-rail-ref |
| T7 | `delivery/theme/progress-track.png` | 384×36 | 进度条底槽：白色胶囊，内侧浅灰蓝 | item16-rail-ref |
| T8 | `delivery/theme/level-pill.png` | 384×72 | 顶部“第 N 关”底牌：白色胶囊，轻阴影 | item16-rail-ref |
| T9 | `delivery/theme/pause.png` | 84×84 | 暂停按钮：蓝色光泽圆形，白色两竖条 | item16-rail-ref |
| T10 | `delivery/theme/dialog-panel.png` | 270×270（9 宫格，圆角约 40） | 弹窗底板：白色卡片，极淡蓝描边，柔和投影 | item05-settings-ref |
| T11 | `delivery/theme/dialog-row.png` | 240×72（9 宫格） | 设置行底：很浅的灰蓝圆角条 | item05-settings-ref |
| T12 | `delivery/theme/icon-music.png`、`icon-sound.png`、`icon-light.png` | 各 96×96 | 设置图标：蓝色光泽的音符、喇叭、太阳 | item05-settings-ref |
| T13 | `delivery/theme/button-blue.png` | 390×108（9 宫格胶囊） | 主按钮：蓝色光泽胶囊，底部略深有厚度，无字 | item04-home-ref“开始游戏” |
| T14 | `delivery/theme/button-yellow.png` | 390×108（9 宫格胶囊） | 黄色主按钮（通关的“下一关”），同 T13 的造型 | item07-win-ref |
| T15 | `delivery/theme/button-white.png` | 260×88（9 宫格胶囊） | 次按钮：白色胶囊，淡蓝描边（失败页“返回主页/分享”） | item17-lose-ref |
| T16 | `delivery/theme/button-red.png` | 390×108（9 宫格胶囊） | 红色按钮（“看视频复活”） | item14-revive-ref |
| T17 | `delivery/theme/icon-home.png`、`icon-replay.png`、`icon-share.png`、`icon-video.png` | 各 64×64 | 白色线面小图标：小房子、循环箭头、分享箭头、视频播放（放在按钮里，和字一起用） | item07、item14、item17 |
| T18 | `delivery/theme/btn-settings.png`、`btn-codex.png` | 各 64×64 | 主页左上齿轮（浅灰蓝）和右上图鉴书（蓝色） | item04-home-ref |

## N：界面新图

| # | 交付文件 | 尺寸 | 内容 | 参考 |
| --- | --- | --- | --- | --- |
| N1 | `delivery/logo/title-logo.png` | 1024×640 | “泡泡小队”字标：四个字圆胖、上下错落，白到黄渐变，粗蓝描边，有立体厚度；右下角是蓝色小熊（现有 2982F4 小熊）从蓝色花盆里探出来，旁边一个小泡泡。加载页和主页共用。**四个字必须准确，交付前逐字核对** | item01-loading-ref、item04-home-ref |
| N2 | `delivery/loading/loading-background.png` | 1080×2340 | 加载页背景：明亮的天蓝渐变，有一层浅色泡泡和四角星的图案 | item01-loading-ref |
| N3 | `delivery/win/mvp-crown.png` | 200×150 | MVP 金色皇冠，旁边带两三颗小闪光 | item07-win-ref |
| N4 | `delivery/lose/hero-sad.png` | 512×512 | 失败主图：现有失败图里那只蓝色小熊（`references/lose-hero-current.png`），垂头、皱眉、委屈地趴在盆沿上；蓝色花盆右侧裂开，掉下一块碎片；嘴边一小团叹气的白烟。同一套毛绒质感 | item17-lose-ref |
| N5 | `delivery/fx/aim-ring.png` | 512×512 | 炸弹准星：发光的青色细圆环，上下左右四个短刻度，中心一个小十字；圈内非常淡的青色 | item13-bomb-ref |
| N6 | `delivery/incoming/title.png` | 720×180 | “泡泡来袭！”艺术字：黄到橙渐变，深色描边，有厚度，略带弹跳感。**四个汉字和全角感叹号必须准确** | item10-incoming-ref |
| N7 | `delivery/incoming/chevron.png` | 96×96 | 黄色双层向下箭头，配合 N6 放在两侧 | item10-incoming-ref |
| N8 | `delivery/slot/locked.png` | 160×160 | 锁住的出战位：蓝色光泽圆角方块，中间白色挂锁，右上角一个蓝色小“视频播放”角标 | item19-locked-slot-ref |
| N9 | `delivery/guide/hand.png` | 160×160 | 引导手指：白色卡通手套，食指朝左上方点，有柔和阴影，指尖在画布左上 1/4 处 | item02、item03 参考 |
| N10 | `delivery/fx/rail-chevron.png` | 48×48 | 护栏预警用的红色双层向上箭头，带一点红色光晕 | item16-rail-ref |
| N11 | `delivery/fx/revive-wave.png` | 810×300 | 复活清屏的浪：一道横向的浅青色水浪，浪尖有白色泡沫、小泡泡和闪光，上下边透明 | item14-revive-success-ref |
| N12 | `delivery/codex/pedestal.png` | 360×90 | 图鉴展示台：白到淡蓝的光泽圆台，边缘有柔光 | item18-codex-ref |

共 T 类 24 张、N 类 12 张，总计 36 张。建议先交 T1、T4–T9、T13、T14 和 N9：这几张决定战斗页和引导换完以后的样子，程序可以先接入。

## 不需要美术的部分（程序做）

引导气泡和文字、黑底提示条、“复活成功”、标题文字、结算排版、彩带、伙伴改名、去掉跳关、“下一位伙伴”卡片、健康游戏忠告。

## 首批交付自查（2026-10-29）

本批已交 T1、T4–T9、T13、T14 和 N9，共 10 张。使用内置 image_gen 生成；其余表格条目留待后续批次，尚未交付。

| 条目 | 实际交付路径 | 已核验尺寸 | PNG 类型 | 采用原图 / 生成次数 |
| --- | --- | --- | --- | --- |
| T1 | `delivery/theme/battle-background.png` | 810×1440 | RGB，不透明 | attempt02 / 2 |
| T4 | `delivery/theme/board-frame.png` | 192×192 | RGBA | attempt03 / 3 |
| T5 | `delivery/theme/queue-cell.png` | 144×114 | RGBA | attempt01 / 1 |
| T6 | `delivery/theme/pad.png` | 120×48 | RGBA | attempt01 / 1 |
| T7 | `delivery/theme/progress-track.png` | 384×36 | RGBA | attempt01 / 1 |
| T8 | `delivery/theme/level-pill.png` | 384×72 | RGBA | attempt01 / 1 |
| T9 | `delivery/theme/pause.png` | 84×84 | RGBA | attempt01 / 1 |
| T13 | `delivery/theme/button-blue.png` | 390×108 | RGBA | attempt01 / 1 |
| T14 | `delivery/theme/button-yellow.png` | 390×108 | RGBA | attempt01 / 1 |
| N9 | `delivery/guide/hand.png` | 160×160 | RGBA | attempt01 / 1 |

- **原图与提示词**：13 次生成的未经修改原图全部保存在 `masters/`，含被弃用的背景和棋盘框尝试；`prompts.json` 逐次记录完整提示词、生成次数、输入图、原图路径、采用版本及导出裁切参数。交付图仅用 sips 裁去多余透明留白并缩放至表格尺寸，生成原图未覆盖。
- **尺寸与透明边**：逐张读取 PNG 检查尺寸；9 张透明素材四角 alpha 均为 0，外沿最高 alpha 为 3/255，白底、浅蓝底下未见黑边、矩形底色或明显杂点。完整数值见 `qa/measurements.json`。
- **双底检查**：已逐张查看白底 `#FFFFFF` 和浅蓝底 `#E8F2FF` 的原尺寸合成图（T1 为缩略展示），分别保存在 [`qa/priority-white.png`](qa/priority-white.png) 和 [`qa/priority-light-blue.png`](qa/priority-light-blue.png)。白色件在白底靠浅蓝边和阴影辨形；浅蓝底下轮廓完整。
- **背景**：T1 首稿装饰偏亮，已弃用；采用稿去掉玻璃高光和彩色反光，以 5% 暗纹为生成约束。已目视确认泡泡、四角星极淡且集中在边缘，中间留空。成品为合并后的 RGB PNG，无法从成品独立核验暗纹图层透明度数值。
- **九宫格**：T4 中央无图案，原先透底的两稿均弃用。以左/上/右/下 `36/36/36/36 px` 拉伸至 576×432；T13、T14 以 `60/36/60/36 px` 横向拉伸至 720×108，已查看完整圆角、连续高光和中部接缝。双底拉伸检查见 `qa/nine-slice-white.png`、`qa/nine-slice-light-blue.png`；上述边距可供程序接入参考。
- **配对与引导**：T14 从 T13 原图改色，保留同一胶囊轮廓与厚度。N9 食指朝左上，指尖落在左上四分之一附近；建议点击热点约 `(36, 40)`（从左上角计像素），四周有透明留白。
- **无字检查**：本批 10 张均无文字、数字、字母或水印；T9 仅含暂停的两条白色竖条。N1、N6 尚未生成，未将其标记为文字核验通过；后续必须按“泡泡小队”和“泡泡来袭！”逐字核对。N6 原文实际为四个汉字加全角感叹号，应以原文为准。

自查脚本：`python3 Art/PlushToy/UIRefresh-20261029/qa/verify_delivery.py`。脚本只读取交付素材并生成检查图与测量记录，不修改交付 PNG。本批未接入 Unity 或进行游戏运行截图验收。

## 第二批交付自查（2026-10-30）

按用户指定顺序补齐：弹窗及按钮图标 → 主页/图鉴背景 → N1 标题 → 其余 N 类。本批 **26 张**已全部交付，加上首批 10 张，表格内 **36 张**文件现已齐全。原总数“T 类 25 张”是计数误差，逐文件核对为 T 类 24 张、N 类 12 张，已更正。

| 条目 | 实际交付路径 | 已核验尺寸 | PNG 类型 | 生成次数 |
| --- | --- | --- | --- | --- |
| T10 | `delivery/theme/dialog-panel.png` | 270×270 | RGBA | 3 |
| T11 | `delivery/theme/dialog-row.png` | 240×72 | RGBA | 1 |
| T12-music | `delivery/theme/icon-music.png` | 96×96 | RGBA | 1 |
| T12-sound | `delivery/theme/icon-sound.png` | 96×96 | RGBA | 1 |
| T12-light | `delivery/theme/icon-light.png` | 96×96 | RGBA | 1 |
| T15 | `delivery/theme/button-white.png` | 260×88 | RGBA | 1 |
| T16 | `delivery/theme/button-red.png` | 390×108 | RGBA | 1 |
| T17-home | `delivery/theme/icon-home.png` | 64×64 | RGBA | 1 |
| T17-replay | `delivery/theme/icon-replay.png` | 64×64 | RGBA | 1 |
| T17-share | `delivery/theme/icon-share.png` | 64×64 | RGBA | 1 |
| T17-video | `delivery/theme/icon-video.png` | 64×64 | RGBA | 2 |
| T18-settings | `delivery/theme/btn-settings.png` | 64×64 | RGBA | 1 |
| T18-codex | `delivery/theme/btn-codex.png` | 64×64 | RGBA | 1 |
| T2 | `delivery/theme/home-background.png` | 1080×2340 | RGB | 1 |
| T3 | `delivery/theme/codex-background.png` | 810×1755 | RGB | 1 |
| N1 | `delivery/logo/title-logo.png` | 1024×640 | RGBA | 2 |
| N2 | `delivery/loading/loading-background.png` | 1080×2340 | RGB | 1 |
| N3 | `delivery/win/mvp-crown.png` | 200×150 | RGBA | 1 |
| N4 | `delivery/lose/hero-sad.png` | 512×512 | RGBA | 1 |
| N5 | `delivery/fx/aim-ring.png` | 512×512 | RGBA | 1 |
| N6 | `delivery/incoming/title.png` | 720×180 | RGBA | 1 |
| N7 | `delivery/incoming/chevron.png` | 96×96 | RGBA | 1 |
| N8 | `delivery/slot/locked.png` | 160×160 | RGBA | 1 |
| N10 | `delivery/fx/rail-chevron.png` | 48×48 | RGBA | 1 |
| N11 | `delivery/fx/revive-wave.png` | 810×300 | RGBA | 1 |
| N12 | `delivery/codex/pedestal.png` | 360×90 | RGBA | 1 |

- **原图与追溯**：本批使用内置 image_gen，共生成 30 次；30 张未经修改原图已存入 `masters/`，连同首批共 43 张。T10 采用第 3 稿，T17-video 和 N1 采用第 2 稿，其余采用第 1 稿；弃用稿保留。完整提示词、输入图、生成次数、采用版本、裁切和缩放参数均追加到 `prompts.json`。已逐一对照工具原始文件计算 SHA-256，确认 masters 未经修改。
- **尺寸**：26 张逐文件读取 PNG 核对，与表格完全一致。T2、T3、N2 为不透明背景，其余 23 张为 RGBA。交付图只做透明留白裁切与尺寸导出，未用代码代画素材。
- **透明边**：23 张透明 PNG 四角 alpha 均为 0；已检查完整圆角、图标孔洞、角色毛边及发光边，没有看到黑色底框或明显杂边。N11 水浪上、下沿 alpha 最大值分别为 0、1/255，符合上下透明的叠加用途。测量见 `qa/batch2-measurements.json`。
- **白底与浅蓝底**：最终采用的 26 张已分别叠在 `#FFFFFF` 和 `#E8F2FF` 上目视检查。UI 对照见 [白底](qa/batch2-ui-white.png)、[浅蓝底](qa/batch2-ui-light-blue.png)；背景见 [白底](qa/batch2-backgrounds-white.png)、[浅蓝底](qa/batch2-backgrounds-light-blue.png)；N 类见 [白底](qa/batch2-new-white.png)、[浅蓝底](qa/batch2-new-light-blue.png)。检查图标签仅用于 QA，不在交付素材中。
- **小图标**：T12 的音符、喇叭、太阳在 96×96 下辨识清楚；T17 四张白色图标还叠在蓝色按钮上按 64×64 检查。视频图标首稿多出的红色圆底已去除，最终只保留白色播放图标。N7 为黄色双层向下箭头，N10 为红色双层向上箭头，方向已核对。
- **九宫格**：T10 中心已修正为平整浅白底，T11 无中心图案，T15/T16 保留完整胶囊端部；已分别查看白底和浅蓝底拉伸样张，未见断角或明显接缝。建议边距（左/上/右/下）：T10 `48/48/48/48`、T11 `18/18/18/18`、T15 `50/30/50/30`、T16 `60/36/60/36` px。样张见 `qa/batch2-nine-slice-white.png`、`qa/batch2-nine-slice-light-blue.png`；参数见 `qa/batch2-nine-slice.json`。
- **N1 逐字核对**：`泡｜泡｜小｜队`，四字完整、顺序正确，无副标题和多余文字。白黄渐变、蓝色描边、右下方蓝熊花盆及单个小泡泡均已检查。蓝熊来自现有 `2982F4/idle.png` 身份参考；第 2 稿调整了角色大小和右下位置。
- **N6 逐字核对**：`泡｜泡｜来｜袭｜！`，四个汉字与一个全角感叹号正确，黄橙渐变、深蓝描边；未混入箭头或其他字。对两张最终尺寸交付图进行人工逐字目视检查，并用本地 Vision OCR 辅助复核，分别识别为“泡泡小队”和“泡泡来袭！”；OCR 原始结果见 `qa/wordmarks-ocr.json`。其中 N6 OCR 置信度为 0.5，最终判断同时依据人工字形检查。
- **其余无字与角色一致性**：除 N1、N6 外，本批 24 张未发现文字、数字或水印。N4 基于指定失败蓝熊原图修改，保留蓝色毛绒、米色口鼻、绿叶及蓝色贴花盆；问号已去除，委屈趴盆姿态、右侧裂口、一块碎片和嘴边叹气均已检查。N5 为四向刻度加中心十字；N8 为白锁加右上播放角标；N11 无“复活成功”等程序文字。

完整文件及原图哈希清单：`qa/inventory-sha256.json`。第二批自查脚本：`python3 Art/PlushToy/UIRefresh-20261029/qa/verify_batch2.py`；首批脚本已限定只检查首批条目。以上为美术文件验收，尚未在本任务中接入 Unity 或完成游戏运行截图验收。


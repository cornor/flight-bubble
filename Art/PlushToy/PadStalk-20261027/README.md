# 卡槽队员：连接杆前探（PadStalk）

> 状态（2026-10-27 11:15）：已停用。用户认为程序变形与默认状态看不出区别，游戏已退回干净版本（只保留泡泡圈、水珠、盆边气团、火力全开效果）；改为手绘前倾姿势试做，见 `../PadReach-20261027/`。GardenSkin 中的连接杆/变形代码保留未启用。下文为当时的记录。

2026-10-27。用户要求卡槽上的玩具攻击时更有张力，参考《植物大战僵尸》向日葵与花盆之间的"树干"：平时藏在花盆里，攻击时借它把身体探出去。只改表现层，战斗逻辑、射速、命中和受保护文件不变（`scripts/r1/check_source.py` 通过）。

## 当前动作（程序已实装，素材沿用现有交付）

- 待机（有弹药、未开火，含"等待"）：ready 鼓腮图，身体略沉入花盆并轻微呼吸。
- 开火：attack 吹泡图，身体沿连接杆顶出约画布高度 18%，朝镜头放大约 14%，弹簧过冲后保持；每发子弹轻微回缩。约 0.9 秒后缩回换气约 0.33 秒（鼓腮图，不出泡泡圈），再探出，循环。连续开火 1.6 秒后进入火力全开：循环加快到约 1 秒，幅度 +15%，自身颜色光晕和速度线。
- 花盆始终不动；不摆头、不转身；只有上下弹簧感。
- 叶子随身体一起移动（现有图里叶子与身体画在一起），身体底部最后 9 行在探出时淡出。
- 连接杆暂由程序绘制（绿色毛毡渐变占位）。

拆图方式：每张队员图在画布 y=148/256（花盆口）切开。绘制顺序为连接杆 → 身体（含叶子，可平移、放大）→ 花盆（固定，在最前）。退回时身体与原图完全重合，连接杆被花盆遮住。

代码：`Garden/GardenSkin.cs`（`BendExtend` / `BendGrow` / `Stem()`）、`Garden/PixelWingGame.GardenPads.cs`（`PadPumpFor`，所有时长与幅度参数）、`Garden/PixelWingGame.Garden.cs`（卡槽绘制接入）、`Garden/PixelSurface.Garden.cs`（`GardenQuadCornersColors`，身体底边淡出）。

## 待美术：毛绒连接杆贴图

替换程序占位。全体队员共用一张。

| 项 | 要求 |
| --- | --- |
| 交付文件 | `Art/PlushToy/PadStalk-20261027/delivery/stalk.png` |
| 尺寸 | 48×96，RGBA 透明底 |
| 内容 | 一段竖直的毛绒/毛毡材质植物茎，正面视角，宽度占满画布约 70%，两侧留透明 |
| 拼接 | 上下边缘可无缝竖向重复（程序按探出高度平铺或拉伸） |
| 颜色 | 与现有花盆叶子一致的毛毡绿（约 #5A9A3C），光从右上方来：右侧略亮、左侧略暗 |
| 质感 | 与现有毛绒玩具同一套写实毛毡渲染，细绒毛边，柔和明暗；无描边、无投影、无文字 |
| 禁止 | 叶子、花、刺、节疤等会在平铺时重复出现的显眼细节 |

显示尺寸参考：卡槽队员画布约 55–70 逻辑像素高，连接杆显示宽约 9–12 逻辑像素、探出时露出约 10–15 逻辑像素高。

英文提示词：

```
A single vertical segment of a plush felt plant stem, front view, seamless vertically tileable top and bottom edges, soft realistic felt / fleece texture with fine fuzzy fibers, leafy felt green (#5A9A3C), gentle lighting from the upper right (right side slightly lighter, left side slightly darker), no outline, no shadow, no leaves, no flowers, no text, centered, about 70% of the canvas width, transparent background, matches a photoreal plush toy style.
```

## 后续可选（未做）

叶子留在盆上、茎从叶子中间伸出（更接近向日葵）需要拆层素材：每位队员一张不带叶子、底部延伸进花盆的身体图（各状态），以及叶子单图。当前不做。

## 过程记录

- `pump-v1-frames.png`：第一版"节拍泵＋斜推歪头"在第 3 关的 24 帧连拍（用户认为歪头效果不达预期）。
- `forward_mock.png`、`long_mock.png`：朝镜头前探与"拉长＋前探"的静态示意，用户选过"更强""更长"档。
- `stem_mock.png`：连接杆方案示意，用户确认按建议实施。

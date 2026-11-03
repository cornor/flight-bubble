# 打泡泡：可染色的泡泡破裂序列帧

**当前交付版采用用户选定的方案 B「果冻拍浪」六帧，文件及播放时长见文末交付自查。**

2026-11-02 16:33，用户反馈：程序画的泡泡破裂"破的这一圈都是什么鬼，不好看"（见 `references/program-pop-problem.png` 右上角）。这一层改由美术出图。

用途：队员（0 到 2 级）每打掉一颗泡泡，就在泡泡的位置播一次破裂。游戏里每秒会破几十颗，所以要**小而精致、一眼读得出"泡泡破了"**。每颗泡泡颜色不同，所以图要能**被程序染成任意颜色**。

参考图在 `references/`：

- `style-rocket-pop.png`：游戏里现有的泡泡爆点序列帧（蓝白肥皂泡 → 星光 → 泡沫圈）。它的泡泡感、画风、高光以它为准；但它是固定的蓝白色、太大，这次要的是能染色、更紧凑的版本。
- `program-pop-problem.png`：程序画的效果，右上角那团一圈圈的弧线就是要避免的样子。

## 要交的图

每一帧交两张：**颜色层**（程序会乘上泡泡的颜色）和**高光层**（保持白色，叠在颜色层上面）。

| # | 交付文件 | 尺寸 | 内容 |
| --- | --- | --- | --- |
| P1 | `delivery/pop-1-color.png`、`pop-1-light.png` | 各 256×256，透明 | 鼓起的薄膜泡：一颗半透明的圆泡泡，占画布约 60%，外沿一圈实一点的边，中间很淡 |
| P2 | `pop-2-color.png`、`pop-2-light.png` | 同上 | 破开的瞬间：泡泡膜从一侧撕开，撕开处有一个亮点 |
| P3 | `pop-3-color.png`、`pop-3-light.png` | 同上 | 炸开：膜碎成几片小而软的碎膜，往外甩；同时溅出一圈大小不一的小水珠，范围约占画布 85% |
| P4 | `pop-4-color.png`、`pop-4-light.png` | 同上 | 散开：水珠更远、更小，碎膜变淡 |
| P5 | `pop-5-color.png`、`pop-5-light.png` | 同上 | 消散：只剩几颗很小的水珠和一点雾 |

要求：

- **颜色层**用白到中灰画：白的地方会变成泡泡的颜色，灰的地方变成更深的同色，透明就是透明。不要带任何彩色。
- **高光层**只画白色高光：泡泡上沿的弧形反光、破开瞬间的亮点、水珠上的小亮点。两张叠在一起要对齐。
- **不要画成扩散的圆环或一圈圈弧线**，碎片和水珠要自然、不规则、有大小变化，不要等距排成一圈。
- 画风干净、圆润，跟 `style-rocket-pop.png` 和游戏整体一致；透明边缘干净，四周不留白边。

程序这边的用法：0.28 秒内顺播 P1 到 P5，颜色层乘上被打泡泡的颜色，高光层叠在上面；连击越高放得越大。

## 交付与记录

- 生成原图存 `masters/`，提示词和生成次数记入 `prompts.json`。
- 交付后在本文件末尾写自查：
  - 尺寸，颜色层和高光层是否对齐
  - 5 帧染成橙、蓝、白、黑四种颜色的总览图，放在 `qa/`
  - 连播预览 GIF，放在 `qa/`

## 方案 B「果冻拍浪」六帧交付自查（2026-11-02）

程序将颜色层的 RGB 乘以命中泡泡色，保留 alpha，再用普通透明混合把对应的白色高光层叠在上面。两层使用相同的帧号、256×256 画布和中心锚点 (128, 128)。单次动作共 0.30 秒。

| 帧与动作 | 灰度水体 | 白色高光 | 时长 |
| --- | --- | --- | --- |
| 1 · 圆泡蓄势 | [pop-1-color.png](delivery/pop-1-color.png) | [pop-1-light.png](delivery/pop-1-light.png) | 20 ms |
| 2 · 横向压扁 | [pop-2-color.png](delivery/pop-2-color.png) | [pop-2-light.png](delivery/pop-2-light.png) | 60 ms |
| 3 · 水舌回弹 | [pop-3-color.png](delivery/pop-3-color.png) | [pop-3-light.png](delivery/pop-3-light.png) | 40 ms |
| 4 · 两翼分裂 | [pop-4-color.png](delivery/pop-4-color.png) | [pop-4-light.png](delivery/pop-4-light.png) | 60 ms |
| 5 · 落珠散开 | [pop-5-color.png](delivery/pop-5-color.png) | [pop-5-light.png](delivery/pop-5-light.png) | 60 ms |
| 6 · 小滴消失 | [pop-6-color.png](delivery/pop-6-color.png) | [pop-6-light.png](delivery/pop-6-light.png) | 60 ms |

- 12 张交付图均为 256×256 透明 RGBA PNG。水体的可见像素为等值 RGB，灰度范围 128–255。高光的可见像素均为纯白，亮度过渡由 alpha 表示。四条边及空白处透明，透明像素的 RGB 均为零。
- 已逐帧将两层叠加，与选定方案 B 的原逐帧图比对。六帧合成后每个像素的 8 位 alpha 均与原图一致，压扁的高度、水舌的轮廓和落珠的位置都保留了原来的配准。[逐帧校验记录](qa/verification.json)
- 橙、蓝、白和纯黑均已用交付文件实际染色检查。白色版本可读出水体明暗，纯黑版本的亮斑随水舌和落珠移动。[四色六帧总览](qa/tint-overview.png) · [泡泡阵单发及连打 GIF](qa/pop-on-board.gif)
- 内置 ImageGen 生成 1 次，原图为一张含六个姿势的灰度水体重绘图集。白色反光取自已选 B 版的对应位置。[生成原图](masters/jelly-slap-neutral-v01.png) · [提示词与导出记录](prompts.json)

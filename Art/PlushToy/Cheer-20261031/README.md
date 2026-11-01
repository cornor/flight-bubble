# 全员欢呼图（胜利页 MVP 用）

2026-10-31 16:28，用户问："胜利的时候，MVP 的玩具是欢呼的状态，是不是每个玩具都要出欢呼的图？"答复：要。现在 22 位队员都没有欢呼图，胜利页的 MVP 只能用平时的待机图，加一点跳动。

MVP 可能是任何一位队员，所以 22 位都要画一张。新伙伴那一排也会用到这张图。

参考图在 `references/`：

- `win-mvp-cheer-ref.png`、`win-mvp-cheer-ref-zoom.png`：胜利页示意图和 MVP 放大图，欢呼的样子以它为准
- `<creatureId>-<色号>-idle.png`：每位队员现在的待机图，造型、颜色、花盆以它为准

## 每位交付 1 张

`cheer.png`，224×256，RGBA 透明 PNG。和该队员的待机图同一画布、同一位置：

- **花盆和两片大叶子与待机图逐像素一致**，不能移动、不能变大小。
- 只改玩具本身，**照示意图的欢呼样子**：
  - 眼睛开心地眯成弯弯的月牙（闭眼笑）
  - 嘴巴张大笑，露出一点舌头
  - 两只手（或翅膀、前爪）高高举起，在头的两侧向上张开，像在欢呼
  - 身体可以略微向上伸展，但头顶不要超出画布，耳朵、角、头上的装饰都要完整
- 每位的毛色、五官特征、身上的花纹、头部装饰都和待机图一致，一眼能认出是同一位。
- 画风、质感、光照和现有角色一致：写实毛绒质感，柔和的右上方光，圆润 3D。
- 不画泡泡、星星、彩带、王冠等任何特效，这些由程序加。
- 待机图本来就没有手的角色（例如企鹅、乌龟、绵羊），用翅膀、鳍或前腿举起表示欢呼；实在没有可举的部位，就把身体后仰、头抬高、表情欢呼。

## 22 位队员

参考图在 `references/`，名单和交付路径也在 `roster.json` 里。

| 动物 | 名字 | 色号 | creatureId | 交付路径 |
| --- | --- | --- | --- | --- |
| 小熊 | 啵啵 | 2982F4 | blue | `delivery/creatures/blue/2982F4/cheer.png` |
| 考拉 | 糯糯 | 42A9FF | koala | `delivery/creatures/koala/42A9FF/cheer.png` |
| 水豚 | 豚豚 | FF7E27 | orange | `delivery/creatures/orange/FF7E27/cheer.png` |
| 狮子 | 卷卷 | FF9D30 | lion | `delivery/creatures/lion/FF9D30/cheer.png` |
| 青蛙 | 呱呱 | 62E18E | green | `delivery/creatures/green/62E18E/cheer.png` |
| 小象 | 朵朵 | BD80FF | purple | `delivery/creatures/purple/BD80FF/cheer.png` |
| 小鸡 | 叽叽 | FFDF48 | yellow | `delivery/creatures/yellow/FFDF48/cheer.png` |
| 猫咪 | 绵绵 | FFFFFF | white | `delivery/creatures/white/FFFFFF/cheer.png` |
| 狐狸 | 桃桃 | FF525B | red | `delivery/creatures/red/FF525B/cheer.png` |
| 企鹅 | 团团 | 2D2D2D | charcoal | `delivery/creatures/charcoal/2D2D2D/cheer.png` |
| 浣熊 | 豆豆 | 696CAF | slate-blue | `delivery/creatures/slate-blue/696CAF/cheer.png` |
| 海豹 | 啾啾 | 46D9FF | cyan | `delivery/creatures/cyan/46D9FF/cheer.png` |
| 河马 | 芋芋 | C8C5FF | lavender | `delivery/creatures/lavender/C8C5FF/cheer.png` |
| 小鹿 | 栗栗 | 9C225E | berry | `delivery/creatures/berry/9C225E/cheer.png` |
| 猴子 | 皮皮 | 65392D | brown | `delivery/creatures/brown/65392D/cheer.png` |
| 猫头鹰 | 咕咕 | B672FD | violet | `delivery/creatures/violet/B672FD/cheer.png` |
| 刺猬 | 球球 | 92EF4D | lime | `delivery/creatures/lime/92EF4D/cheer.png` |
| 乌龟 | 慢慢 | 30D183 | mint | `delivery/creatures/mint/30D183/cheer.png` |
| 鳄鱼 | 咔咔 | 0C7E1C | forest | `delivery/creatures/forest/0C7E1C/cheer.png` |
| 兔子 | 糖糖 | FFC5E0 | pink | `delivery/creatures/pink/FFC5E0/cheer.png` |
| 小猪 | 哼哼 | FFCAA9 | peach | `delivery/creatures/peach/FFCAA9/cheer.png` |
| 绵羊 | 咩咩 | C5FFE4 | pale-mint | `delivery/creatures/pale-mint/C5FFE4/cheer.png` |

## 交付与记录

- 可以分批交，每批几位都行，程序做完一位就能接一位。建议先交前 4 位（啵啵、豚豚、叽叽、绵绵）。
- 生成原图存 `masters/`，提示词和生成次数记入 `prompts.json`。
- 全部交完后，在本文件末尾写自查：
  - 尺寸
  - 花盆和叶子与待机图逐像素一致的检查结果
  - 每位和待机图并排的对照图，放在 `qa/`
  - 头顶、耳朵、举起的手有没有被画布切掉

## 交付自查（2026-10-31）

22 位已全部交付，路径与上表及 `roster.json` 一致。使用内置 ImageGen 生成，共 **24 次**：小象 2 次（修正前脚形状）、海豹 2 次（修复抬鳍后的叶面遮挡区），其余每位 1 次。24 张生成原图均原样保存在 `masters/`，完整提示词、参考图、次数、选用版本和文件校验信息见 [prompts.json](prompts.json)。

- **尺寸 / 格式**：22 张均为 **224×256、RGBA 透明 PNG**，与待机图使用同一画布。花盆位置与大小由原图锁定。
- **花盆与叶子**：花盆及叶子保护遮罩内，逐像素比较全部 RGBA 通道，22 张均为 **0 个差异像素**。逐位遮罩见 `qa/masks/<creatureId>-protected.png`，汇总结果见 [qa/verification.json](qa/verification.json)。
- **海豹遮挡区说明**：原来的两只趴鳍盖住部分叶面；抬鳍后新露出的叶面、原鳍接触边缘及遮挡阴影已补绘。该补绘区不作逐像素一致声明，范围单独保存在 [qa/masks/cyan-occlusion-repair.png](qa/masks/cyan-occlusion-repair.png)。补绘区以外的受保护原叶面和花盆保留原像素。
- **边界 / 透明**：逐位检查了头顶、耳朵、角、头饰及举起的手、翅膀、鳍，均无裁切。可见轮廓（alpha ≥ 16）距上、左、右画布边缘最少 6 px；最外圈 alpha 全为 0。
- **造型 / 表情**：逐位对照待机图，保留毛色、五官特征、花纹、头饰及毛绒质感；均为闭眼笑、张嘴笑，双手、双翅或前肢举起。图内没有文字、王冠、泡泡、星光、彩带等特效。
- **背景复看**：每位都在白底 `#FFFFFF` 和浅蓝底 `#E8F2FF` 上检查了透明边、叶缘衔接和主体清晰度。逐位并排对照见下表。

欢呼总览：[qa/cheer-roster.png](qa/cheer-roster.png)；全员待机 / 欢呼对照：[qa/all-comparisons.png](qa/all-comparisons.png)。

| 队员 | creatureId | 尺寸 | 生成次数 | 保护区差异像素 | 边界检查 | 待机 / 欢呼对照 |
| --- | --- | --- | ---: | ---: | --- | --- |
| 啵啵（小熊） | `blue` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/blue-idle-vs-cheer.png) |
| 糯糯（考拉） | `koala` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/koala-idle-vs-cheer.png) |
| 豚豚（水豚） | `orange` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/orange-idle-vs-cheer.png) |
| 卷卷（狮子） | `lion` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/lion-idle-vs-cheer.png) |
| 呱呱（青蛙） | `green` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/green-idle-vs-cheer.png) |
| 朵朵（小象） | `purple` | 224×256 | 2 | 0 | 完整，无裁切 | [查看](qa/purple-idle-vs-cheer.png) |
| 叽叽（小鸡） | `yellow` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/yellow-idle-vs-cheer.png) |
| 绵绵（猫咪） | `white` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/white-idle-vs-cheer.png) |
| 桃桃（狐狸） | `red` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/red-idle-vs-cheer.png) |
| 团团（企鹅） | `charcoal` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/charcoal-idle-vs-cheer.png) |
| 豆豆（浣熊） | `slate-blue` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/slate-blue-idle-vs-cheer.png) |
| 啾啾（海豹） | `cyan` | 224×256 | 2 | 0 | 完整，无裁切 | [查看](qa/cyan-idle-vs-cheer.png) |
| 芋芋（河马） | `lavender` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/lavender-idle-vs-cheer.png) |
| 栗栗（小鹿） | `berry` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/berry-idle-vs-cheer.png) |
| 皮皮（猴子） | `brown` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/brown-idle-vs-cheer.png) |
| 咕咕（猫头鹰） | `violet` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/violet-idle-vs-cheer.png) |
| 球球（刺猬） | `lime` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/lime-idle-vs-cheer.png) |
| 慢慢（乌龟） | `mint` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/mint-idle-vs-cheer.png) |
| 咔咔（鳄鱼） | `forest` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/forest-idle-vs-cheer.png) |
| 糖糖（兔子） | `pink` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/pink-idle-vs-cheer.png) |
| 哼哼（小猪） | `peach` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/peach-idle-vs-cheer.png) |
| 咩咩（绵羊） | `pale-mint` | 224×256 | 1 | 0 | 完整，无裁切 | [查看](qa/pale-mint-idle-vs-cheer.png) |

复现导出：`python3 qa/export_batch.py`；重新汇总检查与生成记录：`python3 qa/update_manifest.py`。导出只对生成图做画布注册及叶缘局部对齐，再从待机图回填受保护区域；原始 `masters/` 不经过缩放或覆盖。

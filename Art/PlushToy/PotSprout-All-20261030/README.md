# 排队花盆长苗：推广到其余 18 位队员

2026-10-30 09:29 用户：花盆长苗要推广到全部角色。

第 3 关的 4 位（小熊 2982F4、水豚 FF7E27、小鸡 FFDF48、猫咪 FFFFFF）已经在 `../PotSprout-20261029/` 做完，游戏里效果已确认。本目录给其余 18 位做同样的 4 张图，**规格、画法、质感与那 4 位完全相同**。

## 每位交付 4 张

都是 224×256 RGBA 透明 PNG，和该队员的参考图（`references/<creatureId>-<色号>-idle.png`）同一画布、同一位置。

| 文件 | 内容 |
| --- | --- |
| `pot.png` | 只有花盆：去掉两片大叶子和玩具，盆口露出后沿和里面的深色毛毡/土。**盆身（前盆沿以下）与参考图逐像素一致**；原来被叶子或身体挡住的一小段盆沿补画完整。 |
| `sprout.png` | 只有小苗：一根短短的绿色毛毡小茎，顶着两片小嫩芽，不带花苞。茎底落在盆口中央的土面上，不压到前盆沿。整棵高约 30–40 px，宽不超过盆口。 |
| `leaves.png` | 只有参考图里那两片大叶子：位置、形状、颜色与参考图完全一致（可从参考图抠出），被身体挡住的部分补完整。 |
| `body.png` | 只有玩具本身，不要花盆和叶子。表情、姿势、大小、位置与参考图完全一致；被叶子和花盆挡住的下半身补完整，圆润收底，大约画到盆底上方（会藏在盆里，长出来时才露出）。头和身体交界处不能有切口、折痕或透明缝。 |

范例：`example/` 里是小熊的 5 张图（参考原图 idle，以及 pot / sprout / leaves / body），照这个效果做。

## 叠合要求

- `body.png` → `pot.png` → `leaves.png` 依次叠起来，要与参考原图看起来一致：脸、耳朵、轮廓都不能错位或重影。
- `pot.png` + `sprout.png` 要像一盆种着小苗的花盆。
- 18 位的小苗用同一套造型（同样的茎和两片嫩芽），只按各自花盆的盆口位置摆放，不用逐个变化。

## 18 位队员

参考图在 `references/`；“盆中心 x”是程序从参考图量出来的花盆中心，摆小苗时可作参考。

| 动物 | 名字 | 色号 | creatureId | 参考图 | 盆中心 x |
| --- | --- | --- | --- | --- | --- |
| 考拉 | 糯糯 | 42A9FF | koala | koala-42A9FF-idle.png | 108 |
| 狮子 | 卷卷 | FF9D30 | lion | lion-FF9D30-idle.png | 107 |
| 青蛙 | 呱呱 | 62E18E | green | green-62E18E-idle.png | 114 |
| 小象 | 朵朵 | BD80FF | purple | purple-BD80FF-idle.png | 116 |
| 狐狸 | 桃桃 | FF525B | red | red-FF525B-idle.png | 110 |
| 企鹅 | 团团 | 2D2D2D | charcoal | charcoal-2D2D2D-idle.png | 115 |
| 浣熊 | 豆豆 | 696CAF | slate-blue | slate-blue-696CAF-idle.png | 108 |
| 海豹 | 啾啾 | 46D9FF | cyan | cyan-46D9FF-idle.png | 113 |
| 河马 | 芋芋 | C8C5FF | lavender | lavender-C8C5FF-idle.png | 110 |
| 小鹿 | 栗栗 | 9C225E | berry | berry-9C225E-idle.png | 106 |
| 猴子 | 皮皮 | 65392D | brown | brown-65392D-idle.png | 110 |
| 猫头鹰 | 咕咕 | B672FD | violet | violet-B672FD-idle.png | 113 |
| 刺猬 | 球球 | 92EF4D | lime | lime-92EF4D-idle.png | 110 |
| 乌龟 | 慢慢 | 30D183 | mint | mint-30D183-idle.png | 116 |
| 鳄鱼 | 咔咔 | 0C7E1C | forest | forest-0C7E1C-idle.png | 122 |
| 兔子 | 糖糖 | FFC5E0 | pink | pink-FFC5E0-idle.png | 115 |
| 小猪 | 哼哼 | FFCAA9 | peach | peach-FFCAA9-idle.png | 106 |
| 绵羊 | 咩咩 | C5FFE4 | pale-mint | pale-mint-C5FFE4-idle.png | 116 |

同样的信息也在 `roster.json` 里。

## 交付位置与记录

- 交付到 `delivery/creatures/<creatureId>/<色号>/pot.png`、`sprout.png`、`leaves.png`、`body.png`，一共 72 张。例如考拉：`delivery/creatures/koala/42A9FF/`。
- 生成原图存 `masters/`，提示词和生成次数记入 `prompts.json`。
- **每位交付后，在 `delivery/sprout-points.json` 里记 3 个数（画布像素，从左上角量）**，程序要用它们来定位动画：
  - `rimY`：前盆沿最上沿的 y
  - `stemX`、`stemY`：小苗茎底的位置

  格式：`{"42A9FF": {"rimY": 148, "stemX": 108, "stemY": 145}, ...}`。第 3 关那 4 位的盆沿是 y=148，供参考。
- 全部做完后在本文件末尾写自查：
  - 尺寸
  - 盆身逐像素一致的检查结果
  - 三层叠合对照图，放在 `qa/`
  - body 交界处有没有缝

可以分批交付，每批交几位都行，程序这边做完一位就能接一位。

## 交付与自查（2026-10-30）

18 位已全部交付，共 **72 张 224×256 RGBA 透明 PNG**，位于 `delivery/creatures/<creatureId>/<色号>/{pot,sprout,leaves,body}.png`。落点表为 `delivery/sprout-points.json`。参考图与 example 的 SHA-256 均已复核，没有修改。

### 绘制与分层

- 使用内置 image_gen，共 39 次生成：18 张空盆、18 张身体，乌龟去除误留叶子的修正 1 次，兔子和小猪颈部修正各 1 次。原图在 `masters/`，实际英文提示词、引用图、次数及原始输出路径在 `prompts.json`。
- 小苗逐像素复用 `example/bear-2982F4-sprout.png`，仅平移。18 位共用同一造型，可见高度 38px、宽度 42px；茎底在土面上，没有像素压到所记录的前盆沿。
- 大叶子从各自原图抠出，可见保留区域 RGBA 差异均为 0。细小叶根补齐复用上批已生成的叶片原图，副本为 `masters/shared-leaf-completion.png`，不计入新增生成次数。
- **海豹例外已获用户确认**：优先叠合还原，保留前爪遮挡的叶片缺口，前爪归入 body。按 body → pot → leaves 叠合时前爪保持可见。
- 企鹅盆外翅膀归入 body，空盆不含翅膀；乌龟身体误留的大叶子已重绘去掉，保留龟壳和原来的脸。

### 像素和接缝检查

- 全部 72 张重新打开检查，尺寸与 RGBA 模式通过。
- 下表起始行及以下的盆身保留原参考图像素，18 位差异均为 **0**；企鹅仅将盆外翅膀像素分配给 body。
- 原始脸部与耳朵上部保留区域的叠合可见差异为 **0**，逐位检查范围见 `qa/validation.json`。
- 身体交界处已逐张目视检查；颈部中央连续带 alpha 均大于 240，没有透明断缝。圆润下半身延伸到约 y=200，藏在盆内。
- 全图叠合并非逐像素一致：新增盆口、颈部补绘、少量叶根补齐，以及原图 252/253 的半透明 alpha 带来局部差异。在黑白两种底色上检查，差异超过 5/255 的像素占画布 0.923%～4.204%；没有五官或耳朵重影。

### 落点与盆身保留范围

坐标以左上角为原点。`rimY` 是导出空盆前盆沿上沿的定位线，`stemX/stemY` 是小苗茎底。新增后沿和土面高度按各角色调整，前盆沿中央完整保留区从下表“原像素起始行”开始。

| 队员 | creatureId / 色号 | rimY | stemX | stemY | 原像素起始行 |
| --- | --- | ---: | ---: | ---: | ---: |
| 考拉·糯糯 | `koala/42A9FF` | 151 | 108 | 148 | y≥154 |
| 狮子·卷卷 | `lion/FF9D30` | 151 | 107 | 148 | y≥154 |
| 青蛙·呱呱 | `green/62E18E` | 147 | 114 | 144 | y≥150 |
| 小象·朵朵 | `purple/BD80FF` | 151 | 116 | 148 | y≥154 |
| 狐狸·桃桃 | `red/FF525B` | 155 | 110 | 152 | y≥158 |
| 企鹅·团团 | `charcoal/2D2D2D` | 152 | 115 | 149 | y≥155 |
| 浣熊·豆豆 | `slate-blue/696CAF` | 155 | 108 | 152 | y≥158 |
| 海豹·啾啾 | `cyan/46D9FF` | 153 | 113 | 150 | y≥156 |
| 河马·芋芋 | `lavender/C8C5FF` | 155 | 110 | 152 | y≥158 |
| 小鹿·栗栗 | `berry/9C225E` | 154 | 106 | 151 | y≥157 |
| 猴子·皮皮 | `brown/65392D` | 157 | 110 | 154 | y≥160 |
| 猫头鹰·咕咕 | `violet/B672FD` | 153 | 113 | 150 | y≥156 |
| 刺猬·球球 | `lime/92EF4D` | 155 | 110 | 152 | y≥158 |
| 乌龟·慢慢 | `mint/30D183` | 141 | 116 | 138 | y≥144 |
| 鳄鱼·咔咔 | `forest/0C7E1C` | 145 | 122 | 142 | y≥148 |
| 兔子·糖糖 | `pink/FFC5E0` | 157 | 115 | 154 | y≥160 |
| 小猪·哼哼 | `peach/FFCAA9` | 153 | 106 | 150 | y≥156 |
| 绵羊·咩咩 | `pale-mint/C5FFE4` | 145 | 116 | 142 | y≥148 |

### 对照图与复现

- [18 位总览](qa/all-18-preview.png)：每格上方是三层叠合，下方是花盆＋小苗。
- [第一批六位对照](qa/batch-1-comparison.png)
- [第二批六位对照](qa/batch-2-comparison.png)
- [第三批六位对照](qa/batch-3-comparison.png)
- 对照图列顺序：原图 / body→pot→leaves / pot＋sprout / body / leaves。
- [验证数据](qa/validation.json)、[输入校验和](qa/input-sha256.json)。
- 复现导出：`python3 Art/PlushToy/PotSprout-All-20261030/compose.py --deliver`。脚本只做蒙版、像素提取、注册与合成；创作补绘由 image_gen 完成。

本次仅交付美术文件与定位数据，未修改游戏运行代码。

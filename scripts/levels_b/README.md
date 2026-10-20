# 关卡第二套（B 套）工具

工具实施计划见 `docs/r1/关卡第二套-实施计划.md`，分战区制作安排见 `docs/r1/关卡第二套-执行计划-总览.md`，出图提示词与出图规则见 `docs/r1/关卡第二套-出图提示词.md`。关号一律是**游戏关号**：第 N 关 = `sector{N-2}`，B 套覆盖第 3–262 关。

| 脚本 | 用途 |
|---|---|
| `palette_table.py` | 全关配色总表：`Art/levels-b/palette-table.csv`（Excel/Numbers 可打开）、`.json`，以及每个战区一张对照图 `palette-sheet-zone<N>.png` |
| `convert.py` | 图片 → B 关草稿：量化、颜色置换、补齐、校验、预览 |
| `zone_data.py` | 某战区每关的原关数据（颜色、底部构成、各色最低排、队员、补给），写题材用；`--summary` 给概况和建议试点 |
| `screen.py` | 候选图批量筛查：逐张转换（不写清单），判「可用」（补齐不超限且下 10/20 排差异各 ≤0.20），给出画面颜色多少、底部构成和对照图，用来挑图和写修改意见；`--only-s` 只用模式 S，颜色名就是画面里的颜色 |
| `make_temp.py` | 用 A 套原图生成临时图（翻转 / 换色 / 模糊），开发测试用 |
| `selftest.py` | 自测：原文件逐字节重写、哈希口径、校验拦截、临时图端到端 |
| `publish.py` | 验收（`--approve`）、写入已验收关卡到本地 `Resources/levels/B/` 并生成 `released.json`、构建前检查（`--check`，`build.sh` 自动运行）；不编译、不部署线上 |
| `stage_test.py` | 测试通道：把未验收的草稿（或 `--temp` 翻转图）放进 `Resources/levels/B-test/`；`--clean` 清除 |
| `check_web.sh` | 网页实测验收：逐关以 `?level=N&set=B` / `set=A` 打开构建包，核对实际加载来源、文件及棋盘哈希，并验证无 B 文件的关回退 A |
| `structure.py` | 第 4 步检查 D：不靠游戏和机器人的结构对比（前 3/10/20 排颜色构成差异；"理想打法"回放的清完步数、卡住、盲出、救场），全部 260 关约 50 秒 |
| `evaluate.py` | 第 4 步机器人粗筛：Unity 批处理里用 4 种打法、多组固定种子把 A、B 关各打到底，按胜率和复活率差异判定，其他指标供参考；同一草稿的结构检查和粗筛均通过才标为 `evaluated` |
| `check_unity.py` | Unity 验收：实际转换结果构造 `Battle`、非法数据拒绝、Python/C# 哈希互验 |
| `check_trace.py` | 真实 Unity 日志开关对照：同关、同策略、同种子，除日志外所有结果字段必须一致；不改清单 |
| `conversion_guards.py` / `diagnose.py` | 转换预算与主体保护 / 对局诊断报告；不调整玩法或难度门槛 |
| `test_release_checks.py` / `test_check_web.mjs` | 发布与网页验收回归：拦截额外资源、旧包、异常及未完成检查，使用临时目录和模拟连接 |

## 常用

```bash
# 配色总表（挑题材、写提示词用）
python3 scripts/levels_b/palette_table.py

# 一张图转成第 3 关的 B 关草稿
python3 scripts/levels_b/convert.py --level 3 --image ~/Downloads/octopus.png --title 泡泡章鱼

# 自测
python3 scripts/levels_b/selftest.py

# Unity 批处理验收（先关闭打开本项目的 Unity 编辑器）
python3 scripts/levels_b/check_unity.py

# 发布与网页验收脚本的故障回归（不启动浏览器）
python3 scripts/levels_b/test_release_checks.py
node --test scripts/levels_b/test_check_web.mjs
```

## Unity 验收

`check_unity.py` 将第 3、7、11、40、81 关分别生成翻转、置换、模糊临时图，再调用转换工具，覆盖 S/P 两种模式、沙流、问号队员和九色棋盘。

- 15 个转换结果经 Unity `JsonUtility` 解析后，实际构造 `Battle`，核对泡泡总数、队列数、队员数和各色弹量。
- 另加一个缺少格子的非法棋盘，确认构造器拒绝。
- 逐文件核对 `fileSha256` 和解析后的 `cellsSha256`；固定数组、空数组、带符号整数三组样例在 `en-US`、`ar-SA` 两种区域设置下共核对 6 次。
- 输入、转换预览、Unity 日志和 `report.json` 保存在新的 `Builds/R1-evidence/levels-b-construction-时间/` 中。可用 `--out 新目录` 指定位置，`--unity 编辑器路径` 指定 Unity；默认版本为 2022.3.62f3。
- 测试使用独立目录，不写制作清单、Resources 或玩家存档，也不构建 WebGL。Python 自测使用系统临时目录，结束后自动清理。

## convert.py 说明

- **量化**：
  - 非正方形图默认居中裁成正方形，`--fit pad` 改为补边；
  - 默认把图放大到 600 px，每 10×10 格投票决定一个泡泡，像素画边缘更干净；`--resample box` 改为直接缩小。
  - 颜色只用本关已有的颜色，按 Lab 色差取最近色。
- **搜索与置换**：
  - 默认搜索缩放、平移、左右翻转，同时比较模式 S（不置换）和模式 P（按图中各色多少自动配对目标格数）。
  - `--only-s`：搜索时只用模式 S；写改图意见时使用，避免混淆画面颜色和置换后的颜色。
  - `--auto-sigma`：搜索时只尝试自动置换；若配对结果恰好是原色，最终报告仍记为 S。
  - `--sigma 3:10,10:3`：手动指定 A 索引 → B 索引，跳过自动搜索。
  - `--no-search`：直接转换原图；不指定置换参数时用 S，配合 `--auto-sigma` 或 `--sigma` 时仍按对应方式置换。
  - 候选先同时满足逐色补齐、总补齐、D10/D20，再按精确改动数排序；全部失败时按违规项数及超限程度展示较近的方案，不将其判为通过。
  - 默认每种模式先试两档缩放；未找到全部通过的方案时，继续尝试其他档。超过补齐门槛的格数差下界可直接剪枝。
  - `--fine-search`：缩放步长 0.005、平移步长 1 格；范围仍为 0.8–1.1 倍、±3 格。更精细不代表一定有可用方案。
  - 置换同时作用于棋盘、沙流、队员颜色；`queue[].other` 是数值参数，不动。
- **补齐**：
  - 只在颜色交界处逐格改色，直到每种颜色的格子数与目标完全一致；
  - 预览图里黑圈标出改动的格子。
  - 任一颜色改动超过目标的 15%（小色块至少允许 10 格），或总改动超过 150 格，就判为"超限"，退出码 2，建议改图。门槛与 `levelsb_common.py` 一致，执行时不自行放宽。
  - 小于 15 格的连通色块、细轮廓端点改为硬保护；每格最多改一次，减少中间颜色反复改动。保护范围内无解时停止，不能牺牲眼睛、触手等来凑数。
  - 现有颜色只沿颜色边界调整；图中完全缺失的目标色可在未保护、允许改色的区域选一个起点。
  - 补齐是贪心搜索，失败不等于数学上无解；数字通过也不代替视觉检查。
- **主体保护配置**（可选 `--constraints 文件.json`）：
  - 配置必须绑定该源图的 `sourceSha256`，仅支持正方形图片。矩形使用源图 60×60 坐标，左上原点，格式 `[左,上,右,下]`，右下边界不含在内；随缩放、平移、镜像变换。
  - `protectRects`：任何采样点碰到保护矩形的格子都不能补色。
  - `editableRects`：可选；格子全部采样点都在这些矩形并集内才允许补色。未给出则由保护区限制。
  - `subjectRects`：可选；指定需完整保留的主体，防止自动缩放/平移裁切。默认保守地把边框主色之外的颜色全部视为前景。背景海面、云等不宜套用主体边界时，应显式标出主体，人工核对矩形覆盖完整。
  - `recolorFrom`：可选，例如 `"13": [6]` 表示新补的 13 色只允许替换量化原图中的 6 色，防止身上斑纹扩散到背景。索引按画面颜色，不按置换前的 A 角色。
  - 示例配置在 `Art/levels-b/constraints/`。配置不是关卡数据，不改变目标色数、队列或难度门槛。最终仍需视觉检查；非正方形图片的显式 `--fit crop` 会先裁切源图，主体取景检查不覆盖这个预处理，报告单列 `sourceCropped`。
  - `screen.py --constraints-dir Art/levels-b/constraints --fine-search ...` 按图片同名 `.json` 查找配置；未找到时使用默认保护。转换失败另存 `errors.json`，不得把失败图片从待办中漏掉。
- **输出**：在 `Art/levels-b/work/第NNN关_sectorXX/` 下生成：
  - `sectorXX.json`：B 关草稿，格式与原文件一致；
  - `第NNN关_sectorXX.png`：A 原关 | 量化后 | B 新关，下方是各色格数表；
  - `第NNN关_sectorXX.report.json`：模式、置换、源图哈希、`fileSha256`、`cellsSha256`、各色改动。
  - 新增 `mixPass`、`conversionPass`、保护配置与哈希、候选逐色预算及裁切/补齐/下界拒绝计数。退出码 0 现在要求补齐和 D10/D20 都通过；2 为检查未过；1 为未能生成精确补齐结果或输入错误。
- **清单**：`Art/levels-b/manifest.json` 中对应条目写为 `draft`；隔离测试可加 `--no-manifest`，只输出文件，不改制作清单。

## 哈希口径

- `fileSha256`：关卡 json 文件的原始字节。
- `cellsSha256`：`cells` 用英文逗号连接的十进制整数（无空格，UTF-8）。
  - 样例 `[3,3,10,0,17]` → `1a66d125348a5c96f8204b61572a95c421d67a0f668e51b4517d9805f0b0dc1d`
  - C# 实现位于 `UnityProject/Assets/Flightbeat/Garden/GardenLevelHashes.cs`，使用固定区域格式；已由 Unity 批处理与 Python 互验。

## 目录

- `Art/levels-b/work/`：草稿，游戏读不到。
  - `_temp/` 用于手动生成临时图；旧版 `_selftest/` 也继续被 git 忽略。自动自测使用独立临时目录。
- `Builds/R1-evidence/levels-b-construction-时间/`：Unity 构造与哈希验收证据。
- `UnityProject/Assets/Flightbeat/Resources/levels/B/`：只放已验收的关，由 `publish.py` 写入（第 3 步实现）。
- `UnityProject/Assets/Flightbeat/Resources/levels/B-test/`：测试通道（第 3 步实现），已被 git 忽略，正式构建禁止带入。

## 游戏接入（第 3 步）

- **运行时**：`Garden/PixelWingGame.LevelSet.cs`。
  - 按 `released.json`（以及测试通道的 `test.json`）把 `Levels` 中对应关名换成 `B/sectorNN` 或 `B-test/sectorNN`。
  - 文件不存在或哈希不符的关保留 A。切回 A 时恢复原名。
  - 不改任何受保护文件。
- **选择从哪来**（优先级从高到低）：
  1. 启动参数 `--level-set B|A`：验收用，本次有效；
  2. 网址参数 `set=B|A`：试玩用，本次有效，例如 `?level=5&set=B`；
  3. 存档键 `garden.levelSet`：设置里的「新图案关卡」开关。
  - 自动化回归运行时（evidenceControlled）固定 A，除非显式传了 `--level-set`。
- **设置**：首页设置里的「新图案关卡」开关，只在有 B 关卡时出现，下次进关生效。
- **选关**：首页右侧「选关」入口。
  - 每页 30 关，可进入任意已解锁的关重玩；
  - 开启新图案时，有新图案的关带「新」角标。
- **网页指标**：`window.gardenR1` 新增 `levelSet`、`levelName`、`levelSource`（A / B / B-test）、`fileSha256`（资源原始字节）、`cellsSha256`（实际解析的棋盘）、`expectedFileSha256` / `expectedCellsSha256`（清单值，A 套为空）。来源和哈希按对局记录，切换下局选择不会改变当前对局的来源记录。
- **网页验收**：文件哈希与棋盘哈希都要和本地验收清单一致；A 套及回退关与原文件一致。报告记录期望值、实际值、预期与完成用例数；任何异常或未完成都记为失败。正式 B、B-test、回退 A 分开计数。完整检查通过才返回退出码 0。
- **发布检查**：递归检查 `Resources/levels/B/` 的全部资源，只允许 `released.json`、清单中的关卡文件及对应 `.meta`；额外扩展名、子目录资源、孤立 `.meta` 和符号链接均拒绝。

## 测试流程

```bash
python3 scripts/levels_b/stage_test.py --temp 3 7 40      # 翻转图做测试关
R1_LEVELS_TEST=1 bash scripts/r1v3/build.sh                # 测试构建（产物带 LEVELS_TEST_BUILD.txt，不可发布）
bash scripts/levels_b/check_web.sh                         # 网页实测：来源与哈希
python3 scripts/levels_b/stage_test.py --clean             # 测完清除，正式构建才会通过检查
```

## 难度评估（第 4 步）

```bash
python3 scripts/levels_b/evaluate.py --self 3 7          # 同种子 A 对 A：必须完全相同（确定性）
python3 scripts/levels_b/evaluate.py --noise 3 7 40      # 不同种子 A 对 A：机器人自身的波动，用来定容差
python3 scripts/levels_b/evaluate.py --temp 3 7 40       # 翻转图对 A：检验流程
python3 scripts/levels_b/evaluate.py 3 7                 # 正式：清单草稿对 A，通过的标为 evaluated
python3 scripts/levels_b/evaluate.py --trace --no-manifest 20  # 对当前草稿做隔离诊断
python3 scripts/levels_b/evaluate.py --candidate-dir /path/to/candidates --trace --no-manifest 29 30
python3 scripts/levels_b/check_trace.py 20               # 真实 Unity，日志开关对照
```

- 先关闭打开本项目的 Unity 编辑器。
- 打关代码：`Editor/LevelsBDifficultyChecks.cs`，与证据运行同一配置：
  - 空装备、默认武器，不用道具；
  - 0.02 秒步长；
  - 用掉每局一次的复活，决战一就绪就发动；
  - 900 秒或 120 秒无进展判超时。
- 打法：0 内置建议、1 优先攻击、2 优先合成（三合一条件，问号队员按真实颜色）、3 优先清位。
  - 策略 1–3 的最后一个空位只留给能三合一或暴露 ≥3 列的颜色。
- **多次运行**：单次确定性结果对排布极敏感，所以每种打法跑 `--runs` 次（默认 12）。
  - 每次带可复现的"人手"波动：决策间隔 0.4–0.8 秒、同等选择随机、15% 犹豫；
  - A、B 用相同种子。
- 判定（17:40 起只做**粗筛**）：
  - 校准结果：`--noise`（原关换种子）的胜率最多相差 33 个百分点、用时差 11%，和多数真实差异一样大。
  - 当前规则：预期用例缺失、重复或多出时直接报错退出；正常对比中有超时或异常局则为「无法判断」；其余情况下，任一打法的胜率或复活率相差 ≥50 个百分点判超差，否则「通过（粗筛）」。用时、卡住、距离、清除比例只列在报告里供参考。
  - `--self` 单独按同种子的结果字段一致性判定（排除 `name`、`path`），不用粗筛容差。
  - 细的难度一致性看 `structure.py` 加人工试玩。
  - 历史校准记录：当时按胜率粗筛规则重判已有 `--noise` 与 `--temp`（第 3、7、40 关）结果均通过；这不代表已按当前完整规则重新运行。
- 输出：`Builds/R1-evidence/levels-b-eval-…/report.md`、`report.json`、Unity 日志。
  - `--trace` 额外输出 `diagnostics.md`；`results.json` 保留开局前 20 次决策、首次卡住/复活/终局前最多 16 次决策及事件快照。
  - 卡住沿用游戏 `Stalled` 判定，不另计时。有空位但机器人未选择队员单列为观察事件，不计入卡住时间。
  - 日志只读前排索引、队列和上阵数据，不调用有副作用的 `ExposedAt` / `SuggestedQueue`，不额外取随机数；暴露列数口径仅适用于本评估空装备、无穿透条件。
  - `--no-manifest` 不更新制作状态；`--candidate-dir` 从指定目录读取 `sectorNN.json`，强制配合该开关，并禁止与 self/noise/temp 混用。独立目录只用于诊断，不是正式验收凭证。
  - Unity 进程失败、总报告未成功时不写评估状态，即使残留了结果文件也不能当成功。
- `publish.py --approve` 默认要求状态为 `evaluated`；无法判断的关经人工试玩后，用 `--without-evaluation` 验收，清单会记下这一点。

### 历史记录：多次评估版本的审查限制（2026-10-19）

以下保留当时的审查结论，描述的是该次源码快照，不代表当前版本的完整状态。当前判定见上文：已增加预期用例名称的完整性检查、异常局拦截、复活率比较及 `--self` 结果字段比较；这些修改不等于历史审查中的所有问题均已修复或重新实测。

- 当时的 `evaluate.py` 没有校验 Unity 成功状态和完整的预期用例/种子集合，统计出的超时、异常数也没有阻断通过。隔离故障注入已复现异常或缺局仍写入 `evaluated`，不是实际 Unity 对局发生了这些问题。
- 复活率只显示、不参与判定，尚未保留原先的复活差异约束；`--self` 也只比较部分字段。
- 同等候选采用逐个 50% 替换，存在偏向后面列的概率差异，尚非均匀随机。
- 当次真实运行另行保存 `integrity-check.json` 和全字段一致性核对，保留原始报告；当时的独立核对不代表上述实现已修复。当时阈值仍待校准，审查结论是不把该版结果当成正式难度验收依据。
- [审查与复现记录](../../Builds/R1-evidence/levels-b-multirun-review-20261019-165803/review.md)。复现使用隔离的模拟数据，与真实 Unity 实跑证据分开。

### 历史记录：默认 12 次的真实 Unity 实跑（2026-10-19）

下表及分析保留该次运行使用的旧判定结果，不按当前粗筛规则改写；其中「超差／无法判断」不代表当前版本重跑后的结论。

| 命令 | 对局数 | 总耗时 | 退出码与结论 |
|---|---:|---:|---|
| `--self 3 7` | 192 | 301.814 秒 | 0；两关通过，96 组所有结果字段独立核对一致 |
| `--noise 3 7 40` | 288 | 611.348 秒 | 2；第 3 关通过，第 7 关无法判断，第 40 关超差 |
| `--temp 3 7 40` | 288 | 630.386 秒 | 2；第 3 关通过，第 7、40 关超差 |

- 共 768 局，约 25 分 44 秒；无运行错误、超时、缺局或种子错配，跨进程的 A 原关结果一致。
- 噪声：第 40 关内置建议由 0/12 胜到 4/12 胜，仅换种子即超差；当时阈值尚未覆盖本样本的自身波动。第 7 关优先攻击两边均 0/12，仍无法判断。
- 翻转：第 40 关优先合成由 9/12 胜降至 5/12 胜，对应噪声为 9/12 对 9/12；第 7 关内置建议胜率均 12/12，却因卡住、最近距离中位数超差。需继续检查机器人与指标，不能直接把该次通过/超差当成人工难度等价结论。
- [自检报告](../../Builds/R1-evidence/levels-b-eval-self-20261019-170044/report.md)、[噪声报告](../../Builds/R1-evidence/levels-b-eval-noise-20261019-170546/report.md)、[翻转图报告](../../Builds/R1-evidence/levels-b-eval-temp-20261019-171557/report.md)、[并排对照](../../Builds/R1-evidence/levels-b-multirun-review-20261019-165803/comparison.md)。源码快照、完整性检查及耗时在各运行目录；本轮未修改正式草稿状态。

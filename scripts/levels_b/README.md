# 关卡第二套（B 套）工具

计划见 `docs/r1/关卡第二套-实施计划.md`。关号一律是**游戏关号**：第 N 关 = `sector{N-2}`，B 套覆盖第 3–262 关。

| 脚本 | 用途 |
|---|---|
| `palette_table.py` | 全关配色总表：`Art/levels-b/palette-table.csv`（Excel/Numbers 可打开）、`.json`，以及每个战区一张对照图 `palette-sheet-zone<N>.png` |
| `convert.py` | 图片 → B 关草稿：量化、颜色置换、补齐、校验、预览 |
| `make_temp.py` | 用 A 套原图生成临时图（翻转 / 换色 / 模糊），开发测试用 |
| `selftest.py` | 自测：原文件逐字节重写、哈希口径、校验拦截、临时图端到端 |
| `publish.py` | 验收（`--approve`）、发布已验收关卡到 `Resources/levels/B/` 并生成 `released.json`、构建前检查（`--check`，`build.sh` 自动运行） |
| `stage_test.py` | 测试通道：把未验收的草稿（或 `--temp` 翻转图）放进 `Resources/levels/B-test/`；`--clean` 清除 |
| `check_web.sh` | 网页实测验收：逐关以 `?level=N&set=B` / `set=A` 打开构建包，核对实际加载来源、文件及棋盘哈希，并验证无 B 文件的关回退 A |
| `structure.py` | 第 4 步检查 D：不靠游戏和机器人的结构对比（前 3/10/20 排颜色构成差异；"理想打法"回放的清完步数、卡住、盲出、救场），全部 260 关约 50 秒 |
| `evaluate.py` | 第 4 步难度评估：Unity 批处理里用 4 种打法把 A、B 关各打到底，逐关比较胜负、用时、卡住时长、最近距离；通过的草稿标为 `evaluated` |
| `check_unity.py` | Unity 验收：实际转换结果构造 `Battle`、非法数据拒绝、Python/C# 哈希互验 |
| `test_release_checks.py` / `test_check_web.mjs` | 发布与网页验收回归：拦截额外资源、旧包、异常及未完成检查，使用临时目录和模拟连接 |

## 常用

```bash
# 配色总表（挑题材、写提示词用）
python3 scripts/levels_b/palette_table.py

# 一张图转成第 3 关的 B 关草稿
python3 scripts/levels_b/convert.py --level 3 --image ~/Downloads/octopus.png --auto-sigma --title 泡泡章鱼

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
- **置换（模式 P）**：
  - `--auto-sigma`：按图中各颜色的多少自动配对目标格数；
  - `--sigma 3:10,10:3`：手动指定 A 索引 → B 索引；
  - 不指定就是模式 S（不置换）。
  - 置换同时作用于棋盘、沙流、队员颜色；`queue[].other` 是数值参数，不动。
- **补齐**：
  - 只在颜色交界处逐格改色，直到每种颜色的格子数与目标完全一致；
  - 预览图里黑圈标出改动的格子。
  - 任一颜色改动超过目标的 10%（小色块至少允许 4 格），或总改动超过 120 格，就判为"超限"，退出码 2，建议重画或换置换。
- **输出**：在 `Art/levels-b/work/第NNN关_sectorXX/` 下生成：
  - `sectorXX.json`：B 关草稿，格式与原文件一致；
  - `第NNN关_sectorXX.png`：A 原关 | 量化后 | B 新关，下方是各色格数表；
  - `第NNN关_sectorXX.report.json`：模式、置换、源图哈希、`fileSha256`、`cellsSha256`、各色改动。
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
  - 规则：任一打法胜率相差 ≥ 50 个百分点判超差，否则"通过（粗筛）"；用时、卡住、距离、清除比例只列在报告里供参考。
  - 细的难度一致性看 `structure.py` 加人工试玩。
  - 按这个规则重判已有结果：`--noise` 与 `--temp`（第 3、7、40 关）全部通过，没有误报。
- 输出：`Builds/R1-evidence/levels-b-eval-…/report.md`、`report.json`、Unity 日志。
- `publish.py --approve` 默认要求状态为 `evaluated`；无法判断的关经人工试玩后，用 `--without-evaluation` 验收，清单会记下这一点。

### 多次评估版本的审查限制（2026-10-19）

- 当前 `evaluate.py` 没有校验 Unity 成功状态和完整的预期用例/种子集合，统计出的超时、异常数也没有阻断通过。隔离故障注入已复现异常或缺局仍写入 `evaluated`，不是实际 Unity 对局发生了这些问题。
- 复活率只显示、不参与判定，尚未保留原先的复活差异约束；`--self` 也只比较部分字段。
- 同等候选采用逐个 50% 替换，存在偏向后面列的概率差异，尚非均匀随机。
- 本次真实运行另行保存 `integrity-check.json` 和全字段一致性核对，保留原始报告；独立核对不代表上述实现已修复。当前阈值仍待校准，修正前不把它当成正式难度验收依据。
- [审查与复现记录](../../Builds/R1-evidence/levels-b-multirun-review-20261019-165803/review.md)。复现使用隔离的模拟数据，与真实 Unity 实跑证据分开。

### 默认 12 次的真实 Unity 实跑（2026-10-19）

| 命令 | 对局数 | 总耗时 | 退出码与结论 |
|---|---:|---:|---|
| `--self 3 7` | 192 | 301.814 秒 | 0；两关通过，96 组所有结果字段独立核对一致 |
| `--noise 3 7 40` | 288 | 611.348 秒 | 2；第 3 关通过，第 7 关无法判断，第 40 关超差 |
| `--temp 3 7 40` | 288 | 630.386 秒 | 2；第 3 关通过，第 7、40 关超差 |

- 共 768 局，约 25 分 44 秒；无运行错误、超时、缺局或种子错配，跨进程的 A 原关结果一致。
- 噪声：第 40 关内置建议由 0/12 胜到 4/12 胜，仅换种子即超差；当前阈值尚未覆盖本样本的自身波动。第 7 关优先攻击两边均 0/12，仍无法判断。
- 翻转：第 40 关优先合成由 9/12 胜降至 5/12 胜，对应噪声为 9/12 对 9/12；第 7 关内置建议胜率均 12/12，却因卡住、最近距离中位数超差。需继续检查机器人与指标，不能直接把当前通过/超差当成人工难度等价结论。
- [自检报告](../../Builds/R1-evidence/levels-b-eval-self-20261019-170044/report.md)、[噪声报告](../../Builds/R1-evidence/levels-b-eval-noise-20261019-170546/report.md)、[翻转图报告](../../Builds/R1-evidence/levels-b-eval-temp-20261019-171557/report.md)、[并排对照](../../Builds/R1-evidence/levels-b-multirun-review-20261019-165803/comparison.md)。源码快照、完整性检查及耗时在各运行目录；本轮未修改正式草稿状态。

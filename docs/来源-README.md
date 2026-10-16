# 植物守卫菜园（基于 PixelWing 复刻工程）

这个仓库现在是**换包装项目**：在已经 1:1 复刻完成的 PixelWing Unity 工程上，把战机射击包装换成"植物守卫菜园"。玩法、关卡、数值不变，只换表现层。

- 当前阶段：**R1，前 4 关可玩**。2026-10-15 主方向从植物改为**泡泡**，见 [v3 计划](docs/r1/v3-泡泡方向.md)；植物 v2 保留作对照，见 [v2 计划](docs/r1/v2-计划.md)。目标、步骤和验收见 [docs/复刻实施计划.md](docs/复刻实施计划.md) 第 3.2 节和 [docs/创意方案-植物守卫菜园.md](docs/创意方案-植物守卫菜园.md) 第 19 节。
- 复刻阶段（PixelWing 1:1 复刻）已于 2026-10-12 交付，资料已移到仓库外的归档目录，见下方"复刻资料在哪"。

## 目录

| 位置 | 内容 |
| --- | --- |
| `UnityProject/` | Unity 工程（2022.3）：游戏源码、资源、场景、测试。换包装的开发都在这里 |
| `Builds/WebGL/WebGLRelease-v1/` | 复刻完成的 WebGL 包（原版 PixelWing 外观），换包装时用来对照 |
| `Art/Concepts/` | 美术概念图和生图提示词。当前植物定稿在 `plant-characters-v05/` |
| `docs/创意方案-植物守卫菜园.md` | 换包装创意方案：世界观、植物与颜色对应、美术方向、版权红线、第一阶段计划 |
| `docs/游戏系统梳理-植物守卫菜园.md` | 按换包装后的叫法梳理各个游戏系统，附源码位置 |
| `docs/复刻实施计划.md` | 执行计划：复刻阶段的全部任务卡，以及换包装阶段 R1 |
| `scripts/replication.sh` | 把复刻阶段资料临时接回项目的工具，见下文 |

## R1 当前状态：v3（主方向改为泡泡，计划中）

2026-10-15 用户确认：R1 的包装从植物菜园改为泡泡小队，玩法、关卡、数值不变。主示意图为 `Art/Concepts/bubble-squad-v02/01-smiley-intro.png`（第 2 关），空出战位保留金色漩涡表现。决定、目标稿和原型步骤见 [v3 计划](docs/r1/v3-泡泡方向.md)；题材探索见 `Art/Concepts/bubble-squad-v01/`、`bubble-squad-v02/`。

泡泡版的构建和试玩（输出目录固定，每次构建覆盖）：

```sh
bash scripts/r1v3/build.sh        # 图集 → 重建界面 Prefab → 源码边界检查 → WebGL，输出 Builds/WebGL/GardenR1-bubble
bash scripts/r1v3/play.sh         # 浏览器真实点击打通前 4 关，截图和对照图在 Builds/R1-evidence/play-v3
python3 -m http.server 8785 --bind 127.0.0.1 --directory Builds/WebGL/GardenR1-bubble   # 试玩：http://127.0.0.1:8785/
```

试玩服务可以一直开着，重新构建后刷新页面即可（构建会给文件加版本号，避免读到旧缓存）。

下面 v2 一节是植物方案的最终状态，保留作对照。

## R1 当前状态：v2（回到战斗示意图）

当前分支 `reskin/r1-v2`。2026-10-13 起，界面和植物、花盆都改用战斗示意图 `Art/Concepts/plant-defense-v03/03-cascade-pressure.png` 这一版，目标仍是前 4 关可玩、定下 8 种植物。计划和已确认的决定见 [v2 计划](docs/r1/v2-计划.md)。

此前的 v1 已冻结（Git 标签 `r1-v1-frozen`，分支 `reskin/r1` 不再提交），冻结内容、可复用部分和 v1 游戏包位置见 [v1 冻结清单](docs/r1/v1-冻结清单.md)。

战斗页的背景、顶栏、底部操作、暂停和结算已拆成可编辑 UI Prefab。在 Unity 打开 `Assets/Flightbeat/Resources/GardenR1/UI/GardenBattlePage.prefab`；具体入口、文字和点击区域的修改方式见 [战斗 UI Prefab 维护说明](docs/r1/战斗UI-Prefab维护说明.md)。

v2 沿用 v1 的构建、回归和试玩方式：

```sh
bash scripts/r1/build.sh /绝对路径/全新的输出目录      # 命令行构建
bash scripts/r1/regress.sh 一个全新的批次名            # 原逻辑回归
python3 -m http.server 8784 --bind 0.0.0.0 --directory <输出目录>
```

打开 `http://127.0.0.1:8784/`，加 `?perf=1` 显示性能面板。新档从教学开始；已有存档可在无痕窗口重新体验。v1 的运行、回归和性能结果见 [第一版美术验收记录](docs/r1/第一版美术验收记录.md)，灰盒结果见 [灰盒验收记录](docs/r1/灰盒验收记录.md) 与 [实现说明](docs/r1/灰盒实现说明.md)。

## 运行原版基线

本地试玩复刻版 WebGL 包：

```sh
python3 -m http.server 8780 --bind 127.0.0.1 --directory Builds/WebGL/WebGLRelease-v1
```

然后在浏览器打开 `http://127.0.0.1:8780/`。不要直接双击 `index.html`。

## 复刻资料在哪

复刻阶段的证据、逆向资料、原版发布包、分析和回归工具，2026-10-13 整体移到了仓库旁的归档目录，相对路径不变：

```
../Flightbeat-archive/
├── evidence/sha256/      10 月 10 日迁出的大证据（内容寻址存储）
└── replication/          10 月 13 日迁出的复刻资料
    ├── Evidence/  Reverse/  Analysis/  Tools/
    ├── Build/  index.html  download-manifest.json  下载说明.txt   （原版发布包）
    ├── Builds/Native/  Builds/WebGL/Probe-CF-v1、v2               （旧构建）
    └── ui.md  当前进度一览.md  教学试玩说明.md  README-repo.md     （复刻阶段文档）
```

- 换电脑或备份时，`Flightbeat` 和 `Flightbeat-archive` 两个目录要放在一起带走。单独克隆 Git 不包含这些资料。
- 复刻阶段的详细说明见归档里的 `README-repo.md` 和 `Evidence/reports/文件与证据存放说明.md`。
- 这些文件都在 Git 历史里，旧提交仍能查到；归档目录不是缓存，不能当垃圾清理。

### 需要跑回归检查或复刻工具时

换包装第一阶段要求"原有战斗逻辑回归测试全部通过"，这些工具按旧路径读写资料。运行前先接回，用完撤除：

```sh
bash scripts/replication.sh link     # 以链接方式接回原路径
bash scripts/replication.sh status   # 查看状态
bash scripts/replication.sh unlink   # 撤除链接（不动归档里的文件）
```

接回后的路径已在 `.gitignore` 中忽略，不会显示为改动。新产生的证据会写进归档目录。

迁移时，原来设为只读的冻结证据在复制后变成了可写。如需恢复只读保护，在本机终端运行一次：

```sh
bash scripts/replication.sh freeze
```

## 不能放进项目的东西

- 不要把归档里的资料复制回项目；需要时用 `link`。
- Unity 生成的 `Library/`、`Temp/`、`Logs/`、`UserSettings/` 和 `Builds/` 不进 Git。

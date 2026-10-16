# 泡泡小队 · flight-bubble

从 Flightbeat 的 `reskin/r1-v3-bubble@d53a2cc5` 导出的独立 Unity 项目，包含 2 个教学关和 260 个正式关。战斗、经济计算、关卡数值、道具、复活、广告奖励和终结演出保留来源行为，受保护文件通过 SHA-256 校验。

主页只有「开始」和「设置」。开始进入第一个未通关关卡，全通后进入最后一关。首次新存档沿用来源行为，自动进入教学。项目使用独立产品标识和新存档，初始养成不变；没有机库入口。

## 构建与试玩

依赖：Unity **2022.3.62f3**（含 WebGL Build Support）、Python 3 + Pillow、Node.js 22+、Google Chrome；完整回归另需 `zstd`。脚本中的 Unity 和 Chrome 路径使用 macOS 默认安装目录。

```bash
python3 scripts/r1/check_source.py
bash scripts/r1v3/build.sh
python3 -m http.server 8792 --bind 127.0.0.1 --directory Builds/WebGL/GardenR1-bubble
```

打开 <http://127.0.0.1:8792/>。每次构建替换固定输出目录，资源 URL 带构建时间，刷新即可。不要直接打开 index.html。

- `?level=30`：验收用跳关，接受 1–262；沿用正常进关计算，仅提升本地存档的已解锁关号。
- `?perf=1`：显示并导出只读性能数据。
- 本地进度按浏览器/站点保存。清理该站点存储可重新开始；换端口会使用另一份站点存储。

## 目录

| 路径 | 内容 |
|---|---|
| `UnityProject/` | Unity 工程，主场景 `Assets/Flightbeat/Scenes/PixelWing.unity` |
| `scripts/` | 构建、图集生成、浏览器试玩和回归脚本 |
| `tools/` | 检查器及本地冻结数据，数据不提交 Git |
| `Art/` | 预演图、美术交付、字体、颜色表和占位图 |
| `docs/` | 迁移计划、来源、执行/验收记录、包装覆盖清单 |
| `Builds/` | 构建输出和验收证据，不提交 Git |

## 补角色素材

颜色与命名以 `Art/skin-colors.json` 为准。把透明 PNG 放到：

```text
Art/R1/art-v3/delivery/creatures/<creatureId>/<HEX>/idle.png
Art/R1/art-v3/delivery/creatures/<creatureId>/<HEX>/attack.png
Art/R1/art-v3/delivery/creatures/<creatureId>/<HEX>/spent.png
```

画布 224×256，脚底 y=212，HEX 为不带 # 的大写颜色值。可选 `blink.png`、`cheer.png`；未提供时不使用对应动画。子弹为程序绘制光球，不需要 projectile 素材。沿用交付目录中的美术任务书编排新的素材编号。

重新运行构建脚本：正式图片优先，缺图自动生成到 `Art/generated/placeholders/`；三个必要状态齐备后颜色表状态自动成为 `art`。修改显示名也在颜色表完成，无需手改 C#。生成的角色查找表和双图集随构建更新。

覆盖报告：`docs/包装覆盖清单.md`、`Builds/包装覆盖清单.json`。当前 22 种颜色中 10 种有交付，12 种采用圆形占位；另有用于神秘队员的灰色占位。后两排神秘队员只显示灰色和问号，进入第一排才显示真实颜色。

## 验证

```bash
# 自动鼠标连续通关前四关（证据目录必须不存在）
bash scripts/r1v3/play.sh Builds/R1-evidence/play-new

# 高屏、DPR 1.5、CPU 4 倍限速、真实触摸事件
R1_HEIGHT=1170 R1_DPR=1.5 R1_CPU_RATE=4 R1_TOUCH=1 \
  bash scripts/r1v3/play.sh Builds/R1-evidence/play-touch-new

# 第 5、10、30、100、200、262 关，各实际出战 60 秒
bash scripts/r1v3/sample.sh Builds/R1-evidence/sample-new

# 262 关进关/出战及零体力入口、特效绘制集成检查
bash scripts/r1v3/check_levels.sh Builds/R1-evidence/levels-new

# 第 4 关密集阶段性能 + 第 7 关五跑道，自动检查无并行测试
python3 scripts/r1v3/perf.py Builds/R1-evidence/perf-new

# 全部 39 个冻结用例，包含逐帧数组比较
bash scripts/r1/regress.sh unique-run-name
```

自动浏览器使用 HTTP 8802、CDP 9342 和 `/tmp/flight-bubble/` 中独立 Chrome 用户目录，不影响日常浏览器存档。性能验收前须确认没有其他 Unity 批处理或无头 Chrome 工作；常规并行试玩结果不作为独占性能结果。

冻结数据共约 3.27 GB，由用户批准完整复制且排除 Git。新克隆运行完整回归前，用原归档或它的完整备份补齐：

```bash
python3 scripts/prepare_regression_data.py /path/to/Flightbeat-archive/replication
```

`tools/data-manifest.json` 记录逐文件来源、字节数与 SHA-256。普通构建不需要这些回归数据；不要将大数据加入 Git。

## 已知例外

- 「全歼」终结演出仍使用原版战机画面。
- 开始、重试、下一关在体力不足时补 1 点后走原入口，体力不再阻挡进入。
- 广告沿用来源测试包：立即奖励，不接真实广告平台。
- 暂缺角色使用程序占位图。原版火箭爆炸、穿透火花、武器特效、援军清场作为尚未交付包装素材的绘制补充。
- 部分无入口的外围文件保留，因为其共用方法被战斗引用；详细依赖见执行记录。

来源与提交边界见 `docs/来源.md`；最终验证结果见 `docs/迁移验收记录.md`。迁移只在本地提交，不自动推送或部署。

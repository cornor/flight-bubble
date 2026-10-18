# R1 v3 美术 AI 提示词清单（泡泡小队）

- 日期：2026-10-15
- 配套要求：[美术 AI 任务书](美术AI任务书.md)（尺寸、透明底、角色画布、自查清单）。
- **每次生成都附示意图** `Art/Concepts/bubble-squad-v02/01-smiley-intro.png`。第 0 项和第 3 部分角色另附 `Art/Concepts/bubble-squad-v01/01-cloud-playground.png`；第 3 部分在阵容图确认后再附 `Art/R1/art-v3/lineup/01-lineup.png`；第 10 项另附 `Art/Concepts/bubble-squad-v02/reference/empty-pad-swirl.png`；第 4 部分附对应的原色版本成品。
- 提示词是英文，整段复制，不要删改颜色值。生成后由美术 AI 自己去背景、缩放到交付尺寸、存到 `Art/R1/art-v3/delivery/` 下的对应路径。
- 泡泡阵和它的底板由程序绘制，不在本清单里。

## 总览

| # | 交付文件 | 内容 | 交付尺寸 |
| --- | --- | --- | --- |
| 0 | `Art/R1/art-v3/lineup/01-lineup.png` | 阵容确认图（先做，给用户确认，不进游戏） | 横图 |
| 1 | `scene/background.png` | 战场背景 | 1080×1920 |
| 2 | `scene/ceiling.png` | 泡沫云顶棚 | 1080×80 |
| 3 | `scene/rail.png` | 泡沫护栏 | 1080×44 |
| 4 | `ui/pause.png` | 暂停按钮 | 88×88 |
| 5 | `ui/level.png` | 关卡胶囊（空白） | 196×72 |
| 6 | `ui/progress-frame.png` | 进度条框（空） | 552×56 |
| 7 | `ui/progress-fill.png` | 进度条填充 | 528×36 |
| 8 | `ui/pad.png` | 出战位台面 | 140×70 |
| 9 | `ui/pad-locked.png` | 锁定出战位 | 140×140 |
| 10 | `ui/pad-swirl.png` | 空位金色漩涡（正俯视） | 160×160 |
| 11 | `ui/badge-ammo.png` | 数字牌（空白） | 96×36 |
| 12 | `ui/tier-coin.png` | 合成等级金币（空白） | 40×40 |
| 13 | `ui/crown.png` | MAX 小皇冠 | 48×36 |
| 14 | `ui/queue-panel.png` | 队列玻璃管 | 192×360（九宫格） |
| 15 | `ui/queue-corner.png` | 管口折角（纯白） | 28×28 |
| 16 | `ui/button-shuffle.png` | 按钮：重排 | 310×144 |
| 17 | `ui/button-designate.png` | 按钮：优先出战 | 310×144 |
| 18 | `ui/button-bomb.png` | 按钮：泡泡炸弹 | 310×144 |
| 19 | `ui/badge-count.png` | 按钮数量角标（空白） | 44×44 |
| 20–27 | `creatures/<id>/<主色>/` | 8 个角色：待战、出手、打完、弹体 | 224×256 ×3，64×64 |
| 28–29 | `creatures/blue/42A9FF/`、`creatures/orange/FF9D30/` | 教学关颜色版本 | 同上 |
| 30–45 | `ui2/` | 第二批界面：弹窗、按钮、开关、卡片、小蓝头像、气泡、箭头、横幅 | 见第 5 部分 |
| 46–55 | `fx/` | 第二批特效：泡泡风格，文件名、尺寸、帧数同 v2 | 见第 6 部分 |
| 56 | `scene/stage.png` | 下半部托板（出战位和队列后面），替换程序画的平板 | 1080×772 |
| 57 | `scene/background.png` | **完整背景重画**：上半部天空 + 下半部托板画在同一张图里，取代第 1 项背景和第 56 项托板 | 1080×1920 |
| 58 | `creatures/<id>/<主色>/blink.png` | 10 个颜色版本的眨眼帧（待战图只闭上眼睛） | 224×256 ×10 |
| 59 | `creatures/<id>/<主色>/cheer.png` | 10 个颜色版本的欢呼姿势（过关时用） | 224×256 ×10 |
| 60 | `scene/background.png` | **完整背景第二次重画**：补回示意图护栏上方的浮岛、瀑布、小房子；下半部托板改成和示意图一样四周被云包住，不要硬边方框 | 1080×1920 |
| 61 | `Art/R1/art-v3/lineup/02-lineup-new.png` | 第三批阵容确认图：12 个新角色（先做，给用户确认，不进游戏） | 横图 |
| 62–73 | `creatures/<id>/<主色>/` | 12 个新角色：待战、出手、打完（不交弹体） | 224×256 ×3 |
| 74 | `creatures/<id>/<主色>/blink.png` | 12 个新角色的眨眼帧 | 224×256 ×12 |
| 75 | `creatures/<id>/<主色>/cheer.png` | 12 个新角色的欢呼姿势 | 224×256 ×12 |
| 76 | `creatures/mystery/8C919C/idle.png` | 神秘队员（灰色泡泡茧） | 224×256 |
| 77 | `finale/king-ready.png`、`finale/king-blow.png` | 决战：泡泡大王两种姿势 | 512×512 ×2 |
| 78 | `finale/button.png` | 决战按钮（空白） | 680×172 |
| 79 | `fx/rocket-pop.png` | 大泡泡弹炸开（MAX 队员、决战消除） | 1152×192（6 帧） |
| 80 | `fx/rescue-bubble.png` | 援军泡泡（复活清场） | 128×192 |

## 通用画风段（下文写作 [STYLE]，使用时替换成这一整段）

```text
Match EXACTLY the art style of the attached battle screenshot (the mockup): a bright cheerful cloud-top bubble world for an all-ages mobile casual game, clean colored dark outlines, soft controlled cel shading, bright glossy highlights, juicy saturated colors, rounded capsule-and-glass user interface. Strictly original: no plants, flowers, vegetables, flowerpots, fences, dragons or dinosaurs, nothing resembling any existing game's characters, interface or logos.
```

## 角色段（下文写作 [CAST]，第 0 项和第 3 部分使用）

```text
The cast are plump original bubble creatures, one creature per color. Each body is almost entirely its main color with one small distinctive feature; no clothes, no armor, no weapons, no held objects, no leaves. Clean colored outlines, soft cel shading, one bright highlight. Faces are big-eyed and expressive, each with its own personality (not all frowning).
- blue, main color #2982F4: a round puff with two tiny cheek fins and little feet; earnest, often puffing its cheeks. (copy from the mockup, deployment pad 1)
- orange, main color #FF7E27: a pear-shaped creature with two short rounded horn nubs; mischievous grin. (copy from the mockup, pad 3)
- green, main color #62E18E: a squat puffy creature with three short soft bumps on its back; good-natured. (copy the green creature from the second attached image, recolored to #62E18E)
- purple, main color #BD80FF: a bean-shaped creature with two floppy ear nubs and a broad elastic mouth; goofy. (copy the purple creature from the second attached image, recolored to #BD80FF)
- yellow, main color #FFDF48: a soft rounded cube with two tiny antennae; cheerful, winking. (copy from the mockup, pad 2)
- white, main color #FFFFFF: a round pill shape with a cool gray outline; shy. (copy from the mockup queue)
- red, main color #FF525B (pink-leaning red, NOT orange-red): a small inverted-heart shape with a little quiff of hair; hot-tempered. (copy from the mockup queue)
- charcoal, main color #2D2D2D: a round puff covered in short soft spikes like a tiny sea urchin, a LIGHT gray outline so it reads on dark backgrounds, bright white eyes; cool and grumpy. (copy from the mockup, pad 4)
Color rule: the lit and mid areas of each body are exactly its main color hex; shadows only darken it without shifting the hue; white stays cool white, never cream.
```

---

## 0. 阵容确认图（先做）

### 0. 阵容确认图（先做，给用户确认造型，不进游戏）

- 交付：`Art/R1/art-v3/lineup/01-lineup.png`，横图，浅灰底，8 个角色两行。附示意图和 v01 图。

```text
[STYLE]

Character lineup sheet. [CAST]

Layout: landscape image, two rows of four characters, all in their READY pose (standing, alert, looking slightly upward), same scale, feet on one baseline per row, generous empty space between characters, on a flat plain very light cool gray background (#EEF1F5).
Row 1, left to right: blue, orange, green, purple. Row 2, left to right: yellow, white, red, charcoal.
Each character must read clearly by silhouette alone: fins, horns, back bumps, floppy ears, antennae, pill shape, quiff, spikes.
No pads, no ground, no shadows, no scenery, no text, no letters, no numbers, no watermark.
```

---

## 1. 场景

### 1. 战场背景

- 交付：`scene/background.png`，1080×1920，不透明。按 9:16 生成后缩放。

```text
[STYLE]

Empty battle background layer, portrait 9:16. ONLY the background: NO user interface, NO buttons, NO bars, NO characters, NO bubbles or beads, NO pads, NO panels, NO text.
Layout from top to bottom (percent of image height):
- 0% to 9%: soft bright sky with small fluffy clouds; a tiny distant fairy-tale castle and a short rainbow arc in the top-right corner; the left and middle stay calm (interface will sit there).
- 9% to 58%: calm plain light sky, almost empty, only very faint soft clouds near the left and right edges (the game draws its own panel over the middle).
- 58% to 72%: distant floating grassy sky-islands with small waterfalls at the far left and far right, small and soft, leaving the middle open; a few faint bubbles drifting.
- 72% to 100%: thick soft layered clouds filling the bottom, slightly brighter at the very bottom; a few colorful glossy bubbles at the far corners; the middle stays clear and calm.
No text, no letters, no numbers, no watermark.
```

### 2. 泡沫云顶棚

- 交付：`scene/ceiling.png`，1080×80，透明。

```text
[STYLE]

A single horizontal game sprite: a thick soft band of puffy foam cloud spanning the FULL width edge to edge, like the cloud band directly above the bubble mosaic in the mockup. White to very light blue, soft shading, small glossy foam bubbles in it; the bottom edge is fluffy and irregular, the top edge can be flatter. Seamless left and right ends. Wide and short (about 13.5 times wider than tall).
Transparent background, clean edges, no shadow. No text, no letters, no numbers, no watermark.
```

### 3. 泡沫护栏

- 交付：`scene/rail.png`，1080×44，透明。

```text
[STYLE]

A single horizontal game sprite: a thin luminous foam defense rail spanning the FULL width edge to edge, exactly like the glowing foam line between the bubbles and the creatures in the mockup: a softly glowing pale cyan-white strip made of tiny connected foam bubbles with a gentle wavy texture. Seamless left and right ends. Very wide and very thin (about 25 times wider than tall).
Transparent background, the glow fades out softly into transparency. No text, no letters, no numbers, no watermark.
```

---

## 2. 界面部件

### 4. 暂停按钮

- 交付：`ui/pause.png`，88×88。

```text
[STYLE]

A single game UI icon button: a round glossy blue bubble button (like the pause button in the top-left of the mockup) with two thick white rounded vertical bars in the middle, a bright highlight at the top-left and a slightly darker rim. Centered, filling the image.
Transparent background, no shadow outside the button. No text, no letters, no numbers, no watermark.
```

### 5. 关卡胶囊（空白）

- 交付：`ui/level.png`，196×72。

```text
[STYLE]

A single game UI element: a blank rounded capsule label plate, like the level label next to the pause button in the mockup: dark slate-blue, semi-translucent, a soft lighter rim and a subtle top highlight. The middle is completely EMPTY (the game writes the level name there). About 2.7 times wider than tall.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 6. 进度条框（空）

- 交付：`ui/progress-frame.png`，552×56。

```text
[STYLE]

A single game UI element: an EMPTY horizontal progress bar frame, like the progress bar at the top of the mockup: a rounded capsule with a soft light rim and a dark slate-blue recessed inner track. The track is completely empty (no fill, no percentage). About 10 times wider than tall.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 7. 进度条填充

- 交付：`ui/progress-fill.png`，528×36。

```text
[STYLE]

A single game UI element: the fill of a progress bar, a long bright glossy green capsule bar with rounded ends, a light highlight stripe along the top and slightly darker green at the bottom, like the green fill in the mockup's progress bar. Uniform along its length so it can be cropped horizontally. About 15 times wider than tall.
Transparent background. No text, no letters, no numbers, no watermark.
```

### 8. 出战位台面

- 交付：`ui/pad.png`，140×70。

```text
[STYLE]

A single game sprite: an EMPTY deployment pad seen from slightly above, exactly like the round pads the creatures stand on in the mockup: a flat ellipse made of a glossy light-blue soap-bubble ring with a pale, softly glowing top surface. Nothing standing on it. Exactly twice as wide as tall.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 9. 锁定出战位

- 交付：`ui/pad-locked.png`，140×140。下半部的台面和第 8 项同样大小、同样位置。

```text
[STYLE]

A single game sprite, square: a LOCKED deployment pad, exactly like the two locked pads at the right end of the mockup's pad row. The bottom half is the same flat light-blue bubble-ring pad as an unlocked pad but dimmer and grayer. Above it, a semi-translucent pale bubble outlined with a soft dashed white ring, with a simple white padlock icon in its center.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 10. 空位金色漩涡

- 交付：`ui/pad-swirl.png`，160×160。另附 `empty-pad-swirl.png`（用户指定要的效果）。

```text
[STYLE]

A single game effect sprite: a glowing GOLDEN SWIRL seen from DIRECTLY ABOVE (perfectly round, not in perspective), matching the golden swirl lying on the empty pad in the attached reference crop. A spiral of warm golden light winding toward the center: brightest pale gold at the center, fading outward into transparency, with a few tiny golden sparkle dots around it. Centered, filling most of the square. The game will rotate it and flatten it onto the pad.
Transparent background; the glow fades smoothly into full transparency at the edges, no hard edge, no pad, no ring underneath. No text, no letters, no numbers, no watermark.
```

### 11. 数字牌（空白）

- 交付：`ui/badge-ammo.png`，96×36。

```text
[STYLE]

A single game UI element: a small BLANK rounded pill badge, exactly like the dark number badges under the deployed creatures in the mockup: deep navy blue, a thin lighter rim and a subtle top highlight. The middle is completely empty (the game writes the number). Rounded ends, flat middle so it can be stretched horizontally. About 2.7 times wider than tall.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 12. 合成等级金币（空白）

- 交付：`ui/tier-coin.png`，40×40。

```text
[STYLE]

A single small game UI icon: a round glossy golden coin badge with a raised rim and a bright highlight, the face completely EMPTY (the game writes a roman numeral on it). Centered, filling the image.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 13. MAX 小皇冠

- 交付：`ui/crown.png`，48×36。

```text
[STYLE]

A single small game UI icon: a cute plump golden crown with three rounded points, each tipped with a tiny glossy bubble, a bright highlight, soft cel shading. Centered.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 14. 队列玻璃管

- 交付：`ui/queue-panel.png`，192×360。程序按九宫格拉伸，四边 32 像素内是边框。

```text
[STYLE]

A single game UI element: an EMPTY vertical glass tube, like the four waiting-queue tubes in the mockup: a tall rounded capsule of clear glass with soft light-blue reflections along both sides, a gentle highlight down the left side, rounded top and bottom, and a soft glowing ring at the base. The inside is EMPTY and mostly transparent (slightly frosted), so creatures drawn on top stay clearly visible. Uniform along its height so it can be stretched. About 1.9 times taller than wide.
Transparent background, no shadow. No creatures, no text, no letters, no numbers, no watermark.
```

### 15. 管口折角

- 交付：`ui/queue-corner.png`，28×28。

```text
A single tiny game UI mark: a thick rounded L-shaped corner bracket, pure WHITE (#FFFFFF) with a very slight soft gray inner edge, the corner at the top-left, both arms equal length. Flat, no gradient other than the soft edge (the game tints it).
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 16. 按钮：重排

- 交付：`ui/button-shuffle.png`，310×144。

```text
[STYLE]

A single game UI button, exactly like the left bottom button in the mockup: a glossy BLUE rounded capsule button with a bright top highlight and a slightly darker bottom edge. On the left, a white crossing-arrows shuffle icon. On the right, the Chinese label "重排" in bold rounded white characters with a deep blue outline. Wide (about 2.15 times wider than tall).
Transparent background, no shadow outside the button. No other text, no numbers, no watermark.
```

### 17. 按钮：优先出战

- 交付：`ui/button-designate.png`，310×144。

```text
[STYLE]

A single game UI button, exactly like the middle bottom button in the mockup: a glossy YELLOW-ORANGE rounded capsule button with a bright top highlight and a slightly darker bottom edge. On the left, a white double forward-arrow icon. On the right, the Chinese label "优先出战" in bold rounded white characters with a deep orange-brown outline. Wide (about 2.15 times wider than tall).
Transparent background, no shadow outside the button. No other text, no numbers, no watermark.
```

### 18. 按钮：泡泡炸弹

- 交付：`ui/button-bomb.png`，310×144。

```text
[STYLE]

A single game UI button, exactly like the right bottom button in the mockup: a glossy PURPLE rounded capsule button with a bright top highlight and a slightly darker bottom edge. On the left, a white bursting-bubble icon (a round bubble popping with small droplets). On the right, the Chinese label "泡泡炸弹" in bold rounded white characters with a deep purple outline. Wide (about 2.15 times wider than tall).
Transparent background, no shadow outside the button. No other text, no numbers, no watermark.
```

### 19. 按钮数量角标（空白）

- 交付：`ui/badge-count.png`，44×44。

```text
[STYLE]

A single small game UI badge: a round deep navy-blue circle with a thin white rim and a small highlight, the middle completely EMPTY (the game writes the count), like the count badges on the mockup's bottom buttons. Centered.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

---

## 3. 角色（阵容图确认后）

每一项生成一张四格横图，再切成 4 个文件。三态同一缩放；**脚底在 y = 212、水平居中，y 212 以下留空**；身体高 120–150、宽不超过 180。附示意图、v01 图、确认后的阵容图。

### 角色通用段（下文写作 [SHEET]）

```text
Layout: landscape image, four equal cells in one row, separated only by empty space.
Cell 1, READY: the character standing at rest, alert, looking slightly upward.
Cell 2, ATTACK: the character has just inhaled: cheeks puffed, mouth opened into a round "O" pointing upward, body slightly squashed and leaning back with elastic energy. Do NOT draw the projectile in this cell.
Cell 3, SPENT (out of shots): deflated and tired, body sagging and leaning to one side, half-closed eyes, one small sweat drop; colors slightly dulled but the main color is still clearly recognizable.
Cell 4, PROJECTILE only, large and centered: a HOLLOW, semi-transparent soap bubble tinted with the main color, a thin main-color rim, one white highlight, clearly see-through in the middle (NOT a solid ball). No trail, no motion lines.
Cells 1 to 3: the SAME character at the SAME scale, feet on the same baseline and the same horizontal center in all three cells; only the pose changes. No effects, trails, motion lines, pads, ground or shadows.
Transparent background, clean edges, no glow around the character. No text, no letters, no numbers, no watermark.
```

### 20. 角色：blue #2982F4

- 交付：`creatures/blue/2982F4/` 的 `idle.png`、`attack.png`、`spent.png`（224×256）、`projectile.png`（64×64）。另需 #42A9FF 版本，见第 28 项。

```text
[STYLE]

Game sprite sheet of ONE character from this cast: [CAST]

The character: blue, main color #2982F4. Use the approved lineup as the character reference and the mockup for the art style.

[SHEET]
```

### 21. 角色：orange #FF7E27

- 交付：`creatures/orange/FF7E27/`。另需 #FF9D30 版本，见第 29 项。

```text
[STYLE]

Game sprite sheet of ONE character from this cast: [CAST]

The character: orange, main color #FF7E27. Use the approved lineup as the character reference and the mockup for the art style.

[SHEET]
```

### 22. 角色：green #62E18E

- 交付：`creatures/green/62E18E/`。

```text
[STYLE]

Game sprite sheet of ONE character from this cast: [CAST]

The character: green, main color #62E18E (a fresh minty green, NOT yellow-green). Use the approved lineup as the character reference and the mockup for the art style.

[SHEET]
```

### 23. 角色：purple #BD80FF

- 交付：`creatures/purple/BD80FF/`。

```text
[STYLE]

Game sprite sheet of ONE character from this cast: [CAST]

The character: purple, main color #BD80FF (a soft lavender purple, NOT magenta). Its floppy ears must stay clearly visible, because in one tutorial level it appears next to the light-blue creature and color-blind players rely on the ears to tell them apart. Use the approved lineup as the character reference and the mockup for the art style.

[SHEET]
```

### 24. 角色：yellow #FFDF48

- 交付：`creatures/yellow/FFDF48/`。

```text
[STYLE]

Game sprite sheet of ONE character from this cast: [CAST]

The character: yellow, main color #FFDF48. Use the approved lineup as the character reference and the mockup for the art style.

[SHEET]
```

### 25. 角色：white #FFFFFF

- 交付：`creatures/white/FFFFFF/`。

```text
[STYLE]

Game sprite sheet of ONE character from this cast: [CAST]

The character: white, main color #FFFFFF (cool pure white, never cream or yellowish; shading in cool light gray; a clear cool gray outline so it reads on light and mid-gray backgrounds). Use the approved lineup as the character reference and the mockup for the art style.

[SHEET]
```

### 26. 角色：red #FF525B

- 交付：`creatures/red/FF525B/`。

```text
[STYLE]

Game sprite sheet of ONE character from this cast: [CAST]

The character: red, main color #FF525B (pink-leaning red, NOT orange-red; it must stay clearly different from the orange creature). Use the approved lineup as the character reference and the mockup for the art style.

[SHEET]
```

### 27. 角色：charcoal #2D2D2D

- 交付：`creatures/charcoal/2D2D2D/`。

```text
[STYLE]

Game sprite sheet of ONE character from this cast: [CAST]

The character: charcoal, main color #2D2D2D (neutral charcoal, not brown, not blue). Keep a LIGHT gray outline and light rim highlights on the spikes so its silhouette reads clearly on a mid-gray background (#8C919C). Use the approved lineup as the character reference and the mockup for the art style.

[SHEET]
```

---

## 4. 教学关颜色版本

### 28. 颜色版本：blue #42A9FF

- 交付：`creatures/blue/42A9FF/`。在 `creatures/blue/2982F4/` 的 4 个文件上逐个做，文件名、尺寸、位置完全不变。

```text
Edit the attached sprite. Change ONLY the main body color (and the projectile's tint) from #2982F4 to #42A9FF, keeping the same shading structure (lit and mid areas exactly #42A9FF, shadows only darker). Keep everything else pixel-identical: outline, face, highlights, pose, position, size and the transparent background. No text, no numbers.
```

### 29. 颜色版本：orange #FF9D30

- 交付：`creatures/orange/FF9D30/`。在 `creatures/orange/FF7E27/` 的 4 个文件上逐个做，文件名、尺寸、位置完全不变。

```text
Edit the attached sprite. Change ONLY the main body color (and the projectile's tint) from #FF7E27 to #FF9D30, keeping the same shading structure (lit and mid areas exactly #FF9D30, shadows only darker). Keep everything else pixel-identical: outline, face, highlights, pose, position, size and the transparent background. No text, no numbers.
```

---

## 5. 第二批界面（第一批接入后）

每一项附两张参考：示意图 `Art/Concepts/bubble-squad-v02/01-smiley-intro.png` 和实机截图 `Builds/R1-evidence/play-v3-p2/3-2.png`。

### 30. 弹窗底板

- 交付：`ui2/popup-panel.png`，360×360。九宫格，四边 40 像素以内是边框。

```text
[STYLE]

A single game UI element: a BLANK square popup panel for a dialog, in the same style as the interface of the attached game screenshot: a soft white-to-very-light-blue glass panel with a rounded deep-blue border about 10% of its width, gently rounded corners, a faint inner highlight along the top. The whole inside is EMPTY and evenly lit so it can be stretched (nine-slice; keep all border detail within the outer 11% on every side).
Transparent background outside the panel, no shadow. No text, no letters, no numbers, no watermark.
```

### 31. 弹窗标题条

- 交付：`ui2/popup-ribbon.png`，760×120。

```text
[STYLE]

A single game UI element: a BLANK title banner for a popup, a wide glossy blue capsule (like the blue "重排" button in the attached game screenshot, but without icon and text), slightly darker blue rim, a bright highlight stripe along the top, a few tiny white bubbles at both ends. The middle is completely EMPTY (the game writes the title). About 6.3 times wider than tall.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 32. 主按钮（空白）

- 交付：`ui2/button-primary.png`，520×104。横向九宫格，左右各 60 像素是圆头。

```text
[STYLE]

A single game UI button, BLANK: a glossy blue rounded capsule button, the same material as the blue "重排" button in the attached game screenshot but without any icon or text, darker blue rim, a bright highlight along the top, slightly darker bottom edge. Rounded ends; the middle section is uniform so it can be stretched horizontally. About 5 times wider than tall.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 33. 次按钮（空白）

- 交付：`ui2/button-secondary.png`，400×96。横向九宫格，左右各 40 像素是圆头。

```text
[STYLE]

A single game UI button, BLANK: a glossy soft slate-blue (gray-blue) rounded capsule button, same material and shape language as the primary blue button but calmer, darker slate rim, a bright highlight along the top. Rounded ends; the middle is uniform so it can be stretched horizontally. About 4.2 times wider than tall.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 34. 开关：开

- 交付：`ui2/toggle-on.png`，120×64。

```text
[STYLE]

A single small game UI toggle switch in the ON state: a glossy green capsule track with a thin white rim, a round white glossy knob on the RIGHT side. Centered, filling the image.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 35. 开关：关

- 交付：`ui2/toggle-off.png`，120×64。和第 34 项同形状同大小。

```text
[STYLE]

A single small game UI toggle switch in the OFF state: a soft gray capsule track with a thin white rim, a round white glossy knob on the LEFT side. Same shape and size as an ON switch. Centered, filling the image.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 36. 新队员卡片框

- 交付：`ui2/card-frame.png`，520×720。

```text
[STYLE]

A single game UI element: an EMPTY tall card frame for introducing a new squad member: a rounded-rectangle glass card, white to very light blue, a deep-blue rounded border, a soft sky-blue glow inside the upper 70% (empty space where the game draws the character), and a lighter flat name strip across the lower part (empty; the game writes the name). A few tiny glossy bubbles on the border corners.
Transparent background, no shadow. No characters, no text, no letters, no numbers, no watermark.
```

### 37. 卡片背后放射光

- 交付：`ui2/card-rays.png`，720×720。

```text
[STYLE]

A single game effect sprite: soft warm golden light rays radiating from the exact center, about 16 rays, with a gentle glow in the middle and a few small glossy bubbles floating among the rays; everything fades smoothly to full transparency toward the edges (circular falloff, no hard edge). Centered.
Transparent background. No text, no letters, no numbers, no watermark.
```

### 38–41. 向导"小蓝"头像（四种表情）

- 交付：`ui2/guide-explain.png`、`ui2/guide-happy.png`、`ui2/guide-worried.png`、`ui2/guide-proud.png`，各 240×240。另附 `Art/R1/art-v3/delivery/creatures/blue/2982F4/idle.png`。四张同一个角色、同一构图、同一大小，只有表情和小动作不同。

```text
[STYLE]

Portrait sprite of the blue squad member "Xiao Lan" from the attached creature sprite: EXACTLY the same character (round blue #2982F4 bubble body, tiny cheek fins, the same eyes and face style), shown from the chest up, facing three-quarters toward the right (it talks to a speech bubble on its right), filling most of the square, centered. Same scale and position in all four expressions.
Expression for this file:
- explain: friendly, mouth open mid-sentence, one fin raised as if pointing.
- happy: big smile, eyes squeezed into happy arcs, cheeks puffed, a couple of small sparkles.
- worried: eyebrows tilted up, small open mouth, one small sweat drop.
- proud: confident grin, eyes half closed, chest puffed, fin on hip.
Transparent background, clean edges, no shadow, no frame. No text, no letters, no numbers, no watermark.
```

### 42. 对话气泡

- 交付：`ui2/speech-bubble.png`，360×200。九宫格，四边 40 像素以内是边框。

```text
[STYLE]

A single game UI element: a BLANK speech bubble panel, white, softly rounded rectangle, a thin deep-blue rim, a faint light-blue inner glow at the bottom edge. NO tail. The inside is empty and evenly lit so it can be stretched (keep all border detail within the outer 11%).
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 43. 教学箭头

- 交付：`ui2/pointer.png`，96×96。

```text
[STYLE]

A single small game UI icon: a chunky rounded golden arrow pointing straight DOWN, glossy, with a deep orange-brown outline and one bright bubble-like highlight on the shaft. Centered, filling most of the square.
Transparent background, no shadow. No text, no letters, no numbers, no watermark.
```

### 44. 告急横幅

- 交付：`ui2/banner-warning.png`，600×104。横向九宫格，左右各 60 像素是圆头。

```text
[STYLE]

A single game UI element: a BLANK warning banner, a glossy coral-red rounded capsule with a darker red rim and a bright highlight along the top, rounded ends, uniform middle so it can be stretched horizontally. The middle is EMPTY (the game writes the warning). About 5.8 times wider than tall.
Transparent background, no shadow. No text, no letters, no numbers, no icons, no watermark.
```

### 45. 提示横幅

- 交付：`ui2/banner-hint.png`，600×104。横向九宫格，左右各 60 像素是圆头。

```text
[STYLE]

A single game UI element: a BLANK hint banner, a semi-translucent deep slate-blue rounded capsule with a soft light-blue rim, like the level label in the attached game screenshot, rounded ends, uniform middle so it can be stretched horizontally. The middle is EMPTY. About 5.8 times wider than tall.
Transparent background, no shadow. No text, no letters, no numbers, no icons, no watermark.
```

---

## 6. 第二批特效

每一项附三张参考：示意图、实机截图 `Builds/R1-evidence/play-v3-p2/3-2.png`、同名 v2 文件 `Art/R1/art-v2/delivery/fx/<文件名>`。**v2 文件只参考版式、朝向、帧数和透明度**，画风全部换成泡泡。多帧文件等宽横排，每帧在自己格子中央。

### 特效通用段（下文写作 [FX]）

```text
Game visual effect for the attached bubble-world game (see the screenshot for the style): soft glossy bubbles and foam, clean bright highlights, gentle glow, no hard black outlines. The third attached image is the previous version of THIS effect from another theme: copy ONLY its canvas layout, orientation, number of frames and how transparent it is; replace all of its content with bubbles and foam. No leaves, no plants, no water cans, no dust or dirt, no text, no letters, no numbers, no watermark. Transparent background; everything fades softly into full transparency.
```

### 46. 冲锋尾迹（可着色）

- 交付：`fx/dash-trail.png`，96×256，1 帧。

```text
[FX]

A single vertical trail of small bubbles and soft light, as left behind a creature dashing upward: dense and brightest at the top, thinning out and fading to nothing at the bottom. PURE WHITE to light gray only (the game tints it with the creature's color).
```

### 47. 撞击泡沫

- 交付：`fx/impact-dust.png`，960×160，6 帧（每帧 160×160）。

```text
[FX]

Six frames, left to right, of a small foam burst: frame 1 a tight puff of white foam bubbles, frames 2–4 it bursts outward into many small bubbles with a light-blue tint, frames 5–6 the bubbles drift apart and fade out. Each frame centered in its 160x160 cell.
```

### 48. 三合一闪光

- 交付：`fx/merge-burst.png`，1152×192，6 帧（每帧 192×192）。

```text
[FX]

Six frames, left to right, of a celebratory merge burst: a golden ring of light expands from the center while a circle of small golden and white bubbles flies outward, a bright star-shaped sparkle at the peak (frames 2–3), then everything fades (frames 5–6). Gold and white. Each frame centered in its 192x192 cell.
```

### 49. 泡泡光流（可着色）

- 交付：`fx/beam.png`，48×256，1 帧。上下边缘必须能无缝拼接。

```text
[FX]

A vertical stream of tightly packed small round bubbles flowing upward, with a soft glowing core down the middle, filling the full height and seamlessly tileable top-to-bottom (the top and bottom edges must match). PURE WHITE to light gray only (the game tints it).
```

### 50. 光流端点（可着色）

- 交付：`fx/beam-tip.png`，256×64，4 帧（每帧 64×64）。

```text
[FX]

Four frames, left to right, of a pulsing cluster of bubbles at the end of a bubble stream: a soft glowing core with three to five small bubbles around it, growing slightly larger and brighter from frame 1 to frame 4. PURE WHITE to light gray only (the game tints it). Each frame centered in its 64x64 cell.
```

### 51. 击退气浪

- 交付：`fx/repel-gust.png`，1080×120，1 帧。

```text
[FX]

A single horizontal band of rushing foam spanning the full width: curling white and light-blue foam with small bubbles, densest along the middle line, fading at the top and bottom edges and slightly at both ends.
```

### 52. 复活泡沫浪

- 交付：`fx/revive-wave.png`，1080×360，1 帧。

```text
[FX]

A single wide wave of white and light-cyan foam rising upward across the full width: a thick bubbly crest at the top, softer foam and many small bubbles below, fading out toward the bottom edge.
```

### 53. 泡泡炸弹（落下前）

- 交付：`fx/water-can.png`，256×256，1 帧。

```text
[FX]

A single large glossy bubble bomb: a big round soap bubble with a rainbow-iridescent rim, a purple tint, a bright white highlight, and a small twinkling star of light inside it. Centered, filling most of the square. Clearly a bubble, not a container.
```

### 54. 泡泡炸弹爆开

- 交付：`fx/water-splash.png`，2048×256，8 帧（每帧 256×256）。

```text
[FX]

Eight frames, left to right, of the big bubble bomb popping: frame 1 the bubble stretches and starts to burst, frames 2–4 it pops into a ring of foam and many small purple, white and light-blue bubbles flying outward, frames 5–8 the foam ring widens and the bubbles drift and fade out. Each frame centered in its 256x256 cell.
```

### 55. 泡泡炸弹瞄准圈

- 交付：`fx/water-aim.png`，256×160，1 帧。

```text
[FX]

A single targeting marker: a light-cyan dashed ellipse filling the image (about 1.6 times wider than tall), with a faint cyan glow inside and four or five small bubbles sitting on the dashed line. Mostly transparent in the middle.
```

---

## 7. 补件：下半部托板（2026-10-15）

### 56. 下半部托板

- 交付：`scene/stage.png`，1080×772，透明底。对应游戏逻辑 y 574–960（护栏下沿到屏幕底），整张铺满宽度。
- 附三张参考：示意图 `Art/Concepts/bubble-squad-v02/01-smiley-intro.png`（**重点看护栏以下那一块**）、游戏实机截图 `Builds/R1-evidence/play-v3/3-2.png`（现在程序画的平板，要替换的就是它）、交付的背景图 `Art/R1/art-v3/delivery/scene/background.png`（托板会压在它的下半部分上）。

```text
[STYLE]

A single wide game layer, transparent background: the soft "stage" that sits in the lower part of the battle screen, behind the deployment pads and the four queue tubes, exactly like the area below the foam rail in the attached mockup (first image). 1080x772, landscape.
Content: a large calm muted blue-gray frosted panel (soft gray-blue, about #B4C8E0 at the top fading to about #9DB4D2 at the bottom), slightly translucent, filling almost the whole width; its top edge is a soft gently rounded line with a faint white rim; its LEFT, RIGHT and BOTTOM edges are framed by puffy white-and-pale-blue clouds that overlap the panel edges, with a few small glossy colored bubbles (pink, blue, yellow, purple) and one small rainbow arc tucked into the bottom corners, like the mockup.
The middle of the panel must stay PLAIN and evenly lit: no clouds, objects, patterns or text in the central 80% of the width between 5% and 88% of the height, because pads, creatures, glass tubes, numbers and three buttons are drawn on top of it.
The top 3% of the image is transparent (the foam rail sits there). No characters, no pads, no tubes, no buttons, no text, no letters, no numbers, no watermark.
```

### 57. 完整背景（重画整张，含下半部托板）

- 交付：`scene/background.png`，1080×1920，不透明，**覆盖**原来的第 1 项背景（原文件先复制到 `delivery/_originals/background-v1.png` 留档）。第 56 项的 `scene/stage.png` 只作参考，交付后移到 `delivery/_originals/stage-56.png`（游戏不再叠加托板）。
- 附四张参考：示意图 `Art/Concepts/bubble-squad-v02/01-smiley-intro.png`（整体气氛，**下半部托板以它为准**）；现在的背景 `Art/R1/art-v3/delivery/scene/background.png`（**上半部天空的颜色和明暗必须保持**）；第 56 项托板 `Art/R1/art-v3/delivery/scene/stage.png`（下半部可以参考）；游戏实机截图 `Builds/R1-evidence/play-v3/3-2.png`（界面会叠在哪些位置）。
- 位置按游戏逻辑坐标换算成图片高度百分比（540×960 → 1080×1920）：

| 区域 | 图片高度 | 内容 | 要求 |
| --- | --- | --- | --- |
| 顶栏 | 0–9% | 明亮天空、小云；右上角远处小城堡和一段彩虹 | 左上和中间保持平静（暂停、关卡牌、进度条压在上面） |
| 泡泡阵 | 9.6–58% | **平静的天空**，上浅下更浅的蓝色渐变，和现在的背景一致：中线取色约 9.6% 处 #5CAEFD、30% 处 #72BEFE、50% 处 #88CCFD、57% 处 #95D3FE | 只允许在最左、最右边缘有很淡的云；**中间不能有云、小岛、瀑布或任何图案**（泡泡阵直接画在上面，背后有花纹会显得糊） |
| 护栏一带 | 57–60% | 天空过渡到托板 | 游戏会在 58.5% 处画一条泡沫护栏 |
| 托板 | 60–100% | 一整块偏灰的蓝色磨砂托板，上沿是柔和的圆弧并带一道淡白边；颜色上约 #B4C8E0、下约 #9DB4D2；**左右两侧和底部被蓬松的白、浅蓝云团包住，云团压过托板边缘**；四个角落有几颗彩色光泽小泡泡（粉、蓝、黄、紫）和一小段彩虹，像示意图 | 托板中间 80% 宽、62%–97% 高的区域**必须平整干净**（出战位、角色、玻璃管、数字、三个按钮画在上面） |

```text
[STYLE]

ONE complete battle background, portrait 9:16, 1080x1920, opaque, edge to edge. It REPLACES the current background (second attached image): keep the same bright cloud-top bubble world, the same castle and rainbow in the top-right corner, and the SAME calm blue sky colors in the upper part, but now the lower 40% is a soft stage panel painted into the same image, exactly like the area below the foam rail in the mockup (first attached image).
ONLY the background: no user interface, no buttons, no bars, no characters, no bubbles grid, no pads, no tubes, no text.

Layout from top to bottom (percent of image height):
- 0% to 9%: bright sky with small fluffy clouds; a tiny distant fairy-tale castle and a short rainbow arc in the top-right corner; the top-left and middle stay calm.
- 9.6% to 57%: a CALM, PLAIN sky: a smooth vertical blue gradient matching the current background (about #5CAEFD near the top of this band, #72BEFE at 30%, #88CCFD at 50%, #95D3FE at 57%). Only very faint thin clouds hugging the far left and far right edges. Absolutely NO clouds, islands, waterfalls, birds or shapes in the middle 85% of the width: a dense picture made of small bubbles is drawn on top of this sky by the game.
- 57% to 60%: the sky softly meets the top of the stage panel (the game draws a thin glowing foam rail at 58.5%).
- 60% to 100%: a large calm muted blue-gray frosted stage panel (about #B4C8E0 at its top fading to about #9DB4D2 at the bottom), its top edge a soft gentle curve with a faint white rim. Its LEFT, RIGHT and BOTTOM edges are wrapped in puffy white and pale-blue clouds that overlap the panel; a few small glossy colored bubbles (pink, blue, yellow, purple) and one small rainbow arc tucked into the bottom corners, like the mockup. The center of the panel (middle 80% of the width, from 62% to 97% of the height) stays PLAIN and evenly lit, because pads, creatures, glass tubes, numbers and three buttons are drawn on top.
The upper sky and the lower stage must feel like ONE painting with consistent light, not two pasted layers.
No text, no letters, no numbers, no watermark.
```

---

## 8. 补件：眨眼和欢呼（2026-10-15）

游戏里待战角色每 3–5 秒眨一次眼（眨眼帧只显示约 0.13 秒），过关时全体换成欢呼姿势一起蹦跳。两项都按 10 个颜色版本做：8 个原色（第 20–27 项），加 #42A9FF、#FF9D30 两个教学关版本。

- 文件放在对应颜色版本的目录，和 `idle.png` 同目录：`creatures/<id>/<主色>/blink.png`、`cheer.png`，224×256，透明底。
- 画布规则同第 3 部分：**脚底在 y = 212、水平居中，y 212 以下留空**。
- 每个颜色版本附它自己的 `idle.png`，再附示意图和已确认的阵容图 `Art/R1/art-v3/lineup/01-lineup.png`。
- 顺序：先做 blue #2982F4 的两张，自查通过后再做其余 7 个原色；最后 #42A9FF、#FF9D30。

### 58. 眨眼帧

- 交付：10 个 `blink.png`。
- **只改眼睛**：在 `idle.png` 上编辑。后期把生成图里眼睛所在的小矩形（眼睛外扩 4 px）贴回原 `idle.png`，**其余像素必须和 `idle.png` 完全一样**。否则眨眼时身体会跳一下。
- 教学关两个版本直接在 `creatures/blue/42A9FF/idle.png`、`creatures/orange/FF9D30/idle.png` 上做同样的处理（不要从原色眨眼图改色）。
- 自查：和 `idle.png` 逐像素比较，不同的像素只能在眼睛矩形里；把不同像素的范围（x、y、宽、高）写进交付清单。

```text
Edit the attached sprite. Close the character's eyes ONLY: replace each open eye with a short, gently curved closed-eye line in the character's outline color (a relaxed, happy blink), at the same position and the same size as the open eye. Remove the eye highlights. Keep EVERYTHING else pixel-identical: body, color, outline, mouth, cheeks, ears, pose, position, size and the transparent background. No text, no numbers.
```

### 59. 欢呼姿势

- 交付：10 个 `cheer.png`。
- 先做 8 个原色，每个原色单独生成一张单格图，切成 224×256：**脚底在 y = 212、水平居中，缩放和 `idle.png` 一致**（身体最宽处宽度和 `idle.png` 相差不超过 5%）。整个角色（含举起的手、耳朵、头顶的小东西）不能超出画布，最高点 y ≥ 30，宽不超过 210。
- 教学关两个版本：用第 28、29 项的提示词，在对应原色的 `cheer.png` 上改主色。
- 自查：脚底行、水平中心、身体宽度比例写进交付清单；主色取样同第 3 部分（ΔE76 ≤ 8）。

```text
[STYLE]

Game sprite of ONE character from this cast: [CAST]

The character: the one in the attached sprite (same species, same colors, same face design, same proportions; the attached sprite is its READY pose). Draw it CELEBRATING a victory: eyes closed into happy upward arcs, a big open happy smile, cheeks a little rosier, body stretched slightly upward with joy, and its little arms, fins, ears or antennae (whatever this character has) raised up high. Feet still touching the same ground line, not jumping (the game makes it hop).
Same scale as the attached sprite, centered, the whole character inside the frame with a little empty margin. ONE pose only. No confetti, sparkles, stars, effects, trails, motion lines, pads, ground or shadows.
Transparent background, clean edges, no glow around the character. No text, no letters, no numbers, no watermark.
```

---

## 9. 补件：完整背景第二次重画（2026-10-15 晚）

### 60. 完整背景（补回浮岛带，托板照示意图）

第 57 项按当时的要求把天空中间清空了，结果把示意图护栏上方那一排**浮岛、瀑布、小房子**也一起去掉了。这一点是提示词写错了，不是美术执行的问题。第 57 项托板是一块硬边方框，示意图里的托板四周是被云包住的。这次两处都照示意图改。

- 交付：`scene/background.png`，1080×1920，不透明，覆盖第 57 项（原文件先复制到 `delivery/_originals/background-57.png`）。
- 附五张参考：
  - 示意图 `Art/Concepts/bubble-squad-v02/01-smiley-intro.png`：整体以它为准。
  - 浮岛带截图 `Art/Concepts/bubble-squad-v02/reference/island-band.png`：**照着画**，左右两座带瀑布的浮岛，中间远处几座带蘑菇顶小房子的小浮岛，底下一层柔和的云。
  - 托板截图 `Art/Concepts/bubble-squad-v02/reference/lower-stage.png`：**照着画**下半部。
  - 第 1 项旧背景 `Art/R1/art-v3/delivery/_originals/background-v1.png`：天空颜色、右上城堡和彩虹保持。
  - 游戏实机截图 `Builds/R1-evidence/play-v3/3-2.png`：看界面压在哪里。
- 位置（图片高度百分比）：

| 区域 | 图片高度 | 内容 | 要求 |
| --- | --- | --- | --- |
| 顶栏 | 0–9% | 同第 57 项：天空、小云，右上城堡和彩虹 | 左上和中间平静 |
| 泡泡阵 | 9.6–47% | 平静的蓝色渐变天空，取色同第 57 项 | 中间 85% 宽不能有云和图案 |
| **浮岛带** | 47–59% | 照 `island-band.png`：左右边缘各一座带瀑布、绿草、小树、小房子的浮岛，占到图宽约 18%；中间是远处朦胧的小浮岛和蘑菇顶小房子；底部一层蓬松的云，云顶在 55–58% | 越靠上越淡、越朦胧（泡泡阵最下面几排会压在 47–55% 上，背后不能太花）；最清楚的细节放在 52–59% 和左右两侧 |
| 护栏 | 58.5% | 程序画泡沫护栏 | 云层在这里自然接到托板 |
| 托板 | 59–100% | 照 `lower-stage.png`：浅色磨砂托板，颜色约 #B4C8E0 到 #9DB4D2，**四周被白、浅蓝的云包住，没有硬边方框**；左下一小段彩虹，四角几颗彩色光泽小泡泡和一颗小星星 | 中间 80% 宽、62%–97% 高平整干净 |

```text
[STYLE]

ONE complete battle background, portrait 9:16, 1080x1920, opaque, edge to edge, matching the attached mockup (first image) as closely as possible. ONLY the background: no user interface, no buttons, no bars, no characters, no bubble grid, no pads, no tubes, no projectiles, no text.

Layout from top to bottom (percent of image height):
- 0% to 9%: bright sky with small fluffy clouds; a tiny distant fairy-tale castle and a short rainbow arc in the top-right corner; the top-left and middle stay calm.
- 9.6% to 47%: a CALM, PLAIN blue sky gradient (about #56AAFE near the top of this band, #72BEFE at 30%, #88CCFD at 45%). Nothing in the middle 85% of the width.
- 47% to 59%: the FLOATING ISLAND BAND, exactly like the second attached image: on the far left and far right, one floating grassy island each with a small waterfall pouring down, little round trees and tiny cottages; in the middle distance several small hazy floating islands with mushroom-roofed cottages; below them a soft bank of fluffy clouds whose tops sit at 55% to 58%. The band fades into the sky upward: the upper part is pale and hazy with low contrast (a grid of bubbles covers 47% to 55% during play), the clearest details sit between 52% and 59% and on the left and right sides.
- 59% to 100%: the lower stage, exactly like the third attached image: a soft light frosted blue-gray tray (about #B4C8E0 at its top to #9DB4D2 at the bottom) that blends into a floor of puffy white and pale-blue clouds; its left, right and bottom are wrapped in clouds, NO hard rectangular frame or sharp panel edge. One small rainbow arc in the bottom-left corner, a few small glossy colored bubbles (pink, blue, yellow, purple) and one small star tucked into the corners. The center of the tray (middle 80% of the width, from 62% to 97% of the height) stays plain and evenly lit, because pads, creatures, glass tubes, numbers and three buttons are drawn on top.
The whole image is ONE painting with consistent soft light and haze, not pasted layers.
No text, no letters, no numbers, no watermark.
```


---

## 10. 第三批：12 个新角色（2026-10-16）

交付规则见 [任务书](美术AI任务书.md) 第 5 节。新角色和第一批 8 个是**同一家族**：同样圆胖、同样的描边、明暗和高光、同样的小短脚和大眼睛。每一项都附示意图 `Art/Concepts/bubble-squad-v02/01-smiley-intro.png` 和第一批阵容图 `Art/R1/art-v3/lineup/01-lineup.png`；第 62–75 项在第 61 项确认后再附 `Art/R1/art-v3/lineup/02-lineup-new.png`。参考图每次最多 5 张。

### 新角色段（下文写作 [CAST2]）

```text
The NEW cast are 12 more plump original bubble creatures from the same family as the approved first eight in the attached lineup (same chubby proportions, same clean colored outlines, soft cel shading, one bright highlight, big expressive eyes, little stubby feet unless stated otherwise). One creature per color. Each body is almost entirely its main color with ONE distinctive silhouette feature; no clothes, no armor, no weapons, no held objects, no leaves, no flowers, no plants.
They must NOT reuse the first eight's features: cheek fins or tail fin, two short horns, back bumps, two long loop ears, ball-tipped antennae, pill shape, heart shape or quiff, spikes.
- slate-blue, main color #696CAF: a round puff with ONE long soft cone horn in the middle of its forehead, bent slightly forward, rounded tip; sleepy, half-lidded dreamy eyes.
- cyan, main color #46D9FF: jellyfish-like: a rounded dome top, slightly wider than tall, standing on three or four short wavy tendrils instead of feet; curious, wide-eyed.
- lavender, main color #C8C5FF: a fluffy cloud-shaped body whose whole outline is made of six or seven round puffy lobes; dreamy, gentle smile.
- berry, main color #9C225E (deep magenta-wine, NOT red, NOT purple): a tall egg-shaped body with one round bubble bun sitting directly on top of its head (no stalk); sassy and proud, one eyebrow raised.
- brown, main color #65392D (warm cocoa brown, NOT charcoal): a wide round body with two small half-round ears on top of its head, perfectly smooth outline (no spikes); calm and sturdy, gentle grin.
- violet, main color #B672FD: a rounded gem: a soft faceted diamond silhouette with a pointed top, visible facet planes in lighter and darker violet; cool and elegant, confident half-smile.
- lime, main color #92EF4D: a soft rounded five-pointed star shape, the two lower points are its feet; hyperactive, huge grin.
- mint, main color #30D183: a very wide, flat, squishy dumpling-shaped body, clearly wider than tall, with two tiny stubby arms; easygoing, content closed-mouth smile.
- forest, main color #0C7E1C (deep forest green): two stacked round bubbles, a smaller head bubble on a bigger body bubble, like a snowman; a LIGHT green outline and light rim highlights so it reads on dark backgrounds; stoic and brave, determined eyes.
- pink, main color #FFC5E0: a round body with two round puff balls on the upper left and upper right of its head like pigtails; sweet and affectionate, rosy cheeks.
- peach, main color #FFCAA9: a gumdrop shape (flat bottom, rounded top, wide base) with two tiny round wings on its upper back; gentle and cheerful.
- pale-mint, main color #C5FFE4 (very pale cool mint, NOT white): a little wisp: a round head-body tapering into one curly wisp tail at the bottom instead of feet; a medium mint-teal outline so it never reads as white; timid and shy, small mouth.
Color rule: the lit and mid areas of each body are exactly its main color hex; shadows only darken it without shifting the hue.
```

### 角色通用段（三格，下文写作 [SHEET3]）

```text
Layout: landscape image, three equal cells in one row, separated only by empty space.
Cell 1, READY: the character standing at rest, alert, looking slightly upward.
Cell 2, ATTACK: the character has just inhaled: cheeks puffed, mouth opened into a round "O" pointing upward, body slightly squashed and leaning back with elastic energy. Do NOT draw any projectile.
Cell 3, SPENT (out of shots): deflated and tired, body sagging and leaning to one side, half-closed eyes, one small sweat drop; colors slightly dulled but the main color is still clearly recognizable.
All three cells: the SAME character at the SAME scale, its lowest point (feet, tendrils or tail tip) on the same baseline and the same horizontal center; only the pose changes. No effects, trails, motion lines, pads, ground or shadows.
Transparent background, clean edges, no glow around the character. No text, no letters, no numbers, no watermark.
```

### 61. 第三批阵容确认图（先做，给用户确认造型，不进游戏）

- 交付：`Art/R1/art-v3/lineup/02-lineup-new.png`，横图，浅灰底，12 个角色三行。附示意图和第一批阵容图。
- 行的顺序就是第二轮交付的三批，方便用户按批确认。

```text
[STYLE]

Character lineup sheet. [CAST2]

Layout: landscape image, three rows of four characters, all in their READY pose (standing, alert, looking slightly upward), same scale as each other AND as the creatures in the attached first lineup, feet on one baseline per row, generous empty space between characters, on a flat plain very light cool gray background (#EEF1F5).
Row 1, left to right: berry, brown, violet, peach. Row 2, left to right: slate-blue, lavender, cyan, pink. Row 3, left to right: lime, mint, forest, pale-mint.
Each character must read clearly by silhouette alone: forehead horn, jellyfish tendrils, cloud lobes, head bun, small round ears, gem facets, star points, wide flat dumpling, stacked bubbles, pigtail puffs, little wings, wisp tail.
No pads, no ground, no shadows, no scenery, no text, no letters, no numbers, no watermark.
```

### 62–73. 新角色（每项一个颜色）

每一项生成一张三格横图，切成 `idle.png`、`attack.png`、`spent.png`（224×256）：**最低点在 y = 212、水平居中，y 212 以下留空**；身体高 120–150、宽不超过 180。附示意图、两张阵容图。下面是通用提示词，`<id>` 和 `<主色>` 按表替换。

| # | id | 主色 | 交付目录 | 额外一句（加在 The character 那一行后面） |
| --- | --- | --- | --- | --- |
| 62 | berry | #9C225E | `creatures/berry/9C225E/` | Deep magenta-wine, not red and not purple. |
| 63 | brown | #65392D | `creatures/brown/65392D/` | Warm cocoa brown, clearly different from the charcoal creature; smooth outline, no spikes. |
| 64 | violet | #B672FD | `creatures/violet/B672FD/` | Keep the facet planes visible but subtle; the lit facets are exactly #B672FD. |
| 65 | peach | #FFCAA9 | `creatures/peach/FFCAA9/` | Light peach, never orange; outline a warm medium orange-brown. |
| 66 | slate-blue | #696CAF | `creatures/slate-blue/696CAF/` | Muted slate blue, not purple and not bright blue. |
| 67 | lavender | #C8C5FF | `creatures/lavender/C8C5FF/` | Pale lavender, never white; outline a medium violet-gray. |
| 68 | cyan | #46D9FF | `creatures/cyan/46D9FF/` | Bright aqua-cyan, clearly lighter and greener than the blue creature. |
| 69 | pink | #FFC5E0 | `creatures/pink/FFC5E0/` | Soft candy pink, never red; outline a medium rose. |
| 70 | lime | #92EF4D | `creatures/lime/92EF4D/` | Yellow-green lime, clearly yellower than the other greens. |
| 71 | mint | #30D183 | `creatures/mint/30D183/` | Fresh mint green, between the green creature and cyan. |
| 72 | forest | #0C7E1C | `creatures/forest/0C7E1C/` | Deep forest green with a LIGHT green outline so the silhouette reads on a dark background. |
| 73 | pale-mint | #C5FFE4 | `creatures/pale-mint/C5FFE4/` | Very pale mint, never white; medium mint-teal outline. |

```text
[STYLE]

Game sprite sheet of ONE character from this NEW cast: [CAST2]

The character: <id>, main color <主色>. <额外一句> Use the approved new lineup as the character reference, the first lineup for the family style and scale, and the mockup for the art style.

[SHEET3]
```

### 74. 新角色眨眼帧

- 交付：12 个 `blink.png`，放在各自颜色目录。做法、提示词、自查和第 58 项完全一样：在 `idle.png` 上只闭眼，后期把眼睛小矩形（外扩 4 px）贴回原 `idle.png`，逐像素比较的结果写进交付清单。
- 每个附它自己的 `idle.png`。小荧（lime）如果眼睛压在星角上，外扩范围照实记录即可。

### 75. 新角色欢呼姿势

- 交付：12 个 `cheer.png`。做法和自查同第 59 项（脚底 y = 212、缩放和 idle 一致、最高点 y ≥ 30、宽不超过 210）。
- 提示词用第 59 项原文，把 `[CAST]` 换成 `[CAST2]`。没有手的角色（小冰、小清、小荧）用触须、尾巴、星角往上扬来表现欢呼。

---

## 11. 第三批：神秘队员（2026-10-16）

### 76. 神秘队员

- 交付：`creatures/mystery/8C919C/idle.png`，224×256，画布规则同第 3 部分。只交这一张。
- 附示意图和第一批阵容图（只参考缩放和画风）。
- 自查：没有五官、没有问号（问号由程序画在脚下数字牌的位置）；主色取样 ΔE76 ≤ 8。

```text
[STYLE]

Game sprite of a MYSTERY squad member whose identity is still hidden: a plump rounded bubble cocoon the same size and proportions as the creatures in the attached lineup, made of frosted gray glass, main color #8C919C, NO face, NO eyes, NO mouth, NO limbs. Inside it, faint soft swirls of lighter gray mist; one bright highlight on the upper left; a clean slightly darker gray outline; two tiny round stubby feet at the bottom.
Single sprite, centered, standing on the bottom. Transparent background, clean edges, no glow, no shadow. No question mark, no text, no letters, no numbers, no watermark.
```

---

## 12. 第三批：决战演出（2026-10-16）

全员飞到中间合体成泡泡大王，鼓腮往上吹出扇形彩虹泡泡流。规则见任务书 5.4。

### 77. 泡泡大王

- 交付：`finale/king-ready.png`、`finale/king-blow.png`，各 512×512，透明底。生成一张两格横图再切开，保证两张是同一个角色、同一缩放。
- 附示意图、第一批阵容图、`delivery/ui/crown.png`（皇冠照它画）。
- 画布：身体水平居中 x = 256，脚底 y = 470；身高 360–400（含皇冠），宽不超过 440，皇冠顶 y ≥ 16。吹的那张嘴的中心在 x = 256、y 110–150，实测值写进交付清单。
- **先交 ready 那张给用户确认**，确认后再做 blow。

```text
[STYLE]

A giant special character for the finale of this bubble-world game: the BUBBLE KING, formed when the whole squad merges into one. Same family style as the creatures in the attached lineup, but much bigger and grander: a huge plump round bubble creature with a pearly white body and a soft iridescent rainbow sheen (pink, yellow, mint, sky blue and lavender flowing gently across the surface), a clean cool-blue outline, one big glossy highlight, two short stubby arms, little stubby feet, big sparkling determined eyes, and a small round golden crown on top of its head drawn exactly like the attached crown icon.

Layout: landscape image, two equal cells in one row, the SAME character at the SAME scale, feet on the same baseline and the same horizontal center in both cells.
Cell 1, READY: standing tall and proud, chest out, eyes looking straight up, cheeks just starting to puff, tiny arms pulled in with effort, gathering power.
Cell 2, BLOW: head tilted back, cheeks fully puffed, the mouth opened into one big round "O" at the TOP CENTER of the head pointing straight up, body slightly squashed with effort. Do NOT draw the bubble stream or any effect.
No effects, sparkles, trails, pads, ground or shadows. Transparent background, clean edges, no glow around the character. No text, no letters, no numbers, no watermark.
```

### 78. 决战按钮（空白）

- 交付：`finale/button.png`，680×172，透明底。附示意图和 `delivery/ui/button-shuffle.png`（按钮画风照它）。

```text
[STYLE]

A single wide capsule game button, BLANK, in exactly the same glossy bubble-button style as the attached button but larger and more special: a rich golden-to-warm-orange glossy capsule, a thin rainbow band running along the inner edge of the top and bottom rims, a soft white highlight across the upper part, a darker warm-brown outline, and a few small sparkling glossy bubbles hugging the left and right ends. The whole center of the capsule is plain, smooth and evenly lit (the game writes the title and a subtitle there). Wide and short (about 4 times wider than tall).
Transparent background, clean edges, no shadow. No text, no letters, no numbers, no icons in the middle, no watermark.
```

---

## 13. 第三批特效（2026-10-16）

### 79. 大泡泡弹炸开

- 交付：`fx/rocket-pop.png`，1152×192，6 帧等宽横排，每帧 192×192。附示意图和 `delivery/fx/merge-burst.png`（只参考帧的版式和透明度）。

```text
[STYLE]

Game visual effect sprite strip: SIX equal square frames in one row, each frame centered in its own cell and never crossing into the next cell. A big glossy bubble bursting:
frame 1: a full round glossy bubble, slightly wobbling;
frame 2: it pops with a bright white flash in the center;
frames 3 and 4: a ring of white foam and many small bubbles bursts outward;
frames 5 and 6: the foam droplets and small bubbles drift further out and fade, frame 6 almost fully transparent.
Colors: white and light sky blue with a small touch of warm gold in the flash. Soft glossy bubbles and foam, clean bright highlights, gentle glow, no hard black outlines. No fire, no smoke, no dust, no debris.
Transparent background; everything fades softly into full transparency. No text, no letters, no numbers, no watermark.
```

### 80. 援军泡泡

- 交付：`fx/rescue-bubble.png`，128×192，1 帧。附示意图和 `delivery/fx/dash-trail.png`（只参考竖直尾迹的版式）。

```text
[STYLE]

A single game sprite: one large glossy rescue bubble rocketing straight UP, seen from the front. The bubble is in the upper part of the canvas (its center at about 30% of the height), clear and glossy with a white highlight and a pale cyan rim; below it a short trail of small foam bubbles and soft light that thins out and fades to nothing toward the bottom. White and pale cyan only, gentle glow, no hard black outlines. No rocket, no missile, no fire, no smoke.
Transparent background; everything fades softly into full transparency. No text, no letters, no numbers, no watermark.
```


---

## 14. 第四批：6 个角色重画方案图（任务书第 6 节，第 81–86 项）

每项出 A、B 两张，各自单独生成。共用模板：

```text
[STYLE]
Character design sheet of ONE original bubble creature, full body, joyful CHEER pose (both little arms up, big happy smile), facing the viewer, centered on a plain transparent background, no ground, no shadow, no text. Plump glossy bubble body that is almost entirely main color {HEX}; lit and mid areas are exactly {HEX}, shadows only darken it; a few lighter tints of the same hue only on the belly, cheeks and highlights. Clean outline in a LIGHTER tint of the main color so the silhouette reads on dark backgrounds. Big expressive eyes with large white areas and bright catchlights, readable face. No clothes, no held objects, no plants, flowers, fruit, vegetables, dinosaurs or dragons. Distinctive silhouette feature: {FEATURE}. Personality: {MOOD}.
```

| 项 | id / 主色 | 方案 | {FEATURE} | {MOOD} |
| --- | --- | --- | --- | --- |
| 81 | charcoal #2D2D2D | A | a round fluffy soot-sprite body with a soft fur fringe all around the edge (soft tufts, NOT spikes), a pale gray belly patch, light gray outline | cool but secretly sweet, sparkly eyes |
| 81 | charcoal #2D2D2D | B | a round puff with one soft curled wisp of smoke on top of its head like a little curl, two stubby arms, pale gray cheeks and belly | sleepy-cool, half smile |
| 82 | slate-blue #696CAF | A | a little seal pup: teardrop body, two small flippers as arms, a flat tail behind | dreamy, gentle |
| 82 | slate-blue #696CAF | B | a little snail: round body carrying a big spiral bubble shell on its back | dreamy, slow and happy |
| 83 | mint #30D183 | A | a little frog: wide round body with two big round bubble eye-domes on top of its head | easygoing, wide grin |
| 83 | mint #30D183 | B | a round dumpling with a curling wave crest on top of its head like a hairdo | easygoing, cheeky smile |
| 84 | forest #0C7E1C | A | two bubbles stacked: a smaller head bubble on a bigger body bubble, bright green highlights, pale green belly, a big clear face on the head bubble | brave and steady |
| 84 | forest #0C7E1C | B | a round body wearing a ring of small bubbles around its waist like a swim ring | brave, cheerful |
| 85 | berry #9C225E | A | a tall egg shape with two small round bubbles side by side on top of its head like a bow | proud, one eyebrow raised, smiling |
| 85 | berry #9C225E | B | a round body with one long elegant curved tail ending in a small bubble | proud, graceful |
| 86 | purple #BD80FF | A | (keep the existing purple creature) a bean-shaped body with two long floppy ear loops hanging down symmetrically in soft arcs; mouth closed in a happy smile; draw at the same detail and glossy lighting as the attached high-resolution blue creature | goofy and sweet |
| 86 | purple #BD80FF | B | (keep the existing purple creature) two long floppy ear loops, one ear playfully bent in a soft curve (not sticking straight up); small open smile, one eye winking; same detail as the attached blue creature | goofy, playful |

第 81–85 项附：这个角色现在的 `delivery/creatures/<id>/<主色>/cheer.png`（只看颜色和画风，造型按上表重画）、阵容图 `lineup/02-lineup-new.png`。第 86 项附 `Art/R1/home-v1/delivery/_originals/H3-attempt-01.png`（精细度参考）和 `delivery/creatures/purple/BD80FF/cheer.png`（造型参考）。

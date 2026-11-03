using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Flightbeat.Garden;
using Flightbeat.Garden.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Flightbeat.Editor
{
    // One-time authoring tool. Existing prefabs are never regenerated on build.
    // Only atlas-derived Sprite assets are refreshed when the atlas is repacked.
    public static class GardenBattleUiAssets
    {
        public const string Root = "Assets/Flightbeat/Resources/GardenR1/UI";
        const string SpriteRoot = "Assets/Flightbeat/Garden/UI/Sprites";
        const string Catalog = "Assets/Flightbeat/Garden/UI/sprite-catalog.json";
        [Serializable] class Spec { public string name, slot; public Rect crop = new Rect(0, 0, 1, 1); public Vector4 border; }
        [Serializable] class Specs { public List<Spec> sprites = new List<Spec>(); }
        static Specs specs;
        static GardenSkin.Atlas atlas, barAtlas;
        static Texture2D texture, backgroundTexture, creatureTexture, barTexture;
        const string BackgroundSlot = "scene/background";   // own texture (background.png) since 2026-10-15
        static readonly Color Cream = GardenSkin.Cream, Ink = GardenSkin.Ink;

        // R1 v3 (bubble squad, docs/r1/界面规范-v3.md): regenerate the five sections and the page from this
        // authoring code. Deletes only the generated prefabs, their sprite assets and the sprite catalog.
        [MenuItem("Flightbeat/Garden/Recreate battle UI prefabs for v3")]
        public static void RecreateForV3()
        {
            foreach (string name in new[] { "GardenBattlePage", "GardenBattleBackground", "GardenBattleHeader", "GardenBattleControls", "GardenPauseDialog", "GardenResultDialog" })
                AssetDatabase.DeleteAsset(Root + "/" + name + ".prefab");
            foreach (string guid in AssetDatabase.FindAssets("t:Sprite", new[] { SpriteRoot }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            AssetDatabase.DeleteAsset(Catalog);
            AssetDatabase.Refresh();
            Create();
        }
        [MenuItem("Flightbeat/Garden/Create battle UI prefabs (once)")]
        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/GardenBattlePage.prefab"))
                throw new InvalidOperationException("Battle UI already exists. Edit its prefabs; this tool does not overwrite authored layouts.");
            Directory.CreateDirectory(Root); Directory.CreateDirectory(SpriteRoot);
            AssetDatabase.Refresh(); LoadAtlas(); specs = new Specs();
            SaveSection(Background(), "GardenBattleBackground");
            SaveSection(Header(), "GardenBattleHeader");
            SaveSection(Controls(), "GardenBattleControls");
            SaveSection(Pause(), "GardenPauseDialog");
            SaveSection(Result(), "GardenResultDialog");
            var root = Node("GardenBattlePage", null, 0, 0, 540, 960);
            Stretch(root);
            // A real parent Canvas is required before Unity retains the nested
            // canvases' overrideSorting flag in the prefab.
            root.gameObject.AddComponent<Canvas>();
            var view = root.gameObject.AddComponent<GardenBattlePage>();
            view.background = Instance("GardenBattleBackground", root, -1);
            view.header = Instance("GardenBattleHeader", root, 21);
            view.controls = Instance("GardenBattleControls", root, 21);
            view.pauseDialog = Instance("GardenPauseDialog", root, 30);
            view.resultDialog = Instance("GardenResultDialog", root, 31);
            view.backgroundTop = Find<RectTransform>(view.background, "Top band");
            view.backgroundField = Find<RectTransform>(view.background, "Field band");
            view.backgroundBottom = Find<RectTransform>(view.background, "Bottom band");
            view.headerBand = Find<RectTransform>(view.header, "Top safe band");
            view.controlsBand = Find<RectTransform>(view.controls, "Bottom safe band");
            view.lessonTitle = Find<GardenUiText>(view.header, "Lesson title");
            view.levelTitle = Find<GardenUiText>(view.header, "Level title");
            view.progressText = Find<GardenUiText>(view.header, "Progress percent");
            view.progressFill = Find<Image>(view.header, "Progress fill");
            view.progressSpark = Find<Image>(view.header, "Progress spark");
            view.tutorialHint = Find<RectTransform>(view.controls, "Tutorial hint").gameObject;
            view.propButtons = Find<RectTransform>(view.controls, "Prop buttons").gameObject;
            view.props = new GardenUiAction[3]; view.propAmounts = new GardenUiText[3]; view.propCancelLabels = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                var prop = Find<RectTransform>(view.controls, "Prop " + i).gameObject;
                view.props[i] = prop.GetComponent<GardenUiAction>();
                view.propAmounts[i] = Find<GardenUiText>(prop, "Amount");
                view.propCancelLabels[i] = Find<RectTransform>(prop, "Cancel hint").gameObject;
            }
            view.bombPropWord = Find<GardenUiText>(view.props[2].gameObject, "Word");
            view.bombCancelBadge = Find<GardenUiAction>(view.props[2].gameObject, "Cancel badge hit");
            view.settingsPanel = Find<RectTransform>(view.pauseDialog, "Settings").gameObject;
            view.pausedPanel = Find<RectTransform>(view.pauseDialog, "Paused").gameObject;
            view.musicText = Find<GardenUiText>(view.pauseDialog, "Music text");
            view.soundText = Find<GardenUiText>(view.pauseDialog, "Sound text");
            view.lightText = Find<GardenUiText>(view.pauseDialog, "Light text");
            view.toggleIcons = new[] { Find<Image>(view.pauseDialog, "Music toggle"), Find<Image>(view.pauseDialog, "Sound toggle"), Find<Image>(view.pauseDialog, "Light toggle") };
            view.toggleOn = Sprite("ui/toggle-on"); view.toggleOff = Sprite("ui/toggle-off");
            view.missionResult = Find<RectTransform>(view.resultDialog, "Mission result").gameObject;
            view.lessonComplete = Find<RectTransform>(view.resultDialog, "Lesson complete").gameObject;
            view.rewardGroup = Find<RectTransform>(view.resultDialog, "Rewards").gameObject;
            view.campaignComplete = Find<RectTransform>(view.resultDialog, "Campaign complete").gameObject;
            view.resultTitle = Find<GardenUiText>(view.resultDialog, "Result title");
            view.resultLesson = Find<GardenUiText>(view.resultDialog, "Result lesson");
            view.resultLevel = Find<GardenUiText>(view.resultDialog, "Result level");
            view.resultProgress = Find<GardenUiText>(view.resultDialog, "Result progress");
            view.coins = Find<GardenUiText>(view.resultDialog, "Coins");
            view.seeds = Find<GardenUiText>(view.resultDialog, "Seeds");
            view.statistics = Find<GardenUiText>(view.resultDialog, "Statistics");
            view.resultContinue = Find<GardenUiText>(view.resultDialog, "Continue text");
            view.resultPortrait = Find<Image>(view.resultDialog, "Portrait");
            view.happyPortrait = Sprite("ui/guide-happy"); view.worriedPortrait = Sprite("ui/guide-worried");
            // Child previews are visible when opened separately; the composite opens
            // in the normal battle view. Runtime visibility uses these same objects.
            view.pauseDialog.SetActive(false); view.resultDialog.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, Root + "/GardenBattlePage.prefab");
            Object.DestroyImmediate(root.gameObject);
            File.WriteAllText(Catalog, JsonUtility.ToJson(specs, true) + "\n");
            AssetDatabase.Refresh(); AssetDatabase.SaveAssets();
            Debug.Log("GARDEN_UI_PREFABS_CREATED: five nested sections and battle page");
        }
        static void LoadAtlas()
        {
            atlas = JsonUtility.FromJson<GardenSkin.Atlas>(File.ReadAllText("Assets/Flightbeat/Resources/GardenR1/atlas.json"));
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flightbeat/Resources/GardenR1/atlas.png");
            if (!texture) throw new FileNotFoundException("Garden atlas texture");
            barAtlas = JsonUtility.FromJson<GardenSkin.Atlas>(File.ReadAllText("Assets/Flightbeat/Resources/GardenR1/bars.json"));
            barTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flightbeat/Resources/GardenR1/bars.png");
            if (!barTexture) throw new FileNotFoundException("Garden glass atlas texture");
            creatureTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flightbeat/Resources/GardenR1/creatures.png");
            backgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Flightbeat/Resources/GardenR1/background.png");
            if (!backgroundTexture) throw new FileNotFoundException("Garden background texture");
        }
        public static void RefreshSprites()
        {
            if (!File.Exists(Catalog)) return;
            LoadAtlas(); specs = JsonUtility.FromJson<Specs>(File.ReadAllText(Catalog));
            foreach (Spec spec in specs.sprites) WriteSprite(spec);
            AssetDatabase.SaveAssets();
            var page = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/GardenBattlePage.prefab");
            if (page)
                foreach (var label in page.GetComponentsInChildren<GardenUiText>(true))
                    foreach (char c in label.text + label.alternateText)
                        if (!char.IsWhiteSpace(c) && (label.roundedFont ? barAtlas : atlas).characters.IndexOf(c) < 0)
                            throw new InvalidOperationException($"Missing garden font glyph '{c}' in {label.name}. Rebuild the font atlas before building the game.");
        }
        static Sprite Sprite(string id, Vector4 border = default, string suffix = "", Rect? crop = null)
        {
            string name = id.Replace('/', '-') + suffix;
            Spec spec = specs.sprites.Find(s => s.name == name);
            if (spec == null) { spec = new Spec { name = name, slot = id, border = border, crop = crop ?? new Rect(0, 0, 1, 1) }; specs.sprites.Add(spec); }
            return WriteSprite(spec);
        }
        static Sprite WriteSprite(Spec spec)
        {
            Texture2D source = texture; Rect rect;
            if (spec.slot == BackgroundSlot)
            {
                source = backgroundTexture; float w = source.width, h = source.height;
                rect = new Rect(w * spec.crop.x, h - h * (spec.crop.y + spec.crop.height), w * spec.crop.width, h * spec.crop.height);
            }
            else
            {
                var slot = SlotFor(spec.slot);
                if (slot.atlas == 2) source = barTexture;
                else if (slot.atlas == 1) source = creatureTexture;
                float size = slot.atlas == 2 ? barAtlas.size : slot.atlas == 1 && atlas.creatureHeight > 0 ? atlas.creatureHeight : atlas.size;
                rect = new Rect(slot.x + slot.w * spec.crop.x, size - slot.y - slot.h * (spec.crop.y + spec.crop.height), slot.w * spec.crop.width, slot.h * spec.crop.height);
            }
            string path = SpriteRoot + "/" + spec.name + ".asset";
            Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing && existing.rect == rect && existing.border == spec.border && existing.texture == source) return existing;
            Sprite created = UnityEngine.Sprite.Create(source, rect, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, spec.border);
            created.name = spec.name;
            if (existing)
            {
                EditorUtility.CopySerialized(created, existing); EditorUtility.SetDirty(existing); Object.DestroyImmediate(created);
                return existing;
            }
            AssetDatabase.CreateAsset(created, path); return created;
        }
        static RectTransform Background()
        {
            var root = Section("GardenBattleBackground");
            Solid("Sky base", root, GardenSkin.Hex(0xCFBDF0), true);
            float[] ends = { 0, 114, 574, 960 };
            string[] names = { "Top band", "Field band", "Bottom band" };
            for (int i = 0; i < 3; i++)
            {
                var band = Node(names[i], root, 0, ends[i], 540, ends[i + 1] - ends[i]);
                var art = Node("Background image", band, 0, 0, 540, ends[i + 1] - ends[i]); Stretch(art);
                var image = art.gameObject.AddComponent<Image>(); image.raycastTarget = false;
                image.sprite = Sprite(BackgroundSlot, suffix: "-band" + i, crop: new Rect(0, ends[i] / 960f, 1, (ends[i + 1] - ends[i]) / 960f));
            }
            return root;
        }
        static RectTransform Header()
        {
            var root = Section("GardenBattleHeader"); var band = Node("Top safe band", root, 0, 0, 540, 114);
            // UI redesign 12: same glass and rounded type as the battle hints; retain the pause action.
            var pause = Action("Pause button", band, 1, 12, 6, 44, 44);
            Art("Icon", pause, "scene/pause", 2, 2, 40, 40);
            // 2026-10-29 (light theme T8): the white pill has fully round 36 px ends; slice at 36, drawn at 19 (half of 38).
            // 2026-10-31 (user: 第x关、百分比的字没有居中): the labels span the whole plaque and sit on the centre of its face
            // (the pill has a thicker lower rim, so its face centre is ~3.5 px above the middle of the 38 px box).
            Art("Level plaque", band, "scene/level", 66, 12, 116, 38, true, 36, 36, 19);
            RoundLabel("Lesson title", band, "泡泡课堂 {0}/2", 66, 16, 116, 15, GardenBars.Navy);
            RoundLabel("Level title", band, "第 {0} 关", 66, 13f, 116, 22, GardenBars.Navy).gameObject.SetActive(false);
            // 2026-10-21 10:44 (user: too much room around the number): the frame ends 14 px earlier on the right (344 -> 330).
            Art("Progress frame", band, "scene/progress-frame", 190, 12, 330, 38, true, 36, 36, 19);
            // 2026-10-21 (quality pass): the empty groove read as a dull grey bar. Now a pale frosted groove with a fine
            // inner shade along its top edge, like a channel pressed into the glass.
            // 2026-11-03 14:05 (user: the inner bar is not centred up and down inside the big bar): the frame has a thicker
            // lower rim, so its face centre sits ~3.5 px above the box middle; the track sat on the box middle and looked low
            // (wide white above, almost none below). Track, shade and fill all move 3.5 px up onto the face centre.
            // 2026-11-03 17:36 (user: the inner bar is too thin, make it thicker): track 22 -> 26 px tall. Its sprite is all
            // border (36 px, 18 px slices), so the extra 4 px stretch the channel's middle row; the fill grows by the same
            // 4 px (ProgressFillHeight), and the track starts 2 px higher so it stays centred on the face.
            const float TrackY = 14.5f, TrackH = 26f;
            var track = Art("Progress track", band, "scene/progress-track", 201, TrackY, GardenBattlePage.ProgressWidth, TrackH, true, 18, 18, 11);   // T7: 18 px round ends
            // 23:33 (user: at 0 % the pale groove did not read as a bar): deeper, as on the revive dialog, with a
            // stronger inner shade.
            // 10-22 11:50 (user picked option 3): no groove inside the frame any more, only a faint lane (frame
            // glass shows through); the fill a softer deep blue so progress reads at a glance; no head spark.
            track.color = Color.white;
            var shade = Node("Progress track shade", band, 199, TrackY + 3.5f, GardenBattlePage.ProgressWidth - 18f, 2f);
            var shadeImage = shade.gameObject.AddComponent<Image>(); shadeImage.raycastTarget = false;
            shadeImage.color = new Color(.09f, .2f, .38f, 0f);
            // 2026-10-31 10:22 (user: 有进度的时候位置没有摆对): the fill lies inside the groove of the light-theme track
            // (its channel is 7.5 px in from each end and 5-15.5 px down the 22 px track), a green capsule with round ends.
            // 2026-11-03 11:30 (user: the fill is not centred up and down in the groove): it touched the groove's top shade
            // and left a light band below it; 1.2 px lower centres it between the shade and the lower rim.
            Image fill = Art("Progress fill", band, "scene/progress-fill", 201f + GardenBattlePage.ProgressInset, TrackY + 6.1f,
                GardenBattlePage.ProgressFillWidth * .35f, GardenBattlePage.ProgressFillHeight, true, 16, 16, GardenBattlePage.ProgressFillHeight * .5f);
            fill.color = Color.white;   // 2026-10-21: cyan-blue as in the design; mint stays for success hints
            // A soft spark rides the head of the fill (moved by PixelWingGame.GardenUi).
            Image spark = Art("Progress spark", band, "fx/glow", 190, 15, 30, 32);
            spark.color = new Color(1f, 1f, 1f, .8f);
            // 2026-10-21: the percent a size larger (16 -> 18) and closer to the groove.
            RoundLabel("Progress percent", band, "{0}%", 459, 14f, 54, 20, GardenSkin.Hex(0x3F248A));   // 10-31: centred on the pill face (was ~3.5 px low). 2026-11-03 10:56 (user: text review): level 22 px, percent 20 px (were 18).
            return root;
        }
        static RectTransform Controls()
        {
            var root = Section("GardenBattleControls"); var band = Node("Bottom safe band", root, 0, 574, 540, 386);
            band.anchorMin = band.anchorMax = Vector2.zero; band.anchoredPosition = new Vector2(0, 386);
            var tutorial = Node("Tutorial hint", band, 18, 298, 504, 84);
            Art("Hint banner", tutorial, "scene/level", 0, 4, 504, 72, true, 24, 24, 18);
            Art("Hint icon", tutorial, "ui/bar-icon-bubbles", 24, 17, 48, 48);
            RoundLabel("Hint first line", tutorial, "看颜色 · 派队员 · 三合一", 80, 17, 394, 20, GardenBars.Navy);
            RoundLabel("Hint second line", tutorial, "守住护栏，别让泡泡压下来", 80, 46, 394, 16, GardenBars.Navy);
            var buttons = Node("Prop buttons", band, 0, 0, 540, 386);
            string[] keys = { "scene/button-shuffle", "scene/button-designate", "scene/button-water" };
            for (int i = 0; i < 3; i++)
            {
                var button = Action("Prop " + i, buttons, i + 10, 27 + i * 164, 306, 156, 72);
                Image art = Art("Button image", button, keys[i], 0, 0, 156, 72);
                button.GetComponent<GardenUiAction>().selectedVisual = art;
                // 2026-10-22: the same rainbow bomb as the revive reward and projectile, not baked into the base.
                if (i == 2) Art("Bomb icon", art.rectTransform, "ui/bar-icon-bomb", 0, 6, 54, 54);
                // 2026-10-21 10:44: the art is text-free now; the word sits right of the icon in the rounded font, white
                // with a dark outline of the button's own hue (sampled from the old baked words). It is a child of the
                // art so it moves with the pressed offset.
                string[] words = { "重排", "优先出战", "泡泡炸弹" };
                Color[] inks = { new Color32(0, 42, 193, 255), new Color32(187, 58, 0, 255), new Color32(54, 0, 184, 255) };
                float wordSize = i == 0 ? 24f : 20f, wordLeft = i == 0 ? 60f : 56f, wordWidth = i == 0 ? 96f : 92f;
                // The shallow lower lip leaves the face centre near 47% of the full height.
                GardenUiText word = RoundLabel("Word", art.rectTransform, words[i], wordLeft, 37f - wordSize * .5f, wordWidth, wordSize, Color.white);
                if (i == 2) word.alternateText = "取消";
                word.color = i == 2 ? Color.white : i == 0 ? GardenSkin.Hex(0x4E2BA0) : GardenSkin.Hex(0xA9470B);
                word.outlineWidth = i == 0 ? 0f : i == 2 ? 1.5f : .7f; word.outlineColor = i == 2 ? inks[i] : Color.white;
                // Centre the round badge on the button's top edge, half outside the face.
                Art("Count badge", button, "scene/badge", 130, -10, 24, 24);
                var amount = RoundLabel("Amount", button, "{0}", 130, -10, 24, 16, GardenSkin.Hex(0x57338F));
                amount.rectTransform.sizeDelta = new Vector2(24, 24);
                amount.middle = true;
                // The outer half remains tappable, including refill (+) and aiming cancel (×).
                Action("Count badge hit", button, i + 10, 124, -16, 36, 36);
                if (i == 2) Action("Cancel badge hit", button, 12, 120, -18, 40, 40).GetComponent<GardenUiAction>().interactable = false;
                RoundLabel("Cancel hint", button, "取消选择", 0, -22, 156, 15, GardenBars.Navy).gameObject.SetActive(false);
            }
            tutorial.gameObject.SetActive(false);
            return root;
        }
        static RectTransform Pause()
        {
            var root = Section("GardenPauseDialog");
            var settings = Node("Settings", root, 0, 0, 540, 960); Stretch(settings);
            var content = Modal(settings, "暂停一下", 205, 406, panelSlot: "plants/dialog/pause-panel");
            string[] names = { "Music", "Sound", "Light" }, labels = { "音乐", "音效", "光效" };
            string[] icons = { "plants/dialog/icon-music", "plants/dialog/icon-sound", "plants/dialog/icon-light" };
            // v3 (2026-10-15): plain setting rows (label left, switch right, thin dividers) instead of button-styled rows.
            // The whole row stays the hit area for the same action ids; the switch art shows the state.
            // 2026-10-21 18:11 (user): rounded type, a small icon before each label when delivered (ui/icon-*.png).
            for (int i = 0; i < 3; i++)
            {
                var row = Action(names[i], content, 20 + i, 88, 278 + 98 * i, 364, 80);
                Art("Row card", row, "plants/dialog/pause-row", 0, 0, 364, 80, true, 18, 18, 14);
                bool icon = HasSlot(icons[i]);
                if (icon) Art(names[i] + " icon", row, icons[i], 16, 14, 52, 52);
                var label = RoundLabel(names[i] + " text", row, labels[i], icon ? 88 : 20, 25, 164, 26, GardenSkin.Hex(0x1D3A7A));   // 2026-10-30 light theme
                label.centered = false; label.alternateText = labels[i];
                Art(names[i] + " toggle", row, "ui/toggle-on", 258, 16, 96, 48);
                if (i == 2 && HasSlot("plants/dialog/icon-pattern")) Art("Alt icon", row, "plants/dialog/icon-pattern", 16, 14, 52, 52).gameObject.SetActive(false);
            }
            // Reference row: home, restart, rewarded skip. The home settings hide all three.
            // 2026-10-29 21:07 (界面优化 9, user: 去掉跳关功能): no skip button; home + restart centred as a pair (332 wide).
            // 2026-10-30 (light theme): white 主页 · 重新开始 pills over a wide blue 继续游戏 (完成 on the home settings).
            ThemePill("Home", content, 25, 100, 642, 162, 62, "white", "主页", "plants/uiref/icon-home");
            ThemePill("Retry", content, 23, 278, 642, 162, 62, "white", "重新开始", "plants/uiref/icon-replay");
            ThemePill("Resume", content, 24, 120, 736, 300, 80, "blue", "继续游戏");   // 10-31: 8 px more air under the panel
            var close = Action("Close settings", content, 24, 457, 190, 50, 50);
            Art("Close image", close, "plants/dialog/close", 0, 0, 50, 50);
            // Home settings: a readable text link below the last toggle, with a generous hit target.
            var reset = Action("Reset progress", content, GardenBattlePage.ResetProgressAction, 140, 568, 260, 52);
            // 2026-10-30 (调整 5, reference): a bold red 「重置关卡进度」.
            var resetText = RoundLabel("Reset text", reset, "{0}", 0, 8, 260, 24, GardenSkin.Hex(0xE5484D));
            resetText.middle = true;
            // 2026-10-31 16:33 (settings reference): a fine divider between the toggles and 「重置关卡进度」 (home only, with it).
            var divider = Node("Divider", reset, -52, -6, 364, 2);
            var dividerImage = divider.gameObject.AddComponent<Image>(); dividerImage.raycastTarget = false;
            dividerImage.color = GardenSkin.Hex(0xDCE6F5);
            reset.gameObject.SetActive(false);
            var paused = Node("Paused", root, 0, 0, 540, 960); Stretch(paused);
            var simple = Modal(paused, "休息一下", 250, 370, panelSlot: "plants/dialog/pause-panel");
            ThemePill("Resume simple", simple, 1, 120, 474, 300, 80, "blue", "继续");
            paused.gameObject.SetActive(false);
            return root;
        }
        static RectTransform Result()
        {
            var root = Section("GardenResultDialog");
            var mission = Node("Mission result", root, 0, 0, 540, 960); Stretch(mission);
            // 2026-10-21 20:51: 630 -> 590 high (the panel only shows on a loss now; its buttons moved up into one row).
            var content = Modal(mission, "泡泡全打光啦！", 180, 590, "Result title");
            Find<GardenUiText>(content.gameObject, "Result title").alternateText = "失败了";
            Art("Portrait", content, "ui/guide-happy", 226, 210, 88, 88);
            Label("Result lesson", content, "泡泡课堂 {0}/2", 70, 300, 400, 22, Ink).gameObject.SetActive(false);
            Label("Result level", content, "第 {0} 关", 70, 300, 400, 22, Ink);
            Label("Result progress", content, "打掉泡泡 {0}%", 70, 345, 400, 24, Ink);
            var rewards = Node("Rewards", content, 0, 0, 540, 960);
            Label("Coins", rewards, "收获金币 +{0}", 70, 420, 400, 20, Ink);
            Label("Seeds", rewards, "收获碎片 +{0}", 70, 456, 400, 20, Ink);
            Label("Statistics", rewards, "最高连击 {0}  用时 {1} 秒", 70, 510, 400, 18, Ink);   // 2026-11-03 10:56 (user: text review): 18 px (was 16).
            Label("Campaign complete", content, "全部关卡通过", 70, 566, 400, 20, Ink);
            // 2026-10-29 (界面优化 7/17, user: 所有的结算都要统一): light-theme capsules. Loss: a wide blue 重新挑战 with white
            // 返回主页 · 分享 below; win: blue 主页 · 重玩 with a wide yellow 下一关 below (the overlays draw the rest).
            // 2026-10-30 (调整 17): positions and sizes measured from the reference pages.
            ThemePill("Home", content, 41, 72, 752, 204, 62, "white", "返回主页", "plants/uiref/icon-home");
            var next = ThemePill("Continue", content, 40, 97, 637, 366, 90, "blue", "下一关");
            var nextText = Find<GardenUiText>(next.gameObject, "Label");
            nextText.name = "Continue text"; nextText.alternateText = "重新挑战";
            // ID 0 consumes the tap without running a sharing action or touching the battle.
            ThemePill("Share", content, 0, 288, 752, 200, 62, "white", "分享", "plants/uiref/icon-share");
            var winButtons = Node("Win buttons", content, 0, 0, 540, 960);
            ThemePill("Win home", winButtons, 41, 88, 787, 174, 52, "blue", "主页", "plants/uiref/icon-home");
            ThemePill("Win replay", winButtons, 42, 283, 787, 175, 52, "blue", "重玩", "plants/uiref/icon-replay");
            var winNext = ThemePill("Win next", winButtons, 40, 162, 860, 224, 62, "yellow", "下一关");
            Find<GardenUiText>(winNext.gameObject, "Label").name = "Win next text";
            winButtons.gameObject.SetActive(false);
            var lesson = Node("Lesson complete", root, 0, 0, 540, 960); Center(lesson);
            Art("Panel", lesson, "ui/popup-panel", 70, 380, 400, 98, true);
            Art("Portrait", lesson, "ui/guide-happy", 20, 352, 96, 96);
            Label("Title", lesson, "泡泡课堂完成！", 90, 396, 400, 26, Cream, 2);
            Label("Message", lesson, "下一关见", 90, 440, 400, 18, Ink);
            lesson.gameObject.SetActive(false);
            return root;
        }
        static RectTransform Modal(RectTransform parent, string title, float y, float height, string titleName = "Title", string panelSlot = null)
        {
            Image shade = Solid("Modal input shield", parent, new Color(0, 0, 0, .65f), true);
            shade.gameObject.AddComponent<GardenUiAction>().actionId = 0;
            var content = Node("Content", parent, 0, 0, 540, 960); Center(content);
            // 2026-10-21 18:11: a white card panel when delivered (ui/panel-card.png, 9-sliced), else the blue glass panel.
            if (panelSlot != null) Art("Panel", content, panelSlot, 58, y, 424, height, true, 48, 48, 32);   // 2026-10-30: T10 corners
            else if (HasSlot("plants/ui/panel-card")) Art("Panel", content, "plants/ui/panel-card", 50, y, 440, height, true, 36, 36, 24);
            else Art("Panel", content, "ui/popup-panel", 50, y, 440, height, true);
            Art("Ribbon", content, "ui/popup-ribbon", 80, y - 26, 380, 60);
            Label(titleName, content, title, 80, y - 13, 380, 24, Cream, 2);
            return content;
        }
        // 2026-10-29 light-theme capsule (ui/button-blue, ui/button-yellow 390x108 with 54 px round ends; "white" is the white
        // pill scene/level, 384x72, 36 px ends), 3-sliced so the ends stay round; the old green capsule if the art is missing.
        static RectTransform ThemePill(string name, RectTransform parent, int id, float x, float y, float w, float h, string kind, string word, string icon = null)
        {
            string slot = kind == "white" ? (HasSlot("plants/uiref/button-white") ? "plants/uiref/button-white" : "scene/level")
                : kind == "yellow" ? "plants/uiref/button-yellow" : "plants/uiref/button-blue";
            if (!HasSlot(slot)) return CapsuleButton(name, parent, id, x, y, w, h, "plants/dialog/pause-retry", word, GardenSkin.Hex(0x537D2F));
            float stored = slot == "scene/level" ? 36f : 52f, size = Mathf.Min(32f, h * .4f);   // 2026-10-31: flat capsules, 52 px ends
            var root = Action(name, parent, id, x, y, w, h);
            // 2026-10-31 16:11 (button check against the reference): a soft dark shadow under the capsule, bigger words
            // (about half the button height) and icons a fifth larger than the words.
            if (HasSlot("fx/glow")) { var sh = Art("Shadow", root, "fx/glow", -w * .04f, h * .42f, w * 1.08f, h * .9f); sh.color = new Color(0f, .02f, .12f, .38f); }
            Art("Button image", root, slot, 0, 0, w, h, true, stored, 0f, h * .5f);
            bool light = kind == "white";
            size = Mathf.Min(32f, h * .48f, (w - 30f) / (word.Length + (icon != null ? 1.5f : 0f)));   // fits the word (and icon)
            Color ink = light ? GardenSkin.Hex(0x3A4A6B) : Color.white;
            // 2026-10-30: an optional glyph (T17) before the word, white on colour and navy-grey on white; the pair is
            // centred (CJK words ~1 em per character). 调整 (user): clean words like the reference, no outline.
            float shift = 0f;
            if (icon != null && HasSlot(icon))
            {
                float mark = size * 1.2f, gap = size * .3f, textW = word.Length * size;
                shift = (mark + gap) * .5f;
                var glyph = Art("Icon", root, icon, (w - mark - gap - textW) * .5f, h * .47f - mark * .5f, mark, mark);
                glyph.color = ink;
            }
            var label = RoundLabel("Label", root, word, shift * 2f, h * .47f - size * .5f, w - shift * 2f, size, ink);
            label.outlineWidth = 0f;
            // 2026-10-31 (reference 下一关): white words with a warm orange edge on the yellow capsule.
            if (kind == "yellow") { label.outlineColor = GardenSkin.Hex(0xCE720A); label.outlineWidth = size * .1f; }
            return root;
        }
        static RectTransform CapsuleButton(string name, RectTransform parent, int id, float x, float y, float w, float h, string slot, string word, Color outline)
        {
            // Same historical sprite and slicing as DialogGreenButton. The
            // complete rounded ends fit inside the 64 px atlas border.
            var root = Action(name, parent, id, x, y, w, h);
            Art("Button image", root, slot, 0, 0, w, h, true, 64f, 0f, h * 64f / 126f);
            var label = RoundLabel("Label", root, word, 0, h * .47f - 16f, w, 32f, Color.white);
            label.outlineWidth = 2.5f; label.outlineColor = outline;
            return root;
        }
        static RectTransform Button(string name, RectTransform parent, int id, float x, float y, float w, float h, string text, string labelName = "Label")
        {
            var root = Action(name, parent, id, x, y, w, h);
            bool primary = w >= 300 || id == 1 || id == 9;
            Art("Button image", root, primary ? "ui/button-primary" : "ui/button-secondary", 0, 0, w, h, true, primary ? 45 : 30, 0, h * (primary ? 45 : 30) / (primary ? 78 : 72));
            float size = Mathf.Min(20, h * .42f);
            Label(labelName, root, text, 0, (h - size) / 2 - 1, w, size, Cream, 1.8f);
            return root;
        }
        // Big green jelly button of the victory page (win/button-next), the blue primary as fallback; white rounded word.
        static RectTransform GreenButton(string name, RectTransform parent, int id, float x, float y, float w, float h, string word, float fontSize = 36f, float outlineWidth = 3f)
        {
            var root = Action(name, parent, id, x, y, w, h);
            bool painted = HasSlot("plants/win/button-next");
            if (painted) Art("Button image", root, "plants/win/button-next", 0, 0, w, h);
            else Art("Button image", root, "ui/button-primary", 0, 0, w, h, true, 45, 0, h * 45f / 78f);
            // 2026-10-22 11:27 (user): the base under the face is half as thick now (atlas script), so the face centre
            // moves from .42 to .46 of the height; the words grow 30 -> 36.
            var label = RoundLabel("Label", root, word, 0, h * .46f - fontSize * .5f, w, fontSize, Color.white);
            label.outlineWidth = outlineWidth; label.outlineColor = painted ? new Color32(0xA5, 0x54, 0x10, 0xFF) : new Color32(0x16, 0x3E, 0x6D, 0xFF);
            return root;
        }
        static bool HasSlot(string id) => id.StartsWith("bars/") ? barAtlas.slots.Any(s => s.id == id.Substring(5)) : atlas.slots.Any(s => s.id == id);
        // 76 x 76 square button: painted art with its icon when delivered, the light secondary glass otherwise; the
        // word sits in its lower part (white, outlined in the button's own dark hue; bigger and centred on the fallback).
        static RectTransform SquareButton(string name, RectTransform parent, int id, float x, float y, string art, string word, Color ink)
        {
            var root = Action(name, parent, id, x, y, 76, 76);
            bool painted = HasSlot(art);
            if (painted) Art("Button image", root, art, 0, 0, 76, 76);
            else Art("Button image", root, "ui/button-secondary", 0, 0, 76, 76, true, 30, 0, 76 * 30f / 72f);
            // 2026-10-22 (user: text touches the icons again): keep the glyph AND its outline in the lower blank band.
            // At 15 px, top 48 and outline 1.2, visible lettering spans ~47-64; the painted icon ends around 43.
            float size = painted ? 15f : 18f;
            var label = RoundLabel(name + " text", root, word, 0, painted ? 48f : (76f - size) * .5f - 1f, 76, size, Color.white);
            label.outlineWidth = painted ? 1.2f : 2.6f; label.outlineColor = painted ? ink : new Color32(0x16, 0x3E, 0x6D, 0xFF);
            return root;
        }
        static RectTransform Action(string name, RectTransform parent, int id, float x, float y, float w, float h)
        { var r = Node(name, parent, x, y, w, h); r.gameObject.AddComponent<GardenUiAction>().actionId = id; return r; }
        static GardenUiText Label(string name, RectTransform parent, string text, float x, float y, float w, float size, Color color, float outline = 0)
        {
            var r = Node(name, parent, x, y, w, size * 1.4f);
            var t = r.gameObject.AddComponent<GardenUiText>(); t.text = text; t.fontSize = size;
            t.color = color; t.outlineWidth = outline; t.SetFontAtlas(texture); return t;
        }
        static GardenUiText RoundLabel(string name, RectTransform parent, string text, float x, float y, float w, float size, Color color)
        {
            var t = Label(name, parent, text, x, y, w, size, color);
            t.roundedFont = true; t.SetFontAtlas(barTexture); return t;
        }
        static Image Glass(string name, RectTransform parent, float x, float y, float w, float h)
            => Art(name, parent, "bars/capsule", x, y, w, h, true, 144f * 1.1f, 0, h * 1.1f);
        static GardenSkin.Slot SlotFor(string id) => id.StartsWith("bars/")
            ? barAtlas.slots.Single(s => s.id == id.Substring(5)) : atlas.slots.Single(s => s.id == id);
        static Image Art(string name, RectTransform parent, string id, float x, float y, float w, float h, bool sliced = false, float bx = 30, float by = 30, float edge = 18)
        {
            var slot = SlotFor(id);
            // Images have real RectTransforms, including the original transparent-frame trim.
            if (!sliced && slot.fw > 0 && slot.fh > 0)
            { x += slot.ox * w / slot.fw; y += slot.oy * h / slot.fh; w *= slot.w / slot.fw; h *= slot.h / slot.fh; }
            var r = Node(name, parent, x, y, w, h);
            var image = r.gameObject.AddComponent<Image>(); image.raycastTarget = false;
            image.sprite = Sprite(id, sliced ? new Vector4(bx, by, bx, by) : Vector4.zero, sliced ? "-sliced" : "");
            if (sliced) { image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = bx / edge; }
            return image;
        }
        static Image Solid(string name, RectTransform parent, Color color, bool stretch)
        { var r = Node(name, parent, 0, 0, 540, 960); if (stretch) Stretch(r); var image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image; }
        static RectTransform Section(string name) { var root = Node(name, null, 0, 0, 540, 960); Stretch(root); return root; }
        static RectTransform Node(string name, Transform parent, float x, float y, float w, float h)
        {
            var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        static void Stretch(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static void Center(RectTransform r) { r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(540, 960); }
        static void SaveSection(RectTransform root, string name)
        { PrefabUtility.SaveAsPrefabAsset(root.gameObject, Root + "/" + name + ".prefab"); Object.DestroyImmediate(root.gameObject); }
        static GameObject Instance(string name, RectTransform parent, int order)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + name + ".prefab"), parent);
            var canvas = go.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = order;
            return go;
        }
        static T Find<T>(GameObject root, string name) where T : Component
            => root.GetComponentsInChildren<T>(true).First(c => c.name == name);
    }
}

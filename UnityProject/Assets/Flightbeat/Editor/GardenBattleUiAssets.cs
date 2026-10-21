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
            Solid("Sky base", root, GardenSkin.Hex(0x9FD4FF), true);
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
            var pause = Action("Pause button", band, 1, 6, 4, 56, 56);
            Art("Icon", pause, "bars/pause", 4, 4, 48, 48);
            Glass("Level plaque", band, 66, 12, 106, 38);
            RoundLabel("Lesson title", band, "泡泡课堂 {0}/2", 66, 19, 106, 15, GardenBars.Navy);
            RoundLabel("Level title", band, "第 {0} 关", 66, 18.4f, 106, 18, GardenBars.Navy).gameObject.SetActive(false);
            // 2026-10-21 10:44 (user: too much room around the number): the frame ends 14 px earlier on the right (344 -> 330).
            Glass("Progress frame", band, 180, 12, 330, 38);
            // 2026-10-21 (quality pass): the empty groove read as a dull grey bar. Now a pale frosted groove with a fine
            // inner shade along its top edge, like a channel pressed into the glass.
            var track = Glass("Progress track", band, 190, 22, GardenBattlePage.ProgressWidth, 18);
            track.color = new Color(.84f, .92f, .99f, .9f);
            var shade = Node("Progress track shade", band, 199, 23.5f, GardenBattlePage.ProgressWidth - 18f, 2f);
            var shadeImage = shade.gameObject.AddComponent<Image>(); shadeImage.raycastTarget = false;
            shadeImage.color = new Color(.09f, .2f, .38f, .16f);
            Image fill = Glass("Progress fill", band, 190, 22, GardenBattlePage.ProgressWidth * .35f, 18);
            fill.color = new Color(.25f, .78f, 1f, 1f);   // 2026-10-21: cyan-blue as in the design; mint stays for success hints
            // A soft spark rides the head of the fill (moved by PixelWingGame.GardenUi).
            Image spark = Art("Progress spark", band, "fx/glow", 190, 15, 30, 32);
            spark.color = new Color(1f, 1f, 1f, .8f);
            // 2026-10-21: the percent a size larger (16 -> 18) and closer to the groove.
            RoundLabel("Progress percent", band, "{0}%", 448, 18.6f, 56, 18, GardenBars.Navy);
            return root;
        }
        static RectTransform Controls()
        {
            var root = Section("GardenBattleControls"); var band = Node("Bottom safe band", root, 0, 574, 540, 386);
            band.anchorMin = band.anchorMax = Vector2.zero; band.anchoredPosition = new Vector2(0, 386);
            var tutorial = Node("Tutorial hint", band, 18, 298, 504, 84);
            Glass("Hint banner", tutorial, 0, 4, 504, 72);
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
                // 2026-10-21 10:44: the art is text-free now; the word sits right of the icon in the rounded font, white
                // with a dark outline of the button's own hue (sampled from the old baked words). It is a child of the
                // art so it moves with the pressed offset.
                string[] words = { "重排", "优先出战", "泡泡炸弹" };
                Color[] inks = { new Color32(0, 42, 193, 255), new Color32(187, 58, 0, 255), new Color32(54, 0, 184, 255) };
                float wordSize = i == 0 ? 24f : 20f, wordLeft = i == 0 ? 60f : 56f, wordWidth = i == 0 ? 96f : 92f;
                GardenUiText word = RoundLabel("Word", art.rectTransform, words[i], wordLeft, 36.5f - wordSize * .5f, wordWidth, wordSize, Color.white);
                word.outlineWidth = 2.2f; word.outlineColor = inks[i];
                // 2026-10-21 (user): the count / + / × corner badge goes back to the earlier navy badge and white number.
                Art("Count badge", button, "scene/badge", 134, -7, 24, 24);
                Label("Amount", button, "{0}", 134, -2.5f, 24, 14, Color.white);
                RoundLabel("Cancel hint", button, "取消选择", 0, -22, 156, 15, GardenBars.Navy).gameObject.SetActive(false);
            }
            tutorial.gameObject.SetActive(false);
            return root;
        }
        static RectTransform Pause()
        {
            var root = Section("GardenPauseDialog");
            var settings = Node("Settings", root, 0, 0, 540, 960); Stretch(settings);
            var content = Modal(settings, "暂停一下", 250, 402);
            string[] names = { "Music", "Sound", "Light" }, labels = { "音乐", "音效", "光效" };
            // v3 (2026-10-15): plain setting rows (label left, switch right, thin dividers) instead of button-styled rows.
            // The whole row stays the hit area for the same action ids; the switch art shows the state.
            for (int i = 0; i < 3; i++)
            {
                var row = Action(names[i], content, 20 + i, 84, 336 + 58 * i, 372, 48);
                var label = Label(names[i] + " text", row, labels[i], 20, 12, 200, 21, Ink);
                label.centered = false; label.alternateText = labels[i];
                Art(names[i] + " toggle", row, "ui/toggle-on", 296, 8, 60, 32);
                if (i < 2) { var line = Solid("Divider", row, new Color(.62f, .74f, .88f, .7f), false); var lr = line.rectTransform; lr.anchoredPosition = new Vector2(12, -52.5f); lr.sizeDelta = new Vector2(348, 1.5f); }
            }
            Button("Resume", content, 24, 84, 516, 372, 54, "继续战斗");
            Button("Retry", content, 23, 84, 580, 180, 46, "重新开始");
            Button("Home", content, 25, 276, 580, 180, 46, "返回主页");
            var paused = Node("Paused", root, 0, 0, 540, 960); Stretch(paused);
            var simple = Modal(paused, "休息一下", 250, 370);
            Button("Resume simple", simple, 1, 170, 490, 200, 52, "继续");
            paused.gameObject.SetActive(false);
            return root;
        }
        static RectTransform Result()
        {
            var root = Section("GardenResultDialog");
            var mission = Node("Mission result", root, 0, 0, 540, 960); Stretch(mission);
            var content = Modal(mission, "泡泡全打光啦！", 180, 630, "Result title");
            Find<GardenUiText>(content.gameObject, "Result title").alternateText = "这次被泡泡压住了";
            Art("Portrait", content, "ui/guide-happy", 226, 210, 88, 88);
            Label("Result lesson", content, "泡泡课堂 {0}/2", 70, 300, 400, 22, Ink).gameObject.SetActive(false);
            Label("Result level", content, "第 {0} 关", 70, 300, 400, 22, Ink);
            Label("Result progress", content, "打掉泡泡 {0}%", 70, 345, 400, 24, Ink);
            var rewards = Node("Rewards", content, 0, 0, 540, 960);
            Label("Coins", rewards, "收获金币 +{0}", 70, 420, 400, 20, Ink);
            Label("Seeds", rewards, "收获碎片 +{0}", 70, 456, 400, 20, Ink);
            Label("Statistics", rewards, "最高连击 {0}  用时 {1} 秒", 70, 510, 400, 16, Ink);
            Label("Campaign complete", content, "全部关卡通过", 70, 566, 400, 20, Ink);
            var next = Button("Continue", content, 40, 80, 650, 380, 62, "下一关", "Continue text");
            Find<GardenUiText>(next.gameObject, "Continue text").alternateText = "再试一次";
            Button("Home", content, 41, 180, 740, 180, 40, "返回主页");
            // 2026-10-21 (user, victory page after a reference): one row of a small square 主页, a big 下一关 and a small
            // square 重玩 (action 42). Shown only on a win (GardenBattlePage.SetWinLayout hides Continue / Home then);
            // the lose page keeps 再试一次 / 返回主页. Painted art when delivered (win/*.png), program fallback before.
            var winButtons = Node("Win buttons", content, 0, 0, 540, 960);
            // 15:51 (user: too far below the info): moved up 70 px.
            SquareButton("Win home", winButtons, 41, 70, 702, "plants/win/button-home", "主页", new Color32(0xB8, 0x65, 0x0A, 0xFF));
            SquareButton("Win replay", winButtons, 42, 394, 702, "plants/win/button-replay", "重玩", new Color32(0x4E, 0x1F, 0xA8, 0xFF));
            var winNext = Action("Win next", winButtons, 40, 166, 696, 208, 84);
            if (HasSlot("plants/win/button-next")) Art("Button image", winNext, "plants/win/button-next", 0, 0, 208, 84);
            else Art("Button image", winNext, "ui/button-primary", 0, 0, 208, 84, true, 45, 0, 84 * 45f / 78f);
            // 15:59 (user: words not centred): centred on the button face (rows 4-134 of 168, i.e. 2-67 of 84), not
            // on the whole art with its thick lower edge.
            var nextLabel = RoundLabel("Win next text", winNext, "下一关", 0, 19f, 208, 32, Color.white);
            nextLabel.outlineWidth = 3f; nextLabel.outlineColor = HasSlot("plants/win/button-next") ? new Color32(0x1B, 0x7A, 0x2A, 0xFF) : new Color32(0x16, 0x3E, 0x6D, 0xFF);
            winButtons.gameObject.SetActive(false);
            var lesson = Node("Lesson complete", root, 0, 0, 540, 960); Center(lesson);
            Art("Panel", lesson, "ui/popup-panel", 70, 380, 400, 98, true);
            Art("Portrait", lesson, "ui/guide-happy", 20, 352, 96, 96);
            Label("Title", lesson, "泡泡课堂完成！", 90, 396, 400, 26, Cream, 2);
            Label("Message", lesson, "下一关见", 90, 440, 400, 18, Ink);
            lesson.gameObject.SetActive(false);
            return root;
        }
        static RectTransform Modal(RectTransform parent, string title, float y, float height, string titleName = "Title")
        {
            Image shade = Solid("Modal input shield", parent, new Color(0, 0, 0, .65f), true);
            shade.gameObject.AddComponent<GardenUiAction>().actionId = 0;
            var content = Node("Content", parent, 0, 0, 540, 960); Center(content);
            Art("Panel", content, "ui/popup-panel", 50, y, 440, height, true);
            Art("Ribbon", content, "ui/popup-ribbon", 80, y - 26, 380, 60);
            Label(titleName, content, title, 80, y - 13, 380, 24, Cream, 2);
            return content;
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
        static bool HasSlot(string id) => id.StartsWith("bars/") ? barAtlas.slots.Any(s => s.id == id.Substring(5)) : atlas.slots.Any(s => s.id == id);
        // 76 x 76 square button: painted art with its icon when delivered, the light secondary glass otherwise; the
        // word sits in its lower part (white, outlined in the button's own dark hue; bigger and centred on the fallback).
        static RectTransform SquareButton(string name, RectTransform parent, int id, float x, float y, string art, string word, Color ink)
        {
            var root = Action(name, parent, id, x, y, 76, 76);
            bool painted = HasSlot(art);
            if (painted) Art("Button image", root, art, 0, 0, 76, 76);
            else Art("Button image", root, "ui/button-secondary", 0, 0, 76, 76, true, 30, 0, 76 * 30f / 72f);
            // 15:59 (user: the words touched the edge): centred in the free band between the icon (ends at row 86 of
            // 152) and the thick lower rim (starts at 134), i.e. around y 55 of 76; one size larger.
            float size = painted ? 14f : 18f;
            var label = RoundLabel(name + " text", root, word, 0, painted ? 48f : (76f - size) * .5f - 1f, 76, size, Color.white);
            label.outlineWidth = painted ? 2.4f : 2.6f; label.outlineColor = painted ? ink : new Color32(0x16, 0x3E, 0x6D, 0xFF);
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

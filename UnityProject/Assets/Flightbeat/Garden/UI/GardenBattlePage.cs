#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Garden.UI
{
    // Layout and static copy live in nested prefabs. This view only binds state.
    [AddComponentMenu("Flightbeat/Garden UI/Battle Page")]
    public sealed class GardenBattlePage : MonoBehaviour
    {
        public const float ProgressWidth = 256f;   // 2026-10-21: 266 -> 256, room for the larger percent
        [Header("Layers and responsive containers")]
        public GameObject background, header, controls, pauseDialog, resultDialog;
        public RectTransform backgroundTop, backgroundField, backgroundBottom, headerBand, controlsBand;
        [Header("Header")]
        public GardenUiText lessonTitle, levelTitle, progressText;
        public Image progressFill, progressSpark;
        [Header("Controls")]
        public GameObject tutorialHint, propButtons;
        public GardenUiAction[] props;
        public GardenUiText[] propAmounts;
        public GameObject[] propCancelLabels;
        [Header("Pause")]
        public GameObject settingsPanel, pausedPanel;
        public GardenUiText musicText, soundText, lightText;
        public Image[] toggleIcons;
        public Sprite toggleOn, toggleOff;
        [Header("Result")]
        public GameObject missionResult, lessonComplete, rewardGroup, campaignComplete;
        public GardenUiText resultTitle, resultLesson, resultLevel, resultProgress, coins, seeds, statistics, resultContinue;
        public Image resultPortrait;
        public Sprite happyPortrait, worriedPortrait;

        // Home-only "reset level progress" button: a runtime copy of the Retry button, which the
        // home settings hide. Its action ID sits beside the home page's 800 (start) and 801 (settings).
        public const int ResetProgressAction = 802;
        GameObject resetProgress;
        GardenUiText resetLabel;

        // 2026-10-19 B level set: home-only settings row 「新图案关卡」 with a switch, a runtime copy of the 光效 row
        // placed under it. While it shows, the panel grows by one row and the buttons below move down with it.
        // Hidden (and the panel unchanged) when no B level is available.
        public const int LevelSetAction = 805;
        const float LevelSetRow = 58f;
        GameObject levelSetRow, levelSetDivider;
        Image levelSetToggle;
        RectTransform settingsPanelArt, resumeRect;
        float panelHeight, resumeY, resetY;
        bool levelSetShown;

        PixelWingGame owner;
        GardenUiAction[] headerActions, controlActions, pauseActions, resultActions;
        bool visible;
        GardenUiText pauseTitle, resumeLabel;
        float layoutHeight = -1, layoutWidth = -1, layoutTop = -1, layoutBottom = -1;
        public void Bind(PixelWingGame game)
        {
            owner = game;
            // 2026-10-16: header labels sit in the middle of their plaque / progress bar (were hanging high).
            foreach (var label in new[] { levelTitle, lessonTitle, progressText })
                if (label) { label.middle = true; label.SetVerticesDirty(); }
            headerActions = header.GetComponentsInChildren<GardenUiAction>(true);
            controlActions = controls.GetComponentsInChildren<GardenUiAction>(true);
            pauseActions = pauseDialog.GetComponentsInChildren<GardenUiAction>(true);
            resultActions = resultDialog.GetComponentsInChildren<GardenUiAction>(true);
            foreach (var text in settingsPanel.GetComponentsInChildren<GardenUiText>(true))
                if (text.text == "暂停一下") { pauseTitle = text; pauseTitle.alternateText = "设置"; }
            foreach (var action in pauseActions)
                if (action.actionId == 24)
                { resumeLabel = action.GetComponentInChildren<GardenUiText>(true); resumeLabel.alternateText = "完  成"; }
            CreateResetProgress();
            CreateLevelSetRow();
            CreateMainButtonLabels();
            LightenDialogTitles();
            FindWinParts();
            // 2026-10-17 (user, readability): no prefab label under 15 logical px (~11 pt on a 390 pt wide phone).
            foreach (var text in GetComponentsInChildren<GardenUiText>(true))
                if (text.fontSize < 15f) { text.fontSize = 15f; text.SetVerticesDirty(); }
            ApplyLayout();
        }

        // 2026-10-17: dialog titles no longer sit on a big blue ribbon (it read like a heavy button): the ribbon
        // is hidden and the title becomes larger white lettering with a navy outline just inside the panel top.
        void LightenDialogTitles()
        {
            foreach (string path in new[] { "Paused/Content", "Settings/Content" })
            {
                var content = pauseDialog.transform.Find(path);
                if (!content) continue;
                var ribbon = content.Find("Ribbon");
                if (ribbon) ribbon.gameObject.SetActive(false);
                var title = content.Find("Title");
                if (title) RestyleTitle(title.GetComponent<GardenUiText>(), -268f);
                // 18:11: the same deep indigo shade as the victory page (was black 65 %).
                var shade = content.parent ? content.parent.Find("Modal input shield") : null;
                var shadeImage = shade ? shade.GetComponent<Image>() : null;
                if (shadeImage) shadeImage.color = WinShade;
            }
            var mission = resultDialog.transform.Find("Mission result/Content");
            if (mission)
            {
                var ribbon = mission.Find("Ribbon");
                if (ribbon) ribbon.gameObject.SetActive(false);
            }
            RestyleTitle(resultTitle, -198f);
            if (resultTitle) resultTitle.alternateText = "泡泡碰到护栏了";   // 16:09 (user): say what happened (was 这次被泡泡压住了)
        }
        static void RestyleTitle(GardenUiText t, float y)
        {
            if (!t) return;
            t.roundedFont = true;   // 2026-10-21 18:11: the rounded type of the rest of the bubble UI
            var r = t.rectTransform;
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, y);
            r.sizeDelta = new Vector2(r.sizeDelta.x, 40f);
            t.fontSize = 30f; t.outlineWidth = 2.6f; t.color = Color.white; t.middle = true; t.SetVerticesDirty(); t.SetMaterialDirty();
        }

        // 2026-10-17 victory screen (after the lightup reference): no panel on a win. The panel, portrait, title,
        // level/progress lines and rewards are hidden and the overlay draws headline, hero, confetti and stats;
        // the 下 一 关 / 返回主页 buttons stay where they are.
        GameObject[] winHidden = new GameObject[0], loseHidden = new GameObject[0], winOnly = new GameObject[0];
        // 2026-10-21 15:55 (user, after the reference): on a win the shade is a deep indigo at 80 % (was black 65 %), so
        // the board behind all but disappears in a clean night blue and the card, ribbon and buttons stand out.
        UnityEngine.UI.Image resultShield;
        Color resultShieldColor;
        static readonly Color WinShade = new Color(.055f, .078f, .2f, .8f);
        void FindWinParts()
        {
            var win = new System.Collections.Generic.List<GameObject>();
            var both = new System.Collections.Generic.List<GameObject>();
            var mission = resultDialog.transform.Find("Mission result/Content");
            var only = new System.Collections.Generic.List<GameObject>();
            if (mission)
            {
                // 2026-10-21: Continue / Home make way on a win for the 主页 · 下一关 · 重玩 row ("Win buttons").
                foreach (string name in new[] { "Panel", "Portrait", "Campaign complete", "Continue", "Home" })
                {
                    var child = mission.Find(name);
                    if (child) (name == "Portrait" ? both : win).Add(child.gameObject);
                }
                var row = mission.Find("Win buttons");
                if (row) only.Add(row.gameObject);
            }
            winOnly = only.ToArray();
            var shield = resultDialog.transform.Find("Mission result/Modal input shield");
            resultShield = shield ? shield.GetComponent<UnityEngine.UI.Image>() : null;
            if (resultShield) resultShieldColor = resultShield.color;
            if (resultTitle) win.Add(resultTitle.gameObject);
            foreach (var t in new[] { resultLevel, resultProgress })
                if (t) both.Add(t.gameObject);
            winHidden = win.ToArray(); loseHidden = both.ToArray();
        }
        // 2026-10-17: on a loss the portrait / level / progress labels make way for the overlay's fail page
        // (BubbleLose); the panel, the title and the buttons stay.
        public void SetWinLayout(bool win, bool lose = false)
        {
            foreach (var go in winHidden) if (go && go.activeSelf == win) go.SetActive(!win);
            foreach (var go in winOnly) if (go && go.activeSelf != win) go.SetActive(win);
            if (resultShield) { Color want = win ? WinShade : resultShieldColor; if (resultShield.color != want) resultShield.color = want; }
            bool hide = win || lose;
            foreach (var go in loseHidden) if (go && go.activeSelf == hide) go.SetActive(!hide);
        }

        // 2026-10-16 22:50: main-action buttons get a big spaced title and a small level line under it, like 开 始.
        public GardenUiText resultContinueSub, resumeSub, resumeSimpleSub;
        void CreateMainButtonLabels()
        {
            if (resultContinue)
            {
                resultContinue.text = "下 一 关"; resultContinue.alternateText = "再 试 一 次";
                CenterButtonLabel(resultContinue, 62f, 26f);
            }
            // 2026-10-21 18:11: green jelly buttons with rounded type, no letter spacing; centred on the button face
            // (the art's top 70 of 84 px; the rest is its thick lower edge).
            if (resumeLabel)
            {
                resumeLabel.text = "继续战斗"; resumeLabel.alternateText = "完成";   // 21:10 (user): home settings button reads 完成
                CenterButtonLabel(resumeLabel, 70f, 30f);
            }
            foreach (var text in pausedPanel.GetComponentsInChildren<GardenUiText>(true))
                if (text.text == "继续") { CenterButtonLabel(text, 70f, 30f); break; }
        }
        // 2026-10-17 11:10: no level line under these; the bigger label is centred on its whole button.
        static void CenterButtonLabel(GardenUiText main, float buttonHeight, float size)
        {
            var r = main.rectTransform;
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, 0f);
            r.sizeDelta = new Vector2(r.sizeDelta.x, buttonHeight);
            main.fontSize = size; main.middle = true; main.SetVerticesDirty();
        }
        // The label takes the upper ~62 % of its button (bigger, centred), a clone below shows "第 {0} 关".
        static GardenUiText SplitButtonLabel(GardenUiText main, float buttonHeight, float size)
        {
            var mainRect = main.rectTransform;
            var sub = Instantiate(main.gameObject, mainRect.parent, false).GetComponent<GardenUiText>();
            sub.name = main.name + " level";
            float titleH = buttonHeight * .62f, top = buttonHeight * .06f;
            mainRect.anchoredPosition = new Vector2(mainRect.anchoredPosition.x, -top);
            mainRect.sizeDelta = new Vector2(mainRect.sizeDelta.x, titleH);
            main.fontSize = size; main.middle = true; main.SetVerticesDirty();
            var subRect = sub.rectTransform;
            subRect.anchoredPosition = new Vector2(mainRect.anchoredPosition.x, -(top + titleH - 3f));
            subRect.sizeDelta = new Vector2(mainRect.sizeDelta.x, buttonHeight * .26f);
            sub.text = "第 {0} 关"; sub.alternateText = "";
            sub.fontSize = Mathf.Round(buttonHeight * .23f); sub.color = new Color32(0xFF, 0xF3, 0xB0, 0xFF);
            sub.outlineWidth = Mathf.Min(main.outlineWidth, 1.4f);
            sub.middle = true; sub.SetVerticesDirty();
            return sub;
        }
        void CreateResetProgress()
        {
            // 2026-10-21 18:11: the prefab now carries a quiet 「重置关卡进度」 text link (Settings/Content/Reset progress).
            var link = pauseDialog.transform.Find("Settings/Content/Reset progress");
            if (link && !resetProgress)
            {
                resetProgress = link.gameObject;
                resetLabel = resetProgress.GetComponentInChildren<GardenUiText>(true);
                if (resetLabel) { resetLabel.middle = true; resetLabel.SetVerticesDirty(); }
                resetProgress.SetActive(false);
                pauseActions = pauseDialog.GetComponentsInChildren<GardenUiAction>(true);
                return;
            }
            GardenUiAction retry = null;
            foreach (var action in pauseActions) if (action.actionId == 23) { retry = action; break; }
            if (!retry || resetProgress) return;
            resetProgress = Instantiate(retry.gameObject, retry.transform.parent, false);
            resetProgress.name = "Reset progress";
            var rect = (RectTransform)resetProgress.transform;
            rect.anchoredPosition = new Vector2(270 - rect.sizeDelta.x / 2, rect.anchoredPosition.y);   // centred in the Retry/Home row
            resetProgress.GetComponent<GardenUiAction>().actionId = ResetProgressAction;
            resetLabel = resetProgress.GetComponentInChildren<GardenUiText>(true);
            resetLabel.text = "{0}"; resetLabel.alternateText = "";
            resetProgress.SetActive(false);
            pauseActions = pauseDialog.GetComponentsInChildren<GardenUiAction>(true);
        }
        public void PresentResetProgress(string label) { if (resetLabel) resetLabel.Present(false, label); }

        void CreateLevelSetRow()
        {
            GardenUiAction light = null, sound = null, resume = null;
            foreach (var action in pauseActions)
            {
                if (action.actionId == 22) light = action;
                else if (action.actionId == 21) sound = action;
                else if (action.actionId == 24) resume = action;
            }
            if (!light || !resume || levelSetRow) return;
            levelSetRow = Instantiate(light.gameObject, light.transform.parent, false);
            levelSetRow.name = "Level set";
            var rect = (RectTransform)levelSetRow.transform;
            rect.anchoredPosition = ((RectTransform)light.transform).anchoredPosition - new Vector2(0, LevelSetRow);
            levelSetRow.GetComponent<GardenUiAction>().actionId = LevelSetAction;
            // 2026-10-21 18:11 (user): a wording players understand (was 新图案关卡); its own icon when delivered.
            foreach (var text in levelSetRow.GetComponentsInChildren<GardenUiText>(true)) { text.text = text.alternateText = "新版关卡图案"; text.SetVerticesDirty(); }
            var alt = levelSetRow.transform.Find("Alt icon");
            if (alt) { alt.gameObject.SetActive(true); var own = levelSetRow.transform.Find("Light icon"); if (own) own.gameObject.SetActive(false); }
            levelSetToggle = levelSetRow.GetComponentsInChildren<Image>(true).Length > 0 ? FindToggle(levelSetRow.transform) : null;
            // The 光效 row has no divider under it (it was the last row); borrow the 音效 row's.
            var divider = sound ? sound.transform.Find("Divider") : null;
            if (divider) { levelSetDivider = Instantiate(divider.gameObject, light.transform, false); levelSetDivider.SetActive(false); }
            levelSetRow.SetActive(false);
            settingsPanelArt = pauseDialog.transform.Find("Settings/Content/Panel") as RectTransform;
            resumeRect = (RectTransform)resume.transform;
            if (settingsPanelArt) panelHeight = settingsPanelArt.sizeDelta.y;
            resumeY = resumeRect.anchoredPosition.y;
            resetY = resetProgress ? ((RectTransform)resetProgress.transform).anchoredPosition.y : 0f;
            pauseActions = pauseDialog.GetComponentsInChildren<GardenUiAction>(true);
        }
        static Image FindToggle(Transform row)
        {
            foreach (var image in row.GetComponentsInChildren<Image>(true))
                if (image.name.EndsWith(" toggle")) return image;
            return null;
        }
        // show: home settings and at least one B level; on: the B set is selected.
        public void PresentLevelSet(bool show, bool on)
        {
            if (!levelSetRow) return;
            if (show != levelSetShown)
            {
                levelSetShown = show;
                levelSetRow.SetActive(show);
                if (levelSetDivider) levelSetDivider.SetActive(show);
                float shift = show ? LevelSetRow : 0f;
                if (settingsPanelArt) settingsPanelArt.sizeDelta = new Vector2(settingsPanelArt.sizeDelta.x, panelHeight + shift + (homeMode ? HomeExtra : 0f));
                resumeRect.anchoredPosition = new Vector2(resumeRect.anchoredPosition.x, resumeY - shift);
                if (resetProgress)
                {
                    var r = (RectTransform)resetProgress.transform;
                    r.anchoredPosition = new Vector2(r.anchoredPosition.x, resetY - shift);
                }
            }
            if (show && levelSetToggle) levelSetToggle.sprite = on ? toggleOn : toggleOff;
        }

        // Home key art background (Art/R1/home-v1 H2): a full-screen image over the battle background bands,
        // shown only on the home page. The source is 1080x2340 with the 9:16 core picture at y 210-2130; the
        // core stays vertically centred on screen and taller screens reveal the extra sky and cloud sea.
        RawImage homeBackground;
        bool homeBackgroundChecked;
        public bool ShowHomeBackground(bool show)
        {
            if (!homeBackgroundChecked)
            {
                homeBackgroundChecked = true;
                var texture = Resources.Load<Texture2D>("GardenR1/home-background");
                if (texture)
                {
                    var go = new GameObject("Home background", typeof(RectTransform), typeof(RawImage));
                    var rect = (RectTransform)go.transform;
                    rect.SetParent(background.transform, false);
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                    homeBackground = go.GetComponent<RawImage>();
                    homeBackground.texture = texture; homeBackground.raycastTarget = false;
                }
            }
            if (!homeBackground) return false;
            homeBackground.gameObject.SetActive(show);
            if (show) homeBackground.uvRect = PixelWingGame.HomeArtView(out _, out _, out _);
            return true;
        }
        void LateUpdate()
        {
            if (!owner) return;
            ApplyLayout();
            owner.RefreshGardenUi(this);
        }
        public void Show(bool value)
        {
            visible = value;
            background.SetActive(value); header.SetActive(value); controls.SetActive(value);
            if (!value) { pauseDialog.SetActive(false); resultDialog.SetActive(false); }
        }
        // 2026-10-21 18:11: in home settings the panel grows a little for the 重置关卡进度 link under 完成.
        bool homeMode;
        const float HomeExtra = 34f;
        public void SetHomeMode(bool home)
        {
            if (home != homeMode && settingsPanelArt)
            {
                homeMode = home;
                settingsPanelArt.sizeDelta = new Vector2(settingsPanelArt.sizeDelta.x, panelHeight + (levelSetShown ? LevelSetRow : 0f) + (home ? HomeExtra : 0f));
            }
            homeMode = home;
            if (pauseTitle) pauseTitle.Present(home);
            if (resumeLabel) resumeLabel.Present(home);
            foreach (var action in pauseActions)
                if (action.actionId == 23 || action.actionId == 25) action.gameObject.SetActive(!home);
            if (resetProgress) resetProgress.SetActive(home);
        }
        public void ShowHome(bool settings)
        {
            visible = true;
            background.SetActive(true); header.SetActive(false); controls.SetActive(false);
            pauseDialog.SetActive(settings); settingsPanel.SetActive(settings); pausedPanel.SetActive(false);
            resultDialog.SetActive(false);
        }
        public void ApplyLayout()
        {
            if (layoutHeight == ScreenLayout.Height && layoutWidth == ScreenLayout.Width && layoutTop == ScreenLayout.Top && layoutBottom == ScreenLayout.Bottom) return;
            layoutHeight = ScreenLayout.Height; layoutWidth = ScreenLayout.Width; layoutTop = ScreenLayout.Top; layoutBottom = ScreenLayout.Bottom;
            headerBand.anchoredPosition = new Vector2(0, -ScreenLayout.Top);
            controlsBand.anchoredPosition = new Vector2(0, ScreenLayout.Bottom + 386);
            Band(backgroundTop, 0, ScreenLayout.Y(114));
            Band(backgroundField, ScreenLayout.Y(114), ScreenLayout.Y(574));
            Band(backgroundBottom, ScreenLayout.Y(574), ScreenLayout.Height);
        }
        static void Band(RectTransform rect, float top, float bottom)
        {
            rect.anchoredPosition = new Vector2(0, -top);
            rect.sizeDelta = new Vector2(540, bottom - top);
        }
        public bool TryHit(Vector3 world, out int id)
        {
            id = -1;
            if (!visible) return false;
            // Identical precedence to the former paint order: result covers pause,
            // modal shade blocks all lower actions, then controls and header.
            if (Hit(resultActions, world, out id)) return true;
            if (Hit(pauseActions, world, out id))
            {
                // 2026-10-17 (user): a tap on the shade outside the settings / pause panel closes it, as that
                // panel's own resume button would (settings: 24, simple pause: 1).
                if (id == 0) id = OutsideOpenPanel(world, out int resume) ? resume : 0;
                return true;
            }
            return Hit(controlActions, world, out id) || Hit(headerActions, world, out id);
        }
        bool OutsideOpenPanel(Vector3 world, out int resume)
        {
            resume = 0;
            foreach (var (path, action) in new[] { ("Settings/Content/Panel", 24), ("Paused/Content/Panel", 1) })
            {
                var panel = pauseDialog ? pauseDialog.transform.Find(path) as RectTransform : null;
                if (!panel || !panel.gameObject.activeInHierarchy) continue;
                resume = action;
                return !panel.rect.Contains(panel.InverseTransformPoint(world));
            }
            return false;
        }
        static bool Hit(GardenUiAction[] actions, Vector3 world, out int id)
        {
            if (actions != null)
                for (int i = actions.Length - 1; i >= 0; i--)
                    if (actions[i].Contains(world)) { id = actions[i].actionId; return true; }
            id = -1; return false;
        }
    }
}
#endif

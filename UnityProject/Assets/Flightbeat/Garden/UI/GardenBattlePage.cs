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
        // 2026-10-31: the green fill sits inside the track's groove: 7.5 px in from each end, 10.6 px tall.
        public const float ProgressInset = 7.5f, ProgressFillWidth = ProgressWidth - 2f * ProgressInset, ProgressFillHeight = 14.6f;   // 2026-11-03 17:36 (user: thicker bar): 10.6 -> 14.6, with the track 22 -> 26
        // 2026-11-06 (user: adapt to the capsule, header in its row): the fill's full width for the current layout -
        // ProgressFillWidth, less what the bar gives up so it ends left of the capsule (ApplyLayout).
        public float ProgressFillWidthNow { get; private set; } = ProgressFillWidth;
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
        public GardenUiText bombPropWord;
        public GardenUiAction bombCancelBadge;
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
        public const int SkipLevelAction = 27;
        GameObject resetProgress;
        GardenUiText resetLabel;

        // 2026-10-19 B level set: home-only settings row 「新图案关卡」 with a switch, a runtime copy of the 光效 row
        // placed under it. While it shows, the panel grows by one row and the buttons below move down with it.
        // Hidden (and the panel unchanged) when no B level is available.
        RectTransform settingsPanelArt, resumeRect;
        float panelHeight, resumeY, resetY;

        PixelWingGame owner;
        GardenUiAction[] headerActions, controlActions, pauseActions, resultActions;
        bool visible;
        GardenUiText pauseTitle, resumeLabel;
        bool layoutPlush;
        float layoutHeight = -1, layoutWidth = -1, layoutTop = -1, layoutBottom = -1, layoutMenu = -1;
        // The progress row as authored (frame, track, shade, percent): x and width, so the capsule fit can be re-applied.
        RectTransform[] progressParts; Vector2[] progressAuthored;
        public void Bind(PixelWingGame game)
        {
            owner = game;
            // 2026-10-16: header labels sit in the middle of their plaque / progress bar (were hanging high).
            foreach (var label in new[] { levelTitle, lessonTitle, progressText })
                if (label) { label.middle = true; label.SetVerticesDirty(); }
            foreach (var label in propAmounts)
                if (label) { label.middle = true; label.SetVerticesDirty(); }
            headerActions = header.GetComponentsInChildren<GardenUiAction>(true);
            controlActions = controls.GetComponentsInChildren<GardenUiAction>(true);
            pauseActions = pauseDialog.GetComponentsInChildren<GardenUiAction>(true);
            resultActions = resultDialog.GetComponentsInChildren<GardenUiAction>(true);
            foreach (var text in settingsPanel.GetComponentsInChildren<GardenUiText>(true))
                if (text.text == "暂停一下") { pauseTitle = text; pauseTitle.alternateText = "设置"; }
            foreach (var action in pauseActions)
                if (action.actionId == 24 && action.name == "Resume")
                { resumeLabel = action.GetComponentInChildren<GardenUiText>(true); resumeLabel.alternateText = "完  成"; }
            CreateResetProgress();
            CaptureSettingsLayout();
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
                if (title) RestyleTitle(title.GetComponent<GardenUiText>(), path.StartsWith("Settings/") ? -222f : -268f);
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
            if (resultTitle) resultTitle.alternateText = "挑战失败";
        }
        static void RestyleTitle(GardenUiText t, float y)
        {
            if (!t) return;
            t.roundedFont = true;
            t.dialogTitleFont = true;
            var r = t.rectTransform;
            r.anchoredPosition = new Vector2(r.anchoredPosition.x, y);
            r.sizeDelta = new Vector2(r.sizeDelta.x, 40f);
            // 20:30 (user): on the white card the title is deep navy with a fine white edge (white lettering looked faint).
            t.fontSize = GardenTitleArt.Size; t.outlineWidth = GardenTitleArt.EdgeWidth;
            t.outlineColor = Color.white; t.color = GardenTitleArt.Ink;
            t.middle = true; t.SetVerticesDirty(); t.SetMaterialDirty();
        }

        // 2026-10-17 victory screen (after the lightup reference): no panel on a win. The panel, portrait, title,
        // level/progress lines and rewards are hidden and the overlay draws headline, hero, confetti and stats;
        // the 下 一 关 / 返回主页 buttons stay where they are.
        GameObject[] winHidden = new GameObject[0], loseHidden = new GameObject[0], winOnly = new GameObject[0];
        // 2026-10-21 15:55 (user, after the reference): on a win the shade is a deep indigo at 80 % (was black 65 %), so
        // the board behind all but disappears in a clean night blue and the card, ribbon and buttons stand out.
        UnityEngine.UI.Image resultShield;
        Color resultShieldColor;
        // 2026-11-05 (user: the same as 沙画消消): plain black at 200/255, as every popup there (was indigo .055/.078/.2 at 80 %).
        static readonly Color WinShade = new Color(0f, 0f, 0f, 200f / 255f);
        void FindWinParts()
        {
            var win = new System.Collections.Generic.List<GameObject>();
            var both = new System.Collections.Generic.List<GameObject>();
            var mission = resultDialog.transform.Find("Mission result/Content");
            var only = new System.Collections.Generic.List<GameObject>();
            if (mission)
            {
                // Both results use standalone ribbon/hero overlays; only their action rows differ.
                foreach (string name in new[] { "Panel", "Portrait", "Campaign complete", "Home", "Continue", "Share" })
                {
                    var child = mission.Find(name);
                    if (child) (name == "Portrait" || name == "Panel" ? both : win).Add(child.gameObject);
                }
                var row = mission.Find("Win buttons");
                if (row) only.Add(row.gameObject);
            }
            winOnly = only.ToArray();
            var shield = resultDialog.transform.Find("Mission result/Modal input shield");
            resultShield = shield ? shield.GetComponent<UnityEngine.UI.Image>() : null;
            if (resultShield) resultShieldColor = resultShield.color;
            if (resultTitle) both.Add(resultTitle.gameObject);
            foreach (var t in new[] { resultLevel, resultProgress })
                if (t) both.Add(t.gameObject);
            winHidden = win.ToArray(); loseHidden = both.ToArray();
        }
        // The result overlays draw their own ribbon and hero; prefabs retain the shade and action rows.
        public void SetWinLayout(bool win, bool lose = false)
        {
            foreach (var go in winHidden) if (go && go.activeSelf == win) go.SetActive(!win);
            foreach (var go in winOnly) if (go && go.activeSelf != win) go.SetActive(win);
            // 20:51 (user): the fail page gets the same deep indigo shade as the victory page and the other dialogs.
            if (resultShield) { Color want = win || lose ? WinShade : resultShieldColor; if (resultShield.color != want) resultShield.color = want; }
            bool hide = win || lose;
            foreach (var go in loseHidden) if (go && go.activeSelf == hide) go.SetActive(!hide);
        }

        // 2026-10-16 22:50: main-action buttons get a big spaced title and a small level line under it, like 开 始.
        public GardenUiText resultContinueSub, resumeSub, resumeSimpleSub;
        void CreateMainButtonLabels()
        {
            if (resultContinue)
            {
                // 20:51: the green jelly button of the pause dialog (no letter spacing, centred on the button face).
                resultContinue.text = "下一关"; resultContinue.alternateText = "重新挑战";
                CenterButtonLabel(resultContinue, 73f, 32f);
            }
            // Keep the pause/home label centred on the historical capsule face.
            if (resumeLabel)
            {
                resumeLabel.text = "继续游戏"; resumeLabel.alternateText = "完成";
                float rh = ParentHeight(resumeLabel);   // 2026-10-30: the light-theme capsule is 80 high (face centre .46)
                CenterButtonLabel(resumeLabel, rh >= 60f ? rh * .92f : 73f, rh >= 60f ? Mathf.Min(32f, rh * .48f) : 32f);
                resumeLabel.outlineWidth = 0f;   // 2026-10-30: clean words like the reference
            }
            // GardenUiText.middle is runtime-only; centre Retry after loading
            // the prefab just as we do Resume/Done above.
            foreach (var action in pauseActions)
                if (action.actionId == 23 && action.name == "Retry")
                {
                    var retry = action.GetComponentInChildren<GardenUiText>(true);
                    float h = retry ? ParentHeight(retry) : 0f;   // 2026-10-30: a 62 px white pill, centred
                    if (retry) CenterButtonLabel(retry, h < 70f ? h : 73f, h < 70f ? retry.fontSize : 32f);   // keep the prefab's fitted size
                }
            foreach (var text in pausedPanel.GetComponentsInChildren<GardenUiText>(true))
                if (text.text == "继续") { CenterButtonLabel(text, 73f, 32f); break; }
            // 2026-10-31 16:11 (button check): words about half the capsule height, as on the reference (were .38).
            foreach (var action in resultActions)
                if (action.actionId == 40)
                {
                    // 2026-10-29: the light-theme capsules are 86 / 88 high (was 78 with a 73 face).
                    var label = action.GetComponentInChildren<GardenUiText>(true);
                    float h = ((RectTransform)action.transform).sizeDelta.y;
                    if (label) CenterButtonLabel(label, h * .92f, Mathf.Min(32f, h * .48f));   // 2026-10-30: any capsule height
                }
        }
        static float ParentHeight(GardenUiText t) => t && t.transform.parent is RectTransform p ? p.sizeDelta.y : 0f;
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
            // The prefab carries a readable reset text link inside the home settings panel.
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

        // 2026-11-05 (user: the new levels are the game's levels, drop the switch): the hidden 「新版关卡图案」 row is gone;
        // only the settings layout the home mode moves (panel height, Resume and Reset rows) is captured here.
        void CaptureSettingsLayout()
        {
            GardenUiAction resume = null;
            foreach (var action in pauseActions)
                if (action.actionId == 24 && action.name == "Resume") resume = action;
            if (!resume || resumeRect) return;
            settingsPanelArt = pauseDialog.transform.Find("Settings/Content/Panel") as RectTransform;
            resumeRect = (RectTransform)resume.transform;
            if (settingsPanelArt) panelHeight = settingsPanelArt.sizeDelta.y;
            resumeY = resumeRect.anchoredPosition.y;
            resetY = resetProgress ? ((RectTransform)resetProgress.transform).anchoredPosition.y : 0f;
        }

        // Home key art background (Art/R1/home-v1 H2): a full-screen image over the battle background bands,
        // shown only on the home page. The source is 1080x2340 with the 9:16 core picture at y 210-2130; the
        // core stays vertically centred on screen and taller screens reveal the extra sky and cloud sea.
        RawImage homeBackground, codexRings;
        GardenHomeArtwork homeArtwork;
        Texture2D homeBackgroundTexture, codexBackgroundTexture;
        bool homeBackgroundChecked;
        public bool ShowHomeBackground(bool show, bool album = false)
        {
            if (!homeBackgroundChecked)
            {
                homeBackgroundChecked = true;
                var texture = homeBackgroundTexture = Resources.Load<Texture2D>("GardenR1/home-background");
                if (texture)
                {
                    var go = new GameObject("Home background", typeof(RectTransform), typeof(RawImage));
                    var rect = (RectTransform)go.transform;
                    rect.SetParent(background.transform, false);
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                    homeBackground = go.GetComponent<RawImage>();
                    homeBackground.texture = texture; homeBackground.raycastTarget = false;
                    // 2026-10-30 23:26 (user: 图鉴页背景的圆圈, like the partner game): the album's rings, the same crisp
                    // layer as the home's, centred on the selected partner (album centre 270, y 236 of its 960 frame).
                    var rings = Resources.Load<Texture2D>("GardenR1/home-wave-rings");
                    if (rings)
                    {
                        var rg = new GameObject("Album rings", typeof(RectTransform), typeof(RawImage));
                        var rr = (RectTransform)rg.transform; rr.SetParent(rect, false);
                        rr.anchorMin = rr.anchorMax = rr.pivot = new Vector2(0f, 1f);
                        codexRings = rg.GetComponent<RawImage>(); codexRings.texture = rings; codexRings.raycastTarget = false;
                    }
                    var art = new GameObject("Waving bear home artwork", typeof(RectTransform), typeof(GardenHomeArtwork));
                    var ar = (RectTransform)art.transform; ar.SetParent(rect, false);
                    ar.anchorMin = Vector2.zero; ar.anchorMax = Vector2.one; ar.offsetMin = ar.offsetMax = Vector2.zero;
                    homeArtwork = art.GetComponent<GardenHomeArtwork>();
                }
            }
            if (!homeBackground) return false;
            homeBackground.gameObject.SetActive(show);
            if (show)
            {
                if (album && !codexBackgroundTexture) codexBackgroundTexture = Resources.Load<Texture2D>("GardenR1/codex-background");
                homeBackground.texture = album && codexBackgroundTexture ? codexBackgroundTexture : homeBackgroundTexture;
                // Both plates cover wider viewports; the home toys retain their own fitted composition.
                homeBackground.rectTransform.offsetMin = new Vector2(ScreenLayout.Left, 0f);
                homeBackground.rectTransform.offsetMax = new Vector2(-ScreenLayout.Left, 0f);
                if (album && codexBackgroundTexture)
                {
                    // Cover the whole viewport without distorting clouds; content keeps its centred 540×960 frame.
                    float scale = Mathf.Max(ScreenLayout.Width / 540f, ScreenLayout.Height / 960f);
                    float uw = ScreenLayout.Width / (540f * scale), uh = ScreenLayout.Height / (960f * scale);
                    homeBackground.uvRect = new Rect((1f - uw) * .5f, (1f - uh) * .5f, uw, uh);
                }
                else
                {
                    float scale = Mathf.Max(ScreenLayout.Width / 540f, ScreenLayout.Height / 1026f);
                    float uw = ScreenLayout.Width / (540f * scale), uh = ScreenLayout.Height / (1026f * scale);
                    homeBackground.uvRect = new Rect((1f - uw) * .5f, (1f - uh) * .5f, uw, uh);
                }
            }
            if (codexRings)
            {
                codexRings.enabled = show && album;
                if (codexRings.enabled)
                {
                    float top = (ScreenLayout.Height - 960f) * .5f;
                    codexRings.rectTransform.anchoredPosition = new Vector2(270f - 320f - ScreenLayout.Left, -(top + 236f - 320f));
                    codexRings.rectTransform.sizeDelta = new Vector2(640f, 640f);
                }
            }
            if (homeArtwork) homeArtwork.Present(owner, show && !album);
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
        // Home settings reserve space below the toggles for Reset; Done stays outside the panel.
        bool homeMode;
        void RelayoutSettings()
        {
            if (!resumeRect) return;
            const float extraRow = 0f;
            // 2026-10-31 16:33 (user: 设置页的样式跟参照图不一样): on the home settings the 完成 button sits inside the panel,
            // under the divider and 「重置关卡进度」, as in the reference (it hung below the panel, half under its shadow).
            if (settingsPanelArt) settingsPanelArt.sizeDelta = new Vector2(settingsPanelArt.sizeDelta.x, panelHeight + extraRow + (homeMode ? 115f : 0f));
            resumeRect.anchoredPosition = new Vector2(resumeRect.anchoredPosition.x, resumeY - extraRow + (homeMode ? 110f : 0f));
            if (resetProgress)
            {
                var r = (RectTransform)resetProgress.transform;
                r.anchoredPosition = new Vector2(r.anchoredPosition.x, resetY - extraRow);
            }
        }
        public void SetHomeMode(bool home)
        {
            if (home != homeMode) { homeMode = home; RelayoutSettings(); }
            if (pauseTitle) pauseTitle.Present(home);
            if (resumeLabel) resumeLabel.Present(home);
            foreach (var action in pauseActions)
            {
                // 2026-10-29 21:07 (界面优化 9, user: 去掉跳关功能): the rewarded skip is never offered.
                if (action.actionId == SkipLevelAction) action.gameObject.SetActive(false);
                else if (action.actionId == 23 || action.actionId == 25) action.gameObject.SetActive(!home);
            }
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
            float menuLeft = ScreenLayout.HasMenu ? ScreenLayout.MenuLeft : 10000f;
            if (layoutPlush == PlushLayout.Enabled && layoutHeight == ScreenLayout.Height && layoutWidth == ScreenLayout.Width && layoutTop == ScreenLayout.Top && layoutBottom == ScreenLayout.Bottom && layoutMenu == menuLeft) return;
            layoutPlush = PlushLayout.Enabled; layoutHeight = ScreenLayout.Height; layoutWidth = ScreenLayout.Width; layoutTop = ScreenLayout.Top; layoutBottom = ScreenLayout.Bottom; layoutMenu = menuLeft;
            headerBand.anchoredPosition = new Vector2(0, -ScreenLayout.Top);
            FitProgressRow(menuLeft);
            controlsBand.anchoredPosition = new Vector2(0, ScreenLayout.Bottom + 386);
            Band(backgroundTop, 0, PlushLayout.Y(114));
            Band(backgroundField, PlushLayout.Y(114), PlushLayout.Y(574));
            Band(backgroundBottom, PlushLayout.Y(574), ScreenLayout.Height);
        }
        // 2026-11-06 (user: plan A - the header shares the capsule's row): ScreenLayout.Top already puts the header level
        // with the capsule; here the progress bar is shortened so the frame ends 10 px left of it (the percent moves in
        // with the end). Without a capsule the row is as authored.
        void FitProgressRow(float menuLeft)
        {
            if (progressParts == null)
            {
                string[] names = { "Progress frame", "Progress track", "Progress track shade", "Progress percent" };
                progressParts = new RectTransform[names.Length]; progressAuthored = new Vector2[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    progressParts[i] = headerBand ? headerBand.Find(names[i]) as RectTransform : null;
                    if (progressParts[i]) progressAuthored[i] = new Vector2(progressParts[i].anchoredPosition.x, progressParts[i].sizeDelta.x);
                }
            }
            if (!progressParts[0]) return;
            float frameRight = progressAuthored[0].x + progressAuthored[0].y;
            float give = Mathf.Clamp(frameRight - (menuLeft - 10f), 0f, 140f);   // never below ~190 px of frame
            for (int i = 0; i < progressParts.Length; i++)
            {
                RectTransform r = progressParts[i];
                if (!r) continue;
                if (i == 3) r.anchoredPosition = new Vector2(progressAuthored[i].x - give, r.anchoredPosition.y);   // the percent
                else r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, progressAuthored[i].y - give);
            }
            ProgressFillWidthNow = ProgressFillWidth - give;
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

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
        [Header("Layers and responsive containers")]
        public GameObject background, header, controls, pauseDialog, resultDialog;
        public RectTransform backgroundTop, backgroundField, backgroundBottom, headerBand, controlsBand;
        [Header("Header")]
        public GardenUiText lessonTitle, levelTitle, progressText;
        public Image progressFill;
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
                { resumeLabel = action.GetComponentInChildren<GardenUiText>(true); resumeLabel.alternateText = "关闭"; }
            CreateResetProgress();
            CreateMainButtonLabels();
            ApplyLayout();
        }

        // 2026-10-16 22:50: main-action buttons get a big spaced title and a small level line under it, like 开 始.
        public GardenUiText resultContinueSub, resumeSub, resumeSimpleSub;
        void CreateMainButtonLabels()
        {
            if (resultContinue)
            {
                resultContinue.text = "下 一 关"; resultContinue.alternateText = "再 试 一 次";
                resultContinueSub = SplitButtonLabel(resultContinue, 62f, 26f);
            }
            if (resumeLabel)
            {
                resumeLabel.text = "继 续 战 斗"; resumeLabel.alternateText = "关 闭";
                resumeSub = SplitButtonLabel(resumeLabel, 54f, 24f);
            }
            foreach (var text in pausedPanel.GetComponentsInChildren<GardenUiText>(true))
                if (text.text == "继续") { text.text = "继 续"; resumeSimpleSub = SplitButtonLabel(text, 52f, 24f); break; }
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
        public void SetHomeMode(bool home)
        {
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
            return Hit(resultActions, world, out id) || Hit(pauseActions, world, out id)
                || Hit(controlActions, world, out id) || Hit(headerActions, world, out id);
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

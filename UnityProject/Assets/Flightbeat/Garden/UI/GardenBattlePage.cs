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

        PixelWingGame owner;
        GardenUiAction[] headerActions, controlActions, pauseActions, resultActions;
        bool visible;
        GardenUiText pauseTitle, resumeLabel;
        float layoutHeight = -1, layoutWidth = -1, layoutTop = -1, layoutBottom = -1;
        public void Bind(PixelWingGame game)
        {
            owner = game;
            headerActions = header.GetComponentsInChildren<GardenUiAction>(true);
            controlActions = controls.GetComponentsInChildren<GardenUiAction>(true);
            pauseActions = pauseDialog.GetComponentsInChildren<GardenUiAction>(true);
            resultActions = resultDialog.GetComponentsInChildren<GardenUiAction>(true);
            foreach (var text in settingsPanel.GetComponentsInChildren<GardenUiText>(true))
                if (text.text == "暂停一下") { pauseTitle = text; pauseTitle.alternateText = "设置"; }
            foreach (var action in pauseActions)
                if (action.actionId == 24)
                { resumeLabel = action.GetComponentInChildren<GardenUiText>(true); resumeLabel.alternateText = "关闭"; }
            ApplyLayout();
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

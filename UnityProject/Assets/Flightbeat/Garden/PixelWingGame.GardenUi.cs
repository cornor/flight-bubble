#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Flightbeat.Garden.UI;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        internal GardenBattlePage gardenPage;
        PixelSurface gardenOverlay;
        readonly List<UiHit> gardenOverlayHits = new List<UiHit>();
        bool GardenUiActive => !InHome && Battle != null;
        void InitializeGardenUi()
        {
            var prefab = Resources.Load<GardenBattlePage>("GardenR1/UI/GardenBattlePage");
            if (!prefab) throw new InvalidOperationException("Missing GardenR1/UI/GardenBattlePage prefab");
            gardenPage = Instantiate(prefab, hudRect, false);
            gardenPage.name = "Garden Battle UI";
            var overlayObject = new GameObject("Garden legacy overlays", typeof(RectTransform), typeof(Canvas), typeof(CanvasRenderer), typeof(PixelSurface));
            var rect = (RectTransform)overlayObject.transform;
            rect.SetParent(hudRect, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; rect.pivot = Vector2.zero;
            var canvas = overlayObject.GetComponent<Canvas>();
            canvas.overrideSorting = true; canvas.sortingOrder = 40;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            gardenOverlay = overlayObject.GetComponent<PixelSurface>();
            gardenOverlay.Paint = s => { gardenOverlayHits.Clear(); if (GardenUiActive) DrawGardenOverlay(s); };
            gardenPage.Bind(this);
            RefreshGardenUi(gardenPage);
        }
        internal void RefreshGardenUi(GardenBattlePage view)
        {
            view.SetHomeMode(InHome);
            if (InHome)
            {
                view.ShowHome(settings);
                if (view.resumeSub) view.resumeSub.gameObject.SetActive(false);   // at home the button reads 完  成
                view.ShowHomeBackground(HomeArtReady);
                RefreshGardenSettings(view);
                if (gardenOverlay) gardenOverlay.Redraw();
                return;
            }
            view.ShowHomeBackground(false);
            view.Show(GardenUiActive);
            if (gardenOverlay) { gardenOverlay.Lighting = hud.Lighting; gardenOverlay.Redraw(); }
            if (!GardenUiActive) return;
            bool teaching = Battle.Level.lesson > 0;
            // 2026-10-16: one continuous level number like the original header (f54833 "第 NN 关"): the two
            // lessons are 第 1、2 关 and the first formal level is 第 3 关; transitions already used this number.
            view.lessonTitle.gameObject.SetActive(false);
            view.levelTitle.gameObject.SetActive(true);
            view.levelTitle.Present(false, levelIndex + 1);
            view.progressFill.fillAmount = Battle.Progress;
            view.progressText.Present(false, Mathf.FloorToInt(Battle.Progress * 100));
            view.tutorialHint.SetActive(teaching && !evidenceControlled);
            view.propButtons.SetActive(!teaching || evidenceControlled);
            for (int i = 0; i < 3; i++)
            {
                bool cancel = (i == 1 && selection == 1) || (i == 2 && selection == 2);
                // Count goes through the int overload so an unchanged badge allocates nothing.
                int count = i == 2 && Battle.RescueMissiles > 0 ? 1 : Economy.Data.items[i];
                if (cancel) view.propAmounts[i].Present(false, "×");
                else if (count > 0) view.propAmounts[i].Present(false, count);
                else view.propAmounts[i].Present(false, "+");
                view.propCancelLabels[i].SetActive(cancel);
                view.props[i].SetSelected(cancel);
            }
            bool showSettings = Battle.State != Phase.ReviveOffer && settings;
            bool simplePause = Battle.State != Phase.ReviveOffer && !settings && !finaleGuide && paused && selection == 0 && !transitionActive;
            // 2026-10-18 22:55 (user): the 连胜中断 confirm was drawn on top of the still-open pause / settings panel. While
            // it is up the panel is hidden (its buttons stop taking taps too); cancelling it brings the panel back.
            view.pauseDialog.SetActive((showSettings || simplePause) && !streakPrompt);
            view.settingsPanel.SetActive(showSettings); view.pausedPanel.SetActive(simplePause);
            if (view.resumeSub) { view.resumeSub.gameObject.SetActive(true); view.resumeSub.Present(false, levelIndex + 1); }
            if (view.resumeSimpleSub) view.resumeSimpleSub.Present(false, levelIndex + 1);
            RefreshGardenSettings(view);
            bool won = Battle.State == Phase.Won;
            // 2026-10-17 (user decision 11:46): the two teaching levels end on the same victory screen with a
            // 下 一 关 button as every other level (no small "课堂完成" card, no automatic handoff).
            bool result = (Battle.State == Phase.Lost || won) && feedback.EndAge > .48f;
            bool completedLesson = false;
            view.resultDialog.SetActive(result || completedLesson);
            view.missionResult.SetActive(result); view.lessonComplete.SetActive(completedLesson);
            view.resultTitle.Present(!won); view.resultContinue.Present(!won);
            // 下 一 关 shows the level it opens; 再 试 一 次 the level being retried.
            if (view.resultContinueSub) view.resultContinueSub.Present(false, won ? Mathf.Min(levelIndex + 2, Levels.Length) : levelIndex + 1);
            view.resultPortrait.sprite = won ? view.happyPortrait : view.worriedPortrait;
            view.resultLesson.gameObject.SetActive(false);   // resultLevel visibility: SetWinLayout
            view.resultLevel.Present(false, levelIndex + 1);
            view.resultProgress.Present(false, Mathf.FloorToInt(Battle.Progress * 100));
            // 2026-10-17: no coins/fragments in this game for now; a win uses the victory screen (BubbleWin).
            view.rewardGroup.SetActive(false);
            view.SetWinLayout(won && result, !won && result);
            if (!(won && result)) view.campaignComplete.SetActive(false);
        }
        void RefreshGardenSettings(GardenBattlePage view)
        {
            view.musicText.Present(audioChip.MusicMuted); view.soundText.Present(audioChip.EffectsMuted); view.lightText.Present(!effects.Enabled);
            view.toggleIcons[0].sprite = audioChip.MusicMuted ? view.toggleOff : view.toggleOn;
            view.toggleIcons[1].sprite = audioChip.EffectsMuted ? view.toggleOff : view.toggleOn;
            view.toggleIcons[2].sprite = effects.Enabled ? view.toggleOn : view.toggleOff;
            view.PresentResetProgress(ResetProgressLabel);
            view.PresentLevelSet(InHome && LevelSetAvailableCount > 0, LevelSetCurrent == LevelSetB);
        }
        bool TryGardenUiHit(Vector2 logical, out int id)
        {
            id = -1;
            if (!(GardenUiActive || (InHome && settings)) || !gardenPage) return false;
            float y = ScreenLayout.Y(logical.y);
            for (int i = gardenOverlayHits.Count - 1; i >= 0; i--)
            {
                UiHit h = gardenOverlayHits[i];
                if (logical.x >= h.x && logical.x <= h.x + h.w && y >= h.y && y <= h.y + h.h) { id = h.id; return true; }
            }
            Vector3 world = hudRect.TransformPoint(new Vector3(logical.x, ScreenLayout.Height - y));
            return gardenPage.TryHit(world, out id);
        }
    }
}
#endif

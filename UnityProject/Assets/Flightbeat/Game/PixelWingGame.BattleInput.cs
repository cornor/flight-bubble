using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private void TutorialButton() { if (lesson != null) lesson.ShowHelp(); } // f54501

        // f54860: i64 4884153797016223744 at +648 initializes both binary32 components.
        private Vector2 aim = new Vector2(270f, 400f); // +648
        private Vector2 pointerOrigin, lastTapPoint; // +656 / +664
        private bool aimMissile; // +672

        // f54789–f54791, f54714. Distances are measured after the source layout map.
        private static float AimDistance(Vector2 a, Vector2 b)
        {
            float x = OriginalMath.Binary32(a.x - b.x), y = OriginalMath.Binary32(a.y - b.y);
            return OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.Binary32(OriginalMath.Binary32(x * x) + OriginalMath.Binary32(y * y))));
        }
        private static float AimClamp(float value, float min, float max) => value < min ? min : OriginalMath.WasmMin(value, max);
        public void PointerDown(Vector2 p)
        {
            if (AdService.Busy || applicationSuspended) return;
            if (InHome || selection != 2 || HitTest(p) > 0) { Tap(p); return; }
            if (p.y < 114f || p.y > 557f) { Tap(p); return; }
            pointerOrigin = p;
            aimDown = true;
            aimDragged = false;
            if (!(AimDistance(ScreenLayout.Point(p), ScreenLayout.Point(aim)) > 28f)) return;
            lastTargetTap = -10f;
            aimDragged = true;
            aim.y = AimClamp(p.y, 152f, 540f);
            aim.x = AimClamp(p.x, 28f, 512f);
        }
        public void PointerMove(Vector2 p)
        {
            if (selection != 2 || !aimDown) return;
            if (AimDistance(ScreenLayout.Point(p), ScreenLayout.Point(pointerOrigin)) > 6f) aimDragged = true;
            else if (!aimDragged) return;
            lastTargetTap = -10f;
            aim.x = AimClamp(p.x, 28f, 512f);
            aim.y = AimClamp(p.y, 152f, 540f);
        }
        public void PointerUp(Vector2 p)
        {
            if (selection != 2 || !aimDown) return;
            PointerMove(p);
            aimDown = false;
            if (!aimDragged) TargetTap(p);
        }
        private void TargetTap(Vector2 p)
        {
            if (AimDistance(ScreenLayout.Point(p), ScreenLayout.Point(aim)) > 28f)
            {
                lastTargetTap = -10f;
                aim.x = AimClamp(p.x, 28f, 512f);
                aim.y = AimClamp(p.y, 152f, 540f);
                return;
            }
            if (OriginalMath.Binary32(targetClock - lastTargetTap) <= .34f && AimDistance(ScreenLayout.Point(lastTapPoint), ScreenLayout.Point(p)) <= 28f)
            { ConfirmSuperStrike(); lastTargetTap = -10f; return; }
            lastTapPoint = p;
            lastTargetTap = targetClock;
        }

        // f54710, with designation f53970 and bay advert f53971 callbacks.
        public void Tap(Vector2 p)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var audioScope = new Flightbeat.Diagnostics.RuntimeLoopInputs.AudioMethodScope(54710);
#endif
            if (AdService.Busy || applicationSuspended) return;
            if (InHome || transitionActive) { DeckTap(p); return; }
            int id = HitTest(p);
            if (id > 0) { Pressed(id); OnBattleUi(id); return; }
            if (id == 0 || unchecked((uint)((int)Battle.State - 3)) < 3u || settings || (paused && selection == 0)) return;
            if (TutorialTap(p)) return;
            if (selection > 0)
            {
                if (selection == 2) { if (p.y >= 114f && p.y <= 557f) TargetTap(p); return; }
                if (selection == 1 && p.y >= 700f && p.y < 866f && p.x >= 44f && p.x < 496f)
                {
                    int lane = LaneAt(p.x);
                    int row = Math.Max(0, Math.Min((int)(OriginalMath.Binary32((OriginalMath.Binary32(p.y - 700f)) / 50f)), 2));
                    if (Battle.Reserve[lane].Count <= row) return;
                    TakeItem(1, () =>
                    {
                        if (Battle.Designate(lane, row)) ConsumeItem(1);
                        selection = 0;
                        SetPaused(false);
                    });
                    return;
                }
                return;
            }
            if (paused || Battle.State == Phase.Intro) return;
            if (p.y >= 590f && p.y <= 674f)
                for (int bay = Battle.UnlockedSlots; bay < Battle.MaximumSlots; bay++)
                    if (Math.Abs(OriginalMath.Binary32(p.x - Battle.FleetPositionX(bay))) <= 29f && !Battle.BayOpen(bay))
                    {
                        int selectedBay = bay;
                        ShowAd("bay", () => Battle.UnlockBay(selectedBay));
                        return;
                    }
            if (p.y >= 700f && p.y < 754f && p.x >= 20f && p.x < 520f) Battle.Deploy(LaneAt(p.x));
        }

        // f54713 / closures f54871, f54872. Dispatch retains all original IDs.
        private void OnBattleUi(int id)
        {
            audioChip.Play("ui");
            if (OnDevUi(id) || OnEnergyUi(id)) return;
            if (id == 1) { if (selection > 0) CancelSuperAim(); ToggleSettings(); return; }
            if (id >= 10 && id <= 12) { UseProp(id - 10); return; }
            if (id >= 20 && id <= 27)
            {
                switch (id)
                {
                    case 20: audioChip.ToggleMusic(); break;
                    case 21: audioChip.ToggleEffects(); break;
                    case 22:
                        bool before = effects.Enabled;
                        effects.Enabled = !before;
                        hud.Lighting = effects.Enabled;
                        surface.Lighting = effects.Enabled;
                        if (before) effects.Clear();
                        break;
                    case 23: ConfirmStreakBreak("重新开始", () => { settings = false; Restart(); }, false); break;
                    case 24: ToggleSettings(); break;
                    case 25: ConfirmStreakBreak("现在退出", ReturnToDeck, false); break;
                    case 26: tuning = true; MarkUi(60); break;
                }
                return;
            }
            if (id >= 60 && id <= 99) { TuningTap(id - 60); return; }
            switch (id)
            {
                case 8: StartFinale(); break;
                case 9: ConfirmSuperStrike(); break;
                case 30: ShowAd("revive", Rescue); break;
                case 31: ConfirmStreakBreak("放弃复活", () => Battle.DeclineRevive(), true); break;
                case 40: if (Battle.State == Phase.Won) NextLevel(); else Restart(); break;
                case 41: ReturnToDeck(); break;
                case 50: lesson?.ShowHelp(); break;
            }
        }
    }
}

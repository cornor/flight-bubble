using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        public bool AimingMissile => selection == 2 && aimMissile; // f54494

        // f54850 / f54851. Failure reports and ends the iterator; it does not quit.
        private void Marker(string marker, string fields) => Debug.Log("[GameHarness] " + marker + " scenario=pixelwing_merge_shields " + fields);
        private void RunnerFail(string reason) => Marker("HARNESS_TEST_RESULT", "protocol=1 status=failed revision=8 reason=" + reason);
        private void ScreenTap(Vector2 point) => Tap(ScreenLogical(point)); // f54853
        // f54857. Preserve y-then-x evaluation, using the original Wasm min.
        private Vector2 ClampAim(Vector2 point)
        {
            point.y = AimClamp(point.y, 152f, 540f);
            point.x = AimClamp(point.x, 28f, 512f);
            return point;
        }

        // f54852 / f53983. Real-time metrics, with the original indexing and
        // integer multiplication before conversion to double.
        private IEnumerator SampleFrames()
        {
            var frames = new List<double>(1024);
            double start = Time.realtimeSinceStartupAsDouble, last = start;
            long memory = GC.GetTotalMemory(false);
            while (Time.realtimeSinceStartupAsDouble - start < 10.0)
            {
                yield return null;
                double now = Time.realtimeSinceStartupAsDouble;
                frames.Add((now - last) * 1000.0);
                last = now;
            }
            double window = (Time.realtimeSinceStartupAsDouble - start) * 1000.0;
            frames.Sort();
            Debug.Log(String.Format(CultureInfo.InvariantCulture,
                "[GameHarness] PERFORMANCE_SAMPLE scenario=startup_idle_10s run=1 window_ms={0:F1} fps_avg={1:F2} frame_p50_ms={2:F2} frame_p95_ms={3:F2} frame_p99_ms={4:F2} gc_growth_bytes={5}",
                window, (double)unchecked(frames.Count * 1000) / window,
                frames[frames.Count / 2], frames[(int)(frames.Count * .95)], frames[(int)(frames.Count * .99)],
                unchecked(GC.GetTotalMemory(false) - memory)));
        }

        // f54849 / f53985. The shipped --harness-test scenario uses gameplay
        // commands plus explicitly scripted Breach calls; this is not a natural
        // loss replay. Keep every yield, including the blank frames at the deck.
        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            while (!InHome && Battle.State == Phase.Intro) yield return null;
            double ms = Time.realtimeSinceStartupAsDouble * 1000.0;
            Debug.Log(String.Format(CultureInfo.InvariantCulture,
                "[GameHarness] FIRST_INTERACTIVE total_ms={0:F1} unity_ms={1:F1}", ms, ms));
            Debug.Log("[GameHarness] GAME_READY project=wechat_pixelwing level=1 screen=" + (InHome ? "command-deck" : "battle"));
            if (WechatBridge.Scenario == 0) { StartCoroutine(SampleFrames()); yield break; }
            evidenceControlled = true;
            SkipTutorial();
            Marker("HARNESS_TEST_DISPATCH", "protocol=1 transport=package_runtime_entry");
            if (WechatBridge.Scenario != 2101) { RunnerFail("unsupported_scenario"); yield break; }
            Marker("HARNESS_TEST_START", "protocol=1 driver=internal_gameplay_command revision=8");
            float readyOffset = Battle.Offset;
            yield return new WaitForSecondsRealtime(.3f);
            bool frozenReady = Battle.State == Phase.Ready && Battle.Time == 0f && Battle.Offset == readyOffset;
            Marker("HARNESS_TEST_READY", "frozen=" + (frozenReady ? 1 : 0));
            if (!frozenReady) { RunnerFail("first_deployment_gate"); yield break; }
            int actions = Math.Min(Battle.LaneCount, 4);
            for (int step = 1; step <= actions; step++)
            {
                int lane = step - 1, before = Battle.Reserve[lane].Count;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(gameCamera,
                    canvasRect.TransformPoint(new Vector3(Battle.QueuePositionX(lane), ScreenLayout.Height - ScreenLayout.Y(725f), 0f)));
                ScreenTap(screen);
                bool accepted = Battle.Reserve[lane].Count == before - 1;
                if (step <= 2)
                {
                    Marker("HARNESS_TEST_ACTION", "step=" + step + " command=deploy accepted=" + (accepted ? 1 : 0));
                    Marker("MOVE_RESULT", "step=" + step + " fleet=" + Battle.Fleet.Count);
                }
                if (!accepted) { RunnerFail("deploy_rejected"); yield break; }
                yield return new WaitForSecondsRealtime(.07f);
            }
            bool exact = Battle.Level.sourceLevelId == 101 && Battle.ColorCount == 18 && Battle.LaneCount == 4 && Battle.Total == 3600;
            Marker("HARNESS_TEST_SOURCE", "exact=" + (exact ? 1 : 0) + " colors=" + Battle.ColorCount + " lanes=" + Battle.LaneCount + " total=" + Battle.Total);
            if (!exact) { RunnerFail("source_queue_or_palette"); yield break; }
            bool bays = Battle.UnlockedSlots == 7;
            float motionBefore = clock;
            yield return new WaitForSecondsRealtime(.3f);
            bool animated = clock > motionBefore;
            SetPaused(true);
            float frozenClock = clock;
            yield return new WaitForSecondsRealtime(.15f);
            bool pausedClock = frozenClock == clock;
            SetPaused(false);
            Marker("HARNESS_TEST_MOTION", "bays=" + Battle.UnlockedSlots + " animated=" + (animated ? 1 : 0) + " paused=" + (pausedClock ? 1 : 0));
            if (!bays || !animated || !pausedClock) { RunnerFail("seven_bays_or_motion_lifecycle"); yield break; }
            yield return new WaitForSecondsRealtime(1f);
            bool acceptedBreach = Battle.Breach(), debounce = !Battle.Breach();
            bool offer = acceptedBreach && Battle.State == Phase.ReviveOffer;
            // f53985 loads CombatFeedback +192 (RescueAge), not +100 (EndAge).
            while (feedback.RescueAge < .4f && Battle.State == Phase.ReviveOffer) yield return null;
            yield return null;
            int beforeSlots = Battle.UnlockedSlots;
            TapUi(30);
            bool rescued = offer && Battle.State == Phase.Playing && Battle.UnlockedSlots == beforeSlots + 1 && Battle.RescueMissiles == 1 && Battle.LastPurge > 0;
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!AimingMissile && Time.realtimeSinceStartup < deadline) yield return null;
            if (!rescued || !AimingMissile) { RunnerFail("rescue_reward_or_aim"); yield break; }
            Vector2 dest = ClampAim(aim + new Vector2(24f, 15f));
            PointerDown(aim); PointerMove(dest); PointerUp(dest);
            bool dragged = Vector2.Distance(aim, dest) < .1f;
            yield return new WaitForSecondsRealtime(.36f);
            PointerDown(dest); PointerUp(dest);
            yield return new WaitForSecondsRealtime(.12f);
            PointerDown(dest); PointerUp(dest);
            bool launched = Battle.PendingStrike != null && Battle.PendingStrike.Missile && Battle.RescueMissiles == 0;
            deadline = Time.realtimeSinceStartup + 5f;
            while (Battle.PendingStrike != null && Time.realtimeSinceStartup < deadline) yield return null;
            bool crater = Battle.SuperStrikeCount == 1 && Battle.SuperStrikeKills > 0;
            Marker("HARNESS_TEST_RESCUE", "offer=" + (offer ? 1 : 0) + " purge=" + Battle.LastPurge + " bays=" + Battle.UnlockedSlots
                + " drag=" + (dragged ? 1 : 0) + " missile=" + (launched ? 1 : 0) + " crater=" + (crater ? 1 : 0));
            if (!dragged || !launched || !crater) { RunnerFail("missile_interaction_or_impact"); yield break; }
            while (Battle.Invulnerable > 0f || Battle.Repelling) yield return null;
            Battle.Breach();
            bool secondLost = Battle.State == Phase.Lost;
            Marker("HARNESS_TEST_BREACH", "debounce=" + (debounce ? 1 : 0) + " second_breach_lost=" + (secondLost ? 1 : 0));
            bool passed = debounce && secondLost && Levels.Length == WarZones.SectorCount + 2;
            ReturnToDeck();
            deadline = Time.realtimeSinceStartup + 5f;
            while ((!InHome || transitionActive) && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null; yield return null;
            TapUi(120);
            yield return null; yield return null;
            TapUi(310);
            yield return null; yield return null;
            Economy.GiftStarterParts(); TapUi(320);
            yield return null; yield return null;
            TapUi(180);
            yield return null;
            bool upgraded = Economy.Data.parts[0] == 2 && Economy.Spare(0, 0) == 0 && Economy.Snapshot().pierce == 1;
            FleetCore[] modules = Economy.Draw(1);
            bool equipped = modules != null && Economy.Equip(Economy.Data.activeFighter, modules[0].id)
                && Economy.InPreset(Economy.Data.activeFighter, modules[0].Slot) != null;
            bool saved = FleetSaveStore.Decode(FleetSaveStore.Encode(Economy.Data)) != null;
            passed = passed & (saved & (equipped & upgraded));
            Marker("HARNESS_TEST_META", "upgrade=" + (upgraded ? 1 : 0) + " equipped=" + (equipped ? 1 : 0) + " save=" + (saved ? 1 : 0));
            Marker("HARNESS_TEST_RESULT", "protocol=1 status=" + (passed ? "passed" : "failed") + " revision=8 actions=" + actions + " levels=" + Levels.Length);
        }
    }
}

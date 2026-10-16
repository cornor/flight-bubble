using System;
using System.IO;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private int screenWidth, screenHeight; // +436/+440
        internal bool applicationSuspended, broadcastFinished; // +675/+364
        internal float broadcastReportAt, broadcastHumanUntil; // +332/+368
        internal int broadcastWins, broadcastLoops, broadcastRetries, broadcastActions, broadcastHumanActions;

        // src: Update f54783. Battle input, feedback, fixed steps, settlement and
        // redraw retain their original order. Unported callees fail explicitly.
        private void Update()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var audioScope = new Flightbeat.Diagnostics.RuntimeLoopInputs.AudioMethodScope(54783);
            Flightbeat.Diagnostics.RuntimeLoopInputs.Method?.Invoke(54783);
#endif
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54783);
#endif
            if (screenWidth != FrameScreenWidth() || screenHeight != FrameScreenHeight())
            {
                screenWidth = FrameScreenWidth();
                screenHeight = FrameScreenHeight();
                gameCamera.rect = new Rect(0f, 0f, 1f, 1f);
                ApplyDisplay(evidenceViewport.x <= 0 ? FrameScreenWidth() : evidenceViewport.x,
                    evidenceViewport.y <= 0 ? FrameScreenHeight() : evidenceViewport.y);
            }
            AdService.Tick();
            if (AdService.Busy || applicationSuspended) { surface.Redraw(); hud.Redraw(); return; }
            float dt = FrameDelta();
            dt = dt < .05f ? dt : .05f;
            ui  =             OriginalMath.Binary32(ui+(dt));
            TickTransition(dt);
            if (FrameAnyKeyDown()) { broadcastOwnAim = false; broadcastHumanActions = unchecked(broadcastHumanActions + 1); }
            if (FrameAnyKeyDown() || FrameMouseButton(0)) broadcastHumanUntil = OriginalMath.Binary32(ui + 1.2f);
            TickBroadcast(dt);
            if (InHome)
            {
                if (!applicationSuspended) UpdateDeck(dt);
                if (!evidenceControlled && !automatedLessonTest && FrameMouseDown(0))
                    DeckTap(ScreenLogical(FrameMousePosition()));
                surface.Redraw();
                hud.Redraw();
                return;
            }
            UpdateBattleFrame(dt);
        }

        // Battle branch of f54783, extracted only for readability.
        private void UpdateBattleFrame(float dt)
        {
            if (!applicationSuspended)
            {
                targetClock = OriginalMath.Binary32(dt + targetClock);
                if (rescueAimAt >= 0f && !paused && Battle.Time >= rescueAimAt && Battle.State == Phase.Playing)
                { rescueAimAt = -1f; BeginSuperAim(); }
            }
            UpdateFinaleGuide(dt);
            if (!paused)
            {
                clock = OriginalMath.Binary32(dt + clock);
                feedback.Tick(dt);
                TickFinaleVisuals(dt);
                float target = OriginalMath.Binary32((float)Battle.Score), delta = OriginalMath.Binary32(target - scoreDisplay);
                float step = OriginalMath.Binary32(dt * OriginalMath.WasmMax(OriginalMath.Binary32(delta * 10f), 80f));
                scoreDisplay = Math.Abs(delta) <= step ? target : OriginalMath.Binary32(scoreDisplay + (delta >= 0f ? step : -step));
            }
            else if (unchecked((uint)((int)Battle.State - 3)) <= 2u) feedback.Tick(dt);

            if (!evidenceControlled && !automatedLessonTest && !transitionActive)
            {
                if (Battle.State != Phase.ReviveOffer && FrameKeyDown(KeyCode.R)) Restart();
                if (Battle.State != Phase.ReviveOffer && FrameKeyDown(KeyCode.N)) NextLevel();
                if (FrameKeyDown(KeyCode.M)) audioChip.Toggle();
                if (Battle.State != Phase.ReviveOffer && (FrameKeyDown(KeyCode.Space) || FrameKeyDown(KeyCode.Escape)))
                {
                    if (selection > 0) CancelSuperAim();
                    else ToggleSettings();
                }
                if (Battle.LaneCount > 0)
                {
                    int lane = 0;
                    do
                    {
                        if (FrameKeyDown((KeyCode)(49 + lane)) && !paused)
                        {
                            if (tutorialStep >= 0) Tap(new Vector2(Battle.QueuePositionX(lane), 725f));
                            else Battle.Deploy(lane);
                        }
                        lane++;
                    } while (lane < Math.Min(Battle.LaneCount, 9));
                }
                if (FrameMouseDown(0)) PointerDown(ScreenLogical(FrameMousePosition()));
                if (FrameMouseButton(0)) PointerMove(ScreenLogical(FrameMousePosition()));
                if (FrameMouseUp(0)) PointerUp(ScreenLogical(FrameMousePosition()));
            }
            if (!paused)
            {
                float before = finaleHitStop;
                finaleHitStop = OriginalMath.Binary32(before - dt);
                autoClock = OriginalMath.Binary32(autoClock + (before > 0f ? OriginalMath.Binary32(dt * .2f) : dt));
                while (autoClock >= .02f)
                {
                    Battle.Tick(.02f);
                    if (autoplay)
                    {
                        if (Battle.State == Phase.ReviveOffer) Battle.DeclineRevive();
                        else
                        {
                            int lane = Battle.SuggestedQueue();
                            if (lane >= 0) Battle.Deploy(lane);
                        }
                    }
                    autoClock = OriginalMath.Binary32(autoClock + -.02f);
                }
            }
            if (Battle.State == Phase.Won && LastReward == null && sortieId.Length > 0)
                LastReward = Economy.Win(sortieId, sortieLoadout, Battle.Revived, Battle.Time);
            SettleOutcome();
            TickRangeAdvance();
            if (!paused || unchecked((uint)((int)Battle.State - 3)) <= 2u) UpdateComms(dt);
            if ((Battle.State == Phase.Won || Battle.State == Phase.Lost) && !statsRecorded && !evidenceControlled)
            {
                statsRecorded = true;
                Economy.RecordBattle(Battle.Merges, unchecked(Battle.Total - Battle.Remaining), Battle.MaxFusions, Battle.BestCombo);
            }
            float danger = 0f;
            if (Battle.State == Phase.Playing && tutorialStep < 0 && Battle.ActiveFinale == null)
            {
                float value = Battle.Danger;
                float minimum = Battle.Stalled ? .35f : 0f;
                danger = value > minimum ? value : minimum;
            }
            audioChip.SetDanger(danger);
            if (idleTest && Battle.State == Phase.Ready && clock > 1f) Battle.Deploy(0);
            UpdateShownSand(dt);
            if (!capturedLaunch && Battle.Time > 4f && !String.IsNullOrEmpty(capturePath))
            {
                capturedLaunch = true;
                StartCoroutine(Capture("02-combat", 0f));
            }
            if (!capturedEnd && (Battle.State == Phase.Won || Battle.State == Phase.Lost) && !String.IsNullOrEmpty(capturePath))
            {
                capturedEnd = true;
                StartCoroutine(FinishEvidence());
            }
            if (clock > 480f && !capturedEnd && !String.IsNullOrEmpty(capturePath))
            {
                capturedEnd = true;
                StartCoroutine(FinishEvidence());
            }
            surface.Redraw();
            hud.Redraw();
        }

        // src: OnApplicationPause f54779 / OnDisable f54780.
        private void OnApplicationPause(bool value)
        {
            applicationSuspended = value;
            if (!value) return;
            lastTargetTap = -10f;
            aimDown = false;
            if (broadcastAutomatic || AdService.Busy || effects == null || InHome || Battle.State != Phase.Playing || settings) return;
            settings = true;
            SetPaused(true);
        }
        private void OnDisable()
        {
            AdService.Cancel();
            if (effects == null) return;
            SetPaused(true);
            effects.Clear();
        }
        // src: ReportBroadcast f54571. Platform delivery is isolated in the host;
        // status fields, JSON generation and optional file-write order are source.
        private void ReportBroadcast() => DevTools?.ReportBroadcast();
    }
}

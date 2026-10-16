using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54815. Original layer/hit order; revive, settings/tuning, finale guide
        // and paused veil are mutually exclusive in WAT block @3 (lines 487..545).
        private void DrawHUD(PixelSurface s)
        {
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            if (!InHome) { DrawGardenHUD(s); return; }
#endif
            BeginHitsCore();
            s.MapLayout = true;
            if (InHome)
            {
                DrawCommandDeck(s);
                DrawTransition(s);
                DrawToast(s);
                DrawFloats(s);
                EndHitsCore();
                return;
            }
            Phase entryPhase = Battle.State;
            DrawHeader(s);
            feedback.HideCombo = selection == 2 || settings || lesson != null;
            feedback.DrawCombo(s);
            DrawHazard(s);
            DrawBays(s);
            DrawLanes(s, entryPhase == Phase.Intro);
            DrawCommandButtons(s);
            DrawFinaleButton(s);
            if (selection == 1)
            {
                s.TextF("点选前三排任意战机", 271f, 863f, 1.1f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiCyan.a * .7f)), 0);
                s.TextF("点选前三排任意战机", 270f, 862f, 1.1f, uiCyan, 0);
            }
            else if (selection == 0 && Battle.NoticeTime > 0f && Battle.Notice.Length > 0 && tutorialStep < 0)
                Label(s, Battle.Notice, 270f, 862f, 1.1f, uiDim);
            if (entryPhase == Phase.Intro) FleetOpening.Draw(s, Battle);
            DrawCockpit(s);
            if (Battle.State != Phase.ReviveOffer)
            {
                feedback.DrawTargetsAndShots(s);
                feedback.DrawTransitions(s);
            }
            feedback.DrawAccents(s);
            feedback.DrawDanger(s);
            DrawFinale(s);
            DrawWarning(s);
            DrawCommandAlert(s);
            if (selection == 1)
            {
                s.Rect(38f, 517f, 464f, 35f, SourceColor(4381101718365011968UL, 4569892616122128138UL));
                s.TextF("请选择前三排任意战机", 271.0799865722656f, 527.0800170898438f, 1.2f,
                    new Color(0f, 0f, 0f, OriginalMath.Binary32(uiCyan.a * .7f)), 0);
                s.TextF("请选择前三排任意战机", 270f, 526f, 1.2f, uiCyan, 0);
            }
            DrawSuperTargeting(s);
            if (Battle.State == Phase.Ready && tutorialStep < 0) DrawReadyHint(s);
            if (Battle.AdvanceHeld > 0f && selection == 0) DrawHoldBadge(s);
            DrawLegendaryFeedback(s);
            DrawTutorial(s);
            DrawCombatHints(s);
            if (Battle.State == Phase.ReviveOffer)
                DrawRevive(s);
            else if (settings)
            {
                if (tuning) DrawTuning(s);
                else DrawSettings(s);
            }
            else if (finaleGuide) DrawFinaleGuide(s);
            else if (paused && selection == 0 && !transitionActive) DrawPausedVeil(s);
            DrawRange(s);
            if ((Battle.State == Phase.Lost || (Battle.State == Phase.Won && Battle.Level.lesson <= 0)) && feedback.EndAge > .48f)
                DrawMissionResult(s);
            DrawStreakPrompt(s);
            DrawEnergySheet(s);
            DrawDevPanel(s);
            DrawTransition(s);
            DrawToast(s);
            DrawFloats(s);
            EndHitsCore();
        }

    }
}

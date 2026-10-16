using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private float finaleHitStop; // +400

        // f54727: dismiss the teaching pause before applying launch guards.
        private bool StartFinale()
        {
            if (finaleGuide)
            {
                finaleGuide = false;
                Economy.CompleteFinaleTutorial();
                dialogue = "";
                if (paused) SetPaused(false);
            }
            if (selection > 0 || paused || transitionActive) return false;
            int mode = Battle.Level.lesson <= 0 ? Finale.ModeFor(feedback.Model, feedback.Stage) : 0;
            if (!Battle.LaunchFinale(mode)) { audioChip.Play("deny"); return false; }
            if (!evidenceControlled) Economy.CompleteFinaleTutorial();
            return true;
        }

        // f54731: the mask is in mapped screen coordinates while the button,
        // corners, dialogue and tap pointer retain the original logical layout.
        private void DrawFinaleGuide(PixelSurface s)
        {
            if (!FinaleButtonVisible) return;
            float alpha = Clamp01(OriginalMath.Binary32(finaleGuideAge * 4f)), pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 6f)) * .5f) + .5f);
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float y = ScreenLayout.Y(710f), bottom = ScreenLayout.Y(808f);
            Color mask = SourceColor(4306738281929970287UL, 1017370378u, OriginalMath.Binary32(alpha * .72f));
            s.Rect(ScreenLayout.Left, 0f, ScreenLayout.Width, y, mask);
            s.Rect(ScreenLayout.Left, bottom, ScreenLayout.Width, OriginalMath.Binary32(ScreenLayout.Height - bottom), mask);
            s.Rect(ScreenLayout.Left, y, OriginalMath.Binary32(94f - ScreenLayout.Left), OriginalMath.Binary32(bottom - y), mask);
            s.Rect(446f, y, OriginalMath.Binary32(ScreenLayout.Right + -446f), OriginalMath.Binary32(bottom - y), mask);
            s.MapLayout = mapped;
            HitBlock(s);
            HitBox(s, 8, 100f, 716f, 340f, 86f);
            HitBox(s, 1, 12f, 2f, 50f, 50f);
            s.Glow(270f, 759f, 460f, 176f, SourceColor(4561245704525185024UL, 1051931443u, OriginalMath.Binary32(alpha * (OriginalMath.Binary32(OriginalMath.Binary32(pulse * .15f) + .2f)))), 0f);
            s.Corners(92f, 708f, 356f, 102f, A(Mix(uiGold, Color.white, OriginalMath.Binary32(pulse * .5f)), alpha), 16, 3);
            Title(s, "终 局 决 战", 270f, 500f, 2.4f, A(uiGoldHi, alpha), A(uiInk, alpha));
            Label(s, "全部战机已出击 · 敌人不足 " + Flightbeat.Model.Battle.FinaleThreshold.ToString(), 270f, 544f, 1.15f, A(uiText, alpha));
            DrawDialogue(s, 50f, 592f, 440f, false);
            DrawTapPointer(s, new Vector2(270f, 759f), new Vector2(292f, 730f), -150f, ui);
        }
        // f54723. Preserve getter evaluation before the selection/settings tests.
        public bool FinaleButtonVisible => Battle != null && Battle.FinaleReady && selection == 0 && !settings && tutorialStep < 0;

        // f54730. Teaching stages return at the original lesson guard.
        private void UpdateFinaleGuide(float dt)
        {
            if (InHome || Battle == null || Battle.Level.lesson > 0) return;
            if (!finaleGuide)
            {
                if (evidenceControlled || Economy.Data.finaleTutorial || !FinaleButtonVisible
                    || paused || finaleReadyAt < 0f || OriginalMath.Binary32(ui - finaleReadyAt) < .5f) return;
                finaleGuideAge = 0f;
                finaleGuide = true;
                dialogue = "全队已出击，决战准备完成！\n点击「决战」，全机合体清场！";
                dialogueAge = 0f;
                glyphAt = .05f;
                spokenCharacters = 0;
                SetPaused(true);
                audioChip.Play("pilot-confirm");
                return;
            }
            if (Battle.State != Phase.Playing || Battle.ActiveFinale != null) { finaleGuide = false; return; }
            if (!paused && !settings) SetPaused(true);
            finaleGuideAge  =             OriginalMath.Binary32(finaleGuideAge+(dt));
            dialogueAge  =             OriginalMath.Binary32(dialogueAge+(dt));
            if (spokenCharacters >= dialogue.Length || !(dialogueAge >= glyphAt)) return;
            char glyph = dialogue[spokenCharacters++];
            glyphAt = OriginalMath.Binary32(dialogueAge + (OriginalCharacters.IsPunctuation(glyph) || OriginalCharacters.IsWhiteSpace(glyph) ? .1f : .04f));
        }
    }
}

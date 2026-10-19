using System;
using Flightbeat.Model;
using UnityEngine;
namespace Flightbeat.Game
{
    public enum TransitionKind { Launch, Home, Retry, Next }
    public partial class PixelWingGame
    {
        private Action energyThen, transitionSwap;
        private float profileCheck, transitionAge;
        private bool transitionSwapped;
        private int transitionLevel;
        private TransitionKind transitionKind;
        private void UpdateDeck(float dt)
        {
            clock = OriginalMath.Binary32(clock + dt);
            profileCheck = OriginalMath.Binary32(profileCheck + dt);
            if (profileCheck > 1f) { profileCheck = 0f; Economy.Refresh(); }
        }
        private void OpenEnergy(Action then)
        {
            energyThen = then;
            energyOpen = true;
            MarkUi(504);
            audioChip.Play("page");
        }
        public void Sortie(int index)
        {
            if (index < 0 || Economy.Data.unlocked <= index) return;
            if (index >= 2 && !evidenceControlled && !Economy.CanSortie)
            {
                OpenEnergy(() => Sortie(index));
                return;
            }
            BeginTransition(TransitionKind.Launch, () => StartSortie(index), index);
        }
        internal void BeginTransition(TransitionKind kind, Action swap, int level)
        {
            if (transitionActive) return;
            transitionActive = true;
            toast = "";
            floats.Clear();
            transitionLevel = level;
            transitionSwapped = false;
            transitionSwap = swap;
            transitionKind = kind;
            transitionAge = 0f;
            audioChip.Play(kind == TransitionKind.Home ? "whoosh" : "warp");
            if (!InHome && Battle != null) SetPaused(true);
        }
        private void TickTransition(float dt)
        {
            if (!transitionActive) return;
            transitionAge  =             OriginalMath.Binary32(transitionAge+(dt));
            if (!transitionSwapped && transitionAge >= .42f)
            {
                transitionSwapped = true;
                try { transitionSwap?.Invoke(); }
                catch (Exception error) { Debug.LogException(error); }
                if (!InHome && Battle != null) SetPaused(true);
            }
            if (!(transitionAge >= (transitionKind == TransitionKind.Home ? .92f : 1.97f))) return;
            transitionActive = false;
            if (!InHome && Battle != null) SetPaused(false);
        }
        private void DeckTap(Vector2 point)
        {
            if (AdService.Busy || applicationSuspended || transitionActive) return;
            int id = HitTest(point);
            if (id <= 0) return;
            Pressed(id);
            if (codexOpen)   // bubble codex is modal
            {
                if (id == CodexCloseAction) CloseCodex();
                else if (id >= CodexCardAction && id < CodexCardAction + 64) SelectCodex(id - CodexCardAction);
                return;
            }
            if (levelPickOpen) { LevelPickTap(id); return; }   // 2026-10-19 level select page is modal
            if (settings)
            {
                if (id >= 20 && id <= 22) OnBattleUi(id);
                else if (id == 24) { settings = false; SetPaused(false); DisarmResetProgress(); audioChip.Play("ui"); }
                else if (id == 802) TapResetProgress();
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
                else if (id == Flightbeat.Garden.UI.GardenBattlePage.LevelSetAction) ToggleLevelSet();
#endif
                return;
            }
            if (id == 800) StartBubbleLevel();
            else if (id == 801) { settings = true; DisarmResetProgress(); audioChip.Play("page"); }
            else if (id == CodexOpenAction) OpenCodex();
            else if (id == LevelPickOpenAction) OpenLevelPick();
            else if (id >= HomeMemberAction && id < HomeMemberAction + 5) PokeHomeMember(id - HomeMemberAction);
        }
    }
}

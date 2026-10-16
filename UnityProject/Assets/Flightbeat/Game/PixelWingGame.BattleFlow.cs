using System;
using Flightbeat.Model;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54860: i64 -1055916032 at +692 packs float -9 and int -1;
        // the independent i32 store at +84 is also -1. Zero would incorrectly
        // activate f54830's handoff flash immediately on a fresh launch.
        private float rangeHandoffAt = -9f; // +692
        private int rangeHandoffTo = -1, zoneRewardFor = -1; // +696 / +84
        internal bool homeSettings; // +132

        // f54776 / callback f54868: read levelIndex when the transition completes.
        private void Restart()
        {
            if (evidenceControlled) { ResetRuntime(); return; }
            if (transitionActive) return;
            if (!Economy.CanSortie) Economy.AddEnergy(1);
            if (levelIndex >= 2 && !Economy.CanSortie) { OpenEnergy(Restart); return; }
            BeginTransition(TransitionKind.Retry, () => StartSortie(levelIndex), levelIndex);
        }

        // f54787. Evidence wraps levels; ordinary play returns home at the end.
        private void NextLevel()
        {
            if (evidenceControlled) { levelIndex = unchecked(levelIndex + 1) % Levels.Length; ResetRuntime(); return; }
            if (Levels.Length <= unchecked(levelIndex + 1)) { ReturnToDeck(); return; }
            if (!Economy.CanSortie) Economy.AddEnergy(1);
            if (unchecked(levelIndex + 1) >= 2 && !evidenceControlled && !Economy.CanSortie)
                OpenEnergy(NextLevel);
            else StartSortie(unchecked(levelIndex + 1));
        }

        // f54788 / f54704.
        private void ToggleSettings()
        {
            selection = 0;
            tuning = false;
            settings = !settings;
            SetPaused(settings);
            if (settings) MarkUi(20);
        }
        private void CancelSuperAim()
        {
            lastTargetTap = -10f;
            aimDown = false;
            selection = 0;
            SetPaused(false);
        }

        // f54792: teaching levels don't settle streaks or broken modules here.
        private void SettleOutcome()
        {
            if (outcomeSettled || Battle == null || evidenceControlled || Battle.Level.lesson > 0) return;
            if (Battle.State == Phase.Won)
            {
                if (LastReward == null) return;
                outcomeSettled = true;
            }
            else if (Battle.State == Phase.Lost) { outcomeSettled = true; Economy.BreakStreak(); }
            else return;
            ShowBroken(Economy.TakeBroken());
        }

        // f54793: preserve the original automatic teaching-level handoff.
        private void TickRangeAdvance()
        {
            if (rangeAdvanced || Battle == null || Battle.Level.lesson <= 0 || Battle.State != Phase.Won
                || evidenceControlled || broadcastAutomatic || transitionActive || feedback.EndAge < 1.9f || LastReward == null) return;
            int next = unchecked(levelIndex + 1);
            if (next >= Levels.Length || next >= Economy.Data.unlocked) return;
            rangeAdvanced = true;
            audioChip.Play("warp");
            if (next >= 2 && !evidenceControlled && !Economy.CanSortie) { ReturnToDeck(); return; }
            if (StartSortie(next)) { rangeHandoffTo = next; rangeHandoffAt = ui; }
        }

        // f54794. Position smoothing uses grid Y before the shared formation offset.
        private void UpdateShownSand(float dt)
        {
            if (shownY == null || shownY.Length != Battle.Alive.Length) return;
            float step = OriginalMath.Binary32(OriginalMath.Binary32(Battle.Pitch * 26f) * dt);
            for (int i = 0; i < shownY.Length; i++)
            {
                if (!Battle.Alive[i] || !Battle.OnBoard(i)) continue;
                float target = Battle.GridY(i);
                if (target < shownY[i] || OriginalMath.Binary32(target - shownY[i]) > OriginalMath.Binary32(Battle.Pitch * 6f)) shownY[i] = target;
                else
                {
                    float delta = OriginalMath.Binary32(target - shownY[i]);
                    shownY[i] = Math.Abs(delta) <= step ? target : OriginalMath.Binary32(shownY[i] + (delta >= 0f ? step : -step));
                }
            }
        }

        // f54798 / f54811.
        public void ReturnToDeck()
        {
            if (!InHome && !transitionActive) BeginTransition(TransitionKind.Home, EnterDeck, -1);
            else EnterDeck();
        }
        private void EnterDeck()
        {
            if (Battle != null && Battle.State == Phase.Won && LastReward == null && sortieId.Length > 0)
                LastReward = Economy.Win(sortieId, sortieLoadout, Battle.Revived, Battle.Time);
            SettleOutcome();
            if (!statsRecorded && Battle != null && !evidenceControlled)
            {
                statsRecorded = true;
                Economy.RecordBattle(Battle.Merges, unchecked(Battle.Total - Battle.Remaining), Battle.MaxFusions, Battle.BestCombo);
            }
            Economy.Abandon();
            streakPrompt = false;
            energyOpen = false;
            InHome = true;
            var broken = Economy.TakeBroken();
            if (broken.Count > 0) { brokenNotice = true; brokenModules = broken; MarkUi(531); }
            if (!evidenceControlled)
                for (int zone = 0; zone < 5; zone++)
                {
                    if (!Economy.ZoneReady(zone)) continue;
                    deckOverlay = DeckOverlay.Zone;
                    zoneRewardFor = zone;
                    MarkUi(702);
                    audioChip.Play("legend");
                    break;
                }
            tuning = false;
            settings = false;
            selection = 0;
            Page = DeckPage.Bridge;
            homeSettings = false;
            deckOverlay = DeckOverlay.None; // Source clears even the zone overlay assigned above.
            effects.Clear();
            SetPaused(false);
            audioChip.ResetMusic();
            audioChip.Begin();
            MarkUi(199);
        }

        // f54582. The immediate path invokes the action, without prompting.
        private void ConfirmStreakBreak(string what, Action action, bool revive)
        {
            if (Battle == null || InHome || Battle.Level.lesson > 0 || evidenceControlled
                || Battle.State == Phase.Won || Battle.State == Phase.Lost || Economy.Streak <= 0)
            { action(); return; }
            streakRevive = revive;
            streakWhat = what;
            streakAction = action;
            streakPrompt = true;
            MarkUi(512);
            audioChip.Play("warning");
        }
    }
}

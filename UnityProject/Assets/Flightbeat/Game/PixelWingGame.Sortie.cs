using System;
using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Feedback;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private Color[] colors; // +532
        internal CombatFeedback feedback; // +540
        private float autoClock, scoreDisplay; // +552 / +556
        internal bool paused, capturedLaunch, capturedMerge, capturedEnd; // +560 / +562..564
        private float[] shownY; // +584
        private float shownOffset; // +588
        internal bool settings; // +596
        private FleetLoadout sortieLoadout = FleetLoadout.Empty; // +624; f54860
        private string sortieId = ""; // +628; f54860
        private bool statsRecorded, outcomeSettled; // +632 / +640
        private float warningLevel; // +644
        internal bool aimDown, aimDragged; // +673 / +674
        internal float targetClock, lastTargetTap = -10f, rescueAimAt = -1f; // +676..684; f54860
        internal bool rangeAdvanced, tuning; // +688 / +724
        internal IBroadcastPilot broadcastPilot; // +320; optional T9.3 tooling
        internal float broadcastTerminalAge, broadcastAimAge; // +324 / +328
        private int broadcastSerial; // +352
        internal bool broadcastOwnAim; // +366
        public int ItemsUsed { get; private set; } // +620
        public CombatFeedback Feedback => feedback; // f54765
        public bool Paused => paused; // f54766

        private readonly List<FinaleShip> finaleShips = new List<FinaleShip>(); // +376
        private Finale finaleShown; // +380
        private float finaleReadyAt = -1f, finaleEndClock = -1f, finaleKick, finaleMilestoneAt = -9f; // f54860
        private int finaleMilestone; // +404
        private readonly List<FinaleBlast> finaleBlasts = new List<FinaleBlast>(1024); // +408
        internal bool finaleGuide; // +412
        private float finaleGuideAge; // +416
        private struct FinaleShip { public Vector2 from; public int color, tier, id; public bool arrived; }
        private struct FinaleBlast { public Vector2 at; public float born; public Color color; public int seed; }

        // f54695. Explicit name avoids Unity's editor-only Reset message during
        // CLI scene authoring. All game call sites still invoke this same body.
        internal void ResetRuntime()
        {
            StopAllCoroutines();
            if (Battle != null) Battle.Dispose();
            LevelData level = JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Levels/" + Levels[levelIndex]).text);
            colors = BuildPalette(level);
            Battle = new Battle(level, null, true);
            Battle.Cue += OnCue;
            Battle.FinaleStarted += OnFinaleStarted;
            Battle.FinaleBeam += OnFinaleBeam;
            effects.Clear();
            audioChip.ResetMusic();
            feedback = new CombatFeedback(Battle, effects, colors);
            feedback.Impact += OnImpact;
            feedback.WeaponCue += cue => audioChip.Play(cue); // f54870
            feedback.Rumble += Vibrate;
            shownY = new float[Battle.Alive.Length];
            for (int i = 0; i < shownY.Length; i++) shownY[i] = Battle.GridY(i);
            shownOffset = Battle.Offset;
            tuning = false;
            settings = false;
            selection = 0;
            paused = false;
            effects.Freeze(false);
            clock = 0f;
            autoClock = 0f;
            scoreDisplay = 0f;
            warningLevel = 0f;
            capturedEnd = false;
            capturedLaunch = false;
            capturedMerge = false;
            ResetSuperAim();
            ResetFinale();
            ResetComms();
            SetupSortie();
            rangeAdvanced = false;
            ResetBroadcastSortie();
            if (!InHome) audioChip.Begin();
        }

        // f54777.
        private static Color[] BuildPalette(LevelData level) => CombatPalette.ForLevel(level);

        // f54696. SpendSortie's result is intentionally ignored in the source.
        public bool StartSortie(int index)
        {
            if (index < 0 || index >= Levels.Length || Economy.Data.unlocked <= index) return false;
            if (!statsRecorded && Battle != null && !evidenceControlled)
            {
                statsRecorded = true;
                Economy.RecordBattle(Battle.Merges, unchecked(Battle.Total - Battle.Remaining), Battle.MaxFusions, Battle.BestCombo);
            }
            if (index >= 2 && !evidenceControlled)
            {
                Economy.SpendSortie();
                Economy.WearModules(Economy.ActiveFighter);
            }
            sortieId = SortieGuid().ToString("N");
            Economy.Begin(unchecked(index + 1), sortieId);
            InHome = false;
            levelIndex = index;
            ResetRuntime();
            return true;
        }

        // f54697. Lessons use the source's empty loadout and standard appearance;
        // ordinary levels must execute the full economy calculation.
        private void SetupSortie()
        {
            outcomeSettled = false;
            statsRecorded = false;
            LastReward = null;
            ItemsUsed = 0;
            brokenModules = new List<string>();
            sortieLoadout = Battle.Level.lesson > 0 ? FleetLoadout.Empty : Economy.Snapshot();
            Battle.ApplyLoadout(sortieLoadout);
            feedback.Look = Battle.Level.lesson > 0 ? FleetLook.Standard : Economy.Look();
            WeaponProfile weapon = Battle.Level.lesson <= 0 && !evidenceControlled
                ? WeaponProfile.For(Economy.ActiveFighter, Economy.PartLevel(Economy.ActiveFighter, 0))
                : new WeaponProfile();
            Battle.ApplyWeapon(weapon);
            feedback.Weapon = weapon;
        }

        // f54703 (inlined at the end of f54695).
        private void ResetBroadcastSortie()
        {
            broadcastSerial = unchecked(broadcastSerial + 1);
            broadcastPilot = DevTools?.CreatePilot(Battle.ColorCount, unchecked(broadcastSerial * 31 + levelIndex * 7919 + 137));
            broadcastOwnAim = false;
            broadcastTerminalAge = 0f;
            broadcastAimAge = 0f;
        }

        // src: ResetSuperAim f54778.
        private void ResetSuperAim()
        {
            rescueAimAt = -1f;
            aimDown = false;
            aimDragged = false;
            targetClock = 0f;
            lastTargetTap = -10f;
        }
        // src: ResetFinale f54725.
        private void ResetFinale()
        {
            finaleShips.Clear();
            finaleBlasts.Clear();
            finaleMilestone = 0;
            finaleEndClock = -1f;
            finaleKick = 0f;
            finaleShown = null;
            finaleReadyAt = -1f;
            finaleGuideAge = 0f;
            finaleGuide = false;
            finaleMilestoneAt = -9f;
        }

        // f54728.
        public void SetPaused(bool value)
        {
            if (value == paused) return;
            paused = value;
            effects.Freeze(value);
            audioChip.SetPaused(value);
        }

        private static readonly string[] ModelSound = { "", "needle", "quake", "blaze", "arc" }; // f54861
        // f54781.
        private string ModelCue(string cue)
        {
            if (feedback == null) return cue;
            int model = feedback.Model;
            if (model <= 0 || model >= ModelSound.Length) return cue;
            if (cue == "shot" || cue == "shot-twin") return "shot-" + ModelSound[model];
            if (cue == "hit" || cue == "hit-twin") return "hit-" + ModelSound[model];
            return cue;
        }
        // f54869.
        private void OnImpact(int tier) => audioChip.Play(tier == 4 ? "hit-rocket" : tier > 1 ? "hit-max" : ModelCue(tier == 1 ? "hit-twin" : "hit"));
        // f54782; merge capture delay is f32 bits 1047233823 in the WAT.
        private void OnCue(string cue)
        {
            audioChip.Play(ModelCue(cue));
            if (cue == "merge" && !capturedMerge && !String.IsNullOrEmpty(capturePath))
            {
                capturedMerge = true;
                StartCoroutine(Capture("03-fusion", .23f));
            }
        }
        // f54729's shipped body is nop, including on native platforms.
        private void Vibrate(float power) { }

        // src: OnDestroy f54803. The original loads +424 and calls f55052,
        // then loads +536 and calls f54387; retain order and separate null guards.
        private void OnDestroy()
        {
            if (Battle != null) Battle.Dispose();
            if (effects != null) effects.Dispose();
        }
    }
}

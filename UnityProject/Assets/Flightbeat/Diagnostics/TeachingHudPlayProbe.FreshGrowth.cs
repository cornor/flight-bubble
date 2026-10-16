#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
using System;
using System.IO;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    // Stage-level extension of the existing real-time teaching driver. Explicit
    // CLI only: no clock/RNG/physics override and no game-state or resource grants.
    public sealed partial class TeachingHudPlayProbe
    {
        [Serializable] private sealed class FreshGrowthResult
        {
            public bool resumed, defaultProfileChecked, upgraded, reloadedExact, giftNotRepeated, formalReady, exitedFormal;
            public string startingProfile, beforeUpgrade, savedProfile, finalProfile;
        }
        [Serializable] private sealed class FreshCarry
        { public string profile; public SavedValue[] values; }
        private static bool freshGrowth;
        private static FreshCarry freshCarry;
        private static int growthCoins, growthFragments, growthEnergy;

        private static void ConfigureFreshGrowth(string[] args)
        {
            freshGrowth = Array.IndexOf(args, "--flightbeat-fresh-growth") >= 0;
            freshCarry = null;
            int at = Array.IndexOf(args, "--flightbeat-play-save-in");
            if (at >= 0 && (!freshGrowth || at + 1 >= args.Length))
                throw new ArgumentException("Save continuation requires --flightbeat-fresh-growth and a file");
            if (!freshGrowth) return;
            foreach (string incompatible in new[] { "--flightbeat-formal-items", "--flightbeat-super-strike", "--flightbeat-mission-result", "--flightbeat-revive-flow", "--onboarding-test", "--autoplay", "--broadcast-wins" })
                if (Array.IndexOf(args, incompatible) >= 0) throw new ArgumentException("Fresh growth cannot combine with " + incompatible);
            if (at < 0) return;
            freshCarry = JsonUtility.FromJson<FreshCarry>(File.ReadAllText(Path.GetFullPath(args[at + 1])));
            if (freshCarry == null || freshCarry.values == null || freshCarry.values.Length != Saves.Length)
                throw new ArgumentException("Complete actual save continuation required");
            FleetProfile selected = FleetSaveStore.Decode(freshCarry.profile);
            if (selected == null || selected.attempt.Length != 0) throw new ArgumentException("Valid settled carry profile required");
            for (int i = 0; i < Saves.Length; i++)
                if (freshCarry.values[i] == null || freshCarry.values[i].key != Saves[i]) throw new ArgumentException("Save key order");
            SavedValue chosen = freshCarry.values[(selected.revision & 1) == 0 ? 0 : 1];
            if (!chosen.present || chosen.value != freshCarry.profile) throw new ArgumentException("Actual selected save missing");
        }

        private static void InstallFreshGrowthSave()
        {
            if (freshCarry == null) return;
            foreach (SavedValue slot in freshCarry.values) if (slot.present) PlayerPrefs.SetString(slot.key, slot.value);
        }

        private static void ObserveDefaultProfile(PixelWingGame game)
        {
            FleetProfile p = game.Economy.Data;
            // f53995 authored defaults; f54308 uses this constructor for no slots.
            RequireGrowth(p.coins == 300 && p.fragments == 20 && p.energy == 5 && p.items.Length == 3
                && p.items[0] == 2 && p.items[1] == 2 && p.items[2] == 1
                && p.unlocked == 1 && p.wins == 0 && p.cleared.Count == 0 && p.parts[0] == 1
                && !p.starterGift && !p.growthTutorial, "Default no-save profile");
            report.freshGrowth.startingProfile = FleetSaveStore.Encode(p);
            report.freshGrowth.defaultProfileChecked = true;
        }

        private static void BeginFreshGrowth(PixelWingGame game, double now)
        {
            FleetProfile p = game.Economy.Data;
            RequireGrowth(report.trainingWon && report.training02Won && p.wins == 2 && p.unlocked == 3
                && p.cleared.Contains(1) && p.cleared.Contains(2) && p.streak == 2
                && p.attempt.Length == 0 && !p.starterGift && !p.growthTutorial, "Two settled teaching wins");
            report.freshGrowth.beforeUpgrade = FleetSaveStore.Encode(p);
            growthCoins = p.coins; growthFragments = p.fragments; growthEnergy = p.energy;
            Click(game, 120); stageAt = now; stage = 90; // f54641 -> f54650.
        }

        private static bool TickFreshGrowth(PixelWingGame game, double now)
        {
            if (stage < 90) return false;
            if (now - stageAt < .3d) return true; // Driver pacing only, not a game constant.
            FleetProfile p = game.Economy.Data;
            switch (stage)
            {
                case 90:
                    // f53986/f54296 grants two noses on the first eligible visit.
                    RequireGrowth(game.Page == DeckPage.Upgrade && p.starterGift && p.spares[0] == 2, "Starter parts from UI visit");
                    Click(game, 180); stage = 91; break;
                case 91:
                    // f54088/f54291 consumes copies; f54146/f54248 marks growth.
                    RequireGrowth(p.parts[0] == 2 && p.spares[0] == 0 && p.growthTutorial
                        && p.coins == growthCoins && p.fragments == growthFragments && p.energy == growthEnergy,
                        "First upgrade preserves currencies and energy");
                    report.freshGrowth.upgraded = true;
                    Click(game, 121); stage = 92; break;
                case 92:
                    RequireGrowth(game.InHome && game.Page == DeckPage.Bridge && p.attempt.Length == 0, "Upgraded home settled");
                    ExportFreshCarry(game);
                    Finish(null); break;
                case 100:
                    if (report.frames < 12) return true;
                    // f54308 chooses the prior actual save; no expected profile
                    // is injected into Economy and no startup fields are masked.
                    RequireGrowth(game.InHome && FleetSaveStore.Encode(p) == freshCarry.profile, "Full saved profile survives ordinary Awake");
                    report.freshGrowth.reloadedExact = true;
                    report.freshGrowth.startingProfile = FleetSaveStore.Encode(p);
                    growthCoins = p.coins; growthFragments = p.fragments; growthEnergy = p.energy;
                    Click(game, 120); stage = 101; break;
                case 101:
                    RequireGrowth(game.Page == DeckPage.Upgrade && p.parts[0] == 2 && p.spares[0] == 0 && p.starterGift
                        && p.growthTutorial && FleetSaveStore.Encode(p) == freshCarry.profile, "Starter gift not repeated after reload");
                    report.freshGrowth.giftNotRepeated = true;
                    Click(game, 121); stage = 102; break;
                case 102:
                    RequireGrowth(game.InHome && game.Page == DeckPage.Bridge, "Bridge returned");
                    Click(game, 110); stage = 103; break;
                case 103:
                    if (game.InHome || game.Battle.State == Phase.Intro || Read<bool>(game, "transitionActive")) return true;
                    RequireGrowth(game.LevelIndex == 2 && game.Battle.State == Phase.Ready && !game.Battle.TrainingSafe
                        && p.parts[0] == 2 && p.attempt.Length > 0 && p.energy == growthEnergy && p.streak == 2,
                        "Upgraded formal sortie with original streak energy rule"); // f54118.
                    report.freshGrowth.formalReady = true;
                    Click(game, 1); stage = 104; break;
                case 104:
                    RequireGrowth(game.Paused, "Formal pause");
                    Click(game, 25); stage = 105; break;
                case 105:
                    RequireGrowth(Read<bool>(game, "streakPrompt"), "Formal exit streak confirmation");
                    Click(game, 510); stage = 106; break;
                case 106:
                    if (!game.InHome || Read<bool>(game, "transitionActive")) return true;
                    RequireGrowth(p.streak == 0 && p.attempt.Length == 0 && p.wins == 2 && p.parts[0] == 2
                        && p.coins == growthCoins && p.fragments == growthFragments && p.energy == growthEnergy,
                        "Confirmed exit keeps progress and clears the attempt");
                    report.freshGrowth.exitedFormal = true;
                    report.returnedHome = true;
                    ExportFreshCarry(game);
                    Finish(null); break;
                default: throw new InvalidOperationException("Unknown fresh growth stage " + stage);
            }
            stageAt = now;
            return true;
        }

        private static void ExportFreshCarry(PixelWingGame game)
        {
            string encoded = FleetSaveStore.Encode(game.Economy.Data);
            string selected = Saves[(game.Economy.Data.revision & 1) == 0 ? 0 : 1]; // f54309.
            RequireGrowth(PlayerPrefs.GetString(selected, "") == encoded, "Export observes the actual selected slot");
            var carry = new FreshCarry { profile = encoded, values = new SavedValue[Saves.Length] };
            for (int i = 0; i < Saves.Length; i++) carry.values[i] = new SavedValue
            { key = Saves[i], present = PlayerPrefs.HasKey(Saves[i]), value = PlayerPrefs.GetString(Saves[i], "") };
            string file = Output + ".carry.json";
            if (File.Exists(file)) throw new IOException("Fresh carry output required");
            File.WriteAllText(file, JsonUtility.ToJson(carry, true));
            report.freshGrowth.savedProfile = encoded;
            report.freshGrowth.finalProfile = encoded;
            report.events.Add("Read actual FleetSaveStore A/B slots before restoring user preferences");
        }
        private static void RequireGrowth(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            report.events.Add("fresh growth: " + message);
        }
    }
}
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public sealed partial class ReviewCapture
    {
        // f54469/f54879: original v10 screenshot tour and explicit rich fixtures.
        private IEnumerator TourV10()
        {
            yield return Home();
            FleetEconomy e = game.Economy;
            FleetProfile d = e.Data;
            d.zoneClaimed = 3; d.streak = 4; d.bestStreak = 6; d.energy = 3;
            d.unlocked = Math.Max(d.unlocked, 72);
            for (int i = 1; i <= 64; i++) if (!d.cleared.Contains(i)) d.cleared.Add(i);
            d.fragments = unchecked(d.fragments + 400); d.coins = unchecked(d.coins + 5000);
            game.TapUi(121); yield return Shot("a01-home-energy-streak", .8f);
            game.TapUi(100); yield return Shot("a00-settings-version", .5f);
            game.TapUi(740); yield return Shot("a00b-test-panel", .5f);
            game.TapUi(741); yield return Frames(3);
            game.TapUi(253); yield return Frames(3);
            Call(game, "OpenEnergy", new object[] { null }); yield return Shot("a02-energy-sheet", .5f);
            game.TapUi(503); yield return Frames(3);
            game.TapUi(554); yield return Shot("a03-codex-zones", .6f);
            game.TapUi(561); yield return Shot("a04-codex-zone2", .6f);
            game.TapUi(604); yield return Shot("a05-codex-level", .5f);
            game.TapUi(553); yield return Frames(2);
            game.TapUi(552); yield return Frames(2);
            game.TapUi(562); yield return Shot("a06-codex-zone3", .6f);
            game.TapUi(551); yield return Frames(3);
            e.Data.zoneClaimed = 1;
            Call(game, "OpenZoneReward", new object[] { 1 }); yield return Shot("a07-zone-reward", .9f);
            game.TapUi(700); yield return Frames(4);
            Call(game, "StartAssembly", new object[] { 2 }); yield return Shot("a08-assembly-parts", .5f);
            yield return Shot("a09-assembly-lock", .9f);
            yield return Shot("a10-assembly-done", 1.6f);
            game.TapUi(720); yield return new WaitForSecondsRealtime(.7f);
            game.TapUi(720); yield return Frames(4);
            e.Draw(10); yield return Frames(2);
            foreach (FleetCore core in e.Data.cores) e.Equip(0, core.id);
            if (e.Data.cores.Count > 0) e.Data.cores[0].durability = 1;
            game.TapUi(120); yield return Frames(3);
            game.TapUi(300); game.TapUi(311); yield return Shot("a11-gear", .6f);
            game.TapUi(390); yield return Shot("a12-gear-zoom", .7f);
            game.TapUi(390); yield return Frames(3);
            e.Draw(10); yield return Frames(2);
            game.TapUi(384); yield return Shot("a12b-recycle", .6f);
            game.TapUi(770); yield return Shot("a12c-recycle-picked", .4f);
            game.TapUi(773); yield return Frames(3);
            game.TapUi(122); yield return Shot("a13-shop-offers", .7f);
            game.TapUi(121); yield return Frames(3);
            game.TapUi(111); yield return Shot("a14-daily", .8f);
            game.TapUi(298); yield return Frames(3);
            Call(game, "ShowBroken", new object[] { new List<string> { "火力模块·精良", "护盾模块·稀有" } });
            yield return Shot("a15-broken", .6f);
            game.TapUi(530); yield return Frames(3);
            yield return Battle(40); yield return Shot("a16-zone2-battle", 1.2f);
            Call(game, "ConfirmStreakBreak", new object[] { "现在退出", (Action)(() => { }), false }); // f1080 callback
            yield return Shot("a17-streak-break", .6f);
            game.TapUi(511); yield return Frames(3);
            Model.Battle b = game.Battle;
            for (int i = 0; i < b.LaneCount; i++)
            {
                game.Tap(new Vector2(b.QueuePositionX(i), 725f));
                yield return new WaitForSecondsRealtime(.2f);
            }
            yield return new WaitForSecondsRealtime(.6f);
            b.StageFinaleForEvidence(330);
            SetField(game, "finaleGuide", false); e.Data.finaleTutorial = true;
            yield return Shot("a18-finale-slam", .12f);
            yield return Shot("a19-finale-banner", .9f);
            yield return Home(); yield return Battle(70); yield return Shot("a20-zone3-battle", 1.2f);
            yield return Home();
        }

        // f54468/f54881: five zone samples, merge hint, then the two short rounds.
        private IEnumerator TourV11()
        {
            FleetProfile d = game.Economy.Data;
            d.unlocked = PixelWingGame.Levels.Length;
            for (int i = 1; i <= 250; i++) if (!d.cleared.Contains(i)) d.cleared.Add(i);
            d.zoneClaimed = 31; d.energy = 99;
            yield return Home();
            game.TapUi(554); yield return Shot("b01-codex-zones", .6f);
            game.TapUi(563); yield return Shot("b02-codex-zone4", .6f);
            game.TapUi(557); yield return Shot("b03-codex-zone4-page2", .4f);
            game.TapUi(552); yield return Frames(2);
            game.TapUi(564); yield return Shot("b04-codex-zone5", .6f);
            game.TapUi(551); yield return Frames(3);
            // Metadata offset 0x36FF18, BA3D76FB...227975, five little-endian i32.
            int[] indices = { 60, 82, 142, 202, 261 };
            string[] names = { "b05-zone2-late", "b06-zone3-first", "b07-zone4-battle", "b08-zone5-battle", "b09-zone5-boss" };
            for (int k = 0; k < indices.Length; k++)
            {
                yield return Home(); yield return Battle(indices[k]); yield return Shot(names[k], 1.4f);
                Debug.Log("REVIEW ZONE level=" + (indices[k] + 1) + " zone=" + WarZones.OfLevel(indices[k])
                    + " total=" + game.Battle.Total + " lanes=" + game.Battle.LaneCount);
            }
            yield return Home(); yield return Battle(40);
            Model.Battle b = game.Battle;
            int hint = -1;
            for (int k = 0; k < 14 && hint < 0; k++)
            {
                for (int lane = 0; lane < b.LaneCount; lane++) if (b.WouldMerge(lane)) hint = lane;
                if (hint >= 0) break;
                int pick = 0;
                for (int lane = 0; lane < b.LaneCount; lane++)
                {
                    if (b.Reserve[lane].Count == 0) continue;
                    int color = b.Reserve[lane][0].Color, count = 0;
                    foreach (Fighter f in b.Fleet) if (f.Color == color) count++;
                    if (count % 3 == 1) { pick = lane; break; }
                }
                if (b.Fleet.Count >= b.UnlockedSlots) break;
                b.Deploy(pick); yield return new WaitForSecondsRealtime(.5f);
            }
            yield return Shot("b10-merge-hint", .15f);
            Debug.Log("REVIEW MERGE hint=" + hint + " fleet=" + b.Fleet.Count);
            yield return Home(); game.StartSortie(0); yield return WaitBattle(0, 12f);
            SetField(game, "tutorialStep", -1); yield return Shot("b11-range-round1", .8f);
            game.Battle.StageFinaleForEvidence(2);
            float w = 0f;
            while (game.Battle.State != Phase.Won && w < 12f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return Shot("b12-range-clear", .7f);
            Debug.Log("REVIEW RANGE finale=" + game.FinaleButtonVisible + " state=" + game.Battle.State);
            yield return WaitBattle(1, 12f);
            SetField(game, "tutorialStep", -1); yield return Shot("b13-range-round2", .3f);
            game.Battle.StageFinaleForEvidence(2);
            w = 0f;
            while (game.Battle.State != Phase.Won && w < 12f) { w += Time.unscaledDeltaTime; yield return null; }
            w = 0f;
            while (game.LevelIndex != 2 && w < 12f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return Shot("b14-formal-defence", .75f);
            yield return WaitBattle(2, 12f); yield return Shot("b15-sector1-ready", .2f);
            Debug.Log("REVIEW RANGE next=" + game.LevelIndex);
            yield return Home();
        }
    }
}

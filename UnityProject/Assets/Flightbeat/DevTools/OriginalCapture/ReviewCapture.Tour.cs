using System;
using System.Collections;
using Flightbeat.Economy;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public sealed partial class ReviewCapture
    {
        // f54470/f54877: original tour, including its permissive timeouts and
        // artificial state changes. Yield ordering follows source states 1..147.
        private IEnumerator Tour()
        {
            game.PrepareOnboardingEvidence();
            game.Economy.Data.growthTutorial = true;
            game.Economy.Data.unlocked = 3;
            game.Economy.Data.cleared.AddRange(new[] { 1, 2 });
            float w;
            if (!game.InHome)
            {
                game.ReturnToDeck(); w = 0f;
                while ((!game.InHome || game.TransitionActive) && w < 6f)
                { w += Time.unscaledDeltaTime; yield return null; }
            }
            yield return Frames(2);
            FleetEconomy e = game.Economy;
            if (Want("home"))
            {
                yield return Frames(3);
                yield return Shot("10-home", .6f);
                e.RecordBattle(40, 12000, 1, 3200); e.ClaimDaily(1, false);
                game.TapUi(111); yield return Shot("11-daily", .9f);
                game.TapUi(298); yield return Frames(2);
                game.TapUi(112); yield return Shot("12-signin", .5f);
                game.TapUi(240); yield return Shot("13-signin-claimed", .5f);
                game.TapUi(298); yield return Frames(2);
                game.TapUi(100); yield return Shot("14-settings", .45f);
                game.TapUi(253); yield return Frames(2);
            }
            if (Want("upgrade"))
            {
                var random = new System.Random(20260928);
                SetProp(game, "Economy", new FleetEconomy(e.Data, FleetSaveStore.Utc, _ => { }, n => random.Next(n))); // f1406/f54474
                e = game.Economy;
                FleetProfile d = e.Data;
                d.fragments = 400;
                d.spares[8] = 4; d.spares[3] = 5; d.spares[4] = 9; d.spares[0] = 2;
                d.parts[5] = 1; d.parts[6] = 1; d.parts[8] = 1;
                d.spares[12] = 3; d.spares[6] = 2;
                d.parts[14] = 1; d.parts[12] = 1; d.parts[13] = 1;
                d.parts[10] = 1; d.parts[11] = 1; d.spares[13] = 1;
                for (int i = 0; i < 3; i++)
                {
                    d.cores.Add(new FleetCore { id = d.nextCore, kind = 0, quality = 1 });
                    d.nextCore = unchecked(d.nextCore + 1);
                }
                d.cores.Add(new FleetCore { id = d.nextCore, kind = 3, quality = 3 });
                d.nextCore = unchecked(d.nextCore + 1);
                d.cores.Add(new FleetCore { id = d.nextCore, kind = 16, quality = 2 });
                d.nextCore = unchecked(d.nextCore + 1);
                game.TapUi(120); yield return Shot("20-hangar-parts", .7f);
                game.TapUi(180); yield return Shot("21-part-upgrade", .3f);
                yield return Shot("21b-part-upgraded", .9f);
                game.TapUi(324); yield return Shot("22-engine-card", .4f);
                game.TapUi(311); yield return Shot("23-hangar-gear", .5f);
                game.TapUi(340); yield return Shot("24-module-picker", .5f);
                game.TapUi(370); yield return Shot("24b-module-merge", .4f);
                game.TapUi(350); yield return Shot("25-module-equipped", .6f);
                game.TapUi(343); yield return Frames(3);
                game.TapUi(350); yield return Frames(20);
                game.TapUi(312); yield return Shot("26-hangar-bonus", .5f);
                game.TapUi(301); game.TapUi(310); yield return Shot("27-falcon-collect", .6f);
                game.TapUi(322); yield return Frames(3);
                game.TapUi(330); yield return Shot("28-smelt", .5f);
                game.TapUi(472); yield return Shot("29-smelt-done", .5f);
                game.TapUi(302); yield return Shot("30-titan-ready", .5f);
                game.TapUi(331); yield return Shot("31-assemble", .4f);
                yield return Shot("31b-assembled", 1.2f);
                game.TapUi(332); yield return Shot("32-set-active", .5f);
                game.TapUi(333); yield return Shot("33-supply-parts", .5f);
                game.TapUi(413); yield return Frames(3);
                game.TapUi(421); yield return Shot("34-supply-pod", .8f);
                yield return Shot("35-supply-ten", 1.6f);
                yield return Shot("36-supply-ten-done", 1.4f);
                game.TapUi(200); yield return Frames(3);
                game.TapUi(311); yield return Frames(3);
                game.TapUi(334); yield return Frames(3);
                game.TapUi(401); yield return Shot("37-supply-gear", .5f);
                game.TapUi(420); yield return Shot("37b-gear-pod", .8f);
                yield return Shot("37c-gear-card", 1.4f);
                game.TapUi(200); yield return Frames(3);
                game.TapUi(121); yield return Shot("38a-home-fighter", .8f);
                game.TapUi(481); yield return Shot("38b-home-switch", .5f);
                game.TapUi(480); yield return Frames(20);
            }
            if (Want("levels"))
            {
                game.TapUi(120); yield return Frames(3);
                game.TapUi(300); game.TapUi(310); yield return Frames(3);
                for (int level = 1; level <= 5; level++)
                {
                    FleetProfile d = game.Economy.Data; d.activeFighter = 0;
                    d.parts[0] = level; d.parts[1] = level; d.parts[2] = level; d.parts[3] = level; d.parts[4] = level;
                    game.TapUi(320); yield return Shot("9" + level + "-cruiser-parts-lv" + level, .9f);
                }
                game.TapUi(121); yield return Frames(10);
            }
            if (Want("shop"))
            {
                game.TapUi(122); yield return Shot("30z-shop", .6f);
                game.TapUi(210); yield return Shot("31z-shop-buy", .15f);
            }
            if (Want("battle"))
            {
                game.TapUi(121); yield return Frames(3);
                yield return Shot("38-home-liaison", 1.6f);
                game.TapUi(110); yield return Shot("39-launch-ignite", .2f);
                yield return Shot("39b-launch-depart", .28f);
                w = 0f;
                while (!game.TransitionActive && w < 2f) { w += Time.unscaledDeltaTime; yield return null; }
                yield return Shot("40-transition", .35f);
                yield return Shot("41-title-card", .5f);
                w = 0f;
                while (game.TransitionActive && w < 4f) { w += Time.unscaledDeltaTime; yield return null; }
                yield return Shot("42-intro-rain", .45f);
                yield return Shot("43-intro-warning", 1.2f);
                yield return Shot("44-intro-ready", 1.2f);
                w = 0f;
                while (game.Battle.State == Phase.Intro && w < 8f) { w += Time.unscaledDeltaTime; yield return null; }
                yield return Shot("45-tutorial-deploy", 1.4f);
                yield return Shot("45b-tutorial-pointer", .45f);
                yield return Frames(10);
                float worst = 0f;
                game.Tap(new Vector2(game.Battle.QueuePositionX(0), 725f));
                for (int i = 0; i < 30; i++)
                {
                    yield return null;
                    float dt = Time.unscaledDeltaTime; worst = worst > dt ? worst : dt;
                }
                Debug.Log("REVIEW FIRSTTAP worstFrameMs=" + (worst * 1000f).ToString("0.0") + " fleet=" + game.Battle.Fleet.Count);
                yield return Shot("46-tutorial-fusion", .3f);
                game.Tap(new Vector2(game.Battle.QueuePositionX(0), 725f)); yield return new WaitForSecondsRealtime(.4f);
                game.Tap(new Vector2(game.Battle.QueuePositionX(0), 725f)); yield return Shot("47-fusion-stamp", .3f);
                yield return Shot("48-tutorial-repel", 1.6f);
                game.TapUi(50); yield return Shot("49-repel-beam", .15f);
                yield return Shot("50-repel-done", 1.8f);
                game.TapUi(50); yield return Frames(3);
                Model.Battle b = game.Battle;
                for (int i = 0; i < 6; i++)
                {
                    game.Tap(new Vector2(b.QueuePositionX(i % b.LaneCount), 725f));
                    yield return new WaitForSecondsRealtime(.12f);
                }
                yield return Shot("51-battle", 2.5f); yield return Shot("52-combo", 1.5f);
            }
            if (Want("rescue"))
            {
                Model.Battle b = game.Battle;
                if (b.Fleet.Count > 0) b.Fleet[0].Ammo = 1;
                yield return Shot("53-sacrifice", .35f); yield return Shot("54-sacrifice-push", .35f);
                w = 0f;
                while ((b.Repelling || b.Invulnerable > 0f) && w < 4f) { w += Time.unscaledDeltaTime; yield return null; }
                SetField(game, "tutorialStep", -1); SetProp(b, "Offset", (b.Offset + b.Gap) + -8f);
                yield return Shot("55-warning", .5f);
                b.Breach(); w = 0f;
                while (game.Feedback.RescueAge < .9f && b.State == Phase.ReviveOffer && w < 4f)
                { w += Time.unscaledDeltaTime; yield return null; }
                yield return Shot("56-revive");
                game.TapUi(30); yield return Shot("57-purge", .3f); yield return Shot("58-purge-done", .6f);
                w = 0f;
                while (!game.AimingMissile && w < 5f) { w += Time.unscaledDeltaTime; yield return null; }
                yield return Shot("59-missile-aim", .3f);
                Vector2 at = game.AimPosition;
                game.PointerDown(at); game.PointerUp(at); yield return new WaitForSecondsRealtime(.12f);
                game.PointerDown(at); game.PointerUp(at); yield return Shot("60-missile-flight", .25f);
                yield return Shot("61-missile-impact", .45f);
                w = 0f;
                while ((b.Invulnerable > 0f || b.Repelling || b.PendingStrike != null) && w < 8f)
                { w += Time.unscaledDeltaTime; yield return null; }
                game.TapUi(12); yield return Shot("62-bomb-aim", .5f);
                game.TapUi(9); yield return Shot("63-bomb-blast", .4f); yield return Shot("64-bomb-crater", .5f);
                w = 0f;
                while ((b.Invulnerable > 0f || b.Repelling || b.PendingStrike != null) && w < 8f)
                { w += Time.unscaledDeltaTime; yield return null; }
                b.Breach(); yield return Shot("65-fail", 2.2f);
            }
            if (Want("result"))
            {
                game.TapUi(40); w = 0f;
                while ((game.TransitionActive || !game.Battle.Active) && w < 14f)
                { w += Time.unscaledDeltaTime; yield return null; }
                yield return Frames(10);
                Model.Battle b = game.Battle;
                for (int color = 0; color < b.ColorCount && b.State != Phase.Won; color++) b.Sweep(color);
                yield return Shot("66-victory", 1f); yield return Shot("67-victory-rewards", 1.6f);
            }
            if (Want("pierce"))
            {
                e = game.Economy; e.Data.unlocked = Math.Max(e.Data.unlocked, 3); e.Data.cleared.Remove(2);
                int before = e.Data.cores.Count;
                game.StartSortie(1); w = 0f;
                while (game.Battle.State == Phase.Intro && w < 8f) { w += Time.unscaledDeltaTime; yield return null; }
                yield return Frames(5);
                Model.Battle b = game.Battle;
                game.Tap(new Vector2(b.QueuePositionX(0), 725f)); yield return new WaitForSecondsRealtime(.3f);
                for (int color = 0; color < b.ColorCount && b.State != Phase.Won; color++) b.Sweep(color);
                yield return Shot("68-victory-no-module", 2.4f);
                FleetProfile d = game.Economy.Data;
                Debug.Log("REVIEW GRANT coresBefore=" + before + " coresAfter=" + d.cores.Count
                    + " granted=" + (game.LastReward == null ? -1 : game.LastReward.grantedCore));
                d.parts[0] = 5; game.StartSortie(2); w = 0f;
                while (game.Battle.State == Phase.Intro && w < 8f) { w += Time.unscaledDeltaTime; yield return null; }
                b = game.Battle;
                for (int round = 0; round < 4; round++)
                    for (int lane = 0; lane < b.LaneCount; lane++)
                    {
                        game.Tap(new Vector2(b.QueuePositionX(lane), 725f)); yield return new WaitForSecondsRealtime(.35f);
                    }
                yield return Shot("70-pierce", .6f); yield return Shot("71-pierce-gauge", 1.4f);
                Debug.Log("REVIEW PIERCE shots=" + b.Pierced + " sparks=" + game.Feedback.PierceSparks
                    + " firepower=" + b.Firepower.ToString("0.0") + " factor=" + b.SpeedFactor.ToString("0.00"));
            }
            if (Want("finale"))
            {
                e = game.Economy; e.Data.unlocked = Math.Max(e.Data.unlocked, 4);
                game.StartSortie(3); w = 0f;
                while ((game.TransitionActive || game.Battle.State == Phase.Intro) && w < 10f)
                { w += Time.unscaledDeltaTime; yield return null; }
                yield return Frames(5);
                Model.Battle b = game.Battle;
                for (int lane = 0; lane < b.LaneCount; lane++)
                {
                    game.Tap(new Vector2(b.QueuePositionX(lane), 725f)); yield return new WaitForSecondsRealtime(.3f);
                }
                yield return new WaitForSecondsRealtime(1.2f);
                int staged = b.StageFinaleForEvidence(330);
                yield return Shot("72-finale-guide", 1.6f);
                Debug.Log("REVIEW FINALE guide=" + game.FinaleGuideActive + " paused=" + game.Paused
                    + " ready=" + b.FinaleReady + " button=" + game.FinaleButtonVisible + " remaining=" + b.Remaining
                    + " staged=" + staged + " fleet=" + b.Fleet.Count + " reserveEmpty=" + b.ReserveEmpty);
                int before = b.Remaining; bool tapped = game.TapUi(8);
                yield return Shot("73-finale-merge", .62f); yield return Shot("74-finale-charge", .72f);
                yield return Shot("75-finale-beam", .85f); yield return Shot("76-finale-clear", 1.25f);
                yield return Shot("77-finale-victory", 1.6f);
                Debug.Log("REVIEW FINALE tapped=" + tapped + " before=" + before + " kills=" + b.FinaleKills
                    + " remaining=" + b.Remaining + " state=" + b.State + " used=" + b.FinaleUsed);
            }
            if (Want("v10")) yield return TourV10();
            if (Want("v11")) yield return TourV11();
            if (Want("fleet"))
            {
                for (int model = 0; model < 5; model++)
                {
                    FleetProfile d = game.Economy.Data;
                    d.assembled |= 1 << model; d.unlocked = Math.Max(d.unlocked, 4);
                    d.parts[model * 5] = Math.Max(0, Math.Min(model, 4)) + 1;
                    for (int part = 1; part < 5; part++) d.parts[model * 5 + part] = Math.Max(1, Math.Min(model + 1, 5));
                    d.activeFighter = model; game.StartSortie(2); w = 0f;
                    while ((game.TransitionActive || game.Battle.State == Phase.Intro) && w < 10f)
                    { w += Time.unscaledDeltaTime; yield return null; }
                    yield return Frames(5);
                    Model.Battle b = game.Battle; SetField(game, "tutorialStep", -1);
                    for (int i = 0; i < 9; i++)
                    {
                        game.Tap(new Vector2(b.QueuePositionX(i % b.LaneCount), 725f)); yield return new WaitForSecondsRealtime(.25f);
                    }
                    for (int i = 0; i < b.Fleet.Count; i++) b.Fleet[i].Tier = i & 3;
                    yield return Shot("8" + model + "-fleet-" + FleetCatalog.FighterNames[model], 1.2f);
                    Debug.Log("REVIEW FLEET model=" + game.Feedback.Model + " stage=" + game.Feedback.Stage
                        + " fleet=" + b.Fleet.Count + " pierce=" + b.PierceDepth);
                    if (model >= 0)
                    {
                        for (int lane = 0; lane < b.LaneCount; lane++)
                        {
                            game.Tap(new Vector2(b.QueuePositionX(lane), 725f)); yield return new WaitForSecondsRealtime(.2f);
                        }
                        yield return new WaitForSecondsRealtime(.8f);
                        b.StageFinaleForEvidence(330); yield return new WaitForSecondsRealtime(1.2f);
                        game.TapUi(8); yield return Frames(3);
                        if (b.ActiveFinale == null) { game.TapUi(8); yield return Frames(3); }
                        yield return Shot("8" + model + "b-finale-merge-" + FleetCatalog.FighterNames[model], .9f);
                        yield return Shot("8" + model + "c-finale-beam-" + FleetCatalog.FighterNames[model], 1.05f);
                        yield return Shot("8" + model + "d-finale-sweep-" + FleetCatalog.FighterNames[model], .45f);
                        Debug.Log("REVIEW FLEET FINALE model=" + model + " used=" + b.FinaleUsed + " kills=" + b.FinaleKills);
                        w = 0f;
                        while (b.State != Phase.Won && w < 6f) { w += Time.unscaledDeltaTime; yield return null; }
                    }
                }
                game.Economy.Data.activeFighter = 0;
            }
            if (Want("weapons"))
            {
                for (int model = 0; model < 5; model++)
                {
                    FleetProfile d = game.Economy.Data;
                    d.assembled |= 1 << model; d.unlocked = Math.Max(d.unlocked, 24);
                    for (int part = 0; part < 5; part++) d.parts[model * 5 + part] = 5;
                    d.activeFighter = model; game.StartSortie(21); w = 0f;
                    while ((game.TransitionActive || game.Battle.State == Phase.Intro) && w < 10f)
                    { w += Time.unscaledDeltaTime; yield return null; }
                    yield return Frames(5);
                    Model.Battle b = game.Battle; SetField(game, "tutorialStep", -1);
                    for (int i = 0; i < 6; i++)
                    {
                        game.Tap(new Vector2(b.QueuePositionX(i % b.LaneCount), 725f)); yield return new WaitForSecondsRealtime(.2f);
                    }
                    yield return Shot("86-weapon-" + model + "a-" + FleetCatalog.FighterNames[model], .7f);
                    yield return Shot("86-weapon-" + model + "b-" + FleetCatalog.FighterNames[model], .23f);
                    Debug.Log("REVIEW WEAPON model=" + model + " form=" + b.Weapon.Form + " bursts=" + game.Feedback.WeaponBursts);
                    for (int lane = 0; lane < b.LaneCount; lane++)
                    {
                        game.Tap(new Vector2(b.QueuePositionX(lane), 725f)); yield return new WaitForSecondsRealtime(.15f);
                    }
                    yield return new WaitForSecondsRealtime(.6f);
                    b.StageFinaleForEvidence(330); yield return new WaitForSecondsRealtime(1f);
                    game.TapUi(8); yield return Frames(3);
                    if (b.ActiveFinale == null) { game.TapUi(8); yield return Frames(3); }
                    yield return Shot("87-finale-" + model + "a-" + FleetCatalog.FighterNames[model], 1.8f);
                    yield return Shot("87-finale-" + model + "b-" + FleetCatalog.FighterNames[model], .6f);
                    yield return Shot("87-finale-" + model + "c-" + FleetCatalog.FighterNames[model], .95f);
                    Debug.Log("REVIEW FINALE LV5 model=" + model + " mode=" + (b.ActiveFinale == null ? -1 : b.ActiveFinale.Mode) + " kills=" + b.FinaleKills);
                    w = 0f;
                    while (b.State != Phase.Won && w < 6f) { w += Time.unscaledDeltaTime; yield return null; }
                }
                game.Economy.Data.activeFighter = 0;
            }
            Done = true;
        }
    }
}

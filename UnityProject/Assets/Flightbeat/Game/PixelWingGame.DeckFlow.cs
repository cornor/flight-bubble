using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public enum TransitionKind { Launch, Home, Retry, Next }

    public partial class PixelWingGame
    {
        private int launchLevel;
        private bool launchBoom, launchSent;
        private Action energyThen;
        private float profileCheck, gachaAge, transitionAge;
        private bool gachaRevealed, transitionSwapped;
        private int gachaBest, transitionLevel;
        private TransitionKind transitionKind;
        private Action transitionSwap;
        private FleetCore[] gachaResults;
        private PartPull[] partResults;
        private int moduleInspect = -1; // +292, f54860

        // src: UpdateDeck f54796; f54783 inlines this with dt + clock order.
        private void UpdateDeck(float dt)
        {
            clock  =             OriginalMath.Binary32(clock+(dt));
            profileCheck  =             OriginalMath.Binary32(profileCheck+(dt));
            TickLaunch();
            UpdateLiaison(dt);
            if (profileCheck > 1f) { profileCheck = 0f; Economy.Refresh(); }
            TickGacha(dt);
        }
        // src: TickLaunch f54632.
        private void TickLaunch()
        {
            if (launchAt < 0f) return;
            float age = OriginalMath.Binary32(ui - launchAt);
            if (!launchBoom && age >= .3f) { launchBoom = true; audioChip.Play("whoosh"); }
            if (age >= .82f && !launchSent) { launchSent = true; Sortie(launchLevel); }
            if (launchSent && age > 3.82f && InHome && !transitionActive) { launchSent = false; launchAt = -1f; }
        }
        // src: TickGacha f54669.
        private void TickGacha(float dt)
        {
            if (deckOverlay != DeckOverlay.Gacha) return;
            float before = gachaAge;
            gachaAge = OriginalMath.Binary32(before + dt);
            if (before < 1.1f && gachaAge >= 1.1f) audioChip.Play("land");
            if (!gachaRevealed && gachaAge >= 1.45f)
            {
                gachaRevealed = true;
                audioChip.Play(gachaBest > 3 ? "legend" : gachaBest == 3 ? "maxpower" : "reward");
            }
        }
        // src: GachaTapAnywhere f54640.
        private void GachaTapAnywhere()
        {
            float age = gachaAge;
            // f54640 uses the exact constant 0x3fb33334, not the decimal literal 1.4f.
            if (!gachaRevealed) { const float skip = 1.4000000953674316f; gachaAge = age > skip ? age : skip; return; }
            int count = partResults != null ? partResults.Length : gachaResults != null ? gachaResults.Length : 0;
            float duration = count > 1 ? OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)count) * .09f) + .35f) : .5f;
            if (OriginalMath.Binary32(age + -1.45f) < duration) gachaAge = count > 1 ? OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)count) * .09f) + .35f) + 1.45f) : 1.95f;
        }
        // src: OpenEnergy f54575.
        private void OpenEnergy(Action then)
        {
            energyThen = then;
            energyOpen = true;
            MarkUi(504);
            audioChip.Play("page");
        }
        // src: Sortie f54633 and closures f53972/f53973.
        public void Sortie(int index)
        {
            if (index < 0 || Economy.Data.unlocked <= index) return;
            if (index >= 2 && !evidenceControlled && !Economy.CanSortie)
            {
                launchAt = -1f;
                OpenEnergy(() => Sortie(index));
                return;
            }
            BeginTransition(TransitionKind.Launch, () => StartSortie(index), index);
        }
        // src: BeginTransition f54634.
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
        // src: TickTransition f54784. Source catches System.Exception from the
        // swap, logs it, then retains the normal cover/open timing.
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
        // src: SwitchHomeFighter f54637.
        private void SwitchHomeFighter(int direction)
        {
            int fighter = Economy.ActiveFighter, step = unchecked(direction + 5);
            for (int i = 0; i < 5; i++)
            {
                fighter = unchecked(fighter + step) % 5;
                if (Economy.Assembled(fighter)) break;
            }
            if (!Economy.SetActive(fighter)) return;
            homeSwapDir = direction;
            MarkUi(482);
            audioChip.Play("equip");
            Toast(FleetCatalog.FighterNames[fighter] + " 出战 · 配装已载入", FleetCatalog.FighterAccent[fighter]);
        }
        // src: DeckTap f54638.
        private void DeckTap(Vector2 point)
        {
            if (AdService.Busy || applicationSuspended || launchAt >= 0f) return;
            int id = HitTest(point);
            if (id == 0) { if (deckOverlay == DeckOverlay.Gacha) GachaTapAnywhere(); return; }
            if (id < 0)
            {
                if (deckOverlay == DeckOverlay.Gacha) { GachaTapAnywhere(); return; }
                if (moduleInspect >= 0 && Page == DeckPage.Upgrade && deckOverlay == DeckOverlay.None) moduleInspect = -1;
                return;
            }
            Pressed(id);
            OnDeckUi(id);
        }
        // src: GoPage f54643.
        private void GoPage(DeckPage page)
        {
            DeckPage before = Page;
            if (page == before) return;
            Page = page;
            previousPage = before;
            MarkUi(199);
            moduleInspect = -1;
            if (page == DeckPage.Upgrade) EnterHangar();
            audioChip.Play("page");
        }
        // f54641. Preserve the original dispatch order and no-op fallthrough.
        private void OnDeckUi(int id)
        {
            if (id != 110) audioChip.Play("ui");
            if (!evidenceControlled && Economy.Data.unlocked <= 2)
            {
                uint index = unchecked((uint)(id - 100));
                if (index <= 22u && ((1 << (int)index) & 5242886) != 0)
                { Toast("完成两次试飞后开放整备", uiCyan); return; }
            }
            if (id == 100) { deckOverlay = DeckOverlay.Settings; MarkUi(250); return; }
            if (id == 101 || id == 102) { deckOverlay = DeckOverlay.None; GoPage(DeckPage.Shop); return; }
            if (id >= 120 && id <= 122) { deckOverlay = DeckOverlay.None; GoPage((DeckPage)(id - 120)); return; }
            if (id == 299) { deckOverlay = DeckOverlay.None; Sortie(0); return; }
            if (id == 116) { LiaisonTap(); return; }
            if (id == 480 || id == 481) { SwitchHomeFighter(id == 480 ? -1 : 1); return; }
            if (id == 110)
            {
                if (launchAt >= 0f) return;
                int index = unchecked(Economy.Data.unlocked - 1);
                index = index < 0 ? 0 : index > Levels.Length - 1 ? Levels.Length - 1 : index;
                if (!evidenceControlled && index >= 2 && !Economy.CanSortie) { OpenEnergy(null); return; }
                audioChip.Play("launch");
                launchBoom = false; launchSent = false;
                launchLevel = index;
                launchAt = ui;
                return;
            }
            if (id == 111) { deckOverlay = DeckOverlay.Daily; MarkUi(230); Economy.Refresh(); return; }
            if (id == 112) { deckOverlay = DeckOverlay.Sign; MarkUi(240); return; }
            if (id >= 250 && id <= 257)
            {
                switch (id)
                {
                    case 250: audioChip.ToggleMusic(); break;
                    case 251: audioChip.ToggleEffects(); break;
                    case 252: effects.Enabled = !effects.Enabled; hud.Lighting = effects.Enabled; surface.Lighting = effects.Enabled; break;
                    case 253: deckOverlay = DeckOverlay.None; break;
                    case 254: tuning = true; MarkUi(60); break;
                }
                return;
            }
            // f54641 continues through these handlers in this order.
            if (OnDevUi(id)) return;
            if (OnRecycleUi(id)) return;
            if (id == 298) { deckOverlay = DeckOverlay.None; return; }
            if (unchecked((uint)(id - 60)) < 40u)
                { TuningTap(id - 60); return; }
            if (OnEnergyUi(id)) return;
            if (OnCodexUi(id)) return;
            if (OnAssemblyUi(id)) return;
            if (OnUpgradeUi(id)) return;
            if (OnShopUi(id)) return;
            OnOverlayUi(id);
        }
        // src: LiaisonTap f54644.
        private void LiaisonTap()
        {
            if (liaisonText.Length > liaisonChars) { liaisonChars = liaisonText.Length; return; }
            switch (liaisonTopic)
            {
                case 0: case 1: OnDeckUi(110); return;
                case 6: OnDeckUi(112); return;
                case 7: OnDeckUi(111); return;
                case 11: OnDeckUi(122); return;
                case 2: OnDeckUi(120); hangarTab = 0; return;
                case 8: OnDeckUi(120); hangarTab = 1; return;
                case 10: OnDeckUi(120); hangarTab = 0; FocusReadyPart(); return;
                case 12:
                    OnDeckUi(120); hangarTab = 0;
                    for (int fighter = 1; fighter < 5; fighter++) if (Economy.CanAssemble(fighter)) { hangarFighter = fighter; break; }
                    return;
                case 9:
                    OnDeckUi(120); hangarTab = 0; OpenSupply(0, -1); return;
                default:
                    homeEnteredAt = -10f;
                    liaisonTopic = 3;
                    liaisonBriefing = unchecked(liaisonBriefing + 1);
                    liaisonCheck = 1f;
                    liaisonText = "";
                    return;
            }
        }
    }
}

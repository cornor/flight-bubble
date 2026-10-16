using System;
using System.IO;
using Flightbeat.Model;
using Flightbeat.Rendering;
using Flightbeat.Economy;
using UnityEngine;
namespace Flightbeat.Game
{
    internal sealed class GameTools : IGameDevTools
    {
        private readonly PixelWingGame game;
        public GameTools(PixelWingGame game) { this.game = game; }
        public IBroadcastPilot CreatePilot(int colors, int seed) => new BroadcastPilot(colors, seed);
        // f54569. Clear the same overlays/aim state; do not change finaleGuide.
        public void SetBroadcastAutomatic(bool enabled)
        {
            game.broadcastAutomatic = enabled; game.broadcastOwnAim = false; game.broadcastTerminalAge = 0f; game.broadcastAimAge = 0f;
            if (enabled)
            {
                game.settings = false; game.homeSettings = false; game.tuning = false; game.lastTargetTap = -10f; game.aimDown = false;
                game.selection = 0; game.deckOverlay = DeckOverlay.None; game.devPanel = false;
                if (game.paused) { game.paused = false; game.effects.Freeze(false); game.audioChip.SetPaused(false); }
                if (game.InHome) BeginBroadcastSortie(Math.Max(0, Math.Min(Flightbeat.Game.PixelWingGame.Levels.Length - 1, unchecked(game.Economy.Data.unlocked - 1))));
            }
            ReportBroadcast();
        }

        // f54570 / f54867. StartSortie follows ShowAd immediately in the source.
        public void BeginBroadcastSortie(int next)
        {
            if (game.evidenceControlled) { game.levelIndex = next; game.InHome = false; game.ResetRuntime(); return; }
            if (next >= 2 && !game.Economy.CanSortie) game.ShowAd("energy", () => game.Economy.AddEnergy(5));
            game.StartSortie(next);
        }

        // f54706 / shared transition closures f53969; NextLevel is f55152.
        public void TickBroadcast(float dt)
        {
            if (game.Battle == null || game.broadcastFinished) return;
            if (game.ui >= game.broadcastReportAt) { game.broadcastReportAt = game.ui + 1f; ReportBroadcast(); }
            if (!game.broadcastAutomatic || game.transitionActive || game.ui < game.broadcastHumanUntil || game.settings || game.devPanel || game.tuning || game.energyOpen || game.streakPrompt) return;
            if (game.InHome)
            {
                if (game.brokenNotice || game.deckOverlay != DeckOverlay.None || game.Page != DeckPage.Bridge) return;
                int next = Math.Max(0, Math.Min(Flightbeat.Game.PixelWingGame.Levels.Length - 1, unchecked(game.Economy.Data.unlocked - 1)));
                game.BeginTransition(TransitionKind.Launch, () => BeginBroadcastSortie(next), next); return;
            }
            Phase state = game.Battle.State;
            if (state == Phase.Won || state == Phase.Lost)
            {
                game.broadcastTerminalAge = game.broadcastTerminalAge + dt;
                if (game.broadcastTerminalAge < (game.Battle.Level.lesson > 0 ? 1.9f : 3.4f)) return;
                int next;
                if (game.Battle.State == Phase.Won)
                {
                    game.broadcastWins = unchecked(game.broadcastWins + 1); SaveBroadcastFrame("won");
                    next = unchecked(game.levelIndex + 1) % Flightbeat.Game.PixelWingGame.Levels.Length;
                    if (next == 0) game.broadcastLoops = unchecked(game.broadcastLoops + 1);
                }
                else { game.broadcastRetries = unchecked(game.broadcastRetries + 1); SaveBroadcastFrame("retry"); next = game.levelIndex; }
                Debug.Log("BROADCAST_RESULT level=" + unchecked(game.levelIndex + 1) + " state=" + game.Battle.State + " seconds=" + game.Battle.Time + " next=" + unchecked(next + 1) + " loops=" + game.broadcastLoops);
                game.BeginTransition(state == Phase.Won ? TransitionKind.Next : TransitionKind.Retry, () => BeginBroadcastSortie(next), next); return;
            }
            if (game.broadcastTestWins > 0 && game.broadcastWins >= game.broadcastTestWins && state == Phase.Playing && game.Battle.Time > 2f)
            {
                game.broadcastFinished = true; SaveBroadcastFrame("continued"); ReportBroadcast();
                Debug.Log("BROADCAST_TEST passed wins=" + game.broadcastWins + " loops=" + game.broadcastLoops + " next=" + unchecked(game.levelIndex + 1) + " ads=" + AdService.Shown);
                Application.Quit(AdService.Shown != 0 ? 5 : 0); return;
            }
            if (game.selection == 2)
            {
                if (!game.broadcastOwnAim) return;
                game.broadcastAimAge = game.broadcastAimAge + dt;
                if (game.broadcastAimAge >= .8f) { game.TapUi(9); game.broadcastAimAge = 0f; }
                return;
            }
            if (game.finaleGuide) { game.TapUi(8); return; }
            if (game.paused || state == Phase.Intro) return;
            BroadcastDecision decision = game.broadcastPilot.Tick(game.Battle, dt);
            switch (decision.action)
            {
                case BroadcastAction.Deploy: game.Tap(new Vector2(game.Battle.QueuePositionX(decision.index), 725f)); break;
                case BroadcastAction.Bay: game.Tap(new Vector2(game.Battle.FleetPositionX(decision.index), 630f)); break;
                case BroadcastAction.Bomb: game.TapUi(12); game.broadcastAimAge = 0f; game.broadcastOwnAim = game.selection == 2; break;
                case BroadcastAction.Finale: game.TapUi(8); break;
                case BroadcastAction.Rescue:
                    if (game.feedback.EndAge < 2.5f) return;
                    game.TapUi(30); game.broadcastOwnAim = true; break;
                default: return;
            }
            game.broadcastActions = unchecked(game.broadcastActions + 1);
        }

        // f54707. Empty output skips capture; named frames are optional diagnostics.
        public void SaveBroadcastFrame(string state)
        {
            if (String.IsNullOrEmpty(game.broadcastOutput)) return;
            Texture2D frame = CaptureFrame(540, 960, null);
            File.WriteAllBytes(Path.Combine(game.broadcastOutput, unchecked(game.levelIndex + 1).ToString("D3") + "-" + state + ".png"), frame.EncodeToPNG());
            UnityEngine.Object.Destroy(frame);
        }

        // f54711. Native Unity rendering with the original texture formats,
        // relayout order, safe-area override and display restoration.
        public Texture2D CaptureFrame(int width, int height, Rect? safeOverride)
        {
            RenderTexture priorTarget = game.gameCamera.targetTexture, priorActive = RenderTexture.active;
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); target.Create();
            game.gameCamera.targetTexture = target; game.ApplyDisplay(width, height);
            if (safeOverride.HasValue)
            {
                ScreenLayout.Configure(width, height, safeOverride.Value, WechatBridge.Menu);
                foreach (RectTransform rect in new[] { game.canvasRect, game.hudRect })
                { rect.sizeDelta = new Vector2(540f, ScreenLayout.Height); rect.anchoredPosition = new Vector2(-270f, ScreenLayout.Height * -.5f); }
                game.surface.Redraw(); game.hud.Redraw();
            }
            Canvas.ForceUpdateCanvases(); game.effects.Relayout(); game.gameCamera.Render(); RenderTexture.active = target;
            var frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            frame.ReadPixels(new Rect(0f, 0f, (float)width, (float)height), 0, 0); frame.Apply();
            game.gameCamera.targetTexture = priorTarget; RenderTexture.active = priorActive;
            game.ApplyDisplay(game.evidenceViewport.x <= 0 ? Screen.width : game.evidenceViewport.x, game.evidenceViewport.y <= 0 ? Screen.height : game.evidenceViewport.y);
            Canvas.ForceUpdateCanvases(); game.effects.Relayout(); target.Release(); UnityEngine.Object.Destroy(target); return frame;
        }


        [Serializable]
        private sealed class BroadcastStatus // original metadata type 4281
        {
            public int level, levels, wins, loops, retries, actions, remaining, ads, skippedAds, humanActions;
            public int selection, openBays, superStrikes;
            public bool automatic, home, settings, panel, musicMuted, effectsMuted;
            public string state;
            public float seconds;
        }

        // src: ReportBroadcast f54571. Platform delivery is isolated in the host;
        // status fields, JSON generation and optional file-write order are source.
        public void ReportBroadcast()
        {
            var status = new BroadcastStatus
            {
                level = unchecked(game.levelIndex + 1), levels = Flightbeat.Game.PixelWingGame.Levels.Length,
                wins = game.broadcastWins, loops = game.broadcastLoops, retries = game.broadcastRetries, actions = game.broadcastActions,
                remaining = game.Battle.Remaining, ads = AdService.Shown, skippedAds = AdService.Skipped,
                humanActions = game.broadcastHumanActions, automatic = game.broadcastAutomatic, home = game.InHome,
                settings = game.settings, panel = game.devPanel, selection = game.selection, openBays = game.Battle.UnlockedSlots,
                superStrikes = game.Battle.SuperStrikeCount, musicMuted = game.audioChip.MusicMuted, effectsMuted = game.audioChip.EffectsMuted,
                state = game.Battle.State.ToString(), seconds = game.Battle.Time
            };
            string json = JsonUtility.ToJson(status);
            BroadcastHost.Publish(json);
            if (!String.IsNullOrEmpty(game.broadcastOutput)) File.WriteAllText(Path.Combine(game.broadcastOutput, "status.json"), json);
        }

        // f54861 static +4, in the source's button order.
        private static readonly string[] DevLabels = { "金币 +10万", "碎片 +5000", "体力 +20", "无限体力 30 分", "道具各 +10", "战机部件 各 +10",
            "随机装备 +10", "装备耐久 → 1", "连胜 +1", "连胜清零", "解锁全部关卡", "通关当前战区" };

        // f54568. Opening the version panel does not itself pause a battle.
        public bool OnDevUi(int id)
        {
            if (id == 740) { game.devPanel = true; game.MarkUi(740); game.audioChip.Play("page"); return true; }
            if (!game.devPanel) return false;
            if (id == 741) { game.devPanel = false; return true; }
            if (id == 754) { SetBroadcastAutomatic(!game.broadcastAutomatic); return true; }
            if (id < 742 || id >= 742 + DevLabels.Length) return false;
            string caption = DevLabels[id - 742];
            switch (id - 742)
            {
                case 0: game.Economy.DevGrant(100000, 0, 0, 0); break;
                case 1: game.Economy.DevGrant(0, 5000, 0, 0); break;
                case 2: game.Economy.DevGrant(0, 0, 20, 0); break;
                case 3: game.Economy.GrantUnlimited(30); break;
                case 4: game.Economy.DevGrant(0, 0, 0, 10); break;
                case 5: game.Economy.DevParts(10); break;
                case 6: caption = "获得 " + game.Economy.DevModules(10) + " 件装备"; break;
                case 7: caption = "已降低 " + game.Economy.DevWearAll() + " 件装备的耐久"; break;
                case 8: game.Economy.DevStreak(1); break;
                case 9: game.Economy.DevStreak(-99); break;
                case 10: game.Economy.DevUnlockAll(); break;
                case 11:
                    int zone = WarZones.OfLevel(Math.Max(0, Math.Min(Flightbeat.Game.PixelWingGame.Levels.Length - 1, unchecked(game.Economy.Data.unlocked - 1))));
                    game.Economy.DevClearZone(zone); caption = "已通关" + WarZones.Names[zone] + "，回主页领取战区奖励"; break;
            }
            game.audioChip.Play("reward"); game.Toast(caption, Flightbeat.Game.PixelWingGame.uiGreen); return true;
        }

        // f54566. WAT computes the animated top as offset + (half-height - 339).
        public void DrawDevPanel(PixelSurface s)
        {
            if (!game.devPanel) return;
            game.Backdrop(s, .84f); float enter = Flightbeat.Game.PixelWingGame.OutBack(OriginalMath.Binary32(game.Since(740) / .3f));
            float top = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - enter)) * 40f) + (OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) - 339f)));
            game.Plate(s, 50f, top, 440f, 678f, Flightbeat.Game.PixelWingGame.uiPanelHi, Flightbeat.Game.PixelWingGame.uiPanel, Flightbeat.Game.PixelWingGame.uiCyan, 7f, Flightbeat.Game.PixelWingGame.Clamp01(OriginalMath.Binary32(enter * 1.2f)));
            game.Title(s, "测试面板", 270f, OriginalMath.Binary32(top + 18f), 2.1f, Flightbeat.Game.PixelWingGame.uiText, Flightbeat.Game.PixelWingGame.uiInk);
            s.TextF(Flightbeat.Game.PixelWingGame.VersionText + " · 仅供测试", 270f, OriginalMath.Binary32(top + 58f), 1.05f, Flightbeat.Game.PixelWingGame.uiDim, 0);
            FleetProfile d = game.Economy.Data;
            s.TextF("金币 " + d.coins + "  碎片 " + d.fragments + "  体力 " + d.energy + "  连胜 " + d.streak,
                270f, OriginalMath.Binary32(top + 82f), 1.05f, Flightbeat.Game.PixelWingGame.uiGoldHi, 0);
            s.TextF("已解锁 " + d.unlocked + "/" + Flightbeat.Game.PixelWingGame.Levels.Length + " 关  装备 " + d.cores.Count + " 件",
                270f, OriginalMath.Binary32(top + 104f), 1.05f, Flightbeat.Game.PixelWingGame.uiGoldHi, 0);
            float buttonsTop = OriginalMath.Binary32(top + 134f);
            for (int i = 0; i < DevLabels.Length; i++)
                game.Button(s, 742 + i, OriginalMath.Binary32((float)((i & 1) * 202 + 72)), OriginalMath.Binary32(buttonsTop + OriginalMath.Binary32((float)((i >> 1) * 62))), 194f, 52f,
                    DevLabels[i], i > 9 ? Flightbeat.Game.PixelWingGame.Btn.Danger : Flightbeat.Game.PixelWingGame.Btn.Secondary, 1.3f);
            game.Button(s, 754, 72f, OriginalMath.Binary32(top + 512f), 396f, 50f, game.broadcastAutomatic ? "自动操作：开启" : "自动操作：关闭",
                game.broadcastAutomatic ? Flightbeat.Game.PixelWingGame.Btn.Green : Flightbeat.Game.PixelWingGame.Btn.Secondary, 1.6f);
            game.Button(s, 741, 72f, OriginalMath.Binary32(OriginalMath.Binary32(top + 678f) + -66f), 396f, 50f, "完成", Flightbeat.Game.PixelWingGame.Btn.Primary, 1.8f);
        }


        // f54702: inspect adjacent pairs without skipping option values.
        public void InitializeBroadcast(string[] args)
        {
            Application.runInBackground = true;
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] == "--broadcast-wins") Int32.TryParse(args[i + 1], out game.broadcastTestWins);
                if (args[i] == "--broadcast-out") game.broadcastOutput = args[i + 1];
            }
            game.broadcastAutomatic = game.broadcastTestWins > 0;
            if (!String.IsNullOrEmpty(game.broadcastOutput)) Directory.CreateDirectory(game.broadcastOutput);
            Debug.Log("BROADCAST_START levels=" + Flightbeat.Game.PixelWingGame.Levels.Length.ToString() + " level=" + (game.levelIndex + 1).ToString());
        }


        // f54646. Load intentionally returns a fresh authored object in this Test
        // build (f54431). Do not silently wire the controls to live battle pressure.
        public void TuningTap(int k)
        {
            PressureSettings p = PressureSettings.Load(); TuningKnob[] knobs = TuningKnob.All;
            if (k == 38) { PressureSettings.ResetTuning(); game.Toast("已恢复默认数值", Flightbeat.Game.PixelWingGame.uiCyan); return; }
            if (k == 39) { game.tuning = false; return; }
            int index = k / 2;
            if (index >= knobs.Length) return;
            TuningKnob knob = knobs[index]; float value = OriginalMath.Binary32(knob.get(p) + ((k & 1) != 0 ? knob.step : -knob.step));
            value = value < knob.min ? knob.min : value > knob.max ? knob.max : value;
            knob.set(p, OriginalMath.Binary32(OriginalMath.RoundSingle(OriginalMath.Binary32(value / knob.step)) * knob.step));
            PressureSettings.SaveTuning();
        }

        // f54603. Preserve layout mapping and all authored ranges/units.
        public void DrawTuning(PixelSurface s)
        {
            game.Backdrop(s, .85f); bool mapping = s.MapLayout; s.MapLayout = false;
            PressureSettings p = PressureSettings.Load(); TuningKnob[] knobs = TuningKnob.All;
            float enter = Flightbeat.Game.PixelWingGame.OutBack(OriginalMath.Binary32(game.Since(60) / .3f)), height = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)knobs.Length) * 62f) + 150f);
            float safeTop = OriginalMath.Binary32(ScreenLayout.Top + 10f), centered = OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) - OriginalMath.Binary32(height * .5f));
            float top = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - enter)) * 30f) + (safeTop > centered ? safeTop : centered));
            game.Plate(s, 26f, top, 488f, height, Flightbeat.Game.PixelWingGame.uiPanelHi, Flightbeat.Game.PixelWingGame.uiPanel, Flightbeat.Game.PixelWingGame.uiCyan, 7f, Flightbeat.Game.PixelWingGame.Clamp01(OriginalMath.Binary32(enter * 1.3f)));
            game.Title(s, "数值调参", 270f, OriginalMath.Binary32(top + 16f), 2f, Color.white, Flightbeat.Game.PixelWingGame.uiInk);
            string caption = PressureSettings.Tuned ? "已使用自定义数值" : "当前为默认数值";
            game.Label(s, caption, 270f, OriginalMath.Binary32(top + 58f), 1.05f, PressureSettings.Tuned ? Flightbeat.Game.PixelWingGame.uiGold : Flightbeat.Game.PixelWingGame.uiDim, 0);
            float rowsTop = OriginalMath.Binary32(top + 86f);
            for (int i = 0; i < knobs.Length; i++)
            {
                TuningKnob knob = knobs[i]; float value = knob.get(p), y = OriginalMath.Binary32(rowsTop + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * 62f));
                game.Plate(s, 40f, y, 460f, 54f, Flightbeat.Game.PixelWingGame.SourceColor(4474776592501755085UL, 4572054341893425725UL),
                    Flightbeat.Game.PixelWingGame.SourceColor(4428659729042233098UL, 4572054341884366029UL), Flightbeat.Game.PixelWingGame.A(Flightbeat.Game.PixelWingGame.uiEdge, .7f), 6f, 1f);
                game.Label(s, knob.label, 54f, OriginalMath.Binary32(y + 8f), 1.25f, Flightbeat.Game.PixelWingGame.uiText, -1);
                string number = knob.step < 1f ? value.ToString(knob.step < .1f ? "0.00" : "0.0") : Flightbeat.Game.PixelWingGame.ResultRound(value).ToString();
                s.TextF(number + knob.unit, 54f, OriginalMath.Binary32(y + 30f), 1.25f, Flightbeat.Game.PixelWingGame.uiCyan, -1);
                float progress = 0f;
                if (knob.min != knob.max) { float t = OriginalMath.Binary32((OriginalMath.Binary32(value - knob.min)) / (OriginalMath.Binary32(knob.max - knob.min))); progress = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f); }
                s.Rect(190f, OriginalMath.Binary32(y + 36f), 130f, 6f, Flightbeat.Game.PixelWingGame.SourceColor(4417130516401346314UL, 4575657222442571530UL));
                float fill = OriginalMath.Binary32(progress * 130f);
                s.Rect(190f, OriginalMath.Binary32(y + 36f), fill, 6f, Flightbeat.Game.PixelWingGame.uiCyan); s.Rect(OriginalMath.Binary32(OriginalMath.Binary32(fill + 190f) + -2f), OriginalMath.Binary32(y + 32f), 4f, 14f, Color.white);
                game.Button(s, 60 + i * 2, 338f, OriginalMath.Binary32(y + 8f), 70f, 38f, "-", Flightbeat.Game.PixelWingGame.Btn.Secondary, 2f);
                game.Button(s, 61 + i * 2, 418f, OriginalMath.Binary32(y + 8f), 70f, 38f, "+", Flightbeat.Game.PixelWingGame.Btn.Secondary, 2f);
            }
            float footer = OriginalMath.Binary32(OriginalMath.Binary32(rowsTop + OriginalMath.Binary32(OriginalMath.Binary32((float)knobs.Length) * 62f)) + 6f);
            game.Button(s, 98, 44f, footer, 180f, 46f, "恢复默认", Flightbeat.Game.PixelWingGame.Btn.Ghost, 1.3f);
            game.Button(s, 99, 236f, footer, 264f, 46f, "完成", Flightbeat.Game.PixelWingGame.Btn.Primary, 1.7f);
            s.MapLayout = mapping;
        }

    }
}

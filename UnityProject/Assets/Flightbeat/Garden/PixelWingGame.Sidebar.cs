#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using Flightbeat.Garden;
using Flightbeat.Rendering;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    // 2026-11-08 (user: Douyin's 侧边栏复访 - a required review item since 2023-11-24 - was not wired up). On Douyin only
    // (tt.checkScene says the host supports the sidebar), the home page has a 「侧边栏」 button under 伙伴图鉴. It opens a
    // dialog: entering the game from the Douyin home sidebar gives one daily reward - one each of 重排, 优先出战 and
    // 泡泡炸弹 (user's choice). Not entered from the sidebar: the steps and 「去首页侧边栏」 (tt.navigateToScene). Entered
    // from it with today's reward unclaimed: 「立即领奖」, a red dot on the button, and the dialog opens by itself on the
    // home page. Claimed: 「今天已领取，明天再来吧」. The latest entry is tracked in JS (Plugins/WebGL/FlightbeatSidebar.jslib,
    // started early by platforms/douyin/sidebar_launch.py); the claim day is kept in PlayerPrefs, not in the progress
    // data, so 重置关卡进度 does not give it again. The button's icon: plants/home/btn-sidebar (SidebarIcon-20261108, a white
    // gift on a coral app tile, delivered 11-08); without it HomeIconButton's plain ball with 「侧」 stands in.
    public partial class PixelWingGame
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int FlightbeatSidebarSupport();
        [DllImport("__Internal")] static extern int FlightbeatSidebarFrom();
        [DllImport("__Internal")] static extern int FlightbeatSidebarOpen();
#else
        static int FlightbeatSidebarSupport() => 1;
        static int FlightbeatSidebarFrom() => 0;
        static int FlightbeatSidebarOpen() => 0;
#endif
        const int SidebarEntryAction = 880, SidebarGoAction = 881, SidebarCloseAction = 882;
        const string SidebarDayKey = "flightbeat.sidebar.day", SidebarIcon = "plants/home/btn-sidebar";
        static readonly string[] SidebarWords = { "重排", "优先出战", "泡泡炸弹" };
        static readonly string[] SidebarIcons = { "arrows", "pick", "bomb" };
        bool sidebarOpen, sidebarClaimedNow, sidebarFrom, sidebarAutoOpen;
        int sidebarSupport;   // 0 still asking, 1 no, 2 yes
        float sidebarPolledAt = -9f;

        static int SidebarToday { get { DateTime d = DateTime.Now; return d.Year * 10000 + d.Month * 100 + d.Day; } }
        bool SidebarClaimedToday => PlayerPrefs.GetInt(SidebarDayKey, 0) == SidebarToday;
        bool SidebarRewardReady => sidebarFrom && !SidebarClaimedToday;

        void SidebarPoll()
        {
            if (ui - sidebarPolledAt < .5f) return;
            sidebarPolledAt = ui;
            try
            {
                if (sidebarSupport == 0) sidebarSupport = FlightbeatSidebarSupport();
                bool from = FlightbeatSidebarFrom() == 1;
                if (from && !sidebarFrom && !SidebarClaimedToday) sidebarAutoOpen = true;   // just came back through the sidebar
                sidebarFrom = from;
            }
            catch (Exception) { sidebarSupport = 1; }
        }

        // The 「侧边栏」 button under 伙伴图鉴 (same size and words as it); a red dot while today's reward is waiting.
        void DrawSidebarEntry(PixelSurface s, float x, float y)
        {
            SidebarPoll();
            if (sidebarSupport != 2) return;
            HomeIconButton(s, SidebarEntryAction, x, y, SidebarIcon, "侧边栏", Hex(0xFF8A5B), 60f, 18f);
            if (SidebarRewardReady)
            {
                float pulse = 1f + .08f * Mathf.Sin(ui * 6f);
                // a white ring so the dot stands off the icon's coral face
                s.GardenRoundedRect(x + 50f - 10f * pulse, y + 2f - 10f * pulse, 20f * pulse, 20f * pulse, 10f * pulse, Color.white, Color.white);
                s.GardenRoundedRect(x + 50f - 8f * pulse, y + 2f - 8f * pulse, 16f * pulse, 16f * pulse, 8f * pulse, Hex(0xFF5A4E), Hex(0xE8412F));
            }
            if (sidebarAutoOpen && !settings && !levelPickOpen && !transitionActive)
            {
                sidebarAutoOpen = false;
                if (SidebarRewardReady) { sidebarOpen = true; sidebarClaimedNow = false; audioChip.Play("page"); }
            }
        }

        void DrawSidebarDialog(PixelSurface s)
        {
            if (!sidebarOpen) return;
            bool mapped = s.MapLayout;
            try
            {
                bool ready = SidebarRewardReady;
                float oy = GardenModal(s, "侧边栏奖励", 250f, 460f);
                DialogText(s, "每天从抖音首页侧边栏进入游戏，可领取", 270f, oy + 326f, 19f);
                for (int i = 0; i < 3; i++)
                {
                    float cx = 150f + i * 120f, cy = oy + 398f;
                    s.GardenRoundedRect(cx - 34f, cy - 34f, 68f, 68f, 34f, Hex(0xEEF6FF), Hex(0xD9EAFF));
                    if (!GardenBars.Icon(s, SidebarIcons[i], cx, cy, 44f)) DialogText(s, SidebarWords[i].Substring(0, 1), cx, cy - 14f, 26f);
                    GardenSkin.Text(s, SidebarWords[i] + " ×1", cx, cy + 42f, 17f, true, GardenBars.Navy);
                }
                string word;
                if (sidebarClaimedNow) { DialogText(s, "领取成功！道具已放入背包", 270f, oy + 488f, 20f, Hex(0x2E9A45)); word = "好的"; }
                else if (ready) { DialogText(s, "你是从侧边栏进来的，奖励可以领啦", 270f, oy + 488f, 18f, DialogSoftInk); word = "立即领奖"; }
                else if (SidebarClaimedToday) { DialogText(s, "今天已领取，明天再来吧", 270f, oy + 488f, 18f, DialogSoftInk); word = "知道了"; }
                else
                {
                    GardenSkin.Text(s, "1. 点下方按钮，打开抖音首页侧边栏", 270f, oy + 474f, 17f, true, DialogSoftInk);
                    GardenSkin.Text(s, "2. 在侧边栏里点「泡泡小队」回到游戏领奖", 270f, oy + 500f, 17f, true, DialogSoftInk);
                    word = "去首页侧边栏";
                }
                DialogGreenButton(s, SidebarGoAction, 120f, oy + 538f, 300f, 84f, word, false);
                DialogLink(s, SidebarCloseAction, 270f, oy + 646f, "关闭");
            }
            finally { s.MapLayout = mapped; }
        }

        // Home taps: opens the dialog from its button; while open the dialog is modal (returns true for every tap).
        bool SidebarTap(int id)
        {
            if (!sidebarOpen)
            {
                if (id != SidebarEntryAction) return false;
                sidebarOpen = true; sidebarClaimedNow = false; sidebarAutoOpen = false; sidebarPolledAt = -9f;
                audioChip.Play("page");
                return true;
            }
            if (id == SidebarCloseAction) { sidebarOpen = false; audioChip.Play("ui"); }
            else if (id == SidebarGoAction)
            {
                if (sidebarClaimedNow || !SidebarRewardReady && SidebarClaimedToday) { sidebarOpen = false; audioChip.Play("ui"); }
                else if (SidebarRewardReady) ClaimSidebarReward();
                else if (FlightbeatSidebarOpen() == 1) { sidebarOpen = false; audioChip.Play("ui"); }   // the platform asks to close it and jump
                else audioChip.Play("deny");
            }
            return true;
        }

        void ClaimSidebarReward()
        {
            // The day first: a failure in between loses one reward rather than giving it twice.
            PlayerPrefs.SetInt(SidebarDayKey, SidebarToday);
            PlayerPrefs.Save();
            Economy.Commit(d => { for (int i = 0; i < 3 && i < d.items.Length; i++) d.items[i]++; });
            sidebarClaimedNow = true;
            audioChip.Play("reward");
        }
    }
}
#endif

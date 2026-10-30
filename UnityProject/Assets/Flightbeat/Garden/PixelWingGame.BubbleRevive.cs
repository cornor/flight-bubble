#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using Flightbeat.Garden;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private void RescueGarden()
        {
            if (Battle.ReviveInSlotOrder()) { rescueAimAt = OriginalMath.Binary32(Battle.Time + 1.6f); reviveSuccessAt = ui; }
        }
        // 2026-10-30 (界面优化 15, after the reference): for 1.5 s after reviving (the bomb aim starts at 1.6 s), a big
        // 「复活成功！」 over the board with white arrows rising either side, and the black bar 「前排泡泡已清除」 above the
        // rail, while the painted wave (N11) sweeps up the board (CombatFeedback).
        float reviveSuccessAt = -10f;
        void DrawReviveSuccess(PixelSurface s)
        {
            float t = ui - reviveSuccessAt;
            if (t < 0f || t >= 1.5f || Battle == null || Battle.State != Phase.Playing) return;
            float a = Clamp01(t / .15f) * Clamp01((1.5f - t) / .3f);
            bool mapped = s.MapLayout;
            float cy = GardenBars.ScreenY(s, 300f), barY = GardenBars.ScreenY(s, 536f);
            s.MapLayout = false;
            try
            {
                ResultTitle(s, "复活成功！", cy, 46f, t, a);
                float w = GardenBarArt.TextWidth("复活成功！", 46f);
                for (int side = -1; side <= 1; side += 2)
                    for (int k = 0; k < 2; k++)
                    {
                        float u = (t * 1.4f + k * .5f) % 1f, x = 270f + side * (w * .5f + 34f), y = cy + 30f - u * 70f;
                        Color c = new Color(1f, 1f, 1f, a * Mathf.Sin(u * Mathf.PI));
                        s.Glow(x, y, 26f, 26f, new Color(.6f, .9f, 1f, c.a * .5f), 0f);
                        s.Segment(new Vector2(x - 11f, y + 8f), new Vector2(x, y - 4f), 4f, c);
                        s.Segment(new Vector2(x, y - 4f), new Vector2(x + 11f, y + 8f), 4f, c);
                    }
                s.MapLayout = false;
                DarkBar(s, "前排泡泡已清除", barY, t, null, a, 17f);
            }
            finally { s.MapLayout = mapped; }
        }
    }
}
#endif

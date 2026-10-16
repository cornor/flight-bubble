using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private void DrawDeckBackground(PixelSurface s)
        {
            // The shared GardenBattleBackground prefab supplies the home background.
            s.MapLayout = false;
        }
        private static string Clock(int seconds)
        {
            seconds = seconds > 0 ? seconds : 0;
            if (seconds <= 3599) return (seconds / 60).ToString("D2") + ":" + (seconds % 60).ToString("D2");
            return (seconds / 3600).ToString() + ":" + ((seconds / 60) % 60).ToString("D2") + ":" + (seconds % 60).ToString("D2");
        }
        private void EnergyIcon(PixelSurface s, float cx, float cy, float size, bool unlimited = false)
        {
            Color tint = unlimited ? uiGold : uiGreen;
            s.Glow(cx, cy, OriginalMath.Binary32(size * 1.6f), OriginalMath.Binary32(size * 1.6f), A(tint, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(ui * 3f)) * .1f) + .3f)), 0f);
            DeckIcon(s, 13, cx, cy, size, Color.white);
            Color bolt = unlimited ? uiGoldHi : new Color(.666666687f, 1f, .470588237f, 1f);
            float unit = OriginalMath.Binary32(size / 34f), y = OriginalMath.Binary32(unit + cy), bottom = OriginalMath.Binary32(OriginalMath.Binary32(unit * 10f) + cy), left = OriginalMath.Binary32(cx - OriginalMath.Binary32(unit * 3f));
            float right = OriginalMath.Binary32(OriginalMath.Binary32(unit + unit) + cx), middle = OriginalMath.Binary32(cx + OriginalMath.Binary32(unit * -4f)), top = OriginalMath.Binary32(cy + OriginalMath.Binary32(unit * -9f)), upper = OriginalMath.Binary32(OriginalMath.Binary32(unit * 3f) + cx);
            var p0 = new Vector2(upper, top);
            var p1 = new Vector2(middle, y);
            var p2 = new Vector2(right, y);
            var p3 = new Vector2(left, bottom);
            s.Segment(p0, p1, OriginalMath.Binary32(unit * 3f), uiInk);
            s.Segment(p1, p2, OriginalMath.Binary32(unit * 3f), uiInk);
            s.Segment(p2, p3, OriginalMath.Binary32(unit * 3f), uiInk);
            s.Segment(p0, p1, OriginalMath.Binary32(unit * 1.8f), bolt);
            s.Segment(p1, p2, OriginalMath.Binary32(unit * 1.8f), bolt);
            s.Segment(p2, p3, OriginalMath.Binary32(unit * 1.8f), bolt);
        }
    }
}

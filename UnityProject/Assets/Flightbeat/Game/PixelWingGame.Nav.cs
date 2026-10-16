using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54595 InitializeArray: metadata 0x36EE20 / 0x36FF78 (hash-checked).
        private static readonly float[] NavCenters = { 95f, 270f, 445f };
        private static readonly int[] NavIcons = { 5, 4, 7 };
        private static readonly string[] NavLabels = { "升级", "指挥舱", "商店" };

        // src: DrawNav f54595. Keep background, active highlight, icon, badge,
        // then label order; hit regions are registered even for locked pages.
        private void DrawNav(PixelSurface s)
        {
            float top = NavTop, lineY = OriginalMath.Binary32(top + 14f);
            s.Rect(ScreenLayout.Left, lineY, ScreenLayout.Width, OriginalMath.Binary32(ScreenLayout.Height - lineY),
                SourceColor(4411365910074661798UL, 4573495496682346906UL));
            s.Rect(ScreenLayout.Left, lineY, ScreenLayout.Width, 2f, A(uiEdge, .85f));
            s.Rect(ScreenLayout.Left, OriginalMath.Binary32(lineY + 2f), ScreenLayout.Width, 1f,
                SourceColor(4575657222473777152UL, 4441630096532439040UL));
            Color divider = SourceColor(4543231305998925824UL, 4464688526104146739UL);
            s.Rect(182f, OriginalMath.Binary32(lineY + 14f), 1f, 66f, divider);
            s.Rect(358f, OriginalMath.Binary32(lineY + 14f), 1f, 66f, divider);
            float target = NavCenters[(int)Page], previous = NavCenters[(int)previousPage];
            float current = OriginalMath.Binary32(previous + OriginalMath.Binary32((OriginalMath.Binary32(target - previous)) * OutBack(OriginalMath.Binary32(Since(199) / .35f), 1.2f)));
            s.Glow(current, OriginalMath.Binary32(top + 52f), 190f, 86f, A(uiCyan, .16f), 0f);
            s.Rect(OriginalMath.Binary32(current + -42f), lineY, 84f, 3f, uiCyan);
            s.Glow(current, OriginalMath.Binary32(lineY + 1f), 110f, 14f, A(uiCyan, .5f), 0f);
            float labelY = OriginalMath.Binary32(top + 68f), sideY = OriginalMath.Binary32(top + 42f), centerY = OriginalMath.Binary32(top + 20f);
            for (int page = 0; page < 3; page++)
            {
                DeckPage heldPage = Page;
                float x = NavCenters[page];
                HitBox(s, 120 + page, OriginalMath.Binary32(x + -80f), OriginalMath.Binary32(top + -14f), 160f, 110f);
                float press = PressPulse(120 + page);
                bool selected = (int)heldPage == page;
                Color tint = selected ? Color.white : new Color(.58f, .68f, .75f, 1f);
                if (page != 1 && Economy.Data.unlocked <= 2) tint = new Color(.3f, .4f, .48f, .5f);
                float iconY;
                if (page == 1)
                {
                    float radius = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(press * -.07f) + 1f)) * 36f);
                    s.Glow(x, centerY, OriginalMath.Binary32(radius * 3.2f), OriginalMath.Binary32(radius * 2.6f), A(uiCyan, selected ? .28f : .12f), 0f);
                    Disc(s, x, OriginalMath.Binary32(centerY + 4f), OriginalMath.Binary32(radius + 3f), SourceColor(0UL, 4543231304950349824UL));
                    Disc(s, x, centerY, OriginalMath.Binary32(radius + 3f), selected ? new Color(.35f, .75f, .82f, 1f) : new Color(.2f, .36f, .44f, 1f));
                    Disc(s, x, centerY, radius, selected ? new Color(.06f, .22f, .3f, 1f) : new Color(.04f, .12f, .18f, 1f));
                    Disc(s, x, OriginalMath.Binary32(centerY + OriginalMath.Binary32(radius * -.28f)), OriginalMath.Binary32(radius * .62f), SourceColor(4575657222473777152UL, 4417130516449329152UL));
                    s.Ring(new Vector2(x, centerY), OriginalMath.Binary32(radius + -3f), A(selected ? uiCyan : uiEdge, .6f), 1f, 40, 0f, 360f);
                    DeckIcon(s, NavIcons[1], x, centerY, OriginalMath.Binary32((selected ? 54.000003814697266f : 50f) * (OriginalMath.Binary32(OriginalMath.Binary32(press * -.08f) + 1f))), tint);
                    iconY = centerY;
                }
                else
                {
                    DeckIcon(s, NavIcons[page], x, sideY, OriginalMath.Binary32((selected ? 49.279998779296875f : 44f) * (OriginalMath.Binary32(OriginalMath.Binary32(press * -.1f) + 1f))), tint);
                    if (selected) s.Glow(x, sideY, 90f, 70f, A(uiCyan, .12f), 0f);
                    iconY = sideY;
                }
                if (page == 0)
                {
                    int fragments = Economy.Data.fragments;
                    if (Economy.PartsReady + (fragments > 9 ? 1 : 0) > 0 && Economy.Data.unlocked >= 3)
                        RedDot(s, OriginalMath.Binary32(x + 24f), OriginalMath.Binary32(iconY + -20f), 0);
                }
                Label(s, NavLabels[page], x, labelY, selected ? 1.3f : 1.2f, selected ? uiCyan : uiDim);
            }
        }
    }
}

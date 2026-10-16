using System;
using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54805: source zone backdrop, decoration, planets, stars and ambient layers.
        private void DrawSpace(PixelSurface s)
        {
            float left = ScreenLayout.Left, height = ScreenLayout.Height, width = ScreenLayout.Width;
            int zone = WarZones.OfLevel(levelIndex);
            s.Rect(left, 0f, width, height, WarZones.SkyTop[zone]);
            float sourceHeight = zone == 0 ? 768f : 480f;
            float tileHeight = OriginalMath.Binary32((OriginalMath.Binary32(width * .001953125f)) * sourceHeight);
            float tileY = OriginalMath.Binary32(OriginalMath.Remainder(OriginalMath.Binary32(clock * (zone == 0 ? 3.2f : 4f)), tileHeight) - tileHeight);
            Color nebula = zone == 0 ? WarZones.Nebula[0] : SourceColor(4572054341912048435UL, 4575657222472938291UL);
            Vector2 tile = zone == 0 ? Vector2.zero : spaceZoneTiles[zone - 1];
            if (tileY < height)
            {
                float drawnHeight = OriginalMath.Binary32(tileHeight + 1f);
                do
                {
                    s.Space(tile.x, tile.y, 512f, sourceHeight, left, tileY, width, drawnHeight, nebula);
                    tileY = OriginalMath.Binary32(tileHeight + tileY);
                } while (tileY < height);
            }
            DrawSpaceZoneAura(s, zone, left, width, height);
            s.Space(256f, 768f, 128f, 128f, OriginalMath.Binary32(left + OriginalMath.Binary32(width * .72f)),
                OriginalMath.Binary32(OriginalMath.Remainder(OriginalMath.Binary32(OriginalMath.Binary32(clock * 1.2f) + 120f), OriginalMath.Binary32(height + 300f)) + -150f),
                110f, 110f, new Color(1f, 1f, 1f, zone == 0 ? .55f : .3f));
            float planetY = OriginalMath.Binary32(OriginalMath.Remainder(OriginalMath.Binary32(OriginalMath.Binary32(height * .55f) + OriginalMath.Binary32(clock * 2.1f)), OriginalMath.Binary32(height + 420f)) + -260f);
            DrawSpaceZonePlanet(s, zone, left, width, planetY);
            DrawSpaceStars(s, left, width, height);
            if (zone > 0) DrawSpaceZoneDebris(s, zone, left, width, height);
            DrawSpaceZoneParticles(s, zone, left, width, height);
            float meteorTime = OriginalMath.Remainder(clock, 7.3f);
            if (meteorTime < .7f)
            {
                float phase = OriginalMath.Binary32(meteorTime / .7f), fade = OriginalMath.Binary32(1f - phase);
                float y = OriginalMath.Binary32(height * (OriginalMath.Binary32(OriginalMath.Binary32(phase * .3f) + .08f))), x = OriginalMath.Binary32(left + OriginalMath.Binary32(width * (OriginalMath.Binary32(.9f - OriginalMath.Binary32(phase * .7f)))));
                s.Segment(new Vector2(x, y), new Vector2(OriginalMath.Binary32(x + 46f), OriginalMath.Binary32(y + -24f)), 2f, new Color(.8f, .95f, 1f, OriginalMath.Binary32(fade * .6f)));
                s.Glow(x, y, 14f, 14f, new Color(.8f, .95f, 1f, OriginalMath.Binary32(fade * .7f)), 0f);
            }
            s.Rect(left, 0f, width, height, new Color(.01f, .02f, .05f, .35f));
        }

        // Shared star loop of f54805, in original draw order; positions use fmodf
        // and the original float round-to-even helper, not platform approximations.
        private void DrawSpaceStars(PixelSurface s, float left, float width, float height)
        {
            float span = OriginalMath.Binary32(height + 10f);
            for (int index = 0; index != 120; index++)
            {
                int band = index % 3, fifth = index % 5;
                float baseAlpha = band == 0 ? .35f : band == 1 ? .55f : .85f;
                float phase = OriginalMath.Binary32((float)index);
                float twinkle = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(phase * 1.7f) + OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)fifth) * .37f) + 1.1f)) * clock))) * .5f) + .5f)) * .45f) + .55f);
                float red = fifth != 0 ? .85f : .7f, green = fifth != 0 ? .9f : .85f;
                float y = OriginalMath.Binary32(OriginalMath.Remainder(OriginalMath.Binary32(OriginalMath.Binary32(clock * (band == 0 ? 4f : band == 1 ? 11f : 26f))
                    + OriginalMath.Binary32((float)unchecked(index * 83 + 35))), span) + -5f);
                float x = OriginalMath.Binary32(left + OriginalMath.Remainder(OriginalMath.Binary32((float)unchecked(index * 137 + 29)), width));
                float alpha = OriginalMath.Binary32(baseAlpha * twinkle);
                if (index % 7 == 0) { red = 1f; green = .85f; }
                float blue = index % 7 != 0 ? 1f : .7f;
                float size = band == 2 ? 2f : 1f;
                s.Rect(OriginalMath.RoundSingle(x), OriginalMath.RoundSingle(y), size, size, new Color(red, green, blue, alpha));
                if (band == 2)
                    s.Rect(OriginalMath.RoundSingle(x), OriginalMath.Binary32(OriginalMath.RoundSingle(y) + -3f), size, 3f, new Color(red, green, blue, OriginalMath.Binary32(alpha * .35f)));
                if (index % 23 != 0) continue;
                float flare = OriginalMath.Pow(OriginalMath.WasmMax(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * .9f) + phase)), 0f), 10f);
                if (!(flare > .05f)) continue;
                Color cross = new Color(.7f, .9f, 1f, OriginalMath.Binary32(flare * .7f));
                s.Rect(OriginalMath.Binary32(x + -4f), y, 9f, 1f, cross);
                s.Rect(x, OriginalMath.Binary32(y + -4f), 1f, 9f, cross);
                s.Glow(x, y, 20f, 20f, new Color(.6f, .8f, 1f, OriginalMath.Binary32(flare * .4f)), 0f);
            }
        }

        // f54720. The edge is armorEdge when open, the original orange when closed.
        private void DrawDeck(PixelSurface s)
        {
            s.Rect(12f, 582f, 516f, 93f, new Color(.007843137718737125f, .05882352963089943f, .09803921729326248f, 1f));
            for (int bay = 0; bay < Battle.MaximumSlots; bay++)
            {
                float x = Battle.FleetPositionX(bay);
                int intervals = unchecked(Battle.MaximumSlots - 1);
                float width = OriginalMath.WasmMin(OriginalMath.Binary32(OriginalMath.Binary32(456f / OriginalMath.Binary32((float)(intervals > 1 ? intervals : 1))) + -3f), 61f);
                Color edge = Battle.BayOpen(bay) ? armorEdge : new Color(.9333333373069763f, .6352941393852234f, .10588235408067703f, 1f);
                Plate(s, OriginalMath.Binary32(x + OriginalMath.Binary32(width * -.5f)), 605f, width, 62f, uiPanelHi, uiPanel, edge, 6f, 1f);
            }
        }
    }
}

using System;
using Flightbeat.Feedback;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54820. Bay labels, locked ads, occupied hulls, ranks and ammunition.
        private void DrawBays(PixelSurface s)
        {
            for (int bay = 0; bay < Battle.MaximumSlots; bay++)
            {
                float x = Battle.FleetPositionX(bay);
                bool open = Battle.BayOpen(bay);
                int count = unchecked(Battle.MaximumSlots - 1);
                float width = OriginalMath.WasmMin(OriginalMath.Binary32(OriginalMath.Binary32(456f / OriginalMath.Binary32((float)(count > 1 ? count : 1))) + -3f), 61f);
                if (!open)
                {
                    float pulse = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 4f) + OriginalMath.Binary32((float)bay))) * .4f) + .6f);
                    s.Border(OriginalMath.Binary32(x - OriginalMath.Binary32(width * .5f)), 605f, width, 62f, A(uiGold, OriginalMath.Binary32(OriginalMath.Binary32(pulse * .3f) + .5f)), 1f);
                    s.Text("+", OriginalMath.Binary32(x + -8f), 614f, 2f, uiGold, false);
                    AdIcon(s, OriginalMath.Binary32(x + -9f), 641f, 18f);
                    continue;
                }
                float left = OriginalMath.Binary32(x + OriginalMath.Binary32(width * -.5f));
                s.Border(left, 605f, width, 62f, A(uiEdge, .8f), 1f);
                int slot = 0;
                for (; slot < Battle.Fleet.Count; slot++) if (Battle.VisualSlot(slot) == bay) break;
                if (slot == Battle.Fleet.Count)
                {
                    s.Text(unchecked(bay + 1).ToString(), OriginalMath.Binary32(x + -4f), 611f, 1f, A(uiDim, .6f), false);
                    s.Corners(OriginalMath.Binary32(x + -12f), 635f, 24f, 21f, A(uiEdge, .6f), 6, 1);
                    continue;
                }
                Fighter ship = Battle.Fleet[slot];
                Color color = colors[ship.Color];
                s.Rect(OriginalMath.Binary32(left + 1f), 606f, OriginalMath.Binary32(width + -2f), 60f, new Color(color.r, color.g, color.b, .2f));
                s.Border(left, 605f, width, 62f, new Color(color.r, color.g, color.b, .95f), 2f);
                feedback.DrawShip(s, ship, slot);
                CombatFeedback.DrawRank(s, ship, new Vector2(OriginalMath.Binary32(left + 2f), 607f));
                float ratio = ship.InitialAmmo <= 0 ? 0f : OriginalMath.Binary32(OriginalMath.Binary32((float)ship.Ammo) / OriginalMath.Binary32((float)ship.InitialAmmo));
                s.Rect(OriginalMath.Binary32(left + 3f), 650f, OriginalMath.Binary32(width + -6f), 15f, SourceColor(4417130516392957706UL, 4569892616123470316UL));
                bool low = ratio < .2f;
                s.Rect(OriginalMath.Binary32(left + 4f), 662f, OriginalMath.Binary32((OriginalMath.Binary32(width + -8f)) * ratio), 2f, low ? uiRed : color);
                string text = ship.Ammo.ToString();
                float measured = PixelSurface.Measure(ship.Ammo.ToString(), 1f);
                s.Text(text, OriginalMath.Binary32(x - OriginalMath.Binary32(measured * .5f)), 652f, 1f, new Color(1f, low ? .6f : 1f, low ? .5f : 1f, 1f), false);
            }
        }

        // f54821. Queue columns include entry offsets and their exact fade rule.
        private void DrawLanes(PixelSurface s, bool intro)
        {
            float laneWidth = Battle.LaneWidth;
            float width = OriginalMath.WasmMin(OriginalMath.Binary32(laneWidth + -18f), 82f), half = OriginalMath.Binary32(width * .5f), fillWidth = OriginalMath.Binary32(laneWidth + -6f);
            for (int lane = 0; lane < Battle.LaneCount; lane++)
            {
                float left = OriginalMath.Binary32(Battle.LaneLeft(lane) + 3f), center = Battle.QueuePositionX(lane);
                var ships = Battle.Reserve[lane];
                s.Rect(left, 696f, fillWidth, 172f, SourceColor(4447394707099866890UL, 1041194025u, (lane & 1) != 0 ? .34f : .5f));
                s.Rect(left, 696f, fillWidth, 2f, SourceColor(4543231305995570381UL, 4494952716802667315UL));
                for (float y = OriginalMath.Binary32(858f - OriginalMath.Remainder(OriginalMath.Binary32(clock * 26f), 24f)); y > 704f; y  = OriginalMath.Binary32(y+(-24f)))
                {
                    float value = OriginalMath.Binary32((OriginalMath.Binary32(y + -700f)) / 50f);
                    int row = Math.Abs(value) < 2147483648f ? (int)value : int.MinValue;
                    row = row < 2 ? row : 2;
                    row = row > 0 ? row : 0;
                    if (row < ships.Count && y < 864f) continue;
                    Color tint = SourceColor(4564848585072653107UL, 1065353216u, ships.Count != 0 ? .1f : .16f);
                    s.Segment(new Vector2(OriginalMath.Binary32(center + -7f), OriginalMath.Binary32(y + 5f)), new Vector2(center, y), 2f, tint);
                    s.Segment(new Vector2(center, y), new Vector2(OriginalMath.Binary32(center + 7f), OriginalMath.Binary32(y + 5f)), 2f, tint);
                }
                if (ships.Count > 0)
                    s.Corners(OriginalMath.Binary32(center - half), 698f, width, 62f, selection == 1 ? new Color(0f, 1f, 1f, 1f)
                        : new Color(.13333334028720856f, .7215686440467834f, .8274509906768799f, 1f), 8, 2);
                for (int row = 0; row < 3 && row < ships.Count; row++)
                {
                    if (row != 0 && selection == 1)
                        s.Corners(OriginalMath.Binary32(center - half), OriginalMath.Binary32((float)(row * 50 + 698)), width, 58f, new Color(0f, 1f, 1f, 1f), 8, 1);
                    float offset = intro ? FleetOpening.BayOffset(Battle.IntroTime, lane, row) : 0f;
                    if (intro && offset >= 159f) continue;
                    float dy = OriginalMath.Binary32(offset * .35f), alpha = OriginalMath.Binary32(OriginalMath.Binary32(offset / -160f) + 1f);
                    feedback.DrawReserve(s, ships[row], lane, row, dy, alpha);
                    Vector2 at = feedback.ReservePosition(ships[row], lane, row);
                    string text = row != 0 && ships[row].Kind == 2 ? "?" : ships[row].Ammo.ToString();
                    s.Text(text, OriginalMath.Binary32(OriginalMath.Binary32(at.x - OriginalMath.Binary32(PixelSurface.Measure(text, 2f) * .5f)) + 1f), OriginalMath.Binary32(OriginalMath.Binary32(dy + at.y) + 17f), 2f, new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .7f)), false);
                    s.Text(text, OriginalMath.Binary32(at.x - OriginalMath.Binary32(PixelSurface.Measure(text, 2f) * .5f)), OriginalMath.Binary32(OriginalMath.Binary32(dy + at.y) + 16f), 2f, new Color(1f, 1f, 1f, alpha), false);
                    if (row == 0 && Battle.ActiveFinale == null && Battle.WouldMerge(lane))
                        DrawMergeHint(s, OriginalMath.Binary32(at.x + 19f), OriginalMath.Binary32(OriginalMath.Binary32(dy + at.y) + -22f), lane, alpha);
                }
                if (ships.Count != 0 || FinaleButtonVisible || Battle.ActiveFinale != null) continue;
                s.TextF("已出击", OriginalMath.Binary32(center + 1f), 721f, 1f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiDim.a * .7f)), 0);
                s.TextF("已出击", center, 720f, 1f, uiDim, 0);
            }
            for (int lane = 1; lane < Battle.LaneCount; lane++)
            {
                float x = OriginalMath.RoundSingle(Battle.LaneLeft(lane));
                for (int n = 0; n < 28; n++)
                    s.Rect(x, OriginalMath.Binary32(OriginalMath.Binary32((float)(n * 6)) + 700f), 1f, 3f, SourceColor(4516174386413232353UL, 4575657222463348961UL));
            }
        }

        // f54834. Two scan-filled discs and a glyph, matching the source row sweep.
        private void DrawMergeHint(PixelSurface s, float x, float y, int lane, float fade)
        {
            float wave = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(clock * 6f) + OriginalMath.Binary32((float)lane)));
            float scale = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.WasmMax(wave, 0f) * .18f) + 1f);
            s.Glow(x, y, OriginalMath.Binary32(scale * 46f), OriginalMath.Binary32(scale * 46f),
                SourceColor(4561245704525185024UL, 1050253722u, OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(wave * .5f) + .5f)) * .35f) + .4f)) * fade)), 0f);
            float[] radii = { OriginalMath.Binary32(scale * 14f), OriginalMath.Binary32(scale * 12.5f) };
            Color[] fills = { SourceColor(4483423504153391923UL, 1017370378u, fade),
                new Color(1f, OriginalMath.Binary32(OriginalMath.Binary32(wave * .15f) + .78f), BitConverter.Int32BitsToSingle(1049582633), fade) };
            for (int disc = 0; disc < 2; disc++)
            {
                float radius = radii[disc], squared = OriginalMath.Binary32(radius * radius);
                for (float row = -radius; row < radius; row  = OriginalMath.Binary32(row+(2f)))
                {
                    float ordinate = OriginalMath.Binary32(row + 1f);
                    float halfWidth = OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.WasmMax(OriginalMath.Binary32(squared - OriginalMath.Binary32(ordinate * ordinate)), 0f)));
                    if (halfWidth > 0f) s.Rect(OriginalMath.Binary32(x - halfWidth), OriginalMath.Binary32(row + y), OriginalMath.Binary32(halfWidth + halfWidth), 2.3f, fills[disc]);
                }
            }
            s.TextF("合", x, OriginalMath.Binary32(y + OriginalMath.Binary32(scale * -9f)), OriginalMath.Binary32(scale * 1.25f), SourceColor(4441630096517339546UL, 1017370378u, fade), 0);
        }
    }
}

using System;
using System.Collections.Generic;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54732: preserve poses before feedback's next tick discards the fleet.
        private void OnFinaleStarted(Finale finale)
        {
            finaleShips.Clear();
            finaleEndClock = -1f;
            finaleShown = finale;
            for (int i = 0; i < finale.Ships.Count; i++)
            {
                Fighter ship = finale.Ships[i];
                finaleShips.Add(new FinaleShip { from = feedback.Position(ship.Id, i), color = ship.Color, tier = ship.Tier, id = ship.Id });
            }
            feedback.Flash(SourceColor(4575657222473777152UL, 4518011146384441344UL), .2f);
        }

        // f54733: delayed local flashes retain the source cell and color. The
        // five particle cap applies per callback; black-hole hits use no particles.
        private void OnFinaleBeam(Finale finale, List<int> hits)
        {
            if (hits.Count == 0) return;
            int particles = 0;
            for (int i = 0; i < hits.Count; i++)
            {
                int cell = hits[i];
                if (!Battle.OnBoard(cell)) continue;
                Vector2 at = ScreenLayout.EnemyPoint(Battle.EnemyX(cell), Battle.EnemyY(cell));
                Color color = colors[Battle.ColorAt(cell) % colors.Length];
                finaleBlasts.Add(new FinaleBlast { at = at, born = clock + (float)(unchecked(cell * 7919) % 5) * .012f, color = color, seed = cell });
                if (finale.Mode == 2 || particles > 4) continue;
                float ticks = clock * 60f;
                int frame = Math.Abs(ticks) < 2147483648f ? (int)ticks : int.MinValue;
                if (unchecked(frame + cell * 31) % 7 != 0) continue;
                string kind = finale.Mode == 3 || (finale.Mode == 0 && (particles & 1) == 0) ? "PixelRocketImpact" : "PixelLaserHit";
                effects.Play(kind, at, color);
                particles++;
            }
            if (finaleBlasts.Count > 1400) finaleBlasts.RemoveRange(0, finaleBlasts.Count - 1400);
        }

        // f54735. f32 0x3F53F7CF is the compiled merge duration operand.
        private float ShipMerge(int index)
        {
            if (finaleShown == null) return 0f;
            float delay = (float)index * .05f;
            return Clamp01((finaleShown.Age - (delay + .05f)) / OriginalMath.WasmMax(.828000009059906f - delay, .35f));
        }

        // f54734's unrolled effect positions are folded source f32 values.
        private static readonly Vector2[] finaleTierUpAt = {
            new Vector2(360f, 694f), new Vector2(326.11407470703125f, 740.909912109375f),
            new Vector2(249.97311401367188f, 752.4956665039062f), new Vector2(188.91281127929688f, 720.0330200195312f),
            new Vector2(188.91281127929688f, 667.9669799804688f), new Vector2(249.97314453125f, 635.5043334960938f),
            new Vector2(326.11407470703125f, 647.090087890625f)
        };
        // Original data segment at 3647980: modes 1..5.
        private static readonly string[] finaleModeCues = { "whoosh", "fx-collapse", "fx-burn", "legend", "shockwave" };

        private void TickFinaleVisuals(float dt)
        {
            if (finaleShown == null) return;
            float age = finaleShown.Age;
            for (int i = 0; i < finaleShips.Count; i++)
            {
                FinaleShip ship = finaleShips[i];
                if (ship.arrived || ShipMerge(i) < 1f) continue;
                ship.arrived = true;
                finaleShips[i] = ship;
                effects.Play("PixelFusion", new Vector2(270f, 694f), colors[ship.color]);
                feedback.Kick(2f);
            }
            float previous = age - dt;
            if (age >= 1.15f && previous < 1.15f)
            {
                feedback.Flash(SourceColor(4573495496714223616UL, 4554039943400888730UL), .3f);
                feedback.Kick(7f);
                effects.Play("PixelMaxFusion", new Vector2(270f, 694f), SourceColor(4564848585086074880UL, 4575657222462033101UL));
                for (int i = 0; i < 7; i++) effects.Play("PixelTierUp", finaleTierUpAt[i], finaleRainbow[i]);
            }
            if (age >= 1.65f && previous < 1.65f)
            {
                int mode = finaleShown.Mode;
                if (unchecked((uint)(mode - 1)) <= 4u) audioChip.Play(finaleModeCues[mode - 1]);
                feedback.Flash(SourceColor(4575657222473777152UL, 4543231306015703040UL), .22f);
                feedback.Kick(8f);
                audioChip.Play("maxpower");
                finaleHitStop = .16f;
            }
            if (finaleShown.Mode == 3 && finaleShown.Sweeping && FinaleRepeat(age, .3f) < dt) audioChip.Play("fx-burn");
            if (finaleShown.Mode == 2 && age >= 3.15f && previous < 3.15f) audioChip.Play("fx-collapse");
            if (finaleShown.Sweeping)
            {
                finaleKick -= dt;
                if (finaleKick <= 0f)
                {
                    finaleKick = .09f;
                    feedback.Kick(finaleShown.Mode == 2 ? 5f : 4f);
                }
            }
            int kills = finaleShown.Kills;
            if (kills >= unchecked(finaleMilestone * 100 + 100))
            {
                finaleMilestone = kills / 100;
                finaleMilestoneAt = ui;
                audioChip.Play(kills > 299 ? "combo-high" : "combo");
            }
            for (int i = finaleBlasts.Count - 1; i >= 0; i--)
                if (clock - finaleBlasts[i].born > .5f) finaleBlasts.RemoveAt(i);
            if (age >= 3.15f && previous < 3.15f)
            {
                feedback.Flash(SourceColor(4575657222473777152UL, 4561245704525185024UL), .45f);
                feedback.Kick(9f);
                for (int i = 0; i < 7; i++) effects.Play("PixelVictory", new Vector2((float)(40 + i * 76), (i & 1) == 0 ? 200f : 320f), finaleRainbow[i]);
                for (int i = 0; i < 5; i++) effects.Play("PixelSuperBomb", new Vector2((float)(70 + i * 100), (i & 1) == 0 ? 260f : 370f), finaleRainbow[i * 2 % 7]);
            }
        }
    }
}

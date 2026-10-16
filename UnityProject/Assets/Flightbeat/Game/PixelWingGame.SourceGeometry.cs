using System;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // Standalone original interfaces; the existing inlined draw paths keep
        // their own operand order and folded constants.
        private static int AssemblyIndex(int slot) // f54531
        {
            for (int i = 0; i < AssemblyOrder.Length; i++)
                if (AssemblyOrder[i] == slot) return i;
            return 0;
        }

        // f54532: unsigned range test, original tables at 3647928/3647944.
        private static Vector2 AssemblyEntry(int slot) => unchecked((uint)slot) > 3u
            ? new Vector2(.35f, 1f) : new Vector2(AssemblyFromX[slot], AssemblyFromY[slot]);

        private int UpgradeAffordable() => Economy.PartsReady > 0 ? 1 : 0; // f54629

        private int LookLevel(int fighter, int part) // f54690
        {
            if (Economy == null) return 1;
            int value = Economy.PartLevel(fighter, part);
            value = value < 5 ? value : 5;
            return value > 0 ? value : 0;
        }

        // f54691: source FleetArt static +8/+12 are Exploded/Assembled.
        // Interpolation is deliberately unclamped; preserve sub/mul/add/mul/add.
        private Vector2 PieceCentre(int piece, float cx, float cy, float scale, float explode)
        {
            Vector2 spread = FleetArt.Exploded[piece], assembled = FleetArt.Assembled[piece];
            float y = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(
                OriginalMath.Binary32(OriginalMath.Binary32(spread.y - assembled.y) * explode) + assembled.y) * scale) + cy);
            float x = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(
                OriginalMath.Binary32(OriginalMath.Binary32(spread.x - assembled.x) * explode) + assembled.x) * scale) + cx);
            return new Vector2(x, y);
        }

        private static Vector2 BannerPoint(float cx, float cy, float w, float h, int k) // f54737
        {
            w = OriginalMath.Binary32(w * .5f);
            h = OriginalMath.Binary32(h * .5f);
            float bevel = OriginalMath.Binary32(h * .9f), x;
            switch (k % 6)
            {
                case 0: x = OriginalMath.Binary32(cx - w); break;
                case 1:
                    cy = OriginalMath.Binary32(cy - h);
                    x = OriginalMath.Binary32(OriginalMath.Binary32(cx - w) + bevel); break;
                case 2:
                    cy = OriginalMath.Binary32(cy - h);
                    x = OriginalMath.Binary32(OriginalMath.Binary32(w + cx) - bevel); break;
                case 3: x = OriginalMath.Binary32(w + cx); break;
                case 4:
                    cy = OriginalMath.Binary32(h + cy);
                    x = OriginalMath.Binary32(OriginalMath.Binary32(w + cx) - bevel); break;
                default: // Original br_table default includes negative remainders.
                    cy = OriginalMath.Binary32(h + cy);
                    x = OriginalMath.Binary32(OriginalMath.Binary32(cx - w) + bevel); break;
            }
            return new Vector2(x, cy);
        }

        private float FinaleJitter(Finale f) => f.Sweeping
            ? OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(clock * 70f)) * 1.6f) : 0f; // f54757

        private static Vector2 FanDir(float degrees) // f54741
        {
            float radians = OriginalMath.Binary32(degrees * .01745329238474369f);
            return new Vector2(OriginalMath.Sin(radians), -OriginalMath.Cos(radians));
        }

        private float FinaleFade(float age, float after) // f54759
        {
            float fade = 1f;
            if (!(age < 3.15f))
            {
                fade = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(age + -3.15f) / -.7f) + 1f);
                fade = fade < 0f ? 0f : fade > 1f ? 1f : fade;
            }
            after = OriginalMath.Binary32(after + after);
            after = after < 0f ? 0f : after > 1f ? 1f : after;
            return OriginalMath.Binary32(fade * OriginalMath.Binary32(1f - after));
        }

        private float StageGain => OriginalMath.Binary32(OriginalMath.Binary32(
            OriginalMath.Binary32((float)feedback.Stage) * .07f) + .75f); // f54760

        private static float Frac(float v) => OriginalMath.Binary32(
            v - OriginalMath.Binary32((float)Math.Floor(v))); // f54761

        // f54758: literal data tables at 3648060/3648080/3648100.
        // Default handles every mode outside 1..5, including negative values.
        private static Color FinaleHue(int mode)
        {
            switch (mode)
            {
                case 1: return new Color(.7f, .95f, 1f, 1f);
                case 2: return new Color(.8f, .55f, 1f, 1f);
                case 3: return new Color(1f, .55f, .15f, 1f);
                case 4: return new Color(.55f, 1f, .78f, 1f);
                case 5: return new Color(.5f, .9f, 1f, 1f);
                default: return new Color(1f, .85f, .4f, 1f);
            }
        }
    }
}

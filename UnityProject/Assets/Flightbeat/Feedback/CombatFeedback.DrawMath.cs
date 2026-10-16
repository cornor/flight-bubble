using System;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // Decode original paired i64 stores without rounding their float payloads.
        private static Color SourceColor(ulong rg, ulong ba) => SourceColor(rg, (uint)ba,
            BitConverter.Int32BitsToSingle(unchecked((int)(ba >> 32))));
        private static Color SourceColor(ulong rg, uint blue, float alpha) => new Color(
            BitConverter.Int32BitsToSingle(unchecked((int)rg)),
            BitConverter.Int32BitsToSingle(unchecked((int)(rg >> 32))),
            BitConverter.Int32BitsToSingle(unchecked((int)blue)), alpha);
        private static Color Mix(Color a, Color b, float t)
        {
            t = Unit(t);
            return new Color(OriginalMath.Binary32(a.r + OriginalMath.Binary32(t * (OriginalMath.Binary32(b.r - a.r)))), OriginalMath.Binary32(a.g + OriginalMath.Binary32(t * (OriginalMath.Binary32(b.g - a.g)))),
                OriginalMath.Binary32(a.b + OriginalMath.Binary32(t * (OriginalMath.Binary32(b.b - a.b)))), OriginalMath.Binary32(a.a + OriginalMath.Binary32(t * (OriginalMath.Binary32(b.a - a.a)))));
        }
    }
}

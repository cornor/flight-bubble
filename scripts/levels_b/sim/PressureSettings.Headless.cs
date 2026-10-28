// Replaces Model/PressureSettings.cs outside Unity. Identical constructor defaults and FirepowerFactor; the
// game has no Resources/Balance/pressure asset, so LoadAuthored returns the defaults exactly as in Unity.
using System;

namespace Flightbeat.Model
{
    public sealed partial class PressureSettings
    {
        public PressureSettings()
        {
            revivePurgeRows = 10;
            warnDistance = 16f; sacrificeDelay = .42f;
            pushDuration = .52f; reviveDuration = 1.15f;
            revivePush = 64f; retreatFloor = -32f;
            fusionPush = 16f; shieldPush = 24f;
            stepInterval = 1f; emptyPush = 12f;
            weakFirepower = 1.5f; strongFirepower = 10f;
            weakFactor = 1.4f; strongFactor = .8f;
            attackFactor = .85f; idleFactor = 1.8f;
            openingSpeed = 1f; advanceSpeed = .9f;
            enabled = true;
        }
        public static PressureSettings Legacy => new PressureSettings { enabled = false };
        public PressureSettings Clone() => (PressureSettings)MemberwiseClone();
        public float FirepowerFactor(float firepower)
        {
            float high = weakFactor > strongFactor ? weakFactor : strongFactor;
            float weak = OriginalMath.WasmMax(weakFirepower, .01f);
            if (weak >= firepower) return high;
            float low = weakFactor < strongFactor ? weakFactor : strongFactor;
            float next = OriginalMath.Binary32(weak + .01f);
            float strong = next > strongFirepower ? next : strongFirepower;
            if (firepower >= strong) return low;
            float scale = OriginalMath.Binary32((OriginalMath.Binary32(high - low)) / (OriginalMath.Binary32((OriginalMath.Binary32(1f / weak)) - (OriginalMath.Binary32(1f / strong)))));
            return OriginalMath.Binary32((OriginalMath.Binary32(scale / firepower)) + (OriginalMath.Binary32(high - (OriginalMath.Binary32(scale / weak)))));
        }
        public static PressureSettings Load() => LoadAuthored();
        public static PressureSettings LoadAuthored() => new PressureSettings();
    }
}

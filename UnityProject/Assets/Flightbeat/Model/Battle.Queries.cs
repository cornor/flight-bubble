using System;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // f54962: authored legacy speed, otherwise the command-drill branch.
        public float AdvanceSpeed => !Pressure.enabled ? Level.advanceSpeed
            : Level.commanderTutorial ? Pressure.openingSpeed : Pressure.advanceSpeed;
        public bool Attacking => Shots.Count > 0 || OriginalMath.Binary32(Time - lastFire) < .6f; // f54963
        public float PushRemaining => OriginalMath.WasmMax(OriginalMath.Binary32(pushDistance - pushApplied), 0f); // f54972
        public float AdvanceFactor => Time < advanceSlowUntil ? .5f : 1f; // f55064
        public float SandFrozen => OriginalMath.WasmMax(OriginalMath.Binary32(sandFrozenUntil - Time), 0f); // f55117

        // f55018/f55020: integer operations wrap before conversion to float.
        public static float FleetX(int index) => OriginalMath.Binary32((float)unchecked(index * 72 + 54));
        public static float QueueX(int lane) => OriginalMath.Binary32((float)unchecked(lane * 113 + 101));

        // f55013/f55014: custom origin is used only with a positive pitch.
        private float BaseY(int index)
        {
            float pitch = Level.pitchY;
            float origin;
            if (!(pitch > 0f)) { pitch = 9f; origin = 147f; }
            else origin = Level.gridY;
            float distance = OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)(index / Level.columns)));
            return OriginalMath.Binary32(origin + distance);
        }
        private float BaseX(int column)
        {
            float pitch = Level.pitchX;
            float origin;
            if (!(pitch > 0f)) { pitch = 10.5f; origin = 65f; }
            else origin = Level.gridX;
            float distance = OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)column));
            return OriginalMath.Binary32(origin + distance);
        }

        // f55028: use the CURRENT sand slot, then subtract the entire base-Y
        // expression before abs/divide. The decompiler loses these parentheses.
        public float EnemyMotion(int index)
        {
            int slot = position[index];
            if (slot < 0) return 0f;
            float pitch = Level.pitchY;
            float origin;
            if (!(pitch > 0f)) { pitch = 9f; origin = 147f; }
            else origin = Level.gridY;
            float distance = OriginalMath.Binary32(pitch * OriginalMath.Binary32((float)(slot / Level.columns)));
            float baseline = OriginalMath.Binary32(origin + distance);
            float delta = OriginalMath.Binary32(y[index] - baseline);
            float value = OriginalMath.Binary32(Math.Abs(delta) / pitch);
            return value < 0f ? 0f : OriginalMath.WasmMin(value, 1f);
        }
        // f55031: stream cells and missing/short shades return zero. The source
        // has no additional negative-index guard; retain that contract.
        public int ShadeAt(int index) => index < boardCount && Level.shades != null && index < Level.shades.Length
            ? Level.shades[index] : 0;

        // f55093/f55094: exact binary32 display expansion and operation order.
        public float ScreenX(int index)
        {
            float centered = OriginalMath.Binary32(x[index] + -270f);
            float expanded = OriginalMath.Binary32(centered * 1.5649770498275757f);
            return OriginalMath.Binary32(expanded + 270f);
        }
        public float ScreenY(int index)
        {
            float moved = OriginalMath.Binary32(y[index] + Offset);
            float centered = OriginalMath.Binary32(moved + -562f);
            float expanded = OriginalMath.Binary32(centered * 1.31f);
            return OriginalMath.Binary32(expanded + 562f);
        }

        // f896 (only Battle.Recall alias): the original Test build rejects all
        // recall requests without reading state. This is not an unfinished path.
        public bool Recall(int slot) => false;

        // f55081: comparison-select, not Math.Max; a NaN held value is replaced.
        private void PhoenixHold()
        {
            if ((((int)Loadout.legendary) & 4) == 0) return;
            float held = advanceHeldUntil, deadline = OriginalMath.Binary32(Time + 1.5f);
            advanceHeldUntil = held > deadline ? held : deadline;
        }
        // f55126: exponent-bit test, including all infinities and NaN payloads.
        private static bool Finite(float value) => (BitConverter.SingleToInt32Bits(value) & 0x7f800000) != 0x7f800000;
        // f55133: guarded float conversion, wrapping i32 additions, UNSIGNED
        // remainder. Do not copy the decompiler's unqualified '%' expression.
        private int Roll()
        {
            int seed = unchecked(weaponShots * 104729 + Level.seed * 7919);
            return (int)(unchecked((uint)(OriginalIntCast(OriginalMath.Binary32(Time * 1000f)) + seed)) % 100u);
        }
    }
}

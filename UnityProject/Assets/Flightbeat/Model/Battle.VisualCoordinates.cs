namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // src: f55019. Division precedes multiplication; the count is MaximumSlots.
        public float FleetPositionX(int slot) => MaximumSlots >= 2
            ? OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(456f / OriginalMath.Binary32((float)unchecked(MaximumSlots - 1)))) * OriginalMath.Binary32((float)slot)) + 42f) : 270f;

        // src: f55024. Integer arithmetic wraps before the f32 conversion.
        public static float QueueY(int row) => OriginalMath.Binary32((float)unchecked(row * 50 + 725));

        // src: f54986. NaN does not take the negative-grid branch.
        public float FrontY => frontGridY < 0f ? -1f : OriginalMath.Binary32(frontGridY + Offset);

        // src: f55025/f55026. Y includes the current formation offset.
        public float EnemyX(int index) => x[index];
        public float EnemyY(int index) => OriginalMath.Binary32(y[index] + Offset);
        // f55027: the authored/sand grid ordinate before the shared offset.
        public float GridY(int index) => y[index];
        // f55015: shared get_Pitch/get_PitchY body, including NaN fallback.
        public float Pitch => Level.pitchY > 0f ? Level.pitchY : 9f;
        public float PitchY => Level.pitchY > 0f ? Level.pitchY : 9f;
        // f55029 / f54976.
        public bool OnBoard(int index) => index >= 0 && index < position.Length && position[index] >= 0;
        public bool Stalled => BlockedTime >= 1.1f;
        // f55021/f55022/f55063: logical lane edges and remaining hold duration.
        public float LaneWidth => OriginalMath.Binary32(516f / OriginalMath.Binary32((float)(Reserve.Length > 1 ? Reserve.Length : 1)));
        public float LaneLeft(int lane) => OriginalMath.Binary32(OriginalMath.Binary32(LaneWidth * OriginalMath.Binary32((float)lane)) + 12f);
        public float AdvanceHeld => OriginalMath.WasmMax(OriginalMath.Binary32(advanceHeldUntil - Time), 0f);
        // f54984. Source subtract/add/divide order and clamp behavior.
        // R1 2026-10-28 (user): combo window 2 s (was 1.4), matching Battle.Hits / Battle.Tick.
        public float ComboLife
        {
            get { float t = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(lastHit - Time) + 2f)) / 2f); return t < 0f ? 0f : OriginalMath.WasmMin(t, 1f); }
        }
    }
}

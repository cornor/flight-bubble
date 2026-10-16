using Flightbeat.Model;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53921: enumerate beams then waves; revive waves do not flash the front.
        public float FrontFlash
        {
            get
            {
                float flash = 0f;
                foreach (Beam beam in beams)
                {
                    float age = beam.age;
                    if (!(age >= .1f)) continue;
                    float value = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(age + -.1f)) / -.4f) + 1f);
                    flash = flash > value ? flash : value;
                }
                foreach (Wave wave in waves)
                {
                    if (wave.kind == RepelKind.Revive) continue;
                    float value = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(wave.age / -.35f) + 1f)) * .8f);
                    flash = flash > value ? flash : value;
                }
                return flash < 0f ? 0f : OriginalMath.WasmMin(flash, 1f);
            }
        }
    }
}

namespace Flightbeat.Model
{
    public sealed partial class Shot
    {
        public const float FlightTime = .15f;
        // f54958: Rocket takes precedence over Tier.
        public float Duration => Rocket ? .24f : Tier > 1 ? .035f : .15f;
        // f54959: each pair follows the original store order; references are cleared.
        public void Clear()
        {
            X = 0f; Y = 0f;
            PierceB = -1; PierceC = -1;
            SplashTargets = null; Extra = null;
            Rocket = false;
            Pierce = 0; PierceA = -1;
            Tier = 0; Barrel = 0;
            FighterId = 0; TargetIndex = 0;
            Age = 0f; Color = 0;
            TargetX = 0f; TargetY = 0f;
        }
        // f54960.
        public Shot() { PierceC = -1; PierceA = -1; PierceB = -1; }
    }
}

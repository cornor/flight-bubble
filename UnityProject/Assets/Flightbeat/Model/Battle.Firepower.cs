using Flightbeat.Economy;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // src: f55103, f55102, f55104, f55106, f55107 respectively.
        public bool Has(LegendaryKind kind) => (Loadout.legendary & kind) != 0;
        public bool Overclocked => Has((LegendaryKind)1) && Time >= 12f && OriginalMath.Remainder(Time, 12f) < 2f;
        public bool FusionBoosted => Time < fusionFireUntil;
        public int ChainStacks
        {
            get { int count = Combo / 100; count = count < 5 ? count : 5; return count > 0 ? count : 0; }
        }
        public bool LaunchBoosted(Fighter ship) => Loadout.launchFire > 0f && ship.LaunchedAt >= 0f && OriginalMath.Binary32(Time - ship.LaunchedAt) < 2f;

        // src: f55105, also inlined in f54966/f54967. Preserve add order and
        // the lower comparison-select followed by the original f32.min.
        public float FireBonus
        {
            get
            {
                float bonus = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((Overclocked ? .15f : 0f) + Loadout.fire)
                    + (fusionFireUntil > Time ? Loadout.fusionFire : 0f))
                    + (Combo >= 50 ? Loadout.comboFire : 0f))
                    + OriginalMath.Binary32(Loadout.comboChain * OriginalMath.Binary32((float)ChainStacks)));
                return bonus < -.3f ? -.3f : OriginalMath.WasmMin(bonus, .8f);
            }
        }
        // src: unclamped outer sum in f54967 and f54966; tier bonus is added
        // BEFORE FireBonus, followed by the launch bonus.
        private float ShipBonusSum(Fighter ship) => OriginalMath.Binary32(OriginalMath.Binary32((ship.Tier > 1 ? Loadout.fireHigh : Loadout.fireLow)
            + FireBonus) + (LaunchBoosted(ship) ? Loadout.launchFire : 0f));

        // src: f54967.
        public float FireBonusFor(Fighter ship)
        {
            float bonus = ShipBonusSum(ship);
            return bonus < -.3f ? -.3f : OriginalMath.WasmMin(bonus, .8f);
        }
        // src: f54966. Arrival/last-fire predicates exclude unordered values;
        // Ammo does not participate. Keep the source's literal .7f lower factor.
        public float RawFirepower
        {
            get
            {
                float total = 0f;
                foreach (Fighter ship in Fleet)
                {
                    if (!(ship.Arrival <= 0f) || !(OriginalMath.Binary32(Time - ship.LastFired) < .6f)) continue;
                    int tier = ship.Tier < 3 ? ship.Tier : 3;
                    tier = tier > 0 ? tier : 0;
                    float weight = TierWeight[tier];
                    float bonus = ShipBonusSum(ship);
                    float factor = bonus < -.3f ? .7f : OriginalMath.Binary32(OriginalMath.WasmMin(bonus, .8f) + 1f);
                    total = OriginalMath.Binary32(total + OriginalMath.Binary32(weight * factor));
                }
                return total;
            }
        }
        // src: f54968. Recent fire or an in-flight shot uses smoothed Firepower.
        public float SpeedFactor => !Pressure.enabled ? 1f
            : Shots.Count > 0 || OriginalMath.Binary32(Time - lastFire) < .6f ? Pressure.FirepowerFactor(Firepower)
            : OriginalMath.Binary32(Pressure.idleFactor * (OriginalMath.Binary32(1f - Loadout.idleSlow)));
    }
}

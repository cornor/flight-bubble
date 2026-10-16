using System;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // f53994. The inlined Lean/PartLevel/Get/Set operations use the original
        // catalog arrays. No preset is fabricated for a missing or broken module.
        public FleetBonus Bonus(int fighter)
        {
            var bonus = new FleetBonus();
            fighter = Math.Max(0, Math.Min(fighter, 4));
            bonus.fighter = fighter;
            FleetCatalog.Lean(fighter, bonus.lean);
            // f54161: standalone local helper also guards unsigned fighter/index.
            int Part(int index)
            {
                if (unchecked((uint)index) > 4u || unchecked((uint)fighter) > 4u) return 1;
                int value = Data.parts[fighter * 5 + index];
                return value > 0 ? Math.Min(value, 5) : 1;
            }
            bonus.parts[0] += FleetCatalog.NoseFire[Part(0)];
            bonus.pierce = FleetCatalog.NosePierce[Part(0)];
            bonus.parts[3] += FleetCatalog.LeftRepel[Part(1)];
            bonus.parts[4] += FleetCatalog.RightSacrifice[Part(2)];
            bonus.parts[8] += FleetCatalog.BodyCombo[Part(3)];
            bonus.parts[15] += FleetCatalog.BodyGold[Part(3)];
            bonus.parts[7] += FleetCatalog.EngineDanger[Part(4)];
            bonus.parts[10] += FleetCatalog.EngineLaunch[Part(4)];
            var modules = new FleetLoadout();
            for (int slot = 0; slot != 5; slot++)
            {
                FleetCore core = InPreset(fighter, slot);
                if (core == null) continue;
                ModuleSpec spec = core.Spec;
                float multiplier = slot == FleetCatalog.FighterResonance[fighter] ? 1.25f : 1f;
                spec.apply(modules, multiplier * spec.values[core.quality],
                    multiplier * (spec.second != null ? spec.second[core.quality] : 0f));
                if (core.quality == 4) bonus.legendary |= FleetCatalog.SlotLegend[slot];
            }
            var result = new FleetLoadout
            {
                pierce = Math.Max(0, Math.Min(bonus.pierce, 3)),
                legendary = bonus.legendary,
                fusionFireTime = modules.fusionFireTime
            };
            for (int stat = 0; stat != 16; stat++)
            {
                float value = modules.Get((FleetCatalog.Stat)stat);
                bonus.modules[stat] = value;
                // WAT 1107..1121: module + (lean + parts), not (module + lean) + parts.
                value = value + (bonus.lean[stat] + bonus.parts[stat]);
                float cap = FleetCatalog.StatCap[stat];
                bonus.capped[stat] = value > cap + .0001f;
                value = value < -.1f ? -.1f : value > cap ? cap : value;
                bonus.total[stat] = value;
                result.Set((FleetCatalog.Stat)stat, value);
            }
            bonus.Loadout = result;
            return bonus;
        }
    }
}

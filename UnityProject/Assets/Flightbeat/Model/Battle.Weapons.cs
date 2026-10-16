using System;
using System.Collections.Generic;
using Flightbeat.Economy;

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // f55130/f55108: unsigned phase checks accept only Intro and Ready.
        public void ApplyWeapon(WeaponProfile weapon)
        { if (unchecked((uint)State) <= 1u) Weapon = weapon ?? new WeaponProfile(); }
        public void ApplyLoadout(FleetLoadout loadout)
        {
            if (unchecked((uint)State) > 1u) return;
            Loadout = loadout ?? FleetLoadout.Empty;
            if (Loadout.extraBays <= 0) return;
            for (int i = 0; i < extraBay.Length && i != Loadout.extraBays; i++) extraBay[i] = true;
        }
        // f55131.
        private bool WeaponCandidate(int grain, int color) => grain >= 0 && grain < Alive.Length
            && Alive[grain] && !reserved[grain] && grain < position.Length && position[grain] >= 0 && ColorAt(grain) == color;

        // f55080 with f55137 key comparison. Invalid/empty requests preserve the
        // existing scan; valid scans clear it but retain existing weaponPick.
        private void NearestSame(int around, int color, float radius, int max)
        {
            if (around < 0 || max <= 0 || position[around] < 0) return;
            unchecked
            {
                int columns = Level.columns, row = position[around] / columns;
                int column = position[around] - columns * row;
                weaponScan.Clear();
                int extent = OriginalIntCast(OriginalMath.Binary32((float)Math.Ceiling(radius)));
                int first = -extent;
                float radiusSquared = OriginalMath.Binary32(radius * radius);
                for (int dy = first; dy <= extent; dy++)
                {
                    int r = dy + row;
                    if (r < 0) continue;
                    for (int dx = first; dx <= extent; dx++)
                    {
                        int distance = dx * dx + dy * dy, c = dx + column;
                        if (distance == 0 || radiusSquared < OriginalMath.Binary32((float)distance) || c < 0 || r >= sandRows || c >= columns) continue;
                        int grain = sand[c + r * columns];
                        if (!WeaponCandidate(grain, color) || weaponPick.Contains(grain)) continue;
                        weaponScan.Add(new Int2 { x = (grain & 4095) | (distance << 12), y = grain });
                    }
                }
                OriginalSort.Sort(weaponScan, CompareWeaponScan);
                for (int i = 0; i < weaponScan.Count && max > 0; i++, max--) weaponPick.Add(weaponScan[i].y);
            }
        }
        // f55137; comparison deliberately ignores the grain stored in y.
        private static int CompareWeaponScan(Int2 a, Int2 b) => a.x.CompareTo(b.x);
        // WAT's guarded i32.trunc_f32_s pattern (f55078/f55080).
        private static int OriginalIntCast(float value) => Math.Abs(value) < 2147483648f ? (int)value : int.MinValue;

        // f55078. The shot serial wraps; Blaze uses i32.rem_u, not signed %.
        private int[] WeaponExtras(Fighter ship, int target)
        {
            if (target < 0 || ship.Ammo <= 0 || position[target] < 0) return null;
            unchecked
            {
                int ammo = ship.Ammo, level = Weapon.level, lowCount = level > 2 ? 2 : 1;
                int form = level > 4 ? 3 : lowCount;
                weaponShots++;
                weaponPick.Clear();
                int color = ship.Color;
                switch (Weapon.kind)
                {
                    case WeaponKind.Pulse:
                        if (form == 2 && weaponShots % 3 == 0) NearestSame(target, color, 2.3f, 1);
                        else if ((form == 3 || (form == 2 && level >= 5)) && (weaponShots & 1) == 0) NearestSame(target, color, 2.6f, 2);
                        break;
                    case WeaponKind.Needle:
                        int columns = Level.columns, row = position[target] / columns;
                        int column = position[target] - columns * row, limit = level > 4 ? 4 : lowCount;
                        for (int r = row - 1; r >= 0 && weaponPick.Count < limit; r--)
                        {
                            int grain = sand[r * columns + column];
                            if (grain < 0 || !Alive[grain]) continue;
                            if (ColorAt(grain) != color) break;
                            if (!reserved[grain]) weaponPick.Add(grain);
                        }
                        break;
                    case WeaponKind.Quake:
                        NearestSame(target, color, level > 4 ? 2.9f : form == 2 ? 1.8f : 1.5f, level > 4 ? 5 : lowCount);
                        break;
                    case WeaponKind.Blaze:
                        uint roll = (uint)(OriginalIntCast(OriginalMath.Binary32(Time * 1000f)) + Level.seed * 7919 + weaponShots * 104729) % 100u;
                        if (roll >= (level > 4 ? 85u : form == 2 ? 60u : 35u)) break;
                        NearestSame(target, color, 1.5f, 1);
                        if (level < 5 || weaponPick.Count <= 0) break;
                        uint chainRoll = (uint)(OriginalIntCast(OriginalMath.Binary32(Time * 1000f)) + weaponShots * 104729 + Level.seed * 7919) % 100u;
                        if (chainRoll <= 49u) NearestSame(weaponPick[weaponPick.Count - 1], color, 1.5f, 1);
                        break;
                    case WeaponKind.Arc:
                        NearestSame(target, color, 3.2f, 1);
                        if (weaponPick.Count == 0) break;
                        int chain = level > 4 ? 4 : lowCount;
                        for (int i = 1; i != chain; i++)
                        {
                            int count = weaponPick.Count;
                            NearestSame(weaponPick[count - 1], color, 3.2f, 1);
                            if (count == weaponPick.Count) break;
                        }
                        break;
                }
                int countPicked = weaponPick.Count < ammo ? weaponPick.Count : ammo;
                if (countPicked <= 0) return null;
                var result = new int[countPicked];
                for (int i = 0; i < result.Length; i++) { result[i] = weaponPick[i]; reserved[result[i]] = true; }
                ship.Ammo -= countPicked;
                return result;
            }
        }

        // f55072, f55139. Distance ties remain equal; original introsort defines
        // the resulting order. Radius is the original 46 squared, 2116f.
        private int[] RocketTargets(Fighter ship, float centerX, float centerY)
        {
            var targets = new List<int>();
            for (int i = 0; i < Alive.Length; i++)
            {
                if (!WeaponCandidate(i, ship.Color)) continue;
                float dx = OriginalMath.Binary32(x[i] - centerX), dy = OriginalMath.Binary32(OriginalMath.Binary32(y[i] + Offset) - centerY);
                if (OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy)) <= 2116f) targets.Add(i);
            }
            OriginalSort.Sort(targets, (a, b) => CompareRocketDistance(a, b, centerX, centerY));
            int count = Math.Min(Math.Min(7, ship.Ammo), targets.Count);
            if (count <= 0) return null;
            int[] result = targets.GetRange(0, count).ToArray();
            foreach (int target in result) reserved[target] = true;
            ship.Ammo = unchecked(ship.Ammo - count);
            return result;
        }
        // f55139/f68877: note the operand order of y + Offset vs Offset + y.
        private int CompareRocketDistance(int a, int b, float centerX, float centerY)
        {
            float ax = OriginalMath.Binary32(x[a] - centerX), ay = OriginalMath.Binary32(OriginalMath.Binary32(y[a] + Offset) - centerY);
            float bx = OriginalMath.Binary32(x[b] - centerX), by = OriginalMath.Binary32(OriginalMath.Binary32(Offset + y[b]) - centerY);
            float da = OriginalMath.Binary32(OriginalMath.Binary32(ax * ax) + OriginalMath.Binary32(ay * ay)), db = OriginalMath.Binary32(OriginalMath.Binary32(bx * bx) + OriginalMath.Binary32(by * by));
            if (da < db) return -1;
            if (db < da) return 1;
            if (da == db) return 0;
            return float.IsNaN(da) ? (float.IsNaN(db) ? 0 : -1) : 1;
        }
    }
}

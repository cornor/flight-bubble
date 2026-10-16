using System;
using Flightbeat.Game;
using Flightbeat.Model;

namespace Flightbeat.Economy
{
    public sealed partial class FleetEconomy
    {
        // f54108 and mutation f54302. The source always publishes reward.first
        // as false; first-clear membership only controls the profile's cleared list.
        public FleetReward Win(string id, FleetLoadout loadout, bool revived, float seconds)
        {
            if (String.IsNullOrEmpty(id) || Data.attempt != id || String.Equals(Data.settled, id)) return null;
            Refresh();
            int level = Data.attemptLevel;
            bool first = !Data.cleared.Contains(level);
            int baseCoins = unchecked((level - 1) / 5 * 20 + 100);
            float gold = loadout.gold;
            int streak = Math.Min(10, unchecked(Data.streak + 1));
            // f54108: convert_i32_s -> f32.mul -> f32.floor. Mono may
            // retain a wider product unless each source f32 boundary is explicit.
            float floored = (float)Math.Floor(OriginalMath.Binary32(gold * OriginalMath.Binary32((float)baseCoins)));
            int bonus = Math.Abs(floored) < 2147483648f ? (int)floored : int.MinValue;
            int subtotal = unchecked(baseCoins + bonus);
            int percent = FleetCatalog.StreakPercent(streak);
            double rounded = Math.Round((double)OriginalMath.Binary32(OriginalMath.Binary32((float)unchecked(subtotal * percent)) / 100f), MidpointRounding.ToEven);
            int streakCoins = Math.Abs(rounded) < 2147483648.0 ? (int)rounded : int.MinValue;
            int streakFragments = 0;
            if (percent != 0)
            {
                rounded = Math.Round((double)OriginalMath.Binary32(OriginalMath.Binary32((float)unchecked(percent * 6)) / 100f), MidpointRounding.ToEven);
                streakFragments = Math.Max(1, Math.Abs(rounded) < 2147483648.0 ? (int)rounded : int.MinValue);
            }
            int zone = WarZones.OfLevel(unchecked(level - 1));
            int zoneComplete = unchecked(level - 1) == WarZones.LastLevel(zone) && ((Data.zoneClaimed >> zone) & 1) == 0 ? zone : -1;
            var reward = new FleetReward
            {
                bonusCoins = bonus, coins = unchecked(streakCoins + subtotal), first = false,
                fragments = unchecked(streakFragments + 6), zoneComplete = zoneComplete,
                streakFragments = streakFragments, streakCoins = streakCoins, streakPercent = percent, streak = streak
            };
            bool fast = seconds <= 180f && seconds >= 0f;
            Commit(d =>
            {
                d.coins = unchecked(reward.coins + d.coins);
                int fragments = reward.fragments;
                d.wins = unchecked(d.wins + 1);
                d.dayWins = unchecked(d.dayWins + 1);
                d.fragments = unchecked(fragments + d.fragments);
                if (!revived) d.dayCleanWins = unchecked(d.dayCleanWins + 1);
                if (fast) d.dayFastWins = unchecked(d.dayFastWins + 1);
                d.settled = id;
                d.attempt = String.Empty;
                if (first) d.cleared.Add(level);
                d.unlocked = Math.Min(PixelWingGame.Levels.Length, Math.Max(d.unlocked, unchecked(level + 1)));
                d.streak = streak;
                d.bestStreak = Math.Max(d.bestStreak, streak);
            });
            return reward;
        }
    }
}

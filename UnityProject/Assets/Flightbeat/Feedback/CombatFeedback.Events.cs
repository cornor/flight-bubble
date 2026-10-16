using System;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // src: f53870. Independent comparisons retain the source cue order.
        private void Cue(string cue)
        {
            if (cue == "down") rescueAge = 0f;
            if (cue == "advance") stepAge = 0f;
            if (cue == "hurt")
            {
                flashColor.r = 1f; flashColor.g = .1f;
                hurtAge = 0f; flashAge = 0f; flashLength = .35f;
                flashColor.b = .05f; flashColor.a = .55f;
                Kick(6f);
                Color tint = new Color(1f, .25882354378700256f, .3176470696926117f, 1f);
                particles.Play("PixelShield", new Vector2(60f, 574f), tint);
                particles.Play("PixelShield", new Vector2(165f, 574f), tint);
                particles.Play("PixelShield", new Vector2(270f, 574f), tint);
                particles.Play("PixelShield", new Vector2(375f, 574f), tint);
                particles.Play("PixelShield", new Vector2(480f, 574f), tint);
            }
            if (cue == "lose") flights.Clear();
            if (cue == "win" || cue == "lose") ending = true;
        }

        // src: f53929. Distance is unused; the effect height comes from FrontScreenY.
        private void Repel(RepelKind kind, float distance)
        {
            if (waves.Count >= 6) waves.RemoveAt(0);
            waves.Add(new Wave { duration = kind == RepelKind.Revive ? 1.35f : .7f, kind = kind });
            Color tint = kind == RepelKind.Revive ? new Color(.3f, 1f, .85f, 1f)
                : kind == RepelKind.Fusion ? new Color(.8f, .6f, 1f, 1f)
                : kind == RepelKind.Shield ? new Color(1f, .34f, .16f, 1f) : new Color(1f, .55f, .15f, 1f);
            float y = FrontScreenY;
            if (kind != RepelKind.Revive)
            {
                string effect = kind == RepelKind.Empty ? "PixelBurn" : "PixelShockwave";
                particles.Play(effect, new Vector2(70f, y), tint);
                particles.Play(effect, new Vector2(203f, y), tint);
                particles.Play(effect, new Vector2(336f, y), tint);
                particles.Play(effect, new Vector2(469f, y), tint);
                return;
            }
            particles.Play("PixelRescue", new Vector2(70f, 571f), tint);
            particles.Play("PixelRescue", new Vector2(203f, 571f), tint);
            particles.Play("PixelRescue", new Vector2(336f, 571f), tint);
            particles.Play("PixelRescue", new Vector2(469f, 571f), tint);
        }

        // src: f53930. Sacrifice leaves the source Pose in place.
        private void Sacrifice(Fighter ship, int visualSlot, float delay)
        {
            Pose pose;
            Vector2 from = poses.TryGetValue(ship.Id, out pose) ? pose.position
                : new Vector2(battle.FleetPositionX(visualSlot), 628f);
            kamikazes.Add(new Kamikaze { from = from, x = from.x, delay = OriginalMath.WasmMax(delay, .2f), color = ship.Color, tier = ship.Tier });
            particles.Play("PixelBurn", from, new Color(1f, .5f, .1f, 1f));
        }

        // src: f53931. Re-evaluate EnemyPoint for each effect after the Rumble event.
        private void Purged(float frontWorld, int count)
        {
            purges.Add(new PurgeFx { front = ScreenLayout.EnemyPoint(0f, frontWorld).y, count = count });
            flashColor.r = .5f; flashColor.g = 1f;
            flashAge = 0f; flashLength = .35f;
            flashColor.b = .95f; flashColor.a = .55f;
            Kick(7f);
            particles.Play("PixelSuperMissile", new Vector2(50f, OriginalMath.Binary32(ScreenLayout.EnemyPoint(0f, frontWorld).y + -20f)), new Color(.3f, .95f, 1f, 1f));
            particles.Play("PixelSuperMissile", new Vector2(138f, OriginalMath.Binary32(ScreenLayout.EnemyPoint(0f, frontWorld).y + -20f)), new Color(.3f, .95f, 1f, 1f));
            particles.Play("PixelSuperMissile", new Vector2(226f, OriginalMath.Binary32(ScreenLayout.EnemyPoint(0f, frontWorld).y + -20f)), new Color(.3f, .95f, 1f, 1f));
            particles.Play("PixelSuperMissile", new Vector2(314f, OriginalMath.Binary32(ScreenLayout.EnemyPoint(0f, frontWorld).y + -20f)), new Color(.3f, .95f, 1f, 1f));
            particles.Play("PixelSuperMissile", new Vector2(402f, OriginalMath.Binary32(ScreenLayout.EnemyPoint(0f, frontWorld).y + -20f)), new Color(.3f, .95f, 1f, 1f));
            particles.Play("PixelSuperMissile", new Vector2(490f, OriginalMath.Binary32(ScreenLayout.EnemyPoint(0f, frontWorld).y + -20f)), new Color(.3f, .95f, 1f, 1f));
        }

        // src: f53932. Count is unused.
        private void Orphans(int color, int count) => particles.Play("PixelTierUp", new Vector2(270f, 300f), palette[color]);

        // src: f53934. The strike argument is not read.
        private void StrikeLaunched(SuperStrike strike) { launchAge = 0f; }

        // src: f53935. Decimal literals below encode the exact source f32 bits;
        // decompiler decimal displays round several distinct coefficients to .5.
        private void SuperDetonate(SuperStrike strike, int count)
        {
            strikeHits = count; superAge = 0f; lastStrike = strike;
            Vector2 at = ScreenLayout.EnemyPoint(strike.X, strike.Y);
            PixelEffectPool target = particles;
            bool missile = strike.Missile;
            Color tint = missile ? new Color(.3f, .95f, 1f, 1f) : new Color(1f, .62f, .2f, 1f);
            target.Play(missile ? "PixelSuperMissile" : "PixelSuperBomb", at, tint);
            // RadiusX is read after the first Play; all six ring effects share it.
            float radius = OriginalMath.Binary32(strike.RadiusX * 1.5649770498275757f);
            particles.Play("PixelRocketImpact", new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(radius * .7f) + at.x), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(radius * 0f)) * .7f) + at.y)), tint);
            particles.Play("PixelRocketImpact", new Vector2(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(radius * .4999999701976776f)) * .7f) + at.x), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(radius * .6062178015708923f)) * .7f) + at.y)), tint);
            particles.Play("PixelRocketImpact", new Vector2(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(radius * -.5000000596046448f)) * .7f) + at.x), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(radius * .6062177419662476f)) * .7f) + at.y)), tint);
            particles.Play("PixelRocketImpact", new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(radius * -.7f) + at.x), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(radius * -6.119594075926216e-08f)) * .7f) + at.y)), tint);
            float lastY = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(radius * -.6062178015708923f)) * .7f) + at.y);
            particles.Play("PixelRocketImpact", new Vector2(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(radius * -.49999991059303284f)) * .7f) + at.x), lastY), tint);
            particles.Play("PixelRocketImpact", new Vector2(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(radius * .49999991059303284f)) * .7f) + at.x), lastY), tint);
            flashColor.b = .85f; flashColor.a = .75f;
            flashColor.r = 1f; flashColor.g = .97f;
            flashAge = 0f; flashLength = .28f;
            Kick(9f);
        }

        // src: f53936. Identity comparison, back-to-front removal of every match.
        private void CancelShot(Shot shot)
        {
            for (int i = flights.Count - 1; i >= 0; i--)
                if (ReferenceEquals(flights[i].shot, shot)) flights.RemoveAt(i);
        }
    }
}

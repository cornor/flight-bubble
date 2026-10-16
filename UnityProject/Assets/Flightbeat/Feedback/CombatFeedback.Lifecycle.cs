using System;
using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // src: f53838/f53839, f53840/f53841, f53937/f53938.
        public event Action<int> Impact;
        public event Action<float> Rumble;
        public event Action<string> WeaponCue;

        // src: f11964/f13446, f24630, f53842/f53843/f53844,
        // f13081/f53845, f13082/f53846, f24239/f53933/f23829/f23819/f53939.
        public int PierceSparks { get; private set; }
        public float Time => time;
        public float Hurt { get { float v = OriginalMath.Binary32(OriginalMath.Binary32(hurtAge / -.65f) + 1f); return v < 0f ? 0f : OriginalMath.WasmMin(v, 1f); } }
        public float EndAge => endAge;
        public int PendingHits => flights.Count;
        public int Impacts { get; private set; }
        public int Fired { get; private set; }
        public float RescueAge => rescueAge;
        public float SuperImpactAge => superAge;
        public int SuperImpactHits => strikeHits;
        public int WeaponBursts { get; private set; }

        // src: f53848. Explicit initialization preserves the original order.
        public CombatFeedback(Battle battle, PixelEffectPool particles, Color[] palette)
        {
            comboPunch = 10f; comboMilestoneAge = 10f;
            poses = new Dictionary<int, Pose>();
            retired = new List<int>();
            flights = new List<Flight>();
            transitions = new List<Transition>();
            fusions = new List<Fusion>();
            blasts = new List<Blast>();
            beams = new List<Beam>();
            List<PierceSpark> initialSparks = new List<PierceSpark>();
            shakeAge = 10f;
            flashAge = 10f; flashLength = .2f;
            rocketAge = 10f; stepAge = 10f;
            maxAge = 10f; sweepAge = 10f;
            hurtAge = 10f;
            sparks = initialSparks;
            reservePoses = new Dictionary<int, Vector2>();
            Look = FleetLook.Standard;
            marks = new List<Mark>();
            waves = new List<Wave>();
            kamikazes = new List<Kamikaze>();
            List<PurgeFx> initialPurges = new List<PurgeFx>();
            superAge = 10f; launchAge = 10f;
            rescueAge = 10f;
            purges = initialPurges;
            Weapon = new WeaponProfile();
            List<WeaponFx> initialWeaponFx = new List<WeaponFx>();
            particleTokens = 6f;
            weaponCueAt = -10f;
            weaponFx = initialWeaponFx;
            muzzles = new List<Vector3>();
            goldSparks = new List<Vector4>();
            var sweep = new SweepSubscription { palette = palette, particles = particles, owner = this };
            this.battle = battle;
            this.particles = sweep.particles;
            this.palette = sweep.palette;
            battle.StrikeDetonated += SuperDetonate;
            battle.ShotCancelled += CancelShot;
            battle.Repelled += Repel;
            battle.Launched += Launch;
            battle.Fused += Fuse;
            battle.Fired += Fire;
            battle.Impacted += Resolve;
            battle.Cue += Cue;
            battle.Sacrificed += Sacrifice;
            battle.FrontPurged += Purged;
            battle.OrphansCleared += Orphans;
            battle.StrikeLaunched += StrikeLaunched;
            battle.Swept += sweep.Invoke;
        }

        // src: display class 4278, f53946; count is unused. Capture references
        // belong to the subscription, and palette is re-read for every Play call.
        private sealed class SweepSubscription
        {
            internal CombatFeedback owner;
            internal PixelEffectPool particles;
            internal Color[] palette;
            internal void Invoke(int color, int count)
            {
                owner.sweepAge = 0f;
                owner.sweepColor = color;
                particles.Play("PixelFusion", new Vector2(45f, 285f), palette[color]);
                particles.Play("PixelFusion", new Vector2(120f, 375f), palette[color]);
                particles.Play("PixelFusion", new Vector2(195f, 285f), palette[color]);
                particles.Play("PixelFusion", new Vector2(270f, 375f), palette[color]);
                particles.Play("PixelFusion", new Vector2(345f, 285f), palette[color]);
                particles.Play("PixelFusion", new Vector2(420f, 375f), palette[color]);
                particles.Play("PixelFusion", new Vector2(495f, 285f), palette[color]);
            }
        }

        // src: f53849.
        public void Flash(Color color, float length = .2f) { flashColor = color; flashLength = length; flashAge = 0f; }

        // src: f53850. Existing stronger active shakes suppress the Rumble event.
        public void Kick(float power)
        {
            float age = shakeAge;
            if (age < .4f && OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(age / -.4f) + 1f)) * shakePower) > power) return;
            shakeAge = 0f;
            shakePower = power;
            Rumble?.Invoke(power);
        }

        // src: f53847. Four ordered contributions, then original midpoint rounding.
        public float Shake
        {
            get
            {
                float value = 0f, age = shakeAge;
                if (age < .4f) value = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(age / -.4f) + 1f)) * shakePower)) * OriginalMath.Sin(OriginalMath.Binary32(age * 95f))) + 0f);
                age = superAge;
                if (age < .45f) value = OriginalMath.Binary32(value + OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(age / -.45f) + 1f)) * OriginalMath.Sin(OriginalMath.Binary32(age * 110f)))) * 7f));
                age = hurtAge;
                if (age < .3f) value = OriginalMath.Binary32(value + OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(age / -.3f) + 1f)) * OriginalMath.Sin(OriginalMath.Binary32(age * 110f)))) * 5f));
                age = rocketAge;
                if (age < .16f) value = OriginalMath.Binary32(value + OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(age / -.16f) + 1f)) * OriginalMath.Sin(OriginalMath.Binary32(age * 93f)))) * 1.5f));
                return OriginalMath.RoundSingle(value);
            }
        }
    }
}

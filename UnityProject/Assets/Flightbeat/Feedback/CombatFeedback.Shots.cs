using System;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // src: f53864. Record the flight before its pierce sparks and muzzle effects.
        private void Fire(Shot shot)
        {
            int tier = shot.Tier;
            float fromX = shot.X, size = HullSize(tier), fromY;
            tier = shot.Tier;
            Pose pose;
            if (!poses.TryGetValue(shot.FighterId, out pose))
                fromY = OriginalMath.Binary32(598f - OriginalMath.Binary32(size * NoseReach(tier)));
            else
            {
                size = HullSize(shot.Tier);
                Vector2 local;
                if (shot.Rocket)
                    local = new Vector2(OriginalMath.Binary32(size * (shot.X < 270f ? -.26f : .26f)), OriginalMath.Binary32(size * -.18f));
                else if (shot.Tier == 1 && shot.Barrel != 1)
                    local = new Vector2(OriginalMath.Binary32((OriginalMath.Binary32(size * OriginalMath.Binary32((float)unchecked(shot.Barrel - 1)))) * .21f), OriginalMath.Binary32(size * -.3f));
                else local = new Vector2(0f, OriginalMath.Binary32(NoseReach(shot.Tier) * -size));
                Vector2 muzzle = Muzzle(pose, shot.FighterId, local);
                fromX = muzzle.x; fromY = muzzle.y;
                pose.recoil = 1f;
            }
            Vector2 target = ScreenLayout.EnemyPoint(shot.TargetX, shot.TargetY);
            flights.Add(new Flight { from = new Vector2(fromX, fromY), to = target, color = shot.Color, shot = shot, target = shot.TargetIndex });
            if (shot.Pierce > 0)
            {
                float duration = shot.Duration;
                int[] pierced = { shot.PierceA, shot.PierceB, shot.PierceC };
                float dx = OriginalMath.Binary32(fromX - target.x), dy = OriginalMath.Binary32(fromY - target.y);
                float distance = OriginalMath.WasmMax(OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy)))), 1f);
                for (int i = 0; i < pierced.Length; i++)
                {
                    int index = pierced[i];
                    if (index < 0 || index >= battle.Alive.Length) continue;
                    float x = battle.EnemyX(index), y = battle.EnemyY(index);
                    Vector2 at = ScreenLayout.EnemyPoint(x, y);
                    dx = OriginalMath.Binary32(fromX - at.x); dy = OriginalMath.Binary32(fromY - at.y);
                    float fraction = OriginalMath.WasmMin(OriginalMath.Binary32(OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy)))) / distance), 1f);
                    if (sparks.Count >= 48) sparks.RemoveAt(0);
                    sparks.Add(new PierceSpark { at = at, delay = OriginalMath.Binary32(duration * fraction), color = shot.Color });
                    PierceSparks = unchecked(PierceSparks + 1);
                }
            }
            // These labels retain the branch/read order around Pool.Play callbacks.
            if (!shot.Rocket)
            {
                if (shot.Barrel != 0) goto Done;
                if (shot.Tier > 1) goto UpgradeMuzzle;
            }
            particles.Play("PixelMuzzle", new Vector2(fromX, fromY), palette[shot.Color]);
            if (shot.Barrel != 0) goto Done;
        UpgradeMuzzle:
            if ((Fired & 1) == 0) AddMuzzle(new Vector2(fromX, fromY));
        Done:
            Fired = unchecked(Fired + 1);
        }

        // src: f53866. Remove every flight for this exact Shot, back to front.
        private void Resolve(Shot shot)
        {
            for (int i = flights.Count - 1; i >= 0; i--)
                if (ReferenceEquals(flights[i].shot, shot)) flights.RemoveAt(i);
            Vector2 at = ScreenLayout.EnemyPoint(shot.TargetX, shot.TargetY);
            PixelEffectPool target = particles;
            string kind = shot.Rocket ? "PixelRocketImpact" : shot.Tier > 1 ? "PixelLaserHit" : shot.Tier == 1 ? "PixelIonHit" : "PixelHit";
            target.Play(kind, at, palette[shot.Color]);
            if (shot.Rocket)
            {
                if (blasts.Count >= 12) blasts.RemoveAt(0);
                blasts.Add(new Blast { at = at });
                rocketAge = 0f;
            }
            if ((Impacts & 1) == 0 || shot.Rocket || shot.Tier >= 2)
            {
                AddHitMark(at, shot.Color);
                if (shot.Rocket) goto AfterWeapon;
            }
            AddWeaponFx(shot, at);
        AfterWeapon:
            AddGoldSpark(at);
            Impact?.Invoke(shot.Rocket ? 4 : shot.Tier);
            Impacts = unchecked(Impacts + 1);
        }

        // src: f53865. The limit removes one old record, not an arbitrary trim.
        private void AddMuzzle(Vector2 at)
        {
            FleetLook look = Look;
            if (look == null || look.Part(0) < 2) return;
            if (muzzles.Count >= 16) muzzles.RemoveAt(0);
            muzzles.Add(new Vector3(at.x, at.y, 0f));
        }

        // src: f53867. Normal hits stop at the stage limit; fusion marks can evict.
        private void AddHitMark(Vector2 at, int color, bool fusion = false)
        {
            int count = marks.Count;
            if (!fusion)
            {
                FleetLook look = Look;
                int limit = look != null && look.stage > 2 ? 40 : 24;
                if (count >= limit) return;
            }
            if (count >= 56) marks.RemoveAt(0);
            marks.Add(new Mark { at = at, color = color, fusion = fusion });
        }

        // src: f53869. Signed integer remainders follow source i32 wrapping.
        private void AddGoldSpark(Vector2 at)
        {
            FleetLook look = Look;
            if (look == null) return;
            int part = look.Part(3);
            part = part < 5 ? part : 5;
            if (part < 3) return;
            int impacts = Impacts;
            if (impacts % (12 - part) != 0) return;
            if (goldSparks.Count >= 10)
            {
                goldSparks.RemoveAt(0);
                impacts = Impacts;
            }
            float seed = OriginalMath.Binary32(OriginalMath.Binary32((float)(unchecked(impacts * 37) % 100)) / 100f);
            goldSparks.Add(new Vector4(at.x, at.y, 0f, seed));
        }

        // src: f53868. Life table at pristine memory 3647812: .30,.34,.62,.26.
        private void AddWeaponFx(Shot shot, Vector2 at)
        {
            int[] sourceExtras = shot.Extra;
            if (sourceExtras == null || sourceExtras.Length == 0) return;
            Vector2[] extras = new Vector2[sourceExtras.Length];
            for (int i = 0; i < extras.Length; i++)
            {
                // Each native-independent model query re-reads the source array.
                float x = battle.EnemyX(shot.Extra[i]);
                float y = battle.EnemyY(shot.Extra[i]);
                extras[i] = ScreenLayout.EnemyPoint(x, y);
            }
            WeaponProfile weapon = Weapon;
            WeaponKind kind = weapon.kind;
            int form = weapon.Form;
            float life = kind == (WeaponKind)2 ? (form == 3 ? .62f : .4f)
                : kind == (WeaponKind)1 ? .3f : kind == (WeaponKind)3 ? .62f : kind == (WeaponKind)4 ? .26f : .34f;
            if (weaponFx.Count >= 24) weaponFx.RemoveAt(0);
            weaponFx.Add(new WeaponFx { extras = extras, at = at, kind = kind, form = form, color = shot.Color, life = life });
            WeaponBursts = unchecked(WeaponBursts + 1);
            float tokens = particleTokens;
            if (tokens >= 1f)
            {
                particleTokens = OriginalMath.Binary32(tokens + -1f);
                PixelEffectPool target = particles;
                Color[] colors = palette;
                int color = shot.Color;
                Vector2 last = extras[extras.Length - 1];
                Color tint = colors[color];
                string effect = kind == (WeaponKind)3 ? "PixelRocketImpact"
                    : kind == (WeaponKind)2 && form == 3 ? "PixelFusion" : "PixelHit";
                target.Play(effect, last, tint);
            }
            float now = time;
            if (OriginalMath.Binary32(now - weaponCueAt) > .11f)
            {
                weaponCueAt = now;
                Action<string> cue = WeaponCue;
                if (cue != null)
                {
                    string sound = kind == (WeaponKind)1 ? "fx-pierce" : kind == (WeaponKind)2 ? (form == 3 ? "fx-collapse" : "fx-quake")
                        : kind == (WeaponKind)3 ? "fx-burn" : kind == (WeaponKind)4 ? "fx-arc" : "fx-split";
                    cue(sound);
                }
            }
            if (kind == (WeaponKind)2) Kick(form == 3 ? 1.6f : .8f);
        }
    }
}

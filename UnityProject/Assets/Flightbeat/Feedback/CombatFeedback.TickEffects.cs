using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // src: f53872. Fusion marks last longer; expiry is strictly greater-than.
        private void TickMarks(float dt)
        {
            for (int i = marks.Count - 1; i >= 0; i--)
            {
                marks[i].age = OriginalMath.Binary32(marks[i].age + dt);
                if (marks[i].age > (marks[i].fusion ? .7f : .3f)) marks.RemoveAt(i);
            }
        }
        // src: f53873. Token refill precedes weapon effect expiry.
        private void TickWeaponFx(float dt)
        {
            particleTokens = OriginalMath.WasmMin(OriginalMath.Binary32(OriginalMath.Binary32(dt * 10f) + particleTokens), 6f);
            for (int i = weaponFx.Count - 1; i >= 0; i--)
            {
                weaponFx[i].age = OriginalMath.Binary32(weaponFx[i].age + dt);
                if (weaponFx[i].age > weaponFx[i].life) weaponFx.RemoveAt(i);
            }
        }
        // src: f53874. Vector3/Vector4 records store age in z; w is retained.
        private void TickUpgradeFx(float dt)
        {
            for (int i = muzzles.Count - 1; i >= 0; i--)
            {
                Vector3 value = muzzles[i];
                value.z = OriginalMath.Binary32(value.z + dt);
                if (value.z > .09f) muzzles.RemoveAt(i);
                else muzzles[i] = value;
            }
            for (int i = goldSparks.Count - 1; i >= 0; i--)
            {
                Vector4 value = goldSparks[i];
                value.z = OriginalMath.Binary32(value.z + dt);
                if (value.z > .7f) goldSparks.RemoveAt(i);
                else goldSparks[i] = value;
            }
        }
        // src: f53875. Wave expiry uses >=; purge expiry uses >.
        private void TickPressure(float dt)
        {
            rescueAge  =             OriginalMath.Binary32(rescueAge+(dt));
            for (int i = waves.Count - 1; i >= 0; i--)
            {
                waves[i].age = OriginalMath.Binary32(waves[i].age + dt);
                if (waves[i].age >= waves[i].duration) waves.RemoveAt(i);
            }
            for (int i = purges.Count - 1; i >= 0; i--)
            {
                purges[i].age = OriginalMath.Binary32(purges[i].age + dt);
                if (purges[i].age > 1.6f) purges.RemoveAt(i);
            }
        }
        // src: f53878. Set exploded before native effects and the reentrant Rumble callback.
        private void TickSacrifice(float dt)
        {
            for (int i = kamikazes.Count - 1; i >= 0; i--)
            {
                Kamikaze value = kamikazes[i];
                value.age = OriginalMath.Binary32(value.age + dt);
                if (!value.exploded && value.age >= value.delay)
                {
                    value.exploded = true;
                    float x = value.x, y = OriginalMath.Binary32(FrontScreenY + 6f);
                    particles.Play("PixelRocketImpact", new Vector2(x, y), new Color(1f, .55f, .15f, 1f));
                    particles.Play("PixelBurn", new Vector2(x, y), new Color(1f, .35f, .05f, 1f));
                    Kick(3f);
                }
                if (value.age > OriginalMath.Binary32(value.delay + .5f)) kamikazes.RemoveAt(i);
            }
        }
    }
}

using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53889; the model is read from Look and clamped to the five sound/art sets.
        public int Model => Look == null ? 0 : Look.model > 0 ? (Look.model < 4 ? Look.model : 4) : 0;

        // src: f53851/f53852. Slot uses visual bay mapping; Bay uses queue lanes.
        private Vector2 Bay(int lane) => new Vector2(battle.QueuePositionX(lane), Battle.QueueY(0));
        private Vector2 Slot(int slot)
        {
            Battle model = battle;
            return new Vector2(model.FleetPositionX(model.VisualSlot(slot)), 628f);
        }

        // src: f53853. FrontY is deliberately queried again after the first check.
        public float FrontScreenY => battle.FrontY < 0f ? 300f : ScreenLayout.EnemyPoint(0f, battle.FrontY).y;

        // src: f53854. The supplied slot is not used. Particle origin is the Bay,
        // including when the pose starts at a separately recorded reserve position.
        private void Launch(Fighter ship, int slot)
        {
            Vector2 start;
            if (!reservePoses.TryGetValue(ship.Id, out start)) start = Bay(ship.Queue);
            poses[ship.Id] = new Pose { position = start, origin = start, born = time, tier = ship.Tier };
            reservePoses.Remove(ship.Id);
            PixelEffectPool target = particles;
            // f28733 / Vector2 static +0x18 is downVector (0,-1).
            Vector2 at = Bay(ship.Queue) + Vector2.down * 20f;
            Color tint = palette[ship.Color];
            target.Play("PixelLaunch", at, tint);
        }

        // src: f53855. Preserve the existing result pose, including its origin/born.
        private void Fuse(Fighter result, Fighter[] sources, int slot)
        {
            Vector2 at = Slot(slot);
            Pose pose;
            poses.TryGetValue(result.Id, out pose);
            for (int i = 0; i < sources.Length; i++)
            {
                Fighter source = sources[i];
                if (source.Id == result.Id) continue;
                Vector2 from = at;
                Pose prior;
                if (poses.TryGetValue(source.Id, out prior)) from = prior.position;
                transitions.Add(new Transition
                {
                    from = from, to = at, color = source.Color, tier = source.Tier,
                    duration = .22f, spin = from.x < at.x ? -1f : 1f
                });
                poses.Remove(source.Id);
            }
            if (pose == null) pose = new Pose { position = at, origin = at, born = OriginalMath.Binary32(time + -.32f) };
            pose.appear = OriginalMath.Binary32(time + .2f);
            pose.tier = result.Tier;
            poses[result.Id] = pose;
            fusions.Add(new Fusion { at = at, color = result.Color, tier = result.Tier });
            if (result.Tier == 3) maxAge = 0f;
        }

        // src: f53856/f53863/f53907.
        public Vector2 Position(int id, int slot) => poses.TryGetValue(id, out Pose p) ? p.position : Slot(slot);
        public float Heading(int id) => poses.TryGetValue(id, out Pose p) ? p.angle : 0f;
        public Vector2 ReservePosition(Fighter ship, int lane, int row) => reservePoses.TryGetValue(ship.Id, out Vector2 p)
            ? p : new Vector2(battle.QueuePositionX(lane), Battle.QueueY(row));

        // src: f53857/f53858; signed tiers are not clamped.
        public static float HullSize(int tier) => tier > 2 ? 72f : tier == 2 ? 60f : tier == 1 ? 52f : 44f;
        public static float NoseReach(int tier) => tier > 1 ? .5f : tier == 1 ? .375f : .406f;

        // src: f53859, shared Rotate/Rot. WAT constant 0x1.1df46ap-6.
        public static Vector2 Rotate(Vector2 local, float degrees)
        {
            float y = local.y, radians = OriginalMath.Binary32(degrees * .01745329238474369f);
            float cosine = OriginalMath.Cos(radians);
            float x = local.x, sine = OriginalMath.Sin(radians);
            return new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(cosine * x) - OriginalMath.Binary32(sine * y)), OriginalMath.Binary32(OriginalMath.Binary32(x * sine) + OriginalMath.Binary32(cosine * y)));
        }
        private static Vector2 Rot(Vector2 local, float degrees) => Rotate(local, degrees);

        // src: f53860. The explicit +0 is observable for signed zero.
        private Vector2 HullCentre(Pose p, int id)
        {
            float wave = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 4f) + OriginalMath.Binary32((float)id)));
            Vector2 position = p.position;
            float bob = OriginalMath.RoundSingle(OriginalMath.Binary32(wave * 1.5f));
            return new Vector2(OriginalMath.Binary32(position.x + 0f), OriginalMath.Binary32((OriginalMath.Binary32(bob + OriginalMath.Binary32(p.recoil * 5f))) + position.y));
        }

        // src: f53861. WAT groups the rotated offset BEFORE adding to the centre.
        private Vector2 Muzzle(Pose p, int id, Vector2 local)
        {
            float wave = OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(time * 4f) + OriginalMath.Binary32((float)id)));
            Vector2 position = p.position;
            float bob = OriginalMath.RoundSingle(OriginalMath.Binary32(wave * 1.5f));
            float angle = p.angle, recoil = p.recoil;
            float radians = OriginalMath.Binary32(angle * .01745329238474369f);
            float cosine = OriginalMath.Cos(radians), sine = OriginalMath.Sin(radians);
            float y = OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(bob + OriginalMath.Binary32(recoil * 5f))) + position.y)) + (OriginalMath.Binary32(OriginalMath.Binary32(sine * local.x) + OriginalMath.Binary32(cosine * local.y))));
            float x = OriginalMath.Binary32((OriginalMath.Binary32(position.x + 0f)) + (OriginalMath.Binary32(OriginalMath.Binary32(cosine * local.x) - OriginalMath.Binary32(sine * local.y))));
            return new Vector2(x, y);
        }

        // src: f53862. The missing-pose branch retains the source Vector2.down
        // multiplications; the present branch passes a negative local Y to Muzzle.
        public Vector2 Nose(int id, int tier, int slot)
        {
            if (!poses.TryGetValue(id, out Pose p))
            {
                Vector2 at = Slot(slot), down = Vector2.down;
                float reach = NoseReach(tier), size = HullSize(tier);
                return new Vector2(OriginalMath.Binary32(at.x + OriginalMath.Binary32(OriginalMath.Binary32(reach * size) * down.x)), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(reach * size) * down.y) + 628f));
            }
            return Muzzle(p, id, new Vector2(0f, OriginalMath.Binary32(NoseReach(tier) * -HullSize(tier))));
        }

        // src: f53890. Preserve (u+u)*t and both independent coordinate sums.
        private static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t)
        {
            float u = OriginalMath.Binary32(1f - t), aa = OriginalMath.Binary32(u * u), cc = OriginalMath.Binary32((OriginalMath.Binary32(u + u)) * t), bb = OriginalMath.Binary32(t * t);
            return new Vector2(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(aa * a.x) + OriginalMath.Binary32(cc * c.x))) + OriginalMath.Binary32(bb * b.x)), OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(aa * a.y) + OriginalMath.Binary32(cc * c.y))) + OriginalMath.Binary32(bb * b.y)));
        }
        // src: f53837. The WAT product is u*(u*u), not (u*u)*u.
        private static float OutCubicStatic(float t)
        {
            float u = t < 0f ? 1f : OriginalMath.Binary32(1f - OriginalMath.WasmMin(t, 1f));
            return OriginalMath.Binary32(1f - OriginalMath.Binary32(u * (OriginalMath.Binary32(u * u))));
        }
    }
}

using Flightbeat.Economy;
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53884. Muzzle vectors store x/y/age; gold vectors store x/y/age/side.
        private void DrawUpgradeFx(PixelSurface s)
        {
            int nose = Look == null ? 1 : Look.Part(0);
            nose = nose < 5 ? nose : 5; nose = nose > 1 ? nose : 1;
            float baseSize = Look == null ? 6.5f : OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)nose) * 2.5f) + 4f);
            Color tint = Mix(FleetCatalog.FighterAccent[Model], Color.white, .6f);
            foreach (Vector3 muzzle in muzzles)
            {
                float t = OriginalMath.Binary32(muzzle.z / .09f), fade = OriginalMath.Binary32(1f - t), alpha = OriginalMath.Binary32(fade * .8f);
                float size = OriginalMath.Binary32(baseSize * (OriginalMath.Binary32(OriginalMath.Binary32(t * -.5f) + 1f)));
                s.Segment(new Vector2(OriginalMath.Binary32(muzzle.x - size), muzzle.y), new Vector2(OriginalMath.Binary32(muzzle.x + size), OriginalMath.Binary32(muzzle.y + 0f)), 1.5f,
                    Alpha(tint, OriginalMath.Binary32(tint.a * alpha)));
                s.Segment(new Vector2(muzzle.x, OriginalMath.Binary32(muzzle.y + OriginalMath.Binary32(size * -1.2f))), new Vector2(OriginalMath.Binary32(muzzle.x + 0f), OriginalMath.Binary32(muzzle.y + OriginalMath.Binary32(size * .5f))), 1.5f,
                    new Color(1f, 1f, 1f, alpha));
                s.Glow(muzzle.x, muzzle.y, OriginalMath.Binary32(size + size), OriginalMath.Binary32(size + size), Alpha(tint, OriginalMath.Binary32(tint.a * (OriginalMath.Binary32(fade * .3f)))), 0f);
            }
            foreach (Vector4 spark in goldSparks)
            {
                float t = OriginalMath.Binary32(spark.z / .7f), fade = OriginalMath.Binary32(1f - t);
                float x = OriginalMath.Binary32(OriginalMath.Binary32(spark.x + OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(OriginalMath.Binary32(t * 6f) + OriginalMath.Binary32(spark.w * 6f))) * 8f)) + OriginalMath.Binary32(spark.w * (OriginalMath.Binary32(t * -30f))));
                float y = OriginalMath.Binary32(spark.y + OriginalMath.Binary32(t * -70f));
                s.Glow(x, y, 10f, 10f, SourceColor(4564848585086074880UL, 1053609165u, OriginalMath.Binary32(fade * .4f)), 0f);
                float size = OriginalMath.Binary32((OriginalMath.Binary32(1f - OriginalMath.Binary32(t * .4f))) * 3.5f), half = OriginalMath.Binary32(size * .5f);
                s.Rect(OriginalMath.Binary32(x - half), OriginalMath.Binary32(y - half), size, size, SourceColor(4569892616153333760UL, 1057803469u, OriginalMath.Binary32(fade * .85f)));
            }
        }
    }
}

using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53915: six staggered rescue missiles, impacts and the purge count.
        private void DrawPurge(PixelSurface s)
        {
            foreach (PurgeFx purge in purges)
            {
                for (int i = 0; i < 6; i++)
                {
                    float x = OriginalMath.Binary32((float)(i * 88 + 50)), t = OriginalMath.Binary32((OriginalMath.Binary32(purge.age + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * -.04f))) * 4f);
                    t = t < 0f ? 0f : OriginalMath.WasmMin(t, 1f);
                    if (t < 1f)
                    {
                        float y = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.WasmMin(OriginalMath.Binary32(t * t), 1f) * (OriginalMath.Binary32(purge.front + -120f))) + 120f);
                        Vector2 at = new Vector2(x, y);
                        s.GlowLine(at, new Vector2(x, OriginalMath.Binary32(y + -60f)), 16f, SourceColor(4575657222462033101UL, 4554039943402566451UL));
                        s.SpriteAt(10, at, 26f, Color.white, 180f);
                    }
                    float ring = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(purge.age + -.25f) + OriginalMath.Binary32(OriginalMath.Binary32((float)i) * -.06f))) / .6f);
                    if (!(ring > 0f && ring < 1f)) continue;
                    float fade = OriginalMath.Binary32(1f - ring);
                    s.Glow(x, purge.front, OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(ring * -.4f) + 1f)) * 160f), 110f,
                        SourceColor(4575657222462033101UL, 1063675494u, OriginalMath.Binary32(fade * .55f)), 0f);
                    s.Ring(new Vector2(x, purge.front), OriginalMath.Binary32(OriginalMath.Binary32(ring * 70f) + 10f),
                        SourceColor(4575657222470421709UL, 1065353216u, fade), 3f, 20, 0f, 360f);
                }
                float age = purge.age;
                if (!(age > .3f && age < 1.5f)) continue;
                string title = "援军清场 ×" + purge.count.ToString();
                float alpha = OriginalMath.Binary32((OriginalMath.Binary32(1.5f - age)) / .4f);
                alpha = alpha < 0f ? 0f : OriginalMath.WasmMin(alpha, 1f);
                float t2 = OriginalMath.Binary32((OriginalMath.Binary32(age + -.3f)) * 4f);
                float q = t2 < 0f ? 1f : OriginalMath.Binary32(1f - OriginalMath.WasmMin(t2, 1f));
                float scale = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(q * (OriginalMath.Binary32(q * q))) + -1f) + 1f)) * 1.2f) + 1.8f);
                s.TextF(title, 272f, OriginalMath.Binary32(OriginalMath.Binary32(purge.front + -72f) + 2f), scale, new Color(0f, 0f, 0f, OriginalMath.Binary32(alpha * .8f)), 0);
                s.TextF(title, 270f, OriginalMath.Binary32(purge.front + -72f), scale, SourceColor(4575657222468743987UL, 1064514355u, alpha), 0);
            }
        }
    }
}

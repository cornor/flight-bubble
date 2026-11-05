#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Flightbeat.Game;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Garden
{
    // Presentation only: consume existing ages/positions; never tick or mutate Battle.
    public static class GardenFx
    {
        [Serializable] public sealed class Frame { public float x, y, w, h; }
        [Serializable] public sealed class Clip { public string id; public Frame[] frames; [NonSerialized] public Rect[] uv; }
        [Serializable] public sealed class Atlas { public int size; public Clip[] clips; }
        static readonly Dictionary<string, Clip> clips = new Dictionary<string, Clip>();
        static bool loaded;
        public static Clip Get(string id)
        {
            if (!loaded) {
                var json = Resources.Load<TextAsset>("GardenR1/fx-atlas");
                if (!json) throw new InvalidOperationException("Missing GardenR1/fx-atlas");
                var data = JsonUtility.FromJson<Atlas>(json.text);
                foreach (var clip in data.clips) {
                    clip.uv = new Rect[clip.frames.Length];
                    for (int i = 0; i < clip.frames.Length; i++) {
                        var f = clip.frames[i];
                        clip.uv[i] = new Rect(f.x / data.size, 1f - (f.y + f.h) / data.size, f.w / data.size, f.h / data.size);
                    }
                    clips.Add(clip.id, clip);
                }
                loaded = true;
            }
            if (!clips.TryGetValue(id, out var result)) throw new InvalidOperationException("Missing garden effect: " + id);
            return result;
        }
        // Optional clips (third batch): lets callers fall back to the original drawing without per-frame exceptions.
        public static bool Has(string id) {
            if (!loaded) try { Get(id); } catch (InvalidOperationException) { }
            return clips.ContainsKey(id);
        }
        public static void Draw(PixelSurface s, string id, Vector2 center, float width, float height, Color tint, int frame = 0, float angle = 0f)
        {
            var clip = Get(id);
            s.GardenFxQuad(center.x - width * .5f, center.y - height * .5f, width, height,
                clip.uv[Mathf.Clamp(frame, 0, clip.uv.Length - 1)], tint, angle);
        }
        // Burst with a tint (2026-10-21: bead-coloured foam pops).
        public static void BurstTint(PixelSurface s, string id, Vector2 at, float size, float age, float duration, Color tint)
        {
            if (age < 0f || age >= duration) return;
            var clip = Get(id);
            Draw(s, id, at, size, size, tint, Mathf.Min(clip.frames.Length - 1, (int)(age / duration * clip.frames.Length)));
        }
        public static void Burst(PixelSurface s, string id, Vector2 at, float size, float age, float duration)
        {
            if (age < 0f || age >= duration) return;
            var clip = Get(id);
            Draw(s, id, at, size, size, Color.white, Mathf.Min(clip.frames.Length - 1, (int)(age / duration * clip.frames.Length)));
        }
        // 2026-11-05 (user: the white puffs look fake; a see-through soap bubble pop, where the spent member hits the
        // bubble field): the home screen's soap bubble flashes and is gone, its rim spreads out once, fine droplets in
        // the member's colour fly off, and a few small soap bubbles spill out. Everything is over by .5 s. False when the
        // soap bubble art is missing (callers keep their old effect).
        public static bool SoapPop(PixelSurface s, Vector2 c, float radius, Color tint, float age, int seed)
        {
            if (!GardenSkin.Has(GardenSkin.SoapBubble)) return false;
            if (age < 0f || age >= .5f) return true;
            Color sheen = Color.Lerp(Color.white, tint, .3f), drop = Color.Lerp(tint, Color.white, .25f);
            if (age < .05f)
            {
                float d = radius * (2f + .16f * age / .05f);
                GardenSkin.Asset(s, GardenSkin.SoapBubble, c.x - d * .5f, c.y - d * .5f, d, d, new Color(sheen.r, sheen.g, sheen.b, .9f * (1f - age / .05f)));
            }
            if (age < .12f)
            {
                float q = age / .12f;
                s.Ring(c, radius * (1f + .3f * q), new Color(1f, 1f, 1f, .9f * (1f - q)), 3f * (1f - q) + .5f, 28, 0f, 360f);
            }
            for (int k = 0; k < 18; k++)
            {
                float h1 = Hash(seed * 31 + k), h2 = Hash(seed * 37 + k), h3 = Hash(seed * 41 + k);
                float life = .3f + .15f * h3;
                if (age >= life) continue;
                float ang = (k + .6f * h1) / 18f * Mathf.PI * 2f, speed = 160f + 120f * h2;
                Vector2 p = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (radius * .95f + speed * age) + new Vector2(0f, 380f * age * age);
                float a = 1f - age / life, d = 3f + 2.5f * h1;
                s.Glow(p.x, p.y, d * 2.4f, d * 2.4f, new Color(drop.r, drop.g, drop.b, .75f * a), 0f);
                s.Glow(p.x, p.y, d * .9f, d * .9f, new Color(1f, 1f, 1f, .95f * a), 0f);
            }
            for (int k = 0; k < 4; k++)   // small soap bubbles spilling out below and to the sides of the field's edge
            {
                float h1 = Hash(seed * 43 + k), h2 = Hash(seed * 47 + k), h3 = Hash(seed * 53 + k);
                float ang = Mathf.Lerp(15f, 165f, (k + .5f * h1) / 4f) * Mathf.Deg2Rad, speed = 55f + 35f * h2;
                float travel = speed * age * (1f - age);   // slowing down
                Vector2 p = c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (radius * .9f + travel) + new Vector2(4f * Mathf.Sin(age * 10f + k), 0f);
                float d = radius * (.32f + .22f * h3) * Mathf.Clamp01(age / .06f);
                float a = .9f * Mathf.Clamp01((.5f - age) / .15f);
                GardenSkin.Asset(s, GardenSkin.SoapBubble, p.x - d * .5f, p.y - d * .5f, d, d, new Color(sheen.r, sheen.g, sheen.b, a));
            }
            return true;
        }
        static float Hash(int n) { float v = Mathf.Sin(n * 12.9898f) * 43758.5453f; return v - Mathf.Floor(v); }
        public static void Beam(PixelSurface s, Vector2 from, Vector2 to, float width, Color tint, float age)
        {
            bool mapped = s.MapLayout;
            if (mapped) { from = PlushLayout.Point(from); to = PlushLayout.Point(to); s.MapLayout = false; }
            try {
                var delta = to - from; float length = delta.magnitude;
                if (length <= .001f) return;
                var direction = delta / length;
                float angle = Mathf.Atan2(delta.x, -delta.y) * Mathf.Rad2Deg;
                var uv = Get("beam").uv[0];
                // Whole 128px tiles plus a cropped tail; map endpoints before rotating on tall screens.
                for (float distance = 0; distance < length; distance += 128f) {
                    float h = Mathf.Min(128f, length - distance);
                    var center = from + direction * (distance + h * .5f);
                    var part = new Rect(uv.x, uv.y, uv.width, uv.height * h / 128f);
                    s.GardenFxQuad(center.x - width * .5f, center.y - h * .5f, width, h, part, tint, angle);
                }
                int pulse = (int)(age * 18f) % 6;
                Draw(s, "beam-tip", to, 20, 20, tint, pulse < 4 ? pulse : 6 - pulse);
            } finally { s.MapLayout = mapped; }
        }
        // v3 (2026-10-15): high-tier stream drawn as a straight line of glossy bubbles in the shooter's color, flowing
        // from the creature to the target, with a pulsing bubble cluster at the end. Replaces the tiled "beam" strip,
        // whose wavy art did not join between tiles (it read as a tangled line) and turned muddy when tinted.
        // Endpoints and timing are the existing BeamX/BeamY/BeamLife; nothing here touches the model.
        // Size factor of a shot `distance` px from the mouth: a quarter at the lips, full size once it has cleared the
        // head (about 34 px above the mouth on a pad), with a smooth ease between.
        public static float MouthGrow(float distance)
        {
            float u = Mathf.Clamp01(distance / 34f);
            return .25f + .75f * u * u * (3f - 2f * u);
        }
        // 2026-11-01 (user: with Ⅱ 3 / MAX 4 streams the solid part still read as one thick band): `fan` bows a stream
        // sideways (a quadratic bow across the from-to line, on the flower-ring curve or the straight one) so the streams of one
        // member spread apart in the air and meet again at their targets.
        static Vector2 StreamAt(Vector2 from, Vector2 to, float u, bool blown, float fan = 0f)
        {
            Vector2 p = blown ? GardenSkin.BlownCurve(from, to, u, 0f) : Vector2.Lerp(from, to, u);
            if (fan == 0f) return p;
            Vector2 d = to - from; float len = d.magnitude;
            return len < .001f ? p : p + new Vector2(-d.y, d.x) / len * (fan * 2f * u * (1f - u));
        }
        // 2026-10-31 12:30 (user: Ⅰ's several bubbles flying together felt good; Ⅱ and MAX's single stream had less
        // atmosphere). Presentation only, on top of the one stream: a soft glow along it (MAX also a white core), side
        // strands of smaller bubbles braiding round it - Ⅱ two, MAX four (an inner and an outer pair) - meeting again
        // at the mouth and the target, gold glints riding along, and a fuller splash where it lands (MAX: a pulsing light).
        static void StreamExtras(PixelSurface s, Vector2 from, Vector2 to, float length, float width, Color tint, float age, bool blown, int tier, float fan, bool multi, bool lead)
        {
            bool max = tier >= 3;
            Color g = Color.Lerp(tint, Color.white, .55f); g.a = multi ? (max ? .18f : .13f) : max ? .3f : .2f;
            float gw = width * (multi ? 1.2f : max ? 2.4f : 1.7f);
            int segs = blown || fan != 0f ? 10 : 1;
            Vector2 prev = StreamAt(from, to, Mathf.Min(1f, 22f / length), blown, fan);
            for (int i = 1; i <= segs; i++)
            {
                Vector2 p = StreamAt(from, to, (float)i / segs, blown, fan);
                s.GlowLine(prev, p, gw, g);
                if (max && !multi) s.GlowLine(prev, p, 4f, new Color(1f, 1f, 1f, .32f));
                prev = p;
            }
            int strands = multi ? 0 : max ? 4 : 2;   // the braided strands stood in for several streams; not with real ones
            float sstep = width * .62f * 1.25f, flow = (age * 150f) % sstep;
            Color side = max ? Color.Lerp(tint, Color.white, .25f) : tint;
            for (int j = 0; j < strands; j++)
            {
                bool outer = j >= 2;
                float amp = outer ? 15f : 8f, freq = outer ? .035f : .05f, phase = j * Mathf.PI + (outer ? Mathf.PI * .5f : 0f);
                int n = 0;
                for (float d = 24f + flow + j * 3f; d < length - 6f; d += sstep, n++)
                {
                    float u = d / length;
                    Vector2 c = StreamAt(from, to, u, blown, fan), c2 = StreamAt(from, to, Mathf.Min(1f, u + .01f), blown, fan);
                    Vector2 tan = (c2 - c).sqrMagnitude > 1e-6f ? (c2 - c).normalized : (to - from) / length, nrm = new Vector2(-tan.y, tan.x);
                    float env = Mathf.Clamp01((d - 20f) / 50f) * Mathf.Clamp01((length - d) / 60f);
                    Vector2 at = c + nrm * (amp * env * Mathf.Sin(d * freq - age * 8f + phase));
                    if (blown && (at - from).magnitude < GardenSkin.BlownCondense) continue;
                    float size = width * (outer ? .3f : .36f) * (.85f + .15f * Mathf.Sin(n * 2.1f + j));
                    GardenSkin.Shot(s, side, at, size * MouthGrow(d), outer ? .8f : .92f);
                }
            }
            if (GardenSkin.Has("scene/revive-sparkle"))
            {
                int glints = multi ? (max ? 2 : 1) : max ? 6 : 3;
                for (int i = 0; i < glints; i++)
                {
                    float d = (age * 170f + i * length / glints) % length;
                    if (d < 30f) continue;
                    Vector2 c = StreamAt(from, to, d / length, blown, fan) + new Vector2(Mathf.Sin(i * 2.7f) * 12f, Mathf.Cos(i * 1.9f) * 6f);
                    float tw = .5f + .5f * Mathf.Sin(age * 9f + i * 1.7f), sz = (max ? 16f : 12f) * (.6f + .4f * tw);
                    GardenSkin.Asset(s, "scene/revive-sparkle", c.x - sz * .5f, c.y - sz * .5f, sz, sz, new Color(1f, 1f, 1f, .85f * tw * Mathf.Clamp01((length - d) / 40f)));
                }
            }
            if (max && lead)
            {
                Color pulse = Color.Lerp(tint, Color.white, .6f); pulse.a = .3f + .15f * Mathf.Sin(age * 10f);
                s.Glow(to.x, to.y, width * 3.2f, width * 3.2f, pulse, 0f);
            }
        }
        public static void BubbleStream(PixelSurface s, Vector2 from, Vector2 to, float width, Color tint, float age, bool blown = false, int tier = 2, float fan = 0f, bool multi = false, bool lead = true)
        {
            var delta = to - from; float length = delta.magnitude;
            if (length <= .001f) return;
            var dir = delta / length;
            if (tier >= 2) StreamExtras(s, from, to, length, width, tint, age, blown, tier, fan, multi, lead);
            float step = width * .62f, flow = (age * 140f) % step;
            // 2026-10-21 10:39 (user: balls in the stream kept shrinking and growing). The size pattern was indexed from the
            // first ball, so every time a new ball entered at the mouth all the others shifted pattern; it now follows the
            // ball (index counted against the flow), so each ball keeps its size all the way.
            int n = -Mathf.FloorToInt(age * 140f / step);
            // 2026-10-18 23:12 (user: the Ⅱ / MAX stream started too far from the member). It used to skip the first
            // 30 px (the whole head) so no bubble sat on the body; now it leaves the mouth like the Ⅰ shots do.
            // 2026-10-21 (user: small at the mouth so the face stays visible): from 2 px out, sized by MouthGrow.
            const float head = 2f;
            for (float d = head + flow; d < length; d += step, n++) {
                int m = ((n % 3) + 3) % 3;
                float size = width * (m == 0 ? .62f : m == 1 ? .48f : .55f);
                // 2026-10-28 (user): from the flower ring, soap bubbles turning into the solid balls (GardenSkin.BlownShot).
                if (blown) {
                    Vector2 at = StreamAt(from, to, d / length, true, fan);   // rises first, then bends to the target
                    // 2026-10-29 14:36 (user: the clear-to-solid joint of the tier-2 stream looked off, tiers 0/1 fine). The
                    // balls follow each other ~11 px apart, so the single shot's look (1.7x bubble, a 14 px blink and a
                    // white glint) became a fixed seam where a ring flashed on every ball and the big bubbles piled up.
                    // In the stream: smaller bubbles (1.3x), a longer soft change (30 px) with no glint, and each ball
                    // condenses a little earlier or later (+-10 px) so there is no hard line.
                    float jitter = (Mathf.Abs(Mathf.Sin(n * 12.9898f) * 43758.5453f) % 1f - .5f) * 20f;
                    GardenSkin.BlownShot(s, tint, at, size * 1.3f, size, (at - from).magnitude, 1f, false,
                        GardenSkin.BlownCondense - 8f + jitter, 30f);
                }
                else GardenSkin.Shot(s, tint, fan == 0f ? from + dir * d : StreamAt(from, to, d / length, false, fan), size * MouthGrow(d), 1f);
            }
            // 2026-10-19 09:34 (user picked C): no big pulsing ball at the hit point any more; a small splash keeps
            // spraying there instead: 7 droplets in the stream's color fly out in a ring (each with its own cycle, a
            // fresh angle every cycle, a little gravity), shrinking and fading as they go.
            int drops = multi ? 5 : tier >= 3 ? 12 : tier == 2 ? 9 : 7;   // 2026-10-31: a fuller splash for Ⅱ and MAX
            for (int k = 0; k < drops; k++) {
                float cycle = age * 2.6f + k / (float)drops, u = cycle - Mathf.Floor(cycle);
                float seed = Mathf.Floor(cycle) * 1.618f + k * .9f;
                float a = (k / (float)drops) * Mathf.PI * 2f + Mathf.Sin(seed * 12.9898f) * .5f;
                float reach = width * (.35f + 1.25f * u) * (.85f + .3f * Mathf.Abs(Mathf.Sin(seed * 7.3f)));
                Vector2 p = to + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * .8f) * reach + new Vector2(0f, width * 1.1f * u * u);
                GardenSkin.Shot(s, tint, p, width * (.36f - .2f * u), 1f - u * u);
            }
        }
        public static void Aim(PixelSurface s, Vector2 at, float radius, float alpha = 1f)
        {
            // The authored ellipse is a visual marker; targeting and collision radius remain in Battle.
            Draw(s, "water-aim", at, radius * 2f, radius * 1.25f, new Color(1, 1, 1, alpha));
        }
    }
}
#endif

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
        public static void Burst(PixelSurface s, string id, Vector2 at, float size, float age, float duration)
        {
            if (age < 0f || age >= duration) return;
            var clip = Get(id);
            Draw(s, id, at, size, size, Color.white, Mathf.Min(clip.frames.Length - 1, (int)(age / duration * clip.frames.Length)));
        }
        public static void Beam(PixelSurface s, Vector2 from, Vector2 to, float width, Color tint, float age)
        {
            bool mapped = s.MapLayout;
            if (mapped) { from = ScreenLayout.Point(from); to = ScreenLayout.Point(to); s.MapLayout = false; }
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
        public static void BubbleStream(PixelSurface s, Vector2 from, Vector2 to, float width, Color tint, float age)
        {
            var delta = to - from; float length = delta.magnitude;
            if (length <= .001f) return;
            var dir = delta / length;
            float step = width * .62f, flow = (age * 140f) % step;
            int n = 0;
            // 2026-10-18 23:12 (user: the Ⅱ / MAX stream started too far from the member). It used to skip the first
            // 30 px (the whole head) so no bubble sat on the body; now it leaves the mouth like the Ⅰ shots do: the
            // balls start 6 px out and grow to full size over the next 16 px.
            const float head = 6f;
            for (float d = head + flow; d < length; d += step, n++) {
                float size = width * ((n % 3 == 0) ? .62f : (n % 3 == 1) ? .48f : .55f);
                float grow = Mathf.Clamp01((d - head) / 16f);               // solid balls: grow in instead of fading in
                GardenSkin.Shot(s, tint, from + dir * d, size * grow, 1f);
            }
            // 2026-10-19 09:34 (user picked C): no big pulsing ball at the hit point any more; a small splash keeps
            // spraying there instead: 7 droplets in the stream's color fly out in a ring (each with its own cycle, a
            // fresh angle every cycle, a little gravity), shrinking and fading as they go.
            for (int k = 0; k < 7; k++) {
                float cycle = age * 2.6f + k / 7f, u = cycle - Mathf.Floor(cycle);
                float seed = Mathf.Floor(cycle) * 1.618f + k * .9f;
                float a = (k / 7f) * Mathf.PI * 2f + Mathf.Sin(seed * 12.9898f) * .5f;
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

#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Garden
{
    public static partial class GardenSkin
    {
        // 2026-10-28 (user: use concept B 花朵泡泡环 as the battle look; Art/PlushToy/HeadBubbleRing-20261027).
        // A felt flower grows from the crown of every member on a pad; its centre is a bubble wand. While the member
        // fires, a bubble of its own colour swells out of the ring and pops off, and the shots leave from the ring
        // (LastMouth). The member itself keeps its original short body, pot and frames. Drawing only: no battle data.
        // Trial switch: false = the previous look (PadCombat body poses).
        public static bool FlowerRing = true;
        // Size against the concept (1 = the flower's width to the koala's head exactly as in concept B).
        public const float FlowerRingSize = 1f;
        public struct PadFlowerPose
        {
            public bool enabled;
            public float sway;      // degrees, + = clockwise on screen, about the stem's foot on the crown
            public float squash;    // + = pressed down and wider (recoil), - = stretched up
            public float bubble;    // 0..1 a bubble swelling out of the ring; < 0 none
            public float pop;       // 0..1 the bubble leaving upwards; < 0 none
        }
        // One-shot for the next Creature() call, like PadCombat; set by the pads.
        public static PadFlowerPose PadFlower;
        [Serializable] sealed class FlowerCrown { public string hex; public float x, y; }
        [Serializable] sealed class FlowerData { public string sprite; public float w, h, scale, stemX, cx, cy, r, stemCut; public FlowerCrown[] crowns; }
        static FlowerData flowerData;
        static Dictionary<string, Vector2> flowerCrowns;
        static bool TryFlowerCrown(Color tint, out Vector2 crown)
        {
            crown = default;
            if (flowerCrowns == null)
            {
                flowerCrowns = new Dictionary<string, Vector2>();
                var resource = Resources.Load<TextAsset>("GardenR1/flower-ring");
                if (resource != null)
                {
                    flowerData = JsonUtility.FromJson<FlowerData>(resource.text);
                    if (flowerData?.crowns != null)
                        foreach (var c in flowerData.crowns) flowerCrowns[c.hex] = new Vector2(c.x, c.y);
                }
            }
            return flowerData != null && Has(flowerData.sprite) && flowerCrowns.TryGetValue(HexKey(tint), out crown);
        }
        public static bool HasFlowerRing(Color tint) => FlowerRing && TryFlowerCrown(tint, out _);
        // Rest, waiting on the pad: a slow sway; the soap film in the ring is part of the sprite.
        public static PadFlowerPose FlowerRest(float seconds, float phase)
        {
            return new PadFlowerPose { enabled = true, bubble = -1f, pop = -1f,
                sway = 3f * Mathf.Sin(seconds * 1.7f + phase) };
        }
        // Firing, one beat (`beat` 0..1, ~.36 s): the bubble swells for 70 %, then pops off with a recoil.
        public static PadFlowerPose FlowerBlow(float beat, float seconds, float phase)
        {
            var p = new PadFlowerPose { enabled = true, bubble = -1f, pop = -1f };
            if (beat < .7f)
            {
                float g = beat / .7f;
                p.bubble = g;
                p.squash = -.06f * g;                                           // reaching up as it swells
                p.sway = 1.2f * Mathf.Sin(seconds * 9f + phase);
            }
            else
            {
                float q = (beat - .7f) / .3f;
                p.pop = q;
                p.squash = .16f * Mathf.Sin(q * Mathf.PI) * (1f - .5f * q);      // kicked down, springs back
                p.sway = 3f * Mathf.Sin(q * Mathf.PI) * (phase > 0f ? 1f : -1f);
            }
            return p;
        }
        public static PadFlowerPose FlowerSpent()
        {
            return new PadFlowerPose { enabled = true, bubble = -1f, pop = -1f, sway = 14f, squash = .05f };
        }
        // Drawn before the body, so the stem's foot hides behind the head. `pivot`/`lean`: the ready lean about the
        // feet. Returns the ring centre and the flower top (screen).
        static bool DrawFlowerStem(PixelSurface s, Color tint, PadFlowerPose pose, float ax, float ay, float aw, float ah,
            Vector2 pivot, float lean, out Vector2 ring, out float ringR, out float flowerTop)
        {
            ring = default; ringR = 0f; flowerTop = ay;
            if (!TryFlowerCrown(tint, out var crownC) || !slots.TryGetValue(flowerData.sprite, out var slot)) return false;
            var d = flowerData;
            Vector2 crown = new Vector2(ax + aw * crownC.x / 224f, ay + ah * crownC.y / 256f);
            if (Mathf.Abs(lean) > .05f) crown = Rotate(crown, pivot, lean);
            float k = d.scale * aw / 224f * FlowerRingSize;                     // screen px per sprite px
            float sy = 1f - pose.squash, sx = 1f + pose.squash * .5f;
            float y0 = slot.oy;
            // The stem bends: the foot follows the crown (and the lean), the sway grows towards the flower head.
            Vector2 P(float x, float y)
            {
                float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d.stemCut - y) / Mathf.Max(1f, d.stemCut - d.cy)));
                float ang = (lean + pose.sway * w) * Mathf.Deg2Rad, c = Mathf.Cos(ang), n = Mathf.Sin(ang);
                float dx = (x - d.stemX) * k * sx, dy = (y - d.stemCut) * k * sy;
                return crown + new Vector2(dx * c - dy * n, dx * n + dy * c);
            }
            Rect uv = Uv(slot, Data.size);
            float x0 = slot.ox, x1 = slot.ox + slot.w, y1 = slot.oy + slot.h;
            // Strips, so the bend reads as a curve.
            const int bands = 6;
            for (int i = 0; i < bands; i++)
            {
                float a = Mathf.Lerp(y0, y1, i / (float)bands), b = Mathf.Lerp(y0, y1, (i + 1f) / bands);
                Rect rows = new Rect(uv.x, uv.yMax - (b - y0) / slot.h * uv.height, uv.width, (b - a) / slot.h * uv.height);
                s.GardenQuadCorners(P(x0, a), P(x1, a), P(x1, b), P(x0, b), rows, Color.white, slot.atlas);
            }
            ring = P(d.cx, d.cy);
            ringR = d.r * k;
            flowerTop = Mathf.Min(P(x0, y0).y, P(x1, y0).y);
            return true;
        }
        // 2026-10-28 10:40 (user: show the blowing; a shot leaves the ring as a rainbow soap bubble like the concept and
        // turns into the solid shot of the member's colour as it flies). `k` 0 = soap bubble (a hint of the colour,
        // so the colour reads from the start), 1 = the solid bead; the size goes from `soapSize` to `shotSize`.
        public const string SoapBubble = "plants/home/soap-bubble";
        public const float BlownSoapScale = 1.7f;                             // soap bubble size / shot size at the ring
        // 10:45 (user: pale like the concept, not deep): the film is only faintly tinted (12 %) and see-through.
        // 11:34 (user: the clear-to-solid change looked unnatural; preview/morph-options.png). 11:47 the user picked C:
        // the bubble stays a clear soap bubble for ~58 px, then condenses in a blink (~14 px): the film shrinks onto
        // the shot size taking the colour (12 % -> 72 %) and fades, the solid ball swells from half size with a small
        // overshoot, and a thin white glint ring flicks out. `popFx` false: no glint (trailing images).
        public const float BlownCondense = 58f, BlownCondenseSpan = 14f;
        // 2026-10-29 14:36 (user: in the tier-2 stream the clear-to-solid joint looks off; tiers 0/1 are fine): the stream
        // passes its own condense point and span (see GardenFx.BubbleStream).
        public static void BlownShot(PixelSurface s, Color tint, Vector2 at, float soapSize, float shotSize, float dist, float alpha, bool popFx = true,
            float condenseAt = BlownCondense, float condenseSpan = BlownCondenseSpan)
        {
            float u = (dist - condenseAt) / condenseSpan;
            if (u >= 1f) { Shot(s, tint, at, shotSize, alpha); return; }
            float e = u <= 0f ? 0f : u * u * (3f - 2f * u), mix = .12f + .6f * e;
            float size = Mathf.Lerp(soapSize, shotSize, e);
            var film = new Color(Mathf.Lerp(1f, tint.r, mix), Mathf.Lerp(1f, tint.g, mix), Mathf.Lerp(1f, tint.b, mix), alpha * .78f * (1f - e));
            if (!Asset(s, SoapBubble, at.x - size * .5f, at.y - size * .5f, size, size, film)) { Shot(s, tint, at, shotSize, alpha); return; }
            if (u <= 0f) return;
            Shot(s, tint, at, shotSize * (.5f + .5f * e) * (1f + .15f * Mathf.Sin(e * Mathf.PI)), alpha * e);
            if (popFx) s.Ring(at, shotSize * (.5f + .5f * u), new Color(1f, 1f, 1f, .85f * alpha * (1f - u)), .6f + 1.6f * (1f - u), 16, 0f, 360f);
        }
        // In front: the clear soap bubble swelling out of the ring, and the one popping off.
        static void DrawFlowerBubble(PixelSurface s, Color tint, PadFlowerPose pose, Vector2 ring, float ringR)
        {
            if (pose.bubble >= 0f)
            {
                float g = pose.bubble, e = 1f - (1f - g) * (1f - g);
                float r = ringR * (.5f + 1.1f * e);
                Vector2 c = ring + new Vector2(0f, -r * .9f * e);
                BlownShot(s, tint, c, r * 2f, r * 2f / BlownSoapScale, 0f, .7f + .3f * g);
            }
            if (pose.pop >= 0f)
            {
                float q = pose.pop, r = ringR * 1.6f;
                Vector2 c = ring + new Vector2(0f, -r * .9f - ringR * 3.5f * q);
                BlownShot(s, tint, c, r * 2f, r * 2f / BlownSoapScale, q * BlownCondense * .6f, 1f - q * q);
            }
        }
        // 10:45 (user: like the concept, the bubble first floats up and forward, then turns to its target). A quadratic
        // curve whose control point is straight above the ring (at most 110 px, 55 % of the way), so the bubble
        // rises first and bends towards the target; `wobble` shifts it sideways a little. Same start, end and duration
        // as the battle's straight flight: drawing only.
        public static Vector2 BlownCurve(Vector2 from, Vector2 to, float t, float wobble)
        {
            float rise = Mathf.Min(110f, .55f * (to - from).magnitude), u = 1f - t;
            Vector2 c = from + new Vector2(wobble, -rise);
            return from * (u * u) + c * (2f * u * t) + to * (t * t);
        }
        // A shot floats out slowly and speeds up (start at ~.55x the mean speed); still arrives at t = 1.
        public static float BlownEase(float t) => t * (.55f + .45f * t);
        static Vector2 Rotate(Vector2 p, Vector2 about, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad, c = Mathf.Cos(r), n = Mathf.Sin(r), x = p.x - about.x, y = p.y - about.y;
            return new Vector2(about.x + x * c - y * n, about.y + x * n + y * c);
        }
    }
}
#endif

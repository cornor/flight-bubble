#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System;
using System.Collections.Generic;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Garden
{
    public static partial class GardenSkin
    {
        // One breath drives a burst of shots. Presentation only; no battle timing is written.
        public struct PadCombatPose
        {
            public bool enabled;
            public int from, to;
            public float blend, lift, sway, ring;
        }
        public static PadCombatPose PadCombat;
        public const float PadCombatPeriod = .83f;
        [Serializable] sealed class CombatFrame { public string state; public float mouthX, mouthY, top; }
        [Serializable] sealed class CombatEntry { public string hex; public float fit; public CombatFrame[] frames; }
        [Serializable] sealed class CombatCatalog { public CombatEntry[] entries; }
        static Dictionary<string, CombatEntry> combatEntries;
        static CombatEntry CombatArt(Color tint)
        {
            if (combatEntries == null)
            {
                combatEntries = new Dictionary<string, CombatEntry>();
                var resource = Resources.Load<TextAsset>("GardenR1/pad-combat");
                if (resource != null)
                    foreach (var entry in JsonUtility.FromJson<CombatCatalog>(resource.text).entries)
                        if (entry.frames != null && entry.frames.Length == 3) combatEntries[entry.hex] = entry;
            }
            combatEntries.TryGetValue(HexKey(tint), out var found);
            return found;
        }
        public static bool HasPadCombat(Color tint)
        {
            var art = CombatArt(tint);
            return art != null && Has(PlantSlot(tint, art.frames[0].state))
                && Has(PlantSlot(tint, art.frames[1].state)) && Has(PlantSlot(tint, art.frames[2].state));
        }
        public static PadCombatPose SamplePadCombat(float seconds, float individuality = 1f)
        {
            float t = Mathf.Repeat(seconds, PadCombatPeriod);
            var p = new PadCombatPose { enabled = true, ring = -1f };
            // 0-.20 inhale; .20-.28 thrust; .28-.68 blowing; .68-.83 soft recovery.
            if (t < .20f)
            {
                p.from = 2; p.to = 0; p.blend = Mathf.SmoothStep(0f, 1f, t / .08f);
                p.lift = 7f * Mathf.Sin(t / .20f * Mathf.PI * .5f);
                p.sway = -3f * Mathf.Sin(t / .20f * Mathf.PI * .5f);
            }
            else if (t < .28f)
            {
                float q = (t - .20f) / .08f;
                p.from = 0; p.to = 1; p.blend = Mathf.SmoothStep(0f, 1f, q);
                p.lift = Mathf.Lerp(7f, -7f, q); p.sway = Mathf.Lerp(-3f, 4f, q);
                p.ring = q * .3f;
            }
            else if (t < .68f)
            {
                float q = (t - .28f) / .40f;
                p.from = p.to = 1; p.blend = 1f;
                p.lift = -7f * Mathf.Exp(-q * 8f) + 2f * Mathf.Sin(q * Mathf.PI * 4f);
                p.sway = 4f * Mathf.Exp(-q * 8f);
                p.ring = q < .35f ? .3f + q * 2f : -1f;
            }
            else
            {
                float q = (t - .68f) / .15f;
                p.from = 1; p.to = 2; p.blend = Mathf.SmoothStep(0f, 1f, q / .45f);
                p.lift = 5f * Mathf.Sin(q * Mathf.PI);
                p.sway = -3f * Mathf.Sin(q * Mathf.PI);
            }
            p.sway *= individuality;
            return p;
        }
        // The old pot is sampled in place. Root/paws stay still while the soft upper part moves.
        static bool DrawPadCombat(PixelSurface s, Color tint, PadCombatPose pose,
            float ax, float ay, float aw, float ah, out float top)
        {
            top = ay;
            var art = CombatArt(tint);
            if (art == null || !slots.TryGetValue(PlantSlot(tint, "idle"), out var pot)) return false;
            // 2026-10-27 19:50 (user: while attacking the toy looks stitched together, top and bottom do not match).
            // The body strips were drawn apart from the pots and registered by height only, so the paws spanned
            // 1.4-1.75x the pot and the body sat on the rim in front of it, with the idle leaves' lower half (from
            // y 124) showing as a green line between the paws. Now: the body is scaled about its paw line so the paws
            // span ~1.28x the pot (`fit`, measured by pad_combat_art.py), drawn first, and the pot (from the rim,
            // y 146) is drawn over it, so the body comes up out of the pot. The pot itself is unchanged.
            float cut = 146f / 256f;
            Rect puv = Uv(pot, Data.size);
            float pv0 = Mathf.Max(cut, pot.oy / pot.fh), pv1 = (pot.oy + pot.h) / pot.fh;
            float unit = aw / 224f, bodyY = ay - 44f * ah / 256f, fit = art.fit > 0f ? art.fit : 1f;
            float blend = Mathf.Clamp01(pose.blend);
            int selected = blend < .5f ? pose.from : pose.to;
            var activeFrame = art.frames[selected];
            Vector2 P(float x, float y)
            {
                float w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((190f - y) / 100f));
                float px = x + pose.sway * w, py = y - pose.lift * w;
                return new Vector2(ax + (112f + (px - 112f) * fit) * unit, bodyY + (200f + (py - 200f) * fit) * unit);
            }
            void Layer(int index, float opacity)
            {
                if (opacity <= .001f) return;
                var frame = art.frames[index];
                if (!slots.TryGetValue(PlantSlot(tint, frame.state), out var slot)) return;
                Rect uv = Uv(slot, Data.size);
                float x0 = slot.ox, x1 = slot.ox + slot.w, y0 = slot.oy, y1 = slot.oy + slot.h;
                const int bands = 12;
                for (int i = 0; i < bands; i++)
                {
                    float a = Mathf.Lerp(y0, y1, i / (float)bands), b = Mathf.Lerp(y0, y1, (i + 1f) / bands);
                    Rect rows = new Rect(uv.x, uv.yMax - (b - y0) / slot.h * uv.height, uv.width, (b - a) / slot.h * uv.height);
                    s.GardenQuadCorners(P(x0, a), P(x1, a), P(x1, b), P(x0, b), rows, new Color(1, 1, 1, opacity), slot.atlas);
                }
            }
            // Opaque authored key poses preserve the face and avoid double eyes.
            // The rooted soft motion supplies anticipation and follow-through.
            Layer(selected, 1f);
            s.GardenQuad(ax + aw * pot.ox / pot.fw, ay + ah * pv0, aw * pot.w / pot.fw, ah * (pv1 - pv0),
                new Rect(puv.x, puv.y, puv.width, (pv1 - pv0) * pot.fh / pot.h * puv.height), Color.white, false, true, pot.atlas);
            LastMouth = P(activeFrame.mouthX, activeFrame.mouthY);
            top = P(112f, activeFrame.top).y;
            return true;
        }
    }
}
#endif

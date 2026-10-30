#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System.Collections.Generic;
using Flightbeat.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Garden.UI
{
    // Independent art layers, behind the normal game controls and modal shields.
    // Side partners reuse their registered atlas frames; only the waving bear and
    // title need dedicated textures. No input or progression is baked into art.
    public sealed class GardenHomeArtwork : MonoBehaviour
    {
        internal static readonly int[] PaintOrder = { 0, 1, 4, 2, 3 };
        static readonly string[] Colors = { "orange", "yellow", "green", "purple", "blue" };
        static readonly string[] Poses = { "idle", "attack", "blink" };
        readonly RawImage[] portraits = new RawImage[5], shadows = new RawImage[5];
        readonly GardenSkin.Slot[,] frames = new GardenSkin.Slot[4, 3];
        RawImage title, frog, chick;
        Texture2D creatures, atlas, bearIdle, bearBlink;
        GardenSkin.Slot halo;
        bool initialized;

        RawImage Layer(string label)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(RawImage));
            var r = (RectTransform)go.transform; r.SetParent(transform, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            var image = go.GetComponent<RawImage>(); image.raycastTarget = false;
            return image;
        }
        void Initialize()
        {
            initialized = true;
            creatures = Resources.Load<Texture2D>("GardenR1/creatures");
            atlas = Resources.Load<Texture2D>("GardenR1/atlas");
            bearIdle = Resources.Load<Texture2D>("GardenR1/home-wave-bear-idle");
            bearBlink = Resources.Load<Texture2D>("GardenR1/home-wave-bear-blink");
            var lookup = new Dictionary<string, GardenSkin.Slot>();
            foreach (var slot in GardenSkin.Data.slots) lookup[slot.id] = slot;
            lookup.TryGetValue("home/halo", out halo);
            title = Layer("Cream title and subtitle");
            title.texture = Resources.Load<Texture2D>("GardenR1/home-wave-title");
            // 2026-10-30 16:10 (调整 4, 方案B): the frog and chick layers of the redrawn group, behind the bear.
            frog = Layer("Group frog"); frog.texture = Resources.Load<Texture2D>("GardenR1/home-wave-frog");
            chick = Layer("Group chick"); chick.texture = Resources.Load<Texture2D>("GardenR1/home-wave-chick");
            frog.enabled = frog.texture; chick.enabled = chick.texture;
            for (int i = 0; i < 4; i++)
                for (int p = 0; p < 3; p++) lookup.TryGetValue("plants/plush-home/" + Colors[i] + "/" + Poses[p], out frames[i, p]);
            foreach (int i in PaintOrder)
            {
                shadows[i] = Layer(Colors[i] + " contact shadow");
                shadows[i].color = new Color(.16f, .3f, .55f, .12f);   // 2026-10-30: blue-grey on the light home
                portraits[i] = Layer(Colors[i] + " animated portrait");
            }
        }
        static void Place(RawImage image, Rect bounds)
        {
            image.rectTransform.anchoredPosition = new Vector2(bounds.x - ScreenLayout.Left, -bounds.y);
            image.rectTransform.sizeDelta = bounds.size;
        }
        void AtlasFrame(RawImage image, GardenSkin.Slot slot, Rect frame, bool trimmed = true)
        {
            if (slot == null) { image.enabled = false; return; }
            image.enabled = true;
            bool creature = slot.atlas == 1;
            image.texture = creature ? creatures : atlas;
            float w = creature ? GardenSkin.Data.creatureWidth : GardenSkin.Data.size;
            float h = creature ? GardenSkin.Data.creatureHeight : GardenSkin.Data.size;
            image.uvRect = new Rect(slot.x / w, 1f - (slot.y + slot.h) / h, slot.w / w, slot.h / h);
            if (trimmed && slot.fw > 0f && slot.fh > 0f)
                frame = new Rect(frame.x + frame.width * slot.ox / slot.fw, frame.y + frame.height * slot.oy / slot.fh,
                    frame.width * slot.w / slot.fw, frame.height * slot.h / slot.fh);
            Place(image, frame);
        }
        public void Present(PixelWingGame game, bool show)
        {
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
            if (!show) return;
            if (!initialized) Initialize();
            // 2026-10-30 (界面优化 4, N1): the 泡泡小队 wordmark with the bear in its pot, kept at its own aspect.
            Texture tt = title.texture;
            float th = tt ? 400f * tt.height / Mathf.Max(1f, tt.width) : 139f;
            // 2026-10-30 16:10 (调整 4): the reference's one-line wordmark (batch 3, H1, ~10:3) centred under the top bar.
            bool wide = tt && tt.width >= tt.height * 2.6f;
            Place(title, tt && tt.width < tt.height * 2f ? PixelWingGame.HomeWaveRect(70f, 268f - th, 400f, th)
                : wide ? PixelWingGame.HomeWaveRect(95f, 88f, 360f, 360f * tt.height / tt.width) : PixelWingGame.HomeWaveRect(90f, 94f, 385f, 139f));
            bool group = PixelWingGame.HomeGroupArt;
            if (group)
            {
                Rect g = game.HomeWaveMemberRect(4);
                float t = Time.unscaledTime, k = g.width / 480f;
                Place(frog, new Rect(g.x, g.y + Mathf.Sin(t * 2.1f) * 2.5f * k, g.width, g.height));
                Place(chick, new Rect(g.x, g.y + Mathf.Sin(t * 2.4f + 1.7f) * 2.5f * k, g.width, g.height));
            }
            foreach (int i in PaintOrder)
            {
                if (group) { shadows[i].enabled = false; if (i != 4) { portraits[i].enabled = false; continue; } }
                Rect r = game.HomeWaveMemberRect(i);
                float bottom = i == 4 ? r.yMax - r.height * .025f : r.y + r.height * (212f / 256f);
                if (!group) AtlasFrame(shadows[i], halo, new Rect(r.center.x - r.width * .36f, bottom - r.height * .025f, r.width * .72f, r.height * .09f), false);
                int pose = game.HomeWavePose(i);
                if (i < 4) AtlasFrame(portraits[i], frames[i, pose], r);
                else { portraits[i].texture = pose == 2 && bearBlink ? bearBlink : bearIdle; Place(portraits[i], r); }
            }
        }
    }
}
#endif

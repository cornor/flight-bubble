using UnityEngine;

namespace Flightbeat.Rendering
{
    public partial class PixelSurface
    {
        // src: f54424, in allocation/assignment order; literals come from metadata.
        public static readonly string[] DeckArtNames =
            { "icon-sheet", "fleet-parts", "item-bomb", "launch-plate", "ui-plate" };
        private static readonly string[] DeckArtKeys =
            { "_DeckSheetTex", "_ShipTex", "_BombTex", "_LaunchTex", "_PlateTex" };

        private ISurfaceResourceRuntime resourceRuntime;
        private ISurfaceResourceRuntime ResourcesRuntime =>
            resourceRuntime ?? (resourceRuntime = new UnitySurfaceResourceRuntime());

        // src: f54391; Unity's overloaded null comparison, then a fresh field read.
        public override Texture mainTexture { get {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54391);
#endif
            return ResourcesRuntime.NotNull(atlas)
            ? atlas : ResourcesRuntime.WhiteTexture();
        } }

        // src: f54392. There is no base.Awake call in this source body.
        protected override void Awake()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54392);
#endif
            atlas = ResourcesRuntime.LoadTexture("PixelAtlas");
            var template = ResourcesRuntime.LoadMaterial("PixelLitSurface");
            var instance = ResourcesRuntime.CreateMaterial(template);
            ownedMaterial = instance;
            ResourcesRuntime.SetMaterial(this, instance);
            var names = new[] { "assistant", "victory", "rescue", "defeat" };
            var keys = new[] { "_AssistantTex", "_VictoryTex", "_RescueTex", "_DefeatTex" };
            for (int i = 0; i != 4; i++)
            {
                var destination = story;
                var texture = ResourcesRuntime.LoadTexture("Art/" + names[i]);
                destination[i] = texture;
                bool alive = ResourcesRuntime.Alive(story[i]);
                if (i == 0 && !alive)
                {
                    destination = story;
                    texture = ResourcesRuntime.LoadTexture("Art/assistant");
                    destination[0] = texture;
                }
                var probe = story[i];
                string key = keys[i];
                var material = ownedMaterial;
                var selected = ResourcesRuntime.Alive(probe)
                    ? story[i] : ResourcesRuntime.BlackTexture();
                ResourcesRuntime.SetTexture(material, key, selected);
            }
            // The material is captured before each load; these slots have no fallback.
            var target = ownedMaterial;
            var art = ResourcesRuntime.LoadTexture("Art/assistant-talk");
            ResourcesRuntime.SetTexture(target, "_AssistantTalkTex", art);
            target = ownedMaterial;
            art = ResourcesRuntime.LoadTexture("Art/assistant-blink");
            ResourcesRuntime.SetTexture(target, "_AssistantBlinkTex", art);
            target = ownedMaterial;
            art = ResourcesRuntime.LoadTexture("Art/bridge");
            ResourcesRuntime.SetTexture(target, "_BridgeTex", art);
            target = ownedMaterial;
            art = ResourcesRuntime.LoadTexture("Art/space");
            ResourcesRuntime.SetTexture(target, "_SpaceTex", art);
            LoadDeckArt();
            // Source virtual slot +392 resolves to Graphic.set_raycastTarget f24732.
            ResourcesRuntime.SetRaycastTarget(this, false);
        }

        // src: f54393. Retain the captured store array and subsequent fresh reads.
        private void LoadDeckArt()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54393);
#endif
            var destination = deckTextures;
            int i = 0;
            while (i < destination.Length)
            {
                destination[i] = ResourcesRuntime.LoadTexture("DeckArt/" + DeckArtNames[i]);
                if (!ResourcesRuntime.Alive(deckTextures[i]))
                    ResourcesRuntime.LogError("Missing generated deck art: " + DeckArtNames[i]);
                var material = ownedMaterial;
                var probe = deckTextures[i];
                string key = DeckArtKeys[i];
                var selected = ResourcesRuntime.Alive(probe)
                    ? deckTextures[i] : ResourcesRuntime.BlackTexture();
                ResourcesRuntime.SetTexture(material, key, selected);
                i++;
                destination = deckTextures;
            }
        }

        // src: f54397. Destroy uses a fresh ownedMaterial read; do not clear the field.
        protected override void OnDestroy()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54397);
#endif
            if (ResourcesRuntime.Alive(ownedMaterial))
                ResourcesRuntime.Destroy(ownedMaterial);
            ResourcesRuntime.DestroyGraphic(this);
        }

        // MaskableGraphic has no OnDestroy override in the bundled UGUI source;
        // this resolves to the original direct Graphic.OnDestroy boundary f24764.
        internal void DestroyGraphicBase() => base.OnDestroy();

        // src: f54398. This loop keeps its initial array, including across API calls.
        public bool DeckArtReady
        {
            get
            {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54398);
#endif
                var textures = deckTextures;
                for (int i = 0; i < textures.Length; i++)
                    if (!ResourcesRuntime.Alive(textures[i])) return false;
                return true;
            }
        }
    }
}

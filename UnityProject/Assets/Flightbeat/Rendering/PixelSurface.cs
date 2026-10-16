using System;
using Flightbeat.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Rendering
{
    public partial class PixelSurface : MaskableGraphic
    {
        public Action<PixelSurface> Paint;
        // src: f54423 field initializers (native/base constructor not yet validated).
        public bool Lighting = true;
        public bool MapLayout = true;
        private Texture atlas;
        private VertexHelper mesh;
        private Material ownedMaterial;
        private Texture2D[] deckTextures = new Texture2D[5];
        private Texture[] story = new Texture[4];
        private Texture2D codex;
        private UIVertex[] quad = new UIVertex[4];
        private IDisplayRuntime runtime;
        private IDisplayRuntime Runtime => runtime ?? (runtime = new UnityDisplayRuntime());

        // src: f54406; the original virtual slot resolves to Graphic.SetVerticesDirty.
        public void Redraw() {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54406);
#endif
            Runtime.SetVerticesDirty(this);
        }

        // src: f54405. Clear precedes assigning the mesh and reading Paint.
        protected override void OnPopulateMesh(VertexHelper target)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawActivation = Flightbeat.Diagnostics.RuntimeLoopInputs.BeginMeshObservation?.Invoke();
            Flightbeat.Diagnostics.RuntimeLoopInputs.Method?.Invoke(54405);
            // f54405 invokes Paint synchronously. DrawFinaleButton (f54740)
            // can issue f55175 cues inside that call; preserve its ancestry.
            using var audioScope = new Flightbeat.Diagnostics.RuntimeLoopInputs.AudioMethodScope(54405);
#endif
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54405);
#endif
            target.Clear();
            mesh = target;
            var paint = Paint;
            paint?.Invoke(this);
            ObserveMesh(target);
        }
    }
}

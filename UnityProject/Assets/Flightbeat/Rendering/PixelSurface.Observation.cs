using UnityEngine;

namespace Flightbeat.Rendering
{
    public partial class PixelSurface
    {
        // CLI observation only. Calls and argument evaluation disappear from players.
        partial void ObserveQuadBegin(float x, float y, float w, float h, Rect uv, Color color,
            float angle, float mode, bool rigid, float animationFrame);
        partial void ObserveQuadEnd();
        partial void ObserveMesh(UnityEngine.UI.VertexHelper target);
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
        public sealed class QuadObservation
        {
            public float[] input;
            public bool rigid, mapLayout, lighting;
            public UIVertex[] vertices;
            public int order;
            public int[] callPath;
        }
        public System.Action<QuadObservation> QuadObserver;
        private QuadObservation observedQuad;
        partial void ObserveQuadBegin(float x, float y, float w, float h, Rect uv, Color color,
            float angle, float mode, bool rigid, float animationFrame)
        {
            if (QuadObserver == null || DrawObservation.Current == null) return;
            observedQuad = new QuadObservation { input = new[] { x, y, w, h, uv.x, uv.y, uv.width,
                uv.height, color.r, color.g, color.b, color.a, angle, mode, animationFrame },
                rigid = rigid, mapLayout = MapLayout, lighting = Lighting,
                order = DrawObservation.Current?.NextOrder() ?? -1,
                callPath = DrawObservation.Current?.CallPath() ?? new int[0] };
        }
        partial void ObserveQuadEnd()
        {
            if (QuadObserver == null || DrawObservation.Current == null) return;
            observedQuad.vertices = (UIVertex[])quad.Clone();
            QuadObserver(observedQuad);
            observedQuad = null;
        }
        partial void ObserveMesh(UnityEngine.UI.VertexHelper target)
        {
            DrawObservation.Current?.ObserveMesh(this, target);
#if FLIGHTBEAT_CLI_PROBE && !UNITY_EDITOR
            Flightbeat.Diagnostics.RuntimeLoopInputs.MeshOutput?.Invoke(this, target);
#endif
        }
#endif
    }
}

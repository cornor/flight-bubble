using System;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Rendering
{
    public partial class PixelSurface
    {
        private static float Float(uint bits) => BitConverter.Int32BitsToSingle(unchecked((int)bits));
        private static Rect WhiteUv => new Rect(Float(0x3E738000u), Float(0x3F719000u), 0.0009765625f, 0.00048828125f);
        private static Color32 VertexColor(Color color)
        {
            uint bits = OriginalMath.Color32Bits(color.r, color.g, color.b, color.a);
            return new Color32(unchecked((byte)bits), unchecked((byte)(bits >> 8)), unchecked((byte)(bits >> 16)), unchecked((byte)(bits >> 24)));
        }

        // src: f54395. Rotation, layout and y inversion retain WAT grouping;
        // each vertex independently reads simpleVert and ScreenLayout.Height.
        // Materialize every WAT f32 arithmetic result before the next instruction.
        public void Quad(float x, float y, float w, float h, Rect uv, Color color,
            float angle, float mode, bool rigid, float animationFrame)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54395);
#endif
            ObserveQuadBegin(x, y, w, h, uv, color, angle, mode, rigid, animationFrame);
            if (MapLayout)
            {
                if (rigid || mode == 1f)
                {
                    float half = OriginalMath.Binary32(h * 0.5f);
                    y = OriginalMath.Binary32(PlushLayout.Y(OriginalMath.Binary32(half + y)) - half);
                }
                else
                {
                    float bottom = PlushLayout.Y(OriginalMath.Binary32(y + h));
                    y = PlushLayout.Y(y);
                    h = OriginalMath.Binary32(bottom - y);
                }
            }
            float halfW = OriginalMath.Binary32(w * 0.5f);
            float radians = OriginalMath.Binary32(angle * Float(0xBC8EFA35u));
            float sin = OriginalMath.Sin(radians), cos = OriginalMath.Cos(radians);
            float negativeHalfH = OriginalMath.Binary32(h * -0.5f), negativeHalfW = OriginalMath.Binary32(w * -0.5f);
            x = OriginalMath.Binary32(halfW + x);
            var v = UIVertex.simpleVert;
            float height = ScreenLayout.Height;
            float leftX = OriginalMath.Binary32(x + OriginalMath.Binary32(cos * negativeHalfW)), leftSin = OriginalMath.Binary32(sin * negativeHalfW), halfH = OriginalMath.Binary32(h * 0.5f);
            v.position = new Vector3(OriginalMath.Binary32(leftX - OriginalMath.Binary32(sin * negativeHalfH)), OriginalMath.Binary32(OriginalMath.Binary32(cos * negativeHalfH) + OriginalMath.Binary32(leftSin + OriginalMath.Binary32(OriginalMath.Binary32(height - y) - halfH))), 0f);
            v.color = VertexColor(color);
            v.uv1 = new Vector4(mode, animationFrame, 0f, 0f);
            float uvHeight = uv.height, uvX = uv.x, uvY = uv.y;
            v.uv0 = new Vector4(uvX, uvY, 0f, 0f);
            quad[0] = v;
            float uvWidth = uv.width;
            negativeHalfH = OriginalMath.Binary32(h * -0.5f);
            v = UIVertex.simpleVert;
            height = ScreenLayout.Height;
            float rightX = OriginalMath.Binary32(x + OriginalMath.Binary32(cos * halfW)), rightSin = OriginalMath.Binary32(sin * halfW);
            v.position = new Vector3(OriginalMath.Binary32(rightX - OriginalMath.Binary32(sin * negativeHalfH)), OriginalMath.Binary32(OriginalMath.Binary32(cos * negativeHalfH) + OriginalMath.Binary32(rightSin + OriginalMath.Binary32(OriginalMath.Binary32(height - y) - halfH))), 0f);
            v.color = VertexColor(color);
            v.uv1 = new Vector4(mode, animationFrame, 0f, 0f);
            float uvRight = OriginalMath.Binary32(uvWidth + uvX);
            v.uv0 = new Vector4(uvRight, uvY, 0f, 0f);
            quad[1] = v;
            float positiveHalfH = OriginalMath.Binary32(h * 0.5f);
            v = UIVertex.simpleVert;
            height = ScreenLayout.Height;
            v.position = new Vector3(OriginalMath.Binary32(rightX - OriginalMath.Binary32(sin * positiveHalfH)), OriginalMath.Binary32(OriginalMath.Binary32(cos * positiveHalfH) + OriginalMath.Binary32(rightSin + OriginalMath.Binary32(OriginalMath.Binary32(height - y) - halfH))), 0f);
            v.color = VertexColor(color);
            v.uv1 = new Vector4(mode, animationFrame, 0f, 0f);
            float uvTop = OriginalMath.Binary32(uvY + uvHeight);
            v.uv0 = new Vector4(uvRight, uvTop, 0f, 0f);
            quad[2] = v;
            positiveHalfH = OriginalMath.Binary32(h * 0.5f);
            v = UIVertex.simpleVert;
            height = ScreenLayout.Height;
            v.position = new Vector3(OriginalMath.Binary32(leftX - OriginalMath.Binary32(sin * positiveHalfH)), OriginalMath.Binary32(OriginalMath.Binary32(cos * positiveHalfH) + OriginalMath.Binary32(leftSin + OriginalMath.Binary32(OriginalMath.Binary32(height - y) - halfH))), 0f);
            v.color = VertexColor(color);
            v.uv1 = new Vector4(mode, animationFrame, 0f, 0f);
            v.uv0 = new Vector4(uvX, uvTop, 0f, 0f);
            quad[3] = v;
            mesh.AddUIVertexQuad(quad);
            ObserveQuadEnd();
        }

        // src: f54407.
        public void Rect(float x, float y, float w, float h, Color color) {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54407);
#endif
            Quad(x, y, w, h, WhiteUv, color, 0f, 0f, false, 0f);
        }

        // src: f54408. Four direct Quad calls: Rect is inlined in the WAT.
        public void Border(float x, float y, float w, float h, Color color, float thickness)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54408);
#endif
            float t = thickness;
            Quad(x, y, w, t, WhiteUv, color, 0f, 0f, false, 0f);
            Quad(x, OriginalMath.Binary32((OriginalMath.Binary32(y + h)) - t), w, t, WhiteUv, color, 0f, 0f, false, 0f);
            Quad(x, y, t, h, WhiteUv, color, 0f, 0f, false, 0f);
            Quad(OriginalMath.Binary32((OriginalMath.Binary32(x + w)) - t), y, t, h, WhiteUv, color, 0f, 0f, false, 0f);
        }

        // src: f54409. Eight direct Quad calls: Rect is inlined in the WAT.
        public void Corners(float x, float y, float w, float h, Color color, int length, int thickness)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54409);
#endif
            float t = thickness, size = length;
            Quad(x, y, t, size, WhiteUv, color, 0f, 0f, false, 0f); Quad(x, y, size, t, WhiteUv, color, 0f, 0f, false, 0f);
            float right = OriginalMath.Binary32((OriginalMath.Binary32(x + w)) - t);
            Quad(right, y, t, size, WhiteUv, color, 0f, 0f, false, 0f);
            float endX = OriginalMath.Binary32((OriginalMath.Binary32(right - size)) + t);
            Quad(endX, y, size, t, WhiteUv, color, 0f, 0f, false, 0f);
            float bottom = OriginalMath.Binary32((OriginalMath.Binary32(y + h)) - t), endY = OriginalMath.Binary32((OriginalMath.Binary32(bottom - size)) + t);
            Quad(x, endY, t, size, WhiteUv, color, 0f, 0f, false, 0f); Quad(x, bottom, size, t, WhiteUv, color, 0f, 0f, false, 0f);
            Quad(right, endY, t, size, WhiteUv, color, 0f, 0f, false, 0f); Quad(endX, bottom, size, t, WhiteUv, color, 0f, 0f, false, 0f);
        }

        // src: f54410. No finally: an exceptional source call does not restore.
        public void Segment(Vector2 from, Vector2 to, float width, Color color)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54410);
#endif
            bool mapped = MapLayout;
            if (mapped) { from = PlushLayout.Point(from); to = PlushLayout.Point(to); MapLayout = false; }
            float dx = OriginalMath.Binary32(from.x - to.x), dy = OriginalMath.Binary32(from.y - to.y);
            float length = OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy))));
            Quad(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(from.x + to.x)) * 0.5f) - OriginalMath.Binary32(width * 0.5f)), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(from.y + to.y)) * 0.5f) - OriginalMath.Binary32(length * 0.5f)),
                width, length, WhiteUv, color, OriginalMath.Binary32(OriginalMath.Atan2Single(OriginalMath.Binary32(to.x - from.x), -(OriginalMath.Binary32(to.y - from.y))) * Float(0x42652EE1u)), 0f, false, 0f);
            MapLayout = mapped;
        }

        // src: f54411.
        public void Ring(Vector2 center, float radius, Color color, float thickness, int segments, float start, float arc)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54411);
            if (DrawObservation.Current != null) DrawObservation.Current.Input(54411, null, center.x, center.y, radius, thickness, segments, start, arc);
#endif
            if (segments <= 0) return;
            float radians = OriginalMath.Binary32(start * Float(0x3C8EFA35u));
            Vector2 previous = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(radians) * radius) + center.x), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(radians) * radius) + center.y));
            float count = segments;
            int i = 1;
            do
            {
                radians = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((float)i) * arc)) / count) + start)) * Float(0x3C8EFA35u));
                Vector2 next = new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(radians) * radius) + center.x), OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(radians) * radius) + center.y));
                Segment(previous, next, thickness, color);
                previous = next;
                i = unchecked(i + 1);
            } while (i <= segments);
        }

        // src: f54412.
        public void Glow(float x, float y, float w, float h, Color color, float angle)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54412);
#endif
            if (Lighting) Quad(OriginalMath.Binary32(x - OriginalMath.Binary32(w * 0.5f)), OriginalMath.Binary32(y - OriginalMath.Binary32(h * 0.5f)), w, h, new Rect(0f, 0f, 1f, 1f), color, angle, 1f, false, 0f);
        }

        // src: f54413. Mapping occurs even when Lighting is false.
        public void GlowLine(Vector2 from, Vector2 to, float width, Color color)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54413);
#endif
            bool mapped = MapLayout;
            if (mapped) { from = PlushLayout.Point(from); to = PlushLayout.Point(to); MapLayout = false; }
            if (Lighting)
            {
                float dx = OriginalMath.Binary32(from.x - to.x), dy = OriginalMath.Binary32(from.y - to.y);
                float length = OriginalMath.Binary32(OriginalMath.Binary32((float)Math.Sqrt(OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy)))) + width);
                Quad(OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(from.x + to.x)) * 0.5f) - OriginalMath.Binary32(width * 0.5f)), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(from.y + to.y)) * 0.5f) - OriginalMath.Binary32(length * 0.5f)),
                    width, length, new Rect(0f, 0f, 1f, 1f), color, OriginalMath.Binary32(OriginalMath.Atan2Single(OriginalMath.Binary32(to.x - from.x), -(OriginalMath.Binary32(to.y - from.y))) * Float(0x42652EE1u)), 1f, false, 0f);
            }
            MapLayout = mapped;
        }

        // src: f54414.
        public void Icon(int id, float x, float y, float w, float h, Color color, float angle, int sw, int sh)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54414);
#endif
            int row = id / 16, column = unchecked(id - (row << 4));
            var uv = new Rect(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)column) * 16f) * 0.0009765625f),
                OriginalMath.Binary32(1f - OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)row) * 16f) + OriginalMath.Binary32((float)sh))) * 0.00048828125f)), OriginalMath.Binary32(OriginalMath.Binary32((float)sw) * 0.0009765625f), OriginalMath.Binary32(OriginalMath.Binary32((float)sh) * 0.00048828125f));
            Quad(x, y, w, h, uv, color, angle, 0f, true, 0f);
        }

        private static Rect SpriteUv(int cell)
        {
            int row = cell / 8, column = unchecked(cell - (row << 3));
            return new Rect(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)column) * 32f) + 256f)) * 0.0009765625f), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)row) * 32f) + 32f)) * -0.00048828125f) + 1f), 0.03125f, 0.015625f);
        }
        private static Rect HullUv(int cell)
        {
            int row = cell / 16, column = unchecked(cell - (row << 4));
            return new Rect(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)column) * 32f) * 0.0009765625f), OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32((float)row) * 32f) + 1792f)) + 32f)) * -0.00048828125f) + 1f), 0.03125f, 0.015625f);
        }

        // src: f54418.
        public void Sprite(int cell, float x, float y, float w, float h, Color color, float angle) {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54418);
#endif
            Quad(x, y, w, h, SpriteUv(cell), color, angle, 0f, true, 0f);
        }
        // src: f54419.
        public void SpriteAt(int cell, Vector2 at, float size, Color color, float angle)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54419);
#endif
            float half = OriginalMath.Binary32(size * 0.5f);
            Quad(OriginalMath.Binary32(at.x - half), OriginalMath.Binary32(at.y - half), size, size, SpriteUv(cell), color, angle, 0f, true, 0f);
        }
        // src: f54420.
        public void Hull(int cell, float x, float y, float w, float h, Color color, float angle) {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54420);
#endif
            Quad(x, y, w, h, HullUv(cell), color, angle, 0f, true, 0f);
        }
        // src: f54421.
        public void HullAt(int cell, Vector2 at, float size, Color color, float angle)
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            using var drawScope = Flightbeat.Rendering.DrawObservation.Enter(54421);
#endif
            float half = OriginalMath.Binary32(size * 0.5f);
            Quad(OriginalMath.Binary32(at.x - half), OriginalMath.Binary32(at.y - half), size, size, HullUv(cell), color, angle, 0f, true, 0f);
        }
    }
}

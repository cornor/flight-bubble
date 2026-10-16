#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Rendering
{
    // CLI-only equivalent of Tools/draw/observe.js. No expected records are read.
    // Constructors and Awake run before capture, just as in capture_fixed.js.
    public sealed class DrawObservation : IDisposable
    {
        [Serializable] public sealed class Count { public int function, count; }
        [Serializable] public sealed class Hit
        {
            public int sequence, order, id, registeredId;
            public string surface;
            public int[] requestedBits, registeredBits;
            public bool accepted;
        }
        [Serializable] public sealed class MeshData
        {
            public string surface;
            public bool initialized;
            public int[] vertexBits, indices;
        }
        [Serializable] public sealed class InputCall { public int function, invocation; public string text; public int[] bits; }
        public readonly List<InputCall> InputCalls = new List<InputCall>();
        public void Input(int function, string text, params float[] values) => InputCalls.Add(new InputCall {
            function = function, invocation = counts[function].count, text = text, bits = Array.ConvertAll(values, Bits) });
        [Serializable] public sealed class Data
        {
            public List<Count> counts = new List<Count>();
            public List<Hit> hitCalls = new List<Hit>();
            public List<MeshData> meshes = new List<MeshData>();
        }
        public static DrawObservation Current { get; private set; }
        public readonly Data Result = new Data();
        private readonly Dictionary<PixelSurface, string> surfaces;
        private readonly List<int> stack = new List<int>();
        private readonly Dictionary<int, Count> counts = new Dictionary<int, Count>();
        private int ordinal;
        private bool disposed;

        public DrawObservation(Dictionary<PixelSurface, string> surfaces, bool activate = true)
        {
            if (Current != null) throw new InvalidOperationException("Nested draw observation");
            this.surfaces = new Dictionary<PixelSurface, string>(surfaces);
            if (activate) Current = this;
        }
        // Keep one frame's ordering across surfaces, but observe only actual Paint calls.
        public IDisposable Activate()
        {
            if (disposed || Current != null || stack.Count != 0) throw new InvalidOperationException("Invalid drawing activation");
            Current = this;
            return new Activation(this);
        }
        private sealed class Activation : IDisposable
        {
            private readonly DrawObservation owner;
            public Activation(DrawObservation owner) { this.owner = owner; }
            public void Dispose()
            {
                if (Current != owner || owner.stack.Count != 0) throw new InvalidOperationException("Unbalanced drawing activation");
                Current = null;
            }
        }
        public static Scope Enter(int function)
        {
            var current = Current;
            if (current == null) return default;
            if (function == 54783) throw new InvalidOperationException("Update entered isolated drawing");
            current.stack.Add(function);
            if (!current.counts.TryGetValue(function, out var entry))
            {
                entry = new Count { function = function };
                current.counts.Add(function, entry); current.Result.counts.Add(entry);
            }
            entry.count++;
            return new Scope(current, function);
        }
        public readonly struct Scope : IDisposable
        {
            private readonly DrawObservation owner;
            private readonly int function;
            internal Scope(DrawObservation owner, int function) { this.owner = owner; this.function = function; }
            public void Dispose()
            {
                if (owner == null) return;
                int last = owner.stack.Count - 1;
                if (last < 0 || owner.stack[last] != function) throw new InvalidOperationException("Unbalanced draw stack");
                owner.stack.RemoveAt(last);
            }
        }
        public int NextOrder() => ordinal++;
        public int[] CallPath() => stack.ToArray();
        private string Surface(PixelSurface surface) => surfaces.TryGetValue(surface, out var name)
            ? name : throw new InvalidOperationException("Unregistered drawing surface");
        public Hit BeginHit(PixelSurface surface, int id, float x, float y, float w, float h)
        {
            var hit = new Hit { sequence = Result.hitCalls.Count, order = NextOrder(), surface = Surface(surface),
                id = id, requestedBits = new[] { Bits(x), Bits(y), Bits(w), Bits(h) }, registeredBits = new int[0] };
            Result.hitCalls.Add(hit);
            return hit;
        }
        public void ObserveMesh(PixelSurface surface, VertexHelper helper) => Result.meshes.Add(ReadMesh(Surface(surface), helper));
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Current = null;
            if (stack.Count != 0) throw new InvalidOperationException("Drawing scope did not close");
        }
        public static int Bits(float value) => BitConverter.SingleToInt32Bits(value);
        public static int[] Pack(UIVertex v) => new[] {
            Bits(v.position.x), Bits(v.position.y), Bits(v.position.z),
            Bits(v.normal.x), Bits(v.normal.y), Bits(v.normal.z),
            Bits(v.tangent.x), Bits(v.tangent.y), Bits(v.tangent.z), Bits(v.tangent.w),
            unchecked((int)((uint)v.color.r | (uint)v.color.g << 8 | (uint)v.color.b << 16 | (uint)v.color.a << 24)),
            Bits(v.uv0.x), Bits(v.uv0.y), Bits(v.uv0.z), Bits(v.uv0.w),
            Bits(v.uv1.x), Bits(v.uv1.y), Bits(v.uv1.z), Bits(v.uv1.w),
            Bits(v.uv2.x), Bits(v.uv2.y), Bits(v.uv2.z), Bits(v.uv2.w),
            Bits(v.uv3.x), Bits(v.uv3.y), Bits(v.uv3.z), Bits(v.uv3.w) };
        private static T Field<T>(VertexHelper helper, string name) =>
            (T)(typeof(VertexHelper).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingFieldException("VertexHelper", name)).GetValue(helper);
        // Read the actual UGUI lists, without invoking APIs that initialize or modify them.
        // Field spelling is taken from the bundled com.unity.ugui@1.0.0 source.
        public static MeshData ReadMesh(string surface, VertexHelper helper)
        {
            var result = new MeshData { surface = surface, initialized = Field<bool>(helper, "m_ListsInitalized") };
            if (!result.initialized) return result;
            var positions = Field<List<Vector3>>(helper, "m_Positions");
            var normals = Field<List<Vector3>>(helper, "m_Normals");
            var tangents = Field<List<Vector4>>(helper, "m_Tangents");
            var colors = Field<List<Color32>>(helper, "m_Colors");
            var uv0 = Field<List<Vector4>>(helper, "m_Uv0S");
            var uv1 = Field<List<Vector4>>(helper, "m_Uv1S");
            var uv2 = Field<List<Vector4>>(helper, "m_Uv2S");
            var uv3 = Field<List<Vector4>>(helper, "m_Uv3S");
            int count = positions.Count;
            foreach (int length in new[] { normals.Count, tangents.Count, colors.Count, uv0.Count, uv1.Count, uv2.Count, uv3.Count })
                if (length != count) throw new InvalidOperationException("Mismatched mesh channel lengths");
            var packed = new List<int>(count * 27);
            for (int i = 0; i < count; i++) packed.AddRange(Pack(new UIVertex { position = positions[i], normal = normals[i],
                tangent = tangents[i], color = colors[i], uv0 = uv0[i], uv1 = uv1[i], uv2 = uv2[i], uv3 = uv3[i] }));
            result.vertexBits = packed.ToArray(); result.indices = Field<List<int>>(helper, "m_Indices").ToArray();
            return result;
        }
    }
}
#endif

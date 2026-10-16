using System.Collections.Generic;
using Flightbeat.Game;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed class PixelEffectPool
    {
        public const int Capacity = 128; // src: f54381/f54383.
        // src: f54388 array stores; order is significant for prewarming/capacity.
        public static readonly string[] Kinds =
        {
            "PixelHit", "PixelFusion", "PixelLaunch", "PixelMuzzle", "PixelShield", "PixelVictory",
            "PixelMaxFusion", "PixelIonHit", "PixelLaserHit", "PixelRocketImpact", "PixelShockwave",
            "PixelRescue", "PixelSuperBomb", "PixelSuperMissile", "PixelTierUp", "PixelBurn"
        };

        private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
        private readonly List<PixelEffect> instances = new List<PixelEffect>();
        private readonly Transform owner;
        private readonly RectTransform canvas;
        private readonly IPixelEffectPoolRuntime runtime;
        public bool Enabled = true;

        // src: f54375, f6993/f8235, f7354/f7372.
        public int Created => instances.Count;
        public int PeakAlive { get; private set; }
        public int Dropped { get; private set; }

        // src: f54376/f54378/f54379. Preserve the per-effect grouping of sums.
        public int Alive
        {
            get { int count = 0; foreach (PixelEffect effect in instances) count = unchecked(count + (effect.Alive ? 1 : 0)); return count; }
        }
        public int ParticleCount
        {
            get { int count = 0; foreach (PixelEffect effect in instances) count = unchecked(count + effect.ParticleCount); return count; }
        }
        public float SimulationTime
        {
            get { float time = 0f; foreach (PixelEffect effect in instances) time = time + effect.SimulationTime; return time; }
        }

        // src: f54380. Load every declared key, including missing (null) results.
        public PixelEffectPool(Transform owner, RectTransform canvas)
            : this(owner, canvas, UnityPixelEffectPoolRuntime.Instance) { }

        // The extra runtime argument declares the native API boundary for verification.
        internal PixelEffectPool(Transform owner, RectTransform canvas, IPixelEffectPoolRuntime runtime)
        {
            this.runtime = runtime;
            this.canvas = canvas;
            this.owner = owner;
            string[] kinds = Kinds;
            for (int i = 0; i < kinds.Length; i++)
            {
                string kind = kinds[i];
                prefabs.Add(kind, runtime.LoadPrefab("particles/" + kind));
            }
        }

        // src: f54381; the cached FindIndex predicate is f54390 (!p.Alive).
        public void Play(string kind, Vector2 position, Color tint)
        {
            if (!Enabled) return;
            PixelEffect effect = null;
            foreach (PixelEffect candidate in instances)
            {
                if (candidate.Kind == kind && !candidate.Alive) { effect = candidate; break; }
            }
            if (!runtime.Exists(effect))
            {
                if (instances.Count >= Capacity)
                {
                    int index = instances.FindIndex(p => !p.Alive);
                    if (index < 0) { Dropped = unchecked(Dropped + 1); return; }
                    runtime.Destroy(runtime.GameObjectOf(instances[index]));
                    instances.RemoveAt(index);
                }
                if (!runtime.Exists(prefabs[kind]))
                {
                    runtime.LogError("Missing authored particle Prefab: " + kind);
                    return;
                }
                // Re-read the dictionary after the liveness query, as in the WAT.
                effect = runtime.GetEffect(runtime.Instantiate(prefabs[kind], owner));
                effect.Initialize(kind);
                instances.Add(effect);
            }
            effect.LogicalPosition = position;
            Vector3 world = WorldPosition(position);
            float scale = runtime.LossyScale(canvas).x;
            effect.PlayAt(world, scale, tint);
            int previousPeak = PeakAlive;
            int alive = Alive;
            PeakAlive = previousPeak > alive ? previousPeak : alive;
        }

        // src: f54383. Counts include every matching instance, regardless of liveness.
        public void Prewarm()
        {
            string[] kinds = Kinds;
            for (int i = 0; i < kinds.Length; i++)
            {
                string kind = kinds[i];
                if (!runtime.Exists(prefabs[kind])) continue;
                int desired = kind == "PixelHit" ? 12
                    : kind == "PixelLaserHit" || kind == "PixelIonHit" ? 8
                    : kind == "PixelMuzzle" ? 6
                    : kind == "PixelLaunch" || kind == "PixelFusion" || kind == "PixelTierUp" || kind == "PixelRocketImpact" ? 3
                    : 1;
                int count = 0;
                foreach (PixelEffect effect in instances)
                    count = unchecked(count + (effect.Kind == kind ? 1 : 0));
                while (count < desired)
                {
                    if (instances.Count >= Capacity) break;
                    PixelEffect effect = runtime.GetEffect(runtime.Instantiate(prefabs[kind], owner));
                    effect.Initialize(kind);
                    effect.Clear();
                    instances.Add(effect);
                    count = unchecked(count + 1);
                }
            }
        }

        // src: f54382. Capture canvas and Height in original order, round each input
        // with the source f4708-based operation, map Y, then use native TransformPoint.
        private Vector3 WorldPosition(Vector2 position)
        {
            RectTransform target = canvas;
            float x = OriginalMath.RoundSingle(position.x);
            float height = ScreenLayout.Height;
            float y = ScreenLayout.Y(OriginalMath.RoundSingle(position.y));
            return runtime.TransformPoint(target, new Vector3(x, height - y, -2f));
        }

        // src: f54384. Only living effects move; one lossy-scale read per effect.
        public void Relayout()
        {
            foreach (PixelEffect effect in instances)
            {
                if (!effect.Alive) continue;
                Vector3 world = WorldPosition(effect.LogicalPosition);
                float scale = runtime.LossyScale(canvas).x;
                effect.PlaceAt(world, scale);
            }
        }

        // src: f54385/f54386. These do not reset counters or remove instances.
        public void Freeze(bool paused) { foreach (PixelEffect effect in instances) effect.Freeze(paused); }
        public void Clear() { foreach (PixelEffect effect in instances) effect.Clear(); }

        // src: f54387. Skip destroyed effects, destroy their GameObjects, then clear
        // the instance list. Prefabs, Enabled and counters retain their original values.
        public void Dispose()
        {
            foreach (PixelEffect effect in instances)
                if (runtime.Exists(effect)) runtime.Destroy(runtime.GameObjectOf(effect));
            instances.Clear();
        }
    }
}

#if FLIGHTBEAT_CLI_PROBE && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Flightbeat.Feedback;
using UnityEngine;

namespace Flightbeat.Diagnostics
{
    public sealed partial class StartupWindowProbe
    {
        [Serializable] private sealed class ParticleClock
        {
            public int frame, unityFrame, declaredDeltaBits, deltaBits, unscaledDeltaBits,
                captureDeltaBits, timeScaleBits, maximumParticleDeltaBits;
        }
        [Serializable] private sealed class ParticleClocks
        {
            public string scope = "Read-only native Time API values at Game.Update input boundary. Original f90111 uses its explicit captured delta for both scaled/unscaled branches. A separate declared-particle-clock.json is present only when the explicit native input adapter is enabled; otherwise particles use wall time.";
            public List<ParticleClock> frames = new List<ParticleClock>();
        }
        [Serializable] private sealed class ParticleSample
        {
            // Shared source fields from controller.js particleState (f54217,
            // f54216, f54221 wrappers). Extras are observations, not assertions.
            public float time;
            public int particleCount;
            public bool isAliveWithChildren;
            public string name;
            public bool isPlaying, isPaused, isStopped, useUnscaledTime, autoRandomSeed;
            public uint randomSeed;
            public float simulationSpeed;
            public Vector3 position, localPosition, lossyScale;
        }
        [Serializable] private sealed class ParticleInstance
        {
            public int index;
            public string kind;
            public float[] logicalPosition;
            public ParticleSample root;
            public ParticleSample[] tintLayers, children;
        }
        [Serializable] private sealed class ParticleSnapshot
        {
            public string scope = "Read-only observer: original-comparable pool counts, ordered kinds/logical positions, root and tint-layer time/count/IsAlive. Child settings/transforms are supplementary native observations. See separate clock report for any explicitly enabled input adapter. No per-particle position equivalence claim.";
            public int created, peakAlive, dropped;
            public List<ParticleInstance> instances = new List<ParticleInstance>();
        }
        private static readonly ParticleClocks particleClocks = new ParticleClocks();
        private static bool particleClocksSaved;
        private static void ObserveContinuousParticleClock()
        {
            if (drawDirectory == null) return;
            particleClocks.frames.Add(new ParticleClock { frame = loopCursor, unityFrame = Time.frameCount,
                declaredDeltaBits = BitConverter.SingleToInt32Bits(loop.dt), deltaBits = BitConverter.SingleToInt32Bits(Time.deltaTime),
                unscaledDeltaBits = BitConverter.SingleToInt32Bits(Time.unscaledDeltaTime),
                captureDeltaBits = BitConverter.SingleToInt32Bits(Time.captureDeltaTime),
                timeScaleBits = BitConverter.SingleToInt32Bits(Time.timeScale),
                maximumParticleDeltaBits = BitConverter.SingleToInt32Bits(Time.maximumParticleDeltaTime) });
        }
        private static void SaveContinuousParticleClocks()
        {
            if (drawDirectory == null || particleClocksSaved) return;
            particleClocksSaved = true;
            try
            {
                using (var file = new FileStream(Path.Combine(drawDirectory, "particle-clocks.json"), FileMode.CreateNew))
                using (var writer = new StreamWriter(file)) writer.Write(JsonUtility.ToJson(particleClocks));
            }
            catch (Exception e)
            {
                report.passed = false;
                report.error = (report.error ?? "") + "\nParticle clock output failed: " + e;
            }
        }
        private static ParticleSample ReadParticle(ParticleSystem value)
        {
            if (!value) return null;
            ParticleSystem.MainModule main = value.main;
            return new ParticleSample { name = value.name, time = value.time, particleCount = value.particleCount,
                isAliveWithChildren = value.IsAlive(true), isPlaying = value.isPlaying, isPaused = value.isPaused,
                isStopped = value.isStopped, useUnscaledTime = main.useUnscaledTime, autoRandomSeed = value.useAutoRandomSeed,
                randomSeed = value.randomSeed, simulationSpeed = main.simulationSpeed,
                position = value.transform.position, localPosition = value.transform.localPosition, lossyScale = value.transform.lossyScale };
        }
        private static ParticleSnapshot ReadContinuousParticles()
        {
            var pool = (PixelEffectPool)Get(game, "effects");
            var instances = (List<PixelEffect>)Get(pool, "instances");
            var snapshot = new ParticleSnapshot { created = pool.Created, peakAlive = pool.PeakAlive, dropped = pool.Dropped };
            for (int i = 0; i < instances.Count; i++)
            {
                PixelEffect effect = instances[i];
                var row = new ParticleInstance { index = i, kind = effect.Kind,
                    logicalPosition = new[] { effect.LogicalPosition.x, effect.LogicalPosition.y },
                    root = ReadParticle((ParticleSystem)Get(effect, "root")),
                    tintLayers = Array.ConvertAll(effect.tintLayers, ReadParticle),
                    children = Array.ConvertAll(effect.GetComponentsInChildren<ParticleSystem>(), ReadParticle) };
                snapshot.instances.Add(row);
            }
            return snapshot;
        }
    }
}
#endif

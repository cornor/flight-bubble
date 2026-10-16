#if FLIGHTBEAT_CLI_PROBE && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Flightbeat.Feedback;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Flightbeat.Diagnostics
{
    public sealed partial class StartupWindowProbe
    {
        // Explicit diagnostic input adapter, never installed by ordinary Play.
        // f90111 locals 22/23 both receive the declared delta before the original
        // per-system unscaled selector. Unity's public capture clock only controls
        // scaled time. Select that exact input during the native particle phase,
        // restoring each module before game/render observation. This is an input
        // adapter experiment, not proof that every native flag/path is equivalent:
        // the original explicit-delta path also sets simulation flag bit 8.
        [Serializable] private sealed class ParticleStep
        {
            public int frame, declaredDeltaBits, selectedDeltaBits, originalUnscaledDeltaBits,
                systems, liveSystems, temporarilySelectedSystems;
            public bool inputExact, inactiveMismatch, modulesRestored;
        }
        [Serializable] private sealed class DeclaredParticleClockReport
        {
            public string scope = "Explicit CLI input-adapter experiment. Native Begin/EndUpdateAll callbacks remain installed. Temporarily select capture-controlled scaled time only during native particle update; all module flags and captureDeltaTime are restored. No Simulate, Play/Pause, transform, seed, emission or lifetime writes. Original explicit-delta native flag bit 8 is not directly available through this API and remains a scope limitation.";
            public bool playerLoopRestored, captureDeltaRestored;
            public List<ParticleStep> frames = new List<ParticleStep>();
        }
        private sealed class BeforeNativeParticles { }
        private sealed class AfterNativeParticles { }
        private static DeclaredParticleClockReport declaredParticleClock;
        private static PlayerLoopSystem originalPlayerLoop;
        private static bool particleLoopInstalled;
        private static float savedCaptureDelta;
        private static ParticleStep activeParticleStep;
        private static readonly List<ParticleSystem> selectedParticles = new List<ParticleSystem>();
        private static void InstallDeclaredParticleClock()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--flightbeat-declared-particle-clock") < 0) return;
            if (drawDirectory == null) throw new ArgumentException("Declared particle clock requires continuous draw observations");
            float dt = data.continuous.frames[0].dt;
            if (!(dt > 0f) || Single.IsInfinity(dt)) throw new ArgumentException("Positive original delta required");
            foreach (var value in data.continuous.frames)
                if (BitConverter.SingleToInt32Bits(value.dt) != BitConverter.SingleToInt32Bits(dt)) throw new ArgumentException("This adapter requires a declared constant-delta route");
            if (Time.timeScale != 1f) throw new Exception("Non-unit timeScale is outside this adapter");
            originalPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            int before = 0, after = 0;
            PlayerLoopSystem replacement = InsertParticleClock(originalPlayerLoop, ref before, ref after);
            if (before != 1 || after != 1) throw new Exception("Ambiguous native particle PlayerLoop nodes");
            savedCaptureDelta = Time.captureDeltaTime;
            declaredParticleClock = new DeclaredParticleClockReport();
            Time.captureDeltaTime = dt;
            PlayerLoop.SetPlayerLoop(replacement); particleLoopInstalled = true;
        }
        private static PlayerLoopSystem InsertParticleClock(PlayerLoopSystem node, ref int before, ref int after)
        {
            if (node.subSystemList == null) return node;
            var children = new List<PlayerLoopSystem>();
            foreach (var child in node.subSystemList)
            {
                if (child.type == typeof(PreLateUpdate.ParticleSystemBeginUpdateAll))
                { before++; children.Add(new PlayerLoopSystem { type = typeof(BeforeNativeParticles), updateDelegate = SelectDeclaredParticleInput }); }
                children.Add(InsertParticleClock(child, ref before, ref after));
                if (child.type == typeof(PostLateUpdate.ParticleSystemEndUpdateAll))
                { after++; children.Add(new PlayerLoopSystem { type = typeof(AfterNativeParticles), updateDelegate = RestoreParticleInputSelection }); }
            }
            node.subSystemList = children.ToArray();
            return node;
        }
        private static void SelectDeclaredParticleInput()
        {
            if (finished || loop == null) return;
            try
            {
                if (activeParticleStep != null || selectedParticles.Count != 0) throw new Exception("Unclosed particle input selection");
                if (Time.timeScale != 1f || Time.captureDeltaTime != loop.dt) throw new Exception("Declared engine clock changed");
                var pool = (PixelEffectPool)Get(game, "effects");
                var all = new List<ParticleSystem>();
                foreach (var effect in (List<PixelEffect>)Get(pool, "instances")) all.AddRange(effect.GetComponentsInChildren<ParticleSystem>());
                var step = new ParticleStep { frame = loopCursor, systems = all.Count,
                    declaredDeltaBits = BitConverter.SingleToInt32Bits(loop.dt), selectedDeltaBits = BitConverter.SingleToInt32Bits(Time.deltaTime),
                    originalUnscaledDeltaBits = BitConverter.SingleToInt32Bits(Time.unscaledDeltaTime) };
                foreach (var system in all)
                    if (system.isPlaying || system.isPaused || system.particleCount != 0) step.liveSystems++;
                step.inputExact = step.declaredDeltaBits == step.selectedDeltaBits;
                step.inactiveMismatch = !step.inputExact && step.liveSystems == 0;
                declaredParticleClock.frames.Add(step); activeParticleStep = step;
                if (!step.inputExact && !step.inactiveMismatch) throw new Exception("Actual native particle delta differs from declared input");
                foreach (var system in all)
                {
                    var main = system.main;
                    if (main.useUnscaledTime) { selectedParticles.Add(system); main.useUnscaledTime = false; }
                }
                step.temporarilySelectedSystems = selectedParticles.Count;
            }
            catch (Exception e) { Finish(e.ToString()); }
        }
        private static void RestoreParticleInputSelection()
        {
            foreach (var system in selectedParticles)
                if (system) { var main = system.main; main.useUnscaledTime = true; }
            foreach (var system in selectedParticles)
                if (system && !system.main.useUnscaledTime) throw new Exception("Particle time selector restoration failed");
            selectedParticles.Clear();
            if (activeParticleStep != null) activeParticleStep.modulesRestored = true;
            activeParticleStep = null;
        }
        private static void ResetDeclaredParticleClock()
        {
            if (declaredParticleClock == null) return;
            try
            {
                RestoreParticleInputSelection();
                if (particleLoopInstalled) { PlayerLoop.SetPlayerLoop(originalPlayerLoop); particleLoopInstalled = false; }
                declaredParticleClock.playerLoopRestored = true;
                Time.captureDeltaTime = savedCaptureDelta;
                declaredParticleClock.captureDeltaRestored = BitConverter.SingleToInt32Bits(Time.captureDeltaTime) == BitConverter.SingleToInt32Bits(savedCaptureDelta);
                using (var file = new FileStream(Path.Combine(drawDirectory, "declared-particle-clock.json"), FileMode.CreateNew))
                using (var writer = new StreamWriter(file)) writer.Write(JsonUtility.ToJson(declaredParticleClock));
            }
            catch (Exception e) { report.passed = false; report.error = (report.error ?? "") + "\nParticle clock restoration/output failed: " + e; }
        }
    }
}
#endif

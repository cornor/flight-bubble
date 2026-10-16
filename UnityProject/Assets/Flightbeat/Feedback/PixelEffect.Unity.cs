using UnityEngine;

namespace Flightbeat.Feedback
{
    // Explicit engine boundaries for checking translated instruction order.
    public interface IPixelEffectRuntime
    {
        bool Exists(ParticleSystem particle);
        bool IsAlive(ParticleSystem particle, bool children);
        ParticleSystem[] Children(PixelEffect effect);
        ParticleSystem Component(PixelEffect effect);
        int ParticleCount(ParticleSystem particle);
        float Time(ParticleSystem particle);
        Transform Transform(PixelEffect effect);
        void SetPosition(Transform transform, Vector3 value);
        void SetScale(Transform transform, Vector3 value);
        void SetStartColor(ParticleSystem particle, ParticleSystem.MinMaxGradient value);
        void Stop(ParticleSystem particle, bool children, ParticleSystemStopBehavior behavior);
        void Pause(ParticleSystem particle, bool children);
        void Play(ParticleSystem particle, bool children);
    }

    internal sealed class UnityPixelEffectRuntime : IPixelEffectRuntime
    {
        internal static readonly UnityPixelEffectRuntime Instance = new UnityPixelEffectRuntime();
        public bool Exists(ParticleSystem particle) => particle;
        public bool IsAlive(ParticleSystem particle, bool children) => particle.IsAlive(children);
        public ParticleSystem[] Children(PixelEffect effect) => effect.GetComponentsInChildren<ParticleSystem>();
        public ParticleSystem Component(PixelEffect effect) => effect.GetComponent<ParticleSystem>();
        public int ParticleCount(ParticleSystem particle) => particle.particleCount;
        public float Time(ParticleSystem particle) => particle.time;
        public Transform Transform(PixelEffect effect) => effect.transform;
        public void SetPosition(Transform transform, Vector3 value) => transform.position = value;
        public void SetScale(Transform transform, Vector3 value) => transform.localScale = value;
        public void SetStartColor(ParticleSystem particle, ParticleSystem.MinMaxGradient value)
        {
            ParticleSystem.MainModule main = particle.main;
            main.startColor = value;
        }
        public void Stop(ParticleSystem particle, bool children, ParticleSystemStopBehavior behavior) => particle.Stop(children, behavior);
        public void Pause(ParticleSystem particle, bool children) => particle.Pause(children);
        public void Play(ParticleSystem particle, bool children) => particle.Play(children);
    }
}

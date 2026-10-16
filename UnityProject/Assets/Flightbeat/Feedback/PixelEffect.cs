using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed class PixelEffect : MonoBehaviour
    {
        // Source serialized field and private layout: PixelWing.PixelEffect.
        public ParticleSystem[] tintLayers;
        private ParticleSystem root;
        private IPixelEffectRuntime runtime;
        private IPixelEffectRuntime Engine => runtime ?? UnityPixelEffectRuntime.Instance;

        // src: f6993 / f8235 (these bodies also have unrelated compiler aliases).
        public string Kind { get; private set; }
        // src: f8350 / f8351; placement does not modify this logical coordinate.
        public Vector2 LogicalPosition { get; set; }

        // src: f54367. Re-read root after the Unity object liveness check.
        public bool Alive => Engine.Exists(root) && Engine.IsAlive(root, true);

        // src: f54368. The original i32 addition wraps; children are captured once.
        public int ParticleCount
        {
            get
            {
                int count = 0;
                ParticleSystem[] children = Engine.Children(this);
                for (int i = 0; i < children.Length; i++)
                    count = unchecked(Engine.ParticleCount(children[i]) + count);
                return count;
            }
        }

        // src: f54369. Sum every returned child in order using f32 additions.
        public float SimulationTime
        {
            get
            {
                float time = 0f;
                ParticleSystem[] children = Engine.Children(this);
                for (int i = 0; i < children.Length; i++) time = time + Engine.Time(children[i]);
                return time;
            }
        }

        // src: f54370. Kind is stored before GetComponent; root after it returns.
        public void Initialize(string kind)
        {
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            if (GetComponent<Flightbeat.Garden.GardenParticleVisibility>() == null)
                gameObject.AddComponent<Flightbeat.Garden.GardenParticleVisibility>();
#endif
            Kind = kind;
            root = Engine.Component(this);
        }

        // src: f54371 and original MinMaxGradient(Color) f54233.
        public void PlayAt(Vector3 position, float scale, Color tint)
        {
            Engine.Stop(root, true, ParticleSystemStopBehavior.StopEmittingAndClear);
            Engine.SetPosition(Engine.Transform(this), position);
            Engine.SetScale(Engine.Transform(this), Vector3.one * scale);
            ParticleSystem[] layers = tintLayers;
            for (int i = 0; i < layers.Length; i++)
                Engine.SetStartColor(layers[i], new ParticleSystem.MinMaxGradient(tint));
            Engine.Play(root, true);
        }

        // src: f54372. Two separate transform queries; no particle or logical-state update.
        public void PlaceAt(Vector3 position, float scale)
        {
            Engine.SetPosition(Engine.Transform(this), position);
            Engine.SetScale(Engine.Transform(this), Vector3.one * scale);
        }

        // src: f54373. A dead effect is neither paused nor restarted.
        public void Freeze(bool paused)
        {
            if (!Alive) return;
            if (paused) Engine.Pause(root, true);
            else Engine.Play(root, true);
        }

        // src: f54374. No extra null/liveness guard in the original.
        public void Clear() => Engine.Stop(root, true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // src: shared f18483 delegates to the engine base constructor; all fields
        // retain CLR defaults. Native MonoBehaviour construction remains unverified.
    }
}

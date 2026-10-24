#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Game;
using Flightbeat.Feedback;
using UnityEngine;

namespace Flightbeat.Garden
{
    // Adapt particle placement at the existing native boundary; the particle pool stays frozen.
    internal sealed class PlushEffectPoolRuntime : IPixelEffectPoolRuntime
    {
        readonly IPixelEffectPoolRuntime native = UnityPixelEffectPoolRuntime.Instance;
        public GameObject LoadPrefab(string path) => native.LoadPrefab(path);
        public bool Exists(Object value) => native.Exists(value);
        public GameObject Instantiate(GameObject prefab, Transform parent) => native.Instantiate(prefab, parent);
        public PixelEffect GetEffect(GameObject go) => native.GetEffect(go);
        public GameObject GameObjectOf(PixelEffect effect) => native.GameObjectOf(effect);
        public void Destroy(Object value) => native.Destroy(value);
        public void LogError(string value) => native.LogError(value);
        public Vector3 LossyScale(Transform value) => native.LossyScale(value);
        public Vector3 TransformPoint(Transform transform, Vector3 point)
        {
            if (PlushLayout.Enabled)
                point.y = ScreenLayout.Height - PlushLayout.Y(ScreenLayout.InverseY(ScreenLayout.Height - point.y));
            return native.TransformPoint(transform, point);
        }
    }
}
#endif

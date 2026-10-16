using UnityEngine;

namespace Flightbeat.Feedback
{
    public interface IPixelEffectPoolRuntime
    {
        GameObject LoadPrefab(string path);
        bool Exists(Object value);
        GameObject Instantiate(GameObject prefab, Transform parent);
        PixelEffect GetEffect(GameObject gameObject);
        GameObject GameObjectOf(PixelEffect effect);
        void Destroy(Object value);
        void LogError(string message);
        Vector3 LossyScale(Transform transform);
        Vector3 TransformPoint(Transform transform, Vector3 point);
    }

    internal sealed class UnityPixelEffectPoolRuntime : IPixelEffectPoolRuntime
    {
        internal static readonly UnityPixelEffectPoolRuntime Instance = new UnityPixelEffectPoolRuntime();
        public GameObject LoadPrefab(string path) => Resources.Load<GameObject>(path);
        public bool Exists(Object value) => value;
        public GameObject Instantiate(GameObject prefab, Transform parent) => Object.Instantiate(prefab, parent);
        public PixelEffect GetEffect(GameObject gameObject) => gameObject.GetComponent<PixelEffect>();
        public GameObject GameObjectOf(PixelEffect effect) => effect.gameObject;
        public void Destroy(Object value) => Object.Destroy(value);
        public void LogError(string message) => Debug.LogError(message);
        public Vector3 LossyScale(Transform transform) => transform.lossyScale;
        public Vector3 TransformPoint(Transform transform, Vector3 point) => transform.TransformPoint(point);
    }
}

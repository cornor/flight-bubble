#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using UnityEngine;
namespace Flightbeat.Garden {
    // Authored particles still simulate for the original feedback/audio contracts.
    // Only their renderers are hidden during the R1 graybox battle.
    public sealed class GardenParticleVisibility : MonoBehaviour {
        Renderer[] layers;
        void Awake() { layers = GetComponentsInChildren<Renderer>(true); }
        void LateUpdate() { foreach (var layer in layers) layer.enabled = !GardenSkin.HideOriginalParticles; }
    }
}
#endif

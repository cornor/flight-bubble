#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Garden.UI
{
    // Input stays in PixelWingGame.PointerDown/Tap. Do not add a second EventSystem
    // path: that would consume a prop twice or deploy a plant behind a button.
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Flightbeat/Garden UI/Action")]
    public sealed class GardenUiAction : MonoBehaviour
    {
        [Tooltip("Existing OnBattleUi ID. Zero blocks input behind this rectangle.")]
        public int actionId;
        public bool interactable = true;
        public Image selectedVisual;
        public Vector2 selectedOffset = new Vector2(0, -2);
        public Color selectedTint = new Color(.78f, .74f, .7f, 1);
        bool selected, initialized;
        Color normalColor;
        public bool Contains(Vector3 world)
            => isActiveAndEnabled && interactable && ((RectTransform)transform).rect.Contains(transform.InverseTransformPoint(world));
        public void SetSelected(bool value)
        {
            if (!selectedVisual || (initialized && selected == value)) return;
            if (!initialized) { normalColor = selectedVisual.color; initialized = true; }
            if (selected != value)
                selectedVisual.rectTransform.anchoredPosition += (value ? 1 : -1) * selectedOffset;
            selected = value;
            selectedVisual.color = value ? normalColor * selectedTint : normalColor;
        }
    }
}
#endif

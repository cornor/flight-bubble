using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Game
{
    // Native Unity / bundled UGUI API boundary, never a replacement game rule.
    public interface IDisplayRuntime
    {
        void SetScaleFactor(CanvasScaler scaler, float value);
        void SetSizeDelta(RectTransform rect, Vector2 value);
        void SetAnchoredPosition(RectTransform rect, Vector2 value);
        bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 point, Camera camera, out Vector2 local);
        void SetVerticesDirty(PixelSurface surface);
    }

    internal sealed class UnityDisplayRuntime : IDisplayRuntime
    {
        public void SetScaleFactor(CanvasScaler scaler, float value) => scaler.scaleFactor = value;
        public void SetSizeDelta(RectTransform rect, Vector2 value) => rect.sizeDelta = value;
        public void SetAnchoredPosition(RectTransform rect, Vector2 value) => rect.anchoredPosition = value;
        public bool ScreenPointToLocalPointInRectangle(RectTransform rect, Vector2 point, Camera camera, out Vector2 local) =>
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, point, camera, out local);
        public void SetVerticesDirty(PixelSurface surface) => surface.SetVerticesDirty();
    }
}

using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        private CanvasScaler mainScale, hudScale; // original +428 / +432
        internal PixelSurface surface, hud; // +444 / +448
        internal RectTransform canvasRect, hudRect; // +456 / +460
        internal Camera gameCamera; // +464
        private IDisplayRuntime displayRuntime;
        private IDisplayRuntime DisplayRuntime => displayRuntime ?? (displayRuntime = new UnityDisplayRuntime());

        // src: f54716. mainScale and scale are held across the first setter;
        // both rect references are captured before processing either rect.
        internal void ApplyDisplay(int width, int height)
        {
            Rect safe = WechatBridge.SafeArea;
            Rect menu = WechatBridge.Menu;
            ScreenLayout.Configure(width, height, safe, menu);
            CanvasScaler heldMain = mainScale;
            float heldScale = ScreenLayout.Scale;
            DisplayRuntime.SetScaleFactor(hudScale, heldScale);
            DisplayRuntime.SetScaleFactor(heldMain, heldScale);
            RectTransform[] rects = { canvasRect, hudRect };
            foreach (RectTransform rect in rects)
            {
                DisplayRuntime.SetSizeDelta(rect, new Vector2(540f, ScreenLayout.Height));
                DisplayRuntime.SetAnchoredPosition(rect, new Vector2(-270f, OriginalMath.Binary32(ScreenLayout.Height * -0.5f)));
            }
            surface.Redraw();
            hud.Redraw();
        }

        // src: f54785. The native bool is discarded, even when it is false.
        private Vector2 ScreenLogical(Vector2 point)
        {
            DisplayRuntime.ScreenPointToLocalPointInRectangle(canvasRect, point, gameCamera, out Vector2 local);
            local.y = ScreenLayout.InverseY(OriginalMath.Binary32(ScreenLayout.Height - local.y));
#if UNITY_EDITOR
            FrameProjectionObserver?.Invoke(point, local);
#endif
            return local;
        }
    }
}

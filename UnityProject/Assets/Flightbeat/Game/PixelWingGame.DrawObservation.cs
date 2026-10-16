using Flightbeat.Rendering;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        partial void ObserveHitBegin(PixelSurface s, int id, float x, float y, float w, float h);
        partial void ObserveHitEnd();
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
        private DrawObservation.Hit observedHit;
        private int observedHitBefore;
        partial void ObserveHitBegin(PixelSurface s, int id, float x, float y, float w, float h)
        {
            if (DrawObservation.Current == null) return;
            if (observedHit != null) throw new System.InvalidOperationException("Nested HitBox");
            observedHitBefore = uiHitsBuild.Count;
            observedHit = DrawObservation.Current.BeginHit(s, id, x, y, w, h);
        }
        partial void ObserveHitEnd()
        {
            if (DrawObservation.Current == null) return;
            int added = uiHitsBuild.Count - observedHitBefore;
            if (observedHit == null || (added != 0 && added != 1)) throw new System.InvalidOperationException("Unexpected HitBox append count");
            observedHit.accepted = added == 1;
            if (added == 1)
            {
                var hit = uiHitsBuild[uiHitsBuild.Count - 1];
                observedHit.registeredId = hit.id;
                observedHit.registeredBits = new[] { DrawObservation.Bits(hit.x), DrawObservation.Bits(hit.y),
                    DrawObservation.Bits(hit.w), DrawObservation.Bits(hit.h) };
            }
            observedHit = null;
        }
#endif
    }
}

#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // 2026-10-16: display-only smoothing of the formation's downward advance. The model (protected) moves the
        // whole grid in pulses: every AdvanceCycle (1 s) it adds CurrentSpeed * cycle (~1-2 px) within 0.24 s and
        // then holds, which read as the page "refreshing" once a second. The beads are drawn at an offset that
        // follows the model at a steady pace slightly above its average speed, so the motion is continuous and
        // never lags more than one step (a fraction of a bead). Pushes back up (fusion, empty lane, revive) are
        // already eased by the model and are followed exactly; large jumps (new level, intro) snap.
        // Only drawing reads this value; targeting, hits and every rule keep using Battle.Offset.
        Flightbeat.Model.Battle advanceBattle;
        float advanceShown, advanceAt;
        float GardenFormationOffset()
        {
            float target = Battle.Offset, dt = Mathf.Clamp(ui - advanceAt, 0f, .1f);
            advanceAt = ui;
            if (advanceBattle != Battle || Battle.State == Phase.Intro || target < advanceShown
                || target - advanceShown > Battle.PitchY * 2f)
            {
                advanceBattle = Battle; advanceShown = target; return target;
            }
            float rate = Mathf.Max(Battle.CurrentSpeed, 1f) * 1.3f;
            advanceShown = Mathf.MoveTowards(advanceShown, target, rate * dt);
            return advanceShown;
        }
    }
}
#endif

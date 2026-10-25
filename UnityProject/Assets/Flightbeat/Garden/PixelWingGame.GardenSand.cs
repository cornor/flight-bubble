#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using System.Reflection;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // 2026-10-21 (user: beads coming down from the top "jump", or land on the bead below and are then pulled
        // back to their place; nothing like rolling down). Causes: the model moves a sand cell a whole row per pass
        // and eases its x on its own ~0.1 s clock, while the old display (UpdateShownSand) moved y linearly at
        // 26 rows/s, so on a diagonal slide the bead dropped straight onto the bead below and slid sideways after;
        // moves of more than 6 rows and upward moves snapped; stream cells started from a stale y.
        // Display only (the model is protected): every bead falls with gravity to its row, a diagonal slide moves
        // sideways ahead of the fall so the bead rolls off the shoulder of the one below, stream beads drop in from
        // under the cloud ceiling, and a hard landing squashes the bead a little. Rules still read the model;
        // shots aim at the drawn bead (GardenEnemyAt).
        static readonly FieldInfo SandPositionField = typeof(Flightbeat.Model.Battle).GetField("position",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Flightbeat.Model.Battle sandBattle;
        int[] sandPosition;
        float[] sandX, sandY, sandVy, sandLandAt, sandLandK;
        bool[] sandSeen;
        float sandAt, sandTime;
        bool sandSquashing;

        float SandColumnX(int i)
        {
            var level = Battle.Level;
            if (sandPosition == null || i >= sandPosition.Length || sandPosition[i] < 0 || level.columns <= 0)
                return Battle.EnemyX(i);
            float pitch = level.pitchX > 0f ? level.pitchX : 10.5f, origin = level.pitchX > 0f ? level.gridX : 65f;
            return origin + pitch * (sandPosition[i] % level.columns);
        }

        // Called before canvas rebuilds. Report actual display changes so a settled
        // board can keep its mesh while characters, hints and effects animate.
        bool UpdateGardenSand(float offset)
        {
            int n = Battle.Alive.Length;
            float dt = Mathf.Clamp(ui - sandAt, 0f, .05f);
            sandAt = ui;
            bool reset = sandBattle != Battle || sandY == null || sandY.Length != n || Battle.Time < sandTime;
            bool changed = reset || sandSquashing;
            sandSquashing = false;
            sandTime = Battle.Time;
            if (reset)
            {
                sandBattle = Battle;
                sandPosition = SandPositionField != null ? SandPositionField.GetValue(Battle) as int[] : null;
                sandX = new float[n]; sandY = new float[n]; sandVy = new float[n];
                sandLandAt = new float[n]; sandLandK = new float[n]; sandSeen = new bool[n];
            }
            float pitch = Battle.PitchY;
            float pitchX = Battle.Level.pitchX > 0f ? Battle.Level.pitchX : 10.5f;
            float gravity = pitch * 220f, fastest = pitch * 40f;
            // The logical row hidden under the cloud ceiling (screen y ~104) for beads that pour in.
            float ceiling = (104f - 562f) / ScreenLayout.EnemyScaleY + 562f - offset;
            bool snap = reset || Battle.State == Phase.Intro;
            for (int i = 0; i < n; i++)
            {
                if (!Battle.Alive[i] || !Battle.OnBoard(i)) {
                    changed |= sandSeen[i]; sandSeen[i] = false; continue;
                }
                float ty = Battle.GridY(i), tx = SandColumnX(i);
                if (snap)
                {
                    changed = true;
                    sandSeen[i] = true; sandX[i] = tx; sandY[i] = ty; sandVy[i] = 0f; sandLandAt[i] = -9f;
                    continue;
                }
                if (!sandSeen[i])
                {
                    changed = true;
                    sandSeen[i] = true; sandX[i] = tx; sandY[i] = Mathf.Min(ty - pitch, ceiling);
                    sandVy[i] = pitch * 4f; sandLandAt[i] = -9f;
                }
                float age = ui - sandLandAt[i];
                sandSquashing |= age >= 0f && age <= .16f;
                // Exact equality: do not truncate a slow fall or a landing animation.
                if (sandX[i] == tx && sandY[i] == ty && sandVy[i] == 0f) continue;
                float oldX = sandX[i], oldY = sandY[i];
                float dy = ty - sandY[i], dx = tx - sandX[i];
                if (Mathf.Abs(dx) > pitchX * 3f) { sandX[i] = tx; dx = 0f; }
                if (dy > .001f)
                {
                    float v = Mathf.Min(Mathf.Max(sandVy[i], pitch * 3f) + gravity * dt, fastest);
                    float fall = Mathf.Min(v * dt, dy);
                    // Sideways first: x covers its gap by the time y has covered 40% of the remaining fall.
                    sandX[i] += dx * Mathf.Min(1f, fall * 2.5f / dy);
                    sandY[i] += fall;
                    if (fall >= dy)
                    {
                        sandY[i] = ty; sandX[i] = tx; sandVy[i] = 0f;
                        if (v > pitch * 10f) { sandLandAt[i] = ui; sandLandK[i] = Mathf.Clamp01(v / fastest); sandSquashing = true; }
                    }
                    else sandVy[i] = v;
                }
                else
                {
                    sandVy[i] = 0f;
                    if (dy < -pitch * 12f) sandY[i] = ty;
                    else sandY[i] = Mathf.MoveTowards(sandY[i], ty, Mathf.Max(-dy * 14f, pitch * 6f) * dt);
                    sandX[i] = Mathf.MoveTowards(sandX[i], tx, Mathf.Max(Mathf.Abs(dx) * 16f, pitchX * 5f) * dt);
                }
                changed |= oldX != sandX[i] || oldY != sandY[i];
            }
            return changed || sandSquashing;
        }

        // Drawn grid position of a bead (before the formation offset); false when the display has no state for it.
        bool GardenSandAt(int i, out float x, out float y)
        {
            if (sandBattle == Battle && sandSeen != null && i >= 0 && i < sandSeen.Length && sandSeen[i])
            {
                x = sandX[i]; y = sandY[i]; return true;
            }
            x = Battle.EnemyX(i);
            y = shownY != null && i < shownY.Length ? shownY[i] : Battle.GridY(i);
            return false;
        }

        // Landing squash: 0 at rest, up to ~0.14 just after a fast landing.
        float GardenSandSquash(int i)
        {
            if (sandBattle != Battle || sandLandAt == null || i >= sandLandAt.Length) return 0f;
            float age = ui - sandLandAt[i];
            if (age < 0f || age > .16f) return 0f;
            return Mathf.Sin(age / .16f * Mathf.PI) * .14f * sandLandK[i];
        }
    }
}
#endif

#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        PixelSurface gardenBoard;
        Battle gardenBoardBattle;
        float boardOffset, boardShake, boardWidth, boardHeight;
        Vector3 boardLayout;
        Phase boardPhase;
        bool boardMapped, boardAiming;

        void InitializeGardenBoard()
        {
            // A separate sub-canvas retains the large bead mesh independently of
            // the animated queue/rail. Keep it BEFORE the old surface: the original
            // painter order was board rim, beads, rail, characters, then HUD/effects.
            var go = new GameObject("Retained bubble board", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasRenderer), typeof(PixelSurface));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvasRect.parent, false);
            rect.SetSiblingIndex(canvasRect.GetSiblingIndex());
            rect.anchorMin = canvasRect.anchorMin; rect.anchorMax = canvasRect.anchorMax;
            rect.pivot = canvasRect.pivot;
            rect.sizeDelta = canvasRect.sizeDelta; rect.anchoredPosition = canvasRect.anchoredPosition;
            // Inherit sorting and pixel-perfect settings from the same parent canvas.
            go.GetComponent<Canvas>().additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            gardenBoard = go.GetComponent<PixelSurface>();
            gardenBoard.Paint = DrawGardenBoard;
        }

        void RefreshGardenBoard()
        {
            if (!gardenBoard) return; // Standalone drawing fixtures use the immediate path.
            bool active = GardenUiActive;
            if (gardenBoard.gameObject.activeSelf != active) gardenBoard.gameObject.SetActive(active);
            if (!active) return;
            var rect = gardenBoard.rectTransform;
            if (rect.sizeDelta != canvasRect.sizeDelta) rect.sizeDelta = canvasRect.sizeDelta;
            if (rect.anchoredPosition != canvasRect.anchoredPosition) rect.anchoredPosition = canvasRect.anchoredPosition;
            gardenBoard.Lighting = surface.Lighting;
            // Runs in the existing UI LateUpdate, not inside OnPopulateMesh:
            // dirtying a Graphic during a canvas rebuild can defer it by a frame.
            if (PrepareGardenBoard()) gardenBoard.Redraw();
        }

        bool PrepareGardenBoard(bool force = false)
        {
            float offset = GardenFormationOffset();
            feedback.GardenOffsetDelta = offset - Battle.Offset;
            float pitchX = (Battle.Level.pitchX > 0f ? Battle.Level.pitchX : 10.5f) * ScreenLayout.EnemyScaleX;
            float pitchY = Battle.PitchY * ScreenLayout.EnemyScaleY;
            float beadW = pitchX * 1.05f / .94f, beadH = Mathf.Min(beadW, pitchY * 1.3f / .94f);
            feedback.GardenBeadSize = beadH;
            float shake = feedback.GardenStrikeShake;
            bool sandChanged = UpdateGardenSand(offset);
            var layout = new Vector3(ScreenLayout.Height, ScreenLayout.Top, ScreenLayout.Bottom);
            bool aiming = selection == 2;
            // Exact values rather than epsilon comparisons: subpixel movement,
            // final landing frames and changing aim highlights must all survive.
            bool dirty = force || gardenBoardBattle != Battle || sandChanged || offset != boardOffset
                || shake != boardShake || beadW != boardWidth || beadH != boardHeight
                || !layout.Equals(boardLayout) || PlushLayout.Enabled != boardMapped
                || Battle.State != boardPhase || Battle.State == Phase.Intro || aiming || boardAiming;
            if (!dirty) return false;
            gardenBoardBattle = Battle; boardOffset = offset; boardShake = shake;
            boardWidth = beadW; boardHeight = beadH; boardLayout = layout;
            boardMapped = PlushLayout.Enabled; boardPhase = Battle.State; boardAiming = aiming;
            gardenBeadDraws.Clear();
            for (int i = 0; i < Battle.Alive.Length; i++) {
                if (!Battle.Alive[i] || !Battle.OnBoard(i)) continue;
                GardenSandAt(i, out float gx, out float gy);
                Vector2 target = ScreenLayout.EnemyPoint(gx, gy + offset), at = target;
                if (Battle.State == Phase.Intro) {
                    if (FleetOpening.Progress(i, Battle.IntroTime, target) <= 0) continue;
                    at = FleetOpening.EnemyPosition(i, Battle.IntroTime, target);
                }
                if (at.y < 114 || at.y > 572 || at.x < 12 || at.x > 528) continue;
                at.y += shake;
                float squash = GardenSandSquash(i);
                if (squash > 0f) at.y += beadH * squash * .5f;
                gardenBeadDraws.Add(new GardenBeadDraw { index = i, at = at, squash = squash });
            }
            // A settled/grid-ordered array is already in painter order. Flow and
            // streamed replacements can reorder it; retain the exact old comparator.
            for (int i = 1; i < gardenBeadDraws.Count; i++) {
                if (GardenBeadOrder(gardenBeadDraws[i - 1], gardenBeadDraws[i]) <= 0) continue;
                gardenBeadDraws.Sort(GardenBeadOrder); break;
            }
            return true;
        }

        void DrawGardenBoard(PixelSurface s)
        {
            s.MapLayout = false;
            float top = PlushLayout.Y(114f) - 10f, bottom = PlushLayout.Y(562f) + 10f;
            Asset9(s, "scene/plush-board", 2f, top, 536f, bottom - top, 27f, 27f, 18f, 18f, Color.white);
            s.MapLayout = true;
            for (int i = 0; i < gardenBeadDraws.Count; i++) {
                GardenBeadDraw bead = gardenBeadDraws[i];
                Color tint = GardenColor(Battle.ColorAt(bead.index));
                if (selection == 2 && InBombCircle(bead.at))
                    tint = Color.Lerp(tint, Color.white, .3f + .2f * Mathf.Sin(ui * 7f));
                // Original two layers and dimensions: preserve highlights and overlap.
                Bug(s, tint, bead.at, boardHeight * (1f - bead.squash), false, boardWidth * (1f + bead.squash));
            }
        }
    }
}
#endif

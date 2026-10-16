using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54804. Background, deck, enemy cells, then damage border, in original
        // draw order. All coordinates are logical until PixelSurface maps them.
        private void Draw(PixelSurface s)
        {
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            Flightbeat.Garden.GardenSkin.HideOriginalParticles = !InHome && levelIndex < 4 && Battle.ActiveFinale == null;
            if (!InHome && levelIndex < 4) { DrawGarden(s); return; }
#endif
            if (InHome) { DrawDeckBackground(s); return; }
            s.MapLayout = false;
            DrawSpace(s);
            s.MapLayout = true;
            DrawDeck(s);
            float pitchY = Battle.Level.pitchY, pitchX = Battle.Level.pitchX;
            Phase phase = Battle.State;
            float flash = feedback.FrontFlash, frontY = feedback.FrontScreenY, shake = feedback.Shake;
            if (Battle.Alive.Length > 0)
            {
                float width = OriginalMath.Binary32(OriginalMath.Binary32(pitchX * 1.02f) * ScreenLayout.EnemyScaleX);
                float height = OriginalMath.Binary32(OriginalMath.Binary32(pitchY * 1.02f) * 1.31f);
                bool compact = width < 9f || height < 8f;
                int spriteWidth = compact ? 5 : 9, spriteHeight = compact ? 5 : 8;
                float minY = phase != Phase.Intro ? 114f : -80f;
                float trailWidth = OriginalMath.Binary32(width * .3f), trailOffset = OriginalMath.Binary32(width * .35f);
                float baseHeight = OriginalMath.Binary32(height * .92f), baseWidth = OriginalMath.Binary32(width * .93f);
                float halfHeight = OriginalMath.Binary32(height * .5f), halfWidth = OriginalMath.Binary32(width * .5f);
                float flashStrength = OriginalMath.Binary32(flash * .75f), flashLine = OriginalMath.Binary32(frontY + -26f), trailHeight = OriginalMath.Binary32(height + height);
                for (int i = 0; i < Battle.Alive.Length; i++)
                {
                    if (!Battle.Alive[i] || !Battle.OnBoard(i)) continue;
                    float gridY = shownY != null && i < shownY.Length ? shownY[i] : Battle.GridY(i);
                    Vector2 target = ScreenLayout.EnemyPoint(Battle.EnemyX(i), OriginalMath.Binary32(gridY + Battle.Offset));
                    Vector2 position = target;
                    float landing = 0f;
                    if (phase == Phase.Intro)
                    {
                        position = FleetOpening.EnemyPosition(i, Battle.IntroTime, target);
                        landing = FleetOpening.Landing(i, Battle.IntroTime, target);
                        if (FleetOpening.Progress(i, Battle.IntroTime, target) <= 0f) continue;
                    }
                    float screenY = OriginalMath.Binary32(shake + position.y);
                    if (screenY > 572f || screenY < minY || position.x < 12f || position.x > 528f) continue;
                    Color color = colors[Battle.ColorAt(i)];
                    if (landing > 0f) color = Mix(color, Color.white, OriginalMath.Binary32(landing * .8f));
                    if (flash > 0f && target.y > flashLine)
                    {
                        float amount = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(frontY - target.y)) / -26f) + 1f);
                        amount = amount < 0f ? 0f : OriginalMath.WasmMin(amount, 1f);
                        color = Mix(color, Color.white, OriginalMath.Binary32(flashStrength * amount));
                    }
                    if (Battle.Pending(i)) color = Mix(color, Color.white, .12f);
                    if (selection == 2)
                    {
                        float offsetY = OriginalMath.Binary32(aim.y + -562f), offsetX = OriginalMath.Binary32(aim.x + -270f);
                        float dx = OriginalMath.Binary32((OriginalMath.Binary32(Battle.EnemyX(i) - (OriginalMath.Binary32(OriginalMath.Binary32(offsetX / ScreenLayout.EnemyScaleX) + 270f))))
                            / (OriginalMath.Binary32((OriginalMath.Binary32((aimMissile ? 150f : 120f) * (Battle != null ? OriginalMath.Binary32(Battle.Loadout.bombRadius + 1f) : 1f))) / ScreenLayout.EnemyScaleX)));
                        float dy = OriginalMath.Binary32((OriginalMath.Binary32(Battle.EnemyY(i) - (OriginalMath.Binary32(OriginalMath.Binary32(offsetY / 1.31f) + 562f))))
                            / (OriginalMath.Binary32((OriginalMath.Binary32((aimMissile ? 150f : 120f) * (Battle != null ? OriginalMath.Binary32(Battle.Loadout.bombRadius + 1f) : 1f))) / (OriginalMath.Binary32(ScreenLayout.Stretch * 1.31f)))));
                        if (OriginalMath.Binary32(OriginalMath.Binary32(dx * dx) + OriginalMath.Binary32(dy * dy)) <= 1f)
                            color = Mix(color, Color.white, OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(targetClock * 7f)) * .2f) + .3f));
                    }
                    float x = OriginalMath.RoundSingle(OriginalMath.Binary32(position.x - halfWidth));
                    float y = OriginalMath.RoundSingle(OriginalMath.Binary32(screenY - halfHeight));
                    EnemyMotion.Pose pose = EnemyMotion.Sample(i, Battle.ColorAt(i), Battle.Time, compact);
                    float drawnHeight = OriginalMath.Binary32(baseHeight * pose.Height), drawnWidth = OriginalMath.Binary32(baseWidth * pose.Width);
                    if (phase == Phase.Intro && landing <= 0f)
                    {
                        float progress = OriginalMath.Binary32(1f - FleetOpening.Progress(i, Battle.IntroTime, target));
                        if (progress > 0f)
                        {
                            float length = OriginalMath.Binary32(trailHeight * progress);
                            s.Rect(OriginalMath.Binary32(trailOffset + x), OriginalMath.Binary32(y - length), trailWidth, length, new Color(color.r, color.g, color.b, .35f));
                        }
                    }
                    s.Icon(pose.Sprite, OriginalMath.Binary32(x + OriginalMath.Binary32((OriginalMath.Binary32(width - drawnWidth)) * .5f)),
                        OriginalMath.Binary32(OriginalMath.Binary32(y + OriginalMath.Binary32((OriginalMath.Binary32(height - drawnHeight)) * .5f)) - OriginalMath.Binary32(height * pose.Lift)),
                        drawnWidth, drawnHeight, color, 0f, spriteWidth, spriteHeight);
                }
            }
            if (!(feedback.Hurt > 0f)) return;
            s.Corners(17f, 90f, 506f, 483f, new Color(1f, .35f, .1f, feedback.Hurt), 26, 4);
            s.Rect(20f, 572f, 500f, 3f, new Color(1f, 1f, 1f, feedback.Hurt));
        }
    }
}

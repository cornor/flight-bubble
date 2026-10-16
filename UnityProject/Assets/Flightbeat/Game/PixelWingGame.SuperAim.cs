using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // src: UpdateSuperAim f54797. Its named entry adds targetClock + dt;
        // original Update f54783 separately inlines dt + targetClock. Retain
        // both source operand orders, the suspended guard and the f32 store.
        private void UpdateSuperAim(float dt)
        {
            if (applicationSuspended) return;
            targetClock = OriginalMath.Binary32(targetClock + dt);
            if (rescueAimAt >= 0f && !paused && Battle.Time >= rescueAimAt && Battle.State == Phase.Playing)
            { rescueAimAt = -1f; BeginSuperAim(); }
        }

        // f54786 / callback f54875. Selection pauses the battle before any
        // inventory is consumed; a rescue missile can also start from Ready.
        private bool BeginSuperAim()
        {
            if (!evidenceControlled && Battle.Level.lesson > 0) return false;
            if (Battle.PendingStrike != null || Battle.ActiveFinale != null || Battle.TrainingHold) return false;
            if (Battle.State == Phase.Ready)
            {
                aimMissile = Battle.RescueMissiles > 0;
                if (!aimMissile) { Battle.Message("先点击战机出击"); audioChip.Play("deny"); return false; }
            }
            else if (Battle.State == Phase.Playing)
            {
                aimMissile = Battle.RescueMissiles > 0;
                if (!aimMissile && Economy.Data.items[2] <= 0)
                { TakeItem(2, () => BeginSuperAim()); return false; }
            }
            else return false;
            lastTargetTap = -10f;
            aimDown = false;
            settings = false;
            selection = 2;
            aim = BestAim();
            SetPaused(true);
            MarkUi(9);
            return true;
        }

        // f54813 / callback f54876. WAT divides by (Stretch * 1.31),
        // not by Stretch followed by multiplication; UI radius is screen-space.
        private bool ConfirmSuperStrike()
        {
            if (selection != 2) return false;
            if (!aimMissile && Economy.Data.items[2] <= 0)
            { TakeItem(2, () => ConfirmSuperStrike()); return false; }
            float x = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(aim.x + -270f)) / ScreenLayout.EnemyScaleX) + 270f);
            float y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(aim.y + -562f)) / 1.31f) + 562f);
            float size = aimMissile ? 150f : 120f;
            float factor = Battle != null ? OriginalMath.Binary32(Battle.Loadout.bombRadius + 1f) : 1f;
            float rx = OriginalMath.Binary32(OriginalMath.Binary32(size * factor) / ScreenLayout.EnemyScaleX);
            float ry = OriginalMath.Binary32(OriginalMath.Binary32(size * factor) / (OriginalMath.Binary32(ScreenLayout.Stretch * 1.31f)));
            SetPaused(false);
            if (!Battle.LaunchSuperStrike(x, y, rx, ry, aimMissile))
            {
                SetPaused(true);
                audioChip.Play("deny");
                Toast("准星内没有敌人", uiRed);
                return false;
            }
            if (!aimMissile) ConsumeItem(2);
            lastTargetTap = -10f;
            aimDown = false;
            selection = 0;
            Economy.CompleteBombTutorial();
            audioChip.Play("pilot-confirm");
            return true;
        }

        // f54502. This is the logical front before ScreenLayout.Y mapping.
        private float FormationFront()
        {
            float front = 0f;
            for (int cell = 0; cell < Battle.Alive.Length; cell++)
                if (Battle.Alive[cell] && Battle.OnBoard(cell))
                {
                    float y = ScreenLayout.EnemyPoint(Battle.EnemyX(cell), Battle.EnemyY(cell)).y;
                    front = y < front ? front : y;
                }
            return front;
        }

        // f54848. Preserve source candidate order and the strict score comparison.
        private Vector2 BestAim()
        {
            float front = FormationFront();
            float radius = OriginalMath.Binary32((aimMissile ? 150f : 120f) * (Battle != null ? OriginalMath.Binary32(Battle.Loadout.bombRadius + 1f) : 1f));
            float best = -1f, y = front;
            Vector2 result = new Vector2(270f, 400f);
            while (true)
            {
                y = OriginalMath.Binary32(y + OriginalMath.Binary32(radius * -.4f));
                float factor = Battle != null ? OriginalMath.Binary32(Battle.Loadout.bombRadius + 1f) : 1f;
                if (!(y > 150f) || !(y > OriginalMath.Binary32(front + OriginalMath.Binary32(OriginalMath.Binary32((aimMissile ? 150f : 120f) * factor) * -1.6f)))) break;
                float logicalY = y < 152f ? 152f : OriginalMath.WasmMin(y, 540f);
                float modelY = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(logicalY + -562f)) / 1.31f) + 562f), delta = OriginalMath.Binary32(y - front);
                for (int column = 40; column <= 490; column += 30)
                {
                    float logicalX = OriginalMath.WasmMin(OriginalMath.Binary32((float)column), 512f);
                    float modelX = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(logicalX + -270f)) / ScreenLayout.EnemyScaleX) + 270f);
                    float size = aimMissile ? 150f : 120f;
                    factor = Battle != null ? OriginalMath.Binary32(Battle.Loadout.bombRadius + 1f) : 1f;
                    int count = Battle.StrikeTargets(modelX, modelY, OriginalMath.Binary32(OriginalMath.Binary32(size * factor) / ScreenLayout.EnemyScaleX),
                        OriginalMath.Binary32(OriginalMath.Binary32(size * factor) / (OriginalMath.Binary32(ScreenLayout.Stretch * 1.31f))));
                    radius = OriginalMath.Binary32((aimMissile ? 150f : 120f) * (Battle != null ? OriginalMath.Binary32(Battle.Loadout.bombRadius + 1f) : 1f));
                    float score = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(delta + (OriginalMath.Binary32(radius + radius)))) * .004f) + 1f)) * OriginalMath.Binary32((float)count));
                    if (score > best) { best = score; result = new Vector2(logicalX, logicalY); }
                }
                radius = OriginalMath.Binary32((aimMissile ? 150f : 120f) * (Battle != null ? OriginalMath.Binary32(Battle.Loadout.bombRadius + 1f) : 1f));
            }
            return result;
        }

        // f54824. The reticle is rendered with the layout map disabled so its
        // radius remains circular at every supported screen stretch.
        private void DrawSuperTargeting(PixelSurface s)
        {
            if (selection != 2) return;
            Color color = aimMissile ? new Color(1f, .73f, .27f, 1f) : new Color(.3f, .9f, 1f, 1f);
            Plate(s, 110f, 118f, 320f, 34f, SourceColor(4453159313425678991UL, 4572054341889399194UL),
                SourceColor(4405601299457103626UL, 4572054341881681674UL), color, 6f);
            string title = aimMissile ? "超级导弹 · 救援支援" : "超级炸弹 · 定点轰炸";
            s.TextF(title, 271.1700134277344f, 126.1699981689453f, 1.3f, new Color(0f, 0f, 0f, .7f), 0);
            s.TextF(title, 270f, 125f, 1.3f, color, 0);
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            SuperStrikeVisuals.Reticle(s, ScreenLayout.Point(aim),
                OriginalMath.Binary32((aimMissile ? 150f : 120f) * (Battle != null ? OriginalMath.Binary32(Battle.Loadout.bombRadius + 1f) : 1f)), targetClock, color, true);
            s.MapLayout = mapped;
            if (!Economy.Data.bombTutorial)
            {
                Plate(s, 70f, 166f, 400f, 54f, SourceColor(4477658893514493133UL, 4572054341894096814UL),
                    SourceColor(4435865490164012810UL, 4572054341884366029UL), color, 6f);
                s.TextF("拖动准星选择目标", 271.125f, 173.125f, 1.25f, new Color(0f, 0f, 0f, .7f), 0);
                s.TextF("拖动准星选择目标", 270f, 172f, 1.25f, Color.white, 0);
                s.TextF("命中后清敌、击退并压制敌阵", 271f, 197f, 1.05f, new Color(0f, 0f, 0f, OriginalMath.Binary32(uiDim.a * .7f)), 0);
                s.TextF("命中后清敌、击退并压制敌阵", 270f, 196f, 1.05f, uiDim, 0);
            }
            Button(s, 9, 120f, 512f, 300f, 54f, "确认轰炸", Btn.Primary, 2f);
            s.TextF("拖动瞄准 · 双击确认", 271f, 847f, 1.1f, new Color(0f, 0f, 0f, .7f), 0);
            s.TextF("拖动瞄准 · 双击确认", 270f, 846f, 1.1f, color, 0);
        }
    }
}

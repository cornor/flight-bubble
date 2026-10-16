using Flightbeat.Model;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Feedback
{
    public sealed partial class CombatFeedback
    {
        // f53835. Source thresholds, folded outline offsets and WAT arithmetic.
        public void DrawCombo(PixelSurface s)
        {
            if (comboFade <= .01f || HideCombo) return;
            int count = lastCombo > 1 ? lastCombo : 1;
            Color color = ComboColor(count);
            float fade = comboFade, punch = comboPunch < .12f ? OriginalMath.Binary32(OriginalMath.Binary32(comboPunch / -.12f) + 1f) : 0f;
            float milestone = comboMilestoneAge < .5f ? OriginalMath.Binary32(1f - (OriginalMath.Binary32(comboMilestoneAge + comboMilestoneAge))) : 0f;
            float scale = OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(punch * .5f) + 3f) + OriginalMath.Binary32(milestone * 1.2f));
            string text = count.ToString();
            float width = OriginalMath.Binary32(scale * OriginalMath.Binary32((float)(text.Length << 3))), y = OriginalMath.Binary32(116f - (OriginalMath.Binary32(punch + punch)));
            float flicker = comboBand >= 2 ? OriginalMath.Binary32(OriginalMath.Sin(OriginalMath.Binary32(time * 12f)) * .08f) : 0f;
            s.Glow(OriginalMath.Binary32(520f - OriginalMath.Binary32(width * .5f)), OriginalMath.Binary32(y + OriginalMath.Binary32(OriginalMath.Binary32(scale * 12f) * .5f)), OriginalMath.Binary32(width + 70f), 60f,
                Alpha(color, OriginalMath.Binary32(fade * (OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Binary32(milestone * .2f) + .18f) + flicker)))), 0f);
            float x = OriginalMath.Binary32(520f - width);
            Color shadow = new Color(0f, 0f, 0f, OriginalMath.Binary32(fade * .85f));
            DrawBigText(s, text, OriginalMath.Binary32(x + 2f), OriginalMath.Binary32(y + 0f), scale, shadow);
            DrawBigText(s, text, OriginalMath.Binary32(x + 1.4142135381698608f), OriginalMath.Binary32(y + 1.4142135381698608f), scale, shadow);
            DrawBigText(s, text, OriginalMath.Binary32(x + -0f), OriginalMath.Binary32(y + 2f), scale, shadow);
            DrawBigText(s, text, OriginalMath.Binary32(x + -1.4142135381698608f), OriginalMath.Binary32(y + 1.4142135381698608f), scale, shadow);
            DrawBigText(s, text, OriginalMath.Binary32(x + -2f), OriginalMath.Binary32(y + -0f), scale, shadow);
            DrawBigText(s, text, OriginalMath.Binary32(x + -1.4142132997512817f), OriginalMath.Binary32(y + -1.41421377658844f), scale, shadow);
            DrawBigText(s, text, OriginalMath.Binary32(x + 0f), OriginalMath.Binary32(y + -2f), scale, shadow);
            DrawBigText(s, text, OriginalMath.Binary32(x + 1.414214015007019f), OriginalMath.Binary32(y + -1.4142130613327026f), scale, shadow);
            DrawBigText(s, text, x, OriginalMath.Binary32(y + 2f), scale, new Color(OriginalMath.Binary32(color.r * .5f), OriginalMath.Binary32(color.g * .5f), OriginalMath.Binary32(color.b * .5f), fade));
            DrawBigText(s, text, x, y, scale, Alpha(Mix(color, Color.white, OriginalMath.Binary32(punch * .6f)), fade));
            float bottom = OriginalMath.Binary32(y + OriginalMath.Binary32(scale * 8f));
            DrawBigText(s, "×", OriginalMath.Binary32(x + -22f), OriginalMath.Binary32(bottom + -18f), 2f, new Color(0f, 0f, 0f, OriginalMath.Binary32(fade * .8f)));
            DrawBigText(s, "×", OriginalMath.Binary32(x + -23f), OriginalMath.Binary32(bottom + -19f), 2f, Alpha(color, fade));
            s.TextF("连击", 519f, OriginalMath.Binary32(y + -20f), 1.15f, new Color(0f, 0f, 0f, OriginalMath.Binary32(fade * .7f)), 1);
            s.TextF("连击", 518f, OriginalMath.Binary32(y + -21f), 1.15f, Alpha(color, fade), 1);
            float life = battle.ComboLife;
            s.Rect(x, OriginalMath.Binary32(bottom + 4f), width, 3f, new Color(0f, 0f, 0f, OriginalMath.Binary32(fade * .5f)));
            s.Rect(x, OriginalMath.Binary32(bottom + 4f), OriginalMath.Binary32(width * life), 3f, Alpha(color, fade));
            if (!(comboMilestoneAge < 1.2f)) return;
            float t = OriginalMath.Binary32(comboMilestoneAge / 1.2f);
            fade = Unit(OriginalMath.Binary32((OriginalMath.Binary32(1f - t)) * 2.5f));
            float remaining = OriginalMath.Binary32(t / .2f);
            remaining = remaining < 0f ? 1f : OriginalMath.Binary32(1f - OriginalMath.WasmMin(remaining, 1f));
            float pop = OriginalMath.Binary32((OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(remaining * (OriginalMath.Binary32(remaining * remaining))) + -1f)) + 1f)) * 1.6f);
            y = OriginalMath.Binary32(OriginalMath.Binary32(t * -20f) + 200f);
            float centerY = OriginalMath.Binary32(y + 14f);
            string value = milestoneValue.ToString() + " 连击";
            for (int n = 0; n < 10; n++)
            {
                float angle = OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32(time * 60f) + OriginalMath.Binary32((float)(n * 36)))) * .01745329238474369f);
                s.GlowLine(new Vector2(270f, centerY),
                    new Vector2(OriginalMath.Binary32(OriginalMath.Binary32(OriginalMath.Cos(angle) * 220f) + 270f), OriginalMath.Binary32(centerY + OriginalMath.Binary32(OriginalMath.Sin(angle) * 220f))),
                    40f, Alpha(color, OriginalMath.Binary32(fade * .12f)));
            }
            string title = milestoneValue > 499 ? "火力全开！" : "连击突破！";
            s.TextF(title, 273f, OriginalMath.Binary32(y + 3f), OriginalMath.Binary32(pop + 2.4f), new Color(0f, 0f, 0f, OriginalMath.Binary32(fade * .85f)), 0);
            s.TextF(title, 270f, y, OriginalMath.Binary32(pop + 2.4f), Alpha(color, fade), 0);
            s.TextF(value, 271f, OriginalMath.Binary32(y + 44f), 1.4f, new Color(0f, 0f, 0f, OriginalMath.Binary32(fade * .8f)), 0);
            s.TextF(value, 270f, OriginalMath.Binary32(y + 43f), 1.4f, new Color(1f, 1f, 1f, fade), 0);
        }
    }
}

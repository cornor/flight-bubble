using Flightbeat.Model;
using System.Collections.Generic;
using Flightbeat.Rendering;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // src: ShowBroken f54585. Keep the supplied list reference, not a copy.
        private void ShowBroken(List<string> names)
        {
            if (names == null || names.Count == 0) return;
            brokenNotice = true;
            brokenModules = names;
            MarkUi(531);
        }
        // src: DrawBrokenNotice f54584.
        private void DrawBrokenNotice(PixelSurface s)
        {
            if (!brokenNotice || brokenModules.Count == 0) return;
            Backdrop(s, .8f);
            bool mapped = s.MapLayout;
            s.MapLayout = false;
            float eased = OutBack(OriginalMath.Binary32(Since(531) / .3f)), alpha = Clamp01(OriginalMath.Binary32(eased * 1.3f));
            float height = OriginalMath.Binary32((float)unchecked(brokenModules.Count * 30 + 190));
            float y = OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(1f - eased)) * 40f) + (OriginalMath.Binary32(OriginalMath.Binary32(ScreenLayout.Height * .5f) - OriginalMath.Binary32(height * .5f))));
            Card(s, 60f, y, 420f, height, uiRed, alpha);
            Title(s, "装备损坏", 270f, OriginalMath.Binary32(y + 18f), 2.2f, Color.white, uiInk);
            Label(s, "耐久耗尽，以下模块已损坏：", 270f, OriginalMath.Binary32(y + 70f), 1.15f, uiDim);
            float top = OriginalMath.Binary32(y + 100f);
            for (int i = 0; i < brokenModules.Count; i++)
                Label(s, brokenModules[i], 270f, OriginalMath.Binary32(top + OriginalMath.Binary32((float)(i * 30))), 1.3f, new Color(1f, .6f, .5f, 1f));
            Button(s, 530, 150f, OriginalMath.Binary32(OriginalMath.Binary32(y + height) + -62f), 240f, 46f, "知道了", Btn.Primary, 1.6f);
            s.MapLayout = mapped;
        }
    }
}

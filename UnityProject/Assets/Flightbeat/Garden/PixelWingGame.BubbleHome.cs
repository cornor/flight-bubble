#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using Flightbeat.Rendering;
using Flightbeat.Garden;
using UnityEngine;
using static Flightbeat.Garden.GardenSkin;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        bool TryBubbleLevelFromUrl()
        {
            if (!System.Uri.TryCreate(Application.absoluteURL, System.UriKind.Absolute, out var uri)) return false;
            foreach (string pair in uri.Query.TrimStart('?').Split('&'))
            {
                string[] parts = pair.Split('=');
                if (parts.Length != 2 || parts[0] != "level" || !int.TryParse(parts[1], out int requested)) continue;
                if (requested < 1 || requested > Levels.Length) return false;
                Economy.Data.unlocked = System.Math.Max(Economy.Data.unlocked, requested);
                if (!Economy.CanSortie) Economy.AddEnergy(1);
                return StartSortie(requested - 1);
            }
            return false;
        }
        void StartBubbleLevel()
        {
            int next = 0;
            while (next < Levels.Length && Economy.Data.cleared.Contains(next + 1)) next++;
            if (next == Levels.Length) next = Levels.Length - 1;
            if (!Economy.CanSortie) Economy.AddEnergy(1);
            StartSortie(next);
        }
        void DrawBubbleHome(PixelSurface s)
        {
            s.MapLayout = true;
            TextOutline(s, "泡泡小队", 270, 238, 48, Cream, Outline, 3f);
            GardenButton(s, 800, 100, 530, 340, 76, "开始");
            GardenButton(s, 801, 160, 630, 220, 64, "设置");
        }
    }
}
#endif

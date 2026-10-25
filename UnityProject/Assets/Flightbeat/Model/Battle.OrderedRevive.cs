#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // Bubble revival grants the first locked bay. The original revival adds
        // an open bay at the far right; reuse that reward without granting two.
        public bool ReviveInSlotOrder()
        {
            if (!Revive()) return false;
            for (int i = 0; i < extraBay.Length; i++)
            {
                if (extraBay[i]) continue;
                extraBay[i] = true;
                reviveSlots--;
                MaximumSlots--;
                break;
            }
            return true;
        }
    }
}
#endif

namespace Flightbeat.Model
{
    public sealed partial class Battle
    {
        // src: Battle.get_Progress f54983; division is binary32.
        public float Progress => Total == 0 ? 1f : OriginalMath.Binary32(1f - OriginalMath.Binary32(OriginalMath.Binary32((float)Remaining) / OriginalMath.Binary32((float)Total)));

        // src: Battle.QueuePositionX f55023; WAT f32.div precedes f32.mul.
        public float QueuePositionX(int lane) => OriginalMath.Binary32(OriginalMath.Binary32((OriginalMath.Binary32(OriginalMath.Binary32((float)lane) + 0.5f)) * (OriginalMath.Binary32(516f / OriginalMath.Binary32((float)(Reserve.Length > 1 ? Reserve.Length : 1))))) + 12f);

        // src: Battle.get_ReserveEmpty f55091.
        public bool ReserveEmpty
        {
            get
            {
                for (int lane = 0; lane < Reserve.Length; lane++)
                    if (Reserve[lane].Count > 0) return false;
                return true;
            }
        }

        // src: Battle.get_FinaleReady f55092. The unsigned range accepts 1..999.
        public bool FinaleReady => State == Phase.Playing && Level.lesson <= 0 && !TrainingHold
            && ActiveFinale == null && PendingStrike == null && unchecked((uint)(Remaining - 1)) < 999u
            && Fleet.Count > 0 && ReserveEmpty;
    }
}

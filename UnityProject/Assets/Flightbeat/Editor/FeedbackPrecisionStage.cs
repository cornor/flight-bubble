namespace Flightbeat.Editor
{
    // Concentrated regression of the changed feedback group and a fresh native
    // build. Battle draw capture/comparison is a separate completed CLI pass.
    public static class FeedbackPrecisionStage
    {
        public static void RunAndBuild()
        {
            FeedbackContractStage.Run();
            AdvancedCombatStage.Run();
            NativeCliBuild.Run();
        }
    }
}

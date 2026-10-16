namespace Flightbeat.Editor
{
    // One CLI compilation for fixed original page observations, the changed
    // five-zone/background branches, and the native build. Pixel equality is
    // decided separately by the exact comparer; a build does not imply it.
    public static class DrawPrecisionStage
    {
        public static void RunAndBuild()
        {
            FixedDrawStage.Run();
            SpaceDrawingStage.Run();
            NativeCliBuild.Run();
        }
    }
}

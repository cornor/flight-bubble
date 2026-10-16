namespace Flightbeat.Editor
{
    // Preserve the existing CLI entry point; Editor and native builds share one
    // integration scenario. The native probe is included only in diagnostic builds.
    public static class TeachingHudPlayProbe
    {
        public static void Run() => Flightbeat.Diagnostics.TeachingHudPlayProbe.Run();
    }
}

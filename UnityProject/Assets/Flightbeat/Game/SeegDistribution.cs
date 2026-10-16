namespace Flightbeat.Game
{
    // These seven interfaces are literal nop bodies in the shipped source.
    // Keeping them empty restores that platform contract, not a gameplay stub.
    public static class SeegDistribution
    {
        public static void Initialize() { } // f897
        public static void Loaded() { } // f897
        public static void LevelStart(int level) { } // f1080
        public static void LevelEnd(bool won) { } // f1080
        public static void LevelQuit() { } // f897
        public static void Revived() { } // f897
        public static void PropUsed(int kind) { } // f1080
    }
}

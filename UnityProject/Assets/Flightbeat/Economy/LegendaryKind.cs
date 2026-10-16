using System;

namespace Flightbeat.Economy
{
    // Original metadata type 4316, including Flags and every declared bit.
    [Flags]
    public enum LegendaryKind
    {
        None = 0,
        Overclock = 1,
        Pulse = 2,
        Phoenix = 4,
        TimeStop = 8,
        Afterburner = 16
    }
}

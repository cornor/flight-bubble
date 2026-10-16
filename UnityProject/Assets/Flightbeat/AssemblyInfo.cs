using System.Runtime.CompilerServices;

// Explicit diagnostic/editor consumers retain access to original internal
// members after assembly separation. No public gameplay API is broadened.
[assembly: InternalsVisibleTo("Flightbeat.DevTools")]
[assembly: InternalsVisibleTo("Flightbeat.Editor")]

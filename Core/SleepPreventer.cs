using MeowProof.Helpers;

namespace MeowProof.Core;

/// <summary>
/// Keeps the display awake while the keyboard is locked by holding
/// ES_DISPLAY_REQUIRED via SetThreadExecutionState.
/// </summary>
public static class SleepPreventer
{
    public static void Prevent() =>
        Win32.SetThreadExecutionState(Win32.ES_CONTINUOUS | Win32.ES_DISPLAY_REQUIRED);

    public static void Allow() =>
        Win32.SetThreadExecutionState(Win32.ES_CONTINUOUS);
}

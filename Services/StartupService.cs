using Microsoft.Win32;

namespace MeowProof.Services;

public static class StartupService
{
    private const string RegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MeowProof";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath);
            return key?.GetValue(AppName) != null;
        }
    }

    private static string CurrentCommand =>
        $"\"{System.Windows.Forms.Application.ExecutablePath}\"";

    public static void Apply(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
        if (key is null) return;
        if (enable)
            key.SetValue(AppName, CurrentCommand);
        else
            key.DeleteValue(AppName, throwOnMissingValue: false);
    }

    /// <summary>
    /// If startup is enabled but the registered path no longer matches this
    /// exe (the user moved or renamed it), rewrite it to the current location.
    /// Call once on launch so "Launch at Login" survives moving the exe.
    /// </summary>
    public static void SyncPath()
    {
        var command = CurrentCommand;
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
        var existing = key?.GetValue(AppName) as string;
        if (existing is null) return; // not enabled — leave it alone
        if (!string.Equals(existing, command, StringComparison.OrdinalIgnoreCase))
            key!.SetValue(AppName, command);
    }
}

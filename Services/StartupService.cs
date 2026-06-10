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

    public static void Apply(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
        if (key is null) return;
        if (enable)
            key.SetValue(AppName, $"\"{System.Windows.Forms.Application.ExecutablePath}\"");
        else
            key.DeleteValue(AppName, throwOnMissingValue: false);
    }
}

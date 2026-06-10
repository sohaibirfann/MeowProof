using System.IO;
using System.Text.Json;

namespace MeowProof.Models;

public class AppSettings
{
    public bool   LaunchAtStartup       { get; set; } = false;
    public bool   PreventSleep          { get; set; } = true;
    public bool   ShowInTaskbar         { get; set; } = false;
    public bool   CatDetectionEnabled   { get; set; } = true;
    public bool   PlayBlockedSound      { get; set; } = true;
    public bool   PlayLockSound         { get; set; } = false;
    public string  SelectedSound         { get; set; } = "Warning: Cat on Keyboard";
    public string? CustomSoundPath      { get; set; } = null;
    public int     OverlayOpacity       { get; set; } = 65;
    public string UnlockShortcut        { get; set; } = "Ctrl+Shift+L";

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MeowProof", "settings.json");

    public static AppSettings Current { get; private set; } = new();

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                Current = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new();
        }
        catch { Current = new(); }
    }

    public static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Current,
                new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}

using System.Collections.ObjectModel;
using System.IO;
using System.Media;
using System.Reflection;
using MeowProof.Models;

namespace MeowProof.Services;

public static class SoundService
{
    public static readonly ReadOnlyCollection<string> BuiltinSounds = new(new[]
    {
        "Warning: Cat on Keyboard",
        "Alarm",
        "Meow",
        "Dog Bark",
        "Cat Hiss",
        "Bell",
        "Silence",
    });

    // Maps display name → embedded resource filename
    private static readonly Dictionary<string, string> _resourceNames = new()
    {
        ["Warning: Cat on Keyboard"] = "warning.wav",
        ["Alarm"]                    = "alarm.wav",
        ["Meow"]                     = "meow.wav",
        ["Dog Bark"]                 = "dog-bark.wav",
        ["Cat Hiss"]                 = "cat-hiss.wav",
        ["Bell"]                     = "bell.wav",
        ["Silence"]                  = "silence.wav",
    };

    public static void Preview(string soundName) =>
        Task.Run(() => Play(soundName));

    public static void PlayKeyBlocked()
    {
        if (!AppSettings.Current.PlayBlockedSound) return;
        Task.Run(() => Play(AppSettings.Current.SelectedSound));
    }

    public static void PlayLocked()
    {
        if (!AppSettings.Current.PlayLockSound) return;
        Task.Run(() => Play(AppSettings.Current.SelectedSound));
    }

    private static void Play(string soundName)
    {
        if (soundName == "Silence") return;

        // Custom imported file
        if (soundName == "Custom")
        {
            if (AppSettings.Current.CustomSoundPath is { } path && File.Exists(path))
            {
                using var player = new SoundPlayer(path);
                player.PlaySync();
            }
            return;
        }

        // Built-in embedded resource
        if (!_resourceNames.TryGetValue(soundName, out var filename)) return;

        var resourceName = $"MeowProof.Assets.Sounds.{filename}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
        if (stream is null) return;

        // SoundPlayer requires a seekable stream — copy to MemoryStream
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        using var player2 = new SoundPlayer(ms);
        player2.PlaySync();
    }
}

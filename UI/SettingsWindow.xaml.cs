using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MeowProof.Models;
using MeowProof.Services;
using Microsoft.Win32;

namespace MeowProof.UI;

public partial class SettingsWindow : Window
{
    private bool _loading;

    // Sentinel value for the "Import custom…" row.
    private const string ImportValue = "__import__";

    private sealed class SoundOption
    {
        public required string Display { get; init; }
        public required string Value { get; init; } // built-in name, "Custom", or ImportValue
        public override string ToString() => Display;
    }

    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadSettings();
    }

    private void LoadSettings()
    {
        _loading = true;
        var s = AppSettings.Current;
        ToggleStartup.IsChecked      = StartupService.IsEnabled;
        ToggleSleep.IsChecked        = s.PreventSleep;
        ToggleTaskbar.IsChecked      = s.ShowInTaskbar;
        ToggleBlockedSound.IsChecked = s.PlayBlockedSound;
        ToggleLockSound.IsChecked    = s.PlayLockSound;
        ToggleCatDetection.IsChecked = s.CatDetectionEnabled;
        PopulateSounds();
        _loading = false;
    }

    private void PopulateSounds()
    {
        var options = SoundService.BuiltinSounds
            .Select(name => new SoundOption { Display = name, Value = name })
            .ToList();

        if (AppSettings.Current.CustomSoundPath is { } path)
            options.Add(new SoundOption { Display = Path.GetFileName(path), Value = "Custom" });

        options.Add(new SoundOption { Display = "Import custom…", Value = ImportValue });

        SoundCombo.ItemsSource = options;

        var current = AppSettings.Current.SelectedSound;
        SoundCombo.SelectedItem = options.Find(o => o.Value == current) ?? options[0];
    }

    private void SoundCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || SoundCombo.SelectedItem is not SoundOption opt) return;

        if (opt.Value == ImportValue)
        {
            ImportCustomSound();
            return;
        }

        AppSettings.Current.SelectedSound = opt.Value;
        AppSettings.Save();
        SoundService.Preview(opt.Value);
    }

    private void ImportCustomSound()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Import Sound",
            Filter = "Audio files (*.wav;*.mp3)|*.wav;*.mp3",
            CheckFileExists = true,
        };

        bool imported = dlg.ShowDialog(this) == true;

        if (imported)
        {
            var destDir = Path.Combine(AppSettings.AppDataFolder, "Sounds");
            Directory.CreateDirectory(destDir);
            var dest = Path.Combine(destDir, Path.GetFileName(dlg.FileName));
            if (!string.Equals(dlg.FileName, dest, StringComparison.OrdinalIgnoreCase))
                File.Copy(dlg.FileName, dest, overwrite: true);

            AppSettings.Current.CustomSoundPath = dest;
            AppSettings.Current.SelectedSound = "Custom";
            AppSettings.Save();
        }

        // Re-populate in both cases: success selects "Custom", cancel re-selects the persisted sound.
        _loading = true;
        PopulateSounds();
        _loading = false;

        if (imported) SoundService.Preview("Custom");
    }

    private void ToggleStartup_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        var on = ToggleStartup.IsChecked == true;
        AppSettings.Current.LaunchAtStartup = on;
        StartupService.Apply(on);
        AppSettings.Save();
    }

    private void ToggleSleep_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        AppSettings.Current.PreventSleep = ToggleSleep.IsChecked == true;
        AppSettings.Save();
    }

    private void ToggleTaskbar_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        AppSettings.Current.ShowInTaskbar = ToggleTaskbar.IsChecked == true;
        AppSettings.Save();
    }

    private void ToggleBlockedSound_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        AppSettings.Current.PlayBlockedSound = ToggleBlockedSound.IsChecked == true;
        AppSettings.Save();
    }

    private void ToggleLockSound_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        AppSettings.Current.PlayLockSound = ToggleLockSound.IsChecked == true;
        AppSettings.Save();
    }

    private void ToggleCatDetection_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        AppSettings.Current.CatDetectionEnabled = ToggleCatDetection.IsChecked == true;
        AppSettings.Save();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

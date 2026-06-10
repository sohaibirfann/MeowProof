using System.Windows;
using System.Windows.Input;
using MeowProof.Models;
using MeowProof.Services;

namespace MeowProof.UI;

public partial class SettingsWindow : Window
{
    private bool _loading;

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
        UpdateSoundLabel();
        _loading = false;
    }

    private void UpdateSoundLabel() =>
        SelectedSoundLabel.Text = AppSettings.Current.SelectedSound;

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

    private void ChooseSound_Click(object sender, RoutedEventArgs e)
    {
        var picker = new SoundPickerWindow { Owner = this };
        if (picker.ShowDialog() == true)
            UpdateSoundLabel();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

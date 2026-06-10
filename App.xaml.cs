using System.Windows;
using Microsoft.Win32;
using MeowProof.Models;
using MeowProof.Services;
using MeowProof.UI;

namespace MeowProof;

public partial class App : System.Windows.Application
{
    private LockService? _lock;
    private TrayService? _tray;
    private MainWindow? _main;
    private readonly List<OverlayWindow> _overlays = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppSettings.Load();

        _lock = new LockService();

        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            _overlays.Add(new OverlayWindow(screen));

        _lock.StateChanged += (_, _) => UpdateOverlays();

        // Auto-unlock when screen sleeps or the session locks — safety net so
        // the user is never locked out of their own machine.
        SystemEvents.PowerModeChanged  += (_, e) => { if (e.Mode == PowerModes.Suspend)          _lock.Unlock(); };
        SystemEvents.SessionSwitch     += (_, e) => { if (e.Reason == SessionSwitchReason.SessionLock) _lock.Unlock(); };

        _main = new MainWindow(_lock);

        _tray = new TrayService(_lock);
        _tray.QuitRequested += (_, _) =>
        {
            if (_main is not null) _main.AllowClose = true;
            foreach (var ov in _overlays) ov.AllowClose = true;
            Shutdown();
        };
        _tray.OpenRequested += (_, _) => ShowMain();
        _tray.SettingsRequested += (_, _) => OpenSettings();
        _tray.Start();

        ShowMain();
    }

    private void UpdateOverlays()
    {
        if (_lock is null) return;
        foreach (var ov in _overlays)
        {
            if (_lock.State == Services.LockState.Locked)
                ov.Show();
            else
                ov.Hide();
        }
    }

    private void OpenSettings()
    {
        var win = new SettingsWindow();
        win.Show();
    }

    private void ShowMain()
    {
        if (_main is null) return;
        _main.Show();
        if (_main.WindowState == WindowState.Minimized)
            _main.WindowState = WindowState.Normal;
        _main.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _lock?.Dispose();
        _tray?.Dispose();
        base.OnExit(e);
    }
}

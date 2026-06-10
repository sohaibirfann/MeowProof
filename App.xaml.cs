using System.Windows;
using Microsoft.Win32;
using MeowProof.Core;
using MeowProof.Models;
using MeowProof.Services;
using MeowProof.UI;

namespace MeowProof;

public partial class App : System.Windows.Application
{
    private LockService? _lock;
    private TrayService? _tray;
    private MainWindow? _main;
    private CatDetector? _catDetector;
    private readonly List<OverlayWindow> _overlays = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppSettings.Load();

        // If "Launch at Login" is on, keep the registered path pointing at
        // wherever this exe now lives (in case it was moved/renamed).
        StartupService.SyncPath();

        _lock = new LockService();

        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            _overlays.Add(new OverlayWindow(screen));

        _lock.StateChanged += (_, _) => UpdateOverlays();

        // Safety net: never leave the user locked out when the machine sleeps
        // or the session locks (Win+L).
        SystemEvents.PowerModeChanged += (_, pe) => { if (pe.Mode == PowerModes.Suspend)              _lock.Unlock(); };
        SystemEvents.SessionSwitch    += (_, se) => { if (se.Reason == SessionSwitchReason.SessionLock) _lock.Unlock(); };

        _catDetector = new CatDetector();
        _catDetector.CatDetected += () =>
        {
            if (!AppSettings.Current.CatDetectionEnabled || _lock!.IsLocked) return;
            _lock.Lock();
            _tray?.ShowCatNotification();
        };
        _catDetector.Install();

        _main = new MainWindow(_lock);

        _tray = new TrayService(_lock);
        _tray.QuitRequested     += (_, _) => Quit();
        _tray.OpenRequested     += (_, _) => ShowMain();
        _tray.SettingsRequested += (_, _) => OpenSettings();
        _tray.Start();

        ShowMain();
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool TerminateProcess(IntPtr hProcess, uint exitCode);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    private void Quit()
    {
        _tray?.HideIcon();
        // TerminateProcess on the current process is immediate and lets the OS
        // reclaim the keyboard hooks for us. We deliberately do NOT call
        // UnhookWindowsHookEx here — on a low-level hook it can block.
        TerminateProcess(GetCurrentProcess(), 0);
    }

    private void UpdateOverlays()
    {
        if (_lock is null) return;
        foreach (var ov in _overlays)
        {
            if (_lock.State == LockState.Locked)
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
        // Normal quit goes through Quit() -> TerminateProcess. This only runs if
        // WPF shuts us down by another path (e.g. Windows session ending).
        _catDetector?.Dispose();
        _lock?.Dispose();
        _tray?.Dispose();
        base.OnExit(e);
    }
}

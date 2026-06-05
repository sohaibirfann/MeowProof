using System.Windows;
using MeowProof.Services;
using MeowProof.UI;

namespace MeowProof;

public partial class App : System.Windows.Application
{
    private LockService? _lock;
    private TrayService? _tray;
    private MainWindow? _main;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _lock = new LockService();
        _main = new MainWindow(_lock);

        _tray = new TrayService(_lock);
        _tray.QuitRequested += (_, _) =>
        {
            if (_main is not null) _main.AllowClose = true;
            Shutdown();
        };
        _tray.OpenRequested += (_, _) => ShowMain();
        _tray.SettingsRequested += (_, _) => System.Windows.MessageBox.Show("Settings coming in step 8.", "MeowProof");
        _tray.Start();

        ShowMain();
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
        _tray?.Dispose();
        base.OnExit(e);
    }
}

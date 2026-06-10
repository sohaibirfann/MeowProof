using System.Windows;
using System.Windows.Media;
using MeowProof.Models;
using MeowProof.Services;

namespace MeowProof.UI;

public partial class TrayPopupWindow : Window
{
    private readonly LockService _lock;
    private readonly System.Drawing.Point _cursorPos;
    private bool _loading;

    public event EventHandler? OpenRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? QuitRequested;

    public TrayPopupWindow(LockService lockService, System.Drawing.Point cursorPos)
    {
        InitializeComponent();
        _lock = lockService;
        _cursorPos = cursorPos;

        _loading = true;
        ToggleLogin.IsChecked   = StartupService.IsEnabled;
        ToggleStealth.IsChecked = AppSettings.Current.PreferStealthLock;
        _loading = false;

        UpdateState();
        _lock.StateChanged += (_, _) => Dispatcher.BeginInvoke(UpdateState);

        ContentRendered += (_, _) => PositionNearTray();

        // Only close on deactivation after we have been properly activated
        // at least once — prevents false-close during the initial Win32
        // SetForegroundWindow / WM_ACTIVATE dance.
        bool activatedOnce = false;
        Activated   += (_, _) => activatedOnce = true;
        Deactivated += (_, _) => { if (activatedOnce) Close(); };
    }

    private void UpdateState()
    {
        bool locked = _lock.IsLocked;

        StatusLabel.Text = locked
            ? (_lock.State == LockState.StealthLocked ? "Stealth locked" : "Keyboard locked")
            : "Keyboard ready";

        if (locked)
        {
            LockBtn.Background  = new SolidColorBrush(Color.FromRgb(0xD9, 0x64, 0x5C));
            LockBtnText.Text    = "Unlock Keyboard";
        }
        else
        {
            LockBtn.Background  = (Brush)FindResource("AccentGradient");
            LockBtnText.Text    = AppSettings.Current.PreferStealthLock
                ? "Stealth Lock" : "Lock Keyboard";
        }
    }

    private void LockBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_lock.IsLocked)
            _lock.Unlock();
        else if (AppSettings.Current.PreferStealthLock)
            _lock.StealthLock();
        else
            _lock.Lock();

        Close();
    }

    private void ToggleLogin_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        bool on = ToggleLogin.IsChecked == true;
        AppSettings.Current.LaunchAtStartup = on;
        StartupService.Apply(on);
        AppSettings.Save();
    }

    private void ToggleStealth_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        AppSettings.Current.PreferStealthLock = ToggleStealth.IsChecked == true;
        AppSettings.Save();
        UpdateState();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        Close();
        OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        Close();
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        // Don't Close() first — that fires Deactivated -> re-entrant Close and
        // can swallow the invoke. QuitRequested terminates the process, so the
        // window never needs to close on its own.
        QuitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PositionNearTray()
    {
        using var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
        double dpiX = g.DpiX / 96.0;
        double dpiY = g.DpiY / 96.0;

        var screen = System.Windows.Forms.Screen.FromPoint(_cursorPos);
        var wa     = screen.WorkingArea;

        double x = _cursorPos.X / dpiX - ActualWidth + 10;
        double y = _cursorPos.Y / dpiY - ActualHeight - 6;

        Left = Math.Max(wa.Left / dpiX, Math.Min(x, wa.Right  / dpiX - ActualWidth));
        Top  = Math.Max(wa.Top  / dpiY, Math.Min(y, wa.Bottom / dpiY - ActualHeight));

        // Now that it sits in the right place, reveal it. Until this point the
        // window has been off-screen at full transparency, so no flashing.
        Opacity = 1;
    }
}

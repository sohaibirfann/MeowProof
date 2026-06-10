using System.Drawing;
using System.Windows.Forms;
using MeowProof.Helpers;
using MeowProof.UI;

namespace MeowProof.Services;

/// <summary>
/// Owns the NotifyIcon. Left- or right-clicking the tray icon opens
/// the custom WPF TrayPopupWindow instead of a native context menu.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly LockService _lock;
    private readonly NotifyIcon _notifyIcon;
    private TrayPopupWindow? _popup;

    private Icon? _lockedIcon;
    private Icon? _unlockedIcon;

    public event EventHandler? QuitRequested;
    public event EventHandler? OpenRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? UpdateRequested;

    public TrayService(LockService lockService)
    {
        _lock = lockService;
        _lock.StateChanged += (_, _) => UpdateIcon();

        _lockedIcon   = IconFactory.CreateTrayIcon(locked: true);
        _unlockedIcon = IconFactory.CreateTrayIcon(locked: false);

        _notifyIcon = new NotifyIcon
        {
            Icon    = _unlockedIcon,
            Text    = "MeowProof",
            Visible = false,
        };

        _notifyIcon.MouseClick += OnMouseClick;
    }

    public void Start()
    {
        _notifyIcon.Visible = true;
        UpdateIcon();
    }

    public void HideIcon() => _notifyIcon.Visible = false;

    public void ShowCatNotification() =>
        _notifyIcon.ShowBalloonTip(3000, "MeowProof",
            "Cat detected — keyboard locked!", ToolTipIcon.Info);

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        var cursorPos = Cursor.Position;
        // BeginInvoke (fire-and-forget) so the WinForms click thread is
        // never blocked waiting for WPF window activation to complete.
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            // Toggle: clicking while popup is open just closes it.
            if (_popup is { IsVisible: true })
            {
                _popup.Close();
                return;
            }

            _popup = new TrayPopupWindow(_lock, cursorPos);
            _popup.Closed           += (_, _) => _popup = null;
            _popup.OpenRequested     += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
            _popup.SettingsRequested += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
            _popup.QuitRequested     += (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty);
            _popup.UpdateRequested   += (_, _) => UpdateRequested?.Invoke(this, EventArgs.Empty);
            _popup.Show();
            _popup.Activate();
        });
    }

    private void UpdateIcon()
    {
        bool locked = _lock.IsLocked;
        _notifyIcon.Icon = locked ? _lockedIcon : _unlockedIcon;
        _notifyIcon.Text = locked ? "MeowProof — Locked" : "MeowProof — Unlocked";
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _lockedIcon?.Dispose();
        _unlockedIcon?.Dispose();
        _lockedIcon = null;
        _unlockedIcon = null;
    }
}

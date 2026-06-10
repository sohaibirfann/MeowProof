using System.Drawing;
using System.Windows.Forms;
using MeowProof.Helpers;

namespace MeowProof.Services;

/// <summary>
/// Owns the system tray icon and its context menu. Routes lock actions through
/// the shared <see cref="LockService"/> and swaps the tray icon on state change.
/// </summary>
public sealed class TrayService : IDisposable
{
    private readonly LockService _lock;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _lockItem;
    private readonly ToolStripMenuItem _stealthItem;
    private readonly ToolStripMenuItem _startupItem;

    private Icon? _lockedIcon;
    private Icon? _unlockedIcon;

    /// <summary>Raised when the user chooses Quit from the tray menu.</summary>
    public event EventHandler? QuitRequested;

    /// <summary>Raised when the user wants the main window shown (Open / double-click).</summary>
    public event EventHandler? OpenRequested;

    /// <summary>Raised when the user clicks Settings.</summary>
    public event EventHandler? SettingsRequested;

    public TrayService(LockService lockService)
    {
        _lock = lockService;
        _lock.StateChanged += (_, _) => UpdateForState();

        _lockedIcon = IconFactory.CreateTrayIcon(locked: true);
        _unlockedIcon = IconFactory.CreateTrayIcon(locked: false);

        var menu = new ContextMenuStrip();

        var header = new ToolStripMenuItem("🐾  MeowProof") { Enabled = false };
        menu.Items.Add(header);
        menu.Items.Add(new ToolStripSeparator());

        _lockItem = new ToolStripMenuItem("Lock", null, OnLockClicked)
        {
            ShortcutKeyDisplayString = "Ctrl+L"
        };
        _stealthItem = new ToolStripMenuItem("Stealth Lock", null, OnStealthClicked);
        menu.Items.Add(_lockItem);
        menu.Items.Add(_stealthItem);
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add(new ToolStripMenuItem("Open MeowProof", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(new ToolStripMenuItem("Settings", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty)));
        menu.Items.Add(new ToolStripSeparator());

        _startupItem = new ToolStripMenuItem("Launch at Startup", null, OnStartupToggled)
        {
            CheckOnClick = true
        };
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add(new ToolStripMenuItem("Quit", null, (_, _) => QuitRequested?.Invoke(this, EventArgs.Empty)));

        _notifyIcon = new NotifyIcon
        {
            Icon = _unlockedIcon,
            Text = "MeowProof — Unlocked",
            Visible = false,
            ContextMenuStrip = menu
        };
        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Start()
    {
        _notifyIcon.Visible = true;
        _startupItem.Checked = StartupService.IsEnabled;
        UpdateForState();
    }

    private void OnLockClicked(object? sender, EventArgs e) => _lock.Toggle();

    private void OnStealthClicked(object? sender, EventArgs e)
    {
        if (_lock.IsLocked) _lock.Unlock();
        else _lock.StealthLock();
    }

    private void OnStartupToggled(object? sender, EventArgs e) =>
        StartupService.Apply(_startupItem.Checked);

    private void UpdateForState()
    {
        bool locked = _lock.IsLocked;
        _notifyIcon.Icon = locked ? _lockedIcon : _unlockedIcon;
        _notifyIcon.Text = locked ? "MeowProof — Locked" : "MeowProof — Unlocked";
        _lockItem.Text = locked ? "Unlock" : "Lock";
        _stealthItem.Enabled = !locked;
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

using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MeowProof.Core;
using MeowProof.Services;

namespace MeowProof.UI;

public partial class MainWindow : Window
{
    private readonly LockService _lock;
    private readonly GlobalHotkey _hotkey;

    /// <summary>When false (default), closing the window just hides it to the tray.</summary>
    public bool AllowClose { get; set; }

    public MainWindow(LockService lockService)
    {
        InitializeComponent();
        _lock = lockService;
        _lock.StateChanged += (_, _) => Dispatcher.Invoke(UpdateState);
        _hotkey = new GlobalHotkey(_lock.Toggle);
        SourceInitialized += (_, _) => _hotkey.Register(this);
        UpdateState();
    }

    private SolidColorBrush Brush(string key) => (SolidColorBrush)FindResource(key);

    private Brush Gradient(string key) => (Brush)FindResource(key);

    private void UpdateState()
    {
        bool locked = _lock.IsLocked;

        Hero.Locked = locked;
        Hero.Fill = locked ? Brush("Accent") : Brush("TextDim");
        HeroGlow.Visibility = locked ? Visibility.Visible : Visibility.Collapsed;

        StatusText.Text = locked ? "Keyboard Locked" : "Keyboard Unlocked";

        if (locked)
        {
            StatusPill.Background = Brush("AccentSoft");
            PillDot.Fill = Brush("Accent");
            PillText.Foreground = Brush("AccentDeep");
            PillText.Text = _lock.State == LockState.StealthLocked ? "Stealth" : "Locked";

            PrimaryActionButton.Background = Brush("Surface2");
            PrimaryActionButton.BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xD9, 0x64, 0x5C));
            PrimaryText.Text = "Unlock Keyboard";
            PrimaryText.Foreground = Brush("Red");
            PrimaryIcon.Stroke = Brush("Red");
        }
        else
        {
            StatusPill.Background = Brush("SageGlow");
            PillDot.Fill = Brush("Sage");
            PillText.Foreground = Brush("Sage");
            PillText.Text = "Ready";

            PrimaryActionButton.Background = Gradient("AccentGradient");
            PrimaryActionButton.BorderBrush = System.Windows.Media.Brushes.Transparent;
            PrimaryText.Text = "Lock Keyboard";
            var onAccent = System.Windows.Media.Brushes.White;
            PrimaryText.Foreground = onAccent;
            PrimaryIcon.Stroke = onAccent;
        }
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e) => Hide();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (AllowClose)
        {
            _hotkey.Dispose();
            return;
        }
        e.Cancel = true;
        Hide();
    }

    private void Primary_Click(object sender, RoutedEventArgs e) => _lock.Toggle();

    private void Stealth_Click(object sender, RoutedEventArgs e)
    {
        if (_lock.IsLocked) _lock.Unlock();
        else _lock.StealthLock();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.MessageBox.Show("Settings coming in step 8.", "MeowProof");
    }
}

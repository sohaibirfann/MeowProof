using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MeowProof.Services;

namespace MeowProof.UI;

public partial class MainWindow : Window
{
    private readonly LockService _lock;

    /// <summary>When false (default), closing the window just hides it to the tray.</summary>
    public bool AllowClose { get; set; }

    public MainWindow(LockService lockService)
    {
        InitializeComponent();
        _lock = lockService;
        _lock.StateChanged += (_, _) => Dispatcher.Invoke(UpdateState);
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
            StatusPill.Background = Brush("AccentGlow");
            PillDot.Fill = Brush("Accent");
            PillText.Foreground = Brush("Accent");
            PillText.Text = _lock.State == LockState.StealthLocked ? "Stealth" : "Locked";

            PrimaryActionButton.Background = Brush("Surface2");
            PrimaryActionButton.BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0x7A, 0x7A));
            PrimaryText.Text = "Unlock Keyboard";
            PrimaryText.Foreground = Brush("Red");
            PrimaryIcon.Stroke = Brush("Red");
        }
        else
        {
            StatusPill.Background = Brush("Surface2");
            PillDot.Fill = Brush("Green");
            PillText.Foreground = Brush("TextDim");
            PillText.Text = "Ready";

            PrimaryActionButton.Background = Gradient("AccentGradient");
            PrimaryActionButton.BorderBrush = System.Windows.Media.Brushes.Transparent;
            PrimaryText.Text = "Lock Keyboard";
            var dark = new SolidColorBrush(Color.FromRgb(0x06, 0x20, 0x1E));
            PrimaryText.Foreground = dark;
            PrimaryIcon.Stroke = dark;
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
        // The app lives in the tray; closing the window just hides it,
        // unless the app is actually quitting.
        if (AllowClose) return;
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

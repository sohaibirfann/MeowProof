using System.Windows;
using System.Windows.Interop;
using MeowProof.Helpers;

namespace MeowProof.Core;

/// <summary>
/// Registers a system-wide hotkey (Ctrl+Shift+L) that fires regardless of
/// which app is in the foreground. Calls <paramref name="onTriggered"/> on
/// every press — wiring to Toggle() makes it lock and unlock.
/// </summary>
public sealed class GlobalHotkey : IDisposable
{
    private const int HotkeyId = 0x4D50; // 'MP' — unique enough to avoid collisions

    private readonly Action _onTriggered;
    private IntPtr _hwnd;
    private HwndSource? _source;

    public GlobalHotkey(Action onTriggered)
    {
        _onTriggered = onTriggered;
    }

    public void Register(Window owner)
    {
        _hwnd = new WindowInteropHelper(owner).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source?.AddHook(WndProc);

        Win32.RegisterHotKey(_hwnd, HotkeyId,
            Win32.MOD_CONTROL | Win32.MOD_SHIFT, (uint)Win32.VK_L);
    }

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero)
            Win32.UnregisterHotKey(_hwnd, HotkeyId);
        _source?.RemoveHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            _onTriggered();
            handled = true;
        }
        return IntPtr.Zero;
    }
}

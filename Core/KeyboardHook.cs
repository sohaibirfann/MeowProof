using System.Runtime.InteropServices;
using MeowProof.Helpers;

namespace MeowProof.Core;

/// <summary>
/// Installs a system-wide WH_KEYBOARD_LL hook. While active, swallows every
/// keystroke except the unlock shortcut (Ctrl+Shift+L), which calls
/// <paramref name="onUnlock"/> and is also swallowed so no app receives it.
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private readonly Win32.LowLevelKeyboardProc _proc;
    private readonly Action _onUnlock;
    private IntPtr _hook;

    // Track modifier state ourselves — GetAsyncKeyState is unreliable for keys
    // the hook itself swallowed on the way down.
    private bool _ctrlDown;
    private bool _shiftDown;

    public bool IsInstalled => _hook != IntPtr.Zero;

    public KeyboardHook(Action onUnlock)
    {
        _onUnlock = onUnlock;
        // Strong reference — delegate must not be GC'd while the hook is live.
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hook != IntPtr.Zero) return;
        using var mod = System.Diagnostics.Process.GetCurrentProcess().MainModule!;
        _hook = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, _proc,
            Win32.GetModuleHandle(mod.ModuleName), 0);
    }

    public void Uninstall()
    {
        if (_hook == IntPtr.Zero) return;
        Win32.UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vk = Marshal.ReadInt32(lParam);
            bool down = wParam == Win32.WM_KEYDOWN || wParam == Win32.WM_SYSKEYDOWN;
            bool up   = wParam == Win32.WM_KEYUP   || wParam == Win32.WM_SYSKEYUP;

            // Keep modifier state up to date before checking the combo.
            if (vk is Win32.VK_LCONTROL or Win32.VK_RCONTROL)
            {
                _ctrlDown = down || (!up && _ctrlDown);
                return (IntPtr)1;
            }
            if (vk is Win32.VK_LSHIFT or Win32.VK_RSHIFT)
            {
                _shiftDown = down || (!up && _shiftDown);
                return (IntPtr)1;
            }

            if (down && vk == Win32.VK_L && _ctrlDown && _shiftDown)
            {
                // BeginInvoke: don't block the hook thread waiting on the UI thread.
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(_onUnlock);
                return (IntPtr)1;
            }

            return (IntPtr)1;
        }

        return Win32.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose() => Uninstall();
}

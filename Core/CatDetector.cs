using System.Collections.Generic;
using System.Runtime.InteropServices;
using MeowProof.Helpers;
using MeowProof.Models;

namespace MeowProof.Core;

/// <summary>
/// Passive WH_KEYBOARD_LL hook that scores keypress patterns against a
/// cat-walking signature. Never swallows keys — observation only.
/// Fires <see cref="CatDetected"/> when the chaos score crosses the threshold
/// for at least 300 ms continuously.
/// </summary>
public sealed class CatDetector : IDisposable
{
    public event Action? CatDetected;

    private readonly Win32.LowLevelKeyboardProc _proc;
    private IntPtr _hook;

    // Sliding window of key-down timestamps (last 2 s)
    private readonly Queue<long> _timestamps = new();
    private readonly Queue<int>  _keyCodes   = new();
    private readonly HashSet<int> _heldKeys  = new();

    // Keys held-down start times
    private readonly Dictionary<int, long> _holdStart = new();

    // Timestamp of the previous key event — a long gap means a fresh burst.
    private long _lastKeyTick;

    // Ticks when score first crossed threshold (for 300 ms hysteresis)
    private long _aboveThresholdSince = 0;

    // Modifier-only VKs — never count toward cat score
    private static readonly HashSet<int> Modifiers = new()
    {
        Win32.VK_LCONTROL, Win32.VK_RCONTROL,
        Win32.VK_LSHIFT,   Win32.VK_RSHIFT,
        0xA4, 0xA5,  // LMENU, RMENU
        0x5B, 0x5C,  // LWIN, RWIN
    };

    private const long TicksPerMs = TimeSpan.TicksPerMillisecond;
    private const long WindowTicks  = 2_000 * TicksPerMs;
    private const long IdleGuard    = 5_000 * TicksPerMs;
    private const long HysteresisTicks = 300 * TicksPerMs;

    public CatDetector()
    {
        _proc = HookCallback;
        _lastKeyTick = DateTime.UtcNow.Ticks;
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
        Reset();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // When the feature is off, pass through with no bookkeeping at all.
        if (nCode >= 0 && AppSettings.Current.CatDetectionEnabled)
        {
            int vk   = Marshal.ReadInt32(lParam);
            bool down = wParam == Win32.WM_KEYDOWN || wParam == Win32.WM_SYSKEYDOWN;
            bool up   = wParam == Win32.WM_KEYUP   || wParam == Win32.WM_SYSKEYUP;

            if (!Modifiers.Contains(vk))
            {
                long now = DateTime.UtcNow.Ticks;

                if (down)
                {
                    // A long quiet gap before this key starts a fresh burst —
                    // clear any stale window so old keystrokes don't carry over.
                    if (now - _lastKeyTick > IdleGuard) Reset();
                    _lastKeyTick = now;

                    _timestamps.Enqueue(now);
                    _keyCodes.Enqueue(vk);
                    _heldKeys.Add(vk);
                    _holdStart.TryAdd(vk, now);
                    Prune(now);
                    Score(now);
                }
                else if (up)
                {
                    _heldKeys.Remove(vk);
                    _holdStart.Remove(vk);
                    _lastKeyTick = now;
                }
            }
        }

        return Win32.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private void Prune(long now)
    {
        while (_timestamps.Count > 0 && now - _timestamps.Peek() > WindowTicks)
        {
            _timestamps.Dequeue();
            _keyCodes.Dequeue();
        }
    }

    private void Score(long now)
    {
        float score = 0f;

        // 1. Rapid fire: fraction of consecutive pairs < 60 ms apart
        if (_timestamps.Count >= 3)
        {
            var ts = _timestamps.ToArray();
            int rapid = 0;
            for (int i = 1; i < ts.Length; i++)
                if (ts[i] - ts[i - 1] < 60 * TicksPerMs) rapid++;
            score += (float)rapid / (ts.Length - 1) * 40f;
        }

        // 2. Key diversity (Shannon entropy of recent key codes)
        if (_keyCodes.Count >= 4)
        {
            var codes = _keyCodes.ToArray();
            var freq = new Dictionary<int, int>();
            foreach (var k in codes) freq[k] = freq.GetValueOrDefault(k) + 1;
            float entropy = 0;
            foreach (var f in freq.Values)
            {
                float p = (float)f / codes.Length;
                entropy -= p * MathF.Log2(p);
            }
            // Cats spread across the whole keyboard — high entropy
            score += Math.Min(entropy / 3f, 1f) * 35f;
        }

        // 3. Simultaneous keys held (whole paw)
        if (_heldKeys.Count >= 3)
            score += Math.Min(_heldKeys.Count - 2, 4) * 8f;

        // 4. Long holds
        foreach (var (_, start) in _holdStart)
            if (now - start > 500 * TicksPerMs) { score += 15f; break; }

        const float threshold = 55f;

        if (score >= threshold)
        {
            if (_aboveThresholdSince == 0)
                _aboveThresholdSince = now;
            else if (now - _aboveThresholdSince >= HysteresisTicks)
            {
                Reset();
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(
                    () => CatDetected?.Invoke());
            }
        }
        else
        {
            _aboveThresholdSince = 0;
        }
    }

    private void Reset()
    {
        _timestamps.Clear();
        _keyCodes.Clear();
        _heldKeys.Clear();
        _holdStart.Clear();
        _aboveThresholdSince = 0;
        _lastKeyTick = DateTime.UtcNow.Ticks;
    }

    public void Dispose() => Uninstall();
}

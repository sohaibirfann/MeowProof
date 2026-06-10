using MeowProof.Core;
using MeowProof.Models;

namespace MeowProof.Services;

public enum LockState
{
    Unlocked,
    Locked,
    StealthLocked
}

/// <summary>
/// Central state machine for the lock. Installs / removes the keyboard hook
/// on state transitions and broadcasts <see cref="StateChanged"/> so the UI
/// stays consistent.
/// </summary>
public sealed class LockService : IDisposable
{
    private readonly KeyboardHook _hook;

    public LockState State { get; private set; } = LockState.Unlocked;

    public bool IsLocked => State != LockState.Unlocked;

    /// <summary>Raised whenever <see cref="State"/> changes.</summary>
    public event EventHandler? StateChanged;

    public LockService()
    {
        _hook = new KeyboardHook(Unlock);
        _hook.KeyBlocked += SoundService.PlayKeyBlocked;
    }

    public void Lock()
    {
        if (State == LockState.Locked) return;
        State = LockState.Locked;
        _hook.Install();
        if (AppSettings.Current.PreventSleep) SleepPreventer.Prevent();
        SoundService.PlayLocked();
        OnChanged();
    }

    public void StealthLock()
    {
        if (State == LockState.StealthLocked) return;
        State = LockState.StealthLocked;
        _hook.Install();
        if (AppSettings.Current.PreventSleep) SleepPreventer.Prevent();
        SoundService.PlayLocked();
        OnChanged();
    }

    public void Unlock()
    {
        if (State == LockState.Unlocked) return;
        State = LockState.Unlocked;
        _hook.Uninstall();
        SleepPreventer.Allow();
        OnChanged();
    }

    /// <summary>Locks if unlocked, unlocks otherwise. Used by the primary button.</summary>
    public void Toggle()
    {
        if (IsLocked) Unlock();
        else Lock();
    }

    public void Dispose() => _hook.Dispose();

    private void OnChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}

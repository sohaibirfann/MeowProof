namespace MeowProof.Services;

public enum LockState
{
    Unlocked,
    Locked,
    StealthLocked
}

/// <summary>
/// Central state machine for the lock. Everything (tray, main window, overlay)
/// subscribes to <see cref="StateChanged"/> so the UI stays consistent.
///
/// For now this only tracks state. Later build steps wire in the real behavior:
///   step 3 — install/remove the low-level keyboard hook
///   step 4 — show/hide the overlay window
///   step 7 — prevent sleep while locked
/// </summary>
public sealed class LockService
{
    public LockState State { get; private set; } = LockState.Unlocked;

    public bool IsLocked => State != LockState.Unlocked;

    /// <summary>Raised whenever <see cref="State"/> changes.</summary>
    public event EventHandler? StateChanged;

    public void Lock()
    {
        if (State == LockState.Locked) return;
        State = LockState.Locked;
        OnChanged();
    }

    public void StealthLock()
    {
        if (State == LockState.StealthLocked) return;
        State = LockState.StealthLocked;
        OnChanged();
    }

    public void Unlock()
    {
        if (State == LockState.Unlocked) return;
        State = LockState.Unlocked;
        OnChanged();
    }

    /// <summary>Locks if unlocked, unlocks otherwise. Used by the primary button and hotkey.</summary>
    public void Toggle()
    {
        if (IsLocked) Unlock();
        else Lock();
    }

    private void OnChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}

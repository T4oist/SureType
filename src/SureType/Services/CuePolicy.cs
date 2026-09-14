namespace SureType.Services;

public sealed class CuePolicy
{
    private long? _lastFocus;
    private long? _lastCue;
    public bool ShouldShow(long now, bool changed, bool focus, bool manual, bool paused,
        bool suppressed, bool showChanges, bool showFocus, double cooldownSeconds)
    {
        if (manual) return true;
        if (paused || suppressed) return false;
        var focusDue = focus && showFocus && (!_lastFocus.HasValue || now - _lastFocus.Value >= cooldownSeconds * 1000);
        if (!(changed && showChanges) && !focusDue) return false;
        if (!changed && _lastCue.HasValue && now - _lastCue.Value < 150) return false;
        if (focusDue) _lastFocus = now;
        _lastCue = now;
        return true;
    }
    public void Reset() { _lastFocus = null; _lastCue = null; }
}

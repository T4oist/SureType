namespace SureType.Services;

public sealed class TypingCuePolicy
{
    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(2);

    private readonly TimeSpan _cooldown;
    private DateTimeOffset _lastCueAt = DateTimeOffset.MinValue;

    public TypingCuePolicy(TimeSpan? cooldown = null)
    {
        _cooldown = cooldown ?? DefaultCooldown;
    }

    public bool ShouldCue(DateTimeOffset now)
    {
        if (now - _lastCueAt <= _cooldown)
        {
            return false;
        }

        _lastCueAt = now;
        return true;
    }
}
namespace SureType.Models;

public sealed class AppSettings
{
    private double _overlayDurationSeconds = 1.5;
    private double _inputFocusCooldownSeconds = 8;
    private bool _showOnStateChange = true;

    public event EventHandler? Changed;

    public double OverlayDurationSeconds
    {
        get => _overlayDurationSeconds;
        set => SetValue(ref _overlayDurationSeconds, Math.Clamp(value, 0.5, 5));
    }

    public double InputFocusCooldownSeconds
    {
        get => _inputFocusCooldownSeconds;
        set => SetValue(ref _inputFocusCooldownSeconds, Math.Clamp(value, 1, 30));
    }

    public bool ShowOnStateChange
    {
        get => _showOnStateChange;
        set => SetValue(ref _showOnStateChange, value);
    }

    private void SetValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
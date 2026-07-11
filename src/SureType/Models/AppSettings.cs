namespace SureType.Models;

public enum OverlayPosition
{
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft
}

public enum LogoStyle
{
    Filled,
    Soft,
    Mono
}

public enum AppLanguage
{
    Chinese,
    English
}

public sealed class AppSettings
{
    private double _overlayDurationSeconds = 1.5;
    private double _inputFocusCooldownSeconds = 8;
    private bool _showOnStateChange = true;
    private bool _startWithWindows;
    private OverlayPosition _overlayPosition = OverlayPosition.TopRight;
    private LogoStyle _logoStyle = LogoStyle.Filled;
    private AppLanguage _language = AppLanguage.Chinese;

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

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set => SetValue(ref _startWithWindows, value);
    }

    public OverlayPosition OverlayPosition
    {
        get => _overlayPosition;
        set => SetValue(ref _overlayPosition, value);
    }

    public LogoStyle LogoStyle
    {
        get => _logoStyle;
        set => SetValue(ref _logoStyle, value);
    }

    public AppLanguage Language
    {
        get => _language;
        set => SetValue(ref _language, value);
    }

    private void SetValue<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
    }
}

namespace SureType.Models;

public enum OverlayPosition { TopRight, TopLeft, BottomRight, BottomLeft, NearMouse }
public enum LogoStyle { Filled, Soft, Mono }
public enum AppLanguage { Chinese, English }

public sealed class AppSettings
{
    private double _duration = 1.5, _cooldown = 8, _size = 56;
    private bool _stateChange = true, _focus = true, _fullscreen = true, _startup, _onboarded;
    private OverlayPosition _position;
    private LogoStyle _style;
    private AppLanguage _language;
    private string[] _excluded = [];
    public event EventHandler? Changed;
    public double OverlayDurationSeconds { get => _duration; set => Set(ref _duration, Finite(value, 0.5, 5, 1.5)); }
    public double InputFocusCooldownSeconds { get => _cooldown; set => Set(ref _cooldown, Finite(value, 1, 30, 8)); }
    public double OverlaySize { get => _size; set => Set(ref _size, Finite(value, 40, 96, 56)); }
    public bool ShowOnStateChange { get => _stateChange; set => Set(ref _stateChange, value); }
    public bool ShowOnInputFocus { get => _focus; set => Set(ref _focus, value); }
    public bool SuppressFullscreen { get => _fullscreen; set => Set(ref _fullscreen, value); }
    public bool StartWithWindows { get => _startup; set => Set(ref _startup, value); }
    public bool HasCompletedOnboarding { get => _onboarded; set => Set(ref _onboarded, value); }
    public OverlayPosition OverlayPosition { get => _position; set => Set(ref _position, Enum.IsDefined(value) ? value : OverlayPosition.TopRight); }
    public LogoStyle LogoStyle { get => _style; set => Set(ref _style, Enum.IsDefined(value) ? value : LogoStyle.Filled); }
    public AppLanguage Language { get => _language; set => Set(ref _language, Enum.IsDefined(value) ? value : AppLanguage.Chinese); }
    public string[] ExcludedApplications
    {
        get => (string[])_excluded.Clone();
        set => Set(ref _excluded, (value ?? []).Where(p => !string.IsNullOrWhiteSpace(p) && System.IO.Path.IsPathFullyQualified(p))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }
    public void ResetPreferences()
    {
        OverlayDurationSeconds = 1.5; InputFocusCooldownSeconds = 8; OverlaySize = 56;
        ShowOnStateChange = true; ShowOnInputFocus = true; SuppressFullscreen = true;
        OverlayPosition = OverlayPosition.TopRight; LogoStyle = LogoStyle.Filled;
        ExcludedApplications = [];
    }
    private static double Finite(double value, double min, double max, double fallback) =>
        double.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
    private void Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

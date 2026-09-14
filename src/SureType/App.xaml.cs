using System.Windows.Threading;
using SureType.Models;
using SureType.Services;
using SureType.Windows;

namespace SureType;

public partial class App : System.Windows.Application
{
    public App() { }
    private readonly string _instanceName = "SureType";
    internal App(SettingsStore store, string instanceName = "SureType") { _store = store; _instanceName = instanceName; }
    private SingleInstance? _instance;
    private InputStateService? _service;
    private OverlayWindow? _overlay;
    private TrayService? _tray;
    private SettingsStore? _store;
    private AppSettings? _settings;
    private DispatcherTimer? _saveTimer;
    private bool _exiting, _dirty;
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        var automatic = e.Args.Contains("--startup", StringComparer.OrdinalIgnoreCase);
        _instance = new SingleInstance(_instanceName);
        if (!_instance.IsFirst)
        {
            if (!automatic) _instance.Signal();
            Shutdown();
            return;
        }
        _store ??= new SettingsStore();
        _settings = _store.Load();
        _settings.StartWithWindows = StartupService.IsEnabled();
        _overlay = new OverlayWindow(_settings);
        _service = new InputStateService(new Win32InputStateReader(), _settings);
        _tray = new TrayService(_settings, _service, _overlay);
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _saveTimer.Tick += (_, _) => SaveSettings();
        _settings.Changed += SettingsChanged;
        _service.StateChanged += (_, args) => _overlay.ShowState(args.State);
        _service.PauseChanged += (_, _) => { if (_service.IsPaused) _overlay.HideImmediately(); };
        _instance.Listen(() =>
        {
            if (!_exiting) Dispatcher.BeginInvoke(() => { if (!_exiting) _tray.OpenMainWindow(); });
        });
        _service.Start();
        if (!automatic || !_settings.HasCompletedOnboarding) _tray.OpenMainWindow();
        _tray.ReportError(_store.LastError);
    }
    private void SettingsChanged(object? sender, EventArgs e)
    {
        _dirty = true;
        _saveTimer!.Stop(); _saveTimer.Start();
    }
    private void SaveSettings()
    {
        _saveTimer?.Stop();
        if (!_dirty || _store == null || _settings == null) return;
        if (_store.Save(_settings)) _dirty = false;
        _tray?.ReportError(_store.LastError);
    }
    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _exiting = true;
        if (_settings != null) _settings.Changed -= SettingsChanged;
        SaveSettings();
        _service?.Dispose(); _tray?.Dispose(); _overlay?.Close(); _instance?.Dispose();
        base.OnExit(e);
    }
}

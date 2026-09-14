using System.Windows.Threading;
using SureType.Models;

namespace SureType.Services;

public sealed class InputStateService : IDisposable
{
    private readonly IInputStateReader _reader;
    private readonly AppSettings _settings;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _pollTimer;
    private readonly CuePolicy _policy = new(); private readonly Func<ForegroundContext> _context;
    private ForegroundWindowHook? _foreground;
    private InputFocusHook? _focusHook;
    private bool _sampling, _pending, _focusPending, _manualPending, _started;
    private volatile bool _disposed;
    public InputStateService(IInputStateReader reader, AppSettings settings) : this(reader, settings, ForegroundContext.Read) { }
    internal InputStateService(IInputStateReader reader, AppSettings settings, Func<ForegroundContext> context)
    {
        _reader = reader; _settings = settings; _context = context;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _pollTimer.Tick += (_, _) => Request();
    }
    public event EventHandler<InputStateChangedEventArgs>? StateChanged;
    public event EventHandler<InputStateChangedEventArgs>? StateUpdated;
    public event EventHandler? PauseChanged;
    public InputState CurrentState { get; private set; } = InputState.Unknown;
    public bool IsPaused { get; private set; }
    public void Start()
    {
        if (_started || _disposed) return;
        _started = true;
        _foreground = new ForegroundWindowHook(() => Request());
        _focusHook = new InputFocusHook(() => Request(focus: true));
        _foreground.Start(); _focusHook.Start(); _pollTimer.Start(); Request();
    }
    public void TogglePaused()
    {
        if (_disposed) return;
        IsPaused = !IsPaused;
        _policy.Reset();
        PauseChanged?.Invoke(this, EventArgs.Empty);
        if (!IsPaused) Request();
    }
    public void ShowCurrentState() => Request(manual: true);
    internal void Request(bool focus = false, bool manual = false)
    {
        if (_disposed) return;
        if (!_dispatcher.CheckAccess())
        {
            _dispatcher.BeginInvoke(() => Request(focus, manual));
            return;
        }
        _focusPending |= focus; _manualPending |= manual; _pending = true;
        if (!_sampling) _ = SampleAsync();
    }
    private async Task SampleAsync()
    {
        _sampling = true;
        try
        {
            while (_pending && !_disposed)
            {
                _pending = false;
                var focus = _focusPending; var manual = _manualPending;
                _focusPending = false; _manualPending = false;
                var result = await Task.Run(() =>
                {
                    var before = _context();
                    InputState state;
                    try { state = _reader.ReadCurrentState(); }
                    catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException or System.Runtime.InteropServices.COMException)
                    { state = InputState.Unknown; }
                    var after = _context();
                    return (state, context: after, stable: before.Handle == after.Handle);
                });
                if (_disposed) return;
                if (!result.stable)
                {
                    // Retry on the next timer tick, never spin while the user switches windows.
                    _manualPending |= manual; _focusPending |= focus;
                    continue;
                }
                var changed = result.state != CurrentState;
                CurrentState = result.state;
                StateUpdated?.Invoke(this, new(CurrentState));
                if (_policy.ShouldShow(Environment.TickCount64, changed, focus, manual, IsPaused,
                    result.context.IsSuppressed(_settings.SuppressFullscreen, _settings.ExcludedApplications),
                    _settings.ShowOnStateChange, _settings.ShowOnInputFocus, _settings.InputFocusCooldownSeconds))
                    StateChanged?.Invoke(this, new(CurrentState));
            }
        }
        finally { _sampling = false; }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; _pollTimer.Stop(); _foreground?.Dispose(); _focusHook?.Dispose();
    }
}

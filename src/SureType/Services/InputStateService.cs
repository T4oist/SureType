using System.Windows.Threading;
using SureType.Models;

namespace SureType.Services;

public sealed class InputStateService : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private readonly IInputStateReader _reader;
    private readonly DispatcherTimer _pollTimer;
    private ForegroundWindowHook? _foregroundWindowHook;
    private InputFocusHook? _inputFocusHook;
    private InputState _currentState = InputState.Unknown;
    private readonly TypingCuePolicy _inputFocusCuePolicy = new(TimeSpan.FromSeconds(8));
    private bool _disposed;

    public InputStateService(IInputStateReader reader)
    {
        _reader = reader;
        _pollTimer = new DispatcherTimer { Interval = PollInterval };
        _pollTimer.Tick += (_, _) => Refresh(true);
    }

    public event EventHandler<InputStateChangedEventArgs>? StateChanged;

    public InputState CurrentState => _currentState;

    public bool IsPaused { get; private set; }

    public void Start()
    {
        _foregroundWindowHook = new ForegroundWindowHook(() => Refresh(false));
        _inputFocusHook = new InputFocusHook(OnInputFocused);
        _foregroundWindowHook.Start();
        _inputFocusHook.Start();
        _pollTimer.Start();
        Refresh(false);
    }

    public void TogglePaused()
    {
        IsPaused = !IsPaused;
    }

    public void ShowCurrentState()
    {
        Refresh(true, forceNotify: true);
    }

    private void Refresh(bool notifyWhenChanged, bool forceNotify = false)
    {
        if (_disposed)
        {
            return;
        }

        var nextState = _reader.ReadCurrentState();
        if (!forceNotify && nextState == _currentState)
        {
            return;
        }

        _currentState = nextState;

        if (forceNotify || notifyWhenChanged)
        {
            StateChanged?.Invoke(this, new InputStateChangedEventArgs(nextState));
        }
    }

    private void OnInputFocused()
    {
        if (_inputFocusCuePolicy.ShouldCue(DateTimeOffset.UtcNow))
        {
            Refresh(true, forceNotify: true);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pollTimer.Stop();
        _foregroundWindowHook?.Dispose();
        _inputFocusHook?.Dispose();
    }
}

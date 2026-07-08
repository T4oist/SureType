using System.Runtime.InteropServices;

namespace SureType.Services;

public sealed class ForegroundWindowHook : IDisposable
{
    private const uint EventSystemForeground = 0x0003;
    private const uint WineventOutofcontext = 0x0000;

    private readonly Action _onForegroundChanged;
    private readonly WinEventDelegate _proc;
    private nint _hookId;
    private bool _disposed;

    public ForegroundWindowHook(Action onForegroundChanged)
    {
        _onForegroundChanged = onForegroundChanged;
        _proc = WinEventCallback;
    }

    public void Start()
    {
        if (_hookId != nint.Zero)
        {
            return;
        }

        _hookId = SetWinEventHook(
            EventSystemForeground,
            EventSystemForeground,
            nint.Zero,
            _proc,
            0,
            0,
            WineventOutofcontext);
    }

    private void WinEventCallback(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint eventThread, uint eventTime)
    {
        _onForegroundChanged();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_hookId != nint.Zero)
        {
            UnhookWinEvent(_hookId);
            _hookId = nint.Zero;
        }
    }

    private delegate void WinEventDelegate(nint hWinEventHook, uint eventType, nint hwnd, int idObject, int idChild, uint eventThread, uint eventTime);

    [DllImport("user32.dll")]
    private static extern nint SetWinEventHook(uint eventMin, uint eventMax, nint hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(nint hWinEventHook);
}

using System.Security.Principal;

namespace SureType.Services;

public sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activate;
    private RegisteredWaitHandle? _wait;
    public bool IsFirst { get; }
    public SingleInstance() : this("SureType") { }
    internal SingleInstance(string name)
    {
        var suffix = WindowsIdentity.GetCurrent().User!.Value;
        _mutex = new Mutex(true, @"Local\" + name + "-" + suffix, out var first);
        IsFirst = first;
        _activate = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\" + name + "-activate-" + suffix);
    }
    public void Listen(Action activate) => _wait = ThreadPool.RegisterWaitForSingleObject(_activate, (_, _) => activate(), null, Timeout.Infinite, false);
    public void Signal() => _activate.Set();
    public void Dispose()
    {
        _wait?.Unregister(null);
        _activate.Dispose();
        if (IsFirst) _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}

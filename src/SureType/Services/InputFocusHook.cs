using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace SureType.Services;

public sealed class InputFocusHook : IDisposable
{
    private readonly Action _onInputFocused;
    private readonly ManualResetEventSlim _stop = new();
    private Thread? _thread;
    private volatile bool _disposed;
    private readonly int _processId = Environment.ProcessId;
    public InputFocusHook(Action onInputFocused) => _onInputFocused = onInputFocused;
    public void Start()
    {
        if (_thread != null || _disposed) return;
        _thread = new Thread(() =>
        {
            var subscribed = false;
            try
            {
                Automation.AddAutomationFocusChangedEventHandler(OnFocusChanged);
                subscribed = true;
                _stop.Wait();
            }
            catch (Exception ex) when (ex is COMException or InvalidOperationException) { }
            finally
            {
                if (subscribed)
                {
                    try { Automation.RemoveAutomationFocusChangedEventHandler(OnFocusChanged); }
                    catch (Exception ex) when (ex is COMException or InvalidOperationException) { }
                }
                _stop.Dispose();
            }
        }) { IsBackground = true, Name = "SureType focus events" };
        _thread.SetApartmentState(ApartmentState.MTA);
        _thread.Start();
    }
    private void OnFocusChanged(object sender, AutomationFocusChangedEventArgs e)
    {
        if (_disposed || sender is not AutomationElement element) return;
        try
        {
            var current = element.Current;
            if (current.ProcessId == _processId || !current.IsEnabled || current.IsOffscreen) return;
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern))
            {
                if (!((ValuePattern)pattern).Current.IsReadOnly) _onInputFocused();
                return;
            }
            if (element.TryGetCurrentPattern(TextPattern.Pattern, out var textPattern))
            {
                var readOnly = ((TextPattern)textPattern).DocumentRange.GetAttributeValue(TextPattern.IsReadOnlyAttribute);
                if (readOnly is false) _onInputFocused();
                return;
            }
            if (current.ControlType == ControlType.Edit) _onInputFocused();
        }
        catch (Exception ex) when (ex is ElementNotAvailableException or InvalidOperationException or COMException) { }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_thread == null) _stop.Dispose();
        else { try { _stop.Set(); } catch (ObjectDisposedException) { } _thread.Join(500); }
    }
}

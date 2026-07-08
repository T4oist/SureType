using System.Runtime.InteropServices;
using System.Windows.Automation;

namespace SureType.Services;

public sealed class InputFocusHook : IDisposable
{
    private readonly Action _onInputFocused;
    private bool _started;
    private bool _disposed;

    public InputFocusHook(Action onInputFocused)
    {
        _onInputFocused = onInputFocused;
    }

    public void Start()
    {
        if (_started)
        {
            return;
        }

        Automation.AddAutomationFocusChangedEventHandler(OnFocusChanged);
        _started = true;
    }

    private void OnFocusChanged(object sender, AutomationFocusChangedEventArgs e)
    {
        if (_disposed || sender is not AutomationElement element)
        {
            return;
        }

        if (IsTextInputElement(element))
        {
            _onInputFocused();
        }
    }

    private static bool IsTextInputElement(AutomationElement element)
    {
        try
        {
            var controlType = element.Current.ControlType;
            if (controlType == ControlType.Edit ||
                controlType == ControlType.Document ||
                controlType == ControlType.ComboBox)
            {
                return true;
            }

            return element.TryGetCurrentPattern(ValuePattern.Pattern, out _) ||
                   element.TryGetCurrentPattern(TextPattern.Pattern, out _);
        }
        catch (ElementNotAvailableException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (COMException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_started)
        {
            Automation.RemoveAutomationFocusChangedEventHandler(OnFocusChanged);
            _started = false;
        }
    }
}
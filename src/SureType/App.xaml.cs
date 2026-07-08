using System.Windows;
using SureType.Services;
using SureType.Windows;

namespace SureType;

public partial class App : System.Windows.Application
{
    private InputStateService? _inputStateService;
    private OverlayWindow? _overlayWindow;
    private TrayService? _trayService;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        _overlayWindow = new OverlayWindow();
        _inputStateService = new InputStateService(new Win32InputStateReader());
        _trayService = new TrayService(_inputStateService, _overlayWindow);

        _inputStateService.StateChanged += (_, args) =>
        {
            if (!_inputStateService.IsPaused)
            {
                _overlayWindow.ShowState(args.State);
            }
        };

        _inputStateService.Start();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _inputStateService?.Dispose();
        _trayService?.Dispose();
        _overlayWindow?.Close();
        base.OnExit(e);
    }
}

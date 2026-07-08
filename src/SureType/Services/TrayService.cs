using System.Drawing;
using SureType.Models;
using SureType.Windows;
using Forms = System.Windows.Forms;

namespace SureType.Services;

public sealed class TrayService : IDisposable
{
    private readonly AppSettings _settings;
    private readonly InputStateService _inputStateService;
    private readonly OverlayWindow _overlayWindow;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _pauseItem;
    private MainWindow? _mainWindow;

    public TrayService(AppSettings settings, InputStateService inputStateService, OverlayWindow overlayWindow)
    {
        _settings = settings;
        _inputStateService = inputStateService;
        _overlayWindow = overlayWindow;

        var openItem = new Forms.ToolStripMenuItem("Open SureType");
        openItem.Click += (_, _) => OpenMainWindow();

        var showItem = new Forms.ToolStripMenuItem("Show current state");
        showItem.Click += (_, _) => ShowCurrentState();

        _pauseItem = new Forms.ToolStripMenuItem("Pause");
        _pauseItem.Click += (_, _) => TogglePaused();

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => System.Windows.Application.Current.Shutdown();

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "SureType",
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };

        _notifyIcon.ContextMenuStrip.Items.Add(openItem);
        _notifyIcon.ContextMenuStrip.Items.Add(showItem);
        _notifyIcon.ContextMenuStrip.Items.Add(_pauseItem);
        _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());
        _notifyIcon.ContextMenuStrip.Items.Add(exitItem);
        _notifyIcon.DoubleClick += (_, _) => OpenMainWindow();
    }

    private void OpenMainWindow()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_mainWindow is null || !_mainWindow.IsLoaded)
            {
                _mainWindow = new MainWindow(_settings, _inputStateService, _overlayWindow);
                _mainWindow.Closed += (_, _) => _mainWindow = null;
            }

            _mainWindow.Show();
            _mainWindow.Activate();
        });
    }

    private void TogglePaused()
    {
        _inputStateService.TogglePaused();
        _pauseItem.Text = _inputStateService.IsPaused ? "Resume" : "Pause";
    }

    private void ShowCurrentState()
    {
        _inputStateService.ShowCurrentState();
        _overlayWindow.ShowState(_inputStateService.CurrentState);
    }

    public void Dispose()
    {
        _mainWindow?.Close();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
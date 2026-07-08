using System.Drawing;
using System.Windows;
using SureType.Windows;
using Forms = System.Windows.Forms;

namespace SureType.Services;

public sealed class TrayService : IDisposable
{
    private readonly InputStateService _inputStateService;
    private readonly OverlayWindow _overlayWindow;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _pauseItem;

    public TrayService(InputStateService inputStateService, OverlayWindow overlayWindow)
    {
        _inputStateService = inputStateService;
        _overlayWindow = overlayWindow;

        _pauseItem = new Forms.ToolStripMenuItem("Pause");
        _pauseItem.Click += (_, _) => TogglePaused();

        var showItem = new Forms.ToolStripMenuItem("Show current state");
        showItem.Click += (_, _) => ShowCurrentState();

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => System.Windows.Application.Current.Shutdown();

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "SureType",
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };

        _notifyIcon.ContextMenuStrip.Items.Add(showItem);
        _notifyIcon.ContextMenuStrip.Items.Add(_pauseItem);
        _notifyIcon.ContextMenuStrip.Items.Add(new Forms.ToolStripSeparator());
        _notifyIcon.ContextMenuStrip.Items.Add(exitItem);
        _notifyIcon.DoubleClick += (_, _) => ShowCurrentState();
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
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}

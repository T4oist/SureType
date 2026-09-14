using System.Drawing;
using SureType.Models;
using SureType.Windows;
using Forms = System.Windows.Forms;

namespace SureType.Services;

public sealed class TrayService : IDisposable
{
    private readonly AppSettings _settings;
    private readonly InputStateService _service;
    private readonly OverlayWindow _overlay;
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ToolStripMenuItem _open = new(), _show = new(), _pause = new(), _exit = new();
    private MainWindow? _window;
    private string? _error;
    public TrayService(AppSettings settings, InputStateService service, OverlayWindow overlay)
    {
        _settings = settings; _service = service; _overlay = overlay;
        var menu = new Forms.ContextMenuStrip();
        menu.Items.AddRange([_open, _show, _pause, new Forms.ToolStripSeparator(), _exit]);
        _icon = new Forms.NotifyIcon { Icon = LoadIcon(), Visible = true, ContextMenuStrip = menu };
        _open.Click += (_, _) => OpenMainWindow();
        _show.Click += (_, _) => _service.ShowCurrentState();
        _pause.Click += (_, _) => _service.TogglePaused();
        _exit.Click += (_, _) => System.Windows.Application.Current.Shutdown();
        _icon.DoubleClick += (_, _) => OpenMainWindow();
        _settings.Changed += OnChanged; _service.PauseChanged += OnChanged;
        UpdateText();
    }
    private void OnChanged(object? sender, EventArgs e) => UpdateText();
    private void UpdateText()
    {
        bool zh = _settings.Language == AppLanguage.Chinese;
        _open.Text = zh ? "打开 SureType" : "Open SureType";
        _show.Text = zh ? "显示当前状态" : "Show current state";
        _pause.Text = _service.IsPaused ? (zh ? "恢复提示" : "Resume cues") : (zh ? "暂停提示" : "Pause cues");
        _exit.Text = zh ? "退出" : "Exit";
        _icon.Text = "SureType · " + (_service.IsPaused ? (zh ? "已暂停" : "Paused") : (zh ? "正在运行" : "Running"));
    }
    public void OpenMainWindow()
    {
        if (_window == null)
        {
            _window = new MainWindow(_settings, _service, _overlay);
            _window.Closed += (_, _) => _window = null;
        }
        _window.ShowError(_error);
        _window.Show();
        if (_window.WindowState == System.Windows.WindowState.Minimized) _window.WindowState = System.Windows.WindowState.Normal;
        _window.Activate();
    }
    public void ReportError(string? message)
    {
        var changed = message != _error;
        _error = message;
        _window?.ShowError(message);
        if (message != null && _window == null && changed)
            _icon.ShowBalloonTip(8000, "SureType", message, Forms.ToolTipIcon.Warning);
    }
    private static Icon LoadIcon()
    {
        var resource = System.Windows.Application.GetResourceStream(new Uri("/SureType;component/Resources/AppIcon.ico", UriKind.Relative));
        if (resource == null) return (Icon)SystemIcons.Application.Clone();
        using var stream = resource.Stream;
        using var icon = new Icon(stream);
        return (Icon)icon.Clone();
    }
    public void Dispose()
    {
        _settings.Changed -= OnChanged; _service.PauseChanged -= OnChanged;
        _window?.Close(); _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose(); _icon.Icon?.Dispose(); _icon.Dispose();
    }
}

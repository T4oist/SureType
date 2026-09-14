using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SureType.Models;
using Forms = System.Windows.Forms;
using Color = System.Windows.Media.Color;
using Point = System.Drawing.Point;

namespace SureType.Windows;

public partial class OverlayWindow : Window
{
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _hideTimer;
    private int _generation;
    public OverlayWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        _hideTimer = new DispatcherTimer();
        _hideTimer.Tick += (_, _) => FadeOut();
    }
    public void ShowState(InputState state)
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(() => ShowState(state)); return; }
        var wasVisible = IsVisible;
        ++_generation;
        _hideTimer.Stop();
        BeginAnimation(OpacityProperty, null);
        var status = StatusPresentation.FromState(state);
        StatusText.Text = status.Symbol;
        CapsBadge.Visibility = status.CapsLock ? Visibility.Visible : Visibility.Collapsed;
        var color = status.Symbol switch
        {
            "中" => Color.FromRgb(49, 93, 73),
            "英" => Color.FromRgb(59, 89, 113),
            "EN" => Color.FromRgb(83, 93, 105),
            _ => Color.FromRgb(139, 110, 78)
        };
        StatusShell.Background = new SolidColorBrush(_settings.LogoStyle == LogoStyle.Filled ? color : Color.FromRgb(248, 250, 244));
        StatusShell.BorderBrush = new SolidColorBrush(_settings.LogoStyle == LogoStyle.Mono ? Colors.Black : color);
        StatusText.Foreground = new SolidColorBrush(_settings.LogoStyle switch
        {
            LogoStyle.Filled => Colors.White, LogoStyle.Mono => Colors.Black, _ => color
        });
        Width = Height = _settings.OverlaySize;
        var cursor = Forms.Cursor.Position;
        var active = GetForegroundWindow();
        var screen = _settings.OverlayPosition == OverlayPosition.NearMouse
            ? Forms.Screen.FromPoint(cursor) : Forms.Screen.FromHandle(active);
        if (!IsVisible) Show();
        PositionOnScreen(screen, cursor);
        BadgeScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        BadgeScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        BadgeScale.ScaleX = BadgeScale.ScaleY = 1;
        if (SystemParameters.ClientAreaAnimation)
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(100)));
            var enter = new DoubleAnimation(wasVisible ? 0.96 : 0.88, 1, TimeSpan.FromMilliseconds(150))
            {
                FillBehavior = FillBehavior.Stop,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            BadgeScale.BeginAnimation(ScaleTransform.ScaleXProperty, enter);
            BadgeScale.BeginAnimation(ScaleTransform.ScaleYProperty, enter);
        }
        else Opacity = 1;
        _hideTimer.Interval = TimeSpan.FromSeconds(_settings.OverlayDurationSeconds);
        _hideTimer.Start();
    }
    public void HideImmediately()
    {
        ++_generation; _hideTimer.Stop(); BeginAnimation(OpacityProperty, null); Opacity = 0; Hide();
    }
    private void FadeOut()
    {
        _hideTimer.Stop();
        if (!SystemParameters.ClientAreaAnimation) { HideImmediately(); return; }
        var generation = _generation;
        var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180));
        animation.Completed += (_, _) => { if (generation == _generation) Hide(); };
        BeginAnimation(OpacityProperty, animation);
    }
    protected override void OnClosed(EventArgs e) { ++_generation; _hideTimer.Stop(); base.OnClosed(e); }
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowLong(hwnd, -20, GetWindowLong(hwnd, -20) | 0x20 | 0x80 | 0x08000000);
        HwndSource.FromHwnd(hwnd)?.AddHook((nint h, int msg, nint w, nint l, ref bool handled) =>
        {
            if (msg == 0x0084) { handled = true; return new nint(-1); }
            if (msg == 0x0021) { handled = true; return new nint(3); }
            return 0;
        });
    }
    private void PositionOnScreen(Forms.Screen screen, Point cursor)
    {
        var center = new NativePoint(screen.Bounds.Left + screen.Bounds.Width / 2, screen.Bounds.Top + screen.Bounds.Height / 2);
        var monitor = MonitorFromPoint(center, 2);
        var scale = GetDpiForMonitor(monitor, 0, out var dpi, out _) == 0 ? dpi / 96.0 : 1.0;
        var size = (int)Math.Round(_settings.OverlaySize * scale);
        var point = OverlayPlacement.Calculate(screen.WorkingArea, cursor, size, (int)Math.Round(20 * scale), _settings.OverlayPosition);
        SetWindowPos(new WindowInteropHelper(this).Handle, new nint(-1), point.X, point.Y, size, size, 0x0010);
    }
    [StructLayout(LayoutKind.Sequential)] private readonly record struct NativePoint(int X, int Y);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern int GetWindowLong(nint hwnd, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(nint hwnd, int index, int value);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern nint MonitorFromPoint(NativePoint point, uint flags);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(nint monitor, int type, out uint x, out uint y);
}

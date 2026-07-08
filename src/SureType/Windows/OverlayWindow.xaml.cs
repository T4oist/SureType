using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SureType.Models;
using Forms = System.Windows.Forms;
using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;
using InputImeMode = SureType.Models.ImeMode;

namespace SureType.Windows;

public partial class OverlayWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolwindow = 0x00000080;
    private const int WsExNoactivate = 0x08000000;
    private const int OverlayMargin = 24;

    private readonly AppSettings _settings;
    private readonly DispatcherTimer _hideTimer;

    public OverlayWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_settings.OverlayDurationSeconds) };
        _hideTimer.Tick += (_, _) => FadeOut();
    }

    public void ShowState(InputState state)
    {
        Dispatcher.Invoke(() =>
        {
            ApplyLogo(state);
            PositionNearActiveScreen();

            if (!IsVisible)
            {
                Show();
            }

            Visibility = Visibility.Visible;
            _hideTimer.Stop();
            _hideTimer.Interval = TimeSpan.FromSeconds(_settings.OverlayDurationSeconds);
            BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(110)));
            _hideTimer.Start();
        });
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var style = GetWindowLong(hwnd, GwlExstyle);
        SetWindowLong(hwnd, GwlExstyle, style | WsExTransparent | WsExToolwindow | WsExNoactivate);
    }

    private void ApplyLogo(InputState state)
    {
        var logo = LogoPresentation.FromState(state);
        StatusText.Text = logo.Text;

        switch (_settings.LogoStyle)
        {
            case LogoStyle.Soft:
                StatusShell.Background = new SolidColorBrush(logo.SoftBackground);
                StatusShell.BorderBrush = new SolidColorBrush(logo.SoftBorder);
                StatusShell.BorderThickness = new Thickness(1);
                StatusShell.CornerRadius = new CornerRadius(16);
                StatusText.Foreground = new SolidColorBrush(logo.FilledBackground);
                break;
            case LogoStyle.Mono:
                StatusShell.Background = new SolidColorBrush(MediaColor.FromRgb(250, 250, 250));
                StatusShell.BorderBrush = new SolidColorBrush(MediaColor.FromRgb(46, 50, 56));
                StatusShell.BorderThickness = new Thickness(1.4);
                StatusShell.CornerRadius = new CornerRadius(12);
                StatusText.Foreground = new SolidColorBrush(MediaColor.FromRgb(34, 38, 43));
                break;
            default:
                StatusShell.Background = new SolidColorBrush(logo.FilledBackground);
                StatusShell.BorderBrush = new SolidColorBrush(MediaColor.FromArgb(34, 0, 0, 0));
                StatusShell.BorderThickness = new Thickness(1);
                StatusShell.CornerRadius = new CornerRadius(14);
                StatusText.Foreground = MediaBrushes.White;
                break;
        }
    }

    private void FadeOut()
    {
        _hideTimer.Stop();
        var animation = new DoubleAnimation(0, TimeSpan.FromMilliseconds(220));
        animation.Completed += (_, _) => Visibility = Visibility.Hidden;
        BeginAnimation(OpacityProperty, animation);
    }

    private void PositionNearActiveScreen()
    {
        var activeWindow = GetForegroundWindow();
        var screen = activeWindow == nint.Zero
            ? Forms.Screen.PrimaryScreen ?? Forms.Screen.AllScreens[0]
            : Forms.Screen.FromHandle(activeWindow);

        var bounds = screen.WorkingArea;
        var source = PresentationSource.FromVisual(this);
        var dpiScaleX = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;
        var dpiScaleY = source?.CompositionTarget?.TransformFromDevice.M22 ?? 1.0;

        var left = _settings.OverlayPosition is OverlayPosition.TopLeft or OverlayPosition.BottomLeft
            ? bounds.Left * dpiScaleX + OverlayMargin
            : bounds.Right * dpiScaleX - Width - OverlayMargin;

        var top = _settings.OverlayPosition is OverlayPosition.TopLeft or OverlayPosition.TopRight
            ? bounds.Top * dpiScaleY + OverlayMargin
            : bounds.Bottom * dpiScaleY - Height - OverlayMargin;

        Left = left;
        Top = top;
    }

    private sealed record LogoPresentation(string Text, MediaColor FilledBackground, MediaColor SoftBackground, MediaColor SoftBorder)
    {
        public static LogoPresentation FromState(InputState state)
        {
            return (state.InputSource, state.ImeMode, state.CapsMode) switch
            {
                (InputSource.ChineseIme, InputImeMode.Chinese, _) => new("CN", MediaColor.FromRgb(46, 125, 103), MediaColor.FromRgb(232, 246, 241), MediaColor.FromRgb(160, 213, 196)),
                (InputSource.ChineseIme, InputImeMode.English, _) => new("EN", MediaColor.FromRgb(73, 108, 138), MediaColor.FromRgb(233, 241, 248), MediaColor.FromRgb(166, 193, 216)),
                (InputSource.EnglishKeyboard, _, CapsMode.Upper) => new("A", MediaColor.FromRgb(62, 69, 79), MediaColor.FromRgb(239, 241, 244), MediaColor.FromRgb(177, 184, 193)),
                (InputSource.EnglishKeyboard, _, _) => new("en", MediaColor.FromRgb(102, 109, 117), MediaColor.FromRgb(242, 244, 246), MediaColor.FromRgb(189, 195, 202)),
                _ => new("?", MediaColor.FromRgb(139, 129, 117), MediaColor.FromRgb(246, 242, 237), MediaColor.FromRgb(207, 193, 177))
            };
        }
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);
}
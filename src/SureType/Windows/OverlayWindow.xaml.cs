using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SureType.Models;
using Forms = System.Windows.Forms;

namespace SureType.Windows;

public partial class OverlayWindow : Window
{
    private const int GwlExstyle = -20;
    private const int WsExTransparent = 0x00000020;
    private const int WsExToolwindow = 0x00000080;
    private const int WsExNoactivate = 0x08000000;
    private const int OverlayMargin = 24;
    private static readonly TimeSpan VisibleDuration = TimeSpan.FromMilliseconds(1500);

    private readonly DispatcherTimer _hideTimer;

    public OverlayWindow()
    {
        InitializeComponent();
        _hideTimer = new DispatcherTimer { Interval = VisibleDuration };
        _hideTimer.Tick += (_, _) => FadeOut();
    }

    public void ShowState(InputState state)
    {
        Dispatcher.Invoke(() =>
        {
            if (System.Windows.Application.Current.TryFindResource(state.DisplayAsset) is not ImageSource imageSource)
            {
                imageSource = (ImageSource)System.Windows.Application.Current.FindResource(InputStateAssets.Unknown);
            }

            StatusImage.Source = imageSource;
            PositionNearActiveScreen();

            if (!IsVisible)
            {
                Show();
            }

            Visibility = Visibility.Visible;
            _hideTimer.Stop();
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

        Left = (bounds.Right * dpiScaleX) - Width - OverlayMargin;
        Top = (bounds.Top * dpiScaleY) + OverlayMargin;
    }

    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(nint hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);
}

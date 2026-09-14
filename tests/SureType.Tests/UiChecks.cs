using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using SureType.Models;
using SureType.Services;
using SureType.Windows;

internal static class UiChecks
{
    public static int Run(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach (var language in Enum.GetValues<AppLanguage>())
        {
            var settings = new AppSettings { Language = language, OverlayPosition = OverlayPosition.NearMouse };
            using var service = new InputStateService(new Win32InputStateReader(), settings);
            var overlay = new OverlayWindow(settings);
            var window = new MainWindow(settings, service, overlay);
            var tabs = (TabControl)window.FindName("Tabs");
            foreach (var width in new[] { 910, 650 })
            {
                for (var tab = 0; tab < 3; tab++)
                {
                    tabs.SelectedIndex = tab;
                    Render((FrameworkElement)window.Content, width, 680, Path.Combine(directory, $"{language}-{width}-{tab}.png"));
                }
            }
            window.Close(); overlay.Close();
        }
        Console.WriteLine("PASS constructed and rendered all three tabs in Chinese and English at two widths");
        return 0;
    }
    public static int Interact()
    {
        var application = new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var settings = new AppSettings();
        using var service = new InputStateService(new Win32InputStateReader(), settings);
        var overlay = new OverlayWindow(settings);
        var window = new MainWindow(settings, service, overlay);
        window.Show();
        Pump(220);
        var tabs = (TabControl)window.FindName("Tabs");
        tabs.SelectedIndex = 1;
        Pump(240);
        var position = (ComboBox)window.FindName("PositionCombo");
        position.IsDropDownOpen = true;
        Pump(80);
        var popup = (Popup)position.Template.FindName("PART_Popup", position);
        if (!popup.IsOpen || popup.Child.RenderSize.Width < position.ActualWidth)
            throw new Exception("Position dropdown did not open at the control width");
        position.SelectedIndex = (int)OverlayPosition.NearMouse;
        position.IsDropDownOpen = false;
        if (settings.OverlayPosition != OverlayPosition.NearMouse) throw new Exception("Position selection did not reach settings");
        ((ComboBox)window.FindName("StyleCombo")).SelectedIndex = (int)LogoStyle.Soft;
        ((Slider)window.FindName("SizeSlider")).Value = 80;
        if (settings.LogoStyle != LogoStyle.Soft || settings.OverlaySize != 80)
            throw new Exception("Appearance controls did not reach settings");
        Pump(240);
        var badge = (Border)window.FindName("PreviewBadge");
        if (badge.Width != 80) throw new Exception("Preview size was not updated");
        if (Math.Abs(((FrameworkElement)tabs.SelectedContent).Opacity - 1) > 0.001)
            throw new Exception("Page animation did not settle");
        tabs.SelectedIndex = 2;
        Pump(240);
        var focus = (CheckBox)window.FindName("FocusCheck");
        focus.IsChecked = false;
        focus.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
        if (settings.ShowOnInputFocus) throw new Exception("Toggle did not reach settings");
        ((ComboBox)window.FindName("LanguageCombo")).SelectedIndex = (int)AppLanguage.English;
        if (settings.Language != AppLanguage.English || settings.OverlayPosition != OverlayPosition.NearMouse || settings.ShowOnInputFocus)
            throw new Exception("Language switch reset preferences");
        if (((TextBlock)window.FindName("BehaviorTitle")).Text != "Settings")
            throw new Exception("Language change did not update the page");
        window.Close(); overlay.Close(); application.Shutdown();
        Console.WriteLine("PASS styled dropdown, sliders, toggles, preview, page transition, and language switch");
        return 0;
    }

    private static void Pump(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame);
    }
    private static void Render(FrameworkElement element, double width, double height, string path)
    {
        element.Measure(new System.Windows.Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        var background = new DrawingVisual();
        using (var drawing = background.RenderOpen())
            drawing.DrawRectangle(new SolidColorBrush(Color.FromRgb(247, 247, 242)), null, new Rect(0, 0, width, height));
        bitmap.Render(background);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path); encoder.Save(stream);
    }
}

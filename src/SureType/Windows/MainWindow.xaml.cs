using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using SureType.Models;
using SureType.Services;
using Button = System.Windows.Controls.Button;
using ColorConverter = System.Windows.Media.ColorConverter;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

namespace SureType.Windows;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly InputStateService _service;
    private readonly OverlayWindow _overlay;
    private bool _loading = true;
    private InputState? _lastDemoState;

    public MainWindow(AppSettings settings, InputStateService service, OverlayWindow overlay)
    {
        _settings = settings;
        _service = service;
        _overlay = overlay;
        InitializeComponent();
        ApplyLanguage();
        LoadSettings();
        Tabs.SelectedIndex = settings.HasCompletedOnboarding ? 1 : 0;
        Loaded += (_, _) => AnimatePage();
        _service.StateUpdated += OnStateUpdated;
        _service.PauseChanged += OnPauseChanged;
        Closed += (_, _) =>
        {
            _service.StateUpdated -= OnStateUpdated;
            _service.PauseChanged -= OnPauseChanged;
        };
    }

    private string T(string zh, string en) => _settings.Language == AppLanguage.Chinese ? zh : en;

    public void ShowError(string? message)
    {
        ErrorText.Text = message ?? "";
        ErrorText.Visibility = message == null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyLanguage()
    {
        _loading = true;
        Title = "SureType";
        SubtitleText.Text = T("输入状态提示", "Input indicator");
        FooterText.Text = T("关闭后继续在托盘运行", "Closing keeps SureType in the tray.");
        GuideNav.Content = GuideTab.Header = T("试一下", "Try it");
        AppearanceNav.Content = AppearanceTab.Header = T("外观", "Appearance");
        BehaviorNav.Content = BehaviorTab.Header = T("设置", "Settings");
        GuideTitle.Text = T("试一下", "Try it");
        GuideText.Text = T("切换中英文或 CapsLock，看看提示。", "Switch input modes or Caps Lock to try the indicator.");
        AppearanceTitle.Text = T("提示外观", "Appearance");
        BehaviorTitle.Text = T("提醒设置", "Settings");
        TryLabel.Text = T("试输入", "Type here");
        InputPlaceholder.Text = T("在这里输入…", "Type something…");
        PrivacyHint.Text = T("文字不会保存", "Text is not saved.");
        LegendLabel.Text = T("图标含义", "The indicators");
        LegendItems.ItemsSource = new[]
        {
            new LegendItem("中", T("中文", "Chinese"), Paint("#DDE8D3"), Paint("#3D6644")),
            new LegendItem("英", T("输入法英文", "IME English"), Paint("#E0E8EC"), Paint("#426177")),
            new LegendItem("EN", T("英文键盘", "English keyboard"), Paint("#E4E7E0"), Paint("#566151")),
            new LegendItem("⇪", "CapsLock", Paint("#E8E3D9"), Paint("#77674D")),
            new LegendItem("?", T("未知", "Unknown"), Paint("#ECE5DD"), Paint("#88725C"))
        };
        LegendItems.ToolTip = T("⇪ 表示 CapsLock 已开启。实际大小写还受 Shift 和输入法影响。", "⇪ means Caps Lock is on. Shift and the IME can still affect letter case.");
        BeginButton.Content = T("完成", "Done");
        PreviewGuideButton.Content = PreviewButton.Content = T("预览", "Preview");
        PositionLabel.Text = T("位置", "Position");
        PositionCombo.ItemsSource = _settings.Language == AppLanguage.Chinese
            ? new[] { "右上角", "左上角", "右下角", "左下角", "鼠标旁边" }
            : new[] { "Top right", "Top left", "Bottom right", "Bottom left", "Near mouse" };
        StyleLabel.Text = T("样式", "Style");
        StyleCombo.ItemsSource = _settings.Language == AppLanguage.Chinese
            ? new[] { "实心", "柔和", "黑白" } : new[] { "Filled", "Soft", "Mono" };
        PreviewCaptionText.Text = T("效果预览", "Sample");
        LanguageLabel.Text = T("语言", "Language");
        StartupCheck.Content = T("开机启动", "Start with Windows");
        ChangesCheck.Content = T("切换状态时提示", "When input mode changes");
        FocusCheck.Content = T("进入输入框时提示", "When an input gains focus");
        FullscreenCheck.Content = T("全屏时不提示", "Hide in fullscreen");
        ExclusionsLabel.Text = T("不提示的应用", "Excluded apps");
        EmptyExclusionsText.Text = T("暂无", "None");
        AddButton.Content = T("添加…", "Add…");
        RemoveButton.Content = T("移除", "Remove");
        ResetButton.Content = T("恢复默认", "Reset defaults");
        System.Windows.Automation.AutomationProperties.SetName(TryInput, TryLabel.Text);
        System.Windows.Automation.AutomationProperties.SetName(PositionCombo, PositionLabel.Text);
        System.Windows.Automation.AutomationProperties.SetName(StyleCombo, StyleLabel.Text);
        System.Windows.Automation.AutomationProperties.SetName(LanguageCombo, LanguageLabel.Text);
        System.Windows.Automation.AutomationProperties.SetName(ExclusionsList, ExclusionsLabel.Text);
        _loading = false;
        UpdateLabels();
    }

    private void LoadSettings()
    {
        _loading = true;
        PositionCombo.SelectedIndex = (int)_settings.OverlayPosition;
        StyleCombo.SelectedIndex = (int)_settings.LogoStyle;
        LanguageCombo.SelectedIndex = (int)_settings.Language;
        SizeSlider.Value = _settings.OverlaySize;
        DurationSlider.Value = _settings.OverlayDurationSeconds;
        CooldownSlider.Value = _settings.InputFocusCooldownSeconds;
        StartupCheck.IsChecked = _settings.StartWithWindows;
        ChangesCheck.IsChecked = _settings.ShowOnStateChange;
        FocusCheck.IsChecked = _settings.ShowOnInputFocus;
        FullscreenCheck.IsChecked = _settings.SuppressFullscreen;
        ExclusionsList.ItemsSource = _settings.ExcludedApplications;
        _loading = false;
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        SizeLabel.Text = T($"大小 · {_settings.OverlaySize:0}", $"Size · {_settings.OverlaySize:0}");
        DurationLabel.Text = T($"停留 · {_settings.OverlayDurationSeconds:0.0} 秒", $"Duration · {_settings.OverlayDurationSeconds:0.0} s");
        CooldownLabel.Text = T($"间隔 · {_settings.InputFocusCooldownSeconds:0} 秒", $"Cooldown · {_settings.InputFocusCooldownSeconds:0} s");
        RuntimeText.Text = _service.IsPaused ? T("已暂停", "Paused") : T("运行中", "Running");
        RuntimeDot.Fill = Paint(_service.IsPaused ? "#AE9268" : "#557C53");
        PauseButton.Content = _service.IsPaused ? T("继续提示", "Resume") : T("暂停提示", "Pause");
        System.Windows.Automation.AutomationProperties.SetName(SizeSlider, SizeLabel.Text);
        System.Windows.Automation.AutomationProperties.SetName(DurationSlider, DurationLabel.Text);
        System.Windows.Automation.AutomationProperties.SetName(CooldownSlider, CooldownLabel.Text);
        UpdatePreview();
    }

    private void Preferences_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading) return;
        _settings.OverlayPosition = (OverlayPosition)Math.Max(0, PositionCombo.SelectedIndex);
        _settings.LogoStyle = (LogoStyle)Math.Max(0, StyleCombo.SelectedIndex);
        _settings.ShowOnStateChange = ChangesCheck.IsChecked == true;
        _settings.ShowOnInputFocus = FocusCheck.IsChecked == true;
        _settings.SuppressFullscreen = FullscreenCheck.IsChecked == true;
        UpdatePreview();
    }

    private void Slider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_loading) return;
        _settings.OverlaySize = SizeSlider.Value;
        _settings.OverlayDurationSeconds = DurationSlider.Value;
        _settings.InputFocusCooldownSeconds = CooldownSlider.Value;
        UpdateLabels();
    }

    private void Language_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_loading) return;
        _settings.Language = (AppLanguage)Math.Max(0, LanguageCombo.SelectedIndex);
        ApplyLanguage();
        LoadSettings();
    }

    private void Startup_Click(object sender, RoutedEventArgs e)
    {
        var succeeded = StartupService.SetEnabled(StartupCheck.IsChecked == true);
        _settings.StartWithWindows = StartupService.IsEnabled();
        StartupCheck.IsChecked = _settings.StartWithWindows;
        if (!succeeded) ShowError(T("自启动设置失败，请重试。", "Could not change startup. Please try again."));
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        _service.ShowCurrentState();
        if (!SystemParameters.ClientAreaAnimation) return;
        var pulse = new DoubleAnimation(1, 1.06, TimeSpan.FromMilliseconds(110))
        {
            AutoReverse = true, FillBehavior = FillBehavior.Stop,
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        PreviewScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
        PreviewScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
    }

    private void Begin_Click(object sender, RoutedEventArgs e)
    {
        _settings.HasCompletedOnboarding = true;
        Close();
    }

    private void Pause_Click(object sender, RoutedEventArgs e) => _service.TogglePaused();

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _settings.ResetPreferences();
        LoadSettings();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Microsoft.Win32.OpenFileDialog { Filter = T("应用 (*.exe)|*.exe", "Applications (*.exe)|*.exe"), CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
        _settings.ExcludedApplications = [.. _settings.ExcludedApplications, picker.FileName];
        ExclusionsList.ItemsSource = _settings.ExcludedApplications;
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (ExclusionsList.SelectedItem is not string path) return;
        _settings.ExcludedApplications = _settings.ExcludedApplications
            .Where(p => !string.Equals(p, path, StringComparison.OrdinalIgnoreCase)).ToArray();
        ExclusionsList.ItemsSource = _settings.ExcludedApplications;
    }

    private void TryInput_Focused(object sender, RoutedEventArgs e) => _service.ShowCurrentState();
    private void OnPauseChanged(object? sender, EventArgs e) => UpdateLabels();

    private void OnStateUpdated(object? sender, InputStateChangedEventArgs e)
    {
        var display = StatusPresentation.FromState(e.State);
        LiveStateText.Text = display.Symbol + (display.CapsLock ? "  ⇪" : "");
        if (TryInput.IsKeyboardFocusWithin && !_service.IsPaused && e.State != _lastDemoState)
            _overlay.ShowState(e.State);
        _lastDemoState = e.State;
    }

    private void Preview_SizeChanged(object sender, SizeChangedEventArgs e) => UpdatePreview(animate: false);

    private void UpdatePreview(bool animate = true)
    {
        if (PreviewCanvas == null) return;
        PositionHint.Text = _settings.OverlayPosition == OverlayPosition.NearMouse
            ? T("提示出现时定位，靠近边缘会自动避让。", "Positions on appearance; adjusts at screen edges.")
            : T("显示在当前窗口所在屏幕。", "Uses the active window’s screen.");
        PreviewModeText.Text = PositionCombo.SelectedItem as string ?? "";
        PreviewBadge.Background = Paint(_settings.LogoStyle == LogoStyle.Filled ? "#315D49" : "#F8FAF4");
        PreviewBadge.BorderBrush = Paint(_settings.LogoStyle == LogoStyle.Mono ? "#3C433B" : "#315D49");
        PreviewSymbol.Foreground = Paint(_settings.LogoStyle == LogoStyle.Filled ? "#FFFFFF" :
            _settings.LogoStyle == LogoStyle.Mono ? "#303B30" : "#315D49");
        PreviewBadge.Width = PreviewBadge.Height = _settings.OverlaySize;
        var width = PreviewCanvas.ActualWidth;
        if (width < 80) return;
        PreviewDocument.Width = Math.Min(260, width - 50);
        Canvas.SetLeft(PreviewDocument, (width - PreviewDocument.Width) / 2);
        var cursor = new System.Drawing.Point((int)(width * 0.55), 85);
        Canvas.SetLeft(PreviewCursor, cursor.X);
        Canvas.SetTop(PreviewCursor, cursor.Y);
        var point = OverlayPlacement.Calculate(new(0, 0, (int)width, 180), cursor,
            (int)_settings.OverlaySize, 16, _settings.OverlayPosition);
        var motion = animate && IsLoaded && PreviewCanvas.IsVisible && SystemParameters.ClientAreaAnimation;
        Move(PreviewMotion, TranslateTransform.XProperty, point.X, motion);
        Move(PreviewMotion, TranslateTransform.YProperty, point.Y, motion);
    }

    private void Tabs_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.Source, Tabs)) AnimatePage();
    }

    private void AnimatePage()
    {
        if (!IsLoaded || Tabs.SelectedContent is not FrameworkElement panel) return;
        panel.BeginAnimation(OpacityProperty, null);
        panel.Opacity = 1;
        var offset = new TranslateTransform();
        panel.RenderTransform = offset;
        if (!SystemParameters.ClientAreaAnimation) return;
        panel.BeginAnimation(OpacityProperty, new DoubleAnimation(0.45, 1, TimeSpan.FromMilliseconds(180)) { FillBehavior = FillBehavior.Stop });
        offset.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(180))
        {
            FillBehavior = FillBehavior.Stop, EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });
    }

    private static void Move(Animatable target, DependencyProperty property, double to, bool animate)
    {
        var from = (double)target.GetValue(property);
        target.BeginAnimation(property, null);
        target.SetValue(property, to);
        if (!animate || Math.Abs(from - to) < 0.1) return;
        target.BeginAnimation(property, new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(180))
        {
            FillBehavior = FillBehavior.Stop, EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        });
    }

    private void Button_Enter(object sender, MouseEventArgs e) => ScaleButton(sender, 1.025);
    private void Button_Leave(object sender, MouseEventArgs e) => ScaleButton(sender, 1);
    private void Button_Down(object sender, MouseButtonEventArgs e) => ScaleButton(sender, 0.97);
    private void Button_Up(object sender, MouseButtonEventArgs e) => ScaleButton(sender, sender is Button { IsMouseOver: true } ? 1.025 : 1);

    private void ScaleButton(object sender, double scale)
    {
        if (sender is not Button button || !button.IsLoaded) return;
        if (!SystemParameters.ClientAreaAnimation) scale = 1;
        if (button.RenderTransform is not ScaleTransform transform)
            button.RenderTransform = transform = new ScaleTransform(1, 1);
        Move(transform, ScaleTransform.ScaleXProperty, scale, SystemParameters.ClientAreaAnimation);
        Move(transform, ScaleTransform.ScaleYProperty, scale, SystemParameters.ClientAreaAnimation);
    }

    private static SolidColorBrush Paint(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private sealed record LegendItem(string Symbol, string Label, Brush Background, Brush Foreground);
}

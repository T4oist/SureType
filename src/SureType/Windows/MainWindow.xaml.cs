using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MediaBrush = System.Windows.Media.Brush;
using MediaBrushes = System.Windows.Media.Brushes;
using SureType.Models;
using SureType.Services;

namespace SureType.Windows;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly InputStateService _inputStateService;
    private readonly OverlayWindow _overlayWindow;
    private bool _isLoading = true;

    public MainWindow(AppSettings settings, InputStateService inputStateService, OverlayWindow overlayWindow)
    {
        _settings = settings;
        _inputStateService = inputStateService;
        _overlayWindow = overlayWindow;
        _settings.StartWithWindows = StartupService.IsEnabled();
        _isLoading = true;
        InitializeComponent();
        ApplyLanguage();
        LoadSettings();
        SectionList.SelectedIndex = 0;
        ShowSection(0);
    }

    private TextSet Texts => _settings.Language == AppLanguage.Chinese ? TextSet.Chinese : TextSet.English;

    private void ApplyLanguage()
    {
        var t = Texts;
        Title = t.WindowTitle;
        SubtitleText.Text = t.Subtitle;

        GeneralNavItem.Content = t.GeneralNav;
        GuideNavItem.Content = t.GuideNav;
        PositionNavItem.Content = t.PositionNav;
        TimingNavItem.Content = t.TimingNav;
        StyleNavItem.Content = t.StyleNav;

        GeneralTitleText.Text = t.GeneralTitle;
        GeneralDescriptionText.Text = t.GeneralDescription;
        LanguageLabelText.Text = t.LanguageLabel;
        ChineseLanguageItem.Content = t.ChineseLanguage;
        EnglishLanguageItem.Content = t.EnglishLanguage;
        StartWithWindowsCheckBox.Content = t.StartWithWindows;
        StartWithWindowsHintText.Text = t.StartWithWindowsHint;

        GuideTitleText.Text = t.GuideTitle;
        GuideDescriptionText.Text = t.GuideDescription;
        LegendItems.ItemsSource = CreateLegendItems(t);

        PositionTitleText.Text = t.PositionTitle;
        PositionDescriptionText.Text = t.PositionDescription;
        TopLeftRadio.Content = t.TopLeft;
        TopRightRadio.Content = t.TopRight;
        BottomLeftRadio.Content = t.BottomLeft;
        BottomRightRadio.Content = t.BottomRight;
        PreviewPositionButton.Content = t.PreviewPosition;

        TimingTitleText.Text = t.TimingTitle;
        TimingDescriptionText.Text = t.TimingDescription;
        DisplayTimeLabelText.Text = t.DisplayTime;
        CooldownLabelText.Text = t.FocusCooldown;
        ShowOnStateChangeCheckBox.Content = t.ShowOnStateChange;

        StyleTitleText.Text = t.StyleTitle;
        StyleDescriptionText.Text = t.StyleDescription;
        FilledStyleRadio.Content = t.FilledStyle;
        SoftStyleRadio.Content = t.SoftStyle;
        MonoStyleRadio.Content = t.MonoStyle;
        PreviewStyleButton.Content = t.PreviewStyle;

        UpdateValueLabels();
    }

    private static LegendItem[] CreateLegendItems(TextSet t)
    {
        return new[]
        {
            new LegendItem("CN", MediaBrushes.SeaGreen, t.ChineseIconTitle, t.ChineseIconDescription),
            new LegendItem("EN", MediaBrushes.SteelBlue, t.ImeEnglishIconTitle, t.ImeEnglishIconDescription),
            new LegendItem("en", MediaBrushes.DimGray, t.EnglishLowerIconTitle, t.EnglishLowerIconDescription),
            new LegendItem("A", MediaBrushes.DarkSlateGray, t.EnglishUpperIconTitle, t.EnglishUpperIconDescription),
            new LegendItem("?", MediaBrushes.Sienna, t.UnknownIconTitle, t.UnknownIconDescription)
        };
    }

    private void LoadSettings()
    {
        _isLoading = true;
        OverlayDurationSlider.Value = _settings.OverlayDurationSeconds;
        FocusCooldownSlider.Value = _settings.InputFocusCooldownSeconds;
        ShowOnStateChangeCheckBox.IsChecked = _settings.ShowOnStateChange;
        StartWithWindowsCheckBox.IsChecked = _settings.StartWithWindows;
        LanguageComboBox.SelectedIndex = _settings.Language == AppLanguage.Chinese ? 0 : 1;

        TopLeftRadio.IsChecked = _settings.OverlayPosition == OverlayPosition.TopLeft;
        TopRightRadio.IsChecked = _settings.OverlayPosition == OverlayPosition.TopRight;
        BottomLeftRadio.IsChecked = _settings.OverlayPosition == OverlayPosition.BottomLeft;
        BottomRightRadio.IsChecked = _settings.OverlayPosition == OverlayPosition.BottomRight;

        FilledStyleRadio.IsChecked = _settings.LogoStyle == LogoStyle.Filled;
        SoftStyleRadio.IsChecked = _settings.LogoStyle == LogoStyle.Soft;
        MonoStyleRadio.IsChecked = _settings.LogoStyle == LogoStyle.Mono;
        _isLoading = false;
        UpdateValueLabels();
    }

    private void SectionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ShowSection(SectionList.SelectedIndex);
    }

    private void ShowSection(int index)
    {
        if (GeneralPanel is null || GuidePanel is null || PositionPanel is null || TimingPanel is null || StylePanel is null)
        {
            return;
        }

        GeneralPanel.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        GuidePanel.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        PositionPanel.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        TimingPanel.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
        StylePanel.Visibility = index == 4 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _settings.Language = LanguageComboBox.SelectedIndex == 0 ? AppLanguage.Chinese : AppLanguage.English;
        ApplyLanguage();
    }

    private void StartWithWindowsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        var enabled = StartWithWindowsCheckBox.IsChecked == true;
        StartupService.SetEnabled(enabled);
        _settings.StartWithWindows = StartupService.IsEnabled();
        StartWithWindowsCheckBox.IsChecked = _settings.StartWithWindows;
    }

    private void PositionRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _settings.OverlayPosition = sender switch
        {
            var value when ReferenceEquals(value, TopLeftRadio) => OverlayPosition.TopLeft,
            var value when ReferenceEquals(value, BottomLeftRadio) => OverlayPosition.BottomLeft,
            var value when ReferenceEquals(value, BottomRightRadio) => OverlayPosition.BottomRight,
            _ => OverlayPosition.TopRight
        };
    }

    private void StyleRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
        {
            return;
        }

        _settings.LogoStyle = sender switch
        {
            var value when ReferenceEquals(value, SoftStyleRadio) => LogoStyle.Soft,
            var value when ReferenceEquals(value, MonoStyleRadio) => LogoStyle.Mono,
            _ => LogoStyle.Filled
        };
    }

    private void OverlayDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isLoading)
        {
            _settings.OverlayDurationSeconds = OverlayDurationSlider.Value;
            UpdateValueLabels();
        }
    }

    private void FocusCooldownSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isLoading)
        {
            _settings.InputFocusCooldownSeconds = FocusCooldownSlider.Value;
            UpdateValueLabels();
        }
    }

    private void ShowOnStateChangeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoading)
        {
            _settings.ShowOnStateChange = ShowOnStateChangeCheckBox.IsChecked == true;
        }
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _inputStateService.ShowCurrentState();
        _overlayWindow.ShowState(_inputStateService.CurrentState);
    }

    private void UpdateValueLabels()
    {
        if (OverlayDurationValue is null || FocusCooldownValue is null)
        {
            return;
        }

        var unit = Texts.SecondUnit;
        OverlayDurationValue.Text = $"{_settings.OverlayDurationSeconds:0.0} {unit}";
        FocusCooldownValue.Text = $"{_settings.InputFocusCooldownSeconds:0} {unit}";
    }

    private sealed record LegendItem(string Symbol, MediaBrush Background, string Title, string Description);

    private sealed record TextSet(
        string WindowTitle,
        string Subtitle,
        string GeneralNav,
        string GuideNav,
        string PositionNav,
        string TimingNav,
        string StyleNav,
        string GeneralTitle,
        string GeneralDescription,
        string LanguageLabel,
        string ChineseLanguage,
        string EnglishLanguage,
        string StartWithWindows,
        string StartWithWindowsHint,
        string GuideTitle,
        string GuideDescription,
        string ChineseIconTitle,
        string ChineseIconDescription,
        string ImeEnglishIconTitle,
        string ImeEnglishIconDescription,
        string EnglishLowerIconTitle,
        string EnglishLowerIconDescription,
        string EnglishUpperIconTitle,
        string EnglishUpperIconDescription,
        string UnknownIconTitle,
        string UnknownIconDescription,
        string PositionTitle,
        string PositionDescription,
        string TopLeft,
        string TopRight,
        string BottomLeft,
        string BottomRight,
        string PreviewPosition,
        string TimingTitle,
        string TimingDescription,
        string DisplayTime,
        string FocusCooldown,
        string ShowOnStateChange,
        string StyleTitle,
        string StyleDescription,
        string FilledStyle,
        string SoftStyle,
        string MonoStyle,
        string PreviewStyle,
        string SecondUnit)
    {
        public static TextSet English { get; } = new(
            "SureType Settings",
            "Tune where the input logo appears, how long it stays, and how it looks.",
            "General",
            "Guide",
            "Position",
            "Timing",
            "Logo style",
            "General",
            "Choose the interface language and startup behavior.",
            "Language",
            "Chinese",
            "English",
            "Start SureType when Windows starts",
            "This writes a Current User startup entry and does not require administrator permission.",
            "Icon guide",
            "Each logo is intentionally short: one symbol, one meaning.",
            "Chinese",
            "Chinese IME is in Chinese input mode.",
            "IME English",
            "A Chinese IME is active, but it is currently in English mode.",
            "English lower",
            "English keyboard or English input state with CapsLock off.",
            "English upper",
            "English input state with CapsLock on.",
            "Unknown",
            "SureType cannot read the current input state from this app or IME.",
            "Logo position",
            "Choose which corner SureType uses for the floating logo.",
            "Top left",
            "Top right",
            "Bottom left",
            "Bottom right",
            "Preview position",
            "Timing",
            "Control how long the logo stays visible and how often focusing inputs can show it.",
            "Display time",
            "Input-focus cooldown",
            "Show logo when input state changes",
            "Logo style",
            "Pick the visual weight that feels easiest to notice without being distracting.",
            "Filled: clear colored tile",
            "Soft: light background with colored text",
            "Mono: black and white minimal style",
            "Preview style",
            "s");

        public static TextSet Chinese { get; } = new(
            "SureType 设置",
            "调整输入状态 logo 的位置、显示时间和外观样式。",
            "通用",
            "图标说明",
            "显示位置",
            "显示时间",
            "Logo 样式",
            "通用",
            "选择界面语言和启动行为。",
            "界面语言",
            "中文",
            "English",
            "开机时自动启动 SureType",
            "会写入当前用户的开机启动项，不需要管理员权限。",
            "图标说明",
            "每个 logo 都尽量简短：一个符号，一个含义。",
            "中文输入",
            "中文输入法处于中文输入模式。",
            "中文输入法内英文",
            "仍在中文输入法中，但当前是英文模式。",
            "英文小写",
            "英文键盘或英文输入状态，CapsLock 未开启。",
            "英文大写",
            "英文输入状态，CapsLock 已开启。",
            "未知状态",
            "SureType 无法从当前应用或输入法读取状态。",
            "Logo 显示位置",
            "选择浮动 logo 显示在哪个屏幕角落。",
            "左上角",
            "右上角",
            "左下角",
            "右下角",
            "预览位置",
            "显示时间",
            "控制 logo 停留多久，以及输入框获得焦点时多久提示一次。",
            "显示时长",
            "输入框提示冷却时间",
            "输入法或大小写状态变化时显示 logo",
            "Logo 样式",
            "选择容易看清但不打扰的视觉样式。",
            "实心：清晰彩色方块",
            "柔和：浅色背景和彩色文字",
            "黑白：极简黑白样式",
            "预览样式",
            "秒");
    }
}
using System.Windows;
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
    private bool _isLoading;

    public MainWindow(AppSettings settings, InputStateService inputStateService, OverlayWindow overlayWindow)
    {
        _settings = settings;
        _inputStateService = inputStateService;
        _overlayWindow = overlayWindow;
        InitializeComponent();
        LoadLegend();
        LoadSettings();
        SectionList.SelectedIndex = 0;
        ShowSection(0);
    }

    private void LoadLegend()
    {
        LegendItems.ItemsSource = new[]
        {
            new LegendItem("CN", MediaBrushes.SeaGreen, "Chinese", "Chinese IME is in Chinese input mode."),
            new LegendItem("EN", MediaBrushes.SteelBlue, "IME English", "A Chinese IME is active, but it is currently in English mode."),
            new LegendItem("en", MediaBrushes.DimGray, "English lower", "English keyboard or English input state with CapsLock off."),
            new LegendItem("A", MediaBrushes.DarkSlateGray, "English upper", "English input state with CapsLock on."),
            new LegendItem("?", MediaBrushes.Sienna, "Unknown", "SureType cannot read the current input state from this app or IME.")
        };
    }

    private void LoadSettings()
    {
        _isLoading = true;
        OverlayDurationSlider.Value = _settings.OverlayDurationSeconds;
        FocusCooldownSlider.Value = _settings.InputFocusCooldownSeconds;
        ShowOnStateChangeCheckBox.IsChecked = _settings.ShowOnStateChange;

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

    private void SectionList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        ShowSection(SectionList.SelectedIndex);
    }

    private void ShowSection(int index)
    {
        if (GuidePanel is null || PositionPanel is null || TimingPanel is null || StylePanel is null)
        {
            return;
        }

        GuidePanel.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        PositionPanel.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        TimingPanel.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        StylePanel.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
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
        OverlayDurationValue.Text = $"{_settings.OverlayDurationSeconds:0.0} s";
        FocusCooldownValue.Text = $"{_settings.InputFocusCooldownSeconds:0} s";
    }

    private sealed record LegendItem(string Symbol, MediaBrush Background, string Title, string Description);
}
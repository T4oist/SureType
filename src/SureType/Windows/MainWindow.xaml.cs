using System.Windows;
using System.Windows.Media;
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
    }

    private void LoadLegend()
    {
        LegendItems.ItemsSource = new[]
        {
            CreateLegendItem(InputStateAssets.ChineseInput, "Chinese", "Chinese IME is in Chinese input mode."),
            CreateLegendItem(InputStateAssets.ChineseImeEnglish, "IME English", "A Chinese IME is active, but it is currently in English mode."),
            CreateLegendItem(InputStateAssets.EnglishLower, "English lower", "English keyboard or English input state with CapsLock off."),
            CreateLegendItem(InputStateAssets.EnglishUpper, "English upper", "English input state with CapsLock on."),
            CreateLegendItem(InputStateAssets.Unknown, "Unknown", "SureType cannot read the current input state from this app or IME.")
        };
    }

    private LegendItem CreateLegendItem(string assetKey, string title, string description)
    {
        return new LegendItem(
            (ImageSource)System.Windows.Application.Current.FindResource(assetKey),
            title,
            description);
    }

    private void LoadSettings()
    {
        _isLoading = true;
        OverlayDurationSlider.Value = _settings.OverlayDurationSeconds;
        FocusCooldownSlider.Value = _settings.InputFocusCooldownSeconds;
        ShowOnStateChangeCheckBox.IsChecked = _settings.ShowOnStateChange;
        _isLoading = false;
        UpdateValueLabels();
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

    private sealed record LegendItem(ImageSource Asset, string Title, string Description);
}
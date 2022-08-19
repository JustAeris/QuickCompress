using System.Windows;
using QuickCompress.Core.Common;
using QuickCompress.Gui.Models;

namespace QuickCompress.Gui.Tabs.AudioTab;

public partial class AudioOptionsControl
{
    public AudioOptionsControl()
    {
        InitializeComponent();

        SpeedComboBox.ItemsSource = new[]
        {
            new SpeedPreset("Very Slow", FFmpegSpeed.VerySlow),
            new SpeedPreset("Slower", FFmpegSpeed.Slower),
            new SpeedPreset("Slow", FFmpegSpeed.Slow),
            new SpeedPreset("Medium", FFmpegSpeed.Medium),
            new SpeedPreset("Fast", FFmpegSpeed.Fast),
            new SpeedPreset("Faster", FFmpegSpeed.Faster),
            new SpeedPreset("Very Fast", FFmpegSpeed.VeryFast),
            new SpeedPreset("Super Fast", FFmpegSpeed.SuperFast),
            new SpeedPreset("Ultra Fast", FFmpegSpeed.UltraFast)
        };
        SpeedComboBox.SelectedIndex = 3;

        BitratePresetsBox.ItemsSource = new[]
        {
            new BitratePreset(32),
            new BitratePreset(64),
            new BitratePreset(96),
            new BitratePreset(128),
            new BitratePreset(320),
            new BitratePreset(1141),
            new BitratePreset(-1)
        };
        BitratePresetsBox.SelectedIndex = 3;
        BitrateSlider.ValueChanged += (_, _) => BitratePresetsBox.SelectedItem = new BitratePreset(-1);
        BitratePresetsBox.DropDownClosed += (_, _) =>
        {
            if (BitratePresetsBox.SelectedItem is not BitratePreset bitratePreset) return;
            BitrateSlider.Value = bitratePreset.Bitrate;
            BitratePresetsBox.SelectedItem = bitratePreset;
        };
    }

    public AudioOptions GetCompressionOptions() => new(BitrateRadioButton.IsChecked == true,
        (int)BitrateSlider.Value, ResizeButton.IsChecked == true,
        PercentageReductionButton.IsChecked == true,
        PercentageReductionSlider.Value,
        FileSizeButton.IsChecked == true,
        FileSizeNumberBox.Value,
        ((SpeedPreset)SpeedComboBox.SelectedItem).Speed);



    #region UI Automation

    private void BitrateRadioButton_OnClick(object sender, RoutedEventArgs e)
    {
        BitrateSlider.IsEnabled = true;
        BitratePresetsBox.IsEnabled = true;
        ResizePanel.IsEnabled = false;
        ResizeButton.IsChecked = false;
    }

    private void TargetFileSizeRadioButton_OnClick(object sender, RoutedEventArgs e)
    {
        BitrateSlider.IsEnabled = false;
        BitratePresetsBox.IsEnabled = false;
        ResizePanel.IsEnabled = true;
        BitrateRadioButton.IsChecked = false;
    }

    private void FileSizeRadioButton_OnClick(object sender, RoutedEventArgs e)
    {
        PercentageReductionButton.IsChecked = false;
        PercentageReductionSlider.IsEnabled = false;
        FileSizeNumberBox.IsEnabled = true;
    }

    private void PercentageReductionRadioButton_OnClick(object sender, RoutedEventArgs e)
    {
        FileSizeButton.IsChecked = false;
        PercentageReductionSlider.IsEnabled = true;
        FileSizeNumberBox.IsEnabled = false;
    }

    #endregion



    #region Help Messages

    private void AbsoluteSizeHelp_OnClick(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Help", "The program will try to reach the target file as close as it can but it may (and certainly will) be a few Mb off.\n" +
                                "Note: the program will not compress files smaller than the target size.", MessageBox.MessageBoxIcons.Info);

    private void SpeedHelp_OnClick(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Help", "Select the speed preset that best fits your needs.\n" +
                                "Note that the slower the speed is, the better the result will be.",
            MessageBox.MessageBoxIcons.Info);
    #endregion
}

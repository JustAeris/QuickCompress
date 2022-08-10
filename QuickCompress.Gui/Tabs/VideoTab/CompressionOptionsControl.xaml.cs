using System.Windows;
using QuickCompress.Core.VideoCompression;
using QuickCompress.Gui.Models.VideoCompression;

namespace QuickCompress.Gui.Tabs.VideoTab;

public partial class CompressionOptionsControl
{
    public CompressionOptionsControl()
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
    }

    public Options GetCompressionOptions() => new(CrfCompressionRadioButton.IsChecked == true,
        CrfSlider.Value, TargetFileSizeRadioButton.IsChecked == true,
        PercentageReductionRadioButton.IsChecked == true,
        PercentageReductionSlider.Value,
        FileSizeRadioButton.IsChecked == true,
        FileSizeNumberBox.Value,
        TwoPassesCheckBox.IsChecked == true,
        ((SpeedPreset)SpeedComboBox.SelectedItem).Speed);



    #region UI Automation

    private void CrfCompressionRadioButton_OnClick(object sender, RoutedEventArgs e)
    {
        CrfSlider.IsEnabled = true;
        TargetFileSizePanel.IsEnabled = false;
        TargetFileSizeRadioButton.IsChecked = false;
    }

    private void TargetFileSizeRadioButton_OnClick(object sender, RoutedEventArgs e)
    {
        CrfSlider.IsEnabled = false;
        TargetFileSizePanel.IsEnabled = true;
        CrfCompressionRadioButton.IsChecked = false;
    }

    private void FileSizeRadioButton_OnClick(object sender, RoutedEventArgs e)
    {
        PercentageReductionRadioButton.IsChecked = false;
        PercentageReductionSlider.IsEnabled = false;
        FileSizeNumberBox.IsEnabled = true;
    }

    private void PercentageReductionRadioButton_OnClick(object sender, RoutedEventArgs e)
    {
        FileSizeRadioButton.IsChecked = false;
        PercentageReductionSlider.IsEnabled = true;
        FileSizeNumberBox.IsEnabled = false;
    }

    #endregion



    #region Help Messages

    private void AbsoluteSizeHelp_OnClick(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Help", "The program will try to reach the target file as close as it can but it may (and certainly will) be a few Mb off.\n" +
                                "Note: the program will not compress files smaller than the target size.", MessageBox.MessageBoxIcons.Info);

    private void TwoPassesHelp_OnClick(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Help", "FFmpeg can do two passes when reducing the size of a file. " +
                                "Two passes may give better results, but take longer to compress.",
            MessageBox.MessageBoxIcons.Info);

    private void SpeedHelp_OnClick(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Help", "Select the speed preset that best fits your needs.\n" +
                                "Note that the slower the speed is, the better the result will be.",
            MessageBox.MessageBoxIcons.Info);

    private void CRFHelp_OnClick(object sender, RoutedEventArgs e) =>
        MessageBox.Show("Help", "CRF is a quality parameter that determines the amount of compression.\n\n" +
                                "Range is logarithmic 0-51. 0 is lossless (big files), ~18 is roughly visually lossless, 23 is default, and 51 is worst quality.",
            MessageBox.MessageBoxIcons.Info);

    #endregion
}

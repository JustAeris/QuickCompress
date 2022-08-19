using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using Microsoft.WindowsAPICodePack.Dialogs;
using ModernWpf;
using QuickCompress.Core.AudioCompression;
using QuickCompress.Core.Configuration;

namespace QuickCompress.Gui.Tabs.AudioTab;

public partial class AudioTab
{
    private static CancellationTokenSource _cancellationTokenSource = new();
    private CancellationToken _cancellationToken = _cancellationTokenSource.Token;

    public AudioTab()
    {
        InitializeComponent();

        ThemeManager.Current.ActualAccentColorChanged += ActualAccentColorChanged;
        ActualAccentColorChanged(null, null);

        // Check if there was any files left last time the program closed
        if (!Context.Options.QueuedAudios.Any()) return;

        var dialog = MessageBox.Show("Add remaining files",
            $"The program was still compressing audios files the last time it was closed, do you want to add the {Context.Options.QueuedAudios.Count} remaining files to the list?",
            MessageBox.MessageBoxIcons.Info, MessageBox.MessageBoxButtons.YesNo);
        if (dialog == MessageBox.MessageBoxResult.Yes)
            FileList.SetFiles(Context.Options.QueuedAudiosInfo);
        Context.Options.QueuedAudios.Clear();
    }

    #region UI Automation

    /// <summary>
    /// Custom method to highlight the compress button with the current accent color.
    /// </summary>
    private void ActualAccentColorChanged(ThemeManager? sender, object? args)
    {
        var highlightColor = ThemeManager.Current.ActualAccentColor;
        highlightColor.A = 0x40;
        CompressButton.Background = new SolidColorBrush(highlightColor);
        var invertedColor = new Color
        {
            A = 0x40,
            R = (byte)Math.Abs(Convert.ToInt32(highlightColor.R) - 255),
            G = (byte)Math.Abs(Convert.ToInt32(highlightColor.G) - 255),
            B = (byte)Math.Abs(Convert.ToInt32(highlightColor.B) - 255)
        };
        CancelButton.Background = new SolidColorBrush(invertedColor);
    }

    #endregion

    private async void CompressButton_OnClick(object sender, RoutedEventArgs e)
    {
        // Check if there are files to compress
        var files = FileList.GetFiles();

        if (files.Count == 0)
        {
            MessageBox.Show("Oops", "You did not select any files!", MessageBox.MessageBoxIcons.Info);
            return;
        }

        // Ask for the output directory
        var folderDialog = new CommonOpenFileDialog
        {
            IsFolderPicker = true,
            Title = "Choose a folder to save the compressed files",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            EnsureValidNames = true,
        };
        var result = folderDialog.ShowDialog();

        if (result == CommonFileDialogResult.Cancel || string.IsNullOrEmpty(folderDialog.FileName))
            return;

        var outputFolder = new DirectoryInfo(folderDialog.FileName);
        outputFolder.Create();

        // Overwrite checks to determine what we will replace
        var existingFiles = outputFolder.GetFiles().IntersectBy(files.Select(f => f.Name), info => info.Name).ToList();
        var filesToProcess = files.ExceptBy(existingFiles.Select(x => x.Name), info => info.Name).ToList();
        foreach (var file in existingFiles)
        {
            var overwriteDialog = new OverwriteMessageBox("Oops",
                $"The file {file.Name} already exists in the output folder. Do you want to overwrite it?");
            overwriteDialog.ShowDialog();

            if (overwriteDialog.Result == OverwriteMessageBoxResult.Cancel)
                break;

            if (overwriteDialog.Result == OverwriteMessageBoxResult.Yes)
                filesToProcess.Add(files.First(x => x.Name == file.Name));
            if (overwriteDialog.Result == OverwriteMessageBoxResult.YesToAll)
            {
                filesToProcess.AddRange(files.IntersectBy(existingFiles.Take(new Range(existingFiles.IndexOf(file), existingFiles.Count)).Select(x => x.Name), info => info.Name));
                filesToProcess = filesToProcess.DistinctBy(f => f.Name).ToList();
                break;
            }

            if (overwriteDialog.Result == OverwriteMessageBoxResult.Rename)
            {
                var newFile = new FileInfo(Path.Combine(outputFolder.FullName, $"COMPRESSED_{file.Name}"));
                filesToProcess.Insert(0, newFile);
            }
            else if (overwriteDialog.Result == OverwriteMessageBoxResult.RenameAll)
            {
                var newFiles = existingFiles
                    .Take(new Range(existingFiles.IndexOf(file), existingFiles.Count))
                    .Select(info => new FileInfo(Path.Combine(outputFolder.FullName, $"COMPRESSED_{info.Name}")))
                    .ToList();
                filesToProcess.AddRange(newFiles);
                filesToProcess = filesToProcess.DistinctBy(f => f.Name).ToList();
                break;
            }
        }

        // Verify that we have anything to process
        if (filesToProcess.Count == 0)
        {
            MessageBox.Show("Operation cancelled", "No files to compress.", MessageBox.MessageBoxIcons.Info);
            return;
        }

        OverallProgressBar.Value = 0;
        OverallProgressBar.Maximum = filesToProcess.Count;
        LockGui(true);
        foreach (var info in filesToProcess) Context.Options.QueuedAudios.Add(files.FirstOrDefault(f => f.Name == info.Name.Replace("COMPRESSED_", ""))?.FullName ?? string.Empty);

        using var ffmpeg = new FFmpeg();
        var parent = Window.GetWindow(this);
        var (isSetBitrate, bitrate, isResizing, isPercentage, percentage, _, absoluteSize, speed) = OptionsControl.GetCompressionOptions();
        var i = 0;
        foreach (var file in filesToProcess)
        {
            try
            {
                if (isSetBitrate)
                {
                    var j = i;

                    await Task.Run(async () =>
                    {
                        await ffmpeg.SetAudioBitrateAsync(new FileInfo(file.FullName.Replace("COMPRESSED_", "")), Path.Combine(outputFolder.FullName, file.Name),
                            bitrate * 1000, speed, d => Dispatcher.Invoke(() =>
                            {
                                OverallProgressBar.Value = j + d;
                                parent.TaskbarItemInfo.ProgressValue = (j + d) / OverallProgressBar.Maximum;
                            }),
                            _cancellationToken);
                    }, _cancellationToken);
                }

                else if (isResizing)
                {
                    int targetSize;
                    if (isPercentage)
                        targetSize = (int)(file.Length * (int)percentage / 100);
                    else
                        targetSize = (int)absoluteSize * 1_000_000;
                    var j = i;

                    if (targetSize > files.First(f => f.Name == file.Name).Length) continue;

                    await Task.Run(async () =>
                    {
                        await ffmpeg.ResizeAudioFile(file, Path.Combine(outputFolder.FullName, file.Name),
                            targetSize, speed, d => Dispatcher.Invoke(() =>
                            {
                                OverallProgressBar.Value = j + d;
                                parent.TaskbarItemInfo.ProgressValue = (j + d) / OverallProgressBar.Maximum;
                            }), _cancellationToken);
                    }, _cancellationToken);
                }

            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operation cancelled", "Compression stopped.", MessageBox.MessageBoxIcons.Info);
                Context.Options.QueuedAudios.Clear();
                LockGui(false);
                return;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Oops",
                    $"Error while compressing {file.Name}: {exception.Message.Replace("\r\n", " ")}",
                    MessageBox.MessageBoxIcons.Error, exception: exception);
                continue;
            }
            finally
            {
                i++;
            }

            Context.Options.QueuedAudios.Remove(file.FullName);
            FileList.SetFiles(Context.Options.QueuedAudiosInfo);
        }

        await Task.Delay(500, _cancellationToken);
        LockGui(false);
        MessageBox.Show("Operation completed", $"All files compressed, except {FileList.FileListView.Items.Count} files.", MessageBox.MessageBoxIcons.Info);
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();

        // Regenerate the cancellation token for future use
        _cancellationTokenSource = new CancellationTokenSource();
        _cancellationToken = _cancellationTokenSource.Token;

        LockGui(false);
    }

    private void LockGui(bool locked)
    {
        OptionsControl.IsEnabled = !locked;
        FileList.IsEnabled = !locked;
        CompressButton.IsEnabled = !locked;

        CompressButtonContent.Visibility = locked ? Visibility.Hidden : Visibility.Visible;
        CancelButton.Visibility = locked ? Visibility.Visible : Visibility.Collapsed;
        OverallProgressBar.Visibility = locked ? Visibility.Visible : Visibility.Hidden;
        OverallProgressBar.Value = 0;

        Window.GetWindow(this)!.TaskbarItemInfo.ProgressState = locked ? TaskbarItemProgressState.Normal : TaskbarItemProgressState.None;
    }
}

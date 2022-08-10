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
using QuickCompress.Core.Configuration;
using QuickCompress.Core.VideoCompression;

namespace QuickCompress.Gui.Tabs.VideoTab;

public partial class VideosTab
{

    private static CancellationTokenSource _cancellationTokenSource = new();
    private CancellationToken _cancellationToken = _cancellationTokenSource.Token;

    public VideosTab()
    {
        InitializeComponent();

        ThemeManager.Current.ActualAccentColorChanged += ActualAccentColorChanged;
        ActualAccentColorChanged(null, null);

        // Check if there was any files left last time the program closed
        if (!Context.Options.VideoCompressionOptions.QueuedFiles.Any()) return;

        var dialog = MessageBox.Show("Add remaining files",
            $"The program was still compressing videos the las time it was closed, do you want to add the {Context.Options.VideoCompressionOptions.QueuedFiles.Count} remaining files to the list?",
            MessageBox.MessageBoxIcons.Info, MessageBox.MessageBoxButtons.YesNo);
        if (dialog == MessageBox.MessageBoxResult.Yes)
            FileList.SetFiles(Context.Options.VideoCompressionOptions.QueuedFilesInfo);
        Context.Options.VideoCompressionOptions.QueuedFiles.Clear();
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
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
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
        foreach (var info in filesToProcess) Context.Options.VideoCompressionOptions.QueuedFiles.Add(files.First(f => f.Name == info.Name).FullName);

        using var ffmpeg = new FFmpeg();
        var parent = Window.GetWindow(this);
        var (isCRF, crf, isResizing, isPercentage, percentage, _, absoluteSize, twoPasses, speed) = OptionsControl.GetCompressionOptions();
        var i = 0;
        foreach (var file in filesToProcess)
        {
            try
            {
                if (isCRF)
                {
                    var j = i;

                    await Task.Run(async () =>
                    {
                        await ffmpeg.CompressVideoAsync(file, Path.Combine(outputFolder.FullName, file.Name),
                            (int)crf, speed, d => Dispatcher.Invoke(() =>
                            {
                                OverallProgressBar.Value = j + d / 100;
                                parent.TaskbarItemInfo.ProgressValue = (j + d / 100) / OverallProgressBar.Maximum;
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
                        await ffmpeg.SetVideoFileSizeAsync(file, Path.Combine(outputFolder.FullName, file.Name),
                            targetSize, speed, twoPasses, tuple =>
                            {
                                var (progress, pass) = tuple;
                                Dispatcher.Invoke(() =>
                                {
                                    OverallProgressBar.Value = progress / (pass == 2 ? 200 : 100) + j + (pass == 2 ? .5 : 0);
                                    parent.TaskbarItemInfo.ProgressValue = (progress / (pass == 2 ? 200 : 100) + j + (pass == 2 ? .5 : 0)) / OverallProgressBar.Maximum;
                                });
                            }, _cancellationToken);
                    }, _cancellationToken);
                }

            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operation cancelled", "Compression stopped.", MessageBox.MessageBoxIcons.Info);
                Context.Options.VideoCompressionOptions.QueuedFiles.Clear();
                LockGui(false);
                return;
            }
            catch (Exception exception)
            {
                MessageBox.Show("Oops",
                    $"Error while compressing {file.Name}: {exception.Message.Replace("\r\n", " ")}",
                    MessageBox.MessageBoxIcons.Error, exception: exception);
            }
            finally
            {
                i++;
            }

            Context.Options.VideoCompressionOptions.QueuedFiles.Remove(file.FullName);
            FileList.SetFiles(Context.Options.VideoCompressionOptions.QueuedFilesInfo);
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

    /// <summary>
    /// Lock the GUI during compression
    /// </summary>
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

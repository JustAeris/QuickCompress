using System.Diagnostics;
using System.Text.RegularExpressions;

namespace QuickCompress.Core.VideoCompression;

public sealed partial class FFmpeg : IDisposable
{
    private readonly DirectoryInfo _tempDirectory;
    private int _totalFrames;
    private string? _fullLog;
    private bool _isChangingBitrate;
    private bool _isOnSecondPass;

    public FFmpeg()
    {
        var cd = Directory.GetCurrentDirectory();
        var path = Path.Combine(cd, "Tools\\FFmpeg\\Temp");
        _tempDirectory = new DirectoryInfo(path);
        _tempDirectory.Create();
    }

    public event EventHandler<(double, int)>? ProgressChanged;
    private void OnProgressChanged(double e, int pass) => ProgressChanged?.Invoke(this, (e, pass));

    private void FFmpegProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;
        _fullLog += e.Data + "\n";
        var match = Regex.Match(e.Data, @"frame=\s*([0-9]*)");
        var progress = match.Success ? double.Parse(match.Groups[1].Value) * 100 / _totalFrames : -1;
        OnProgressChanged(progress, _isChangingBitrate ? _isOnSecondPass ? 2 : 1 : 0);
    }

    /// <summary>
    /// Compresses a video file using FFmpeg.
    /// </summary>
    /// <param name="inputFile">Video file to compress</param>
    /// <param name="outputFile">Where to output the compressed file</param>
    /// <param name="crf">Constant Rate Factor. Range is logarithmic 0-51. 0 is lossless (big files), ~18 is roughly visually lossless, 23 is default, and 51 is worst quality.</param>
    /// <param name="preset">What FFmpeg speed to use, the slower the speed is, the better the result will be</param>
    /// <param name="token">The cancellation token to pass to the process</param>
    public async Task CompressVideoAsync(FileInfo inputFile, string outputFile, int crf = 23, FFmpegSpeed preset = FFmpegSpeed.Medium, CancellationToken token = default)
    {
        // Perform arguments checks
        if (!File.Exists(inputFile.FullName))
            throw new FileNotFoundException("Input file not found", inputFile.FullName);

        if (crf is < 0 or > 51)
            throw new ArgumentOutOfRangeException(nameof(crf), "CRF must be between 0 and 51");

        // Get the number of frames, will be used to give a percentage of completion in the ProgressChanged event
        _totalFrames = await GetTotalFrames(inputFile.FullName, token);

        var exitCode = await StartFFmpeg($"-i \"{inputFile.FullName}\" -c:v libx264 -crf {crf} -preset {Enum.GetName(preset)?.ToLower()} -vbr 5 " +
                                         $"-pix_fmt yuv420p -vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" -y \"{Path.Combine(_tempDirectory.FullName, new FileInfo(outputFile).Name)}\"", token);

        if (exitCode != 0)
            throw new Exception("FFmpeg exited with error code " + exitCode + "\n" + _fullLog);

        File.Move(Path.Combine(_tempDirectory.FullName, new FileInfo(outputFile).Name), outputFile, true);
    }

    /// <summary>
    /// Reduces the size of a video file by changing the bitrate using FFmpeg.
    /// </summary>
    /// <param name="inputFile">File to process</param>
    /// <param name="outputFile">Where to output the new file</param>
    /// <param name="targetSize">File size to reach in bytes</param>
    /// <param name="preset">What FFmpeg speed to use, the slower the speed is, the better the result will be</param>
    /// <param name="twoPasses">By using two passes, the result will be more accurate to the given size, however it can take a lot longer</param>
    /// <exception cref="ArgumentOutOfRangeException">Target size is either greater than input file size or lower than 0</exception>
    /// <exception cref="FileNotFoundException">File could not be found</exception>
    /// <exception cref="InvalidOperationException">Could not get the video duration</exception>
    public async Task SetVideoFileSizeAsync(FileInfo inputFile, string outputFile, int targetSize, FFmpegSpeed preset = FFmpegSpeed.Medium, bool twoPasses = true)
    {
        // Perform arguments checks
        if (!File.Exists(inputFile.FullName))
            throw new FileNotFoundException("Input file not found", inputFile.FullName);

        if (targetSize > inputFile.Length)
            throw new ArgumentOutOfRangeException(nameof(targetSize), "Target size must be less than the file size");

        if (targetSize < 0)
            throw new ArgumentOutOfRangeException(nameof(targetSize), "Target size must be greater than 0");

        // Calculate the bitrate to use
        var coefficient = (double)targetSize / inputFile.Length;
        var bitrate = Math.Floor(await GetBitrate(inputFile.FullName) * coefficient);

        // Get the number of frames, will be used to give a percentage of completion in the ProgressChanged event
        _totalFrames = await GetTotalFrames(inputFile.FullName);

        _isChangingBitrate = true;
        var exitCode =
            await StartFFmpeg(
                $"-y -i \"{inputFile.FullName}\" -pass 1 -codec:v libx264 -preset {Enum.GetName(preset)?.ToLower()} " +
                $"-b:v {bitrate} -an \"{Path.Combine(_tempDirectory.FullName, new FileInfo(outputFile).Name)}\"");
        _isChangingBitrate = false;

        // Throw an exception if there was an error
        if (exitCode != 0)
            throw new Exception("FFmpeg exited with error code " + exitCode + "\n" + _fullLog);

        if (twoPasses)
        {
            _isChangingBitrate = true;
            _isOnSecondPass = true;
            exitCode =
                await StartFFmpeg(
                    $"-y -i \"{Path.Combine(_tempDirectory.FullName, new FileInfo(outputFile).Name)}\" -pass 2 " +
                    $"-codec:v libx264 -preset {Enum.GetName(preset)?.ToLower()} -b:v {bitrate} -b:a 128k " +
                    $"\"{Path.Combine(_tempDirectory.FullName, "2" + new FileInfo(outputFile).Name)}\"");
            _isChangingBitrate = false;
            _isOnSecondPass = false;

            if (exitCode != 0)
                throw new Exception("FFmpeg exited with error code " + exitCode + "\n" + _fullLog);

            File.Move(Path.Combine(_tempDirectory.FullName, "2" + new FileInfo(outputFile).Name), outputFile, true);
            return;
        }
        File.Move(Path.Combine(_tempDirectory.FullName, new FileInfo(outputFile).Name), outputFile, true);
    }

    public void Dispose() => _tempDirectory.Delete(true);
}

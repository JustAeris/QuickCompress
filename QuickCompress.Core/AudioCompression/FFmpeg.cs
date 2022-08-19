using System.Text;
using QuickCompress.Core.Common;
using QuickCompress.Core.Common.ProgressWatcher;
using QuickCompress.Core.Configuration;
using QuickCompress.Core.VideoCompression;

namespace QuickCompress.Core.AudioCompression;

public class FFmpeg : IDisposable
{
    private readonly DirectoryInfo _tempDirectory;

    public FFmpeg()
    {
        var cd = Directory.GetCurrentDirectory();
        var path = Path.Combine(cd, Context.Options.TempPath);
        _tempDirectory = new DirectoryInfo(path);
        _tempDirectory.Create();
    }

    /// <summary>
    /// Reduces the size of a video file by changing the bitrate using FFmpeg.
    /// </summary>
    /// <param name="inputFile">File to process</param>
    /// <param name="outputFile">Where to output the new file</param>
    /// <param name="bitrate">The bitrate to set for the given audio stream</param>
    /// <param name="preset">What FFmpeg speed to use, the slower the speed is, the better the result will be</param>
    /// <param name="onProgress">This action will trigger when progress is made</param>
    /// <param name="token">The cancellation token to pass to the process</param>
    /// <exception cref="ArgumentOutOfRangeException">Target size is either greater than input file size or lower than 0</exception>
    /// <exception cref="FileNotFoundException">File could not be found</exception>
    /// <exception cref="InvalidOperationException">Could not get the video duration</exception>
    public async Task SetAudioBitrateAsync(FileInfo inputFile, string outputFile, long bitrate,
        FFmpegSpeed preset = FFmpegSpeed.Medium, Action<double>? onProgress = null, CancellationToken token = default)
    {
        // Perform arguments checks
        if (!File.Exists(inputFile.FullName))
            throw new FileNotFoundException("Input file not found", inputFile.FullName);

        if (bitrate < 0)
            throw new ArgumentOutOfRangeException(nameof(bitrate), "Bitrate must be greater than 0");

        // Get the number of frames, will be used to give a percentage of completion in the ProgressChanged event
        var fileBitrate = await FFmpegUtils.GetBitrateAsync(inputFile.FullName, FFmpegUtils.BitrateType.Audio, token);

        if(fileBitrate.Bitrate < 0 || fileBitrate.ExitCode != 0)
            throw new Exception(
                $"FFprobe could not parse the bitrate and exited with error code {fileBitrate.ExitCode}\n{fileBitrate.FullOutput}");

        var coefficient = (double)bitrate / fileBitrate.Bitrate;
        var targetSize = inputFile.Length * coefficient;

        var watcher = new SetAudioBitrateProgressWatcher(onProgress ?? new Action<double>(_ => { }), targetSize);

        var ffmpegArgs = new StringBuilder();
        ffmpegArgs.Append($"-y -i \"{inputFile.FullName}\" ")
            .Append("-map 0:a:0 ")
            .Append($"-b:a {bitrate/1000}k ");
        if (Enum.GetName(preset) != null)
            ffmpegArgs.Append($"-preset {Enum.GetName(preset)?.ToLower()} ");
        ffmpegArgs.Append($"\"{Path.Combine(_tempDirectory.FullName, new FileInfo(outputFile).Name)}\"");

        var result = await FFmpegUtils.StartFFmpegAsync(ffmpegArgs.ToString(), watcher, token);

        // Throw an exception if there was an error
        if (result.ExitCode != 0)
            throw new Exception("FFmpeg exited with error code " + result.ExitCode + "\n" + result.Output);

        File.Move(Path.Combine(_tempDirectory.FullName, new FileInfo(outputFile).Name), outputFile, true);
    }

    public async Task ResizeAudioFile(FileInfo inputFile, string outputFile, int targetSize,
        FFmpegSpeed preset = FFmpegSpeed.Medium, Action<double>? onProgress = null, CancellationToken token = default)
    {
        var coefficient = (double)targetSize / inputFile.Length;
        var bitrate = await FFmpegUtils.GetBitrateAsync(inputFile.FullName, FFmpegUtils.BitrateType.Audio, token);
        var newBitrate = (long)(bitrate.Bitrate * coefficient);
        await SetAudioBitrateAsync(inputFile, outputFile, newBitrate, preset, onProgress, token);
    }
    public void Dispose()
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000);
            _tempDirectory.Delete(true);
        });
    }
}

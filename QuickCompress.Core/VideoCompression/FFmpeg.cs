using System.Text;
using QuickCompress.Core.Common.ProgressWatcher;
using QuickCompress.Core.Common;
using QuickCompress.Core.Configuration;

namespace QuickCompress.Core.VideoCompression;

public sealed class FFmpeg : IDisposable
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
    /// Compresses a video file using FFmpeg.
    /// </summary>
    /// <param name="inputFile">Video file to compress</param>
    /// <param name="outputFile">Where to output the compressed file</param>
    /// <param name="crf">Constant Rate Factor. Range is logarithmic 0-51. 0 is lossless (big files), ~18 is roughly visually lossless, 23 is default, and 51 is worst quality.</param>
    /// <param name="preset">What FFmpeg speed to use, the slower the speed is, the better the result will be</param>
    /// <param name="onProgress">Action which will trigger on progress. The double is the percentage</param>
    /// <param name="token">The cancellation token to pass to the process</param>
    public async Task CompressVideoAsync(FileInfo inputFile, string outputFile, int crf = 23, FFmpegSpeed preset = FFmpegSpeed.Medium, Action<double>? onProgress = null, CancellationToken token = default)
    {
        // Perform arguments checks
        if (!File.Exists(inputFile.FullName))
            throw new FileNotFoundException("Input file not found", inputFile.FullName);

        if (crf is < 0 or > 51)
            throw new ArgumentOutOfRangeException(nameof(crf), "CRF must be between 0 and 51");

        // Get the number of frames, will be used to give a percentage of completion in the ProgressChanged event
        var totalFrames = await FFmpegUtils.GetTotalFramesAsync(inputFile.FullName, token);

        if(totalFrames.Frames < 0 || totalFrames.ExitCode != 0)
            throw new Exception(
                $"FFprobe could not parse the number of frames and exited with error code {totalFrames.ExitCode}\n{totalFrames.FullOutput}");

        var watcher = new CompressionProgressWatcher(onProgress ?? new Action<double>(_ => { }), totalFrames.Frames);

        var ffMpegArgs = new StringBuilder();
        ffMpegArgs.Append($"-y -i \"{inputFile.FullName}\" ")
            .Append("-c:v libx264 ")
            .Append($"-crf {crf} ");
        if (Enum.GetName(preset) != null)
            ffMpegArgs.Append($"-preset {Enum.GetName(preset)?.ToLower()} ");
        ffMpegArgs.Append("-vbr 5 ")
            .Append("-pix_fmt yuv420p ")
            .Append("-vf \"scale=trunc(iw/2)*2:trunc(ih/2)*2\" ")
            .Append($"-y \"{Path.Combine(_tempDirectory.FullName, new FileInfo(outputFile).Name)}\"");

        var result = await FFmpegUtils.StartFFmpegAsync(ffMpegArgs.ToString(), watcher, token);

        if (result.ExitCode != 0)
            throw new Exception("FFmpeg exited with error code " + result.ExitCode + "\n" + result.Output);

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
    /// <param name="onProgress">This action will trigger when progress is made</param>
    /// <param name="token">The cancellation token to pass to the process</param>
    /// <exception cref="ArgumentOutOfRangeException">Target size is either greater than input file size or lower than 0</exception>
    /// <exception cref="FileNotFoundException">File could not be found</exception>
    /// <exception cref="InvalidOperationException">Could not get the video duration</exception>
    public async Task SetVideoBitrateAsync(FileInfo inputFile, string outputFile, int targetSize, FFmpegSpeed preset = FFmpegSpeed.Medium, bool twoPasses = true, Action<(double Progress, int Pass)>? onProgress = null, CancellationToken token = default)
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
        var bitrate = await FFmpegUtils.GetBitrateAsync(inputFile.FullName, FFmpegUtils.BitrateType.Video, token);

        if (bitrate.Bitrate < 0 || bitrate.ExitCode != 0)
            throw new Exception(
                $"FFprobe could not parse the bitrate and exited with error code {bitrate.ExitCode}\n{bitrate.FullOutput}");

        var newBitrate = (int)(bitrate.Bitrate * coefficient);

        // Get the number of frames, will be used to give a percentage of completion in the ProgressChanged event
        var totalFrames = await FFmpegUtils.GetTotalFramesAsync(inputFile.FullName, token);

        if(totalFrames.Frames < 0 || totalFrames.ExitCode != 0)
            throw new Exception(
                $"FFprobe could not parse the number of frames and exited with error code {totalFrames.ExitCode}\n{totalFrames.FullOutput}");

        var watcher = new SetVideoBitrateProgressWatcher(onProgress ?? new Action<(double Progress, int Pass)>(_ => { }), totalFrames.Frames, 1);

        var ffmpegArgs = new StringBuilder();
        ffmpegArgs.Append($"-y -i \"{inputFile.FullName}\" ")
            .Append("-pass 1 ")
            .Append("-c:v libx264 ");
        if (Enum.GetName(preset) != null)
            ffmpegArgs.Append($"-preset {Enum.GetName(preset)?.ToLower()} ");
        ffmpegArgs.Append($"-b:v {newBitrate} ")
            .Append($"\"{Path.Combine(_tempDirectory.FullName, new FileInfo(outputFile).Name)}\"");

        var result = await FFmpegUtils.StartFFmpegAsync(ffmpegArgs.ToString(), watcher, token);

        // Throw an exception if there was an error
        if (result.ExitCode != 0)
            throw new Exception("FFmpeg exited with error code " + result.ExitCode + "\n" + result.Output);

        if (twoPasses)
        {
            watcher = new SetVideoBitrateProgressWatcher(onProgress ?? new Action<(double Progress, int Pass)>(_ => { }), totalFrames.Frames, 2);

            ffmpegArgs.Clear();
            ffmpegArgs.Append($"-y -i \"{inputFile.FullName}\" ")
                .Append("-pass 2 ")
                .Append("-c:v libx264 ");
            if (Enum.GetName(preset) != null)
                ffmpegArgs.Append($"-preset {Enum.GetName(preset)?.ToLower()} ");
            ffmpegArgs.Append($"-b:v {newBitrate} ")
                .Append("-b:a 128k ")
                .Append($"\"{Path.Combine(_tempDirectory.FullName, "2" + new FileInfo(outputFile).Name)}\"");

            result = await FFmpegUtils.StartFFmpegAsync(ffmpegArgs.ToString(), watcher, token);

            if (result.ExitCode != 0)
                throw new Exception("FFmpeg exited with error code " + result.ExitCode + "\n" + result.Output);

            File.Move(Path.Combine(_tempDirectory.FullName, "2" + new FileInfo(outputFile).Name), outputFile, true);
            return;
        }
        File.Move(Path.Combine(_tempDirectory.FullName, new FileInfo(outputFile).Name), outputFile, true);
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

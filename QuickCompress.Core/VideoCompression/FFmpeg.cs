using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.WindowsAPICodePack.Shell;

namespace QuickCompress.Core.VideoCompression;

public static class FFmpeg
{
    static FFmpeg()
    {
        GlobalFFOptions.Configure(new FFOptions { BinaryFolder = "/Tools/FFmpeg/ffmpeg.exe", TemporaryFilesFolder = "/Tools/FFmpeg/Temp" });
    }

    /// <summary>
    /// Compresses a video file using FFmpeg.
    /// </summary>
    /// <param name="inputFile">Video file to compress</param>
    /// <param name="outputFile">Where to output the compressed file</param>
    /// <param name="crf">Constant Rate Factor. Range is logarithmic 0-51. 0 is lossless (big files), ~18 is roughly visually lossless, 23 is default, and 51 is worst quality.</param>
    /// <param name="preset">What FFmpeg speed to use, the slower the speed is, the better the result will be</param>
    /// <param name="onProgress">An action which will trigger on any progress</param>
    public static async Task CompressVideo(FileInfo inputFile, string outputFile, int crf = 23, Speed preset = Speed.Medium, Action<double>? onProgress = null)
    {
        if (!File.Exists(inputFile.FullName))
            throw new FileNotFoundException("Input file not found", inputFile.FullName);

        if (crf is < 0 or > 51)
            throw new ArgumentOutOfRangeException(nameof(crf), "CRF must be between 0 and 51");

        await FFMpegArguments.FromFileInput(inputFile).OutputToFile(outputFile, true,
            options => options
                .WithVideoCodec(VideoCodec.LibX264)
                .WithConstantRateFactor(crf)
                .WithSpeedPreset(preset)
                .WithAudioCodec(AudioCodec.Aac)
                .WithVideoFilters(filterOptions => filterOptions.Scale(VideoSize.Original))
                .ForcePixelFormat("yuv420p"))
            .NotifyOnProgress(onProgress ?? (_ => { }), TimeSpan.Zero)
            .ProcessAsynchronously();
    }

    /// <summary>
    /// Reduces the size of a video file by changing the bitrate using FFmpeg.
    /// </summary>
    /// <param name="inputFile">File to process</param>
    /// <param name="outputFile">Where to output the new file</param>
    /// <param name="targetSize">File size to reach</param>
    /// <param name="preset">What FFmpeg speed to use, the slower the speed is, the better the result will be</param>
    /// <param name="onProgress">Action which will trigger on any progress</param>
    /// <exception cref="ArgumentOutOfRangeException">Target size is either greater than input file size or lower than 0</exception>
    /// <exception cref="FileNotFoundException">File could not be found</exception>
    /// <exception cref="InvalidOperationException">Could not get the video duration</exception>
    public static async Task SetVideoFileSize(FileInfo inputFile, string outputFile, int targetSize, Speed preset = Speed.Medium, Action<double>? onProgress = null)
    {
        if (!File.Exists(inputFile.FullName))
            throw new FileNotFoundException("Input file not found", inputFile.FullName);

        if (targetSize > inputFile.Length)
            throw new ArgumentOutOfRangeException(nameof(targetSize), "Target size must be less than the file size");

        if (targetSize < 0)
            throw new ArgumentOutOfRangeException(nameof(targetSize), "Target size must be greater than 0");

        using var shell = ShellObject.FromParsingName(inputFile.FullName);
        var duration = (shell.Properties.System.Media.Duration.Value ?? throw new InvalidOperationException("Duration not found")) / 10_000_000;
        var bitrate = duration / (ulong)targetSize;

        await FFMpegArguments.FromFileInput(inputFile).OutputToFile(outputFile, true,
                options => options
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithVideoBitrate((int)bitrate)
                    .WithSpeedPreset(preset)
                    .WithAudioCodec(AudioCodec.Aac)
                    .WithVideoFilters(filterOptions => filterOptions.Scale(VideoSize.Original))
                    .ForcePixelFormat("yuv420p"))
            .NotifyOnProgress(onProgress ?? (_ => { }), TimeSpan.Zero)
            .ProcessAsynchronously();
    }
}

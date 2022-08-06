using System.Diagnostics;

namespace QuickCompress.Core.VideoCompression;

public partial class FFmpeg
{
    /// <summary>
    /// Starts FFmpeg process
    /// </summary>
    /// <param name="args">CLI arguments</param>
    /// <param name="token">The cancellation token to pass to the process</param>
    /// <returns>The exit code</returns>
    private async Task<int> StartFFmpeg(string args, CancellationToken token = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Directory.GetCurrentDirectory(), "Tools\\FFmpeg\\ffmpeg.exe"),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                Arguments = args
            }
        };
        process.ErrorDataReceived += FFmpegProcessOnOutputDataReceived;
        process.Start();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(token);
        return process.ExitCode;
    }

    /// <summary>
    /// Starts FFprobe process
    /// </summary>
    /// <param name="args">CLI arguments</param>
    /// <param name="token">The cancellation token to pass to the process</param>
    /// <returns>The whole output</returns>
    private async Task<string> StartFFprobe(string args, CancellationToken token = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Directory.GetCurrentDirectory(), "Tools\\FFmpeg\\ffprobe.exe"),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                Arguments = args
            }
        };
        process.Start();
        await process.WaitForExitAsync(token);
        return await process.StandardOutput.ReadToEndAsync();
    }

    /// <summary>
    /// Gets the number of frames in a video
    /// </summary>
    /// <param name="filePath">File to process</param>
    /// <param name="token">The cancellation token to pass to the process</param>
    /// <returns>The total number of frames, -1 if none</returns>
    private async Task<int> GetTotalFrames(string filePath, CancellationToken token = default)
    {
        var frames =
            await StartFFprobe(
                $"-v error -select_streams v:0 -count_packets -show_entries stream=nb_read_packets -of csv=p=0 \"{filePath}\"", token);
        return int.TryParse(frames, out var result) ? result : -1;
    }

    /// <summary>
    /// Gets the bitrate of a video
    /// </summary>
    /// <param name="filePath">File to process</param>
    /// <param name="token">The cancellation token to pass to the process</param>
    /// <returns>The bitrate of the given video, -1 if none</returns>
    private async Task<long> GetBitrate(string filePath, CancellationToken token = default)
    {
        var bitrate =
            await StartFFprobe(
                $"-v error -select_streams v:0 -show_entries stream=bit_rate -of default=noprint_wrappers=1:nokey=1 \"{filePath}\"", token);
        return long.TryParse(bitrate, out var result) ? result : -1;
    }
}

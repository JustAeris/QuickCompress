using System.Diagnostics;
using System.Text;
using QuickCompress.Core.Common.ProgressWatcher;
using QuickCompress.Core.Configuration;

namespace QuickCompress.Core.Common;

internal static class FFmpegUtils
{
    /// <summary>
    /// Starts a given process with given arguments
    /// </summary>
    /// <param name="processPath">Executable path</param>
    /// <param name="args">CLI arguments</param>
    /// <param name="onOutputDataReceived">Event handler to handle the <see cref="Process.OutputDataReceived"/> event</param>
    /// <param name="onErrorDataReceived">Event handler to handle the <see cref="Process.ErrorDataReceived"/> event</param>
    /// <param name="cancellationToken">Cancellation token to cancel if needed</param>
    /// <returns>The process after execution</returns>
    private static async Task<Process> StartProcessAsync(string processPath, string args, DataReceivedEventHandler? onOutputDataReceived = null, DataReceivedEventHandler? onErrorDataReceived = null, CancellationToken cancellationToken = default)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Directory.GetCurrentDirectory(), processPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = args
            }
        };

        if (onOutputDataReceived != null) process.OutputDataReceived += onOutputDataReceived;
        if (onErrorDataReceived != null) process.ErrorDataReceived += onErrorDataReceived;

        process.Start();

        if (onOutputDataReceived != null) process.BeginOutputReadLine();
        if (onErrorDataReceived != null) process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        return process;
    }

    /// <summary>
    /// Starts FFmpeg and returns the exit code and output
    /// </summary>
    /// <param name="args">CLI arguments</param>
    /// <param name="cancellationToken">The cancellation token to pass to the process</param>
    /// <returns>Exit code of FFprobe and full output</returns>
    private static async Task<(int ExitCode, string Output)> StartFFprobeAsync(string args, CancellationToken cancellationToken = default)
    {
        using var process = await StartProcessAsync(Path.Combine(Directory.GetCurrentDirectory(), Context.Options.FFprobePath), args, null, null, cancellationToken);

        var stdOut = await process.StandardOutput.ReadToEndAsync();
        var exitCode = process.ExitCode;
        return (exitCode, stdOut);
    }

    /// <summary>
    /// Starts FFmpeg and returns the exit code and output
    /// </summary>
    /// <param name="args">CLI arguments</param>
    /// <param name="watcher">The progress watcher to track the state of the current process</param>
    /// <param name="cancellationToken">The cancellation token to pass to the process</param>
    /// <typeparam name="T">The type of the watcher. It will be inferred automatically, no need to explicitly state it.</typeparam>
    /// <returns>Exit code of FFmpeg and full output</returns>
    internal static async Task<(int ExitCode, string Output)> StartFFmpegAsync<T>(string args, IProgressWatcher<T>? watcher = null, CancellationToken cancellationToken = default)
    {
        using var process = await StartProcessAsync(Path.Combine(Directory.GetCurrentDirectory(), Context.Options.FFmpegPath), args,
            watcher?.OnOutputReceived ?? null, watcher?.OnErrorReceived ?? null, cancellationToken);
        return (process.ExitCode, watcher?.StandardLog.ToString())!;
    }

    /// <summary>
    /// Gets the number of frames in a video
    /// </summary>
    /// <param name="filePath">File to process</param>
    /// <param name="token">The cancellation token to pass to the process</param>
    /// <returns>The total number of frames, -1 if none</returns>
    internal static async Task<(int ExitCode, int Frames, string FullOutput)> GetTotalFramesAsync(string filePath, CancellationToken token = default)
    {
        var ffProbeArgs = new StringBuilder()
            .Append("-v error ")
            .Append("-select_streams v:0 ")
            .Append("-count_packets ")
            .Append("-show_entries stream=nb_read_packets ")
            .Append($"-of csv=p=0 \"{filePath}\"");

        var result = await StartFFprobeAsync(ffProbeArgs.ToString(), token);
        var framesCount = int.TryParse(result.Output, out var frames) ? frames : -1;
        return (result.ExitCode, framesCount, result.Output);
    }

    /// <summary>
    /// Gets the bitrate of a video/audio file
    /// </summary>
    /// <param name="filePath">File to process</param>
    /// <param name="bitrateType">Whether to select the audio or video bitrate of the file</param>
    /// <param name="token">The cancellation token to pass to the process</param>
    /// <returns>The bitrate of the given video/audio file, -1 if none</returns>
    internal static async Task<(int ExitCode, long Bitrate, string FullOutput)> GetBitrateAsync(string filePath, BitrateType bitrateType, CancellationToken token = default)
    {
        var ffProbeArgs = new StringBuilder()
            .Append("-v error ")
            .Append(bitrateType switch
                {
                    BitrateType.Video => "-select_streams v:0 ",
                    BitrateType.Audio => "-select_streams a:0 ",
                    _ => throw new ArgumentOutOfRangeException(nameof(bitrateType), bitrateType, null)
                })
            .Append("-show_entries stream=bit_rate ")
            .Append($"-of default=noprint_wrappers=1:nokey=1 \"{filePath}\"");

        var result = await StartFFprobeAsync(ffProbeArgs.ToString(), token);
        var bitrate = long.TryParse(result.Output, out var bitrateValue) ? bitrateValue : -1;
        return (result.ExitCode, bitrate, result.Output);
    }

    public enum BitrateType
    {
        Audio,
        Video
    }
}

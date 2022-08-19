using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace QuickCompress.Core.Configuration;

[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
public class Options : Context
{
    private ThemeType _theme;

    public Options(string fFmpegPath, string fFprobePath, string tempPath, ObservableCollection<string> queuedVideos, ObservableCollection<string> queuedAudios)
    {

        FFmpegPath = fFmpegPath;
        FFprobePath = fFprobePath;
        TempPath = tempPath;
        QueuedVideos = queuedVideos;
        QueuedAudios = queuedAudios;
        QueuedVideos.CollectionChanged += (_, _) => SaveOptions();
        QueuedAudios.CollectionChanged += (_, _) => SaveOptions();
    }

    /// <summary>
    /// The theme to use for the application
    /// </summary>
    public ThemeType Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            SaveOptions();
        }
    }

    /// <summary>
    /// The list of files still left to be compressed
    /// </summary>
    public ObservableCollection<string> QueuedVideos { get; }

    [JsonIgnore]
    public IEnumerable<FileInfo> QueuedVideosInfo => new List<FileInfo>(QueuedVideos.Select(x => new FileInfo(x)));

    /// <summary>
    /// The list of files still left to be compressed
    /// </summary>
    public ObservableCollection<string> QueuedAudios { get; }

    [JsonIgnore]
    public IEnumerable<FileInfo> QueuedAudiosInfo => new List<FileInfo>(QueuedAudios.Select(x => new FileInfo(x)));

    /// <summary>
    /// The path to ffmpeg.exe
    /// </summary>
    public string FFmpegPath { get; }
    /// <summary>
    /// The path to ffprobe.exe
    /// </summary>
    public string FFprobePath { get; }
    /// <summary>
    /// The path to the temporary folder
    /// </summary>
    public string TempPath { get; }
}

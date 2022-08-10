
using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace QuickCompress.Core.Configuration;

// ReSharper disable once ClassNeverInstantiated.Global
public class VideoCompressionOptions : Context
{
    public VideoCompressionOptions(ObservableCollection<string> queuedFiles, string fFmpegPath, string fFprobePath, string tempPath)
    {
        QueuedFiles = queuedFiles;
        FFmpegPath = fFmpegPath;
        FFprobePath = fFprobePath;
        TempPath = tempPath;
        QueuedFiles.CollectionChanged += (_, _) => SaveOptions();
    }

    /// <summary>
    /// The list of files still left to be compressed
    /// </summary>
    public ObservableCollection<string> QueuedFiles { get; set; }

    [JsonIgnore]
    public IEnumerable<FileInfo> QueuedFilesInfo => new List<FileInfo>(QueuedFiles.Select(x => new FileInfo(x))) ;

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

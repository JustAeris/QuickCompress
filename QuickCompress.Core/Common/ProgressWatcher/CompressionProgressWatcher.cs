using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace QuickCompress.Core.Common.ProgressWatcher;

/// <summary>
/// <see cref="IProgressWatcher{T}"/> to watch for progression on video compression
/// </summary>
internal class CompressionProgressWatcher : IProgressWatcher<double>
{
    private int TotalFrames { get; }
    public StringBuilder StandardLog { get; }
    public StringBuilder ErrorLog { get; } = new();
    private readonly Action<double> _onProgress;

    public CompressionProgressWatcher(Action<double> onProgress, int totalFrames)
    {
        TotalFrames = totalFrames;
        _onProgress = onProgress;
        StandardLog = new StringBuilder();
    }

    Action<double> IProgressWatcher<double>.OnProgress => _onProgress;

    DataReceivedEventHandler? IProgressWatcher<double>.OnOutputReceived => null;

    DataReceivedEventHandler IProgressWatcher<double>.OnErrorReceived => (_, e) =>
    {
        if (e.Data == null) return;
        StandardLog.AppendLine(e.Data);
        var match = Regex.Match(e.Data, @"frame=\s*([0-9]*)");
        var progress = match.Success ? double.Parse(match.Groups[1].Value) * 100 / TotalFrames : 0;
        ((IProgressWatcher<double>)this).OnProgress.Invoke(progress);
    };
}

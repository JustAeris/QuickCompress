using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace QuickCompress.Core.Common.ProgressWatcher;

/// <summary>
/// <see cref="IProgressWatcher{T}"/> to watch for progression when resizing a video file, includes the pass the program is currently on.
/// </summary>
internal class SetVideoBitrateProgressWatcher : IProgressWatcher<(double Progress, int Pass)>
{
    private int TotalFrames { get; }
    public StringBuilder StandardLog { get; }
    public StringBuilder ErrorLog { get; } = new();
    private readonly Action<(double Progress, int Pass)> _onProgress;

    private int Pass { get; }

    internal SetVideoBitrateProgressWatcher(Action<(double Progress, int Pass)> onProgress, int totalFrames, int pass)
    {
        _onProgress = onProgress;
        TotalFrames = totalFrames;
        Pass = pass;
        StandardLog = new StringBuilder();
    }

    Action<(double Progress, int Pass)> IProgressWatcher<(double Progress, int Pass)>.OnProgress => _onProgress;

    DataReceivedEventHandler? IProgressWatcher<(double Progress, int Pass)>.OnOutputReceived => null;

    DataReceivedEventHandler IProgressWatcher<(double Progress, int Pass)>.OnErrorReceived => (_, e) =>
    {
        if (e.Data == null) return;
        StandardLog.AppendLine(e.Data);
        var match = Regex.Match(e.Data, @"frame=\s*([0-9]*)");
        var progress = match.Success ? double.Parse(match.Groups[1].Value) * 100 / TotalFrames : -1;
        ((IProgressWatcher<(double Progress, int Pass)>)this).OnProgress.Invoke((progress, Pass));
    };
}

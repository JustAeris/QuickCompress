using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace QuickCompress.Core.Common.ProgressWatcher;

public class SetAudioBitrateProgressWatcher : IProgressWatcher<double>
{
    private double TargetSize { get; }
    public StringBuilder StandardLog { get; }
    public StringBuilder ErrorLog { get; } = new();
    private readonly Action<double> _onProgress;

    public SetAudioBitrateProgressWatcher(Action<double> onProgress, double targetSize)
    {
        TargetSize = targetSize;
        _onProgress = onProgress;
        StandardLog = new StringBuilder();
    }

    Action<double> IProgressWatcher<double>.OnProgress => _onProgress;

    DataReceivedEventHandler? IProgressWatcher<double>.OnOutputReceived => null;

    DataReceivedEventHandler IProgressWatcher<double>.OnErrorReceived => (_, e) =>
    {
        if (e.Data == null) return;
        StandardLog.AppendLine(e.Data);
        var match = Regex.Match(e.Data, @"size=\s*([0-9]*)");
        var progress = Math.Min(match.Success ? double.Parse(match.Groups[1].Value) * 1000 / TargetSize : 0, 1D) ;
        ((IProgressWatcher<double>)this).OnProgress.Invoke(progress);
    };
}

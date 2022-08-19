using System.Diagnostics;
using System.Text;

namespace QuickCompress.Core.Common.ProgressWatcher;

/// <summary>
/// Base interface of a progress watcher
/// </summary>
/// <typeparam name="T">The input type of the action you're going to accept</typeparam>
internal interface IProgressWatcher<in T>
{
    /// <summary>
    /// The action you can fire when the progress changes
    /// </summary>
    internal Action<T> OnProgress { get; }

    /// <summary>
    /// This is where you can log the content of the process (from <see cref="OnOutputReceived"/>) in case you need it afterwards.
    /// </summary>
    public StringBuilder StandardLog { get; }

    /// <summary>
    /// Another place where you can log the content of the process (from <see cref="OnErrorReceived"/>) in case you need it afterwards.
    /// </summary>
    public StringBuilder ErrorLog { get; }

    /// <summary>
    /// Event handler to contain the logic for the progression. This is where you can trigger the action you've set in OnProgress.
    /// </summary>
    internal DataReceivedEventHandler? OnOutputReceived { get; }

    /// <summary>
    /// Another event handler in case you need to do something on error or the program you're using outputs to the <see cref="Process.StandardError"/> stream (lookin' at you FFmpeg...).
    /// </summary>
    internal DataReceivedEventHandler OnErrorReceived { get; }
}

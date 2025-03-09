#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui;

/// <summary>
///     Interface for main loop that runs the core Terminal.Gui UI loop.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IMainLoop<T> : IDisposable
{
    /// <summary>
    ///     Gets the class responsible for servicing user timeouts and idles
    /// </summary>
    public ITimedEvents TimedEvents { get; }

    /// <summary>
    ///     Gets the class responsible for writing final rendered output to the console
    /// </summary>
    public IOutputBuffer OutputBuffer { get; }

    /// <summary>
    ///     Class for writing output to the console.
    /// </summary>
    public IConsoleOutput Out { get; }

    /// <summary>
    ///     Gets the class responsible for processing buffered console input and translating
    ///     it into events on the UI thread.
    /// </summary>
    public IInputProcessor InputProcessor { get; }

    /// <summary>
    ///     Gets the class responsible for sending ANSI escape requests which expect a response
    ///     from the remote terminal e.g. Device Attribute Request
    /// </summary>
    public AnsiRequestScheduler AnsiRequestScheduler { get; }

    /// <summary>
    ///     Gets the class responsible for determining the current console size
    /// </summary>
    public IWindowSizeMonitor WindowSizeMonitor { get; }

    /// <summary>
    ///     Initializes the loop with a buffer from which data can be read
    /// </summary>
    /// <param name="timedEvents"></param>
    /// <param name="inputBuffer"></param>
    /// <param name="inputProcessor"></param>
    /// <param name="consoleOutput"></param>
    void Initialize (ITimedEvents timedEvents, ConcurrentQueue<T> inputBuffer, IInputProcessor inputProcessor, IConsoleOutput consoleOutput);

    /// <summary>
    ///     Perform a single iteration of the main loop then blocks for a fixed length
    ///     of time, this method is designed to be run in a loop.
    /// </summary>
    public void Iteration ();
}

#nullable enable
using System.Collections.Concurrent;

namespace Terminal.Gui.App;

/// <summary>
///     Interface for the main application loop that runs the core Terminal.Gui UI rendering and event processing.
/// </summary>
/// <remarks>
///     This interface defines the contract for the main loop that coordinates:
///     <list type="bullet">
///         <item>Processing input events from the console</item>
///         <item>Running user timeout callbacks</item>
///         <item>Detecting UI changes that need redrawing</item>
///         <item>Rendering UI updates to the console</item>
///     </list>
/// </remarks>
/// <typeparam name="T">Type of raw input events processed by the loop, e.g. <see cref="ConsoleKeyInfo"/> for cross-platform .NET driver</typeparam>
public interface IApplicationMainLoop<T> : IDisposable
{
    /// <summary>
    ///     Gets the class responsible for servicing user timeouts
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
    /// <param name="componentFactory"></param>
    void Initialize (
        ITimedEvents timedEvents,
        ConcurrentQueue<T> inputBuffer,
        IInputProcessor inputProcessor,
        IConsoleOutput consoleOutput,
        IComponentFactory<T> componentFactory
    );

    /// <summary>
    ///     Perform a single iteration of the main loop then blocks for a fixed length
    ///     of time, this method is designed to be run in a loop.
    /// </summary>
    public void Iteration ();
}

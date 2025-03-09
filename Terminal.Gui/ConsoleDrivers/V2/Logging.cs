using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Terminal.Gui;

/// <summary>
///     Singleton logging instance class. Do not use console loggers
///     with this class as it will interfere with Terminal.Gui
///     screen output (i.e. use a file logger).
/// </summary>
/// <remarks>
///     Also contains the
///     <see cref="Meter"/> instance that should be used for internal metrics
///     (iteration timing etc.).
/// </remarks>
public static class Logging
{
    /// <summary>
    ///     Logger, defaults to NullLogger (i.e. no logging).  Set this to a
    ///     file logger to enable logging of Terminal.Gui internals.
    /// </summary>
    public static ILogger Logger { get; set; } = NullLogger.Instance;

    /// <summary>
    ///     Metrics reporting meter for internal Terminal.Gui processes. To use
    ///     create your own static instrument e.g. CreateCounter, CreateHistogram etc
    /// </summary>
    internal static readonly Meter Meter = new ("Terminal.Gui");

    /// <summary>
    ///     Metric for how long it takes each full iteration of the main loop to occur
    /// </summary>
    public static readonly Histogram<int> TotalIterationMetric = Meter.CreateHistogram<int> ("Iteration (ms)");

    /// <summary>
    ///     Metric for how long it took to do the 'timeouts and invokes' section of main loop.
    /// </summary>
    public static readonly Histogram<int> IterationInvokesAndTimeouts = Meter.CreateHistogram<int> ("Invokes & Timers (ms)");

    /// <summary>
    ///     Counter for when we redraw, helps detect situations e.g. where we are repainting entire UI every loop
    /// </summary>
    public static readonly Counter<int> Redraws = Meter.CreateCounter<int> ("Redraws");

    /// <summary>
    ///     Metric for how long it takes to read all available input from the input stream - at which
    ///     point input loop will sleep.
    /// </summary>
    public static readonly Histogram<int> DrainInputStream = Meter.CreateHistogram<int> ("Drain Input (ms)");

    /// <summary>
    ///     Logs an error message including the class and method name.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="caller"></param>
    /// <param name="filePath"></param>
    public static void Error (
        string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = ""
    )
    {
        string className = Path.GetFileNameWithoutExtension (filePath);
        Logger.LogError ($"[{className}] [{caller}] {message}");
    }

    /// <summary>
    ///     Logs a fatal/critical message including the class and method name.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="caller"></param>
    /// <param name="filePath"></param>
    public static void Critical (
        string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = ""
    )
    {
        string className = Path.GetFileNameWithoutExtension (filePath);
        Logger.LogCritical ($"[{className}] [{caller}] {message}");
    }

    /// <summary>
    ///     Logs a debug message including the class and method name.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="caller"></param>
    /// <param name="filePath"></param>
    public static void Debug (
        string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = ""
    )
    {
        string className = Path.GetFileNameWithoutExtension (filePath);
        Logger.LogDebug ($"[{className}] [{caller}] {message}");
    }

    /// <summary>
    ///     Logs an informational message including the class and method name.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="caller"></param>
    /// <param name="filePath"></param>
    public static void Information (
        string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = ""
    )
    {
        string className = Path.GetFileNameWithoutExtension (filePath);
        Logger.LogInformation ($"[{className}] [{caller}] {message}");
    }

    /// <summary>
    ///     Logs a trace/verbose message including the class and method name.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="caller"></param>
    /// <param name="filePath"></param>
    public static void Trace (
        string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = ""
    )
    {
        string className = Path.GetFileNameWithoutExtension (filePath);
        Logger.LogTrace ($"[{className}] [{caller}] {message}");
    }

    /// <summary>
    ///     Logs a warning message including the class and method name.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="caller"></param>
    /// <param name="filePath"></param>
    public static void Warning (
        string message,
        [CallerMemberName] string caller = "",
        [CallerFilePath] string filePath = ""
    )
    {
        string className = Path.GetFileNameWithoutExtension (filePath);
        Logger.LogWarning ($"[{className}] [{caller}] {message}");
    }
}

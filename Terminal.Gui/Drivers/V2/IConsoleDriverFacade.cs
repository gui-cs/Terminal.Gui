namespace Terminal.Gui.Drivers;

/// <summary>
///     Interface for v2 driver abstraction layer
/// </summary>
public interface IConsoleDriverFacade
{
    /// <summary>
    ///     Class responsible for processing native driver input objects
    ///     e.g. <see cref="ConsoleKeyInfo"/> into <see cref="Key"/> events
    ///     and detecting and processing ansi escape sequences.
    /// </summary>
    IInputProcessor InputProcessor { get; }

    /// <summary>
    ///     Describes the desired screen state. Data source for <see cref="IConsoleOutput"/>.
    /// </summary>
    IOutputBuffer OutputBuffer { get; }

    /// <summary>
    ///     Interface for classes responsible for reporting the current
    ///     size of the terminal window.
    /// </summary>
    IWindowSizeMonitor WindowSizeMonitor { get; }
}

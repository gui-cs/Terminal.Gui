namespace Terminal.Gui;

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
    public IInputProcessor InputProcessor { get; }
}

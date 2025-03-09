namespace Terminal.Gui;

/// <summary>
///     Interface for writing console output
/// </summary>
public interface IConsoleOutput : IDisposable
{
    /// <summary>
    ///     Writes the given text directly to the console. Use to send
    ///     ansi escape codes etc. Regular screen output should use the
    ///     <see cref="IOutputBuffer"/> overload.
    /// </summary>
    /// <param name="text"></param>
    void Write (string text);

    /// <summary>
    ///     Write the contents of the <paramref name="buffer"/> to the console
    /// </summary>
    /// <param name="buffer"></param>
    void Write (IOutputBuffer buffer);

    /// <summary>
    ///     Returns the current size of the console window in rows/columns (i.e.
    ///     of characters not pixels).
    /// </summary>
    /// <returns></returns>
    public Size GetWindowSize ();

    /// <summary>
    ///     Updates the console cursor (the blinking underscore) to be hidden,
    ///     visible etc.
    /// </summary>
    /// <param name="visibility"></param>
    void SetCursorVisibility (CursorVisibility visibility);

    /// <summary>
    ///     Moves the console cursor to the given location.
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    void SetCursorPosition (int col, int row);
}

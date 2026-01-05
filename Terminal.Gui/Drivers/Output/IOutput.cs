using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     The low-level interface drivers implement to provide output capabilities; encapsulates platform-specific
///     output functionality.
/// </summary>
public interface IOutput : IDisposable
{
    /// <seealso cref="IDriver.Force16Colors"/>
    bool Force16Colors { get; set; }

    /// <seealso cref="IDriver.IsLegacyConsole"/>
    bool IsLegacyConsole { get; set; }

    /// <seealso cref="IDriver.GetSixels"/>
    ConcurrentQueue<SixelToRender> GetSixels ();

    /// <summary>
    ///     Returns the current size of the console in rows/columns (i.e.
    ///     of characters not pixels).
    /// </summary>
    /// <returns></returns>
    Size GetSize ();

    /// <summary>
    ///     Sets the size of the console.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    void SetSize (int width, int height);

    /// <summary>
    ///     Gets the current position of the console cursor.
    /// </summary>
    /// <returns></returns>
    Point GetCursorPosition ();

    /// <summary>
    ///     Moves the console cursor to the given location.
    /// </summary>
    /// <param name="col"></param>
    /// <param name="row"></param>
    void SetCursorPosition (int col, int row);

    /// <summary>
    ///     Updates the console cursor to be hidden, visible etc.
    /// </summary>
    /// <param name="visibility"></param>
    void SetCursorVisibility (CursorVisibility visibility);

    /// <summary>
    ///     Gets the visibility of the console cursor.
    /// </summary>
    /// <remarks>
    ///     There is no standard ANSI sequence for querying cursor visibility state. As a result, most drivers
    ///     track visibility state.
    /// </remarks>
    /// <returns>The current cursor visibility</returns>
    CursorVisibility GetCursorVisibility ();

    /// <summary>
    ///     Writes the given text directly to the console. Use to send
    ///     ansi escape codes etc. Regular screen output should use the
    ///     <see cref="IOutputBuffer"/> overload.
    /// </summary>
    /// <param name="text"></param>
    void Write (ReadOnlySpan<char> text);

    /// <summary>
    ///     Write the contents of the <paramref name="buffer"/> to the console
    /// </summary>
    /// <param name="buffer"></param>
    void Write (IOutputBuffer buffer);

    /// <summary>
    ///     Gets a string containing the ANSI escape sequences and content most recently written
    ///     to the terminal via <see cref="Write(IOutputBuffer)"/>
    /// </summary>
    string GetLastOutput ();

    /// <summary>
    ///     Generates an ANSI escape sequence string representation of the given <paramref name="buffer"/> contents.
    ///     This is the same output that would be written to the terminal to recreate the current screen contents.
    /// </summary>
    /// <param name="buffer">The output buffer to convert to ANSI.</param>
    /// <returns>A string containing ANSI escape sequences representing the buffer contents.</returns>
    string ToAnsi (IOutputBuffer buffer);
}

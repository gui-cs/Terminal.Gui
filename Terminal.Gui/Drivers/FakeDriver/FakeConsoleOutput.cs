#nullable enable
namespace Terminal.Gui.Drivers;

/// <summary>
///     Fake console output for testing that captures what would be written to the console.
/// </summary>
public class FakeConsoleOutput : OutputBase, IConsoleOutput
{
    private readonly StringBuilder _output = new ();
    private int _cursorLeft;
    private int _cursorTop;
    private Size _consoleSize = new (80, 25);

    /// <summary>
    ///     Gets the captured output as a string.
    /// </summary>
    public string Output => _output.ToString ();

    /// <inheritdoc/>
    public void SetCursorPosition (int col, int row) { SetCursorPositionImpl (col, row); }

    /// <inheritdoc />
    public void SetSize (int width, int height)
    {
        _consoleSize = new (width, height);
    }

    /// <inheritdoc/>
    protected override bool SetCursorPositionImpl (int col, int row)
    {
        _cursorLeft = col;
        _cursorTop = row;

        return true;
    }

    /// <summary>
    ///     Sets the fake window size.
    /// </summary>
    public void SetConsoleSize (int width, int height) { _consoleSize = new (width, height); }

    /// <summary>
    ///     Gets the current cursor position.
    /// </summary>
    public (int left, int top) GetCursorPosition () { return (_cursorLeft, _cursorTop); }

    /// <inheritdoc/>
    public Size GetSize () { return _consoleSize; }

    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text) { _output.Append (text); }

    /// <inheritdoc/>
    public override void SetCursorVisibility (CursorVisibility visibility)
    {
        // Capture but don't act on it in fake output
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        // Nothing to dispose
    }

    /// <inheritdoc/>
    protected override void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle)
    {
        // For testing, we can skip the actual color/style output
        // or capture it if needed for verification
    }

    /// <inheritdoc/>
    protected override void Write (StringBuilder output) { _output.Append (output); }
}

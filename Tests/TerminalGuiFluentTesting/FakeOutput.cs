using System.Drawing;

namespace TerminalGuiFluentTesting;

internal class FakeOutput : IConsoleOutput
{
    public IOutputBuffer LastBuffer { get; set; }
    public Size Size { get; set; }

    /// <inheritdoc/>
    public void Dispose () { }

    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text) { }

    /// <inheritdoc/>
    public void Write (IOutputBuffer buffer) { LastBuffer = buffer; }

    /// <inheritdoc/>
    public Size GetWindowSize () { return Size; }

    /// <inheritdoc/>
    public void SetCursorVisibility (CursorVisibility visibility) { }

    /// <inheritdoc/>
    public void SetCursorPosition (int col, int row) { CursorPosition = new Point (col, row); }

    /// <summary>
    /// The last value set by calling <see cref="SetCursorPosition"/>
    /// </summary>
    public Point CursorPosition { get; private set; }
}

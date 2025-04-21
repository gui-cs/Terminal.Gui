using System.Drawing;
using Terminal.Gui;

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
    public void SetCursorPosition (int col, int row) { }
}

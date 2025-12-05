using System;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Fake console output for testing that captures what would be written to the console.
/// </summary>
public class FakeOutput : OutputBase, IOutput
{
    private readonly StringBuilder _output = new ();
    private int _cursorLeft;
    private int _cursorTop;
    private Size _consoleSize = new (80, 25);

    /// <summary>
    /// 
    /// </summary>
    public FakeOutput ()
    {
        LastBuffer = new OutputBufferImpl ();
        LastBuffer.SetSize (80, 25);
        IsVirtualTerminal = true;
    }

    /// <summary>
    ///     Gets or sets the last output buffer written.
    /// </summary>
    public IOutputBuffer? LastBuffer { get; set; }

    /// <summary>
    ///     Gets the captured output as a string.
    /// </summary>
    public string Output => _output.ToString ();
    
    /// <inheritdoc />
    public Point GetCursorPosition ()
    {
        return new (_cursorLeft, _cursorTop);
    }

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

    /// <inheritdoc/>
    public Size GetSize () { return _consoleSize; }

    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text)
    {
        _output.Append (text);
    }

    /// <inheritdoc cref="IDriver"/>
    public override void Write (IOutputBuffer buffer)
    {
        LastBuffer = buffer;
        base.Write (buffer);
    }

    /// <inheritdoc cref="IDriver"/>
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
        if (Force16Colors)
        {
            if (IsVirtualTerminal)
            {
                output.Append (EscSeqUtils.CSI_SetForegroundColor (attr.Foreground.GetAnsiColorCode ()));
                output.Append (EscSeqUtils.CSI_SetBackgroundColor (attr.Background.GetAnsiColorCode ()));

                EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
            }
            else
            {
                Write (output);
                Console.ForegroundColor = (ConsoleColor)attr.Foreground.GetClosestNamedColor16 ();
                Console.BackgroundColor = (ConsoleColor)attr.Background.GetClosestNamedColor16 ();
            }
        }
        else
        {
            EscSeqUtils.CSI_AppendForegroundColorRGB (
                                                      output,
                                                      attr.Foreground.R,
                                                      attr.Foreground.G,
                                                      attr.Foreground.B
                                                     );

            EscSeqUtils.CSI_AppendBackgroundColorRGB (
                                                      output,
                                                      attr.Background.R,
                                                      attr.Background.G,
                                                      attr.Background.B
                                                     );

            EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
        }
    }

    /// <inheritdoc/>
    protected override void Write (StringBuilder output)
    {
        _output.Append (output);
    }
}

#nullable enable
namespace Terminal.Gui.Drivers;

/// <summary>
///     Abstract base class to assist with implementing <see cref="IOutput"/>.
/// </summary>
public abstract class OutputBase
{
    private CursorVisibility? _cachedCursorVisibility;

    // Last text style used, for updating style with EscSeqUtils.CSI_AppendTextStyleChange().
    private TextStyle _redrawTextStyle = TextStyle.None;

    /// <summary>
    ///     Changes the visibility of the cursor in the terminal to the specified <paramref name="visibility"/> e.g.
    ///     the flashing indicator, invisible, box indicator etc.
    /// </summary>
    /// <param name="visibility"></param>
    public abstract void SetCursorVisibility (CursorVisibility visibility);

    /// <inheritdoc cref="IOutput.Write(IOutputBuffer)"/>
    public virtual void Write (IOutputBuffer buffer)
    {
        var top = 0;
        var left = 0;
        int rows = buffer.Rows;
        int cols = buffer.Cols;
        var output = new StringBuilder ();
        Attribute? redrawAttr = null;
        int lastCol = -1;

        CursorVisibility? savedVisibility = _cachedCursorVisibility;
        SetCursorVisibility (CursorVisibility.Invisible);

        for (int row = top; row < rows; row++)
        {
            if (!SetCursorPositionImpl (0, row))
            {
                return;
            }

            output.Clear ();

            for (int col = left; col < cols; col++)
            {
                lastCol = -1;
                var outputWidth = 0;

                for (; col < cols; col++)
                {
                    if (!buffer.Contents! [row, col].IsDirty)
                    {
                        if (output.Length > 0)
                        {
                            WriteToConsole (output, ref lastCol, row, ref outputWidth);
                        }
                        else if (lastCol == -1)
                        {
                            lastCol = col;
                        }

                        if (lastCol + 1 < cols)
                        {
                            lastCol++;
                        }

                        continue;
                    }

                    if (lastCol == -1)
                    {
                        lastCol = col;
                    }

                    Attribute? attribute = buffer.Contents [row, col].Attribute;

                    if (attribute is { })
                    {
                        Attribute attr = attribute.Value;

                        // Performance: Only send the escape sequence if the attribute has changed.
                        if (attr != redrawAttr)
                        {
                            redrawAttr = attr;

                            AppendOrWriteAttribute (output, attr, _redrawTextStyle);

                            _redrawTextStyle = attr.Style;
                        }
                    }

                    outputWidth++;

                    string text = buffer.Contents [row, col].Grapheme;
                    output.Append (text);

                    buffer.Contents [row, col].IsDirty = false;
                }
            }

            if (output.Length > 0)
            {
                SetCursorPositionImpl (lastCol, row);

                // Wrap URLs with OSC 8 hyperlink sequences using the new Osc8UrlLinker
                StringBuilder processed = Osc8UrlLinker.WrapOsc8 (output);
                Write (processed);
            }
        }

        foreach (SixelToRender s in Application.Sixel)
        {
            if (!string.IsNullOrWhiteSpace (s.SixelData))
            {
                SetCursorPositionImpl (s.ScreenPosition.X, s.ScreenPosition.Y);
                Console.Out.Write (s.SixelData);
            }
        }

        SetCursorVisibility (savedVisibility ?? CursorVisibility.Default);
        _cachedCursorVisibility = savedVisibility;
    }

    /// <summary>
    ///     Changes the color and text style of the console to the given <paramref name="attr"/> and
    ///     <paramref name="redrawTextStyle"/>.
    ///     If command can be buffered in line with other output (e.g. CSI sequence) then it should be appended to
    ///     <paramref name="output"/>
    ///     otherwise the relevant output state should be flushed directly (e.g. by calling relevant win 32 API method)
    /// </summary>
    /// <param name="output"></param>
    /// <param name="attr"></param>
    /// <param name="redrawTextStyle"></param>
    protected abstract void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle);

    /// <summary>
    ///     When overriden in derived class, positions the terminal output cursor to the specified point on the screen.
    /// </summary>
    /// <param name="screenPositionX">Column to move cursor to</param>
    /// <param name="screenPositionY">Row to move cursor to</param>
    /// <returns></returns>
    protected abstract bool SetCursorPositionImpl (int screenPositionX, int screenPositionY);

    /// <summary>
    ///     Output the contents of the <paramref name="output"/> to the console.
    /// </summary>
    /// <param name="output"></param>
    protected abstract void Write (StringBuilder output);

    private void WriteToConsole (StringBuilder output, ref int lastCol, int row, ref int outputWidth)
    {
        SetCursorPositionImpl (lastCol, row);

        // Wrap URLs with OSC 8 hyperlink sequences using the new Osc8UrlLinker
        StringBuilder processed = Osc8UrlLinker.WrapOsc8 (output);
        Write (processed);

        output.Clear ();
        lastCol += outputWidth;
        outputWidth = 0;
    }
}

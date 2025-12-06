using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Abstract base class to assist with implementing <see cref="IOutput"/>.
/// </summary>
public abstract class OutputBase
{
    private bool _force16Colors;

    /// <inheritdoc cref="IOutput.Force16Colors"/>
    public bool Force16Colors
    {
        get => _force16Colors;
        set
        {
            if (IsLegacyConsole && !value)
            {
                return;
            }

            _force16Colors = value;
        }
    }

    private bool _isLegacyConsole;

    /// <inheritdoc cref="IOutput.IsLegacyConsole"/>
    public bool IsLegacyConsole
    {
        get => _isLegacyConsole;
        set
        {
            _isLegacyConsole = value;

            if (value) // If legacy console (true), force 16 colors
            {
                Force16Colors = true;
            }
        }
    }

    private readonly ConcurrentQueue<SixelToRender> _sixels = [];

    /// <inheritdoc cref="IOutput.GetSixels"/>>
    public ConcurrentQueue<SixelToRender> GetSixels () => _sixels;

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

                        if (IsLegacyConsole)
                        {
                            SetCursorPositionImpl (lastCol, row);
                        }

                        continue;
                    }

                    if (lastCol == -1)
                    {
                        lastCol = col;
                    }

                    Cell cell = buffer.Contents [row, col];
                    AppendCellAnsi (cell, output, ref redrawAttr, ref _redrawTextStyle, cols, ref col);

                    outputWidth++;

                    buffer.Contents [row, col].IsDirty = false;
                }
            }

            if (output.Length > 0)
            {
                if (IsLegacyConsole)
                {
                    Write (output);
                }
                else
                {
                    SetCursorPositionImpl (lastCol, row);

                    // Wrap URLs with OSC 8 hyperlink sequences using the new Osc8UrlLinker
                    StringBuilder processed = Osc8UrlLinker.WrapOsc8 (output);
                    Write (processed);
                }
            }
        }

        if (IsLegacyConsole)
        {
            return;
        }

        foreach (SixelToRender s in GetSixels ())
        {
            if (string.IsNullOrWhiteSpace (s.SixelData))
            {
                continue;
            }

            SetCursorPositionImpl (s.ScreenPosition.X, s.ScreenPosition.Y);
            Write ((StringBuilder)new (s.SixelData));
        }


        // DO NOT restore cursor visibility here - let ApplicationMainLoop.SetCursor() handle it
        // The old code was saving/restoring visibility which caused flickering because
        // it would restore to the old value even if the application wanted it hidden
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

    /// <summary>
    ///     Builds ANSI escape sequences for the specified rectangular region of the buffer.
    /// </summary>
    /// <param name="buffer">The output buffer to build ANSI for.</param>
    /// <param name="startRow">The starting row (inclusive).</param>
    /// <param name="endRow">The ending row (exclusive).</param>
    /// <param name="startCol">The starting column (inclusive).</param>
    /// <param name="endCol">The ending column (exclusive).</param>
    /// <param name="output">The StringBuilder to append ANSI sequences to.</param>
    /// <param name="lastAttr">The last attribute used, for optimization.</param>
    /// <param name="includeCellPredicate">Predicate to determine which cells to include. If null, includes all cells.</param>
    /// <param name="addNewlines">Whether to add newlines between rows.</param>
    protected void BuildAnsiForRegion (
        IOutputBuffer buffer,
        int startRow,
        int endRow,
        int startCol,
        int endCol,
        StringBuilder output,
        ref Attribute? lastAttr,
        Func<int, int, bool>? includeCellPredicate = null,
        bool addNewlines = true
    )
    {
        TextStyle redrawTextStyle = TextStyle.None;

        for (int row = startRow; row < endRow; row++)
        {
            for (int col = startCol; col < endCol; col++)
            {
                if (includeCellPredicate != null && !includeCellPredicate (row, col))
                {
                    continue;
                }

                Cell cell = buffer.Contents! [row, col];
                AppendCellAnsi (cell, output, ref lastAttr, ref redrawTextStyle, endCol, ref col);
            }

            // Add newline at end of row if requested
            if (addNewlines)
            {
                output.AppendLine ();
            }
        }
    }

    /// <summary>
    ///     Appends ANSI sequences for a single cell to the output.
    /// </summary>
    /// <param name="cell">The cell to append ANSI for.</param>
    /// <param name="output">The StringBuilder to append to.</param>
    /// <param name="lastAttr">The last attribute used, updated if the cell's attribute is different.</param>
    /// <param name="redrawTextStyle">The current text style for optimization.</param>
    /// <param name="maxCol">The maximum column, used for wide character handling.</param>
    /// <param name="currentCol">The current column, updated for wide characters.</param>
    protected void AppendCellAnsi (Cell cell, StringBuilder output, ref Attribute? lastAttr, ref TextStyle redrawTextStyle, int maxCol, ref int currentCol)
    {
        Attribute? attribute = cell.Attribute;

        // Add ANSI escape sequence for attribute change
        if (attribute.HasValue && attribute.Value != lastAttr)
        {
            lastAttr = attribute.Value;
            AppendOrWriteAttribute (output, attribute.Value, redrawTextStyle);
            redrawTextStyle = attribute.Value.Style;
        }

        // Add the grapheme
        string grapheme = cell.Grapheme;
        output.Append (grapheme);

        // Handle wide grapheme
        if (grapheme.GetColumns () > 1 && currentCol + 1 < maxCol)
        {
            currentCol++; // Skip next cell for wide character
        }
    }

    /// <summary>
    ///     Generates an ANSI escape sequence string representation of the given <paramref name="buffer"/> contents.
    ///     This is the same output that would be written to the terminal to recreate the current screen contents.
    /// </summary>
    /// <param name="buffer">The output buffer to convert to ANSI.</param>
    /// <returns>A string containing ANSI escape sequences representing the buffer contents.</returns>
    public string ToAnsi (IOutputBuffer buffer)
    {
        var output = new StringBuilder ();
        Attribute? lastAttr = null;

        BuildAnsiForRegion (buffer, 0, buffer.Rows, 0, buffer.Cols, output, ref lastAttr);

        return output.ToString ();
    }

    private void WriteToConsole (StringBuilder output, ref int lastCol, int row, ref int outputWidth)
    {
        if (IsLegacyConsole)
        {
            Write (output);
        }
        else
        {
            SetCursorPositionImpl (lastCol, row);

            // Wrap URLs with OSC 8 hyperlink sequences using the new Osc8UrlLinker
            StringBuilder processed = Osc8UrlLinker.WrapOsc8 (output);
            Write (processed);
        }

        output.Clear ();
        lastCol += outputWidth;
        outputWidth = 0;
    }
}

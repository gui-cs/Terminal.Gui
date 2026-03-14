using System.Collections.Concurrent;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Abstract base class to assist with implementing <see cref="IOutput"/>.
/// </summary>
public abstract class OutputBase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OutputBase"/> class and detects whether the output is attached to a real terminal device.
    /// </summary>
    protected OutputBase () => IsAttachedToTerminal = Driver.IsAttachedToTerminal (out _, out _);

    /// <summary>
    ///     Gets whether this output instance is attached to a real terminal device.
    /// </summary>
    protected bool IsAttachedToTerminal { get; }

    /// <inheritdoc cref="IOutput.Force16Colors"/>
    public bool Force16Colors
    {
        get;
        set
        {
            if (IsLegacyConsole && !value)
            {
                return;
            }

            field = value;
        }
    }

    /// <inheritdoc cref="IOutput.IsLegacyConsole"/>
    public bool IsLegacyConsole
    {
        get;
        set
        {
            field = value;

            if (value) // If legacy console (true), force 16 colors
            {
                Force16Colors = true;
            }
        }
    }

    private readonly ConcurrentQueue<SixelToRender> _sixels = [];

    /// <inheritdoc cref="IOutput.GetSixels"/>
    public ConcurrentQueue<SixelToRender> GetSixels () => _sixels;

    // Last text style used, for updating style with EscSeqUtils.CSI_AppendTextStyleChange().
    private TextStyle _redrawTextStyle = TextStyle.None;

    // Last URL used for tracking hyperlink state
    private string? _lastUrl = null;

    private readonly StringBuilder _lastOutputStringBuilder = new ();
    private bool _clearLastOutputPending;

    /// <summary>
    ///     Writes dirty cells from the buffer to the console. Hides cursor, iterates rows/cols,
    ///     skips clean cells, batches dirty cells into ANSI sequences, wraps URLs with OSC 8,
    ///     then renders sixel images. Cursor visibility is managed by <c>ApplicationMainLoop.SetCursor()</c>.
    /// </summary>
    public virtual void Write (IOutputBuffer buffer)
    {
        _clearLastOutputPending = true;
        StringBuilder outputStringBuilder = new ();
        var top = 0;
        var left = 0;
        int rows = buffer.Rows;
        int cols = buffer.Cols;
        Attribute? redrawAttr = null;
        int lastCol = -1;

        // Process each row
        for (int row = top; row < rows; row++)
        {
            if (!SetCursorPositionImpl (0, row))
            {
                return;
            }

            outputStringBuilder.Clear ();
            _lastUrl = null; // Reset URL state at the start of each row

            // Process columns in row
            for (int col = left; col < cols; col++)
            {
                lastCol = -1;
                var outputWidth = 0;

                // Batch consecutive dirty cells
                for (; col < cols; col++)
                {
                    // Skip clean cells - position cursor and continue
                    if (!buffer.Contents! [row, col].IsDirty)
                    {
                        if (outputStringBuilder.Length > 0)
                        {
                            // This clears outputStringBuilder
                            WriteToConsole (outputStringBuilder, ref lastCol, ref outputWidth);
                        }
                        else if (lastCol == -1)
                        {
                            lastCol = col;
                        }

                        if (lastCol + 1 < cols)
                        {
                            lastCol++;
                        }

                        SetCursorPositionImpl (lastCol, row);

                        continue;
                    }

                    if (lastCol == -1)
                    {
                        lastCol = col;
                    }

                    // Handle URL hyperlink state changes
                    if (!IsLegacyConsole)
                    {
                        string? cellUrl = buffer.GetCellUrl (col, row);

                        if (cellUrl != _lastUrl)
                        {
                            // If we were in a hyperlink, end it
                            if (_lastUrl is { })
                            {
                                outputStringBuilder.Append (EscSeqUtils.OSC_EndHyperlink ());
                            }

                            // If starting a new hyperlink, begin it
                            if (!string.IsNullOrEmpty (cellUrl))
                            {
                                outputStringBuilder.Append (EscSeqUtils.OSC_StartHyperlink (cellUrl));
                            }

                            _lastUrl = cellUrl;
                        }
                    }

                    // Append dirty cell as ANSI and mark clean
                    Cell cell = buffer.Contents [row, col];
                    buffer.Contents [row, col].IsDirty = false;
                    AppendCellAnsi (cell, outputStringBuilder, ref redrawAttr, ref _redrawTextStyle, cols, ref col, ref outputWidth);

                    if (col != lastCol)
                    {
                        // Was a wide grapheme so mark clean next cell
                        // See https://github.com/gui-cs/Terminal.Gui/issues/4466
                        buffer.Contents [row, col].IsDirty = false;
                    }
                }
            }

            // Flush buffered output for row
            if (outputStringBuilder.Length <= 0)
            {
                continue;
            }

            if (IsLegacyConsole)
            {
                Write (outputStringBuilder);
            }
            else
            {
                SetCursorPositionImpl (lastCol, row);

                // Close any open hyperlink before processing URLs
                if (_lastUrl is { })
                {
                    outputStringBuilder.Append (EscSeqUtils.OSC_EndHyperlink ());
                    _lastUrl = null;
                }

                // Wrap URLs with OSC 8 hyperlink sequences
                StringBuilder processed = Osc8UrlLinker.WrapOsc8 (outputStringBuilder);
                Write (processed);
            }
        }

        if (IsLegacyConsole)
        {
            return;
        }

        // Render queued sixel images
        foreach (SixelToRender s in GetSixels ())
        {
            if (string.IsNullOrWhiteSpace (s.SixelData))
            {
                continue;
            }

            SetCursorPositionImpl (s.ScreenPosition.X, s.ScreenPosition.Y);
            Write (new StringBuilder (s.SixelData));
        }
    }

    /// <inheritdoc cref="IOutput.GetLastOutput"/>
    public virtual string GetLastOutput () => _lastOutputStringBuilder.ToString ();

    /// <summary>
    ///     Changes the color and text style of the console to the given <paramref name="attr"/> and
    ///     <paramref name="redrawTextStyle"/>.
    ///     If command can be buffered in line with other output (e.g. CSI sequence) then it should be appended to
    ///     <paramref name="output"/>
    ///     otherwise the relevant output state should be flushed directly (e.g. by calling relevant win 32 API method).
    ///     <para>
    ///         When a color is <see cref="Color.None"/> (alpha = 0), the terminal's default foreground or
    ///         background color is used via ANSI reset sequences (CSI 39m / CSI 49m), allowing native terminal
    ///         transparency to show through.
    ///     </para>
    /// </summary>
    /// <param name="output"></param>
    /// <param name="attr"></param>
    /// <param name="redrawTextStyle"></param>
    protected virtual void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle)
    {
        if (attr.Foreground == Color.None)
        {
            EscSeqUtils.CSI_AppendResetForegroundColor (output);
        }
        else if (Force16Colors)
        {
            output.Append (EscSeqUtils.CSI_SetForegroundColor (attr.Foreground.GetAnsiColorCode ()));
        }
        else
        {
            EscSeqUtils.CSI_AppendForegroundColorRGB (output, attr.Foreground.R, attr.Foreground.G, attr.Foreground.B);
        }

        if (attr.Background == Color.None)
        {
            EscSeqUtils.CSI_AppendResetBackgroundColor (output);
        }
        else if (Force16Colors)
        {
            output.Append (EscSeqUtils.CSI_SetBackgroundColor (attr.Background.GetAnsiColorCode ()));
        }
        else
        {
            EscSeqUtils.CSI_AppendBackgroundColorRGB (output, attr.Background.R, attr.Background.G, attr.Background.B);
        }

        EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
    }

    /// <summary>
    ///     When overriden in derived class, positions the terminal draw cursor to the specified point on the screen.
    ///     Note, this does NOT update any internal cursor position state - that is the responsibility of the caller.
    /// </summary>
    /// <param name="screenPositionX">Column to move cursor to</param>
    /// <param name="screenPositionY">Row to move cursor to</param>
    /// <returns></returns>
    protected abstract bool SetCursorPositionImpl (int screenPositionX, int screenPositionY);

    /// <summary>
    ///     Output the contents of the <paramref name="output"/> to the console.
    /// </summary>
    /// <param name="output"></param>
    protected virtual void Write (StringBuilder output)
    {
        if (_clearLastOutputPending)
        {
            _lastOutputStringBuilder.Clear ();
            _clearLastOutputPending = false;
        }

        _lastOutputStringBuilder.Append (output);
    }

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
    protected void BuildAnsiForRegion (IOutputBuffer buffer,
                                       int startRow,
                                       int endRow,
                                       int startCol,
                                       int endCol,
                                       StringBuilder output,
                                       ref Attribute? lastAttr,
                                       Func<int, int, bool>? includeCellPredicate = null,
                                       bool addNewlines = true)
    {
        var redrawTextStyle = TextStyle.None;

        for (int row = startRow; row < endRow; row++)
        {
            for (int col = startCol; col < endCol; col++)
            {
                if (includeCellPredicate != null && !includeCellPredicate (row, col))
                {
                    continue;
                }

                Cell cell = buffer.Contents! [row, col];
                int outputWidth = -1;
                AppendCellAnsi (cell, output, ref lastAttr, ref redrawTextStyle, endCol, ref col, ref outputWidth);
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
    /// <param name="outputWidth">The current output width, updated for wide characters.</param>
    protected void AppendCellAnsi (Cell cell,
                                   StringBuilder output,
                                   ref Attribute? lastAttr,
                                   ref TextStyle redrawTextStyle,
                                   int maxCol,
                                   ref int currentCol,
                                   ref int outputWidth)
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
        outputWidth++;

        // Handle wide grapheme
        if (grapheme.GetColumns () <= 1 || currentCol + 1 >= maxCol)
        {
            return;
        }
        currentCol++; // Skip next cell for wide character
        outputWidth++;
    }

    /// <summary>
    ///     Generates an ANSI escape sequence string representation of the given <paramref name="buffer"/> contents.
    ///     This is the same output that would be written to the terminal to recreate the current screen contents.
    /// </summary>
    /// <param name="buffer">The output buffer to convert to ANSI.</param>
    /// <returns>A string containing ANSI escape sequences representing the buffer contents.</returns>
    public string ToAnsi (IOutputBuffer buffer)
    {
        // Legacy consoles don't support ANSI escape sequences
        // Return plain text representation instead
        if (IsLegacyConsole)
        {
            StringBuilder output = new ();

            for (var row = 0; row < buffer.Rows; row++)
            {
                for (var col = 0; col < buffer.Cols; col++)
                {
                    Cell cell = buffer.Contents! [row, col];
                    string grapheme = cell.Grapheme;
                    output.Append (grapheme);

                    // Handle wide grapheme
                    if (grapheme.GetColumns () > 1 && col + 1 < buffer.Cols)
                    {
                        col++; // Skip next cell for wide character
                    }
                }

                output.AppendLine ();
            }

            return output.ToString ();
        }

        StringBuilder ansiOutput = new ();
        Attribute? lastAttr = null;

        BuildAnsiForRegion (buffer, 0, buffer.Rows, 0, buffer.Cols, ansiOutput, ref lastAttr);

        return ansiOutput.ToString ();
    }

    /// <summary>
    ///     Writes buffered output to console, wrapping URLs with OSC 8 hyperlinks (non-legacy only),
    ///     then clears the buffer and advances <paramref name="lastCol"/> by <paramref name="outputWidth"/>.
    /// </summary>
    private void WriteToConsole (StringBuilder output, ref int lastCol, ref int outputWidth)
    {
        if (IsLegacyConsole)
        {
            Write (output);
        }
        else
        {
            // Wrap URLs with OSC 8 hyperlink sequences
            StringBuilder processed = Osc8UrlLinker.WrapOsc8 (output);
            Write (processed);
        }

        output.Clear ();
        lastCol += outputWidth;
        outputWidth = 0;
    }
}

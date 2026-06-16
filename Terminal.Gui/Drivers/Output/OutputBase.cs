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

    /// <inheritdoc cref="IOutput.UseKittyGraphics"/>
    public bool UseKittyGraphics { get; set; }

    private readonly ConcurrentQueue<SixelToRender> _sixels = [];

    /// <inheritdoc cref="IOutput.GetSixels"/>
    public ConcurrentQueue<SixelToRender> GetSixels () => _sixels;

    // Last text style used, for updating style with EscSeqUtils.CSI_AppendTextStyleChange().
    private TextStyle _redrawTextStyle = TextStyle.None;

    // Last URL used for tracking hyperlink state
    private string? _lastUrl = null;

    // Rows that contained URLs in the last rendered frame; used to emit OSC 8 close
    // before re-rendering a row that has since lost all URL cells, so terminals don't
    // keep stale hyperlink metadata.
    private readonly HashSet<int> _rowsWithUrls = [];

    // Identifies the buffer state we last synced _rowsWithUrls against. When the buffer
    // is replaced, resized, or its URL maps are wiped, this stops matching and we drop
    // the stale tracking before reading it.
    private IOutputBuffer? _lastTrackedBuffer;
    private int _lastTrackedRows;
    private int _lastTrackedCols;
    private int _lastTrackedUrlVersion;

    private readonly StringBuilder _lastOutputStringBuilder = new ();
    private bool _clearLastOutputPending;

    // Kitty image ids placed on the previous Write, keyed by RasterImageCommand.Id. A single image
    // can occupy more than one placement when its visible region is fragmented by clipping (e.g. a
    // SubView punches a hole), and each fragment needs its own image id — sharing one id makes each
    // a=T overwrite the previous fragment's data. Kitty placements persist until explicitly deleted
    // (unlike Sixel, which is erased by redrawing cells), so every id placed last frame must be
    // deleted when the image is resized, re-fragmented, or removed.
    private readonly Dictionary<string, List<int>> _placedKittyImageIds = [];

    /// <summary>
    ///     Writes dirty cells from the buffer to the console. Iterates rows/cols, skips clean cells,
    ///     batches dirty cells into ANSI sequences, emits OSC 8 hyperlink start/close around URL cells,
    ///     and finally renders queued sixel images. Cursor visibility is managed by
    ///     <c>ApplicationMainLoop.SetCursor()</c>.
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

        InvalidateRowsWithUrlsIfStale (buffer, rows, cols);
        IReadOnlyList<Rectangle> rasterCellRectangles = GetRasterCellRectangles (buffer);

        // Erase Kitty placements for images that were present last frame but are gone now. Kitty
        // placements persist until explicitly deleted, so removed images would otherwise linger.
        if (!IsLegacyConsole && UseKittyGraphics)
        {
            EmitVanishedKittyDeletes (buffer);
            ClearKittyRasterBlankCells (buffer);
        }

        // Raster images must be written before dirty cells so later text draws above them.
        if (!IsLegacyConsole)
        {
            RenderRasterImages (buffer, renderAfterText: false);
        }

        // Process each row
        for (int row = top; row < rows; row++)
        {
            // Skip rows that have no dirty cells — avoids emitting cursor-move sequences
            // for rows that were never drawn (important for inline mode where only the
            // view's rows are dirty and the rest of the buffer is clean).
            if (!buffer.DirtyLines [row])
            {
                continue;
            }

            if (!SetCursorPositionImpl (0, row))
            {
                return;
            }

            if (!IsLegacyConsole && buffer is OutputBufferImpl outputBuffer)
            {
                outputBuffer.SyncAutoUrlsForRow (row);
            }

            bool rowHadUrlsPreviously = _rowsWithUrls.Contains (row);
            bool rowHasUrlsNow = !IsLegacyConsole && RowContainsUrls (buffer, row, cols);

            outputStringBuilder.Clear ();
            _lastUrl = null; // Reset URL state at the start of each row

            if (!IsLegacyConsole && rowHadUrlsPreviously && !rowHasUrlsNow)
            {
                outputStringBuilder.Append (EscSeqUtils.OSC_EndHyperlink ());
            }

            // Process columns in row
            for (int col = left; col < cols; col++)
            {
                lastCol = -1;
                var outputWidth = 0;

                // Batch consecutive dirty cells
                for (; col < cols; col++)
                {
                    // Skip clean cells, plus blank cells owned by raster images. Raster-owned
                    // blanks must not be emitted because normal text output would erase Sixel
                    // images and is unnecessary over Kitty's transparent-cleared placement.
                    bool skipRasterBlank = IsRasterCoveredBlankCell (buffer, row, col, rasterCellRectangles);

                    if (!buffer.Contents! [row, col].IsDirty || skipRasterBlank)
                    {
                        if (skipRasterBlank)
                        {
                            buffer.Contents [row, col].IsDirty = false;
                        }

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

            // Track row's URL status BEFORE the early-exit so _rowsWithUrls stays consistent
            // with the buffer state — even for rows whose cells were all flushed via WriteToConsole
            // during the inner loop (leaving outputStringBuilder empty at this point).
            if (!IsLegacyConsole)
            {
                if (rowHasUrlsNow)
                {
                    _rowsWithUrls.Add (row);
                }
                else
                {
                    _rowsWithUrls.Remove (row);
                }
            }

            // Flush buffered output for row. Even when nothing remains buffered, an OSC 8 hyperlink
            // may still be open in the terminal because it was started in a prior batch flushed by
            // WriteToConsole and the row ended (or only clean cells followed) before any cell with
            // a different URL closed it. Emit the close so the link does not bleed into later rows.
            if (outputStringBuilder.Length <= 0 && _lastUrl is null)
            {
                continue;
            }

            if (IsLegacyConsole)
            {
                if (outputStringBuilder.Length > 0)
                {
                    Write (outputStringBuilder);
                }

                continue;
            }

            if (_lastUrl is { })
            {
                outputStringBuilder.Append (EscSeqUtils.OSC_EndHyperlink ());
                _lastUrl = null;
            }

            SetCursorPositionImpl (lastCol, row);

            Write (outputStringBuilder);
        }

        if (IsLegacyConsole)
        {
            return;
        }

        RenderRasterImages (buffer, renderAfterText: true);

        // Render queued sixel images
        foreach (SixelToRender s in GetSixels ())
        {
            if (string.IsNullOrWhiteSpace (s.SixelData) || (!s.IsDirty && !s.AlwaysRender))
            {
                continue;
            }

            SetCursorPositionImpl (s.ScreenPosition.X, s.ScreenPosition.Y);
            Write (new StringBuilder (s.SixelData));
            s.IsDirty = false;
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
        string? lastUrl = null;

        for (int row = startRow; row < endRow; row++)
        {
            for (int col = startCol; col < endCol; col++)
            {
                if (includeCellPredicate != null && !includeCellPredicate (row, col))
                {
                    continue;
                }

                // Handle OSC 8 hyperlink state transitions
                string? cellUrl = buffer.GetCellUrl (col, row);

                if (cellUrl != lastUrl)
                {
                    if (lastUrl is { })
                    {
                        output.Append (EscSeqUtils.OSC_EndHyperlink ());
                    }

                    if (!string.IsNullOrEmpty (cellUrl))
                    {
                        output.Append (EscSeqUtils.OSC_StartHyperlink (cellUrl));
                    }

                    lastUrl = cellUrl;
                }

                Cell cell = buffer.Contents! [row, col];
                int outputWidth = -1;
                AppendCellAnsi (cell, output, ref lastAttr, ref redrawTextStyle, endCol, ref col, ref outputWidth);
            }

            // Close any open hyperlink at end of row
            if (lastUrl is { })
            {
                output.Append (EscSeqUtils.OSC_EndHyperlink ());
                lastUrl = null;
            }

            // Add newline at end of row if requested. Use a fixed '\n', NOT
            // StringBuilder.AppendLine () / Environment.NewLine: ToAnsi produces a portable
            // escape-sequence stream that must be byte-identical regardless of the OS it ran
            // on (golden snapshots, cross-platform diffing). Terminals map LF -> CRLF via the
            // ONLCR tty discipline, so a '\n' row break still recreates the screen correctly.
            if (addNewlines)
            {
                output.Append ('\n');
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

                // Fixed '\n' (not Environment.NewLine) — keep legacy-console output portable too.
                output.Append ('\n');
            }

            return output.ToString ();
        }

        if (buffer is OutputBufferImpl outputBuffer)
        {
            outputBuffer.SyncAutoUrlsForAllRows ();
        }

        StringBuilder ansiOutput = new ();
        IReadOnlyList<Rectangle> rasterCellRectangles = GetRasterCellRectangles (buffer);
        bool wroteRasterImages = AppendRasterImageAnsi (buffer, ansiOutput, renderAfterText: false);

        if (wroteRasterImages)
        {
            ansiOutput.Append (EscSeqUtils.CSI_SetCursorPosition (1, 1));
        }

        Attribute? lastAttr = null;

        if (rasterCellRectangles.Count > 0)
        {
            BuildAnsiForRegionSkippingRasterCoveredBlanks (buffer,
                                                           0,
                                                           buffer.Rows,
                                                           0,
                                                           buffer.Cols,
                                                           ansiOutput,
                                                           ref lastAttr,
                                                           rasterCellRectangles);
        }
        else
        {
            BuildAnsiForRegion (buffer, 0, buffer.Rows, 0, buffer.Cols, ansiOutput, ref lastAttr);
        }

        AppendRasterImageAnsi (buffer, ansiOutput, renderAfterText: true);

        return ansiOutput.ToString ();
    }

    private void BuildAnsiForRegionSkippingRasterCoveredBlanks (IOutputBuffer buffer,
                                                                int startRow,
                                                                int endRow,
                                                                int startCol,
                                                                int endCol,
                                                                StringBuilder output,
                                                                ref Attribute? lastAttr,
                                                                IReadOnlyList<Rectangle> rasterCellRectangles)
    {
        var redrawTextStyle = TextStyle.None;
        string? lastUrl = null;

        for (int row = startRow; row < endRow; row++)
        {
            for (int col = startCol; col < endCol; col++)
            {
                if (IsRasterCoveredBlankCell (buffer, row, col, rasterCellRectangles))
                {
                    if (lastUrl is { })
                    {
                        output.Append (EscSeqUtils.OSC_EndHyperlink ());
                        lastUrl = null;
                    }

                    continue;
                }

                output.Append (EscSeqUtils.CSI_SetCursorPosition (row + 1, col + 1));
                lastAttr = null;
                redrawTextStyle = TextStyle.None;

                for (; col < endCol; col++)
                {
                    if (IsRasterCoveredBlankCell (buffer, row, col, rasterCellRectangles))
                    {
                        col--;

                        break;
                    }

                    string? cellUrl = buffer.GetCellUrl (col, row);

                    if (cellUrl != lastUrl)
                    {
                        if (lastUrl is { })
                        {
                            output.Append (EscSeqUtils.OSC_EndHyperlink ());
                        }

                        if (!string.IsNullOrEmpty (cellUrl))
                        {
                            output.Append (EscSeqUtils.OSC_StartHyperlink (cellUrl));
                        }

                        lastUrl = cellUrl;
                    }

                    Cell cell = buffer.Contents! [row, col];
                    int outputWidth = -1;
                    AppendCellAnsi (cell, output, ref lastAttr, ref redrawTextStyle, endCol, ref col, ref outputWidth);
                }
            }

            if (lastUrl is { })
            {
                output.Append (EscSeqUtils.OSC_EndHyperlink ());
                lastUrl = null;
            }
        }
    }

    private void ClearKittyRasterBlankCells (IOutputBuffer buffer)
    {
        if (buffer.Contents is null)
        {
            return;
        }

        foreach (RasterImageCommand command in buffer.GetRasterImages ())
        {
            bool clearAllCoveredBlanks = command.IsDirty || command.AlwaysRender || command.NeedsTransparentCellClear;

            foreach (Rectangle visibleCells in GetVisibleRasterCellRectangles (command))
            {
                Rectangle visible = Rectangle.Intersect (visibleCells, new Rectangle (0, 0, buffer.Cols, buffer.Rows));

                for (int row = visible.Y; row < visible.Bottom; row++)
                {
                    for (int col = visible.X; col < visible.Right; col++)
                    {
                        Cell cell = buffer.Contents [row, col];

                        if (!IsBlankCell (cell) || (!clearAllCoveredBlanks && !cell.IsDirty))
                        {
                            continue;
                        }

                        if (!SetCursorPositionImpl (col, row))
                        {
                            return;
                        }

                        WriteTransparentBlankCell ();
                        buffer.Contents [row, col].IsDirty = false;
                    }
                }
            }

            command.NeedsTransparentCellClear = false;
        }
    }

    private void WriteTransparentBlankCell ()
    {
        StringBuilder output = new ();
        EscSeqUtils.CSI_AppendResetForegroundColor (output);
        EscSeqUtils.CSI_AppendResetBackgroundColor (output);
        output.Append (' ');
        Write (output);
    }

    private static IReadOnlyList<Rectangle> GetRasterCellRectangles (IOutputBuffer buffer)
    {
        List<Rectangle> rectangles = [];
        Rectangle screen = new (0, 0, buffer.Cols, buffer.Rows);

        foreach (RasterImageCommand command in buffer.GetRasterImages ())
        {
            foreach (Rectangle visibleCells in GetVisibleRasterCellRectangles (command))
            {
                Rectangle visible = Rectangle.Intersect (visibleCells, screen);

                if (visible.Width > 0 && visible.Height > 0)
                {
                    rectangles.Add (visible);
                }
            }
        }

        return rectangles;
    }

    private static bool IsRasterCoveredBlankCell (IOutputBuffer buffer,
                                                 int row,
                                                 int col,
                                                 IReadOnlyList<Rectangle> rasterCellRectangles)
    {
        if (rasterCellRectangles.Count == 0 || buffer.Contents is null || !IsBlankCell (buffer.Contents [row, col]))
        {
            return false;
        }

        foreach (Rectangle rectangle in rasterCellRectangles)
        {
            if (rectangle.Contains (col, row))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBlankCell (Cell cell) => string.IsNullOrEmpty (cell.Grapheme) || cell.Grapheme == " ";

    // Emits Kitty delete sequences for images that were placed on the previous Write but are no
    // longer in the buffer, dropping their tracking entries. Entries for images still present are
    // left untouched — their placements persist on screen and are managed by RenderRasterImages.
    private void EmitVanishedKittyDeletes (IOutputBuffer buffer)
    {
        HashSet<string> currentIds = [];

        foreach (RasterImageCommand command in buffer.GetRasterImages ())
        {
            if (string.IsNullOrEmpty (command.Id))
            {
                continue;
            }

            currentIds.Add (command.Id);
        }

        List<string> vanished = [];

        foreach ((string id, List<int> placedImageIds) in _placedKittyImageIds)
        {
            if (currentIds.Contains (id))
            {
                continue;
            }

            DeleteKittyPlacements (placedImageIds);
            vanished.Add (id);
        }

        foreach (string id in vanished)
        {
            _placedKittyImageIds.Remove (id);
        }
    }

    private void DeleteKittyPlacements (List<int> imageIds)
    {
        foreach (int imageId in imageIds)
        {
            Write (new StringBuilder (KittyGraphicsEncoder.EncodeDeletePlacements (imageId)));
        }
    }

    // Derives the Kitty image id for the rectIndex-th visible fragment of a command. Fragment 0 uses
    // the command's base id (matching ImageView's pre-encoded payload); later fragments get distinct
    // ids so their data does not overwrite earlier fragments under a shared id.
    private static int GetKittyImageId (string commandId, int rectIndex) =>
        rectIndex == 0
            ? KittyGraphicsEncoder.GetImageId (commandId)
            : KittyGraphicsEncoder.GetImageId ($"{commandId}#{rectIndex}");

    private void RenderRasterImages (IOutputBuffer buffer, bool renderAfterText)
    {
        foreach (RasterImageCommand command in buffer.GetRasterImages ())
        {
            if (command.RenderAfterText != renderAfterText)
            {
                continue;
            }

            if (!command.IsDirty && !command.AlwaysRender)
            {
                continue;
            }

            bool trackKitty = UseKittyGraphics && !string.IsNullOrEmpty (command.Id);

            // Erase every prior placement of this image before re-placing it, so a resized, moved, or
            // re-fragmented Kitty image does not leave stale placements on screen. Sixel needs no such
            // delete — redrawing the cells overwrites it.
            if (trackKitty && _placedKittyImageIds.TryGetValue (command.Id!, out List<int>? previous))
            {
                DeleteKittyPlacements (previous);
            }

            List<int> placedImageIds = [];
            var rectIndex = 0;

            foreach (Rectangle visibleCells in GetVisibleRasterCellRectangles (command))
            {
                if (!TryCropRasterImagePixels (command.Pixels!, command.DestinationCells, visibleCells, out Color [,] pixels))
                {
                    continue;
                }

                int imageId = trackKitty ? GetKittyImageId (command.Id!, rectIndex) : 0;
                SetCursorPositionImpl (visibleCells.X, visibleCells.Y);
                Write (new StringBuilder (GetRasterImageData (command, visibleCells, pixels, imageId)));

                if (trackKitty)
                {
                    placedImageIds.Add (imageId);
                }

                rectIndex++;
            }

            if (trackKitty)
            {
                _placedKittyImageIds [command.Id!] = placedImageIds;
            }

            command.IsDirty = false;
        }
    }

    private bool AppendRasterImageAnsi (IOutputBuffer buffer, StringBuilder output, bool renderAfterText)
    {
        bool wroteRasterImages = false;

        foreach (RasterImageCommand command in buffer.GetRasterImages ())
        {
            if (command.RenderAfterText != renderAfterText)
            {
                continue;
            }

            bool useKittyIds = UseKittyGraphics && !string.IsNullOrEmpty (command.Id);
            var rectIndex = 0;

            foreach (Rectangle visibleCells in GetVisibleRasterCellRectangles (command))
            {
                if (!TryCropRasterImagePixels (command.Pixels!, command.DestinationCells, visibleCells, out Color [,] pixels))
                {
                    continue;
                }

                int imageId = useKittyIds ? GetKittyImageId (command.Id!, rectIndex) : 0;
                output.Append (EscSeqUtils.CSI_SetCursorPosition (visibleCells.Y + 1, visibleCells.X + 1));
                output.Append (GetRasterImageData (command, visibleCells, pixels, imageId));
                wroteRasterImages = true;
                rectIndex++;
            }
        }

        return wroteRasterImages;
    }

    private string GetRasterImageData (RasterImageCommand command, Rectangle visibleCells, Color [,] pixels, int imageId)
    {
        if (UseKittyGraphics)
        {
            return GetRasterImageKittyData (command, visibleCells, pixels, imageId);
        }

        return GetRasterImageSixelData (command, visibleCells, pixels);
    }

    private static string GetRasterImageKittyData (RasterImageCommand command, Rectangle visibleCells, Color [,] pixels, int imageId)
    {
        // The pre-encoded payload carries the base image id and covers the whole destination, so it
        // only applies to fragment 0 rendered un-clipped.
        if (command.EncodedKitty is { } encodedKitty
            && visibleCells == command.DestinationCells
            && imageId == KittyGraphicsEncoder.GetImageId (command.Id!))
        {
            return encodedKitty;
        }

        KittyGraphicsEncoder encoder = new ();

        return encoder.EncodeKitty (pixels, visibleCells.Width, visibleCells.Height, imageId);
    }

    private static string GetRasterImageSixelData (RasterImageCommand command, Rectangle visibleCells, Color [,] pixels)
    {
        if (command.EncodedSixel is { } encodedSixel && visibleCells == command.DestinationCells)
        {
            return encodedSixel;
        }

        SixelEncoder encoder = command.Encoder ?? new ();

        return encoder.EncodeSixel (pixels);
    }

    private static IEnumerable<Rectangle> GetVisibleRasterCellRectangles (RasterImageCommand command)
    {
        if (command.Pixels is null || command.DestinationCells.Width <= 0 || command.DestinationCells.Height <= 0)
        {
            yield break;
        }

        if (command.Clip is null)
        {
            yield return command.DestinationCells;

            yield break;
        }

        foreach (Rectangle clipRect in command.Clip.GetRectangles ())
        {
            Rectangle visible = Rectangle.Intersect (command.DestinationCells, clipRect);

            if (visible.Width <= 0 || visible.Height <= 0)
            {
                continue;
            }

            yield return visible;
        }
    }

    private static bool TryCropRasterImagePixels (Color [,] source,
                                                 Rectangle destinationCells,
                                                 Rectangle visibleCells,
                                                 out Color [,] pixels)
    {
        pixels = source;

        int sourceWidth = source.GetLength (0);
        int sourceHeight = source.GetLength (1);

        if (sourceWidth <= 0
            || sourceHeight <= 0
            || destinationCells.Width <= 0
            || destinationCells.Height <= 0
            || visibleCells.Width <= 0
            || visibleCells.Height <= 0)
        {
            return false;
        }

        int xStart = ScaleCellOffsetToPixels (visibleCells.X - destinationCells.X, destinationCells.Width, sourceWidth);
        int xEnd = ScaleCellOffsetToPixels (visibleCells.Right - destinationCells.X, destinationCells.Width, sourceWidth);
        int yStart = ScaleCellOffsetToPixels (visibleCells.Y - destinationCells.Y, destinationCells.Height, sourceHeight);
        int yEnd = ScaleCellOffsetToPixels (visibleCells.Bottom - destinationCells.Y, destinationCells.Height, sourceHeight);

        xStart = Math.Clamp (xStart, 0, sourceWidth);
        xEnd = Math.Clamp (xEnd, xStart, sourceWidth);
        yStart = Math.Clamp (yStart, 0, sourceHeight);
        yEnd = Math.Clamp (yEnd, yStart, sourceHeight);

        int width = xEnd - xStart;
        int height = yEnd - yStart;

        if (width <= 0 || height <= 0)
        {
            return false;
        }

        if (width == sourceWidth && height == sourceHeight)
        {
            pixels = source;

            return true;
        }

        pixels = new Color [width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                pixels [x, y] = source [xStart + x, yStart + y];
            }
        }

        return true;
    }

    private static int ScaleCellOffsetToPixels (int cellOffset, int cellCount, int pixelCount) =>
        (int)((long)cellOffset * pixelCount / cellCount);

    /// <summary>
    ///     Writes buffered output to console, then clears the buffer and advances
    ///     <paramref name="lastCol"/> by <paramref name="outputWidth"/>.
    /// </summary>
    private void WriteToConsole (StringBuilder output, ref int lastCol, ref int outputWidth)
    {
        Write (output);

        output.Clear ();
        lastCol += outputWidth;
        outputWidth = 0;
    }

    private static bool RowContainsUrls (IOutputBuffer buffer, int row, int cols)
    {
        for (int col = 0; col < cols; col++)
        {
            if (!string.IsNullOrEmpty (buffer.GetCellUrl (col, row)))
            {
                return true;
            }
        }

        return false;
    }

    private void InvalidateRowsWithUrlsIfStale (IOutputBuffer buffer, int rows, int cols)
    {
        int urlVersion = buffer is OutputBufferImpl outputBuffer ? outputBuffer.UrlStateVersion : 0;

        if (!ReferenceEquals (_lastTrackedBuffer, buffer)
            || _lastTrackedRows != rows
            || _lastTrackedCols != cols
            || _lastTrackedUrlVersion != urlVersion)
        {
            _rowsWithUrls.Clear ();
            _lastTrackedBuffer = buffer;
            _lastTrackedRows = rows;
            _lastTrackedCols = cols;
            _lastTrackedUrlVersion = urlVersion;
        }
    }
}

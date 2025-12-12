using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Stores the desired output state for the whole application. This is updated during
///     draw operations before being flushed to the console as part of the main loop.
///     operation
/// </summary>
public class OutputBufferImpl : IOutputBuffer
{
    /// <summary>
    ///     The contents of the application output. The driver outputs this buffer to the terminal when
    ///     UpdateScreen is called.
    ///     <remarks>The format of the array is rows, columns. The first index is the row, the second index is the column.</remarks>
    /// </summary>
    public Cell [,]? Contents { get; set; } = new Cell [0, 0];

    private int _cols;
    private int _rows;

    /// <summary>
    ///     The <see cref="Attribute"/> that will be used for the next <see cref="AddRune(Rune)"/> or <see cref="AddStr"/>
    ///     call.
    /// </summary>
    public Attribute CurrentAttribute { get; set; }

    /// <summary>The leftmost column in the terminal.</summary>
    public virtual int Left { get; set; } = 0;

    /// <summary>
    ///     Gets the row last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Row { get; private set; }

    /// <summary>
    ///     Gets the column last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Col { get; private set; }

    /// <summary>The number of rows visible in the terminal.</summary>
    public int Rows
    {
        get => _rows;
        set
        {
            _rows = value;
            ClearContents ();
        }
    }

    /// <summary>The number of columns visible in the terminal.</summary>
    public int Cols
    {
        get => _cols;
        set
        {
            _cols = value;
            ClearContents ();
        }
    }

    /// <summary>The topmost row in the terminal.</summary>
    public virtual int Top { get; set; } = 0;

    /// <summary>
    ///     Indicates which lines have been modified and need to be redrawn.
    /// </summary>
    public bool [] DirtyLines { get; set; } = [];

    // QUESTION: When non-full screen apps are supported, will this represent the app size, or will that be in Application?
    /// <summary>Gets the location and size of the terminal screen.</summary>
    internal Rectangle Screen => new (0, 0, Cols, Rows);

    private Region? _clip;

    /// <summary>
    ///     Gets or sets the clip rectangle that <see cref="AddRune(Rune)"/> and <see cref="AddStr(string)"/> are subject
    ///     to.
    /// </summary>
    /// <value>The rectangle describing the of <see cref="Clip"/> region.</value>
    public Region? Clip
    {
        get => _clip;
        set
        {
            if (ReferenceEquals (_clip, value))
            {
                return;
            }

            _clip = value;

            // Don't ever let Clip be bigger than Screen
            _clip?.Intersect (Screen);
        }
    }

    /// <summary>Adds the specified rune to the display at the current cursor position.</summary>
    /// <remarks>
    ///     <para>
    ///         When the method returns, <see cref="Col"/> will be incremented by the number of columns
    ///         <paramref name="rune"/> required, even if the new column value is outside the <see cref="Clip"/> or screen
    ///         dimensions defined by <see cref="Cols"/>.
    ///     </para>
    ///     <para>
    ///         If <paramref name="rune"/> requires more than one column, and <see cref="Col"/> plus the number of columns
    ///         needed exceeds the <see cref="Clip"/> or screen dimensions, the default Unicode replacement character (U+FFFD)
    ///         will be added instead.
    ///     </para>
    /// </remarks>
    /// <param name="rune">Text to add.</param>
    public void AddRune (Rune rune) { AddStr (rune.ToString ()); }

    /// <summary>
    ///     Adds the specified <see langword="char"/> to the display at the current cursor position. This method is a
    ///     convenience method that calls <see cref="AddRune(Rune)"/> with the <see cref="Rune"/> constructor.
    /// </summary>
    /// <param name="c">Character to add.</param>
    public void AddRune (char c) { AddRune (new Rune (c)); }

    /// <summary>Adds the <paramref name="str"/> to the display at the cursor position.</summary>
    /// <remarks>
    ///     <para>
    ///         When the method returns, <see cref="Col"/> will be incremented by the number of columns
    ///         <paramref name="str"/> required, unless the new column value is outside the <see cref="Clip"/> or screen
    ///         dimensions defined by <see cref="Cols"/>.
    ///     </para>
    ///     <para>If <paramref name="str"/> requires more columns than are available, the output will be clipped.</para>
    /// </remarks>
    /// <param name="str">String.</param>
    public void AddStr (string str)
    {
        foreach (string grapheme in GraphemeHelper.GetGraphemes (str))
        {
            AddGrapheme (grapheme);
        }
    }

    /// <summary>
    ///     Adds a single grapheme to the display at the current cursor position.
    /// </summary>
    /// <param name="grapheme">The grapheme to add.</param>
    private void AddGrapheme (string grapheme)
    {
        if (Contents is null)
        {
            return;
        }

        Clip ??= new (Screen);
        Rectangle clipRect = Clip!.GetBounds ();

        int printableGraphemeWidth = -1;

        lock (Contents)
        {
            if (IsValidLocation (grapheme, Col, Row))
            {
                // Set attribute and mark dirty for current cell
                SetAttributeAndDirty (Col, Row);
                InvalidateOverlappedWideGlyph (Col, Row);

                string printableGrapheme = grapheme.MakePrintable ();
                printableGraphemeWidth = printableGrapheme.GetColumns ();
                WriteGraphemeByWidth (Col, Row, printableGrapheme, printableGraphemeWidth, clipRect);

                DirtyLines [Row] = true;
            }

            // Always advance cursor (even if location was invalid)
            // Keep Col/Row updates inside the lock to prevent race conditions
            Col++;

            if (printableGraphemeWidth > 1)
            {
                // Skip the second column of a wide character
                // IMPORTANT: We do NOT modify column N+1's IsDirty or Attribute here.
                // See: https://github.com/gui-cs/Terminal.Gui/issues/4258
                Col++;
            }
        }
    }

    /// <summary>
    ///     INTERNAL: Helper to set the attribute and mark the cell as dirty.
    /// </summary>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    private void SetAttributeAndDirty (int col, int row)
    {
        Contents! [row, col].Attribute = CurrentAttribute;
        Contents [row, col].IsDirty = true;
    }

    /// <summary>
    ///     INTERNAL: If we're writing at an odd column and there's a wide glyph to our left,
    ///     invalidate it since we're overwriting the second half.
    /// </summary>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    private void InvalidateOverlappedWideGlyph (int col, int row)
    {
        if (col > 0 && Contents! [row, col - 1].Grapheme.GetColumns () > 1)
        {
            Contents [row, col - 1].Grapheme = Rune.ReplacementChar.ToString ();
            Contents [row, col - 1].IsDirty = true;
        }
    }

    /// <summary>
    ///     INTERNAL: Writes a Grapheme to the buffer based on its width (0, 1, or 2 columns).
    /// </summary>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <param name="text">The printable text to write.</param>
    /// <param name="textWidth">The column width of the text.</param>
    /// <param name="clipRect">The clipping rectangle.</param>
    private void WriteGraphemeByWidth (int col, int row, string text, int textWidth, Rectangle clipRect)
    {
        switch (textWidth)
        {
            case 0:
            case 1:
                WriteGrapheme (col, row, text, clipRect);

                break;

            case 2:
                WriteWideGrapheme (col, row, text);

                break;

            default:
                // Negative width or non-spacing character (shouldn't normally occur)
                Contents! [row, col].Grapheme = " ";
                Contents [row, col].IsDirty = false;

                break;
        }
    }

    /// <summary>
    ///     INTERNAL: Writes a (0 or 1 column wide) Grapheme.
    /// </summary>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <param name="grapheme">The single-width Grapheme to write.</param>
    /// <param name="clipRect">The clipping rectangle.</param>
    private void WriteGrapheme (int col, int row, string grapheme, Rectangle clipRect)
    {
        Debug.Assert (grapheme.GetColumns () < 2);
        Contents! [row, col].Grapheme = grapheme;

        // Mark the next cell as dirty to ensure proper rendering of adjacent content
        if (col < clipRect.Right - 1 && col + 1 < Cols)
        {
            Contents [row, col + 1].IsDirty = true;
        }
    }

    /// <summary>
    ///     INTERNAL: Writes a wide Grapheme (2 columns wide) handling clipping and partial overlap cases.
    /// </summary>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <param name="grapheme">The wide Grapheme to write.</param>
    private void WriteWideGrapheme (int col, int row, string grapheme)
    {
        Debug.Assert (grapheme.GetColumns () == 2);
        if (!Clip!.Contains (col + 1, row))
        {
            // Second column is outside clip - can't fit wide char here
            Contents! [row, col].Grapheme = Rune.ReplacementChar.ToString ();
        }
        else if (!Clip.Contains (col, row))
        {
            // First column is outside clip but second isn't
            // Mark second column as replacement to indicate partial overlap
            if (col + 1 < Cols)
            {
                Contents! [row, col + 1].Grapheme = Rune.ReplacementChar.ToString ();
                Contents! [row, col + 1].IsDirty = true;
            }
        }
        else
        {
            // Both columns are in bounds - write the wide character
            // It will naturally render across both columns when output to the terminal
            Contents! [row, col].Grapheme = grapheme;

            // DO NOT modify column N+1 here!
            // The wide glyph will naturally render across both columns.
            // If we set column N+1 to replacement char, we would overwrite
            // any content that was intentionally drawn there (like borders at odd columns).
            // See: https://github.com/gui-cs/Terminal.Gui/issues/4258
        }
    }

    /// <summary>Clears the <see cref="Contents"/> of the driver.</summary>
    public void ClearContents ()
    {
        Contents = new Cell [Rows, Cols];

        // CONCURRENCY: Unsynchronized access to Clip isn't safe.
        // TODO: ClearContents should not clear the clip; it should only clear the contents. Move clearing it elsewhere.
        Clip = new (Screen);

        DirtyLines = new bool [Rows];

        lock (Contents)
        {
            for (var row = 0; row < Rows; row++)
            {
                for (var c = 0; c < Cols; c++)
                {
                    Contents [row, c] = new ()
                    {
                        Grapheme = " ",
                        Attribute = new Attribute (Color.White, Color.Black),
                        IsDirty = true
                    };
                }

                DirtyLines [row] = true;
            }
        }
    }

    /// <summary>Tests whether the specified coordinate are valid for drawing the specified Text.</summary>
    /// <param name="text">Used to determine if one or two columns are required.</param>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>
    ///     <see langword="false"/> if the coordinate is outside the screen bounds or outside of <see cref="Clip"/>.
    ///     <see langword="true"/> otherwise.
    /// </returns>
    public bool IsValidLocation (string text, int col, int row)
    {
        int textWidth = text.GetColumns ();

        return col >= 0 && row >= 0 && col + textWidth <= Cols && row < Rows && Clip!.Contains (col, row);
    }

    /// <inheritdoc/>
    public void SetSize (int cols, int rows)
    {
        Cols = cols;
        Rows = rows;
        ClearContents ();
    }

    /// <inheritdoc/>
    public void FillRect (Rectangle rect, Rune rune)
    {
        Rectangle clipBounds = Clip?.GetBounds () ?? Screen;
        // BUGBUG: This should be a method on Region
        rect = Rectangle.Intersect (rect, clipBounds);

        lock (Contents!)
        {
            for (int r = rect.Y; r < rect.Y + rect.Height; r++)
            {
                for (int c = rect.X; c < rect.X + rect.Width; c++)
                {
                    if (!IsValidLocation (rune.ToString (), c, r))
                    {
                        continue;
                    }

                    // We could call AddGrapheme here, but that would acquire the lock again.
                    // So we inline the logic instead.
                    SetAttributeAndDirty (c, r);
                    InvalidateOverlappedWideGlyph (c, r);
                    string grapheme = rune != default (Rune) ? rune.ToString () : " ";
                    WriteGraphemeByWidth (c, r, grapheme, grapheme.GetColumns (), clipBounds);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void FillRect (Rectangle rect, char rune)
    {
        for (int y = rect.Top; y < rect.Top + rect.Height; y++)
        {
            for (int x = rect.Left; x < rect.Left + rect.Width; x++)
            {
                Move (x, y);
                AddRune (rune);
            }
        }
    }

    /// <summary>
    ///     Updates <see cref="Col"/> and <see cref="Row"/> to the specified column and row in <see cref="Contents"/>.
    ///     Used by <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    /// <remarks>
    ///     <para>This does not move the cursor on the screen, it only updates the internal state of the driver.</para>
    ///     <para>
    ///         If <paramref name="col"/> or <paramref name="row"/> are negative or beyond  <see cref="Cols"/> and
    ///         <see cref="Rows"/>, the method still sets those properties.
    ///     </para>
    /// </remarks>
    /// <param name="col">Column to move to.</param>
    /// <param name="row">Row to move to.</param>
    public void Move (int col, int row)
    {
        Col = col;
        Row = row;
    }
}

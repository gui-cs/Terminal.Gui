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
            if (_clip == value)
            {
                return;
            }

            _clip = value;

            // Don't ever let Clip be bigger than Screen
            if (_clip is { })
            {
                _clip.Intersect (Screen);
            }
        }
    }

    /// <summary>Adds the specified rune to the display at the current cursor position.</summary>
    /// <remarks>
    ///     <para>
    ///         When the method returns, <see cref="Col"/> will be incremented by the number of columns
    ///         <paramref name="rune"/> required, even if the new column value is outside of the <see cref="Clip"/> or screen
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

        string text = grapheme;
        int textWidth = -1;

        lock (Contents)
        {
            bool validLocation = IsValidLocation (text, Col, Row);

            if (validLocation)
            {
                text = text.MakePrintable ();
                textWidth = text.GetColumns ();

                // Set attribute and mark dirty for current cell
                Contents [Row, Col].Attribute = CurrentAttribute;
                Contents [Row, Col].IsDirty = true;

                InvalidateOverlappedWideGlyph ();

                WriteGraphemeByWidth (text, textWidth, clipRect);

                DirtyLines [Row] = true;
            }

            // Always advance cursor (even if location was invalid)
            // Keep Col/Row updates inside the lock to prevent race conditions
            Col++;

            if (textWidth > 1)
            {
                // Skip the second column of a wide character
                // IMPORTANT: We do NOT modify column N+1's IsDirty or Attribute here.
                // See: https://github.com/gui-cs/Terminal.Gui/issues/4258
                Col++;
            }
        }
    }

    /// <summary>
    ///     If we're writing at an odd column and there's a wide glyph to our left,
    ///     invalidate it since we're overwriting the second half.
    /// </summary>
    private void InvalidateOverlappedWideGlyph ()
    {
        if (Col > 0 && Contents! [Row, Col - 1].Grapheme.GetColumns () > 1)
        {
            Contents [Row, Col - 1].Grapheme = Rune.ReplacementChar.ToString ();
            Contents [Row, Col - 1].IsDirty = true;
        }
    }

    /// <summary>
    ///     Writes a grapheme to the buffer based on its width (0, 1, or 2 columns).
    /// </summary>
    /// <param name="text">The printable text to write.</param>
    /// <param name="textWidth">The column width of the text.</param>
    /// <param name="clipRect">The clipping rectangle.</param>
    private void WriteGraphemeByWidth (string text, int textWidth, Rectangle clipRect)
    {
        switch (textWidth)
        {
            case 0:
            case 1:
                WriteSingleWidthGrapheme (text, clipRect);

                break;

            case 2:
                WriteWideGrapheme (text);

                break;

            default:
                // Zero-width or non-spacing character (e.g., combining marks)
                Contents! [Row, Col].Grapheme = " ";
                Contents [Row, Col].IsDirty = false;

                break;
        }
    }

    /// <summary>
    ///     Writes a single-width character (0 or 1 column wide).
    /// </summary>
    private void WriteSingleWidthGrapheme (string text, Rectangle clipRect)
    {
        Contents! [Row, Col].Grapheme = text;

        // Mark the next cell as dirty to ensure proper rendering of adjacent content
        if (Col < clipRect.Right - 1 && Col + 1 < Cols)
        {
            Contents [Row, Col + 1].IsDirty = true;
        }
    }

    /// <summary>
    ///     Writes a wide character (2 columns wide) handling clipping and partial overlap cases.
    /// </summary>
    private void WriteWideGrapheme (string text)
    {
        if (!Clip!.Contains (Col + 1, Row))
        {
            // Second column is outside clip - can't fit wide char here
            Contents! [Row, Col].Grapheme = Rune.ReplacementChar.ToString ();
        }
        else if (!Clip.Contains (Col, Row))
        {
            // First column is outside clip but second isn't
            // Mark second column as replacement to indicate partial overlap
            if (Col + 1 < Cols)
            {
                Contents! [Row, Col + 1].Grapheme = Rune.ReplacementChar.ToString ();
            }
        }
        else
        {
            // Both columns are in bounds - write the wide character
            // It will naturally render across both columns when output to the terminal
            Contents! [Row, Col].Grapheme = text;

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

        //CONCURRENCY: Unsynchronized access to Clip isn't safe.
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

        // TODO: Who uses this and why? I am removing for now - this class is a state class not an events class
        //ClearedContents?.Invoke (this, EventArgs.Empty);
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
        // BUGBUG: This should be a method on Region
        rect = Rectangle.Intersect (rect, Clip?.GetBounds () ?? Screen);

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

                    Contents [r, c] = new ()
                    {
                        Grapheme = rune != default (Rune) ? rune.ToString () : " ",
                        Attribute = CurrentAttribute, IsDirty = true
                    };
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

    // TODO: Make internal once Menu is upgraded
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
    public virtual void Move (int col, int row)
    {
        //Debug.Assert (col >= 0 && row >= 0 && col < Contents.GetLength(1) && row < Contents.GetLength(0));
        Col = col;
        Row = row;
    }
}

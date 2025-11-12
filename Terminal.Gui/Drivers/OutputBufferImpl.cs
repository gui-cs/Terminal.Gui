#nullable enable
using System.Diagnostics;

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
    public Cell [,]? Contents { get; set; } = new Cell[0, 0];

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
    /// Indicates which lines have been modified and need to be redrawn.
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
            string text = grapheme;

            int textWidth = -1;
            bool validLocation = IsValidLocation (text, Col, Row);

            if (Contents is null)
            {
                return;
            }

            Clip ??= new (Screen);

            Rectangle clipRect = Clip!.GetBounds ();

            if (validLocation)
            {
                text = text.MakePrintable ();
                textWidth = text.GetColumns ();

                lock (Contents)
                {
                    Contents [Row, Col].Attribute = CurrentAttribute;
                    Contents [Row, Col].IsDirty = true;

                    if (Col > 0)
                    {
                        // Check if cell to left has a wide glyph
                        if (Contents [Row, Col - 1].Grapheme.GetColumns () > 1)
                        {
                            // Invalidate cell to left
                            Contents [Row, Col - 1].Grapheme = Rune.ReplacementChar.ToString ();
                            Contents [Row, Col - 1].IsDirty = true;
                        }
                    }

                    if (textWidth is 0 or 1)
                    {
                        Contents [Row, Col].Grapheme = text;

                        if (Col < clipRect.Right - 1)
                        {
                            Contents [Row, Col + 1].IsDirty = true;
                        }
                    }
                    else if (textWidth == 2)
                    {
                        if (!Clip.Contains (Col + 1, Row))
                        {
                            // We're at the right edge of the clip, so we can't display a wide character.
                            // TODO: Figure out if it is better to show a replacement character or ' '
                            Contents [Row, Col].Grapheme = Rune.ReplacementChar.ToString ();
                        }
                        else if (!Clip.Contains (Col, Row))
                        {
                            // Our 1st column is outside the clip, so we can't display a wide character.
                            Contents [Row, Col + 1].Grapheme = Rune.ReplacementChar.ToString ();
                        }
                        else
                        {
                            Contents [Row, Col].Grapheme = text;

                            if (Col < clipRect.Right - 1)
                            {
                                // Invalidate cell to right so that it doesn't get drawn
                                // TODO: Figure out if it is better to show a replacement character or ' '
                                Contents [Row, Col + 1].Grapheme = Rune.ReplacementChar.ToString ();
                                Contents [Row, Col + 1].IsDirty = true;
                            }
                        }
                    }
                    else
                    {
                        // This is a non-spacing character, so we don't need to do anything
                        Contents [Row, Col].Grapheme = " ";
                        Contents [Row, Col].IsDirty = false;
                    }

                    DirtyLines [Row] = true;
                }
            }

            Col++;

            if (textWidth > 1)
            {
                Debug.Assert (textWidth <= 2);

                if (validLocation && Col < clipRect.Right)
                {
                    lock (Contents!)
                    {
                        // This is a double-width character, and we are not at the end of the line.
                        // Col now points to the second column of the character. Ensure it doesn't
                        // Get rendered.
                        Contents [Row, Col].IsDirty = false;
                        Contents [Row, Col].Attribute = CurrentAttribute;

                        // TODO: Determine if we should wipe this out (for now now)
                        //Contents [Row, Col].Text = (Text)' ';
                    }
                }

                Col++;
            }
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
        if (text.GetColumns () < 2)
        {
            return col >= 0 && row >= 0 && col < Cols && row < Rows && Clip!.Contains (col, row);
        }

        return Clip!.Contains (col, row) || Clip!.Contains (col + 1, row);
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

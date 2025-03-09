#nullable enable
using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     Stores the desired output state for the whole application. This is updated during
///     draw operations before being flushed to the console as part of <see cref="MainLoop{T}"/>
///     operation
/// </summary>
public class OutputBuffer : IOutputBuffer
{
    /// <summary>
    ///     The contents of the application output. The driver outputs this buffer to the terminal when
    ///     UpdateScreen is called.
    ///     <remarks>The format of the array is rows, columns. The first index is the row, the second index is the column.</remarks>
    /// </summary>
    public Cell [,] Contents { get; set; } = new Cell[0, 0];

    private Attribute _currentAttribute;
    private int _cols;
    private int _rows;

    /// <summary>
    ///     The <see cref="Attribute"/> that will be used for the next <see cref="AddRune(Rune)"/> or <see cref="AddStr"/>
    ///     call.
    /// </summary>
    public Attribute CurrentAttribute
    {
        get => _currentAttribute;
        set
        {
            // TODO: This makes IConsoleDriver dependent on Application, which is not ideal. Once Attribute.PlatformColor is removed, this can be fixed.
            if (Application.Driver is { })
            {
                _currentAttribute = new (value.Foreground, value.Background);

                return;
            }

            _currentAttribute = value;
        }
    }

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

    /// <inheritdoc/>
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
    /// <param name="rune">Rune to add.</param>
    public void AddRune (Rune rune)
    {
        int runeWidth = -1;
        bool validLocation = IsValidLocation (rune, Col, Row);

        if (Contents is null)
        {
            return;
        }

        Rectangle clipRect = Clip!.GetBounds ();

        if (validLocation)
        {
            rune = rune.MakePrintable ();
            runeWidth = rune.GetColumns ();

            lock (Contents)
            {
                if (runeWidth == 0 && rune.IsCombiningMark ())
                {
                    // AtlasEngine does not support NON-NORMALIZED combining marks in a way
                    // compatible with the driver architecture. Any CMs (except in the first col)
                    // are correctly combined with the base char, but are ALSO treated as 1 column
                    // width codepoints E.g. `echo "[e`u{0301}`u{0301}]"` will output `[é  ]`.
                    // 
                    // Until this is addressed (see Issue #), we do our best by 
                    // a) Attempting to normalize any CM with the base char to it's left
                    // b) Ignoring any CMs that don't normalize
                    if (Col > 0)
                    {
                        if (Contents [Row, Col - 1].CombiningMarks.Count > 0)
                        {
                            // Just add this mark to the list
                            Contents [Row, Col - 1].CombiningMarks.Add (rune);

                            // Ignore. Don't move to next column (let the driver figure out what to do).
                        }
                        else
                        {
                            // Attempt to normalize the cell to our left combined with this mark
                            string combined = Contents [Row, Col - 1].Rune + rune.ToString ();

                            // Normalize to Form C (Canonical Composition)
                            string normalized = combined.Normalize (NormalizationForm.FormC);

                            if (normalized.Length == 1)
                            {
                                // It normalized! We can just set the Cell to the left with the
                                // normalized codepoint 
                                Contents [Row, Col - 1].Rune = (Rune)normalized [0];

                                // Ignore. Don't move to next column because we're already there
                            }
                            else
                            {
                                // It didn't normalize. Add it to the Cell to left's CM list
                                Contents [Row, Col - 1].CombiningMarks.Add (rune);

                                // Ignore. Don't move to next column (let the driver figure out what to do).
                            }
                        }

                        Contents [Row, Col - 1].Attribute = CurrentAttribute;
                        Contents [Row, Col - 1].IsDirty = true;
                    }
                    else
                    {
                        // Most drivers will render a combining mark at col 0 as the mark
                        Contents [Row, Col].Rune = rune;
                        Contents [Row, Col].Attribute = CurrentAttribute;
                        Contents [Row, Col].IsDirty = true;
                        Col++;
                    }
                }
                else
                {
                    Contents [Row, Col].Attribute = CurrentAttribute;
                    Contents [Row, Col].IsDirty = true;

                    if (Col > 0)
                    {
                        // Check if cell to left has a wide glyph
                        if (Contents [Row, Col - 1].Rune.GetColumns () > 1)
                        {
                            // Invalidate cell to left
                            Contents [Row, Col - 1].Rune = Rune.ReplacementChar;
                            Contents [Row, Col - 1].IsDirty = true;
                        }
                    }

                    if (runeWidth < 1)
                    {
                        Contents [Row, Col].Rune = Rune.ReplacementChar;
                    }
                    else if (runeWidth == 1)
                    {
                        Contents [Row, Col].Rune = rune;

                        if (Col < clipRect.Right - 1)
                        {
                            Contents [Row, Col + 1].IsDirty = true;
                        }
                    }
                    else if (runeWidth == 2)
                    {
                        if (!Clip.Contains (Col + 1, Row))
                        {
                            // We're at the right edge of the clip, so we can't display a wide character.
                            // TODO: Figure out if it is better to show a replacement character or ' '
                            Contents [Row, Col].Rune = Rune.ReplacementChar;
                        }
                        else if (!Clip.Contains (Col, Row))
                        {
                            // Our 1st column is outside the clip, so we can't display a wide character.
                            Contents [Row, Col + 1].Rune = Rune.ReplacementChar;
                        }
                        else
                        {
                            Contents [Row, Col].Rune = rune;

                            if (Col < clipRect.Right - 1)
                            {
                                // Invalidate cell to right so that it doesn't get drawn
                                // TODO: Figure out if it is better to show a replacement character or ' '
                                Contents [Row, Col + 1].Rune = Rune.ReplacementChar;
                                Contents [Row, Col + 1].IsDirty = true;
                            }
                        }
                    }
                    else
                    {
                        // This is a non-spacing character, so we don't need to do anything
                        Contents [Row, Col].Rune = (Rune)' ';
                        Contents [Row, Col].IsDirty = false;
                    }

                    DirtyLines [Row] = true;
                }
            }
        }

        if (runeWidth is < 0 or > 0)
        {
            Col++;
        }

        if (runeWidth > 1)
        {
            Debug.Assert (runeWidth <= 2);

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
                    //Contents [Row, Col].Rune = (Rune)' ';
                }
            }

            Col++;
        }
    }

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
    ///         <paramref name="str"/> required, unless the new column value is outside of the <see cref="Clip"/> or screen
    ///         dimensions defined by <see cref="Cols"/>.
    ///     </para>
    ///     <para>If <paramref name="str"/> requires more columns than are available, the output will be clipped.</para>
    /// </remarks>
    /// <param name="str">String.</param>
    public void AddStr (string str)
    {
        List<Rune> runes = str.EnumerateRunes ().ToList ();

        for (var i = 0; i < runes.Count; i++)
        {
            AddRune (runes [i]);
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
                        Rune = (Rune)' ',
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

    /// <summary>Tests whether the specified coordinate are valid for drawing the specified Rune.</summary>
    /// <param name="rune">Used to determine if one or two columns are required.</param>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>
    ///     <see langword="false"/> if the coordinate is outside the screen bounds or outside of <see cref="Clip"/>.
    ///     <see langword="true"/> otherwise.
    /// </returns>
    public bool IsValidLocation (Rune rune, int col, int row)
    {
        if (rune.GetColumns () < 2)
        {
            return col >= 0 && row >= 0 && col < Cols && row < Rows && Clip!.Contains (col, row);
        }

        return Clip!.Contains (col, row) || Clip!.Contains (col + 1, row);
    }

    /// <inheritdoc/>
    public void SetWindowSize (int cols, int rows)
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
                    if (!IsValidLocation (rune, c, r))
                    {
                        continue;
                    }

                    Contents [r, c] = new ()
                    {
                        Rune = rune != default (Rune) ? rune : (Rune)' ',
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

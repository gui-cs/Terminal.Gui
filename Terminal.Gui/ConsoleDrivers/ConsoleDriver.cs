#nullable enable

using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>Base class for Terminal.Gui IConsoleDriver implementations.</summary>
/// <remarks>
///     There are currently four implementations: - <see cref="CursesDriver"/> (for Unix and Mac) -
///     <see cref="WindowsDriver"/> - <see cref="NetDriver"/> that uses the .NET Console API - <see cref="FakeConsole"/>
///     for unit testing.
/// </remarks>
public abstract class ConsoleDriver : IConsoleDriver
{
    /// <summary>
    ///     Set this to true in any unit tests that attempt to test drivers other than FakeDriver.
    ///     <code>
    ///  public ColorTests ()
    ///  {
    ///    ConsoleDriver.RunningUnitTests = true;
    ///  }
    /// </code>
    /// </summary>
    internal static bool RunningUnitTests { get; set; }

    /// <summary>Get the operating system clipboard.</summary>
    public IClipboard? Clipboard { get; internal set; }

    /// <summary>Returns the name of the driver and relevant library version information.</summary>
    /// <returns></returns>
    public virtual string GetVersionInfo () { return GetType ().Name; }

    #region ANSI Esc Sequence Handling

    // QUESTION: This appears to be an API to help in debugging. It's only implemented in CursesDriver and WindowsDriver.
    // QUESTION: Can it be factored such that it does not contaminate the ConsoleDriver API?
    /// <summary>
    ///     Provide proper writing to send escape sequence recognized by the <see cref="ConsoleDriver"/>.
    /// </summary>
    /// <param name="ansi"></param>
    public abstract void WriteRaw (string ansi);

    #endregion ANSI Esc Sequence Handling

    #region Screen and Contents


    /// <summary>
    /// How long after Esc has been pressed before we give up on getting an Ansi escape sequence
    /// </summary>
    public TimeSpan EscTimeout { get; } = TimeSpan.FromMilliseconds (50);

    // As performance is a concern, we keep track of the dirty lines and only refresh those.
    // This is in addition to the dirty flag on each cell.
    internal bool []? _dirtyLines;

    // QUESTION: When non-full screen apps are supported, will this represent the app size, or will that be in Application?
    /// <summary>Gets the location and size of the terminal screen.</summary>
    public Rectangle Screen => new (0, 0, Cols, Rows);

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

    /// <summary>
    ///     Gets the column last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Col { get; private set; }

    /// <summary>The number of columns visible in the terminal.</summary>
    public virtual int Cols
    {
        get => _cols;
        set
        {
            _cols = value;
            ClearContents ();
        }
    }

    /// <summary>
    ///     The contents of the application output. The driver outputs this buffer to the terminal when
    ///     <see cref="UpdateScreen"/> is called.
    ///     <remarks>The format of the array is rows, columns. The first index is the row, the second index is the column.</remarks>
    /// </summary>
    public Cell [,]? Contents { get; set; }

    /// <summary>The leftmost column in the terminal.</summary>
    public virtual int Left { get; set; } = 0;

    /// <summary>Tests if the specified rune is supported by the driver.</summary>
    /// <param name="rune"></param>
    /// <returns>
    ///     <see langword="true"/> if the rune can be properly presented; <see langword="false"/> if the driver does not
    ///     support displaying this rune.
    /// </returns>
    public virtual bool IsRuneSupported (Rune rune) { return Rune.IsValid (rune.Value); }

    /// <summary>Tests whether the specified coordinate are valid for drawing.</summary>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>
    ///     <see langword="false"/> if the coordinate is outside the screen bounds or outside of <see cref="Clip"/>.
    ///     <see langword="true"/> otherwise.
    /// </returns>
    public bool IsValidLocation (int col, int row) { return col >= 0 && row >= 0 && col < Cols && row < Rows && Clip!.Contains (col, row); }

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

    /// <summary>
    ///     Gets the row last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Row { get; private set; }

    /// <summary>The number of rows visible in the terminal.</summary>
    public virtual int Rows
    {
        get => _rows;
        set
        {
            _rows = value;
            ClearContents ();
        }
    }

    /// <summary>The topmost row in the terminal.</summary>
    public virtual int Top { get; set; } = 0;

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
                            Contents [Row, Col+1].Rune = Rune.ReplacementChar;
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

                    _dirtyLines! [Row] = true;
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

    /// <summary>Fills the specified rectangle with the specified rune, using <see cref="CurrentAttribute"/></summary>
    /// <remarks>
    ///     The value of <see cref="Clip"/> is honored. Any parts of the rectangle not in the clip will not be drawn.
    /// </remarks>
    /// <param name="rect">The Screen-relative rectangle.</param>
    /// <param name="rune">The Rune used to fill the rectangle</param>
    public void FillRect (Rectangle rect, Rune rune = default)
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
                    Contents [r, c] = new Cell
                    {
                        Rune = rune != default ? rune : (Rune)' ',
                        Attribute = CurrentAttribute, IsDirty = true
                    };
                    _dirtyLines! [r] = true;
                }
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

        _dirtyLines = new bool [Rows];

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

                _dirtyLines [row] = true;
            }
        }

        ClearedContents?.Invoke (this, EventArgs.Empty);
    }

    /// <summary>
    ///     Raised each time <see cref="ClearContents"/> is called. For benchmarking.
    /// </summary>
    public event EventHandler<EventArgs>? ClearedContents;

    /// <summary>
    /// Sets <see cref="Contents"/> as dirty for situations where views
    /// don't need layout and redrawing, but just refresh the screen.
    /// </summary>
    protected void SetContentsAsDirty ()
    {
        lock (Contents!)
        {
            for (var row = 0; row < Rows; row++)
            {
                for (var c = 0; c < Cols; c++)
                {
                    Contents [row, c].IsDirty = true;
                }

                _dirtyLines! [row] = true;
            }
        }
    }

    /// <summary>
    ///     Fills the specified rectangle with the specified <see langword="char"/>. This method is a convenience method
    ///     that calls <see cref="FillRect(Rectangle, Rune)"/>.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="c"></param>
    public void FillRect (Rectangle rect, char c) { FillRect (rect, new Rune (c)); }

    #endregion Screen and Contents

    #region Cursor Handling

    /// <summary>Gets the terminal cursor visibility.</summary>
    /// <param name="visibility">The current <see cref="CursorVisibility"/></param>
    /// <returns><see langword="true"/> upon success</returns>
    public abstract bool GetCursorVisibility (out CursorVisibility visibility);

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
        else
        {

            return Clip!.Contains (col, row) || Clip!.Contains (col + 1, row);
        }
    }

    /// <summary>Called when the terminal size changes. Fires the <see cref="SizeChanged"/> event.</summary>
    /// <param name="args"></param>
    public void OnSizeChanged (SizeChangedEventArgs args) { SizeChanged?.Invoke (this, args); }

    /// <summary>Updates the screen to reflect all the changes that have been done to the display buffer</summary>
    public void Refresh ()
    {
        bool updated = UpdateScreen ();
        UpdateCursor ();

        Refreshed?.Invoke (this, new EventArgs<bool> (in updated));
    }

    /// <summary>
    ///     Raised each time <see cref="Refresh"/> is called. For benchmarking.
    /// </summary>
    public event EventHandler<EventArgs<bool>>? Refreshed;

    /// <summary>Sets the terminal cursor visibility.</summary>
    /// <param name="visibility">The wished <see cref="CursorVisibility"/></param>
    /// <returns><see langword="true"/> upon success</returns>
    public abstract bool SetCursorVisibility (CursorVisibility visibility);

    /// <summary>The event fired when the terminal is resized.</summary>
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    #endregion Cursor Handling

    /// <summary>Suspends the application (e.g. on Linux via SIGTSTP) and upon resume, resets the console driver.</summary>
    /// <remarks>This is only implemented in <see cref="CursesDriver"/>.</remarks>
    public abstract void Suspend ();

    /// <summary>Sets the position of the terminal cursor to <see cref="Col"/> and <see cref="Row"/>.</summary>
    public abstract void UpdateCursor ();

    /// <summary>Redraws the physical screen with the contents that have been queued up via any of the printing commands.</summary>
    /// <returns><see langword="true"/> if any updates to the screen were made.</returns>
    public abstract bool UpdateScreen ();

    #region Setup & Teardown

    /// <summary>Initializes the driver</summary>
    /// <returns>Returns an instance of <see cref="MainLoop"/> using the <see cref="IMainLoopDriver"/> for the driver.</returns>
    public abstract MainLoop Init ();

    /// <summary>Ends the execution of the console driver.</summary>
    public abstract void End ();

    #endregion

    #region Color Handling

    /// <summary>Gets whether the <see cref="IConsoleDriver"/> supports TrueColor output.</summary>
    public virtual bool SupportsTrueColor => true;

    // TODO: This makes IConsoleDriver dependent on Application, which is not ideal. This should be moved to Application.
    // BUGBUG: Application.Force16Colors should be bool? so if SupportsTrueColor and Application.Force16Colors == false, this doesn't override
    /// <summary>
    ///     Gets or sets whether the <see cref="IConsoleDriver"/> should use 16 colors instead of the default TrueColors.
    ///     See <see cref="Application.Force16Colors"/> to change this setting via <see cref="ConfigurationManager"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Will be forced to <see langword="true"/> if <see cref="IConsoleDriver.SupportsTrueColor"/> is
    ///         <see langword="false"/>, indicating that the <see cref="IConsoleDriver"/> cannot support TrueColor.
    ///     </para>
    /// </remarks>
    public virtual bool Force16Colors
    {
        get => Application.Force16Colors || !SupportsTrueColor;
        set => Application.Force16Colors = value || !SupportsTrueColor;
    }

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

    /// <summary>Selects the specified attribute as the attribute to use for future calls to AddRune and AddString.</summary>
    /// <remarks>Implementations should call <c>base.SetAttribute(c)</c>.</remarks>
    /// <param name="c">C.</param>
    public Attribute SetAttribute (Attribute c)
    {
        Attribute prevAttribute = CurrentAttribute;
        CurrentAttribute = c;

        return prevAttribute;
    }

    /// <summary>Gets the current <see cref="Attribute"/>.</summary>
    /// <returns>The current attribute.</returns>
    public Attribute GetAttribute () { return CurrentAttribute; }

    // TODO: This is only overridden by CursesDriver. Once CursesDriver supports 24-bit color, this virtual method can be
    // removed (and Attribute can lose the platformColor property).
    /// <summary>Makes an <see cref="Attribute"/>.</summary>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <returns>The attribute for the foreground and background colors.</returns>
    public virtual Attribute MakeColor (in Color foreground, in Color background)
    {
        // Encode the colors into the int value.
        return new (
                    -1, // only used by cursesdriver!
                    foreground,
                    background
                   );
    }

    #endregion Color Handling

    #region Mouse Handling

    /// <summary>Event fired when a mouse event occurs.</summary>
    public event EventHandler<MouseEventArgs>? MouseEvent;

    /// <summary>Called when a mouse event occurs. Fires the <see cref="MouseEvent"/> event.</summary>
    /// <param name="a"></param>
    public void OnMouseEvent (MouseEventArgs a)
    {
        // Ensure ScreenPosition is set
        a.ScreenPosition = a.Position;

        MouseEvent?.Invoke (this, a);
    }

    #endregion Mouse Handling

    #region Keyboard Handling

    /// <summary>Event fired when a key is pressed down. This is a precursor to <see cref="KeyUp"/>.</summary>
    public event EventHandler<Key>? KeyDown;

    /// <summary>
    ///     Called when a key is pressed down. Fires the <see cref="KeyDown"/> event. This is a precursor to
    ///     <see cref="OnKeyUp"/>.
    /// </summary>
    /// <param name="a"></param>
    public void OnKeyDown (Key a) { KeyDown?.Invoke (this, a); }

    /// <summary>Event fired when a key is released.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will fire this event after <see cref="KeyDown"/> processing is
    ///     complete.
    /// </remarks>
    public event EventHandler<Key>? KeyUp;

    /// <summary>Called when a key is released. Fires the <see cref="KeyUp"/> event.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will call this method after <see cref="OnKeyDown"/> processing
    ///     is complete.
    /// </remarks>
    /// <param name="a"></param>
    public void OnKeyUp (Key a) { KeyUp?.Invoke (this, a); }

    // TODO: Remove this API - it was needed when we didn't have a reliable way to simulate key presses.
    // TODO: We now do: Applicaiton.RaiseKeyDown and Application.RaiseKeyUp
    /// <summary>Simulates a key press.</summary>
    /// <param name="keyChar">The key character.</param>
    /// <param name="key">The key.</param>
    /// <param name="shift">If <see langword="true"/> simulates the Shift key being pressed.</param>
    /// <param name="alt">If <see langword="true"/> simulates the Alt key being pressed.</param>
    /// <param name="ctrl">If <see langword="true"/> simulates the Ctrl key being pressed.</param>
    public abstract void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool ctrl);

    #endregion

    private AnsiRequestScheduler? _scheduler;

    /// <summary>
    /// Queues the given <paramref name="request"/> for execution
    /// </summary>
    /// <param name="request"></param>
    public void QueueAnsiRequest (AnsiEscapeSequenceRequest request)
    {
        GetRequestScheduler ().SendOrSchedule (request);
    }

    internal abstract IAnsiResponseParser GetParser ();

    /// <summary>
    ///     Gets the <see cref="AnsiRequestScheduler"/> for this <see cref="ConsoleDriver"/>.
    /// </summary>
    /// <returns></returns>
    public AnsiRequestScheduler GetRequestScheduler ()
    {
        // Lazy initialization because GetParser is virtual
        return _scheduler ??= new (GetParser ());
    }

}
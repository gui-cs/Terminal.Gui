#nullable enable
//
// ConsoleDriver.cs: Base class for Terminal.Gui ConsoleDriver implementations.
//

using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>Base class for Terminal.Gui ConsoleDriver implementations.</summary>
/// <remarks>
///     There are currently four implementations: - <see cref="CursesDriver"/> (for Unix and Mac) -
///     <see cref="WindowsDriver"/> - <see cref="NetDriver"/> that uses the .NET Console API - <see cref="FakeConsole"/>
///     for unit testing.
/// </remarks>
public abstract class ConsoleDriver
{
    // As performance is a concern, we keep track of the dirty lines and only refresh those.
    // This is in addition to the dirty flag on each cell.
    internal bool []? _dirtyLines;

    // QUESTION: When non-full screen apps are supported, will this represent the app size, or will that be in Application?
    /// <summary>Gets the location and size of the terminal screen.</summary>
    internal Rectangle Screen => new (0, 0, Cols, Rows);

    private Rectangle _clip;

    /// <summary>
    ///     Gets or sets the clip rectangle that <see cref="AddRune(Rune)"/> and <see cref="AddStr(string)"/> are subject
    ///     to.
    /// </summary>
    /// <value>The rectangle describing the of <see cref="Clip"/> region.</value>
    public Rectangle Clip
    {
        get => _clip;
        set
        {
            if (_clip == value)
            {
                return;
            }

            // Don't ever let Clip be bigger than Screen
            _clip = Rectangle.Intersect (Screen, value);
        }
    }

    /// <summary>Get the operating system clipboard.</summary>
    public IClipboard? Clipboard { get; internal set; }

    /// <summary>
    ///     Gets the column last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Col { get; internal set; }

    /// <summary>The number of columns visible in the terminal.</summary>
    public virtual int Cols
    {
        get => _cols;
        internal set
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
    public Cell [,]? Contents { get; internal set; }

    /// <summary>The leftmost column in the terminal.</summary>
    public virtual int Left { get; internal set; } = 0;

    /// <summary>
    ///     Gets the row last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Row { get; internal set; }

    /// <summary>The number of rows visible in the terminal.</summary>
    public virtual int Rows
    {
        get => _rows;
        internal set
        {
            _rows = value;
            ClearContents ();
        }
    }

    /// <summary>The topmost row in the terminal.</summary>
    public virtual int Top { get; internal set; } = 0;

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
        bool validLocation = IsValidLocation (Col, Row);

        if (Contents is null)
        {
            return;
        }

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

                        if (Col < Clip.Right - 1)
                        {
                            Contents [Row, Col + 1].IsDirty = true;
                        }
                    }
                    else if (runeWidth == 2)
                    {
                        if (Col == Clip.Right - 1)
                        {
                            // We're at the right edge of the clip, so we can't display a wide character.
                            // TODO: Figure out if it is better to show a replacement character or ' '
                            Contents [Row, Col].Rune = Rune.ReplacementChar;
                        }
                        else
                        {
                            Contents [Row, Col].Rune = rune;

                            if (Col < Clip.Right - 1)
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

            if (validLocation && Col < Clip.Right)
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
        Clip = Screen;
        _dirtyLines = new bool [Rows];

        lock (Contents)
        {
            for (var row = 0; row < Rows; row++)
            {
                for (var c = 0; c < Cols; c++)
                {
                    Contents [row, c] = new Cell
                    {
                        Rune = (Rune)' ',
                        Attribute = new Attribute (Color.White, Color.Black),
                        IsDirty = true
                    };
                }
                _dirtyLines [row] = true;
            }
        }
    }

    /// <summary>
    /// Sets <see cref="Contents"/> as dirty for situations where views
    /// don't need layout and redrawing, but just refresh the screen.
    /// </summary>
    public void SetContentsAsDirty ()
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

    /// <summary>Determines if the terminal cursor should be visible or not and sets it accordingly.</summary>
    /// <returns><see langword="true"/> upon success</returns>
    public abstract bool EnsureCursorVisibility ();

    /// <summary>Fills the specified rectangle with the specified rune, using <see cref="CurrentAttribute"/></summary>
    /// <remarks>
    /// The value of <see cref="Clip"/> is honored. Any parts of the rectangle not in the clip will not be drawn.
    /// </remarks>
    /// <param name="rect">The Screen-relative rectangle.</param>
    /// <param name="rune">The Rune used to fill the rectangle</param>
    public void FillRect (Rectangle rect, Rune rune = default)
    {
        rect = Rectangle.Intersect (rect, Clip);
        lock (Contents!)
        {
            for (int r = rect.Y; r < rect.Y + rect.Height; r++)
            {
                for (int c = rect.X; c < rect.X + rect.Width; c++)
                {
                    Contents [r, c] = new Cell
                    {
                        Rune = (rune != default ? rune : (Rune)' '),
                        Attribute = CurrentAttribute, IsDirty = true
                    };
                    _dirtyLines! [r] = true;
                }
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

    /// <summary>Gets the terminal cursor visibility.</summary>
    /// <param name="visibility">The current <see cref="CursorVisibility"/></param>
    /// <returns><see langword="true"/> upon success</returns>
    public abstract bool GetCursorVisibility (out CursorVisibility visibility);

    /// <summary>Returns the name of the driver and relevant library version information.</summary>
    /// <returns></returns>
    public virtual string GetVersionInfo () { return GetType ().Name; }

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
    public bool IsValidLocation (int col, int row)
    {
        return col >= 0 && row >= 0 && col < Cols && row < Rows && Clip.Contains (col, row);
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
    public virtual void Move (int col, int row)
    {
        //Debug.Assert (col >= 0 && row >= 0 && col < Contents.GetLength(1) && row < Contents.GetLength(0));
        Col = col;
        Row = row;
    }

    /// <summary>Called when the terminal size changes. Fires the <see cref="SizeChanged"/> event.</summary>
    /// <param name="args"></param>
    public void OnSizeChanged (SizeChangedEventArgs args) { SizeChanged?.Invoke (this, args); }

    /// <summary>Updates the screen to reflect all the changes that have been done to the display buffer</summary>
    public abstract void Refresh ();

    /// <summary>Sets the terminal cursor visibility.</summary>
    /// <param name="visibility">The wished <see cref="CursorVisibility"/></param>
    /// <returns><see langword="true"/> upon success</returns>
    public abstract bool SetCursorVisibility (CursorVisibility visibility);

    /// <summary>The event fired when the terminal is resized.</summary>
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <summary>Suspends the application (e.g. on Linux via SIGTSTP) and upon resume, resets the console driver.</summary>
    /// <remarks>This is only implemented in <see cref="CursesDriver"/>.</remarks>
    public abstract void Suspend ();

    /// <summary>Sets the position of the terminal cursor to <see cref="Col"/> and <see cref="Row"/>.</summary>
    public abstract void UpdateCursor ();

    /// <summary>Redraws the physical screen with the contents that have been queued up via any of the printing commands.</summary>
    public abstract void UpdateScreen ();

    #region Setup & Teardown

    /// <summary>Initializes the driver</summary>
    /// <returns>Returns an instance of <see cref="MainLoop"/> using the <see cref="IMainLoopDriver"/> for the driver.</returns>
    internal abstract MainLoop Init ();

    /// <summary>Ends the execution of the console driver.</summary>
    internal abstract void End ();

    #endregion

    #region Color Handling

    /// <summary>Gets whether the <see cref="ConsoleDriver"/> supports TrueColor output.</summary>
    public virtual bool SupportsTrueColor => true;

    // TODO: This makes ConsoleDriver dependent on Application, which is not ideal. This should be moved to Application.
    // BUGBUG: Application.Force16Colors should be bool? so if SupportsTrueColor and Application.Force16Colors == false, this doesn't override
    /// <summary>
    ///     Gets or sets whether the <see cref="ConsoleDriver"/> should use 16 colors instead of the default TrueColors.
    ///     See <see cref="Application.Force16Colors"/> to change this setting via <see cref="ConfigurationManager"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Will be forced to <see langword="true"/> if <see cref="ConsoleDriver.SupportsTrueColor"/> is
    ///         <see langword="false"/>, indicating that the <see cref="ConsoleDriver"/> cannot support TrueColor.
    ///     </para>
    /// </remarks>
    internal virtual bool Force16Colors
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
            // TODO: This makes ConsoleDriver dependent on Application, which is not ideal. Once Attribute.PlatformColor is removed, this can be fixed.
            if (Application.Driver is { })
            {
                _currentAttribute = new Attribute (value.Foreground, value.Background);

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
        return new Attribute (
                              -1, // only used by cursesdriver!
                              foreground,
                              background
                             );
    }

    #endregion

    #region Mouse and Keyboard

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

    /// <summary>Simulates a key press.</summary>
    /// <param name="keyChar">The key character.</param>
    /// <param name="key">The key.</param>
    /// <param name="shift">If <see langword="true"/> simulates the Shift key being pressed.</param>
    /// <param name="alt">If <see langword="true"/> simulates the Alt key being pressed.</param>
    /// <param name="ctrl">If <see langword="true"/> simulates the Ctrl key being pressed.</param>
    public abstract void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool ctrl);

    #endregion
}

/// <summary>
///     The <see cref="KeyCode"/> enumeration encodes key information from <see cref="ConsoleDriver"/>s and provides a
///     consistent way for application code to specify keys and receive key events.
///     <para>
///         The <see cref="Key"/> class provides a higher-level abstraction, with helper methods and properties for
///         common operations. For example, <see cref="Key.IsAlt"/> and <see cref="Key.IsCtrl"/> provide a convenient way
///         to check whether the Alt or Ctrl modifier keys were pressed when a key was pressed.
///     </para>
/// </summary>
/// <remarks>
///     <para>
///         Lowercase alpha keys are encoded as values between 65 and 90 corresponding to the un-shifted A to Z keys on a
///         keyboard. Enum values are provided for these (e.g. <see cref="KeyCode.A"/>, <see cref="KeyCode.B"/>, etc.).
///         Even though the values are the same as the ASCII values for uppercase characters, these enum values represent
///         *lowercase*, un-shifted characters.
///     </para>
///     <para>
///         Numeric keys are the values between 48 and 57 corresponding to 0 to 9 (e.g. <see cref="KeyCode.D0"/>,
///         <see cref="KeyCode.D1"/>, etc.).
///     </para>
///     <para>
///         The shift modifiers (<see cref="KeyCode.ShiftMask"/>, <see cref="KeyCode.CtrlMask"/>, and
///         <see cref="KeyCode.AltMask"/>) can be combined (with logical or) with the other key codes to represent shifted
///         keys. For example, the <see cref="KeyCode.A"/> enum value represents the un-shifted 'a' key, while
///         <see cref="KeyCode.ShiftMask"/> | <see cref="KeyCode.A"/> represents the 'A' key (shifted 'a' key). Likewise,
///         <see cref="KeyCode.AltMask"/> | <see cref="KeyCode.A"/> represents the 'Alt+A' key combination.
///     </para>
///     <para>
///         All other keys that produce a printable character are encoded as the Unicode value of the character. For
///         example, the <see cref="KeyCode"/> for the '!' character is 33, which is the Unicode value for '!'. Likewise,
///         `â` is 226, `Â` is 194, etc.
///     </para>
///     <para>
///         If the <see cref="SpecialMask"/> is set, then the value is that of the special mask, otherwise, the value is
///         the one of the lower bits (as extracted by <see cref="CharMask"/>).
///     </para>
/// </remarks>
[Flags]
public enum KeyCode : uint
{
    /// <summary>
    ///     Mask that indicates that the key is a unicode codepoint. Values outside this range indicate the key has shift
    ///     modifiers or is a special key like function keys, arrows keys and so on.
    /// </summary>
    CharMask = 0x_f_ffff,

    /// <summary>
    ///     If the <see cref="SpecialMask"/> is set, then the value is that of the special mask, otherwise, the value is
    ///     in the lower bits (as extracted by <see cref="CharMask"/>).
    /// </summary>
    SpecialMask = 0x_fff0_0000,

    /// <summary>
    ///     When this value is set, the Key encodes the sequence Shift-KeyValue. The actual value must be extracted by
    ///     removing the ShiftMask.
    /// </summary>
    ShiftMask = 0x_1000_0000,

    /// <summary>
    ///     When this value is set, the Key encodes the sequence Alt-KeyValue. The actual value must be extracted by
    ///     removing the AltMask.
    /// </summary>
    AltMask = 0x_8000_0000,

    /// <summary>
    ///     When this value is set, the Key encodes the sequence Ctrl-KeyValue. The actual value must be extracted by
    ///     removing the CtrlMask.
    /// </summary>
    CtrlMask = 0x_4000_0000,

    /// <summary>The key code representing an invalid or empty key.</summary>
    Null = 0,

    /// <summary>Backspace key.</summary>
    Backspace = 8,

    /// <summary>The key code for the tab key (forwards tab key).</summary>
    Tab = 9,

    /// <summary>The key code for the return key.</summary>
    Enter = ConsoleKey.Enter,

    /// <summary>The key code for the clear key.</summary>
    Clear = 12,

    /// <summary>The key code for the escape key.</summary>
    Esc = 27,

    /// <summary>The key code for the space bar key.</summary>
    Space = 32,

    /// <summary>Digit 0.</summary>
    D0 = 48,

    /// <summary>Digit 1.</summary>
    D1,

    /// <summary>Digit 2.</summary>
    D2,

    /// <summary>Digit 3.</summary>
    D3,

    /// <summary>Digit 4.</summary>
    D4,

    /// <summary>Digit 5.</summary>
    D5,

    /// <summary>Digit 6.</summary>
    D6,

    /// <summary>Digit 7.</summary>
    D7,

    /// <summary>Digit 8.</summary>
    D8,

    /// <summary>Digit 9.</summary>
    D9,

    /// <summary>The key code for the A key</summary>
    A = 65,

    /// <summary>The key code for the B key</summary>
    B,

    /// <summary>The key code for the C key</summary>
    C,

    /// <summary>The key code for the D key</summary>
    D,

    /// <summary>The key code for the E key</summary>
    E,

    /// <summary>The key code for the F key</summary>
    F,

    /// <summary>The key code for the G key</summary>
    G,

    /// <summary>The key code for the H key</summary>
    H,

    /// <summary>The key code for the I key</summary>
    I,

    /// <summary>The key code for the J key</summary>
    J,

    /// <summary>The key code for the K key</summary>
    K,

    /// <summary>The key code for the L key</summary>
    L,

    /// <summary>The key code for the M key</summary>
    M,

    /// <summary>The key code for the N key</summary>
    N,

    /// <summary>The key code for the O key</summary>
    O,

    /// <summary>The key code for the P key</summary>
    P,

    /// <summary>The key code for the Q key</summary>
    Q,

    /// <summary>The key code for the R key</summary>
    R,

    /// <summary>The key code for the S key</summary>
    S,

    /// <summary>The key code for the T key</summary>
    T,

    /// <summary>The key code for the U key</summary>
    U,

    /// <summary>The key code for the V key</summary>
    V,

    /// <summary>The key code for the W key</summary>
    W,

    /// <summary>The key code for the X key</summary>
    X,

    /// <summary>The key code for the Y key</summary>
    Y,

    /// <summary>The key code for the Z key</summary>
    Z,

    ///// <summary>
    ///// The key code for the Delete key.
    ///// </summary>
    //Delete = 127,

    // --- Special keys ---
    // The values below are common non-alphanum keys. Their values are
    // based on the .NET ConsoleKey values, which, in-turn are based on the
    // VK_ values from the Windows API.
    // We add MaxCodePoint to avoid conflicts with the Unicode values.

    /// <summary>The maximum Unicode codepoint value. Used to encode the non-alphanumeric control keys.</summary>
    MaxCodePoint = 0x10FFFF,

    /// <summary>Cursor up key</summary>
    CursorUp = MaxCodePoint + ConsoleKey.UpArrow,

    /// <summary>Cursor down key.</summary>
    CursorDown = MaxCodePoint + ConsoleKey.DownArrow,

    /// <summary>Cursor left key.</summary>
    CursorLeft = MaxCodePoint + ConsoleKey.LeftArrow,

    /// <summary>Cursor right key.</summary>
    CursorRight = MaxCodePoint + ConsoleKey.RightArrow,

    /// <summary>Page Up key.</summary>
    PageUp = MaxCodePoint + ConsoleKey.PageUp,

    /// <summary>Page Down key.</summary>
    PageDown = MaxCodePoint + ConsoleKey.PageDown,

    /// <summary>Home key.</summary>
    Home = MaxCodePoint + ConsoleKey.Home,

    /// <summary>End key.</summary>
    End = MaxCodePoint + ConsoleKey.End,

    /// <summary>Insert (INS) key.</summary>
    Insert = MaxCodePoint + ConsoleKey.Insert,

    /// <summary>Delete (DEL) key.</summary>
    Delete = MaxCodePoint + ConsoleKey.Delete,

    /// <summary>Print screen character key.</summary>
    PrintScreen = MaxCodePoint + ConsoleKey.PrintScreen,

    /// <summary>F1 key.</summary>
    F1 = MaxCodePoint + ConsoleKey.F1,

    /// <summary>F2 key.</summary>
    F2 = MaxCodePoint + ConsoleKey.F2,

    /// <summary>F3 key.</summary>
    F3 = MaxCodePoint + ConsoleKey.F3,

    /// <summary>F4 key.</summary>
    F4 = MaxCodePoint + ConsoleKey.F4,

    /// <summary>F5 key.</summary>
    F5 = MaxCodePoint + ConsoleKey.F5,

    /// <summary>F6 key.</summary>
    F6 = MaxCodePoint + ConsoleKey.F6,

    /// <summary>F7 key.</summary>
    F7 = MaxCodePoint + ConsoleKey.F7,

    /// <summary>F8 key.</summary>
    F8 = MaxCodePoint + ConsoleKey.F8,

    /// <summary>F9 key.</summary>
    F9 = MaxCodePoint + ConsoleKey.F9,

    /// <summary>F10 key.</summary>
    F10 = MaxCodePoint + ConsoleKey.F10,

    /// <summary>F11 key.</summary>
    F11 = MaxCodePoint + ConsoleKey.F11,

    /// <summary>F12 key.</summary>
    F12 = MaxCodePoint + ConsoleKey.F12,

    /// <summary>F13 key.</summary>
    F13 = MaxCodePoint + ConsoleKey.F13,

    /// <summary>F14 key.</summary>
    F14 = MaxCodePoint + ConsoleKey.F14,

    /// <summary>F15 key.</summary>
    F15 = MaxCodePoint + ConsoleKey.F15,

    /// <summary>F16 key.</summary>
    F16 = MaxCodePoint + ConsoleKey.F16,

    /// <summary>F17 key.</summary>
    F17 = MaxCodePoint + ConsoleKey.F17,

    /// <summary>F18 key.</summary>
    F18 = MaxCodePoint + ConsoleKey.F18,

    /// <summary>F19 key.</summary>
    F19 = MaxCodePoint + ConsoleKey.F19,

    /// <summary>F20 key.</summary>
    F20 = MaxCodePoint + ConsoleKey.F20,

    /// <summary>F21 key.</summary>
    F21 = MaxCodePoint + ConsoleKey.F21,

    /// <summary>F22 key.</summary>
    F22 = MaxCodePoint + ConsoleKey.F22,

    /// <summary>F23 key.</summary>
    F23 = MaxCodePoint + ConsoleKey.F23,

    /// <summary>F24 key.</summary>
    F24 = MaxCodePoint + ConsoleKey.F24
}

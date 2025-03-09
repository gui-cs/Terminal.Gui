#nullable enable
namespace Terminal.Gui;

/// <summary>Base interface for Terminal.Gui ConsoleDriver implementations.</summary>
/// <remarks>
///     There are currently four implementations: - <see cref="CursesDriver"/> (for Unix and Mac) -
///     <see cref="WindowsDriver"/> - <see cref="NetDriver"/> that uses the .NET Console API - <see cref="FakeConsole"/>
///     for unit testing.
/// </remarks>
public interface IConsoleDriver
{
    /// <summary>Get the operating system clipboard.</summary>
    IClipboard? Clipboard { get; }

    /// <summary>Gets the location and size of the terminal screen.</summary>
    Rectangle Screen { get; }

    /// <summary>
    ///     Gets or sets the clip rectangle that <see cref="AddRune(Rune)"/> and <see cref="AddStr(string)"/> are subject
    ///     to.
    /// </summary>
    /// <value>The rectangle describing the of <see cref="Clip"/> region.</value>
    Region? Clip { get; set; }


    /// <summary>
    ///     Gets the column last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    int Col { get; }

    /// <summary>The number of columns visible in the terminal.</summary>
    int Cols { get; set; }

    // BUGBUG: This should not be publicly settable.
    /// <summary>
    ///     Gets or sets the contents of the application output. The driver outputs this buffer to the terminal.
    ///     <remarks>The format of the array is rows, columns. The first index is the row, the second index is the column.</remarks>
    /// </summary>
    Cell [,]? Contents { get; set; }

    /// <summary>The leftmost column in the terminal.</summary>
    int Left { get; set; }

    /// <summary>
    ///     Gets the row last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    int Row { get; }

    /// <summary>The number of rows visible in the terminal.</summary>
    int Rows { get; set; }

    /// <summary>The topmost row in the terminal.</summary>
    int Top { get; set; }

    /// <summary>Gets whether the <see cref="ConsoleDriver"/> supports TrueColor output.</summary>
    bool SupportsTrueColor { get; }

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
    bool Force16Colors { get; set; }

    /// <summary>
    ///     The <see cref="Attribute"/> that will be used for the next <see cref="AddRune(Rune)"/> or <see cref="AddStr"/>
    ///     call.
    /// </summary>
    Attribute CurrentAttribute { get; set; }

    /// <summary>Returns the name of the driver and relevant library version information.</summary>
    /// <returns></returns>
    string GetVersionInfo ();

    /// <summary>
    ///     Provide proper writing to send escape sequence recognized by the <see cref="ConsoleDriver"/>.
    /// </summary>
    /// <param name="ansi"></param>
    void WriteRaw (string ansi);

    /// <summary>Tests if the specified rune is supported by the driver.</summary>
    /// <param name="rune"></param>
    /// <returns>
    ///     <see langword="true"/> if the rune can be properly presented; <see langword="false"/> if the driver does not
    ///     support displaying this rune.
    /// </returns>
    bool IsRuneSupported (Rune rune);

    /// <summary>Tests whether the specified coordinate are valid for drawing the specified Rune.</summary>
    /// <param name="rune">Used to determine if one or two columns are required.</param>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>
    ///     <see langword="false"/> if the coordinate is outside the screen bounds or outside of
    ///     <see cref="ConsoleDriver.Clip"/>.
    ///     <see langword="true"/> otherwise.
    /// </returns>
    bool IsValidLocation (Rune rune, int col, int row);

    /// <summary>
    ///     Updates <see cref="ConsoleDriver.Col"/> and <see cref="ConsoleDriver.Row"/> to the specified column and row in
    ///     <see cref="ConsoleDriver.Contents"/>.
    ///     Used by <see cref="ConsoleDriver.AddRune(System.Text.Rune)"/> and <see cref="ConsoleDriver.AddStr"/> to determine
    ///     where to add content.
    /// </summary>
    /// <remarks>
    ///     <para>This does not move the cursor on the screen, it only updates the internal state of the driver.</para>
    ///     <para>
    ///         If <paramref name="col"/> or <paramref name="row"/> are negative or beyond  <see cref="ConsoleDriver.Cols"/>
    ///         and
    ///         <see cref="ConsoleDriver.Rows"/>, the method still sets those properties.
    ///     </para>
    /// </remarks>
    /// <param name="col">Column to move to.</param>
    /// <param name="row">Row to move to.</param>
    void Move (int col, int row);

    /// <summary>Adds the specified rune to the display at the current cursor position.</summary>
    /// <remarks>
    ///     <para>
    ///         When the method returns, <see cref="ConsoleDriver.Col"/> will be incremented by the number of columns
    ///         <paramref name="rune"/> required, even if the new column value is outside of the
    ///         <see cref="ConsoleDriver.Clip"/> or screen
    ///         dimensions defined by <see cref="ConsoleDriver.Cols"/>.
    ///     </para>
    ///     <para>
    ///         If <paramref name="rune"/> requires more than one column, and <see cref="ConsoleDriver.Col"/> plus the number
    ///         of columns
    ///         needed exceeds the <see cref="ConsoleDriver.Clip"/> or screen dimensions, the default Unicode replacement
    ///         character (U+FFFD)
    ///         will be added instead.
    ///     </para>
    /// </remarks>
    /// <param name="rune">Rune to add.</param>
    void AddRune (Rune rune);

    /// <summary>
    ///     Adds the specified <see langword="char"/> to the display at the current cursor position. This method is a
    ///     convenience method that calls <see cref="ConsoleDriver.AddRune(System.Text.Rune)"/> with the <see cref="Rune"/>
    ///     constructor.
    /// </summary>
    /// <param name="c">Character to add.</param>
    void AddRune (char c);

    /// <summary>Adds the <paramref name="str"/> to the display at the cursor position.</summary>
    /// <remarks>
    ///     <para>
    ///         When the method returns, <see cref="ConsoleDriver.Col"/> will be incremented by the number of columns
    ///         <paramref name="str"/> required, unless the new column value is outside of the <see cref="ConsoleDriver.Clip"/>
    ///         or screen
    ///         dimensions defined by <see cref="ConsoleDriver.Cols"/>.
    ///     </para>
    ///     <para>If <paramref name="str"/> requires more columns than are available, the output will be clipped.</para>
    /// </remarks>
    /// <param name="str">String.</param>
    void AddStr (string str);

    /// <summary>Clears the <see cref="ConsoleDriver.Contents"/> of the driver.</summary>
    void ClearContents ();

    /// <summary>
    ///     Fills the specified rectangle with the specified rune, using <see cref="ConsoleDriver.CurrentAttribute"/>
    /// </summary>
    event EventHandler<EventArgs> ClearedContents;

    /// <summary>Fills the specified rectangle with the specified rune, using <see cref="ConsoleDriver.CurrentAttribute"/></summary>
    /// <remarks>
    ///     The value of <see cref="ConsoleDriver.Clip"/> is honored. Any parts of the rectangle not in the clip will not be
    ///     drawn.
    /// </remarks>
    /// <param name="rect">The Screen-relative rectangle.</param>
    /// <param name="rune">The Rune used to fill the rectangle</param>
    void FillRect (Rectangle rect, Rune rune = default);

    /// <summary>
    ///     Fills the specified rectangle with the specified <see langword="char"/>. This method is a convenience method
    ///     that calls <see cref="ConsoleDriver.FillRect(System.Drawing.Rectangle,System.Text.Rune)"/>.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="c"></param>
    void FillRect (Rectangle rect, char c);


    /// <summary>Gets the terminal cursor visibility.</summary>
    /// <param name="visibility">The current <see cref="CursorVisibility"/></param>
    /// <returns><see langword="true"/> upon success</returns>
    bool GetCursorVisibility (out CursorVisibility visibility);

    /// <summary>Updates the screen to reflect all the changes that have been done to the display buffer</summary>
    void Refresh ();

    /// <summary>Sets the terminal cursor visibility.</summary>
    /// <param name="visibility">The wished <see cref="CursorVisibility"/></param>
    /// <returns><see langword="true"/> upon success</returns>
    bool SetCursorVisibility (CursorVisibility visibility);

    /// <summary>The event fired when the terminal is resized.</summary>
    event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <summary>Suspends the application (e.g. on Linux via SIGTSTP) and upon resume, resets the console driver.</summary>
    /// <remarks>This is only implemented in <see cref="CursesDriver"/>.</remarks>
    void Suspend ();

    /// <summary>
    ///     Sets the position of the terminal cursor to <see cref="ConsoleDriver.Col"/> and
    ///     <see cref="ConsoleDriver.Row"/>.
    /// </summary>
    void UpdateCursor ();

    /// <summary>Initializes the driver</summary>
    /// <returns>Returns an instance of <see cref="MainLoop"/> using the <see cref="IMainLoopDriver"/> for the driver.</returns>
    MainLoop Init ();

    /// <summary>Ends the execution of the console driver.</summary>
    void End ();

    /// <summary>Selects the specified attribute as the attribute to use for future calls to AddRune and AddString.</summary>
    /// <remarks>Implementations should call <c>base.SetAttribute(c)</c>.</remarks>
    /// <param name="c">C.</param>
    Attribute SetAttribute (Attribute c);

    /// <summary>Gets the current <see cref="Attribute"/>.</summary>
    /// <returns>The current attribute.</returns>
    Attribute GetAttribute ();

    /// <summary>Makes an <see cref="Attribute"/>.</summary>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <returns>The attribute for the foreground and background colors.</returns>
    Attribute MakeColor (in Color foreground, in Color background);

    /// <summary>Event fired when a mouse event occurs.</summary>
    event EventHandler<MouseEventArgs>? MouseEvent;

    /// <summary>Event fired when a key is pressed down. This is a precursor to <see cref="ConsoleDriver.KeyUp"/>.</summary>
    event EventHandler<Key>? KeyDown;

    /// <summary>Event fired when a key is released.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will fire this event after <see cref="ConsoleDriver.KeyDown"/>
    ///     processing is
    ///     complete.
    /// </remarks>
    event EventHandler<Key>? KeyUp;

    /// <summary>Simulates a key press.</summary>
    /// <param name="keyChar">The key character.</param>
    /// <param name="key">The key.</param>
    /// <param name="shift">If <see langword="true"/> simulates the Shift key being pressed.</param>
    /// <param name="alt">If <see langword="true"/> simulates the Alt key being pressed.</param>
    /// <param name="ctrl">If <see langword="true"/> simulates the Ctrl key being pressed.</param>
    void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool ctrl);

    /// <summary>
    ///     Queues the given <paramref name="request"/> for execution
    /// </summary>
    /// <param name="request"></param>
    public void QueueAnsiRequest (AnsiEscapeSequenceRequest request);

    /// <summary>
    ///     Gets the <see cref="AnsiRequestScheduler"/> for the driver
    /// </summary>
    /// <returns></returns>
    public AnsiRequestScheduler GetRequestScheduler ();
}

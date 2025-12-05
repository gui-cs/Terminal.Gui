namespace Terminal.Gui.Drivers;

/// <summary>Base interface for Terminal.Gui Driver implementations.</summary>
/// <remarks>
///     There are currently four implementations: UnixDriver, WindowsDriver, DotNetDriver, and FakeDriver
/// </remarks>
public interface IDriver
{
    /// <summary>
    ///     Gets the name of the driver implementation.
    /// </summary>
    string? GetName ();

    /// <summary>
    ///     Class responsible for processing native driver input objects
    ///     e.g. <see cref="ConsoleKeyInfo"/> into <see cref="Key"/> events
    ///     and detecting and processing ansi escape sequences.
    /// </summary>
    IInputProcessor InputProcessor { get; }

    /// <summary>
    ///     Describes the desired screen state. Data source for <see cref="IOutput"/>.
    /// </summary>
    IOutputBuffer OutputBuffer { get; }

    /// <summary>
    ///     Interface for classes responsible for reporting the current
    ///     size of the terminal window.
    /// </summary>
    ISizeMonitor SizeMonitor { get; }

    /// <summary>Get the operating system clipboard.</summary>
    IClipboard? Clipboard { get; }

    /// <summary>Gets the location and size of the terminal screen.</summary>
    Rectangle Screen { get; }

    /// <summary>
    ///     Sets the screen size. <see cref="Screen"/> is the source of truth for screen dimensions.
    /// </summary>
    /// <param name="width">The new width in columns.</param>
    /// <param name="height">The new height in rows.</param>
    void SetScreenSize (int width, int height);

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

    /// <summary>Gets whether the <see cref="IDriver"/> supports TrueColor output.</summary>
    bool SupportsTrueColor { get; }

    /// <summary>
    ///     Gets or sets whether the <see cref="IDriver"/> should use 16 colors instead of the default TrueColors.
    ///     See <see cref="Application.Force16Colors"/> to change this setting via <see cref="ConfigurationManager"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Will be forced to <see langword="true"/> if <see cref="IDriver.SupportsTrueColor"/> is
    ///         <see langword="false"/>, indicating that the <see cref="IDriver"/> cannot support TrueColor.
    ///     </para>
    /// </remarks>
    bool Force16Colors { get; set; }

    /// <summary>
    ///     The <see cref="System.Attribute"/> that will be used for the next <see cref="AddRune(Rune)"/> or
    ///     <see cref="AddStr"/>
    ///     call.
    /// </summary>
    Attribute CurrentAttribute { get; set; }

    /// <summary>Returns the name of the driver and relevant library version information.</summary>
    /// <returns></returns>
    string GetVersionInfo ();

    /// <summary>
    ///     Provide proper writing to send escape sequence recognized by the <see cref="IDriver"/>.
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

    /// <summary>Tests whether the specified coordinate are valid for drawing the specified Text.</summary>
    /// <param name="text">Used to determine if one or two columns are required.</param>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>
    ///     <see langword="false"/> if the coordinate is outside the screen bounds or outside of
    ///     <see cref="IDriver.Clip"/>.
    ///     <see langword="true"/> otherwise.
    /// </returns>
    bool IsValidLocation (string text, int col, int row);

    /// <summary>
    ///     Updates <see cref="IDriver.Col"/> and <see cref="IDriver.Row"/> to the specified column and row in
    ///     <see cref="IDriver.Contents"/>.
    ///     Used by <see cref="IDriver.AddRune(System.Text.Rune)"/> and <see cref="IDriver.AddStr"/> to determine
    ///     where to add content.
    /// </summary>
    /// <remarks>
    ///     <para>This does not move the cursor on the screen, it only updates the internal state of the driver.</para>
    ///     <para>
    ///         If <paramref name="col"/> or <paramref name="row"/> are negative or beyond  <see cref="IDriver.Cols"/>
    ///         and
    ///         <see cref="IDriver.Rows"/>, the method still sets those properties.
    ///     </para>
    /// </remarks>
    /// <param name="col">Column to move to.</param>
    /// <param name="row">Row to move to.</param>
    void Move (int col, int row);

    /// <summary>Adds the specified rune to the display at the current cursor position.</summary>
    /// <remarks>
    ///     <para>
    ///         When the method returns, <see cref="IDriver.Col"/> will be incremented by the number of columns
    ///         <paramref name="rune"/> required, even if the new column value is outside of the
    ///         <see cref="IDriver.Clip"/> or screen
    ///         dimensions defined by <see cref="IDriver.Cols"/>.
    ///     </para>
    ///     <para>
    ///         If <paramref name="rune"/> requires more than one column, and <see cref="IDriver.Col"/> plus the number
    ///         of columns
    ///         needed exceeds the <see cref="IDriver.Clip"/> or screen dimensions, the default Unicode replacement
    ///         character (U+FFFD)
    ///         will be added instead.
    ///     </para>
    /// </remarks>
    /// <param name="rune">Rune to add.</param>
    void AddRune (Rune rune);

    /// <summary>
    ///     Adds the specified <see langword="char"/> to the display at the current cursor position. This method is a
    ///     convenience method that calls <see cref="IDriver.AddRune(System.Text.Rune)"/> with the <see cref="Rune"/>
    ///     constructor.
    /// </summary>
    /// <param name="c">Character to add.</param>
    void AddRune (char c);

    /// <summary>Adds the <paramref name="str"/> to the display at the cursor position.</summary>
    /// <remarks>
    ///     <para>
    ///         When the method returns, <see cref="IDriver.Col"/> will be incremented by the number of columns
    ///         <paramref name="str"/> required, unless the new column value is outside of the <see cref="IDriver.Clip"/>
    ///         or screen
    ///         dimensions defined by <see cref="IDriver.Cols"/>.
    ///     </para>
    ///     <para>If <paramref name="str"/> requires more columns than are available, the output will be clipped.</para>
    /// </remarks>
    /// <param name="str">String.</param>
    void AddStr (string str);

    /// <summary>Clears the <see cref="IDriver.Contents"/> of the driver.</summary>
    void ClearContents ();

    /// <summary>
    ///     Fills the specified rectangle with the specified rune, using <see cref="IDriver.CurrentAttribute"/>
    /// </summary>
    event EventHandler<EventArgs> ClearedContents;

    /// <summary>Fills the specified rectangle with the specified rune, using <see cref="IDriver.CurrentAttribute"/></summary>
    /// <remarks>
    ///     The value of <see cref="IDriver.Clip"/> is honored. Any parts of the rectangle not in the clip will not be
    ///     drawn.
    /// </remarks>
    /// <param name="rect">The Screen-relative rectangle.</param>
    /// <param name="rune">The Rune used to fill the rectangle</param>
    void FillRect (Rectangle rect, Rune rune = default);

    /// <summary>
    ///     Fills the specified rectangle with the specified <see langword="char"/>. This method is a convenience method
    ///     that calls <see cref="IDriver.FillRect(System.Drawing.Rectangle,System.Text.Rune)"/>.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="c"></param>
    void FillRect (Rectangle rect, char c);

    /// <summary>Gets the terminal cursor visibility.</summary>
    /// <param name="visibility">The current <see cref="CursorVisibility"/></param>
    /// <returns><see langword="true"/> upon success</returns>
    bool GetCursorVisibility (out CursorVisibility visibility);

    /// <summary>
    ///     INTERNAL: Updates the terminal with the current output buffer. Should not be used by applications. Drawing occurs
    ///     once each Application main loop iteration.
    /// </summary>
    void Refresh ();

    /// <summary>Sets the terminal cursor visibility.</summary>
    /// <param name="visibility">The wished <see cref="CursorVisibility"/></param>
    /// <returns><see langword="true"/> upon success</returns>
    bool SetCursorVisibility (CursorVisibility visibility);

    /// <summary>
    ///     The event fired when the screen changes (size, position, etc.).
    ///     <see cref="Screen"/> is the source of truth for screen dimensions.
    /// </summary>
    event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <summary>Suspends the application (e.g. on Linux via SIGTSTP) and upon resume, resets the console driver.</summary>
    /// <remarks>This is only implemented in UnixDriver.</remarks>
    void Suspend ();

    /// <summary>
    ///     Sets the position of the terminal cursor to <see cref="IDriver.Col"/> and
    ///     <see cref="IDriver.Row"/>.
    /// </summary>
    void UpdateCursor ();

    /// <summary>Initializes the driver</summary>
    void Init ();

    /// <summary>Ends the execution of the console driver.</summary>
    void End ();

    /// <summary>Selects the specified attribute as the attribute to use for future calls to AddRune and AddString.</summary>
    /// <remarks>Implementations should call <c>base.SetAttribute(c)</c>.</remarks>
    /// <param name="c">C.</param>
    Attribute SetAttribute (Attribute c);

    /// <summary>Gets the current <see cref="Attribute"/>.</summary>
    /// <returns>The current attribute.</returns>
    Attribute GetAttribute ();

    /// <summary>Event fired when a mouse event occurs.</summary>
    event EventHandler<MouseEventArgs>? MouseEvent;

    /// <summary>Event fired when a key is pressed down. This is a precursor to <see cref="IDriver.KeyUp"/>.</summary>
    event EventHandler<Key>? KeyDown;

    /// <summary>Event fired when a key is released.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will fire this event after <see cref="IDriver.KeyDown"/>
    ///     processing is
    ///     complete.
    /// </remarks>
    event EventHandler<Key>? KeyUp;

    /// <summary>
    ///     Enqueues a key input event to the driver. For unit tests.
    /// </summary>
    /// <param name="key"></param>
    void EnqueueKeyEvent (Key key);

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

    /// <summary>
    ///     Gets a string representation of <see cref="Contents"/>.
    /// </summary>
    /// <returns></returns>
    public string ToString ();

    /// <summary>
    ///     Gets an ANSI escape sequence representation of <see cref="Contents"/>. This is the
    ///     same output as would be written to the terminal to recreate the current screen contents.
    /// </summary>
    /// <returns></returns>
    public string ToAnsi ();
}

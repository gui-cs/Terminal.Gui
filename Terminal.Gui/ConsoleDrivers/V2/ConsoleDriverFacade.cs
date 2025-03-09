using System.Runtime.InteropServices;

namespace Terminal.Gui;

internal class ConsoleDriverFacade<T> : IConsoleDriver, IConsoleDriverFacade
{
    private readonly IConsoleOutput _output;
    private readonly IOutputBuffer _outputBuffer;
    private readonly AnsiRequestScheduler _ansiRequestScheduler;
    private CursorVisibility _lastCursor = CursorVisibility.Default;

    /// <summary>The event fired when the terminal is resized.</summary>
    public event EventHandler<SizeChangedEventArgs> SizeChanged;

    public IInputProcessor InputProcessor { get; }

    public ConsoleDriverFacade (
        IInputProcessor inputProcessor,
        IOutputBuffer outputBuffer,
        IConsoleOutput output,
        AnsiRequestScheduler ansiRequestScheduler,
        IWindowSizeMonitor windowSizeMonitor
    )
    {
        InputProcessor = inputProcessor;
        _output = output;
        _outputBuffer = outputBuffer;
        _ansiRequestScheduler = ansiRequestScheduler;

        InputProcessor.KeyDown += (s, e) => KeyDown?.Invoke (s, e);
        InputProcessor.KeyUp += (s, e) => KeyUp?.Invoke (s, e);
        InputProcessor.MouseEvent += (s, e) => MouseEvent?.Invoke (s, e);

        windowSizeMonitor.SizeChanging += (_, e) => SizeChanged?.Invoke (this, e);

        CreateClipboard ();
    }

    private void CreateClipboard ()
    {
        PlatformID p = Environment.OSVersion.Platform;

        if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            Clipboard = new WindowsClipboard ();
        }
        else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            Clipboard = new MacOSXClipboard ();
        }
        else if (CursesDriver.Is_WSL_Platform ())
        {
            Clipboard = new WSLClipboard ();
        }
        else
        {
            Clipboard = new FakeDriver.FakeClipboard ();
        }
    }

    /// <summary>Gets the location and size of the terminal screen.</summary>
    public Rectangle Screen => new (new (0, 0), _output.GetWindowSize ());

    /// <summary>
    ///     Gets or sets the clip rectangle that <see cref="AddRune(Rune)"/> and <see cref="AddStr(string)"/> are subject
    ///     to.
    /// </summary>
    /// <value>The rectangle describing the of <see cref="Clip"/> region.</value>
    public Region Clip
    {
        get => _outputBuffer.Clip;
        set => _outputBuffer.Clip = value;
    }

    /// <summary>Get the operating system clipboard.</summary>
    public IClipboard Clipboard { get; private set; } = new FakeDriver.FakeClipboard ();

    /// <summary>
    ///     Gets the column last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Col => _outputBuffer.Col;

    /// <summary>The number of columns visible in the terminal.</summary>
    public int Cols
    {
        get => _outputBuffer.Cols;
        set => _outputBuffer.Cols = value;
    }

    /// <summary>
    ///     The contents of the application output. The driver outputs this buffer to the terminal.
    ///     <remarks>The format of the array is rows, columns. The first index is the row, the second index is the column.</remarks>
    /// </summary>
    public Cell [,] Contents
    {
        get => _outputBuffer.Contents;
        set => _outputBuffer.Contents = value;
    }

    /// <summary>The leftmost column in the terminal.</summary>
    public int Left
    {
        get => _outputBuffer.Left;
        set => _outputBuffer.Left = value;
    }

    /// <summary>
    ///     Gets the row last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/> are used by
    ///     <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
    /// </summary>
    public int Row => _outputBuffer.Row;

    /// <summary>The number of rows visible in the terminal.</summary>
    public int Rows
    {
        get => _outputBuffer.Rows;
        set => _outputBuffer.Rows = value;
    }

    /// <summary>The topmost row in the terminal.</summary>
    public int Top
    {
        get => _outputBuffer.Top;
        set => _outputBuffer.Top = value;
    }

    // TODO: Probably not everyone right?

    /// <summary>Gets whether the <see cref="ConsoleDriver"/> supports TrueColor output.</summary>
    public bool SupportsTrueColor => true;

    // TODO: Currently ignored
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
    public bool Force16Colors { get; set; }

    /// <summary>
    ///     The <see cref="Attribute"/> that will be used for the next <see cref="AddRune(Rune)"/> or <see cref="AddStr"/>
    ///     call.
    /// </summary>
    public Attribute CurrentAttribute
    {
        get => _outputBuffer.CurrentAttribute;
        set => _outputBuffer.CurrentAttribute = value;
    }

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
    public void AddRune (Rune rune) { _outputBuffer.AddRune (rune); }

    /// <summary>
    ///     Adds the specified <see langword="char"/> to the display at the current cursor position. This method is a
    ///     convenience method that calls <see cref="ConsoleDriver.AddRune(System.Text.Rune)"/> with the <see cref="Rune"/>
    ///     constructor.
    /// </summary>
    /// <param name="c">Character to add.</param>
    public void AddRune (char c) { _outputBuffer.AddRune (c); }

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
    public void AddStr (string str) { _outputBuffer.AddStr (str); }

    /// <summary>Clears the <see cref="ConsoleDriver.Contents"/> of the driver.</summary>
    public void ClearContents ()
    {
        _outputBuffer.ClearContents ();
        ClearedContents?.Invoke (this, new MouseEventArgs ());
    }

    /// <summary>
    ///     Raised each time <see cref="ConsoleDriver.ClearContents"/> is called. For benchmarking.
    /// </summary>
    public event EventHandler<EventArgs> ClearedContents;

    /// <summary>
    ///     Fills the specified rectangle with the specified rune, using <see cref="ConsoleDriver.CurrentAttribute"/>
    /// </summary>
    /// <remarks>
    ///     The value of <see cref="ConsoleDriver.Clip"/> is honored. Any parts of the rectangle not in the clip will not be
    ///     drawn.
    /// </remarks>
    /// <param name="rect">The Screen-relative rectangle.</param>
    /// <param name="rune">The Rune used to fill the rectangle</param>
    public void FillRect (Rectangle rect, Rune rune = default) { _outputBuffer.FillRect (rect, rune); }

    /// <summary>
    ///     Fills the specified rectangle with the specified <see langword="char"/>. This method is a convenience method
    ///     that calls <see cref="ConsoleDriver.FillRect(System.Drawing.Rectangle,System.Text.Rune)"/>.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="c"></param>
    public void FillRect (Rectangle rect, char c) { _outputBuffer.FillRect (rect, c); }

    /// <inheritdoc/>
    public virtual string GetVersionInfo ()
    {
        var type = "";

        if (InputProcessor is WindowsInputProcessor)
        {
            type = "win";
        }
        else if (InputProcessor is NetInputProcessor)
        {
            type = "net";
        }

        return "v2" + type;
    }

    /// <summary>Tests if the specified rune is supported by the driver.</summary>
    /// <param name="rune"></param>
    /// <returns>
    ///     <see langword="true"/> if the rune can be properly presented; <see langword="false"/> if the driver does not
    ///     support displaying this rune.
    /// </returns>
    public bool IsRuneSupported (Rune rune) { return Rune.IsValid (rune.Value); }

    /// <summary>Tests whether the specified coordinate are valid for drawing the specified Rune.</summary>
    /// <param name="rune">Used to determine if one or two columns are required.</param>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>
    ///     <see langword="false"/> if the coordinate is outside the screen bounds or outside of
    ///     <see cref="ConsoleDriver.Clip"/>.
    ///     <see langword="true"/> otherwise.
    /// </returns>
    public bool IsValidLocation (Rune rune, int col, int row) { return _outputBuffer.IsValidLocation (rune, col, row); }

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
    public void Move (int col, int row) { _outputBuffer.Move (col, row); }

    // TODO: Probably part of output

    /// <summary>Sets the terminal cursor visibility.</summary>
    /// <param name="visibility">The wished <see cref="CursorVisibility"/></param>
    /// <returns><see langword="true"/> upon success</returns>
    public bool SetCursorVisibility (CursorVisibility visibility)
    {
        _lastCursor = visibility;
        _output.SetCursorVisibility (visibility);

        return true;
    }

    /// <inheritdoc/>
    public bool GetCursorVisibility (out CursorVisibility current)
    {
        current = _lastCursor;

        return true;
    }

    /// <inheritdoc/>
    public void Suspend () { }

    /// <summary>
    ///     Sets the position of the terminal cursor to <see cref="ConsoleDriver.Col"/> and
    ///     <see cref="ConsoleDriver.Row"/>.
    /// </summary>
    public void UpdateCursor () { _output.SetCursorPosition (Col, Row); }

    /// <summary>Initializes the driver</summary>
    /// <returns>Returns an instance of <see cref="MainLoop"/> using the <see cref="IMainLoopDriver"/> for the driver.</returns>
    public MainLoop Init () { throw new NotSupportedException (); }

    /// <summary>Ends the execution of the console driver.</summary>
    public void End ()
    {
        // TODO: Nope
    }

    /// <summary>Selects the specified attribute as the attribute to use for future calls to AddRune and AddString.</summary>
    /// <remarks>Implementations should call <c>base.SetAttribute(c)</c>.</remarks>
    /// <param name="c">C.</param>
    public Attribute SetAttribute (Attribute c) { return _outputBuffer.CurrentAttribute = c; }

    /// <summary>Gets the current <see cref="Attribute"/>.</summary>
    /// <returns>The current attribute.</returns>
    public Attribute GetAttribute () { return _outputBuffer.CurrentAttribute; }

    /// <summary>Makes an <see cref="Attribute"/>.</summary>
    /// <param name="foreground">The foreground color.</param>
    /// <param name="background">The background color.</param>
    /// <returns>The attribute for the foreground and background colors.</returns>
    public Attribute MakeColor (in Color foreground, in Color background)
    {
        // TODO: what even is this? why Attribute constructor wants to call Driver method which must return an instance of Attribute? ?!?!?!
        return new (
                    -1, // only used by cursesdriver!
                    foreground,
                    background
                   );
    }

    /// <summary>Event fired when a key is pressed down. This is a precursor to <see cref="ConsoleDriver.KeyUp"/>.</summary>
    public event EventHandler<Key> KeyDown;

    /// <summary>Event fired when a key is released.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will fire this event after <see cref="ConsoleDriver.KeyDown"/>
    ///     processing is
    ///     complete.
    /// </remarks>
    public event EventHandler<Key> KeyUp;

    /// <summary>Event fired when a mouse event occurs.</summary>
    public event EventHandler<MouseEventArgs> MouseEvent;

    /// <summary>Simulates a key press.</summary>
    /// <param name="keyChar">The key character.</param>
    /// <param name="key">The key.</param>
    /// <param name="shift">If <see langword="true"/> simulates the Shift key being pressed.</param>
    /// <param name="alt">If <see langword="true"/> simulates the Alt key being pressed.</param>
    /// <param name="ctrl">If <see langword="true"/> simulates the Ctrl key being pressed.</param>
    public void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool ctrl)
    {
        // TODO: implement
    }

    /// <summary>
    ///     Provide proper writing to send escape sequence recognized by the <see cref="ConsoleDriver"/>.
    /// </summary>
    /// <param name="ansi"></param>
    public void WriteRaw (string ansi) { _output.Write (ansi); }

    /// <summary>
    ///     Queues the given <paramref name="request"/> for execution
    /// </summary>
    /// <param name="request"></param>
    public void QueueAnsiRequest (AnsiEscapeSequenceRequest request) { _ansiRequestScheduler.SendOrSchedule (request); }

    public AnsiRequestScheduler GetRequestScheduler () { return _ansiRequestScheduler; }

    /// <inheritdoc/>
    public void Refresh ()
    {
        // No need we will always draw when dirty
    }
}

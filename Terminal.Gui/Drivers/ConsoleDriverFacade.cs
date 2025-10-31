#nullable enable
using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

internal class ConsoleDriverFacade<T> : IConsoleDriver, IConsoleDriverFacade
{
    private readonly IConsoleOutput _output;
    private readonly IOutputBuffer _outputBuffer;
    private readonly AnsiRequestScheduler _ansiRequestScheduler;
    private CursorVisibility _lastCursor = CursorVisibility.Default;

    /// <summary>
    /// The event fired when the screen changes (size, position, etc.).
    /// </summary>
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    public IInputProcessor InputProcessor { get; }
    public IOutputBuffer OutputBuffer => _outputBuffer;

    public IConsoleSizeMonitor ConsoleSizeMonitor { get; }


    public ConsoleDriverFacade (
        IInputProcessor inputProcessor,
        IOutputBuffer outputBuffer,
        IConsoleOutput output,
        AnsiRequestScheduler ansiRequestScheduler,
        IConsoleSizeMonitor sizeMonitor
    )
    {
        InputProcessor = inputProcessor;
        _output = output;
        _outputBuffer = outputBuffer;
        _ansiRequestScheduler = ansiRequestScheduler;

        InputProcessor.KeyDown += (s, e) => KeyDown?.Invoke (s, e);
        InputProcessor.KeyUp += (s, e) => KeyUp?.Invoke (s, e);
        InputProcessor.MouseEvent += (s, e) =>
                                     {
                                         //Logging.Logger.LogTrace ($"Mouse {e.Flags} at x={e.ScreenPosition.X} y={e.ScreenPosition.Y}");
                                         MouseEvent?.Invoke (s, e);
                                     };

        ConsoleSizeMonitor = sizeMonitor;
        sizeMonitor.SizeChanged += (_, e) =>
        {
            SetScreenSize(e.Size!.Value.Width, e.Size.Value.Height);
            //SizeChanged?.Invoke (this, e);
        };

        CreateClipboard ();
    }

    private void CreateClipboard ()
    {
        if (LegacyFakeConsoleDriver.FakeBehaviors.UseFakeClipboard)
        {
            Clipboard = new FakeClipboard (
                LegacyFakeConsoleDriver.FakeBehaviors.FakeClipboardAlwaysThrowsNotSupportedException,
                LegacyFakeConsoleDriver.FakeBehaviors.FakeClipboardIsSupportedAlwaysFalse);

            return;
        }

        PlatformID p = Environment.OSVersion.Platform;

        if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            Clipboard = new WindowsClipboard ();
        }
        else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            Clipboard = new MacOSXClipboard ();
        }
        else if (PlatformDetection.IsWSLPlatform ())
        {
            Clipboard = new WSLClipboard ();
        }
        else
        {
            Clipboard = new FakeClipboard ();
        }
    }

    /// <summary>Gets the location and size of the terminal screen.</summary>
    public Rectangle Screen
    {
        get
        {
            if (LegacyConsoleDriver.RunningUnitTests && _output is WindowsConsoleOutput or NetConsoleOutput)
            {
                // In unit tests, we don't have a real output, so we return an empty rectangle.
                return Rectangle.Empty;
            }

            return new (0, 0, _outputBuffer.Cols, _outputBuffer.Rows);
        }
    }

    /// <summary>
    /// Sets the screen size for testing purposes. Only supported by FakeDriver.
    /// </summary>
    /// <param name="width">The new width in columns.</param>
    /// <param name="height">The new height in rows.</param>
    /// <exception cref="NotSupportedException">Thrown when called on non-FakeDriver instances.</exception>
    public virtual void SetScreenSize (int width, int height)
    {
        _outputBuffer.SetSize (width, height);
        _output.SetSize (width, height);
        SizeChanged?.Invoke(this, new (new (width, height)));
    }

    /// <summary>
    ///     Gets or sets the clip rectangle that <see cref="AddRune(Rune)"/> and <see cref="AddStr(string)"/> are subject
    ///     to.
    /// </summary>
    /// <value>The rectangle describing the of <see cref="Clip"/> region.</value>
    public Region? Clip
    {
        get => _outputBuffer.Clip;
        set => _outputBuffer.Clip = value;
    }

    /// <summary>Get the operating system clipboard.</summary>
    public IClipboard Clipboard { get; private set; } = new FakeClipboard ();

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
    public Cell [,]? Contents
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

    /// <summary>Gets whether the <see cref="IConsoleDriver"/> supports TrueColor output.</summary>
    public bool SupportsTrueColor => true;

    // TODO: Currently ignored
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
    public bool Force16Colors
    {
        get => Application.Force16Colors || !SupportsTrueColor;
        set => Application.Force16Colors = value || !SupportsTrueColor;
    }

    /// <summary>
    ///     The <see cref="System.Attribute"/> that will be used for the next <see cref="AddRune(Rune)"/> or <see cref="AddStr"/>
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
    ///         When the method returns, <see cref="IConsoleDriver.Col"/> will be incremented by the number of columns
    ///         <paramref name="rune"/> required, even if the new column value is outside of the
    ///         <see cref="IConsoleDriver.Clip"/> or screen
    ///         dimensions defined by <see cref="IConsoleDriver.Cols"/>.
    ///     </para>
    ///     <para>
    ///         If <paramref name="rune"/> requires more than one column, and <see cref="IConsoleDriver.Col"/> plus the number
    ///         of columns
    ///         needed exceeds the <see cref="IConsoleDriver.Clip"/> or screen dimensions, the default Unicode replacement
    ///         character (U+FFFD)
    ///         will be added instead.
    ///     </para>
    /// </remarks>
    /// <param name="rune">Rune to add.</param>
    public void AddRune (Rune rune) { _outputBuffer.AddRune (rune); }

    /// <summary>
    ///     Adds the specified <see langword="char"/> to the display at the current cursor position. This method is a
    ///     convenience method that calls <see cref="IConsoleDriver.AddRune(System.Text.Rune)"/> with the <see cref="Rune"/>
    ///     constructor.
    /// </summary>
    /// <param name="c">Character to add.</param>
    public void AddRune (char c) { _outputBuffer.AddRune (c); }

    /// <summary>Adds the <paramref name="str"/> to the display at the cursor position.</summary>
    /// <remarks>
    ///     <para>
    ///         When the method returns, <see cref="IConsoleDriver.Col"/> will be incremented by the number of columns
    ///         <paramref name="str"/> required, unless the new column value is outside of the <see cref="IConsoleDriver.Clip"/>
    ///         or screen
    ///         dimensions defined by <see cref="IConsoleDriver.Cols"/>.
    ///     </para>
    ///     <para>If <paramref name="str"/> requires more columns than are available, the output will be clipped.</para>
    /// </remarks>
    /// <param name="str">String.</param>
    public void AddStr (string str) { _outputBuffer.AddStr (str); }

    /// <summary>Clears the <see cref="IConsoleDriver.Contents"/> of the driver.</summary>
    public void ClearContents ()
    {
        _outputBuffer.ClearContents ();
        ClearedContents?.Invoke (this, new MouseEventArgs ());
    }

    /// <summary>
    ///     Raised each time <see cref="IConsoleDriver.ClearContents"/> is called. For benchmarking.
    /// </summary>
    public event EventHandler<EventArgs>? ClearedContents;

    /// <summary>
    ///     Fills the specified rectangle with the specified rune, using <see cref="IConsoleDriver.CurrentAttribute"/>
    /// </summary>
    /// <remarks>
    ///     The value of <see cref="IConsoleDriver.Clip"/> is honored. Any parts of the rectangle not in the clip will not be
    ///     drawn.
    /// </remarks>
    /// <param name="rect">The Screen-relative rectangle.</param>
    /// <param name="rune">The Rune used to fill the rectangle</param>
    public void FillRect (Rectangle rect, Rune rune = default) { _outputBuffer.FillRect (rect, rune); }

    /// <summary>
    ///     Fills the specified rectangle with the specified <see langword="char"/>. This method is a convenience method
    ///     that calls <see cref="IConsoleDriver.FillRect(System.Drawing.Rectangle,System.Text.Rune)"/>.
    /// </summary>
    /// <param name="rect"></param>
    /// <param name="c"></param>
    public void FillRect (Rectangle rect, char c) { _outputBuffer.FillRect (rect, c); }

    /// <inheritdoc/>
    public virtual string GetVersionInfo ()
    {
        string type = InputProcessor.DriverName ?? throw new ArgumentNullException (nameof (InputProcessor.DriverName));

        return type;
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
    ///     <see cref="IConsoleDriver.Clip"/>.
    ///     <see langword="true"/> otherwise.
    /// </returns>
    public bool IsValidLocation (Rune rune, int col, int row) { return _outputBuffer.IsValidLocation (rune, col, row); }

    /// <summary>
    ///     Updates <see cref="IConsoleDriver.Col"/> and <see cref="IConsoleDriver.Row"/> to the specified column and row in
    ///     <see cref="IConsoleDriver.Contents"/>.
    ///     Used by <see cref="IConsoleDriver.AddRune(System.Text.Rune)"/> and <see cref="IConsoleDriver.AddStr"/> to determine
    ///     where to add content.
    /// </summary>
    /// <remarks>
    ///     <para>This does not move the cursor on the screen, it only updates the internal state of the driver.</para>
    ///     <para>
    ///         If <paramref name="col"/> or <paramref name="row"/> are negative or beyond  <see cref="IConsoleDriver.Cols"/>
    ///         and
    ///         <see cref="IConsoleDriver.Rows"/>, the method still sets those properties.
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
    public void Suspend ()
    {
        if (Environment.OSVersion.Platform != PlatformID.Unix)
        {
            return;
        }

        Console.Out.Write (EscSeqUtils.CSI_DisableMouseEvents);

        if (!LegacyConsoleDriver.RunningUnitTests)
        {
            Console.ResetColor ();
            Console.Clear ();

            //Disable alternative screen buffer.
            Console.Out.Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);

            //Set cursor key to cursor.
            Console.Out.Write (EscSeqUtils.CSI_ShowCursor);

            Platform.Suspend ();

            //Enable alternative screen buffer.
            Console.Out.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);

            Application.LayoutAndDraw ();
        }

        Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);
    }

    /// <summary>
    ///     Sets the position of the terminal cursor to <see cref="IConsoleDriver.Col"/> and
    ///     <see cref="IConsoleDriver.Row"/>.
    /// </summary>
    public void UpdateCursor () { _output.SetCursorPosition (Col, Row); }

    /// <summary>Initializes the driver</summary>
    public void Init () { throw new NotSupportedException (); }

    /// <summary>Ends the execution of the console driver.</summary>
    public void End ()
    {
        // TODO: Nope
    }

    /// <summary>Selects the specified attribute as the attribute to use for future calls to AddRune and AddString.</summary>
    /// <remarks>Implementations should call <c>base.SetAttribute(c)</c>.</remarks>
    /// <param name="newAttribute">C.</param>
    /// <returns>The previously set Attribute.</returns>
    public Attribute SetAttribute (Attribute newAttribute)
    {
        Attribute currentAttribute = _outputBuffer.CurrentAttribute;
        _outputBuffer.CurrentAttribute = newAttribute;

        return currentAttribute;
    }

    /// <summary>Gets the current <see cref="Attribute"/>.</summary>
    /// <returns>The current attribute.</returns>
    public Attribute GetAttribute () { return _outputBuffer.CurrentAttribute; }

    /// <summary>Event fired when a key is pressed down. This is a precursor to <see cref="IConsoleDriver.KeyUp"/>.</summary>
    public event EventHandler<Key>? KeyDown;

    /// <summary>Event fired when a key is released.</summary>
    /// <remarks>
    ///     Drivers that do not support key release events will fire this event after <see cref="IConsoleDriver.KeyDown"/>
    ///     processing is
    ///     complete.
    /// </remarks>
    public event EventHandler<Key>? KeyUp;

    /// <summary>Event fired when a mouse event occurs.</summary>
    public event EventHandler<MouseEventArgs>? MouseEvent;

    /// <summary>
    ///     Provide proper writing to send escape sequence recognized by the <see cref="IConsoleDriver"/>.
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

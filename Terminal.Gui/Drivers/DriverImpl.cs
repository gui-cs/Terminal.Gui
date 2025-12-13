using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Terminal.Gui.Input;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Provides the main implementation of the driver abstraction layer for Terminal.Gui.
///     This implementation of <see cref="IDriver"/> coordinates the interaction between input processing, output
///     rendering,
///     screen size monitoring, and ANSI escape sequence handling.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="DriverImpl"/> implements <see cref="IDriver"/>,
///         serving as the central coordination point for console I/O operations. It delegates functionality
///         to specialized components:
///     </para>
///     <list type="bullet">
///         <item><see cref="IInputProcessor"/> - Processes keyboard and mouse input</item>
///         <item><see cref="IOutputBuffer"/> - Manages the screen buffer state</item>
///         <item><see cref="IOutput"/> - Handles actual console output rendering</item>
///         <item><see cref="AnsiRequestScheduler"/> - Manages ANSI escape sequence requests</item>
///         <item><see cref="ISizeMonitor"/> - Monitors terminal size changes</item>
///     </list>
///     <para>
///         This class is internal and should not be used directly by application code.
///         Applications interact with drivers through the <see cref="Application"/> class.
///     </para>
/// </remarks>
internal class DriverImpl : IDriver
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DriverImpl"/> class.
    /// </summary>
    /// <param name="inputProcessor">The input processor for handling keyboard and mouse events.</param>
    /// <param name="outputBuffer">The output buffer for managing screen state.</param>
    /// <param name="output">The output interface for rendering to the console.</param>
    /// <param name="ansiRequestScheduler">The scheduler for managing ANSI escape sequence requests.</param>
    /// <param name="sizeMonitor">The monitor for tracking terminal size changes.</param>
    public DriverImpl (
        IInputProcessor inputProcessor,
        IOutputBuffer outputBuffer,
        IOutput output,
        AnsiRequestScheduler ansiRequestScheduler,
        ISizeMonitor sizeMonitor
    )
    {
        _inputProcessor = inputProcessor;
        _inputProcessor.KeyDown += (s, e) => KeyDown?.Invoke (s, e);
        _inputProcessor.KeyUp += (s, e) => KeyUp?.Invoke (s, e);
        _inputProcessor.SyntheticMouseEvent += (s, e) =>
                                               {
                                                   //Logging.Logger.LogTrace ($"Mouse {e.Flags} at x={e.ScreenPosition.X} y={e.ScreenPosition.Y}");
                                                   MouseEvent?.Invoke (s, e);
                                               };
        _outputBuffer = outputBuffer;
        _output = output;
        _ansiRequestScheduler = ansiRequestScheduler;
        _sizeMonitor = sizeMonitor;
        _sizeMonitor.SizeChanged += OnSizeMonitorOnSizeChanged;

        CreateClipboard ();

        Driver.Force16ColorsChanged += OnDriverOnForce16ColorsChanged;
    }

    #region Driver Lifecycle

    /// <inheritdoc/>
    public void Init () { throw new NotSupportedException (); }

    /// <inheritdoc/>
    public void Refresh ()
    {
        _output.Write (_outputBuffer);
    }

    /// <inheritdoc/>
    public string? GetName () => GetInputProcessor ().DriverName?.ToLowerInvariant ();

    /// <inheritdoc/>
    public virtual string GetVersionInfo ()
    {
        string type = GetInputProcessor ().DriverName ?? throw new InvalidOperationException ("Driver name is not set.");

        return type;
    }

    /// <inheritdoc/>
    public void Suspend ()
    {
        // BUGBUG: This is all platform-specific and should not be implemented here.
        // BUGBUG: This needs to be in each platform's driver implementation.
        if (Environment.OSVersion.Platform != PlatformID.Unix)
        {
            return;
        }

        Console.Out.Write (EscSeqUtils.CSI_DisableMouseEvents);

        try
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
        }
        catch (Exception ex)
        {
            Logging.Error ($"Error suspending terminal: {ex.Message}");
        }

        Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);
    }

    /// <inheritdoc/>
    public bool IsLegacyConsole
    {
        get => _output.IsLegacyConsole;
        set => _output.IsLegacyConsole = value;
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        _sizeMonitor.SizeChanged -= OnSizeMonitorOnSizeChanged;
        Driver.Force16ColorsChanged -= OnDriverOnForce16ColorsChanged;
        _output.Dispose ();
    }

    #endregion Driver Lifecycle

    #region Driver Components

    private readonly IOutput _output;

    public IOutput GetOutput () => _output;

    private readonly IInputProcessor _inputProcessor;

    /// <inheritdoc/>
    public IInputProcessor GetInputProcessor () => _inputProcessor;

    private readonly IOutputBuffer _outputBuffer;

    private readonly ISizeMonitor _sizeMonitor;

    /// <inheritdoc/>
    public IClipboard? Clipboard { get; private set; } = new FakeClipboard ();

    private void CreateClipboard ()
    {
        if (GetInputProcessor ().DriverName is { } && GetInputProcessor ()!.DriverName!.Contains ("fake"))
        {
            if (Clipboard is null)
            {
                Clipboard = new FakeClipboard ();
            }

            return;
        }

        PlatformID p = Environment.OSVersion.Platform;

        if (p is PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows)
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

        // Clipboard is set to FakeClipboard at initialization
    }

    #endregion Driver Components

    #region Screen and Display

    /// <inheritdoc/>
    public Rectangle Screen => new (0, 0, _outputBuffer.Cols, _outputBuffer.Rows);

    /// <inheritdoc/>
    public virtual void SetScreenSize (int width, int height)
    {
        _outputBuffer.SetSize (width, height);
        _output.SetSize (width, height);
        SizeChanged?.Invoke (this, new (new (width, height)));
    }

    /// <inheritdoc/>
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    private void OnSizeMonitorOnSizeChanged (object? _, SizeChangedEventArgs e) { SetScreenSize (e.Size!.Value.Width, e.Size.Value.Height); }

    /// <inheritdoc/>
    public int Cols
    {
        get => _outputBuffer.Cols;
        set => _outputBuffer.Cols = value;
    }

    /// <inheritdoc/>
    public int Rows
    {
        get => _outputBuffer.Rows;
        set => _outputBuffer.Rows = value;
    }

    /// <inheritdoc/>
    public int Left
    {
        get => _outputBuffer.Left;
        set => _outputBuffer.Left = value;
    }

    /// <inheritdoc/>
    public int Top
    {
        get => _outputBuffer.Top;
        set => _outputBuffer.Top = value;
    }

    #endregion Screen and Display

    #region Color Support

    /// <inheritdoc/>
    public bool SupportsTrueColor => !IsLegacyConsole;

    /// <inheritdoc/>
    public bool Force16Colors
    {
        get => _output.Force16Colors;
        set => _output.Force16Colors = value;
    }

    private void OnDriverOnForce16ColorsChanged (object? _, ValueChangedEventArgs<bool> e) { Force16Colors = e.NewValue; }

    #endregion Color Support

    #region Content Buffer

    /// <inheritdoc/>
    public Cell [,]? Contents
    {
        get => _outputBuffer.Contents;
        set => _outputBuffer.Contents = value;
    }

    /// <inheritdoc/>
    public Region? Clip
    {
        get => _outputBuffer.Clip;
        set => _outputBuffer.Clip = value;
    }

    /// <summary>Clears the <see cref="IDriver.Contents"/> of the driver.</summary>
    public void ClearContents ()
    {
        _outputBuffer.ClearContents ();
        ClearedContents?.Invoke (this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs>? ClearedContents;

    #endregion Content Buffer

    #region Drawing and Rendering

    /// <inheritdoc/>
    public int Col => _outputBuffer.Col;

    /// <inheritdoc/>
    public int Row => _outputBuffer.Row;

    /// <inheritdoc/>
    public Attribute CurrentAttribute
    {
        get => _outputBuffer.CurrentAttribute;
        set => _outputBuffer.CurrentAttribute = value;
    }

    /// <inheritdoc/>
    public void Move (int col, int row) { _outputBuffer.Move (col, row); }

    /// <inheritdoc/>
    public bool IsRuneSupported (Rune rune) => Rune.IsValid (rune.Value);

    /// <summary>Tests whether the specified coordinate are valid for drawing the specified Text.</summary>
    /// <param name="text">Used to determine if one or two columns are required.</param>
    /// <param name="col">The column.</param>
    /// <param name="row">The row.</param>
    /// <returns>
    ///     <see langword="false"/> if the coordinate is outside the screen bounds or outside of
    ///     <see cref="IDriver.Clip"/>.
    ///     <see langword="true"/> otherwise.
    /// </returns>
    public bool IsValidLocation (string text, int col, int row) => _outputBuffer.IsValidLocation (text, col, row);

    /// <inheritdoc/>
    public void AddRune (Rune rune) { _outputBuffer.AddRune (rune); }

    /// <inheritdoc/>
    public void AddRune (char c) { _outputBuffer.AddRune (c); }

    /// <inheritdoc/>
    public void AddStr (string str) { _outputBuffer.AddStr (str); }

    /// <inheritdoc/>
    public void FillRect (Rectangle rect, Rune rune = default) { _outputBuffer.FillRect (rect, rune); }

    /// <inheritdoc/>
    public Attribute SetAttribute (Attribute newAttribute)
    {
        Attribute currentAttribute = _outputBuffer.CurrentAttribute;
        _outputBuffer.CurrentAttribute = newAttribute;

        return currentAttribute;
    }

    /// <inheritdoc/>
    public Attribute GetAttribute () => _outputBuffer.CurrentAttribute;

    /// <inheritdoc/>
    public void WriteRaw (string ansi) { _output.Write (ansi); }

    /// <inheritdoc/>
    public ConcurrentQueue<SixelToRender> GetSixels () => _output.GetSixels ();

    /// <inheritdoc/>
    public new string ToString ()
    {
        StringBuilder sb = new ();

        Cell [,] contents = Contents!;

        for (var r = 0; r < Rows; r++)
        {
            for (var c = 0; c < Cols; c++)
            {
                string text = contents [r, c].Grapheme;

                sb.Append (text);

                if (text.GetColumns () > 1)
                {
                    c++;
                }
            }

            sb.AppendLine ();
        }

        return sb.ToString ();
    }

    /// <inheritdoc/>
    public string ToAnsi () => _output.ToAnsi (_outputBuffer);

    #endregion Drawing and Rendering

    #region Cursor

    private CursorVisibility _lastCursor = CursorVisibility.Default;

    /// <inheritdoc/>
    public void UpdateCursor () { _output.SetCursorPosition (Col, Row); }

    /// <inheritdoc/>
    public bool GetCursorVisibility (out CursorVisibility current)
    {
        current = _lastCursor;

        return true;
    }

    /// <inheritdoc/>
    public bool SetCursorVisibility (CursorVisibility visibility)
    {
        _lastCursor = visibility;
        _output.SetCursorVisibility (visibility);

        return true;
    }

    #endregion Cursor

    #region Input Events

    /// <summary>Event fired when a key is pressed down. This is a precursor to <see cref="IDriver.KeyUp"/>.</summary>
    public event EventHandler<Key>? KeyDown;

    /// <inheritdoc/>
    public event EventHandler<Key>? KeyUp;

    /// <inheritdoc/>
    public void EnqueueKeyEvent (Key key) { GetInputProcessor ().EnqueueKeyDownEvent (key); }

    /// <summary>Event fired when a mouse event occurs.</summary>
    public event EventHandler<Mouse>? MouseEvent;

    /// <inheritdoc />
    public void EnqueueMouseEvent (Mouse mouse) { GetInputProcessor ().EnqueueMouseEvent (null, mouse); }

    #endregion Input Events

    #region ANSI Escape Sequences

    private readonly AnsiRequestScheduler _ansiRequestScheduler;

    /// <inheritdoc/>
    public virtual void QueueAnsiRequest (AnsiEscapeSequenceRequest request) { _ansiRequestScheduler.SendOrSchedule (this, request); }

    #endregion ANSI Escape Sequences
}

using System.Text.RegularExpressions;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <para>
///         Pure ANSI console output.
///     </para>
///     <para>
///         <b>ANSI Output Architecture:</b>
///     </para>
///     <list type="bullet">
///         <item>
///             <b>Pure ANSI</b> - All output operations use ANSI escape sequences via <see cref="EscSeqUtils"/>,
///             making it portable across ANSI-compatible terminals (Unix, Windows Terminal, ConEmu, etc.).
///         </item>
///         <item>
///             <b>Buffer Capture</b> - <see cref="GetLastBuffer"/> provides access to the last written
///             <see cref="IOutputBuffer"/> for test verification, independent of actual console output.
///         </item>
///         <item>
///             <b>Graceful Degradation</b> - Detects if console is unavailable or redirected, silently
///             operating in buffer-only mode for CI/headless environments.
///         </item>
///         <item>
///             <b>Size Management</b> - Uses <see cref="SetSize"/> for controlling terminal dimensions
///             in tests. In real terminals, size would be queried via ANSI requests
///             (see <see cref="EscSeqUtils.CSI_ReportWindowSizeInChars"/>) or platform APIs.
///         </item>
///     </list>
///     <para>
///         <b>Color Support:</b> Supports both 16-color (via <see cref="OutputBase.Force16Colors"/>)
///         and true-color (24-bit RGB) output through ANSI SGR sequences.
///     </para>
/// </summary>
public class ANSIOutput : OutputBase, IOutput
{
    private Size _consoleSize = new (80, 25);
    private IOutputBuffer? _lastBuffer;
    private readonly bool _terminalInitialized;

    /// <summary>
    ///     Initializes a new instance of <see cref="ANSIOutput"/>.
    ///     Checks if a real console is available for ANSI output and activates the alternate screen buffer.
    /// </summary>
    public ANSIOutput ()
    {
        _lastBuffer = new OutputBufferImpl ();
        _lastBuffer.SetSize (80, 25);

        try
        {
            // Check if console is available (not redirected)
            if (!Console.IsOutputRedirected && !Console.IsInputRedirected)
            {
                Stream stream = Console.OpenStandardOutput ();

                if (stream.CanWrite)
                {
                    _terminalInitialized = true;

                    // Initialize terminal for ANSI output
                    // Activate alternate screen buffer, hide cursor, enable mouse tracking
                    Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
                    Write (EscSeqUtils.CSI_ClearScreen (EscSeqUtils.ClearScreenOptions.EntireScreen));
                    Write (EscSeqUtils.CSI_SetCursorPosition (1, 1)); // Move to top-left
                    Write (EscSeqUtils.CSI_HideCursor);
                    Write (EscSeqUtils.CSI_EnableMouseEvents);

                    // Note: Size will be queried via ANSI by ANSISizeMonitor.Initialize()
                    // Don't use Console.WindowWidth/Height here as it may reflect the main buffer,
                    // not the alternate screen buffer we just activated.
                    // Start with default size; actual size will be set when ANSI response arrives.
                    _consoleSize = new (80, 25);
                }
            }
        }
        catch
        {
            _terminalInitialized = false;
        }
    }

    /// <summary>
    ///     Gets or sets the last output buffer written. The <see cref="IOutputBuffer.Contents"/> contains
    ///     a reference to the buffer last written with <see cref="Write(IOutputBuffer)"/>.
    /// </summary>
    public IOutputBuffer? GetLastBuffer () { return _lastBuffer; }

    ///// <inheritdoc cref="IOutput.GetLastOutput"/>
    //public override string GetLastOutput () => _outputStringBuilder.ToString ();

    /// <inheritdoc/>
    public void SetSize (int width, int height) { _consoleSize = new (width, height); }

    /// <inheritdoc/>
    public Size GetSize () { return _consoleSize; }

    /// <inheritdoc/>
    protected override void Write (StringBuilder output)
    {
        base.Write (output);

        if (!_terminalInitialized)
        {
            return;
        }

        try
        {
            Console.Out.Write (output);
        }
        catch
        {
            // ignore for unit tests
        }
    }

    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text)
    {
        if (!_terminalInitialized)
        {
            return;
        }

        try
        {
            Console.Out.Write (text);
        }
        catch
        {
            // ignore for unit tests
        }
    }

    /// <inheritdoc cref="IOutput.Write(IOutputBuffer)"/>
    public override void Write (IOutputBuffer buffer)
    {
        _lastBuffer = buffer;
        base.Write (buffer);
    }

    private Point? _lastCursorPosition;
    private EscSeqUtils.DECSCUSR_Style? _currentDecscusrStyle;

    /// <inheritdoc/>
    public Point GetCursorPosition () { return _lastCursorPosition ?? Point.Empty; }

    /// <inheritdoc/>
    public void SetCursorPosition (int col, int row) { SetCursorPositionImpl (col, row); }

    /// <inheritdoc cref="IOutput.SetCursorVisibility"/>
    public override void SetCursorVisibility (CursorVisibility visibility)
    {
        if (!_terminalInitialized)
        {
            return;
        }

        try
        {
            if (visibility != CursorVisibility.Invisible)
            {
                if (_currentDecscusrStyle is null || _currentDecscusrStyle != (EscSeqUtils.DECSCUSR_Style)(((int)visibility >> 24) & 0xFF))
                {
                    _currentDecscusrStyle = (EscSeqUtils.DECSCUSR_Style)(((int)visibility >> 24) & 0xFF);

                    Write (EscSeqUtils.CSI_SetCursorStyle ((EscSeqUtils.DECSCUSR_Style)_currentDecscusrStyle));
                }

                Write (EscSeqUtils.CSI_ShowCursor);
            }
            else
            {
                Write (EscSeqUtils.CSI_HideCursor);
            }
        }
        catch
        {
            // ignore
        }
    }

    /// <inheritdoc/>
    protected override bool SetCursorPositionImpl (int screenPositionX, int screenPositionY)
    {
        if (_lastCursorPosition is { } && _lastCursorPosition.Value.X == screenPositionX && _lastCursorPosition.Value.Y == screenPositionY)
        {
            return true;
        }

        _lastCursorPosition = new (screenPositionX, screenPositionY);

        if (!_terminalInitialized)
        {
            return true;
        }

        try
        {
            // Convert from 0-based (Terminal.Gui) to 1-based (ANSI) coordinates
            EscSeqUtils.CSI_WriteCursorPosition (Console.Out, screenPositionY + 1, screenPositionX + 1);
        }
        catch
        {
            // ignore
        }

        return true;
    }

    /// <inheritdoc/>
    protected override void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle)
    {
        if (Force16Colors)
        {
            output.Append (EscSeqUtils.CSI_SetForegroundColor (attr.Foreground.GetAnsiColorCode ()));
            output.Append (EscSeqUtils.CSI_SetBackgroundColor (attr.Background.GetAnsiColorCode ()));
        }
        else
        {
            EscSeqUtils.CSI_AppendForegroundColorRGB (
                                                      output,
                                                      attr.Foreground.R,
                                                      attr.Foreground.G,
                                                      attr.Foreground.B
                                                     );

            EscSeqUtils.CSI_AppendBackgroundColorRGB (
                                                      output,
                                                      attr.Background.R,
                                                      attr.Background.G,
                                                      attr.Background.B
                                                     );
            EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
        }
    }

    /// <summary>
    ///     Handles ANSI size query responses.
    ///     Expected format: ESC [ 8 ; height ; width t
    /// </summary>
    /// <param name="response">The ANSI response string</param>
    public void HandleSizeQueryResponse (string? response)
    {
        if (string.IsNullOrEmpty (response))
        {
            return;
        }

        try
        {
            // Parse response: ESC [ 8 ; height ; width t
            // Example: "[8;25;80t"
            Match match = Regex.Match (response, @"\[(\d+);(\d+);(\d+)t$");

            if (match.Success && match.Groups.Count == 4)
            {
                // Group 1 should be "8" (the response value)
                // Group 2 is height, Group 3 is width
                if (int.TryParse (match.Groups [2].Value, out int height) && int.TryParse (match.Groups [3].Value, out int width))
                {
                    _consoleSize = new (width, height);

                    //Logging.Trace ($"Terminal size from ANSI query: {width}x{height}");
                }
            }
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to parse size query response '{response}': {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (!_terminalInitialized)
        {
            return;
        }

        try
        {
            // Restore terminal state: disable mouse, restore buffer, show cursor
            Write (EscSeqUtils.CSI_DisableMouseEvents);
            Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);
            Write (EscSeqUtils.CSI_ShowCursor);
        }
        catch
        {
            // Ignore errors - we're shutting down
        }
    }
}

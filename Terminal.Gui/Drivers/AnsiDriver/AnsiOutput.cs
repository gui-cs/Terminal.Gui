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
public class AnsiOutput : OutputBase, IOutput
{
    private Size _consoleSize = new (80, 25);
    private IOutputBuffer? _lastBuffer;
    private readonly bool _terminalInitialized;

    /// <summary>
    ///     Initializes a new instance of <see cref="AnsiOutput"/>.
    ///     Checks if a real console is available for ANSI output and activates the alternate screen buffer.
    /// </summary>
    public AnsiOutput ()
    {
        Logging.Information ($"Creating {nameof (AnsiOutput)}");

        _lastBuffer = new OutputBufferImpl ();
        _lastBuffer.SetSize (80, 25);
        _currentCursor = new ();

        try
        {
            // Check if console is available (not redirected)
            if (Console.IsOutputRedirected || Console.IsInputRedirected)
            {
                Logging.Warning ($"Console redirected (Output: {Console.IsOutputRedirected}, Input: {Console.IsInputRedirected}). Running in degraded mode.");
                _terminalInitialized = false;
            }
            else
            {
                Stream stream = Console.OpenStandardOutput ();

                if (!stream.CanWrite)
                {
                    Logging.Warning ("Console output stream is not writable. Running in degraded mode.");
                    _terminalInitialized = false;
                }
                else
                {
                    _terminalInitialized = true;

                    // Initialize terminal for ANSI output
                    // Activate alternate screen buffer, hide cursor, enable mouse tracking
                    Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
                    Write (EscSeqUtils.CSI_ClearScreen (EscSeqUtils.ClearScreenOptions.EntireScreen));
                    Write (EscSeqUtils.CSI_SetCursorPosition (1, 1)); // Move to top-left
                    Write (EscSeqUtils.CSI_HideCursor);
                    Write (EscSeqUtils.CSI_EnableMouseEvents);

                    // Flush to ensure all sequences are sent
                    Console.Out.Flush ();
                    Logging.Information ("ANSIOutput initialized successfully");

                    // Note: Size will be queried via ANSI by ANSISizeMonitor.Initialize()
                    // Don't use Console.WindowWidth/Height here as it may reflect the main buffer,
                    // not the alternate screen buffer we just activated.
                    // Start with default size; actual size will be set when ANSI response arrives.
                    _consoleSize = new (80, 25);
                }
            }
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to initialize ANSIOutput: {ex.GetType ().Name}: {ex.Message}");
            Logging.Warning ($"Stack trace: {ex.StackTrace}");
            _terminalInitialized = false;
        }
    }

    /// <summary>
    ///     Gets or sets the last output buffer written. The <see cref="IOutputBuffer.Contents"/> contains
    ///     a reference to the buffer last written with <see cref="Write(IOutputBuffer)"/>.
    /// </summary>
    public IOutputBuffer? GetLastBuffer () { return _lastBuffer; }

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

    private Cursor _currentCursor;

    /// <inheritdoc />
    public Cursor GetCursor ()
    {
        return _currentCursor;
    }

    /// <inheritdoc />
    public void SetCursor (Cursor cursor)
    {
        try
        {
            if (!cursor.IsVisible)
            {
                Write (EscSeqUtils.CSI_HideCursor);
            }
            else
            {
                if (_currentCursor!.Shape != cursor.Shape)
                {
                    Write (EscSeqUtils.CSI_SetCursorStyle (cursor.Shape));
                }

                Write (EscSeqUtils.CSI_ShowCursor);
            }
        }
        catch
        {
            // Ignore any exceptions
        }
        finally
        {
            SetCursorPositionImpl (
                                   cursor.Position?.X ?? 0,
                                   cursor.Position?.Y ?? 0
                                  );

            _currentCursor = cursor;
        }
    }

    /// <inheritdoc/>
    protected override bool SetCursorPositionImpl (int col, int row)
    {
        if (_currentCursor!.Position is { } && _currentCursor.Position.Value.X == col && _currentCursor.Position.Value.Y == row)
        {
            return false;
        }

        if (!_terminalInitialized)
        {
            return true;
        }

        // + 1 is needed because non-Windows is based on 1 instead of 0 and
        // Console.CursorTop/CursorLeft isn't reliable.
        EscSeqUtils.CSI_WriteCursorPosition (Console.Out, row + 1, col + 1);
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

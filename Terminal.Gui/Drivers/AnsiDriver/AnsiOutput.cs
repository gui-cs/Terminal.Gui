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
    // Tracks which underlying platform APIs are in use
    private readonly AnsiPlatform _platform;

    private Size _consoleSize = new (80, 25);
    private IOutputBuffer? _lastBuffer;

    private readonly WindowsVTOutputHelper? _windowsVTOutput;

    /// <summary>
    ///     Initializes a new instance of <see cref="AnsiOutput"/>.
    ///     Checks if a real console is available for ANSI output and activates the alternate screen buffer.
    /// </summary>
    public AnsiOutput ()
    {
        // Logging.Information ($"Creating {nameof (AnsiOutput)}");

        _platform = AnsiPlatform.Degraded;

        _lastBuffer = new OutputBufferImpl ();
        _lastBuffer.SetSize (80, 25);
        _currentCursor = new Cursor ();

        try
        {
            // Check if console is available (not redirected)
            if (Console.IsOutputRedirected || Console.IsInputRedirected)
            {
                Logging.Warning ($"Console redirected (Output: {Console.IsOutputRedirected}, Input: {Console.IsInputRedirected}). Running in degraded mode.");

                return;
            }

            // Initialize platform-specific output helpers
            if (PlatformDetection.IsWindows ())
            {
                _windowsVTOutput = new WindowsVTOutputHelper ();

                if (!_windowsVTOutput.TryEnable ())
                {
                    _windowsVTOutput.Dispose ();
                    _windowsVTOutput = null;

                    Logging.Warning ("Failed to enable Windows VT Input mode. Terminal input will not work. Running in degraded mode.");

                    return;
                }
                _platform = AnsiPlatform.WindowsVT;
            }
            else if (PlatformDetection.IsUnixLike ())
            {
                // duplicate stdout so we don't mess with Console.Out's FD
                int fdCopy = UnixIOHelper.dup (UnixIOHelper.STDOUT_FILENO);

                if (fdCopy == -1)
                {
                    Logging.Warning ("Console output stream is not writable. Running in degraded mode.");

                    return;
                }

                _platform = AnsiPlatform.UnixRaw;
            }

            // Initialize terminal for ANSI output
            // Activate alternate screen buffer, hide cursor, enable mouse tracking
            Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
            Write (EscSeqUtils.CSI_ClearScreen (EscSeqUtils.ClearScreenOptions.EntireScreen));
            Write (EscSeqUtils.CSI_SetCursorPosition (1, 1)); // Move to top-left
            Write (EscSeqUtils.CSI_HideCursor);
            // TODO: Move Input related CSI sequences to AnsiInput
            Write (EscSeqUtils.CSI_EnableMouseEvents);

            // Flush to ensure all sequences are sent
            // NOTE: Default implementation of Flush does nothing.
            Console.Out.Flush ();

            //Logging.Information ("ANSIOutput initialized successfully");

            // Note: Size will be queried via ANSI by ANSISizeMonitor.Initialize()
            // Don't use Console.WindowWidth/Height here as it may reflect the main buffer,
            // not the alternate screen buffer we just activated.
            // Start with default size; actual size will be set when ANSI response arrives.
            _consoleSize = new Size (80, 25);
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to initialize ANSIOutput: {ex.GetType ().Name}: {ex.Message}");
            Logging.Warning ($"Stack trace: {ex.StackTrace}");
            _platform = AnsiPlatform.Degraded;
        }
    }

    /// <summary>
    ///     Gets or sets the last output buffer written. The <see cref="IOutputBuffer.Contents"/> contains
    ///     a reference to the buffer last written with <see cref="Write(IOutputBuffer)"/>.
    /// </summary>
    public IOutputBuffer? GetLastBuffer () => _lastBuffer;

    /// <inheritdoc/>
    public void SetSize (int width, int height) => _consoleSize = new Size (width, height);

    /// <inheritdoc/>
    public Size GetSize () => _consoleSize;

    /// <inheritdoc/>
    protected override void Write (StringBuilder output)
    {
        base.Write (output);

        try
        {
            switch (_platform)
            {
                case AnsiPlatform.WindowsVT:
                    _windowsVTOutput!.Write (output);

                    break;

                case AnsiPlatform.UnixRaw:
                    byte [] utf8 = Encoding.UTF8.GetBytes (output.ToString ());
                    UnixIOHelper.TryWriteStdout (utf8);

                    return;

                case AnsiPlatform.Degraded:
                default:
                    break;
            }
        }
        catch (Exception)
        {
            //Logging.Warning (e.Message);

            // ignore for unit tests
        }
    }

    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text)
    {
        try
        {
            switch (_platform)
            {
                case AnsiPlatform.WindowsVT:
                    StringBuilder sb = new ();
                    sb.Append (text);
                    _windowsVTOutput!.Write (sb);

                    break;

                case AnsiPlatform.UnixRaw:
                    byte [] utf8 = Encoding.UTF8.GetBytes (text.ToArray ());
                    UnixIOHelper.TryWriteStdout (utf8);

                    return;

                case AnsiPlatform.Degraded:
                default:
                    break;
            }
        }
        catch (Exception)
        {
            //Logging.Warning (e.Message);

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

    /// <inheritdoc/>
    public Cursor GetCursor () => _currentCursor;

    /// <inheritdoc/>
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
                if (_currentCursor!.Style != cursor.Style)
                {
                    Write (EscSeqUtils.CSI_SetCursorStyle (cursor.Style));
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
            SetCursorPositionImpl (cursor.Position?.X ?? 0, cursor.Position?.Y ?? 0);

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

        if (_platform == AnsiPlatform.Degraded)
        {
            return true;
        }

        // + 1 is needed because non-Windows is based on 1 instead of 0 and
        // Console.CursorTop/CursorLeft isn't reliable.
        Write (EscSeqUtils.CSI_SetCursorPosition (row + 1, col + 1));

        return true;
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

            if (match is { Success: true, Groups.Count: 4 })
            {
                // Group 1 should be "8" (the response value)
                // Group 2 is height, Group 3 is width
                if (int.TryParse (match.Groups [2].Value, out int height) && int.TryParse (match.Groups [3].Value, out int width))
                {
                    _consoleSize = new Size (width, height);
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
        if (_platform == AnsiPlatform.Degraded)
        {
            return;
        }

        try
        {
            // Restore terminal state: disable mouse, restore buffer, show cursor
            // TODO: Move Input related CSI sequences to AnsiInput
            Write (EscSeqUtils.CSI_DisableMouseEvents);
            Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);
            Write (EscSeqUtils.CSI_ShowCursor);
        }
        catch
        {
            // Ignore errors - we're shutting down
        }
        finally
        {
            //Logging.Trace ("Flushing and closing.");

            _windowsVTOutput?.Dispose ();
        }
    }
}

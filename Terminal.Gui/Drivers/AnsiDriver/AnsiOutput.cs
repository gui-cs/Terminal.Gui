using System.Text.RegularExpressions;
using Terminal.Gui.Tracing;

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
    ///     Gets the <see cref="AppModel"/> that this output was initialized with.
    /// </summary>
    internal AppModel AppModel { get; }

    /// <summary>
    ///     Initializes a new instance of <see cref="AnsiOutput"/>.
    ///     Checks if a real console is available for ANSI output and activates the alternate screen buffer.
    /// </summary>
    /// <param name="appModel">
    ///     The rendering mode. When <see cref="AppModel.Inline"/>, the alternate screen buffer is not activated
    ///     and the terminal's primary (scrollback) buffer is used instead.
    /// </param>
    public AnsiOutput (AppModel appModel = AppModel.FullScreen)
    {
        AppModel = appModel;
        _platform = AnsiPlatform.Degraded;

        _lastBuffer = new OutputBufferImpl ();
        _lastBuffer.SetSize (80, 25);
        _currentCursor = new Cursor ();

        try
        {
            // Ensure the console output code page is UTF-8 (65001). The ANSI driver writes
            // UTF-8 encoded bytes via WriteFile; without this, a fresh Windows terminal uses
            // its default OEM code page (e.g. 437), causing multi-byte UTF-8 characters
            // (box-drawing glyphs, etc.) to be misinterpreted as garbled single-byte characters.
            // See https://github.com/gui-cs/Terminal.Gui/issues/4848
            Console.OutputEncoding = Encoding.UTF8;

            // Check if we have a real console first
            if (!IsAttachedToTerminal)
            {
                Trace.Lifecycle (nameof (AnsiOutput), "Init", "No real terminal attached. Running in degraded mode.");

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

                    Trace.Lifecycle (nameof (AnsiOutput),
                                     "Init",
                                     "Failed to enable Windows VT Input mode. Terminal input will not work. Running in degraded mode.");

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
                    Trace.Lifecycle (nameof (AnsiOutput), "Init", "Console output stream is not writable. Running in degraded mode.");

                    return;
                }

                _platform = AnsiPlatform.UnixRaw;
            }

            // Initialize terminal for ANSI output
            if (AppModel == AppModel.Inline)
            {
                // Inline mode: do NOT switch to alternate screen buffer.
                // Stay in the primary (scrollback) buffer.
                Write (EscSeqUtils.CSI_HideCursor);

                // TODO: Move Input related CSI sequences to AnsiInput
                Write (EscSeqUtils.CSI_EnableMouseEvents);
            }
            else
            {
                // FullScreen mode: activate alternate screen buffer, hide cursor, enable mouse tracking
                Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
                Write (EscSeqUtils.CSI_ClearScreen (EscSeqUtils.ClearScreenOptions.EntireScreen));
                Write (EscSeqUtils.CSI_SetCursorPosition (1, 1)); // Move to top-left
                Write (EscSeqUtils.CSI_HideCursor);

                // TODO: Move Input related CSI sequences to AnsiInput
                Write (EscSeqUtils.CSI_EnableMouseEvents);
            }

            // Flush to ensure all sequences are sent
            AnsiTerminalHelper.FlushNative (_platform);

            // Initialize _consoleSize with the real terminal dimensions.
            // In FullScreen mode we cannot rely on Console.WindowWidth/Height because we just
            // switched to the alternate screen buffer, which may have different dimensions.
            // The ANSI size monitor will update the size asynchronously via CSI 8 t.
            // In Inline mode we stay in the primary buffer, so the Console APIs are accurate
            // and we must query them now — the async ANSI response arrives too late to avoid
            // a first render using the wrong (default 80×25) size.
            if (AppModel == AppModel.Inline)
            {
                Size? nativeSize = QueryNativeTerminalSize ();

                if (nativeSize is { Width: > 0, Height: > 0 })
                {
                    _consoleSize = nativeSize.Value;
                }
            }
        }
        catch (Exception ex)
        {
            Trace.Lifecycle (nameof (AnsiOutput), "Init", $"Failed to initialize ANSIOutput: {ex.GetType ().Name}: {ex.Message}. Stack trace: {ex.StackTrace}");
            _platform = AnsiPlatform.Degraded;
        }
    }

    /// <inheritdoc/>
    public void Suspend () => UnixTerminalHelper.Suspend (this);

    /// <summary>
    ///     Queries the real terminal size from the OS using platform-native APIs
    ///     (<see cref="Console.WindowWidth"/>/<see cref="Console.WindowHeight"/> on Windows,
    ///     <c>ioctl(TIOCGWINSZ)</c> on Unix). Returns <see langword="null"/> when the size
    ///     cannot be determined (e.g. no terminal attached).
    /// </summary>
    internal static Size? QueryNativeTerminalSize ()
    {
        try
        {
            if (PlatformDetection.IsWindows ())
            {
                int w = Console.WindowWidth;
                int h = Console.WindowHeight;

                return w > 0 && h > 0 ? new Size (w, h) : null;
            }

            if (PlatformDetection.IsUnixLike ())
            {
                return UnixIOHelper.TryGetTerminalSize (out Size s) && s.Width > 0 && s.Height > 0 ? s : null;
            }
        }
        catch (Exception ex)
        {
            Trace.Lifecycle (nameof (AnsiOutput), "QueryNativeTerminalSize", $"Failed: {ex.GetType ().Name}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    ///     Gets or sets the last output buffer written. The <see cref="IOutputBuffer.Contents"/> contains
    ///     a reference to the buffer last written with <see cref="Write(IOutputBuffer)"/>.
    /// </summary>
    public IOutputBuffer? GetLastBuffer () => _lastBuffer;

    /// <inheritdoc/>
    public void SetSize (int width, int height) => _consoleSize = new Size (width, height);

    /// <summary>
    ///     When non-<see langword="null"/>, <see cref="GetSize"/> calls this delegate to obtain the
    ///     real terminal size directly from the OS (e.g. <c>ioctl(TIOCGWINSZ)</c> on Unix or the
    ///     Console API on Windows). Set by <see cref="AnsiComponentFactory"/> when
    ///     <see cref="Driver.SizeDetection"/> is <see cref="SizeDetectionMode.Polling"/>.
    /// </summary>
    internal Func<Size?>? NativeSizeQuery { get; set; }

    /// <inheritdoc/>
    public Size GetSize ()
    {
        if (NativeSizeQuery is null)
        {
            return _consoleSize;
        }
        Size? native = NativeSizeQuery ();

        if (native is { })
        {
            _consoleSize = native.Value;
        }

        return _consoleSize;
    }

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
            // ignore for unit tests
        }
    }

    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text)
    {
        StringBuilder capturedOutput = new ();
        capturedOutput.Append (text);
        base.Write (capturedOutput);

        try
        {
            switch (_platform)
            {
                case AnsiPlatform.WindowsVT:
                    _windowsVTOutput!.Write (capturedOutput);

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
            // ignore for unit tests
        }
    }

    /// <summary>
    ///     Gets the kitty keyboard flags currently enabled on the terminal.
    /// </summary>
    internal KittyKeyboardFlags KittyKeyboardEnabledFlags { get; private set; }

    /// <summary>
    ///     Enables kitty keyboard progressive enhancement flags for the active terminal.
    /// </summary>
    /// <param name="flags">The kitty keyboard flags to enable.</param>
    internal void EnableKittyKeyboard (KittyKeyboardFlags flags)
    {
        if (flags == KittyKeyboardFlags.None || _platform == AnsiPlatform.Degraded)
        {
            return;
        }

        Trace.Lifecycle (nameof (AnsiOutput), "KittyKeyboard", $"Writing enable sequence for flags {flags}");
        Write (EscSeqUtils.CSI_EnableKittyKeyboardFlags (flags));
        KittyKeyboardEnabledFlags = flags;
    }

    /// <summary>
    ///     Restores the previous kitty keyboard flag state if kitty mode was enabled.
    /// </summary>
    internal void DisableKittyKeyboard ()
    {
        if (KittyKeyboardEnabledFlags == KittyKeyboardFlags.None)
        {
            return;
        }

        Trace.Lifecycle (nameof (AnsiOutput), "KittyKeyboard", $"Writing disable sequence for flags {KittyKeyboardEnabledFlags}");
        Write (EscSeqUtils.CSI_DisableKittyKeyboardFlags);
        KittyKeyboardEnabledFlags = KittyKeyboardFlags.None;
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
                if (_currentCursor.Style != cursor.Style)
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
        if (_currentCursor.Position is { } && _currentCursor.Position.Value.X == col && _currentCursor.Position.Value.Y == row)
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

            if (match is not { Success: true, Groups.Count: 4 })
            {
                return;
            }

            // Group 1 should be "8" (the response value)
            // Group 2 is height, Group 3 is width
            if (int.TryParse (match.Groups [2].Value, out int height) && int.TryParse (match.Groups [3].Value, out int width))
            {
                _consoleSize = new Size (width, height);
            }
        }
        catch (Exception ex)
        {
            Trace.Lifecycle (nameof (AnsiOutput), "SizeQuery", $"Failed to parse size query response '{response}': {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        try
        {
            DisableKittyKeyboard ();

            if (_platform == AnsiPlatform.Degraded)
            {
                return;
            }

            // Restore terminal state: disable mouse, show cursor
            Write (EscSeqUtils.CSI_DisableMouseEvents);

            if (AppModel == AppModel.Inline)
            {
                // Inline mode: do NOT restore alternate buffer. Move cursor to just
                // below the inline region and show it so the shell prompt appears
                // naturally after the rendered content.
                Write (EscSeqUtils.CSI_SetCursorPosition (_consoleSize.Height + 1, 1));
                Write (EscSeqUtils.CSI_ShowCursor);
            }
            else
            {
                // FullScreen mode: restore alternate buffer and show cursor
                Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);
                Write (EscSeqUtils.CSI_ShowCursor);
            }
        }
        catch
        {
            // Ignore errors - we're shutting down
        }
        finally
        {
            Trace.Lifecycle (nameof (AnsiOutput), "Dispose", "Flushing output and releasing resources.");

            _windowsVTOutput?.Dispose ();
        }
    }
}

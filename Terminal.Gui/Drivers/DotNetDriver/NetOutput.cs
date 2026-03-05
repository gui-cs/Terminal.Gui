namespace Terminal.Gui.Drivers;

/// <summary>
///     Implementation of <see cref="IOutput"/> that uses native dotnet
///     methods e.g. <see cref="System.Console"/>
/// </summary>
public class NetOutput : OutputBase, IOutput
{
    private readonly bool _isWinPlatform;

    /// <summary>
    ///     Creates a new instance of the <see cref="NetOutput"/> class.
    /// </summary>
    public NetOutput ()
    {
        // Logging.Information ($"Creating {nameof (NetOutput)}");

        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch
        {
            // ignore for unit tests
        }

        PlatformID p = Environment.OSVersion.Platform;

        if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            _isWinPlatform = true;
        }
    }

    /// <inheritdoc/>
    public Size GetSize ()
    {
        try
        {
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                return new Size (80, 25);
            }
            Size size = new (Console.WindowWidth, Console.WindowHeight);

            return size.IsEmpty ? new Size (80, 25) : size;
        }
        catch (IOException)
        {
            // Not connected to a terminal; return a default size
            return new Size (80, 25);
        }
    }

    /// <inheritdoc/>
    public void SetSize (int width, int height)
    {
        // Do Nothing.
    }


    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text)
    {
        try
        {
            Console.Out.Write (text);
        }
        catch (IOException)
        {
            // Not connected to a terminal; do nothing
        }
    }

    /// <inheritdoc/>
    protected override void Write (StringBuilder output)
    {
        base.Write (output);

        try
        {
            Console.Out.Write (output);
        }
        catch (IOException)
        {
            // Not connected to a terminal; do nothing
        }
    }

    private Cursor _currentCursor = new ();

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

        if (_isWinPlatform)
        {
            try
            {
                Console.SetCursorPosition (col, row);
            }
            catch
            {
                // Could happen that the windows is still resizing and the col is bigger than Console.WindowWidth.
            }
            return true;
        }

        // + 1 is needed because non-Windows is based on 1 instead of 0 and
        // Console.CursorTop/CursorLeft isn't reliable.
        EscSeqUtils.CSI_WriteCursorPosition (Console.Out, row + 1, col + 1);

        return true;
    }

    /// <inheritdoc/>
    public void Dispose () { }

    /// <inheritdoc/>
    public void Suspend ()
    {
        if (PlatformDetection.IsWindows ())
        {
            return;
        }

        // Best-effort: mirror behavior of ANSI/Unix outputs for consoles that accept CSI sequences.
        try
        {
            // Disable mouse events to prevent mouse events from being sent to the application while it is suspended.
            Write (EscSeqUtils.CSI_DisableMouseEvents);

            // Check if we have a real console first
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                Logging.Information ($"Console redirected (Output: {Console.IsOutputRedirected}, Input: {Console.IsInputRedirected}). Running in degraded mode.");

                return;
            }

            Console.ResetColor ();
            Console.Clear ();

            //Disable alternative screen buffer.
            Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);

            //Set cursor key to cursor.
            Write (EscSeqUtils.CSI_ShowCursor);

            if (!SuspendHelper.Suspend ())
            {
                return;
            }

            //Enable alternative screen buffer.
            Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);
        }
        catch (Exception ex)
        {
            Logging.Error ($"Error suspending terminal: {ex.Message}");
        }
        finally
        {
            // Enable mouse events to allow mouse events to be sent to the application when it is resumed.
            Write (EscSeqUtils.CSI_EnableMouseEvents);
        }
    }
}

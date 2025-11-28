namespace Terminal.Gui.Drivers;

/// <summary>
///     Implementation of <see cref="IOutput"/> that uses native dotnet
///     methods e.g. <see cref="System.Console"/>
/// </summary>
public class NetOutput : OutputBase, IOutputInternal
{
    private readonly bool _isWinPlatform;

    /// <inheritdoc />
    public IDriver? Driver { get; set; }

    /// <inheritdoc />
    public bool IsVirtualTerminal { get; init; } = true;

    /// <summary>
    ///     Creates a new instance of the <see cref="NetOutput"/> class.
    /// </summary>
    public NetOutput ()
    {
        Logging.Information ($"Creating {nameof (NetOutput)}");

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
    public Size GetSize ()
    {
        try
        {
            Size size = new (Console.WindowWidth, Console.WindowHeight);
            return size.IsEmpty ? new (80, 25) : size;
        }
        catch (IOException)
        {
            // Not connected to a terminal; return a default size
            return new (80, 25);
        }
    }

    /// <inheritdoc />
    public Point GetCursorPosition ()
    {
        return _lastCursorPosition ?? Point.Empty;
    }

    /// <inheritdoc/>
    public void SetCursorPosition (int col, int row) { SetCursorPositionImpl (col, row); }

    /// <inheritdoc />
    public void SetSize (int width, int height)
    {
        // Do Nothing.
    }

    private Point? _lastCursorPosition;

    /// <inheritdoc/>
    protected override void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle)
    {
        if (Application.Force16Colors)
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
        }

        EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
    }

    /// <inheritdoc />
    protected override void Write (StringBuilder output)
    {
        try
        {
            Console.Out.Write (output);
        }
        catch (IOException)
        {
            // Not connected to a terminal; do nothing
        }
    }

    /// <inheritdoc />
    protected override bool SetCursorPositionImpl (int col, int row)
    {
        if (_lastCursorPosition is { } && _lastCursorPosition.Value.X == col && _lastCursorPosition.Value.Y == row)
        {
            return true;
        }

        _lastCursorPosition = new (col, row);

        if (_isWinPlatform)
        {
            // Could happen that the windows is still resizing and the col is bigger than Console.WindowWidth.
            try
            {
                Console.SetCursorPosition (col, row);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // + 1 is needed because non-Windows is based on 1 instead of 0 and
        // Console.CursorTop/CursorLeft isn't reliable.
        EscSeqUtils.CSI_WriteCursorPosition (Console.Out, row + 1, col + 1);

        return true;
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
    }


    private EscSeqUtils.DECSCUSR_Style? _currentDecscusrStyle;

    /// <inheritdoc cref="IOutput.SetCursorVisibility"/>
    public override void SetCursorVisibility (CursorVisibility visibility)
    {
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
            // Ignore any exceptions
        }
    }
}

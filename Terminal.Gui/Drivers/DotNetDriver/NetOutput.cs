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

    /// <inheritdoc/>
    public void SetSize (int width, int height)
    {
        // Do Nothing.
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
        }

        EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
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
                if (_currentCursor!.Shape != cursor.Shape)
                {
                    Write (EscSeqUtils.CSI_SetCursorStyle ((EscSeqUtils.DECSCUSR_Style)cursor.Shape));
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
}

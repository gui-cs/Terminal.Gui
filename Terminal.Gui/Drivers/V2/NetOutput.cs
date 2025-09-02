using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Implementation of <see cref="IConsoleOutput"/> that uses native dotnet
///     methods e.g. <see cref="System.Console"/>
/// </summary>
public class NetOutput : OutputBase, IConsoleOutput
{
    private readonly bool _isWinPlatform;

    /// <summary>
    ///     Creates a new instance of the <see cref="NetOutput"/> class.
    /// </summary>
    public NetOutput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof (NetOutput)}");

        Console.OutputEncoding = Encoding.UTF8;

        PlatformID p = Environment.OSVersion.Platform;

        if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            _isWinPlatform = true;
        }
    }

    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text) { Console.Out.Write (text); }

    /// <inheritdoc/>
    public Size GetWindowSize ()
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            // For unit tests, we return a default size.
            return Size.Empty;
        }

        return new (Console.WindowWidth, Console.WindowHeight);
    }

    /// <inheritdoc/>
    public void SetCursorPosition (int col, int row) { SetCursorPositionImpl (col, row); }

    private Point? _lastCursorPosition;

    /// <inheritdoc/>
    protected override void AppendOrWriteAttribute (StringBuilder output, Attribute attr, TextStyle redrawTextStyle)
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

    /// <inheritdoc/>
    protected override void Write (StringBuilder output) { Console.Out.Write (output); }

    protected override bool SetCursorPositionImpl (int col, int row)
    {
        if (_lastCursorPosition is { } && _lastCursorPosition.Value.X == col && _lastCursorPosition.Value.Y == row)
        {
            return true;
        }

        _lastCursorPosition = new (col, row);

        if (_isWinPlatform)
        {
            // Could happens that the windows is still resizing and the col is bigger than Console.WindowWidth.
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
    public void Dispose () { }

    /// <inheritdoc/>
    public override void SetCursorVisibility (CursorVisibility visibility)
    {
        Console.Out.Write (visibility == CursorVisibility.Default ? EscSeqUtils.CSI_ShowCursor : EscSeqUtils.CSI_HideCursor);
    }
}

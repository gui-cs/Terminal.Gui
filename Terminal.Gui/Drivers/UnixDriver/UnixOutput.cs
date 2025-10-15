using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Terminal.Gui.Drivers;

internal class UnixOutput : OutputBase, IConsoleOutput
{
    [StructLayout (LayoutKind.Sequential)]
    private struct WinSize
    {
        public ushort ws_row;
        public ushort ws_col;
        public ushort ws_xpixel;
        public ushort ws_ypixel;
    }

    private static readonly uint TIOCGWINSZ =
        RuntimeInformation.IsOSPlatform (OSPlatform.OSX) ||
        RuntimeInformation.IsOSPlatform (OSPlatform.FreeBSD)
            ? 0x40087468u  // Darwin/BSD
            : 0x5413u;     // Linux

    [DllImport ("libc", SetLastError = true)]
    private static extern int ioctl (int fd, uint request, out WinSize ws);

    // File descriptor for stdout
    private const int STDOUT_FILENO = 1;

    [DllImport ("libc")]
    private static extern int write (int fd, byte [] buf, int n);

    [DllImport ("libc", SetLastError = true)]
    private static extern int dup (int fd);

    /// <inheritdoc />
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
            EscSeqUtils.CSI_AppendTextStyleChange (output, redrawTextStyle, attr.Style);
        }
    }

    /// <inheritdoc />
    protected override void Write (StringBuilder output)
    {
        byte [] utf8 = Encoding.UTF8.GetBytes (output.ToString ());
        // Write to stdout (fd 1)
        write (STDOUT_FILENO, utf8, utf8.Length);
    }

    private Point? _lastCursorPosition;

    /// <inheritdoc />
    protected override bool SetCursorPositionImpl (int screenPositionX, int screenPositionY)
    {
        if (_lastCursorPosition is { } && _lastCursorPosition.Value.X == screenPositionX && _lastCursorPosition.Value.Y == screenPositionY)
        {
            return true;
        }

        _lastCursorPosition = new (screenPositionX, screenPositionY);

        using var writer = CreateUnixStdoutWriter ();

        // + 1 is needed because Unix is based on 1 instead of 0 and
        EscSeqUtils.CSI_WriteCursorPosition (writer, screenPositionY + 1, screenPositionX + 1);

        return true;
    }

    private TextWriter CreateUnixStdoutWriter ()
    {
        // duplicate stdout so we don’t mess with Console.Out’s FD
        int fdCopy = dup (STDOUT_FILENO);

        if (fdCopy == -1)
        {
            throw new IOException ("Failed to dup STDOUT_FILENO");
        }

        // wrap the raw fd into a SafeFileHandle
        var handle = new SafeFileHandle (fdCopy, ownsHandle: true);

        // create FileStream from the safe handle
        var stream = new FileStream (handle, FileAccess.Write);

        return new StreamWriter (stream)
        {
            AutoFlush = true
        };
    }

    /// <inheritdoc />
    public void Write (ReadOnlySpan<char> text)
    {
        if (!ConsoleDriver.RunningUnitTests)
        {
            byte [] utf8 = Encoding.UTF8.GetBytes (text.ToArray ());
            // Write to stdout (fd 1)
            write (STDOUT_FILENO, utf8, utf8.Length);
        }
    }

    /// <inheritdoc />
    public Size GetWindowSize ()
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            // For unit tests, we return a default size.
            return Size.Empty;
        }

        if (ioctl (1, TIOCGWINSZ, out WinSize ws) == 0)
        {
            if (ws.ws_col > 0 && ws.ws_row > 0)
            {
                return new (ws.ws_col, ws.ws_row);
            }
        }

        return Size.Empty; // fallback
    }

    private EscSeqUtils.DECSCUSR_Style? _currentDecscusrStyle;

    /// <inheritdoc cref="IConsoleOutput.SetCursorVisibility"/>
    public override void SetCursorVisibility (CursorVisibility visibility)
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

    /// <inheritdoc />
    public void SetCursorPosition (int col, int row)
    {
        SetCursorPositionImpl (col, row);
    }

    /// <inheritdoc />
    public void Dispose ()
    {
    }
}

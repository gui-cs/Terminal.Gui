
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming
// ReSharper disable CommentTypo

namespace Terminal.Gui.Drivers;

internal class UnixOutput : OutputBase, IOutput
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

    /// <inheritdoc />
    protected override void Write (StringBuilder output)
    {
        base.Write (output);
        try
        {
            byte [] utf8 = Encoding.UTF8.GetBytes (output.ToString ());

            // Write to stdout (fd 1)
            write (STDOUT_FILENO, utf8, utf8.Length);
        }
        catch
        {
            // ignore for unit tests
        }
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

        try
        {
            using TextWriter? writer = CreateUnixStdoutWriter ();

            // + 1 is needed because Unix is based on 1 instead of 0 and
            EscSeqUtils.CSI_WriteCursorPosition (writer!, screenPositionY + 1, screenPositionX + 1);
        }
        catch
        {
            // ignore
        }

        return true;
    }

    private TextWriter? CreateUnixStdoutWriter ()
    {
        // duplicate stdout so we don't mess with Console.Out's FD
        int fdCopy = dup (STDOUT_FILENO);

        if (fdCopy == -1)
        {
            // Log but don't throw - we're likely running without a TTY (CI/CD, tests, etc.)
            var errno = Marshal.GetLastWin32Error ();
            Logging.Warning ($"Failed to dup STDOUT_FILENO, errno={errno}. Running without TTY support.");
            return null;  // Return null instead of throwing
        }

        try
        {
            // wrap the raw fd into a SafeFileHandle
            SafeFileHandle handle = new SafeFileHandle (fdCopy, ownsHandle: true);

            // create FileStream from the safe handle
            FileStream stream = new FileStream (handle, FileAccess.Write);

            return new StreamWriter (stream)
            {
                AutoFlush = true
            };
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to create TextWriter from dup'd STDOUT: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public void Write (ReadOnlySpan<char> text)
    {
        try
        {
            byte [] utf8 = Encoding.UTF8.GetBytes (text.ToArray ());

            // Write to stdout (fd 1)
            write (STDOUT_FILENO, utf8, utf8.Length);
        }
        catch
        {
            // ignore for unit tests
        }
    }

    /// <inheritdoc />
    public Size GetSize ()
    {
        try
        {
            if (ioctl (1, TIOCGWINSZ, out WinSize ws) == 0)
            {
                if (ws.ws_col > 0 && ws.ws_row > 0)
                {
                    return new (ws.ws_col, ws.ws_row);
                }
            }
        }
        catch
        {
            // ignore
        }

        return new (80, 25); // fallback
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
            // ignore
        }
    }

    /// <inheritdoc />
    public Point GetCursorPosition ()
    {
        return _lastCursorPosition ?? Point.Empty;
    }

    /// <inheritdoc />
    public void SetCursorPosition (int col, int row)
    {
        SetCursorPositionImpl (col, row);
    }

    /// <inheritdoc />
    public void SetSize (int width, int height)
    {
        // Do nothing
    }

    /// <inheritdoc />
    public void Dispose ()
    {
    }
}

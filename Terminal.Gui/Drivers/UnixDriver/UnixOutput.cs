using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming
// ReSharper disable CommentTypo

namespace Terminal.Gui.Drivers;

internal class UnixOutput : OutputBase, IOutput
{
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

    /// <inheritdoc/>
    public void Write (ReadOnlySpan<char> text)
    {
        byte [] utf8 = Encoding.UTF8.GetBytes (text.ToArray ());
        UnixIOHelper.TryWriteStdout (utf8);
    }

    /// <inheritdoc/>
    protected override void Write (StringBuilder output)
    {
        base.Write (output);

        byte [] utf8 = Encoding.UTF8.GetBytes (output.ToString ());
        UnixIOHelper.TryWriteStdout (utf8);
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
    protected override bool SetCursorPositionImpl (int screenPositionX, int screenPositionY)
    {
        if (_currentCursor!.Position is { } && _currentCursor.Position.Value.X == screenPositionX && _currentCursor.Position.Value.Y == screenPositionY)
        {
            return false;
        }

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
        int fdCopy = UnixIOHelper.dup (UnixIOHelper.STDOUT_FILENO);

        if (fdCopy == -1)
        {
            // Log but don't throw - we're likely running without a TTY (CI/CD, tests, etc.)
            int errno = Marshal.GetLastWin32Error ();
            Logging.Warning ($"Failed to dup STDOUT_FILENO, errno={errno}. Running without TTY support.");

            return null; // Return null instead of throwing
        }

        try
        {
            // wrap the raw fd into a SafeFileHandle
            var handle = new SafeFileHandle (fdCopy, true);

            // create FileStream from the safe handle
            var stream = new FileStream (handle, FileAccess.Write);

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

    /// <inheritdoc/>
    public Size GetSize ()
    {
        if (UnixIOHelper.TryGetTerminalSize (out Size size))
        {
            return size;
        }

        return new (80, 25); // fallback
    }

    /// <inheritdoc/>
    public void SetSize (int width, int height)
    {
        // Do nothing
    }

    /// <inheritdoc/>
    public void Dispose () { }
}

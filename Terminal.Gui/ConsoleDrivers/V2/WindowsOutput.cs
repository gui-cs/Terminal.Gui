#nullable enable
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using static Terminal.Gui.WindowsConsole;

namespace Terminal.Gui;

internal class WindowsOutput : IConsoleOutput
{
    [DllImport ("kernel32.dll", EntryPoint = "WriteConsole", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool WriteConsole (
        nint hConsoleOutput,
        string lpbufer,
        uint numberOfCharsToWriten,
        out uint lpNumberOfCharsWritten,
        nint lpReserved
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle (nint handle);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint CreateConsoleScreenBuffer (
        DesiredAccess dwDesiredAccess,
        ShareMode dwShareMode,
        nint secutiryAttributes,
        uint flags,
        nint screenBufferData
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleScreenBufferInfoEx (nint hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX csbi);

    [Flags]
    private enum ShareMode : uint
    {
        FileShareRead = 1,
        FileShareWrite = 2
    }

    [Flags]
    private enum DesiredAccess : uint
    {
        GenericRead = 2147483648,
        GenericWrite = 1073741824
    }

    internal static nint INVALID_HANDLE_VALUE = new (-1);

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleActiveScreenBuffer (nint handle);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleCursorPosition (nint hConsoleOutput, Coord dwCursorPosition);

    private readonly nint _screenBuffer;

    public WindowsOutput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof (WindowsOutput)}");

        _screenBuffer = CreateConsoleScreenBuffer (
                                                   DesiredAccess.GenericRead | DesiredAccess.GenericWrite,
                                                   ShareMode.FileShareRead | ShareMode.FileShareWrite,
                                                   nint.Zero,
                                                   1,
                                                   nint.Zero
                                                  );

        if (_screenBuffer == INVALID_HANDLE_VALUE)
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        if (!SetConsoleActiveScreenBuffer (_screenBuffer))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error ());
        }
    }

    public void Write (string str)
    {
        if (!WriteConsole (_screenBuffer, str, (uint)str.Length, out uint _, nint.Zero))
        {
            throw new Win32Exception (Marshal.GetLastWin32Error (), "Failed to write to console screen buffer.");
        }
    }

    public void Write (IOutputBuffer buffer)
    {
        ExtendedCharInfo [] outputBuffer = new ExtendedCharInfo [buffer.Rows * buffer.Cols];

        // TODO: probably do need this right?
        /*
        if (!windowSize.IsEmpty && (windowSize.Width != buffer.Cols || windowSize.Height != buffer.Rows))
        {
            return;
        }*/

        var bufferCoords = new Coord
        {
            X = (short)buffer.Cols, //Clip.Width,
            Y = (short)buffer.Rows //Clip.Height
        };

        for (var row = 0; row < buffer.Rows; row++)
        {
            if (!buffer.DirtyLines [row])
            {
                continue;
            }

            buffer.DirtyLines [row] = false;

            for (var col = 0; col < buffer.Cols; col++)
            {
                int position = row * buffer.Cols + col;
                outputBuffer [position].Attribute = buffer.Contents [row, col].Attribute.GetValueOrDefault ();

                if (buffer.Contents [row, col].IsDirty == false)
                {
                    outputBuffer [position].Empty = true;
                    outputBuffer [position].Char = (char)Rune.ReplacementChar.Value;

                    continue;
                }

                outputBuffer [position].Empty = false;

                if (buffer.Contents [row, col].Rune.IsBmp)
                {
                    outputBuffer [position].Char = (char)buffer.Contents [row, col].Rune.Value;
                }
                else
                {
                    //outputBuffer [position].Empty = true;
                    outputBuffer [position].Char = (char)Rune.ReplacementChar.Value;

                    if (buffer.Contents [row, col].Rune.GetColumns () > 1 && col + 1 < buffer.Cols)
                    {
                        // TODO: This is a hack to deal with non-BMP and wide characters.
                        col++;
                        position = row * buffer.Cols + col;
                        outputBuffer [position].Empty = false;
                        outputBuffer [position].Char = ' ';
                    }
                }
            }
        }

        var damageRegion = new SmallRect
        {
            Top = 0,
            Left = 0,
            Bottom = (short)buffer.Rows,
            Right = (short)buffer.Cols
        };

        //size, ExtendedCharInfo [] charInfoBuffer, Coord , SmallRect window,
        if (!WriteToConsole (
                             new (buffer.Cols, buffer.Rows),
                             outputBuffer,
                             bufferCoords,
                             damageRegion,
                             false))
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        SmallRect.MakeEmpty (ref damageRegion);
    }

    public bool WriteToConsole (Size size, ExtendedCharInfo [] charInfoBuffer, Coord bufferSize, SmallRect window, bool force16Colors)
    {
        var stringBuilder = new StringBuilder ();

        //Debug.WriteLine ("WriteToConsole");

        //if (_screenBuffer == nint.Zero)
        //{
        //    ReadFromConsoleOutput (size, bufferSize, ref window);
        //}

        var result = false;

        if (force16Colors)
        {
            var i = 0;
            CharInfo [] ci = new CharInfo [charInfoBuffer.Length];

            foreach (ExtendedCharInfo info in charInfoBuffer)
            {
                ci [i++] = new ()
                {
                    Char = new () { UnicodeChar = info.Char },
                    Attributes =
                        (ushort)((int)info.Attribute.Foreground.GetClosestNamedColor16 () | ((int)info.Attribute.Background.GetClosestNamedColor16 () << 4))
                };
            }

            result = WriteConsoleOutput (_screenBuffer, ci, bufferSize, new () { X = window.Left, Y = window.Top }, ref window);
        }
        else
        {
            stringBuilder.Clear ();

            stringBuilder.Append (EscSeqUtils.CSI_SaveCursorPosition);
            stringBuilder.Append (EscSeqUtils.CSI_SetCursorPosition (0, 0));

            Attribute? prev = null;

            foreach (ExtendedCharInfo info in charInfoBuffer)
            {
                Attribute attr = info.Attribute;

                if (attr != prev)
                {
                    prev = attr;
                    stringBuilder.Append (EscSeqUtils.CSI_SetForegroundColorRGB (attr.Foreground.R, attr.Foreground.G, attr.Foreground.B));
                    stringBuilder.Append (EscSeqUtils.CSI_SetBackgroundColorRGB (attr.Background.R, attr.Background.G, attr.Background.B));
                }

                if (info.Char != '\x1b')
                {
                    if (!info.Empty)
                    {
                        stringBuilder.Append (info.Char);
                    }
                }
                else
                {
                    stringBuilder.Append (' ');
                }
            }

            stringBuilder.Append (EscSeqUtils.CSI_RestoreCursorPosition);
            stringBuilder.Append (EscSeqUtils.CSI_HideCursor);

            var s = stringBuilder.ToString ();

            // TODO: requires extensive testing if we go down this route
            // If console output has changed
            //if (s != _lastWrite)
            //{
            // supply console with the new content
            result = WriteConsole (_screenBuffer, s, (uint)s.Length, out uint _, nint.Zero);

            foreach (SixelToRender sixel in Application.Sixel)
            {
                SetCursorPosition ((short)sixel.ScreenPosition.X, (short)sixel.ScreenPosition.Y);
                WriteConsole (_screenBuffer, sixel.SixelData, (uint)sixel.SixelData.Length, out uint _, nint.Zero);
            }
        }

        if (!result)
        {
            int err = Marshal.GetLastWin32Error ();

            if (err != 0)
            {
                throw new Win32Exception (err);
            }
        }

        return result;
    }

    public Size GetWindowSize ()
    {
        var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
        csbi.cbSize = (uint)Marshal.SizeOf (csbi);

        if (!GetConsoleScreenBufferInfoEx (_screenBuffer, ref csbi))
        {
            //throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
            return Size.Empty;
        }

        Size sz = new (
                       csbi.srWindow.Right - csbi.srWindow.Left + 1,
                       csbi.srWindow.Bottom - csbi.srWindow.Top + 1);

        return sz;
    }

    /// <inheritdoc/>
    public void SetCursorVisibility (CursorVisibility visibility)
    {
        var sb = new StringBuilder ();
        sb.Append (visibility != CursorVisibility.Invisible ? EscSeqUtils.CSI_ShowCursor : EscSeqUtils.CSI_HideCursor);
        Write (sb.ToString ());
    }

    private Point _lastCursorPosition;

    /// <inheritdoc/>
    public void SetCursorPosition (int col, int row)
    {
        if (_lastCursorPosition.X == col && _lastCursorPosition.Y == row)
        {
            return;
        }

        _lastCursorPosition = new (col, row);

        SetConsoleCursorPosition (_screenBuffer, new ((short)col, (short)row));
    }

    private bool _isDisposed;

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_screenBuffer != nint.Zero)
        {
            try
            {
                CloseHandle (_screenBuffer);
            }
            catch (Exception e)
            {
                Logging.Logger.LogError (e, "Error trying to close screen buffer handle in WindowsOutput via interop method");
            }
        }

        _isDisposed = true;
    }
}

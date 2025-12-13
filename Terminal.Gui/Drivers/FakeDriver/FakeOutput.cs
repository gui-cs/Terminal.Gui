using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <para>
///         Pure ANSI console output for testing that captures output buffer state while optionally
///         writing ANSI escape sequences to a real terminal.
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
public class FakeOutput : OutputBase, IOutput
{
   // private readonly StringBuilder _outputStringBuilder = new ();
    private Size _consoleSize = new (80, 25);
    private IOutputBuffer? _lastBuffer;
    private bool _terminalInitialized;

    /// <summary>
    ///     Initializes a new instance of <see cref="FakeOutput"/>.
    ///     Checks if a real console is available for ANSI output.
    /// </summary>
    public FakeOutput ()
    {
        _lastBuffer = new OutputBufferImpl ();
        _lastBuffer.SetSize (80, 25);

        try
        {
            // Check if console is available (not redirected)
            if (!Console.IsOutputRedirected && !Console.IsInputRedirected)
            {
                Stream stream = Console.OpenStandardOutput ();

                if (stream.CanWrite)
                {
                    _terminalInitialized = true;
                }
            }
        }
        catch
        {
            _terminalInitialized = false;
        }
    }

    /// <summary>
    ///     Gets or sets the last output buffer written. The <see cref="IOutputBuffer.Contents"/> contains
    ///     a reference to the buffer last written with <see cref="Write(IOutputBuffer)"/>.
    /// </summary>
    public IOutputBuffer? GetLastBuffer () => _lastBuffer;

    ///// <inheritdoc cref="IOutput.GetLastOutput"/>
    //public override string GetLastOutput () => _outputStringBuilder.ToString ();

    /// <inheritdoc />
    public void SetSize (int width, int height)
    {
        _consoleSize = new (width, height);
    }

    /// <inheritdoc/>
    public Size GetSize ()
    {
        return _consoleSize;
    }

    /// <inheritdoc />
    protected override void Write (StringBuilder output)
    {
        base.Write (output);

        if (!_terminalInitialized)
        {
            return;
        }

        try
        {
            Console.Out.Write (output);
        }
        catch
        {
            // ignore for unit tests
        }
    }


    /// <inheritdoc />
    public void Write (ReadOnlySpan<char> text)
    {
        if (!_terminalInitialized)
        {
            return;
        }

        try
        {
            Console.Out.Write (text);
        }
        catch
        {
            // ignore for unit tests
        }
    }
    /// <inheritdoc cref="IOutput.Write(IOutputBuffer)"/>
    public override void Write (IOutputBuffer buffer)
    {
        _lastBuffer = buffer;
        base.Write (buffer);
    }

    private Point? _lastCursorPosition;
    private EscSeqUtils.DECSCUSR_Style? _currentDecscusrStyle;


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

    /// <inheritdoc cref="IOutput.SetCursorVisibility"/>
    public override void SetCursorVisibility (CursorVisibility visibility)
    {
        if (!_terminalInitialized)
        {
            return;
        }

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
    protected override bool SetCursorPositionImpl (int screenPositionX, int screenPositionY)
    {
        if (_lastCursorPosition is { } && _lastCursorPosition.Value.X == screenPositionX && _lastCursorPosition.Value.Y == screenPositionY)
        {
            return true;
        }

        _lastCursorPosition = new (screenPositionX, screenPositionY);

        if (!_terminalInitialized)
        {
            return true;
        }

        try
        {
            EscSeqUtils.CSI_WriteCursorPosition (Console.Out, screenPositionY, screenPositionX);
        }
        catch
        {
            // ignore
        }

        return true;
    }

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

    /// <inheritdoc/>
    public void Dispose ()
    {
        // Nothing to dispose
    }
}

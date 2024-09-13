#nullable enable

namespace Terminal.Gui.ConsoleDrivers.Windows.Interop;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using DWORD = uint;

/// <summary>
///     Native type for Windows interop.<br/>
///     Bit-flagged value for control of console operation.
/// </summary>
/// <remarks>
///     <para>
///         This type should not be used outside of interop with the Win32 APIs.
///     </para>
///     <para>Note that some bits have different effects depending on whether they are used with an input our output stream.</para>
///     <para>
///         Use of the boolean properties instead of direct bitwise manipulation is recommended, as the properties help enforce
///         certain constraints on legal values.
///     </para>
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/windows/console/setconsolemode"/>
[SuppressMessage (
                     "ReSharper",
                     "InconsistentNaming",
                     Justification = "Following recommendation to keep types named the same as the native types.")]
[StructLayout (LayoutKind.Explicit, Size = 4)]
internal record struct CONSOLE_MODE : IEqualityOperators<CONSOLE_MODE, CONSOLE_MODE, bool>
{
    private const DWORD ALL_KNOWN_FLAGS_MASK =
        DISABLE_NEWLINE_AUTO_RETURN_MASK
      | ENABLE_AUTO_POSITION_MASK
      | ENABLE_ECHO_INPUT_MASK
      | ENABLE_EXTENDED_FLAGS_MASK
      | ENABLE_INSERT_MODE_MASK
      | ENABLE_LINE_INPUT_MASK
      | ENABLE_LVB_GRID_WORLDWIDE_MASK
      | ENABLE_MOUSE_INPUT_MASK
      | ENABLE_PROCESSED_INPUT_MASK
      | ENABLE_QUICK_EDIT_MODE_MASK
      | ENABLE_VIRTUAL_TERMINAL_INPUT_MASK
      | ENABLE_VIRTUAL_TERMINAL_PROCESSING_MASK
      | ENABLE_WRAP_AT_EOL_OUTPUT_MASK
      | ENABLE_PROCESSED_OUTPUT_MASK
      | ENABLE_WINDOW_INPUT_MASK;

    /// <summary>
    ///     <para>
    ///         When writing with WriteFile or WriteConsole, this adds an additional state to end-of-line wrapping that can delay the
    ///         cursor move and buffer scroll operations.
    ///     </para>
    ///     <para>
    ///         Normally when <see cref="ENABLE_WRAP_AT_EOL_OUTPUT"/> is set and text
    ///         reaches the end of the line, the cursor will immediately move to the next line and the contents of the buffer will scroll
    ///         up by one line.<br/>
    ///         In contrast with this flag set, the cursor does not move to the next line, and the scroll operation is not performed.
    ///     </para>
    ///     <para>
    ///         The written character will be printed in the final position on the line and the cursor will remain above this character,
    ///         as if <see cref="ENABLE_WRAP_AT_EOL_OUTPUT"/> was off, but the next printable character will be printed as if
    ///         <see cref="ENABLE_WRAP_AT_EOL_OUTPUT"/> is on. No overwrite will occur.<br/>
    ///         Specifically, the cursor quickly advances down to the following line, a scroll is performed if necessary, the character
    ///         is printed, and the cursor advances one more position.
    ///     </para>
    ///     <para>
    ///         The typical usage of this flag is intended in conjunction with setting <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/>
    ///         to better emulate a terminal emulator where writing the final character
    ///         on the screen (../in the bottom right corner) without triggering an immediate scroll is the desired behavior.
    ///     </para>
    ///     <para>
    ///         This flag will not modify any other flags, including <see cref="ENABLE_WRAP_AT_EOL_OUTPUT"/> and
    ///         <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/>.
    ///     </para>
    /// </summary>
    private const DWORD DISABLE_NEWLINE_AUTO_RETURN_MASK = 0x00000008;

    /// <summary>
    ///     Undocumented.
    /// </summary>
    private const DWORD ENABLE_AUTO_POSITION_MASK = 0x00000100;

    /// <summary>
    ///     Characters read by the ReadFile or ReadConsole function are written to the active screen buffer as they are typed into the
    ///     console.
    /// </summary>
    /// <remarks>
    ///     This mode can be used only if the ENABLE_LINE_INPUT mode is also enabled.
    /// </remarks>
    private const DWORD ENABLE_ECHO_INPUT_MASK = 0x00000004;

    /// <summary>
    ///     Required flag to enable or disable certain other flags.
    /// </summary>
    private const DWORD ENABLE_EXTENDED_FLAGS_MASK = 0x00000080;

    /// <summary>
    ///     When enabled, text entered in a console window will be inserted at the current cursor location and all text following that
    ///     location will not be overwritten.<br/>
    ///     When disabled, all following text will be overwritten.
    /// </summary>
    private const DWORD ENABLE_INSERT_MODE_MASK = 0x00000020;

    /// <summary>
    ///     The ReadFile or ReadConsole function returns only when a carriage return character is read.<br/>
    ///     If this mode is disabled, the functions return when one or more characters are available.
    /// </summary>
    private const DWORD ENABLE_LINE_INPUT_MASK = 0x00000002;

    /// <summary>
    ///     <para>
    ///         The APIs for writing character attributes including WriteConsoleOutput and WriteConsoleOutputAttribute allow the usage of
    ///         flags from character attributes to adjust the color of the foreground and background of text. Additionally, a range of
    ///         DBCS flags was specified with the COMMON_LVB prefix. Historically, these flags only functioned in DBCS code pages for
    ///         Chinese, Japanese, and Korean languages.
    ///     </para>
    ///     <para>
    ///         Setting this console mode flag will allow these attributes to be used in every code page on every language.
    ///     </para>
    ///     <para>
    ///         It is off by default to maintain compatibility with known applications that have historically taken advantage of the
    ///         console ignoring these flags on non-CJK machines to store bits in these fields for their own purposes or by accident.
    ///     </para>
    ///     <para>
    ///         Note that using the <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/> mode can result in LVB grid and reverse video flags
    ///         being set
    ///         while this flag is still off if the attached application requests underlining or inverse video via Console Virtual
    ///         Terminal Sequences.
    ///     </para>
    /// </summary>
    private const DWORD ENABLE_LVB_GRID_WORLDWIDE_MASK = 0x00000010;

    /// <summary>
    ///     If the mouse pointer is within the borders of the console window and the window has the keyboard focus, mouse events
    ///     generated by mouse movement and button presses are placed in the input buffer.
    /// </summary>
    /// <remarks>
    ///     These events are discarded by ReadFile or ReadConsole, even when this mode is enabled.<br/>
    ///     The ReadConsoleInput function can be used to read MOUSE_EVENT input records from the input buffer.
    /// </remarks>
    private const DWORD ENABLE_MOUSE_INPUT_MASK = 0x00000010;

    /// <summary>
    ///     CTRL+C is processed by the system and is not placed in the input buffer.<br/>
    ///     If the input buffer is being read by ReadFile or ReadConsole, other control keys are processed by the system and are not
    ///     returned in the ReadFile or ReadConsole buffer.<br/>
    ///     If the ENABLE_LINE_INPUT mode is also enabled, backspace, carriage return, and line feed characters are handled by the
    ///     system.
    /// </summary>
    private const DWORD ENABLE_PROCESSED_INPUT_MASK = 0x00000001;

    /// <summary>
    ///     Characters written by the WriteFile or WriteConsole function or echoed by the ReadFile or ReadConsole function are parsed for
    ///     ASCII control sequences, and the correct action is performed. Backspace, tab, bell, carriage return, and line feed characters
    ///     are processed.<br/>It should be enabled when using control sequences or when <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/>
    ///     is set.
    /// </summary>
    private const DWORD ENABLE_PROCESSED_OUTPUT_MASK = 0x00000001;

    /// <summary>
    ///     This flag enables the user to use the mouse to select and edit text.
    /// </summary>
    /// <remarks>
    ///     To enable this mode, use <see cref="ENABLE_QUICK_EDIT_MODE"/> | <see cref="ENABLE_EXTENDED_FLAGS"/>.<br/>
    ///     To disable this mode, use <see cref="ENABLE_EXTENDED_FLAGS"/> without this flag.
    /// </remarks>
    private const DWORD ENABLE_QUICK_EDIT_MODE_MASK = 0x00000040;

    /// <summary>
    ///     Setting this flag directs the Virtual Terminal processing engine to convert user input received by the console window into
    ///     Console Virtual Terminal Sequences that can be retrieved by a supporting application through ReadFile or ReadConsole
    ///     functions.
    /// </summary>
    /// <remarks>
    ///     The typical usage of this flag is intended in conjunction with <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/> on the output
    ///     handle to connect to an application that communicates exclusively via virtual terminal sequences.
    /// </remarks>
    private const DWORD ENABLE_VIRTUAL_TERMINAL_INPUT_MASK = 0x00000200;

    /// <summary>
    ///     When writing with WriteFile or WriteConsole, characters are parsed for VT100 and similar control character sequences that
    ///     control cursor movement, color/font mode, and other operations that can also be performed via the existing Console APIs.
    /// </summary>
    /// <remarks>
    ///     Ensure <see cref="ENABLE_PROCESSED_OUTPUT"/> is set when using this flag.
    /// </remarks>
    private const DWORD ENABLE_VIRTUAL_TERMINAL_PROCESSING_MASK = 0x00000004;

    /// <summary>
    ///     User interactions that change the size of the console screen buffer are reported in the console's input buffer.<br/>
    ///     Information about these events can be read from the input buffer by applications using the ReadConsoleInput function, but not
    ///     by those using ReadFile or ReadConsole.
    /// </summary>
    private const DWORD ENABLE_WINDOW_INPUT_MASK = 0x00000008;

    /// <summary>
    ///     When writing with WriteFile or WriteConsole or echoing with ReadFile or ReadConsole, the cursor moves to the beginning of the
    ///     next row when it reaches the end of the current row. This causes the rows displayed in the console window to scroll up
    ///     automatically when the cursor advances beyond the last row in the window. It also causes the contents of the console screen
    ///     buffer to scroll up (../discarding the top row of the console screen buffer) when the cursor advances beyond the last row in
    ///     the console screen buffer.<br/>
    ///     If this mode is disabled, the last character in the row is overwritten with any subsequent characters.
    /// </summary>
    private const DWORD ENABLE_WRAP_AT_EOL_OUTPUT_MASK = 0x00000002;

    public CONSOLE_MODE (DWORD value) { _value = value; }

    [FieldOffset (0)]
    [MarshalAs (UnmanagedType.U4)]
    private DWORD _value;

    /// <inheritdoc cref="uint.CompareTo(object?)"/>
    public readonly int CompareTo (object? obj) => _value.CompareTo (obj);

    public static explicit operator checked CONSOLE_MODE (DWORD value)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual (value, value & ALL_KNOWN_FLAGS_MASK, nameof (value));

        return new (value);
    }

    public static explicit operator CONSOLE_MODE (DWORD value) => new (value);

    public static implicit operator DWORD (CONSOLE_MODE value) => value._value;

    /// <inheritdoc cref="uint.ToString(string?,System.IFormatProvider?)"/>
    public readonly string ToString (string? format, IFormatProvider? formatProvider) => _value.ToString (format, formatProvider);

    /// <inheritdoc cref="uint.TryFormat(Span{char},out int,ReadOnlySpan{char},IFormatProvider?)"/>
    public readonly bool TryFormat (Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        _value.TryFormat (destination, out charsWritten, format, provider);

    /// <summary>
    ///     Gets or sets the <see cref="DISABLE_NEWLINE_AUTO_RETURN"/> flag.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When writing with WriteFile or WriteConsole, this adds an additional state to end-of-line wrapping that can delay the
    ///         cursor move and buffer scroll operations.
    ///     </para>
    ///     <para>
    ///         Normally when <see cref="ENABLE_WRAP_AT_EOL_OUTPUT"/> is set and text reaches the end of the line, the cursor will
    ///         immediately move to the next line and the contents of the buffer will scroll up by one line.<br/>
    ///         With this flag set, the cursor does not move to the next line, and the scroll operation is not performed.
    ///     </para>
    ///     <para>
    ///         The written character will be printed in the final position on the line and the cursor will remain above this character,
    ///         as if <see cref="ENABLE_WRAP_AT_EOL_OUTPUT"/> was off, but the next printable character will be printed as if
    ///         <see cref="ENABLE_WRAP_AT_EOL_OUTPUT"/> is on. No overwrite will occur.<br/>
    ///         Specifically, the cursor quickly advances down to the following line, a scroll is performed if necessary, the character
    ///         is printed, and the cursor advances one more position.
    ///     </para>
    ///     <para>
    ///         The typical usage of this flag is intended in conjunction with setting <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/>
    ///         to better emulate a terminal emulator where writing the final character on the screen without triggering an immediate
    ///         scroll is the desired behavior.
    ///     </para>
    ///     <para>
    ///         This flag will not modify any other flags, including <see cref="ENABLE_WRAP_AT_EOL_OUTPUT"/> and
    ///         <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/>.
    ///     </para>
    /// </remarks>
    internal bool DISABLE_NEWLINE_AUTO_RETURN
    {
        readonly get => (_value & DISABLE_NEWLINE_AUTO_RETURN_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= DISABLE_NEWLINE_AUTO_RETURN_MASK;

                return;
            }

            _value &= ~DISABLE_NEWLINE_AUTO_RETURN_MASK;
        }
    }

    /// <summary>
    ///     Gets or sets the <see cref="ENABLE_AUTO_POSITION"/> flag, which is not documented on Microsoft Learn.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The results of setting or clearing this flag are not documented.<br/>
    ///         It is probably best to just preserve its value across calls to <see cref="PInvoke.GetConsoleMode"/> and
    ///         <see cref="PInvoke.SetConsoleMode"/>.
    ///     </para>
    ///     <para>Included in this type definition for completeness only.</para>
    /// </remarks>
    internal bool ENABLE_AUTO_POSITION
    {
        readonly get => (_value & ENABLE_AUTO_POSITION_MASK) != 0;
        set

        {
            if (value)
            {
                _value |= ENABLE_AUTO_POSITION_MASK;

                return;
            }

            _value &= ~ENABLE_AUTO_POSITION_MASK;
        }
    }

    /// <summary>Gets or sets the <see cref="ENABLE_ECHO_INPUT"/> flag.</summary>
    /// <remarks>
    ///     <para>
    ///         Characters read by the ReadFile or ReadConsole function are written to the active screen buffer as they are typed into
    ///         the console.
    ///     </para>
    ///     <para>
    ///         This mode can be used only if the <see cref="ENABLE_LINE_INPUT"/> flag is also set.
    ///     </para>
    ///     <para>Setting this flag will automatically set the <see cref="ENABLE_LINE_INPUT"/> flag.</para>
    ///     <para>Clearing this flag will not change the value of <see cref="ENABLE_LINE_INPUT"/>.</para>
    /// </remarks>
    internal bool ENABLE_ECHO_INPUT
    {
        readonly get => (_value & ENABLE_ECHO_INPUT_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_ECHO_INPUT_MASK;

                return;
            }

            _value &= ~ENABLE_ECHO_INPUT_MASK;
        }
    }

    /// <summary>Gets or sets the <see cref="ENABLE_EXTENDED_FLAGS"/> flag.</summary>
    /// <remarks>
    ///     <para>
    ///         Required flag to enable or disable certain other flags.
    ///     </para>
    ///     <para>
    ///         Clearing this flag will clear all other flags which require this flag to be set.<br/>
    ///         Setting this flag will not change any other flag value.
    ///     </para>
    /// </remarks>
    internal bool ENABLE_EXTENDED_FLAGS
    {
        readonly get => (_value & ENABLE_EXTENDED_FLAGS_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_EXTENDED_FLAGS_MASK;

                return;
            }

            _value &= ~ENABLE_EXTENDED_FLAGS_MASK;
        }
    }

    /// <summary>Gets or sets the <see cref="ENABLE_INSERT_MODE"/> flag.</summary>
    /// <remarks>
    ///     When enabled, text entered in a console window will be inserted at the current cursor location and all text following that
    ///     location will not be overwritten.<br/>
    ///     When disabled, all following text will be overwritten.
    /// </remarks>
    internal bool ENABLE_INSERT_MODE
    {
        readonly get => (_value & ENABLE_INSERT_MODE_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_INSERT_MODE_MASK;

                return;
            }

            _value &= ~ENABLE_INSERT_MODE_MASK;
        }
    }

    /// <summary>Gets or sets the <see cref="ENABLE_LINE_INPUT"/> flag.</summary>
    /// <remarks>
    ///     The ReadFile or ReadConsole function returns only when a carriage return character is read.<br/>
    ///     If this mode is disabled, the functions return when one or more characters are available.
    /// </remarks>
    /// <remarks>Clearing this flag will automatically clear the <see cref="ENABLE_ECHO_INPUT"/> and  flag.</remarks>
    /// <remarks>Setting this flag to <see langword="true"/> will not change the value of <see cref="ENABLE_ECHO_INPUT"/>.</remarks>
    /// <seealso cref="ENABLE_LINE_INPUT_MASK"/>
    internal bool ENABLE_LINE_INPUT
    {
        readonly get => (_value & ENABLE_LINE_INPUT_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_LINE_INPUT_MASK;

                return;
            }

            _value &= ~ENABLE_LINE_INPUT_MASK;
        }
    }

    internal bool ENABLE_LVB_GRID_WORLDWIDE
    {
        readonly get => (_value & ENABLE_LVB_GRID_WORLDWIDE_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_LVB_GRID_WORLDWIDE_MASK;

                return;
            }

            _value &= ~ENABLE_LVB_GRID_WORLDWIDE_MASK;
        }
    }

    /// <summary>
    ///     Gets or sets the <see cref="ENABLE_MOUSE_INPUT"/> flag.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the mouse pointer is within the borders of the console window and the window has the keyboard focus, mouse events
    ///         generated by mouse movement and button presses are placed in the input buffer.<br/>
    ///         These events are discarded by ReadFile or ReadConsole, even when this mode is enabled.
    ///     </para>
    ///     <para>
    ///         The ReadConsoleInput function can be used to read MOUSE_EVENT input records from the input buffer.
    ///     </para>
    /// </remarks>
    internal bool ENABLE_MOUSE_INPUT
    {
        readonly get => (_value & ENABLE_MOUSE_INPUT_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_MOUSE_INPUT_MASK;

                return;
            }

            _value &= ~ENABLE_MOUSE_INPUT_MASK;
        }
    }

    /// <summary>Gets or sets the <see cref="ENABLE_PROCESSED_INPUT"/> flag.</summary>
    /// <remarks>
    ///     CTRL+C is processed by the system and is not placed in the input buffer.<br/>
    ///     If the input buffer is being read by ReadFile or ReadConsole, other control keys are processed by the system and are not
    ///     returned in the ReadFile or ReadConsole buffer.<br/>
    ///     If the <see cref="ENABLE_LINE_INPUT"/> mode is also enabled, backspace, carriage return, and line feed characters are handled
    ///     by the
    ///     system.
    /// </remarks>
    internal bool ENABLE_PROCESSED_INPUT
    {
        readonly get => (_value & ENABLE_PROCESSED_INPUT_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_PROCESSED_INPUT_MASK;

                return;
            }

            _value &= ~ENABLE_PROCESSED_INPUT_MASK;
        }
    }

    /// <summary>Gets or sets the <see cref="ENABLE_PROCESSED_OUTPUT"/> flag.</summary>
    /// <remarks>
    ///     <para>
    ///         Characters written by the WriteFile or WriteConsole function or echoed by the ReadFile or ReadConsole function are parsed
    ///         for ANSI control sequences, and the correct action is performed.
    ///     </para>
    ///     <para>
    ///         Backspace, tab, bell, carriage return, and line feed characters are processed.
    ///     </para>
    ///     <para>
    ///         It must be enabled when using control sequences or when <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/> is set.
    ///     </para>
    ///     <para>
    ///         Clearing this flag automatically clears the <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/> flag.<br/>
    ///         Setting this flag will not change the <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/> flag.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/>
    internal bool ENABLE_PROCESSED_OUTPUT
    {
        readonly get => (_value & ENABLE_PROCESSED_OUTPUT_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_PROCESSED_OUTPUT_MASK;

                return;
            }

            _value &= ~ENABLE_PROCESSED_OUTPUT_MASK;
        }
    }

    /// <summary>Gets or sets the <see cref="ENABLE_QUICK_EDIT_MODE"/> flag.</summary>
    /// <remarks>
    ///     <para>
    ///         This flag enables the user to use the mouse to select and edit text.
    ///     </para>
    ///     <para>
    ///         Setting this flag automatically sets the <see cref="ENABLE_EXTENDED_FLAGS"/> flag.<br/>
    ///         Clearing this flag will not change the <see cref="ENABLE_EXTENDED_FLAGS"/> flag.
    ///     </para>
    /// </remarks>
    internal bool ENABLE_QUICK_EDIT_MODE
    {
        readonly get => (_value & ENABLE_QUICK_EDIT_MODE_MASK) != 0;
        set
        {
            if (value)
            {
                Interlocked.Or (ref _value, ENABLE_QUICK_EDIT_MODE_MASK | ENABLE_EXTENDED_FLAGS_MASK);

                return;
            }

            _value &= ~ENABLE_QUICK_EDIT_MODE_MASK;
        }
    }

    /// <summary>Gets or sets the <see cref="ENABLE_VIRTUAL_TERMINAL_INPUT"/> flag.</summary>
    /// <remarks>
    ///     <para>
    ///         Setting this flag directs the Virtual Terminal processing engine to convert user input received by the console window
    ///         into Console Virtual Terminal Sequences that can be retrieved by a supporting application through ReadFile or ReadConsole
    ///         functions.
    ///     </para>
    ///     <para>
    ///         The typical usage of this flag is intended in conjunction with <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/> on the
    ///         output handle to connect to an application that communicates exclusively via virtual terminal sequences.
    ///     </para>
    /// </remarks>
    internal bool ENABLE_VIRTUAL_TERMINAL_INPUT
    {
        readonly get => (_value & ENABLE_VIRTUAL_TERMINAL_INPUT_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_VIRTUAL_TERMINAL_INPUT_MASK;

                return;
            }

            _value &= ~ENABLE_VIRTUAL_TERMINAL_INPUT_MASK;
        }
    }

    /// <summary>Gets or sets the <see cref="ENABLE_VIRTUAL_TERMINAL_PROCESSING"/> flag.</summary>
    /// <remarks>
    ///     <para>
    ///         When writing with WriteFile or WriteConsole, characters are parsed for VT100 and similar control character sequences that
    ///         control cursor movement, color/font mode, and other operations that can also be performed via the existing Console APIs.
    ///     </para>
    ///     <para>
    ///         Setting this flag automatically sets the <see cref="ENABLE_PROCESSED_OUTPUT"/> flag.<br/>
    ///         Clearing this flag will not change the <see cref="ENABLE_PROCESSED_OUTPUT"/> flag.<br/>
    ///     </para>
    /// </remarks>
    internal bool ENABLE_VIRTUAL_TERMINAL_PROCESSING
    {
        readonly get => (_value & ENABLE_VIRTUAL_TERMINAL_PROCESSING_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_VIRTUAL_TERMINAL_PROCESSING_MASK | ENABLE_PROCESSED_OUTPUT_MASK;

                return;
            }

            _value &= ~ENABLE_VIRTUAL_TERMINAL_PROCESSING_MASK;
        }
    }

    /// <summary>Gets or sets the <see cref="ENABLE_WINDOW_INPUT"/> flag.</summary>
    /// <remarks>
    ///     <para>
    ///         User interactions that change the size of the console screen buffer are reported in the console's input buffer.
    ///     </para>
    ///     <para>
    ///         Information about these events can be read from the input buffer by applications using the ReadConsoleInput function, but
    ///         not by those using ReadFile or ReadConsole.
    ///     </para>
    /// </remarks>
    /// <remarks>
    ///     <para>
    ///         Setting this flag will clear the <see cref="ENABLE_LINE_INPUT"/> flag.
    ///     </para>
    ///     <para>
    ///         Clearing this flag will not change the <see cref="ENABLE_LINE_INPUT"/> flag.
    ///     </para>
    /// </remarks>
    internal bool ENABLE_WINDOW_INPUT
    {
        readonly get => (_value & ENABLE_WINDOW_INPUT_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_WINDOW_INPUT_MASK;

                return;
            }

            _value &= ~ENABLE_WINDOW_INPUT_MASK;
        }
    }

    /// <summary>Gets or sets the <see cref="ENABLE_WRAP_AT_EOL_OUTPUT"/> flag.</summary>
    /// <summary>
    ///     When writing with WriteFile or WriteConsole or echoing with ReadFile or ReadConsole, the cursor moves to the beginning of the
    ///     next row when it reaches the end of the current row. This causes the rows displayed in the console window to scroll up
    ///     automatically when the cursor advances beyond the last row in the window. It also causes the contents of the console screen
    ///     buffer to scroll up (../discarding the top row of the console screen buffer) when the cursor advances beyond the last row in
    ///     the console screen buffer.<br/>
    ///     If this mode is disabled, the last character in the row is overwritten with any subsequent characters.
    /// </summary>
    /// <seealso cref="ENABLE_WRAP_AT_EOL_OUTPUT_MASK"/>
    internal bool ENABLE_WRAP_AT_EOL_OUTPUT
    {
        readonly get => (_value & ENABLE_WRAP_AT_EOL_OUTPUT_MASK) != 0;
        set
        {
            if (value)
            {
                _value |= ENABLE_WRAP_AT_EOL_OUTPUT_MASK;

                return;
            }

            _value &= ~ENABLE_WRAP_AT_EOL_OUTPUT_MASK;
        }
    }
}

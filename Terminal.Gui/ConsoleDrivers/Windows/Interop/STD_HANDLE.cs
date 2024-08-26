namespace Terminal.Gui.ConsoleDrivers.Windows.Interop;

using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Standard values for the stdin, stdout, and stderr streams for a Win32 application.
/// </summary>
/// <remarks>
///     The values for these constants are unsigned numbers, but are defined in the header files as a cast from a signed number and
///     take advantage of the C compiler rolling them over to just under the maximum 32-bit value. When interfacing with these
///     handles in a language that does not parse the headers and is re-defining the constants, please be aware of this constraint.
///     As an example, ((DWORD)-10) is actually the unsigned number 4294967286.
/// </remarks>
/// <seealso href="https://learn.microsoft.com/en-us/windows/console/getstdhandle#parameters"/>
[SuppressMessage (
                     "ReSharper",
                     "InconsistentNaming",
                     Justification = "Following recommendation to keep types named the same as the native types.")]
internal enum STD_HANDLE : uint
{
    /// <summary>Explicit zero value for .net formality.</summary>
    INVALID = 0U,

    /// <summary>The standard input device. Initially, this is the console input buffer, CONIN$.</summary>
    /// <remarks>(DWORD)-10</remarks>
    STD_INPUT_HANDLE = 4294967286U,

    /// <summary>The standard output device. Initially, this is the active console screen buffer, CONOUT$.</summary>
    /// <remarks>(DWORD)-11</remarks>
    STD_OUTPUT_HANDLE = 4294967285U,

    /// <summary>The standard error device. Initially, this is the active console screen buffer, CONOUT$.</summary>
    /// <remarks>(DWORD)-12</remarks>
    STD_ERROR_HANDLE = 4294967284U
}

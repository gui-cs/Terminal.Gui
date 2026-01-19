using System.Runtime.InteropServices;
using System.Text.Json;
using static Terminal.Gui.Drivers.WindowsConsole;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Helper class for outputting to the with the Windows Console in Virtual Terminal Sequence (VTS) mode using only
///     low-level Windows APIs.
/// </summary>
/// <remarks>
///     <para>
///         When Virtual Terminal Sequences (VTS) Output mode is enabled via <c>ENABLE_VIRTUAL_TERMINAL_PROCESSING</c>,
///         the Windows Console converts user input (keyboard, mouse) into ANSI escape sequences that
///         can be read via standard input APIs like <c>ReadFile</c> or <c>Console.OpenStandardInput()</c>.
///     </para>
///     <para>
///         This provides a unified, cross-platform ANSI input mechanism where:
///         <list type="bullet">
///             <item>Keyboard input becomes ANSI sequences (e.g., Arrow Up = ESC[A)</item>
///             <item>Mouse input becomes SGR format sequences (e.g., ESC[&lt;0;10;5M)</item>
///             <item>All input can be parsed uniformly with <see cref="AnsiResponseParser{TInputRecord}"/></item>
///         </list>
///     </para>
/// </remarks>
internal sealed class WindowsVTOutputHelper : IDisposable
{
    #region P/Invoke Declarations

    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;
    private const int STD_OUTPUT_HANDLE = -11;

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle (int nStdHandle);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll")]
    private static extern uint GetLastError ();

    [DllImport ("kernel32.dll")]
    public static extern bool WriteFile (
        nint hConsoleHandle,
        byte [] lpBuffer,
        uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten, 
        nint lpOverlapped);

    #endregion

    private uint _originalConsoleMode;
    private bool _disposed;

    /// <summary>
    ///     Gets whether VTS output mode was successfully enabled.
    /// </summary>
    public bool IsEnabled
    {
        get => field;
        private set
        {
            field = value;
            Logging.Trace ($"{value}");
        }
    }

    /// <summary>
    ///     Gets the Windows console output handle.
    /// </summary>
    public nint OutputHandle { get; private set; }

    /// <summary>
    ///     Attempts to enable Windows Virtual Terminal Output mode.
    /// </summary>
    /// <returns>True if VTS mode was enabled successfully; false otherwise.</returns>
    public bool TryEnable ()
    {
        if (IsEnabled)
        {
            return true;
        }

        // Only attempt on Windows
        if (!RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
        {
            return false;
        }

        try
        {
            OutputHandle = GetStdHandle (STD_OUTPUT_HANDLE);

            if (OutputHandle == nint.Zero || OutputHandle == new nint (-1))
            {
                Logging.Warning ("Failed to get Windows console output handle.");

                return false;
            }

            if (!GetConsoleMode (OutputHandle, out _originalConsoleMode))
            {
                Logging.Warning ("Failed to get Windows console mode.");

                return false;
            }

            uint newMode = _originalConsoleMode;

            if ((newMode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) == 0)
            {
                newMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;

                if (!SetConsoleMode (OutputHandle, newMode))
                {
                    throw new ApplicationException ($"Failed to set output console mode, error code: {GetLastError ()}.");
                }
            }

            IsEnabled = true;
            Logging.Information ($"Windows VTS output mode enabled successfully. Mode: 0x{newMode:X} (was 0x{_originalConsoleMode:X})");

            return true;
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to enable Windows VTS mode: {ex.Message}");

            return false;
        }
    }

    /// <summary>
    ///     Restores the console to its original mode.
    /// </summary>
    public void Restore ()
    {
        if (!IsEnabled || _disposed || OutputHandle == nint.Zero)
        {
            return;
        }

        try
        {
            SetConsoleMode (OutputHandle, _originalConsoleMode);
            IsEnabled = false;
            Logging.Information ("Windows console mode restored.");
        }
        catch (Exception ex)
        {
            Logging.Warning ($"Failed to restore Windows console mode: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        Restore ();
        _disposed = true;
    }

    public void Write (StringBuilder output)
    {
        // Convert StringBuilder to string and then to byte array
        byte [] byteArray = Encoding.UTF8.GetBytes (output.ToString ());

        WriteFile (OutputHandle, byteArray, (uint)byteArray.Length, out _, nint.Zero);
    }
}

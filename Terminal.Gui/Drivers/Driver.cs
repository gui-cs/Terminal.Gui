using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Holds global driver settings and cross-driver utility methods.
/// </summary>
public sealed class Driver
{
    // NOTE: Force16Colors is a configuration property (Driver.Force16Colors).
    // NOTE: IDriver also has a Force16Colors property, which is an instance property
    // NOTE: set whenever this static property is set.
    /// <summary>
    ///     Determines if driver instances should use 16 colors instead of the default TrueColors.
    /// </summary>
    /// <seealso cref="IDriver.Force16Colors"/>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool Force16Colors
    {
        get;
        set
        {
            bool oldValue = field;
            field = value;
            Force16ColorsChanged?.Invoke (null, new ValueChangedEventArgs<bool> (oldValue, field));
        }
    }

    /// <summary>Raised when <see cref="Force16Colors"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<bool>>? Force16ColorsChanged;

    // NOTE: SizeDetection is a configuration property (Driver.SizeDetection).
    // It controls which strategy the ANSI driver uses to determine the terminal's size.
    /// <summary>
    ///     Controls how the ANSI driver detects the terminal's window size.
    ///     Defaults to <see cref="SizeDetectionMode.Polling"/>, which uses a synchronous
    ///     native syscall (<c>ioctl</c> on Unix, Console API on Windows) for immediate,
    ///     reliable size information.  Set to <see cref="SizeDetectionMode.AnsiQuery"/>
    ///     to use pure-ANSI escape-sequence queries instead (useful when the native API
    ///     does not reflect the remote terminal size, e.g. over an SSH tunnel).
    /// </summary>
    /// <seealso cref="SizeDetectionMode"/>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static SizeDetectionMode SizeDetection { get; set; } = SizeDetectionMode.Polling;

    /// <summary>
    ///     Determines whether the process is attached to a real terminal (i.e. stdin/stdout
    ///     are connected to a console device rather than redirected or running inside a test harness). Set the environment
    ///     variable "DisableRealDriverIO=1" to skip real terminal detection and force this method to return false, which is
    ///     required for running in test harnesses that do not have a real terminal attached.
    /// </summary>
    /// <param name="inputAttached">
    ///     When this method returns, <see langword="true"/> if standard input is connected to a console device;
    ///     otherwise <see langword="false"/>.
    /// </param>
    /// <param name="outputAttached">
    ///     When this method returns, <see langword="true"/> if standard output is connected to a console device;
    ///     otherwise <see langword="false"/>.
    /// </param>
    /// <returns><see langword="true"/> if both input and output are attached to a terminal; otherwise <see langword="false"/>.</returns>
    public static bool IsAttachedToTerminal (out bool inputAttached, out bool outputAttached)
    {
        inputAttached = outputAttached = false;

        // When the test harness sets DisableRealDriverIO, skip real terminal detection entirely.
        if (string.Equals (Environment.GetEnvironmentVariable ("DisableRealDriverIO"), "1", StringComparison.Ordinal))
        {
            return false;
        }

        if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
        {
            const int STD_INPUT_HANDLE = -10;
            const int STD_OUTPUT_HANDLE = -11;
            nint inH = GetStdHandle (STD_INPUT_HANDLE);
            nint outH = GetStdHandle (STD_OUTPUT_HANDLE);

            inputAttached = inH != nint.Zero && GetConsoleMode (inH, out _);
            outputAttached = outH != nint.Zero && GetConsoleMode (outH, out _);

            return inputAttached && outputAttached;
        }

        const int STDIN_FILENO = 0;
        const int STDOUT_FILENO = 1;
        inputAttached = isatty (STDIN_FILENO) == 1;
        outputAttached = isatty (STDOUT_FILENO) == 1;

        return inputAttached && outputAttached;
    }

    // Unix
    [DllImport ("libc", SetLastError = true)]
    private static extern int isatty (int fd);

    // Windows
    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle (int nStdHandle);

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);
}

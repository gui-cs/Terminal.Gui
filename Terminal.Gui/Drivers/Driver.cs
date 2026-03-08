using System.Runtime.InteropServices;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Holds global driver settings and cross-driver utility methods.
/// </summary>
public sealed class Driver
{
    private static bool _force16Colors = false; // Resources/config.json overrides

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
        get => _force16Colors;
        set
        {
            bool oldValue = _force16Colors;
            _force16Colors = value;
            Force16ColorsChanged?.Invoke (null, new ValueChangedEventArgs<bool> (oldValue, _force16Colors));
        }
    }

    /// <summary>Raised when <see cref="Force16Colors"/> changes.</summary>
    public static event EventHandler<ValueChangedEventArgs<bool>>? Force16ColorsChanged;

    /// <summary>
    ///     Determines whether the process is attached to a real terminal (i.e. stdin/stdout
    ///     are connected to a console device rather than redirected or running inside a test harness).
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

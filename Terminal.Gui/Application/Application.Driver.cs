#nullable enable
namespace Terminal.Gui;

public static partial class Application // Driver abstractions
{
    internal static bool _forceFakeConsole;

    /// <summary>Gets the <see cref="ConsoleDriver"/> that has been selected. See also <see cref="ForceDriver"/>.</summary>
    public static ConsoleDriver? Driver { get; internal set; }

    /// <summary>
    ///     Gets or sets whether <see cref="Application.Driver"/> will be forced to output only the 16 colors defined in
    ///     <see cref="ColorName"/>. The default is <see langword="false"/>, meaning 24-bit (TrueColor) colors will be output
    ///     as long as the selected <see cref="ConsoleDriver"/> supports TrueColor.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool Force16Colors { get; set; }

    /// <summary>
    ///     Forces the use of the specified driver (one of "fake", "ansi", "curses", "net", or "windows"). If not
    ///     specified, the driver is selected based on the platform.
    /// </summary>
    /// <remarks>
    ///     Note, <see cref="Application.Init(ConsoleDriver, string)"/> will override this configuration setting if called
    ///     with either `driver` or `driverName` specified.
    /// </remarks>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static string ForceDriver { get; set; } = string.Empty;
}

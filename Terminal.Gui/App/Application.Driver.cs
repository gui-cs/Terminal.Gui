#nullable enable

namespace Terminal.Gui.App;

public static partial class Application // Driver abstractions
{
    internal static bool _forceFakeConsole;

    /// <summary>Gets the <see cref="IConsoleDriver"/> that has been selected. See also <see cref="ForceDriver"/>.</summary>
    public static IConsoleDriver? Driver { get; internal set; }

    // BUGBUG: Force16Colors should be nullable.
    /// <summary>
    ///     Gets or sets whether <see cref="Application.Driver"/> will be forced to output only the 16 colors defined in
    ///     <see cref="ColorName16"/>. The default is <see langword="false"/>, meaning 24-bit (TrueColor) colors will be output
    ///     as long as the selected <see cref="IConsoleDriver"/> supports TrueColor.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool Force16Colors { get; set; }

    // BUGBUG: ForceDriver should be nullable.
    /// <summary>
    ///     Forces the use of the specified driver (one of "fake", "ansi", "curses", "net", or "windows"). If not
    ///     specified, the driver is selected based on the platform.
    /// </summary>
    /// <remarks>
    ///     Note, <see cref="Application.Init(IConsoleDriver, string)"/> will override this configuration setting if called
    ///     with either `driver` or `driverName` specified.
    /// </remarks>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static string ForceDriver { get; set; } = string.Empty;

    /// <summary>
    /// Collection of sixel images to write out to screen when updating.
    /// Only add to this collection if you are sure terminal supports sixel format.
    /// </summary>
    public static List<SixelToRender> Sixel = new List<SixelToRender> ();
}

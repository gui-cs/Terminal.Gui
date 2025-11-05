#nullable enable

namespace Terminal.Gui.App;

public static partial class Application // Driver abstractions
{
    internal static bool _forceFakeConsole;

    /// <summary>Gets the <see cref="IDriver"/> that has been selected. See also <see cref="ForceDriver"/>.</summary>
    public static IDriver? Driver
    {
        get => ApplicationImpl.Instance.Driver;
        internal set => ApplicationImpl.Instance.Driver = value;
    }

    /// <summary>
    ///     Gets or sets whether <see cref="Application.Driver"/> will be forced to output only the 16 colors defined in
    ///     <see cref="ColorName16"/>. The default is <see langword="false"/>, meaning 24-bit (TrueColor) colors will be output
    ///     as long as the selected <see cref="IDriver"/> supports TrueColor.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool Force16Colors
    {
        get => ApplicationImpl.Instance.Force16Colors;
        set => ApplicationImpl.Instance.Force16Colors = value;
    }

    /// <summary>
    ///     Forces the use of the specified driver (one of "fake", "dotnet", "windows", or "unix"). If not
    ///     specified, the driver is selected based on the platform.
    /// </summary>
    /// <remarks>
    ///     Note, <see cref="Application.Init(IDriver, string)"/> will override this configuration setting if called
    ///     with either `driver` or `driverName` specified.
    /// </remarks>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static string ForceDriver
    {
        get => ApplicationImpl.Instance.ForceDriver;
        set => ApplicationImpl.Instance.ForceDriver = value;
    }

    /// <summary>
    /// Collection of sixel images to write out to screen when updating.
    /// Only add to this collection if you are sure terminal supports sixel format.
    /// </summary>
    public static List<SixelToRender> Sixel => ApplicationImpl.Instance.Sixel;
}

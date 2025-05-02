#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Describes the location of the configuration files. The constants can be combined (bitwise) to specify multiple
///     locations. The more significant the bit, the higher the priority meaning that the last location will override the
///     earlier ones.
/// </summary>

[Flags]
public enum ConfigLocations
{
    /// <summary>The values of the <see cref="ConfigProperty"/>s (static properites) will be loaded.</summary>
    /// <remarks>
    ///     Inteneded to be used for development and testing only.
    /// </remarks>
    None = 0,

    /// <summary>
    ///    Deafult configuration in <c>Terminal.Gui.dll</c>'s resources (<c>Terminal.Gui.Resources.config.json</c>).
    /// </summary>
    Default = 0b_0000_0001,

    /// <summary>
    ///     App resources (e.g. <c>MyApp.Resources.config.json</c>).
    /// </summary>
    AppResources = 0b_0000_0010,

    /// <summary>
    ///     Settings in the <see cref="ConfigurationManager.RuntimeConfig"/> static property.
    /// </summary>
    Runtime = 0b_0000_0100,

    /// <summary>
    ///     Global settings in the current directory (e.g. <c>./.tui/config.json</c>).
    /// </summary>
    GlobalCurrent = 0b_0000_1000,

    /// <summary>
    ///    Global settings in the home directory (e.g. <c>~/.tui/config.json</c>).
    /// </summary>
    GlobalHome = 0b_0001_0000,

    /// <summary>
    ///     App settings in the current directory (e.g. <c>./.tui/MyApp.config.json</c>).
    /// </summary>
    AppCurrent = 0b_0010_0000,

    /// <summary>
    ///     App settings in the home directory (e.g. <c>~/.tui/MyApp.config.json</c>).
    /// </summary>
    AppHome = 0b_0100_0000,

    /// <summary>This constant is a combination of all locations</summary>
    All = 0b_1111_1111
}

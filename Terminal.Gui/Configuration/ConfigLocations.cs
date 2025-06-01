#nullable enable
namespace Terminal.Gui.Configuration;

/// <summary>
///     Describes the location of the configuration settings. The constants can be combined (bitwise) to specify multiple
///     locations. The more significant the bit, the higher the priority the location, meaning that the last location will
///     override the
///     earlier ones.
/// </summary>
[Flags]
public enum ConfigLocations
{
    /// <summary>
    ///     No locaitons are specified. This is the default value.
    /// </summary>
    None = 0b_0000_0000,

    /// <summary>
    ///     Settings of the <see cref="ConfigurationPropertyAttribute"/> static properites when the module is
    ///     initiallly loaded.
    ///     <para>
    ///         When the module is initialized, the <see cref="ConfigurationManager"/> will retrieve the values of the
    ///         configuration
    ///         properties
    ///         from their corresponding static properties. These are default settigs available even if
    ///         <see cref="ConfigurationManager.IsEnabled"/>
    ///         is <see langword="false"/>.
    ///     </para>
    /// </summary>
    HardCoded = 0b_0000_0001,

    /// <summary>
    ///     Settings defined in <c>Terminal.Gui.dll</c>'s resources (<c>Terminal.Gui.Resources.config.json</c>).
    /// </summary>
    LibraryResources = 0b_0000_0010,

    /// <summary>
    ///     App resources (e.g. <c>MyApp.Resources.config.json</c>). See <see cref="AppSettingsScope"/>.
    /// </summary>
    AppResources = 0b_0000_0100,

    /// <summary>
    ///     Settings in the <see cref="ConfigurationManager.RuntimeConfig"/> static property.
    /// </summary>
    Runtime = 0b_0000_1000,

    /// <summary>
    ///     Global settings in the current directory (e.g. <c>./.tui/config.json</c>).
    /// </summary>
    GlobalCurrent = 0b_0001_0000,

    /// <summary>
    ///     Global settings in the home directory (e.g. <c>~/.tui/config.json</c>).
    /// </summary>
    GlobalHome = 0b_0010_0000,

    /// <summary>
    ///     App settings in the current directory (e.g. <c>./.tui/MyApp.config.json</c>).
    /// </summary>
    AppCurrent = 0b_0100_0000,

    /// <summary>
    ///     App settings in the home directory (e.g. <c>~/.tui/MyApp.config.json</c>).
    /// </summary>
    AppHome = 0b_1000_0000,

    /// <summary>This constant is a combination of all locations</summary>
    All = 0b_1111_1111
}

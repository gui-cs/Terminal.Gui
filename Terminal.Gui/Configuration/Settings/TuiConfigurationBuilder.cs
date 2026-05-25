using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Builds and manages a Terminal.Gui MEC-based configuration, loading from all standard sources
///     in the correct precedence order.
/// </summary>
/// <remarks>
///     This is the MEC-based equivalent of the legacy <see cref="SourcesManager"/>.
///     It is additive — it does not replace or remove the existing CM during the transition period.
/// </remarks>
public class TuiConfigurationBuilder
{
    private readonly string? _appName;
    private string? _runtimeConfig;
    private IConfiguration? _configuration;

    /// <summary>Initializes a new instance of <see cref="TuiConfigurationBuilder"/>.</summary>
    /// <param name="appName">The application name for app-specific config file discovery. If null, uses entry assembly name.</param>
    public TuiConfigurationBuilder (string? appName = null)
    {
        _appName = appName ?? System.Reflection.Assembly.GetEntryAssembly ()?.GetName ().Name;
    }

    /// <summary>
    ///     Gets or sets the runtime configuration JSON string (highest priority).
    ///     Setting this invalidates the cached configuration, causing a rebuild on next access.
    /// </summary>
    public string? RuntimeConfig
    {
        get => _runtimeConfig;
        set
        {
            _runtimeConfig = value;
            _configuration = null; // force rebuild
        }
    }

    /// <summary>
    ///     Gets the built <see cref="IConfiguration"/> instance. Lazily built on first access.
    ///     Rebuilt when <see cref="RuntimeConfig"/> changes.
    /// </summary>
    public IConfiguration Configuration => _configuration ??= Build ();

    /// <summary>
    ///     Builds the configuration from all sources in precedence order.
    /// </summary>
    /// <returns>The built configuration root.</returns>
    public IConfiguration Build ()
    {
        IConfigurationBuilder builder = new ConfigurationBuilder ()
                                        .AddTuiLibraryDefaults ()
                                        .AddTuiAppDefaults (_appName)
                                        .AddTuiUserFiles (_appName)
                                        .AddTuiEnvironmentVariable ()
                                        .AddTuiRuntimeConfig (_runtimeConfig);

        _configuration = builder.Build ();

        return _configuration;
    }

    /// <summary>
    ///     Applies the current configuration to all static settings facades.
    ///     Call this after building or rebuilding to push MEC values to the static <c>Defaults</c> properties.
    /// </summary>
    public void ApplyToStaticFacades ()
    {
        IConfiguration config = Configuration;

        // Bind ApplicationSettings
        ApplicationSettings appSettings = new ();
        config.GetSection ("Application").Bind (appSettings);
        ApplicationSettings.Defaults = appSettings;

        // Bind DriverSettings
        DriverSettings driverSettings = new ();
        config.GetSection ("Driver").Bind (driverSettings);
        DriverSettings.Defaults = driverSettings;

        // Bind ButtonSettings
        ButtonSettings buttonSettings = new ();
        config.GetSection ("Button").Bind (buttonSettings);
        ButtonSettings.Defaults = buttonSettings;

        // Bind DialogSettings
        DialogSettings dialogSettings = new ();
        config.GetSection ("Dialog").Bind (dialogSettings);
        DialogSettings.Defaults = dialogSettings;

        // Bind WindowSettings
        WindowSettings windowSettings = new ();
        config.GetSection ("Window").Bind (windowSettings);
        WindowSettings.Defaults = windowSettings;

        // Bind MessageBoxSettings
        MessageBoxSettings messageBoxSettings = new ();
        config.GetSection ("MessageBox").Bind (messageBoxSettings);
        MessageBoxSettings.Defaults = messageBoxSettings;

        // Bind CheckBoxSettings
        CheckBoxSettings checkBoxSettings = new ();
        config.GetSection ("CheckBox").Bind (checkBoxSettings);
        CheckBoxSettings.Defaults = checkBoxSettings;
    }
}

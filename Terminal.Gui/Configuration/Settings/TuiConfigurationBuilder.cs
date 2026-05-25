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

        // SettingsScope POCOs
        BindSection<ApplicationSettings> (config, "Application", s => ApplicationSettings.Defaults = s);
        BindSection<DriverSettings> (config, "Driver", s => DriverSettings.Defaults = s);
        BindSection<FileDialogSettings> (config, "FileDialog", s => FileDialogSettings.Defaults = s);
        BindSection<FileDialogStyleSettings> (config, "FileDialogStyle", s => FileDialogStyleSettings.Defaults = s);
        BindSection<KeySettings> (config, "Key", s => KeySettings.Defaults = s);
        BindSection<TraceSettings> (config, "Trace", s => TraceSettings.Defaults = s);

        // ThemeScope POCOs
        BindSection<ButtonSettings> (config, "Button", s => ButtonSettings.Defaults = s);
        BindSection<CheckBoxSettings> (config, "CheckBox", s => CheckBoxSettings.Defaults = s);
        BindSection<CharMapSettings> (config, "CharMap", s => CharMapSettings.Defaults = s);
        BindSection<DialogSettings> (config, "Dialog", s => DialogSettings.Defaults = s);
        BindSection<FrameViewSettings> (config, "FrameView", s => FrameViewSettings.Defaults = s);
        BindSection<HexViewSettings> (config, "HexView", s => HexViewSettings.Defaults = s);
        BindSection<LinearRangeSettings> (config, "LinearRange", s => LinearRangeSettings.Defaults = s);
        BindSection<MenuBarSettings> (config, "MenuBar", s => MenuBarSettings.Defaults = s);
        BindSection<MenuSettings> (config, "Menu", s => MenuSettings.Defaults = s);
        BindSection<MessageBoxSettings> (config, "MessageBox", s => MessageBoxSettings.Defaults = s);
        BindSection<NerdFontsSettings> (config, "NerdFonts", s => NerdFontsSettings.Defaults = s);
        BindSection<PopoverMenuSettings> (config, "PopoverMenu", s => PopoverMenuSettings.Defaults = s);
        BindSection<SelectorBaseSettings> (config, "SelectorBase", s => SelectorBaseSettings.Defaults = s);
        BindSection<StatusBarSettings> (config, "StatusBar", s => StatusBarSettings.Defaults = s);
        BindSection<TextFieldSettings> (config, "TextField", s => TextFieldSettings.Defaults = s);
        BindSection<TextViewSettings> (config, "TextView", s => TextViewSettings.Defaults = s);
        BindSection<WindowSettings> (config, "Window", s => WindowSettings.Defaults = s);
        BindSection<GlyphSettings> (config, "Glyphs", s => GlyphSettings.Defaults = s);
    }

    private static void BindSection<T> (IConfiguration config, string sectionName, Action<T> apply) where T : new ()
    {
        T settings = new ();
        config.GetSection (sectionName).Bind (settings);
        apply (settings);
    }
}

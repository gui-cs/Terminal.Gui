using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Builds and manages a Terminal.Gui MEC-based configuration, loading from all standard sources
///     in the correct precedence order.
/// </summary>
/// <remarks>
///     <para>
///         This is the MEC-based replacement for the legacy <see cref="ConfigurationManager"/>.
///         It provides the same multi-source precedence (library defaults → app defaults → user files
///         → environment variables → runtime config) using standard Microsoft.Extensions.Configuration.
///     </para>
///     <para><b>App Developer Usage:</b></para>
///     <code>
///     // Define your app settings POCO:
///     public class MyAppSettings
///     {
///         public string Title { get; set; } = "My App";
///         public bool DarkMode { get; set; }
///         public static MyAppSettings Defaults { get; set; } = new ();
///     }
///
///     // In your app startup:
///     var builder = new TuiConfigurationBuilder ("MyApp");
///     builder.BindAppSettings&lt;MyAppSettings&gt; ("MyApp", s =&gt; MyAppSettings.Defaults = s);
///     builder.ApplyToStaticFacades ();
///
///     // Access settings:
///     string title = MyAppSettings.Defaults.Title;
///     </code>
///     <para>
///         To add custom configuration sources, use the MEC extension methods directly:
///     </para>
///     <code>
///     IConfigurationBuilder configBuilder = new ConfigurationBuilder ()
///         .AddTuiLibraryDefaults ()
///         .AddTuiUserFiles ("MyApp")
///         .AddJsonFile ("custom-settings.json", optional: true);
///     IConfiguration config = configBuilder.Build ();
///     </code>
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
    ///     Gets the MEC-backed theme manager instance for this builder.
    /// </summary>
    public IThemeManager ThemeManager => _themeManager ??= new MecThemeManager (this);
    private IThemeManager? _themeManager;

    /// <summary>
    ///     Gets the MEC-backed scheme manager instance for this builder.
    /// </summary>
    public ISchemeManager SchemeManager => _schemeManager ??= new MecSchemeManager ();
    private ISchemeManager? _schemeManager;

    /// <summary>
    ///     Applies the current configuration to all static settings facades.
    ///     Call this after building or rebuilding to push MEC values to the static <c>Defaults</c> properties.
    /// </summary>
    public void ApplyToStaticFacades ()
    {
        IConfiguration config = Configuration;

        // SettingsScope POCOs
        BindSection<ThemeSettings> (config, "Theme", s => ThemeSettings.Defaults = s);
        BindSection<ApplicationSettings> (config, "Application", s => ApplicationSettings.Defaults = s);
        BindSection<DriverSettings> (config, "Driver", s => DriverSettings.Defaults = s);
        BindSection<FileDialogSettings> (config, "FileDialog", s => FileDialogSettings.Defaults = s);
        BindSection<FileDialogStyleSettings> (config, "FileDialogStyle", s => FileDialogStyleSettings.Defaults = s);
        BindSection<KeySettings> (config, "Key", s => KeySettings.Defaults = s);
        BindSection<TraceSettings> (config, "Trace", s => TraceSettings.Defaults = s);

        // ThemeScope POCOs: two-pass overlay (root section + Themes:<active>:<section>) writes Current.
        // TODO(A2): when ThemeSettings converts to record + Current, this becomes an immutable snapshot.
        string activeTheme = ThemeSettings.Defaults.Theme;
        BindThemeScope<ButtonSettings> (config, "Button", activeTheme, s => ButtonSettings.Current = s);
        BindThemeScope<CheckBoxSettings> (config, "CheckBox", activeTheme, s => CheckBoxSettings.Current = s);
        BindThemeScope<CharMapSettings> (config, "CharMap", activeTheme, s => CharMapSettings.Current = s);
        BindThemeScope<DialogSettings> (config, "Dialog", activeTheme, s => DialogSettings.Current = s);
        BindThemeScope<FrameViewSettings> (config, "FrameView", activeTheme, s => FrameViewSettings.Current = s);
        BindThemeScope<HexViewSettings> (config, "HexView", activeTheme, s => HexViewSettings.Current = s);
        BindThemeScope<LinearRangeSettings> (config, "LinearRange", activeTheme, s => LinearRangeSettings.Current = s);
        BindThemeScope<MenuBarSettings> (config, "MenuBar", activeTheme, s => MenuBarSettings.Current = s);
        BindThemeScope<MenuSettings> (config, "Menu", activeTheme, s => MenuSettings.Current = s);
        BindThemeScope<MessageBoxSettings> (config, "MessageBox", activeTheme, s => MessageBoxSettings.Current = s);
        BindThemeScope<NerdFontsSettings> (config, "NerdFonts", activeTheme, s => NerdFontsSettings.Current = s);
        BindThemeScope<PopoverMenuSettings> (config, "PopoverMenu", activeTheme, s => PopoverMenuSettings.Current = s);
        BindThemeScope<SelectorBaseSettings> (config, "SelectorBase", activeTheme, s => SelectorBaseSettings.Current = s);
        BindThemeScope<StatusBarSettings> (config, "StatusBar", activeTheme, s => StatusBarSettings.Current = s);
        BindThemeScope<TextFieldSettings> (config, "TextField", activeTheme, s => TextFieldSettings.Current = s);
        BindThemeScope<TextViewSettings> (config, "TextView", activeTheme, s => TextViewSettings.Current = s);
        BindThemeScope<WindowSettings> (config, "Window", activeTheme, s => WindowSettings.Current = s);
        BindThemeScope<GlyphSettings> (config, "Glyphs", activeTheme, s => GlyphSettings.Current = s);
    }

    /// <summary>
    ///     Binds a custom application settings section from the configuration to a POCO instance.
    ///     This is the MEC replacement for the legacy <see cref="AppSettingsScope"/>.
    /// </summary>
    /// <typeparam name="T">The settings POCO type.</typeparam>
    /// <param name="sectionName">The JSON section name to bind from.</param>
    /// <param name="apply">Action to apply the bound settings (typically update a static Defaults property).</param>
    /// <returns>This builder for chaining.</returns>
    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "Settings POCOs are simple types preserved by DynamicDependency in ConfigPropertyHostTypes.")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "Settings POCOs are simple types; no generic instantiation needed at runtime.")]
    public TuiConfigurationBuilder BindAppSettings<T> (string sectionName, Action<T> apply) where T : new ()
    {
        T settings = new ();
        Configuration.GetSection (sectionName).Bind (settings);
        apply (settings);

        return this;
    }

    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "Settings POCOs are simple types preserved by DynamicDependency in ConfigPropertyHostTypes.")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "Settings POCOs are simple types; no generic instantiation needed at runtime.")]
    private static void BindSection<T> (IConfiguration config, string sectionName, Action<T> apply) where T : new ()
    {
        T settings = new ();
        config.GetSection (sectionName).Bind (settings);
        apply (settings);
    }

    /// <summary>
    ///     Two-pass overlay bind for ThemeScope POCOs. Binds the root section, then overlays
    ///     <c>Themes:<paramref name="activeTheme"/>:<paramref name="sectionName"/></c>. Properties not present in the
    ///     overlay JSON retain the root value (property-level merge — matches legacy CM <c>Scope.Apply</c> semantics).
    /// </summary>
    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "Settings POCOs are simple types preserved by DynamicDependency in ConfigPropertyHostTypes.")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "Settings POCOs are simple types; no generic instantiation needed at runtime.")]
    private static void BindThemeScope<T> (IConfiguration config, string sectionName, string activeTheme, Action<T> apply) where T : new ()
    {
        T settings = new ();
        config.GetSection (sectionName).Bind (settings);
        config.GetSection ($"Themes:{activeTheme}:{sectionName}").Bind (settings);
        apply (settings);
    }
}

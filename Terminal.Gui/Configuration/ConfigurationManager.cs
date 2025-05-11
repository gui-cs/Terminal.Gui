#nullable enable

global using static Terminal.Gui.ConfigurationManager;
global using CM = Terminal.Gui.ConfigurationManager;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.Configuration;

namespace Terminal.Gui;

/// <summary>
///     Provides settings and configuration management for Terminal.Gui applications.
///     <para>
///         Users can set Terminal.Gui settings on a global or per-application basis by providing JSON formatted
///         configuration files. The configuration files can be placed in at <c>.tui</c> folder in the user's home
///         directory (e.g. <c>C:/Users/username/.tui</c>, or <c>/usr/username/.tui</c>), the folder where the Terminal.Gui
///         application was launched from (e.g. <c>./.tui</c> ), or as a resource within the Terminal.Gui application's
///         main assembly.
///     </para>
///     <para>
///         Settings are defined in JSON format, according to this schema:
///         https://gui-cs.github.io/Terminal.GuiV2Docs/schemas/tui-config-schema.json
///     </para>
///     <para>
///         Settings that will apply to all applications (global settings) reside in files named <c>config.json</c>.
///         Settings that will apply to a specific Terminal.Gui application reside in files named
///         <c>appname.config.json</c>, where <c>appname</c> is the assembly name of the application (e.g.
///         <c>UICatalog.config.json</c>).
///     </para>
///     <para>
///     Settings are applied using the precedence defined in <see cref="ConfigLocations"/>.
///     </para>
///     <para>
///     Configuration Management is based on static properties decorated with the <see cref="ConfigurationPropertyAttribute"/>. Since these properties are static, changes to
///     configuration settings are applied process-wide.
///     </para>
///     <para>
///     Configuration Management is disabled by default and can be enabled by setting calling <see cref="ConfigurationManager.Enable"/>.
///     </para>
///     <para>
///     See the UICatalog example for a complete example of how to use ConfigurationManager.
///     See the Configuration Deep Dive for more information: <see href="https://gui-cs.github.io/Terminal.GuiV2Docs/docs/config.html"/>.
///     </para>
/// </summary>
public static class ConfigurationManager
{
    /// <summary>The backing property for <see cref="Settings"/> (config settings of <see cref="SettingsScope"/>).</summary>
    /// <remarks>
    ///     Is <see langword="null"/> until <see cref="Reset"/> is called. Gets set to a new instance by
    ///     deserialization
    ///     (see <see cref="Load"/>).
    /// </remarks>
    internal static SettingsScope? _settings;

    private static readonly ReaderWriterLockSlim _settingsLockSlim = new ();

    /// <summary>
    ///     The root object of Terminal.Gui configuration settings / JSON schema.
    /// </summary>
    public static SettingsScope? Settings
    {
        get
        {
            _settingsLockSlim.EnterReadLock ();

            try
            {
                return _settings;
            }
            finally
            {
                _settingsLockSlim.ExitReadLock ();
            }
        }
        set
        {
            _settingsLockSlim.EnterWriteLock ();

            try
            {
                _settings = value;
            }
            finally
            {
                _settingsLockSlim.ExitWriteLock ();
            }
        }
    }

    #region Initialization

    // ConfigurationManager is initialized when the module is loaded, via ModuleInitializers.InitializeConfigurationManager
    // Once initialized, the ConfigurationManager is never un-initialized.
    // The _initialized field is set to true when the module is loaded and the ConfigurationManager is initialized.
    private static bool _initialized;
    private static readonly object _initializedLock = new ();

    /// <summary>
    ///     INTERNAL: For Testing - Indicates whether the <see cref="ConfigurationManager"/> has been initialized.
    /// </summary>
    internal static bool IsInitialized ()
    {
        lock (_initializedLock)
        {
            return _initialized;
        }
    }

    /// <summary>
    ///     A cache of all<see cref="ConfigurationPropertyAttribute"/> properties and their hard coded values.
    /// </summary>
    /// <remarks>Is <see langword="null"/> until <see cref="Initialize"/> is called.</remarks>
    internal static FrozenDictionary<string, ConfigProperty>? _hardCodedConfigPropertyCache;

    internal static FrozenDictionary<string, ConfigProperty>? GetHardCodedConfigPropertyCache ()
    {
        if (_hardCodedConfigPropertyCache is null)
        {
            throw new InvalidOperationException ("_hardCodedConfigPropertyCache has not been set.");
        }

        return _hardCodedConfigPropertyCache;
    }

    /// <summary>
    ///     An immutable cache of all <see cref="ConfigProperty"/>s in module decorated with the
    ///     <see cref="ConfigurationPropertyAttribute"/> attribute. Bott the dictionary and the contained <see cref="ConfigProperty"/>s
    ///     are immutable.
    /// </summary>
    /// <remarks>Is <see langword="null"/> until <see cref="Initialize"/> is called.</remarks>
    internal static ImmutableSortedDictionary<string, ConfigProperty>? _allConfigPropertiesCache;

    private static readonly object _allConfigPropertiesCacheLock = new ();

    /// <summary>
    ///     INTERNAL: Initializes the <see cref="ConfigurationManager"/>.
    ///     This method is called when the module is loaded,
    ///     via <see cref="ModuleInitializers.InitializeConfigurationManager"/>.
    ///     For ConfigurationManager to access config resources, <see cref="IsEnabled"/> needs to be
    ///     set to <see langword="true"/> after this method has been called.
    /// </summary>
    [RequiresDynamicCode ("Uses reflection to scan assemblies for configuration properties. " +
                          "Only called during initialization and not needed during normal operation. " +
                          "In AOT environments, ensure all types with ConfigurationPropertyAttribute are preserved.")]
    [RequiresUnreferencedCode ("Reflection requires all types with ConfigurationPropertyAttribute to be preserved in AOT. " +
                               "Use the SourceGenerationContext to register all configuration property types.")]
    internal static void Initialize ()
    {
        lock (_initializedLock)
        {
            if (_initialized)
            {
                throw new InvalidOperationException ("ConfigurationManager is already initialized.");
            }
        }

        // Ensure ConfigProperty has cached the list of all the classes with config properties.
        ConfigProperty.Initialize ();

        // Cache all configuration properties
        lock (_allConfigPropertiesCacheLock)
        {
            // _allConfigProperties: for ordered, iterable access (LINQ-friendly)
            // _frozenConfigPropertyCache: for high-speed key lookup (frozen)

            // Note GetAllConfigProperties returns a new instance and all the properties !HasValue and Immutable.
            _allConfigPropertiesCache = ConfigProperty.GetAllConfigProperties ();
        }

        _hardCodedConfigPropertyCache = _allConfigPropertiesCache.ToFrozenDictionary ();

        foreach (KeyValuePair<string, ConfigProperty> hardCodedProperty in _hardCodedConfigPropertyCache)
        {
            // Set the PropertyValue to the hard coded value
            hardCodedProperty.Value.Immutable = false;
            hardCodedProperty.Value.UpdateToCurrentValue ();
            hardCodedProperty.Value.Immutable = true;
        }

        lock (_initializedLock)
        {
            _initialized = true;
        }

        ResetToHardCodedDefaults();
    }

    #endregion Initialization

    #region Enable/Disable

    private static bool _enabled = false;
    private static readonly object _enabledLock = new ();
    /// <summary>
    ///     Gets whether <see cref="ConfigurationManager"/> is enabled or not.
    ///     If <see langword="true"/>, the <see cref="ConfigurationManager"/> is enabled <see cref="Load"/> will load configurations from <see cref="Locations"/>.
    ///     If <see langword="false"/>, only the hard coded defaults will be loaded. See <see cref="Enable"/> and
    ///     <see cref="Disable"/>
    /// </summary>
    public static bool IsEnabled
    {
        get
        {
            lock (_enabledLock)
            {
                return _enabled;
            }
        }
    }

    /// <summary>
    ///     Enables <see cref="ConfigurationManager"/>. The first time Enable is called, <see cref="Settings"/> will be reset to
    ///     with the current values of the static <see cref="ConfigurationPropertyAttribute"/> properties.
    /// </summary>
    public static void Enable ()
    {
        if (IsEnabled)
        {
            return;
        }

        lock (_enabledLock)
        {
            _enabled = true;
            if (_settings is null)
            {
                // If _settings is null, we need to load the settings from the current values of the static properties
                ResetToCurrentValues ();
            }
        }

    }

    /// <summary>
    ///     Disables <see cref="ConfigurationManager"/>.
    /// </summary>
    public static void Disable ()
    {
        if (!IsEnabled)
        {
            return;
        }

        lock (_enabledLock)
        {
            _enabled = false;
        }
    }

    #endregion Enable/Disable

    #region Reset
    // `Reset` - Reset the configuration to either the current values or the hard-coded defaults.
    // Resetting does not load the configuration; it only resets the configuration to the default values.

    /// <summary>
    ///     INTERNAL: Resets <see cref="ConfigurationManager"/>. Clears JsonErrors and loads all settings from the current values of the static <see cref="ConfigurationPropertyAttribute"/> properties.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal static void Reset ()
    {
        if (!IsInitialized ())
        {
            throw new InvalidOperationException ("Initialize must be called first.");
        }

        ClearJsonErrors ();

        _settingsLockSlim.EnterWriteLock ();

        try
        {
            _settings = new ();
        }
        finally
        {
            _settingsLockSlim.ExitWriteLock ();
        }

        _cachedAppSettingsLock.EnterWriteLock ();

        try
        {
            _cachedAppSettings = new ();
        }
        finally
        {
            _cachedAppSettingsLock.ExitWriteLock ();
        }

        ResetToCurrentValues ();
    }

    /// <summary>
    ///     INTERNAL: Resets all settings to the current values of the static <see cref="ConfigurationPropertyAttribute"/> properties.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal static void ResetToCurrentValues ()
    {
        if (!IsInitialized ())
        {
            throw new InvalidOperationException ("Initialize must be called first.");
        }

        Settings = new ();
        Settings.UpdateToCurrentValues ();
        ThemeManager.UpdateToCurrentValues ();
        AppSettings?.UpdateToCurrentValues ();

        //foreach (KeyValuePair<string, ConfigProperty> p in Settings!.Where (cp => cp.Value.PropertyInfo is { }))
        //{
        //    Settings! [p.Key].PropertyValue = p.Value.PropertyInfo?.GetValue (null);
        //}
    }

    /// <summary>
    ///     INTERNAL: Resets settings to the values the <see cref="ConfigurationPropertyAttribute"/> properties contained
    ///     when the module was initialized.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal static void ResetToHardCodedDefaults ()
    {
        if (!IsInitialized ())
        {
            throw new InvalidOperationException ("Initialize must be called first.");
        }

        SourcesManager!.Sources.Clear();
        SourcesManager.AddSource (ConfigLocations.HardCoded, "HardCoded");

        Settings = new ();
        Settings!.UpdateToHardCodedDefaults ();
        ThemeManager.UpdateToHardCodedDefaults ();
        AppSettings!.UpdateToHardCodedDefaults ();
    }

    #endregion Reset


    #region Load
    // `Load` - Load configuration from the given location(s), updating the configuration with any new values.
    // Loading does not apply the settings to the application; that happens when the `Apply` method is called.

    /// <summary>
    ///     Loads all settings found in <paramref name="locations"/>. Use <see cref="Apply"/> to cause the loaded settings to be applied to the running application.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static void Load (ConfigLocations locations)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException ("ConfigurationManager is not enabled.");
        }

        // TODO: We ignore HardCodedDefaults. Use ResetToHardCodedDefaults to reset to hard coded defaults? Or 
        // TODO: Add an if (locations == ConfigLocations.HardCoded) to the switch statement?

        if (locations.HasFlag (ConfigLocations.LibraryResources))
        {
            SourcesManager?.Load (
                                                Settings,
                                                typeof (ConfigurationManager).Assembly,
                                                $"Terminal.Gui.Resources.{_configFilename}",
                                                ConfigLocations.LibraryResources
                                               );
        }

        if (locations.HasFlag (ConfigLocations.AppResources))
        {
            string? embeddedStylesResourceName = Assembly.GetEntryAssembly ()
                                                         ?
                                                         .GetManifestResourceNames ()
                                                         .FirstOrDefault (x => x.EndsWith (_configFilename));

            if (string.IsNullOrEmpty (embeddedStylesResourceName))
            {
                embeddedStylesResourceName = _configFilename;
            }

            SourcesManager?.Load (Settings, Assembly.GetEntryAssembly ()!, embeddedStylesResourceName!, ConfigLocations.AppResources);
        }

        // TODO: Determine if Runtime should be applied last.
        if (locations.HasFlag (ConfigLocations.Runtime) && !string.IsNullOrEmpty (RuntimeConfig))
        {
            SourcesManager?.Load (Settings, RuntimeConfig, "ConfigurationManager.RuntimeConfig", ConfigLocations.Runtime);
        }

        if (locations.HasFlag (ConfigLocations.GlobalCurrent))
        {
            SourcesManager?.Load (Settings, $"./.tui/{_configFilename}", ConfigLocations.GlobalCurrent);
        }

        if (locations.HasFlag (ConfigLocations.GlobalHome))
        {
            SourcesManager?.Load (Settings, $"~/.tui/{_configFilename}", ConfigLocations.GlobalHome);
        }

        if (locations.HasFlag (ConfigLocations.AppCurrent))
        {
            SourcesManager?.Load (Settings, $"./.tui/{AppName}.{_configFilename}", ConfigLocations.AppCurrent);
        }

        if (locations.HasFlag (ConfigLocations.AppHome))
        {
            SourcesManager?.Load (Settings, $"~/.tui/{AppName}.{_configFilename}", ConfigLocations.AppHome);
        }

        ThemeManager.Theme = Settings! ["Theme"].PropertyValue as string ?? throw new InvalidOperationException ();
    }

    // TODO: Rename to Loaded?
    /// <summary>
    ///     Called when the configuration has been updated from a configuration file or reset. Invokes the
    ///     <see cref="Updated"/>
    ///     event.
    /// </summary>
    public static void OnUpdated ()
    {
        //Logging.Trace (@"");
        // Use a local copy of the event delegate when invoking it to avoid race conditions.
        EventHandler<ConfigurationManagerEventArgs>? handler = Updated;
        handler?.Invoke (null, new ());
    }

    /// <summary>Event fired when the configuration has been updated from a configuration source or reset.</summary>
    public static event EventHandler<ConfigurationManagerEventArgs>? Updated;

    #endregion Load



    #region Apply
    // `Apply` - Apply the configuration to the application; this means the settings are copied from the
    // configuration properties to the corresponding `static` `[ConfigurationProperty]` properties.

    /// <summary>
    ///     Applies the configuration settings to static <see cref="ConfigurationPropertyAttribute"/> properties.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static void Apply ()
    {
        if (!IsEnabled)
        {
            Logging.Trace ("ConfigurationManager is disabled.");
            return;
        }

        var settings = false;
        var themes = false;
        var appSettings = false;

        try
        {
            if (string.IsNullOrEmpty (ThemeManager.Theme))
            {
                // First start. Apply settings first.
                settings = Settings?.Apply () ?? false;
                themes = !string.IsNullOrEmpty (ThemeManager.Theme)
                                             && (ThemeManager.Themes? [ThemeManager.Theme]?.Apply () ?? false);
            }
            else
            {
                // Subsequently. Apply Themes first.
                themes = ThemeManager.Themes? [ThemeManager.Theme]?.Apply () ?? false;
                settings = Settings?.Apply () ?? false;
            }

            appSettings = AppSettings?.Apply () ?? false;
        }
        catch (JsonException e)
        {
            if (ThrowOnJsonErrors ?? false)
            {
                throw;
            }
            else
            {
                AddJsonError ($"Error applying Configuration Change: {e.Message}");
            }
        }
        finally
        {
            if (settings || themes || appSettings)
            {
                OnApplied ();
            }
        }
    }

    /// <summary>
    ///     Called when an updated configuration has been applied to the application. Fires the <see cref="Applied"/>
    ///     event.
    /// </summary>
    public static void OnApplied ()
    {
        //Logging.Trace ("");

        // Use a local copy of the event delegate when invoking it to avoid race conditions.
        EventHandler<ConfigurationManagerEventArgs>? handler = Applied;
        handler?.Invoke (null, new ());

        // TODO: Refactor ConfigurationManager to not use an event handler for this.
        // Instead, have it call a method on any class appropriately attributed
        // to update the cached values. See Issue #2871
    }

    /// <summary>Event fired when an updated configuration has been applied to the application.</summary>
    public static event EventHandler<ConfigurationManagerEventArgs>? Applied;

    #endregion Apply

    #region Sources
    // `Sources` - A source is a location where a configuration can be stored. Sources are defined in the `ConfigLocations` enum.

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static readonly SourceGenerationContext SerializerContext = new (
                                                                              new JsonSerializerOptions
                                                                              {
                                                                                  ReadCommentHandling = JsonCommentHandling.Skip,
                                                                                  PropertyNameCaseInsensitive = true,
                                                                                  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                                                                                  WriteIndented = true,
                                                                                  Converters =
                                                                                  {
                                                                                      // We override the standard Rune converter to support specifying Glyphs in
                                                                                      // a flexible way
                                                                                      new RuneJsonConverter (),

                                                                                      // Override Key to support "Ctrl+Q" format.
                                                                                      new KeyJsonConverter ()
                                                                                  },

                                                                                  // Enables Key to be "Ctrl+Q" vs "Ctrl\u002BQ"
                                                                                  Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                                                                  TypeInfoResolver = SourceGenerationContext.Default
                                                                              });


    /// <summary>
    ///     Gets the Sources Manager - manages the loading of configuration sources from files and resources.
    /// </summary>
    public static SourcesManager? SourcesManager { get; internal set; } = new ();

    /// <summary>
    ///     Gets or sets the in-memory config.json. See <see cref="ConfigLocations.Runtime"/>.
    /// </summary>
    public static string? RuntimeConfig { get; set; } = """{  }""";

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    private static readonly string _configFilename = "config.json";

    #endregion Sources

    #region AppSettings
    // TODO: Encapsulate in AppSettingsManager like ThemeManager

    /// <summary>
    ///     Since AppSettings is a dynamic property, we need to cache the value of the current appsettings for when CM is not enabled.
    /// </summary>
    private static AppSettingsScope? _cachedAppSettings;

    private static readonly ReaderWriterLockSlim _cachedAppSettingsLock = new ();

    /// <summary>Application-specific configuration settings (config properties with the <see cref="AppSettingsScope"/> scope.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    [JsonPropertyName ("AppSettings")]
    public static AppSettingsScope? AppSettings
    {
        get
        {
            if (!IsInitialized ())
            {
                // We're being called from the module initializer.
                // Hard coded default value is an empty AppSettingsScope
                return _cachedAppSettings = new AppSettingsScope ();
            }

            if (!IsEnabled)
            {
                // If CM is not enabled, return current value
                return _cachedAppSettings!;
            }

            if (Settings is { } && Settings.TryGetValue ("AppSettings", out ConfigProperty? appsettingsConfigProperty))
            {
                return (appsettingsConfigProperty.PropertyValue as AppSettingsScope)!;
            }

            throw new InvalidOperationException ("Settings is null.");
        }
        set
        {
            if (!IsInitialized ())
            {
                throw new InvalidOperationException ("AppSettings cannot be set before ConfigurationManager is initialized.");
            }

            if (!IsEnabled)
            {
                _cachedAppSettingsLock.EnterWriteLock ();

                try
                {
                    _cachedAppSettings = value;
                }
                finally
                {
                    _cachedAppSettingsLock.ExitWriteLock ();
                }

                return;
            }

            // Check if the AppSettings is the same as the previous one
            if (value != _cachedAppSettings)
            {
                // Update the backing store
                Settings! ["AppSettings"].PropertyValue = value;

                _cachedAppSettingsLock.EnterWriteLock ();

                try
                {
                    _cachedAppSettings = value;
                }
                finally
                {
                    _cachedAppSettingsLock.ExitWriteLock ();
                }
                //Instance.OnThemeChanged (previousThemeValue);
            }
        }
    }

    #endregion AppSettings

    #region Error Logging

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static StringBuilder _jsonErrors = new ();

    /// <summary>
    ///     Gets or sets whether the <see cref="ConfigurationManager"/> should throw an exception if it encounters an
    ///     error on deserialization. If <see langword="false"/> (the default), the error is logged and printed to the console
    ///     when <see cref="Application.Shutdown"/> is called.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool? ThrowOnJsonErrors { get; set; } = false;

    private static readonly object _jsonErrorsLock = new ();

    internal static void AddJsonError (string error)
    {
        Logging.Error ($"{error}");

        lock (_jsonErrorsLock)
        {
            _jsonErrors.AppendLine (error);
        }
    }

    private static void ClearJsonErrors ()
    {
        lock (_jsonErrorsLock)
        {
            _jsonErrors.Clear ();
        }
    }

    /// <summary>Prints any Json deserialization errors that occurred during deserialization to the console.</summary>
    public static void PrintJsonErrors ()
    {
        lock (_jsonErrorsLock)
        {
            if (_jsonErrors.Length > 0)
            {
                Console.WriteLine (@"Terminal.Gui ConfigurationManager encountered the following errors while deserializing configuration files:");
                Console.WriteLine (_jsonErrors.ToString ());
            }
        }
    }

    #endregion Error Logging

    /// <summary>Returns an empty Json document with just the $schema tag.</summary>
    /// <returns></returns>
    public static string GetEmptyConfig ()
    {
        var emptyScope = new SettingsScope ();
        emptyScope.Clear ();

        return JsonSerializer.Serialize (emptyScope, typeof (SettingsScope), SerializerContext!);
    }

    /// <summary>Returns a Json document containing the hard-coded config.</summary>
    /// <returns></returns>
    public static string GetHardCodedConfig ()
    {
        var emptyScope = new SettingsScope ();
        IEnumerable<KeyValuePair<string, ConfigProperty>>? settings = GetHardCodedConfigPropertiesByScope ("SettingsScope");

        if (settings is null)
        {
            throw new InvalidOperationException ("GetHardCodedConfigPropertiesByScope returned null.");
        }
        Dictionary<string, ConfigProperty> settingsDict = settings.ToDictionary ();

        foreach (KeyValuePair<string, ConfigProperty> p in Settings!.Where (cp => cp.Value.PropertyInfo is { }))
        {
            emptyScope [p.Key].PropertyValue = settingsDict [p.Key].PropertyValue;
        }

        return JsonSerializer.Serialize (emptyScope, typeof (SettingsScope), SerializerContext!);
    }

    /// <summary>Name of the running application. By default, this property is set to the application's assembly name.</summary>
    public static string AppName { get; set; } = Assembly.GetEntryAssembly ()?.FullName?.Split (',') [0]?.Trim ()!;

    /// <summary>
    ///     INTERNAL: Retrieves all uninitialized configuration properties that belong to a specific scope from the cache.
    ///     The items in the collection are references to the original <see cref="ConfigProperty"/> objects in the
    ///     cache. They do not have values and have <see cref="ConfigProperty.Immutable"/> set.
    /// </summary>
    internal static IEnumerable<KeyValuePair<string, ConfigProperty>>? GetConfigPropertiesByScope (string scopeType)
    {
        // AOT Note: This method does NOT need the RequiresUnreferencedCode attribute as it is not using reflection
        // and is not using any dynamic code. _allConfigProperties is a static property that is set in the module initializer
        // and is not using any dynamic code. In addition, ScopeType are registered in SourceGenerationContext.

        if (_allConfigPropertiesCache is null)
        {
            throw new InvalidOperationException ("_allConfigPropertiesCache has not been set.");
        }

        if (string.IsNullOrEmpty (scopeType))
        {
            return _allConfigPropertiesCache;
        }

        // Filter properties by scope using the cached ScopeType property instead of reflection
        IEnumerable<KeyValuePair<string, ConfigProperty>>? filtered = _allConfigPropertiesCache?.Where (cp => cp.Value.ScopeType == scopeType);

        Debug.Assert (filtered is { });
        return filtered;
    }

    /// <summary>
    ///     INTERNAL: Retrieves all configuration properties that belong to a specific scope from the hard coded value cache.
    ///     The items in the collection are references to the original <see cref="ConfigProperty"/> objects in the
    ///     cache. They contain the hard coded values and have <see cref="ConfigProperty.Immutable"/> set.
    /// </summary>
    internal static IEnumerable<KeyValuePair<string, ConfigProperty>>? GetHardCodedConfigPropertiesByScope (string scopeType)
    {
        // AOT Note: This method does NOT need the RequiresUnreferencedCode attribute as it is not using reflection
        // and is not using any dynamic code. _allConfigProperties is a static property that is set in the module initializer
        // and is not using any dynamic code. In addition, ScopeType are registered in SourceGenerationContext.

        // Filter properties by scope
        IEnumerable<KeyValuePair<string, ConfigProperty>>? cache = GetHardCodedConfigPropertyCache ();

        if (cache is null)
        {
            throw new InvalidOperationException ("GetHardCodedConfigPropertyCache returned null");
        }

        if (string.IsNullOrEmpty (scopeType))
        {
            return cache;
        }

        // Use the cached ScopeType property instead of reflection
        IEnumerable<KeyValuePair<string, ConfigProperty>>? scopedCache = cache?.Where (cp => cp.Value.ScopeType == scopeType);

        return scopedCache;
    }
}

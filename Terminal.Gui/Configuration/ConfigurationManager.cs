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
///     Configuration Management can be disabled by setting <see cref="ConfigurationManager.Locations"/>
///     to <see cref="ConfigLocations.HardCoded"/>.
///     </para>
/// </summary>
[ComponentGuarantees (ComponentGuaranteesOptions.None)]
public static class ConfigurationManager
{
    private static bool _initialized;

    private static readonly object _initializedLock = new ();

    /// <summary>
    ///     Indicates whether the <see cref="ConfigurationManager"/> has been initialized.
    /// </summary>
    /// <returns></returns>
    public static bool IsInitialized ()
    {
        lock (_initializedLock)
        {
            return _initialized;
        }
    }

    /// <summary>
    ///     A cache of all<see cref="SerializableConfigurationProperty"/> properties and their hard coded values.
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
    ///     <see cref="SerializableConfigurationProperty"/> attribute. Bott the dictionary and the contained <see cref="ConfigProperty"/>s
    ///     are immutable.
    /// </summary>
    /// <remarks>Is <see langword="null"/> until <see cref="Initialize"/> is called.</remarks>
    internal static ImmutableSortedDictionary<string, ConfigProperty>? _allConfigPropertiesCache;

    private static readonly object _allConfigPropertiesCacheLock = new ();


    /// <summary>
    ///     Initializes the <see cref="ConfigurationManager"/>.
    ///     This method is called when the module is loaded,
    ///     via <see cref="ModuleInitializers.InitializeConfigurationManager"/>.
    ///     For ConfigurationManager to access config resources, <see cref="IsEnabled"/> needs to be
    ///     set to <see langword="true"/> after this method has been called.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
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
            hardCodedProperty.Value.RetrieveValue ();
            hardCodedProperty.Value.Immutable = true;
        }

        lock (_initializedLock)
        {
            _initialized = true;
        }

        SourcesManager?.AddSource (ConfigLocations.HardCoded, "HardCoded");

        // Note, we do not set _settings, _appSettings, or _themeManager here. They get set in Load

    }

    private static bool _enabled = false;

    /// <summary>
    ///     Gets whehter the <see cref="ConfigurationManager"/> is enabled.
    ///     If <see langword="true"/>, the <see cref="ConfigurationManager"/> is enabled and will load and apply
    ///     settings from the configuration. If <see langword="false"/>, only the hard coded defaults will be loaded. See <see cref="Enable"/> and
    ///     <see cref="Disable"/>
    /// </summary>
    public static bool IsEnabled => _enabled;

    /// <summary>
    ///     If <see langword="true"/>, the <see cref="ConfigurationManager"/> is enabled and will load and apply
    ///     settings from the configuration. If <see langword="false"/>, only the hard coded defaults will be used.
    /// </summary>
    public static void Disable ()
    {
        _enabled = false;
        Reset ();
    }

    /// <summary>
    /// </summary>
    public static void Enable ()
    {
        if (!_enabled)
        {
            ResetToCurrentValues ();
        }

        _enabled = true;
    }

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

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static StringBuilder _jsonErrors = new ();

    // TODO: If Locations gets set to HardCoded, should we disable CM?
    /// <summary>
    ///     Gets and sets the locations where <see cref="ConfigurationManager"/> will look for config files. The default value
    ///     is
    ///     <see cref="ConfigLocations.All"/>.
    /// </summary>
    public static ConfigLocations Locations { get; set; } = ConfigLocations.All;

    /// <summary>
    ///     Gets the Sources Manager - manages the loading of configuration sources from files and resources.
    /// </summary>
    public static SourcesManager? SourcesManager { get; internal set; } = new ();

    /// <summary>Name of the running application. By default, this property is set to the application's assembly name.</summary>
    public static string AppName { get; set; } = Assembly.GetEntryAssembly ()?.FullName?.Split (',') [0]?.Trim ()!;

    /// <summary>
    ///     Since AppSettings is a dynamic property, we need to cache the value of the current appsettings for when CM is not enabled.
    /// </summary>
    private static AppScope? _cachedAppSettings;

    private static readonly ReaderWriterLockSlim _cachedAppSettingsLock = new ();

    /// <summary>Application-specific configuration settings (config properties with the <see cref="AppScope"/> scope.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    [JsonPropertyName ("AppSettings")]
    public static AppScope? AppSettings
    {
        get
        {
            if (!IsInitialized ())
            {
                // We're being called from the module initializer.
                // Hard coded default value is an empty AppScope
                return _cachedAppSettings = new AppScope ();
            }

            if (!IsEnabled)
            {
                // If CM is not enabled, return current value
                return _cachedAppSettings!;
            }

            if (Settings is { } && Settings.TryGetValue ("AppSettings", out ConfigProperty? appsettingsConfigProperty))
            {
                return (appsettingsConfigProperty.PropertyValue as AppScope)!;
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
                //Instance.OnThemeChanged (prevousThemeValue);
            }
        }
    }

    /// <summary>The backing property for <see cref="Settings"/> (config settings of <see cref="SettingsScope"/>).</summary>
    /// <remarks>
    ///     Is <see langword="null"/> until <see cref="Reset"/> is called. Gets set to a new instance by
    ///     deserialization
    ///     (see <see cref="Load"/>).
    /// </remarks>
    internal static SettingsScope? _settings;

    private static readonly ReaderWriterLockSlim _settingsLockSlim = new ();

    /// <summary>
    ///     The root object of Terminal.Gui configuration settings / JSON schema. Contains only properties with the
    ///     <see cref="SettingsScope"/> attribute value.
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

    /// <summary>
    ///     Gets or sets whether the <see cref="ConfigurationManager"/> should throw an exception if it encounters an
    ///     error on deserialization. If <see langword="false"/> (the default), the error is logged and printed to the console
    ///     when <see cref="Application.Shutdown"/> is called.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static bool? ThrowOnJsonErrors { get; set; } = false;

    /// <summary>Event fired when an updated configuration has been applied to the application.</summary>
    public static event EventHandler<ConfigurationManagerEventArgs>? Applied;

    /// <summary>
    ///     Applies the configuration settings to static <see cref="SerializableConfigurationProperty"/> properties.
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
    ///     Gets or sets the in-memory config.json. See <see cref="ConfigLocations.Runtime"/>.
    /// </summary>
    public static string? RuntimeConfig { get; set; } = """{  }""";

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    private static readonly string _configFilename = "config.json";

    /// <summary>
    ///     Loads all settings found in the configuration storage locations (<see cref="ConfigLocations"/>). Optionally, resets
    ///     all settings attributed with
    ///     <see cref="SerializableConfigurationProperty"/> to the those loaded from <see cref="ConfigLocations.LibraryResources"/>, or if
    ///     a property is not found, to the value the static property was initialized with when the module loaded.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use <see cref="Apply"/> to cause the loaded settings to be applied to the running application.
    ///     </para>
    /// </remarks>
    /// <param name="reset">
    ///     If <see langword="true"/> the state of <see cref="ConfigurationManager"/> will be reset to the
    ///     defaults (<see cref="ConfigLocations.LibraryResources"/>).
    /// </param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static void Load (bool reset = false)
    {
        Logging.Trace ($"reset = {reset}");

        if (!IsEnabled)
        {
            throw new InvalidOperationException ("ConfigurationManager is not enabled. Call Enable() to enable it.");
        }

        if (reset)
        {
            Reset ();
        }

        if (Settings is null)
        {
            ResetToCurrentValues ();
        }

        // To enable some unit tests, we only load from resources if the flag is set
        if (Locations.HasFlag (ConfigLocations.LibraryResources))
        {
            SourcesManager?.UpdateFromResource (
                                                Settings,
                                                typeof (ConfigurationManager).Assembly,
                                                $"Terminal.Gui.Resources.{_configFilename}",
                                                ConfigLocations.LibraryResources
                                               );
        }

        if (Locations.HasFlag (ConfigLocations.AppResources))
        {
            string? embeddedStylesResourceName = Assembly.GetEntryAssembly ()
                                                         ?
                                                         .GetManifestResourceNames ()
                                                         .FirstOrDefault (x => x.EndsWith (_configFilename));

            if (string.IsNullOrEmpty (embeddedStylesResourceName))
            {
                embeddedStylesResourceName = _configFilename;
            }

            SourcesManager?.UpdateFromResource (Settings, Assembly.GetEntryAssembly ()!, embeddedStylesResourceName!, ConfigLocations.AppResources);
        }

        // TODO: Determine if Runtime should be applied last.
        if (Locations.HasFlag (ConfigLocations.Runtime) && !string.IsNullOrEmpty (RuntimeConfig))
        {
            SourcesManager?.Update (Settings, RuntimeConfig, "ConfigurationManager.RuntimeConfig", ConfigLocations.Runtime);
        }

        if (Locations.HasFlag (ConfigLocations.GlobalCurrent))
        {
            SourcesManager?.Update (Settings, $"./.tui/{_configFilename}", ConfigLocations.GlobalCurrent);
        }

        if (Locations.HasFlag (ConfigLocations.GlobalHome))
        {
            SourcesManager?.Update (Settings, $"~/.tui/{_configFilename}", ConfigLocations.GlobalHome);
        }

        if (Locations.HasFlag (ConfigLocations.AppCurrent))
        {
            SourcesManager?.Update (Settings, $"./.tui/{AppName}.{_configFilename}", ConfigLocations.AppCurrent);
        }

        if (Locations.HasFlag (ConfigLocations.AppHome))
        {
            SourcesManager?.Update (Settings, $"~/.tui/{AppName}.{_configFilename}", ConfigLocations.AppHome);
        }

        ThemeManager.Theme = Settings! ["Theme"].PropertyValue as string ?? throw new InvalidOperationException ();
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

    /// <summary>
    ///     Internal method. Resets <see cref="ConfigurationManager"/>.
    /// 
    ///     Called by <see cref="Load"/> if the <c>reset</c> parameter is <see langword="true"/>.
    /// </summary>
    /// <remarks>
    ///     If <see cref="Locations"/> does not include <see cref="ConfigLocations.LibraryResources"/>, the settings will all be
    ///     <see langword="null"/> or <see langword="default"/>.
    /// </remarks>
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
            if (IsEnabled)
            {
                _settings = new ();
            }
            else
            {
                _settings = null;
            }
        }
        finally
        {
            _settingsLockSlim.ExitWriteLock ();
        }

        _cachedAppSettingsLock.EnterWriteLock ();

        try
        {
            if (IsEnabled)
            {
                _cachedAppSettings = new ();
            }
            else
            {
                _cachedAppSettings = null;
            }
        }
        finally
        {
            _cachedAppSettingsLock.ExitWriteLock ();
        }

        if (!IsEnabled)
        {
            Logging.Error ($"ConfigurationManager is not enabled. Settings are invalid.");

            return;
        }

        ResetToCurrentValues ();

        OnUpdated ();

        // BUGBUG: Why do we apply here?
        Apply ();
        ThemeManager.Themes? [ThemeManager.Theme]?.Apply ();

        AppSettings?.Apply ();
    }


    /// <summary>Event fired when the configuration has been updated from a configuration source or reset.</summary>
    public static event EventHandler<ConfigurationManagerEventArgs>? Updated;

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

    /// <summary>
    ///     Resets ConfigurationManager to the current values of the static properites attributed with
    ///     <see cref="SerializableConfigurationProperty"/> in the Terminal.Gui library. Used in
    ///     development of
    ///     the library to generate the default configuration file.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method is only really useful when using ConfigurationManagerTests to generate the JSON doc that is
    ///         embedded into Terminal.Gui (during development).
    ///     </para>
    ///     <para>
    ///         WARNING: The <c>Terminal.Gui.Resources.config.json</c> resource has setting definitions (Themes) that are NOT
    ///         generated by this function. If you use this function to regenerate <c>Terminal.Gui.Resources.config.json</c>,
    ///         make sure you copy the Theme definitions from the existing <c>Terminal.Gui.Resources.config.json</c> file.
    ///     </para>
    /// </remarks>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal static void ResetToCurrentValues ()
    {
        if (!IsInitialized ())
        {
            throw new InvalidOperationException ("Initialize must be called first.");
        }

        Settings = new ();
        Settings.RetrieveValues ();
        ThemeManager.ResetToCurrentValues ();
        AppSettings?.RetrieveValues ();

        //foreach (KeyValuePair<string, ConfigProperty> p in Settings!.Where (cp => cp.Value.PropertyInfo is { }))
        //{
        //    Settings! [p.Key].PropertyValue = p.Value.PropertyInfo?.GetValue (null);
        //}
    }

    /// <summary>
    ///     Retrieves all uninitialized configuration properties that belong to a specific scope from the cache.
    ///     The items in the collection are references to the original <see cref="ConfigProperty"/> objects in the
    ///     cache. They do not have values and have <see cref="ConfigProperty.Immutable"/> set.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    internal static IEnumerable<KeyValuePair<string, ConfigProperty>>? GetConfigPropertiesByScope (Type scopeType)
    {
        if (_allConfigPropertiesCache is null)
        {
            throw new InvalidOperationException ("_allConfigPropertiesCache has not been set.");
        }

        // Filter properties by scope
        IEnumerable<KeyValuePair<string, ConfigProperty>>? filtered = _allConfigPropertiesCache?.Where (
                                                                                                      cp =>
                                                                                                          cp.Value.PropertyInfo?.GetCustomAttribute<SerializableConfigurationProperty> ()?.Scope == scopeType);

        Debug.Assert (filtered is { });
        return filtered;
    }

    /// <summary>
    ///     Retrieves all configuration properties that belong to a specific scope from the hard coded value cache.
    ///     The items in the collection are references to the original <see cref="ConfigProperty"/> objects in the
    ///     cache. They contain the hard coded values and have <see cref="ConfigProperty.Immutable"/> set.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    internal static IEnumerable<KeyValuePair<string, ConfigProperty>>? GetHardCodedConfigPropertiesByScope (Type scopeType)
    {
        // Filter properties by scope
        IEnumerable<KeyValuePair<string, ConfigProperty>>? cache = GetHardCodedConfigPropertyCache ();

        if (cache is null)
        {
            throw new InvalidOperationException ("GetHardCodedConfigPropertyCache returned null");
        }

        IEnumerable<KeyValuePair<string, ConfigProperty>>? scopedCache = cache?.Where (
                                                                                       cp =>
                                                                                       {
                                                                                           var ret = cp.Value.PropertyInfo?.GetCustomAttribute<SerializableConfigurationProperty> ()?.Scope == scopeType;

                                                                                           return ret;
                                                                                       });


        return scopedCache;
    }


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
        var settings = GetHardCodedConfigPropertiesByScope (typeof (SettingsScope));

        if (settings is null)
        {
            throw new InvalidOperationException ("GetHardCodedConfigPropertiesByScope returned null.");
        }
        var settingsDict = settings.ToDictionary();

        foreach (KeyValuePair<string, ConfigProperty> p in Settings!.Where (cp => cp.Value.PropertyInfo is { }))
        {
            emptyScope [p.Key].PropertyValue = settingsDict [p.Key].PropertyValue;
        }

        return JsonSerializer.Serialize (emptyScope, typeof (SettingsScope), SerializerContext!);
    }

}

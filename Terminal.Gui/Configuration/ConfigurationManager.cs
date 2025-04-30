global using static Terminal.Gui.ConfigurationManager;
global using CM = Terminal.Gui.ConfigurationManager;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Terminal.Gui.Configuration;

#nullable enable

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
///     Settings are applied using the following precedence (higher precedence settings overwrite lower precedence
///     settings):
///     <para>
///         1. Application configuration found in the users' home directory (<c>~/.tui/appname.config.json</c>) --
///         Highest precedence
///     </para>
///     <para>
///         2. Application configuration found in the directory the app was launched from (
///         <c>./.tui/appname.config.json</c>).
///     </para>
///     <para>3. Application configuration found in the applications' resources (<c>Resources/config.json</c>).</para>
///     <para>4. Global configuration found in the user's home directory (<c>~/.tui/config.json</c>).</para>
///     <para>5. Global configuration found in the directory the app was launched from (<c>./.tui/config.json</c>).</para>
///     <para>
///         6. Global configuration in <c>Terminal.Gui.dll</c>'s resources (<c>Terminal.Gui.Resources.config.json</c>) --
///         Lowest Precedence.
///     </para>
/// </summary>
[ComponentGuarantees (ComponentGuaranteesOptions.None)]
public static class ConfigurationManager
{
    /// <summary>
    ///     A cache of all properties in the Terminal.Gui project that are decorated with the
    ///     <see cref="SerializableConfigurationProperty"/> attribute. The keys are the property names pre-pended with the
    ///     class that implements the property (e.g. <c>Application.UseSystemConsole</c>). The values are instances of
    ///     <see cref="ConfigProperty"/> which hold the property's value and the <see cref="PropertyInfo"/> that allows
    ///     <see cref="ConfigurationManager"/> to get and set the property's value.
    /// </summary>
    /// <remarks>Is <see langword="null"/> until <see cref="Initialize"/> is called.</remarks>
    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static Dictionary<string, ConfigProperty>? _allConfigProperties;

    /// <summary>
    ///     A cache of all classes that have properties decorated with the <see cref="SerializableConfigurationProperty"/>.
    /// </summary>
    /// <remarks>Is <see langword="null"/> until <see cref="Initialize"/> is called.</remarks>
    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static Dictionary<string, Type>? _classesWithConfigProps;

    [RequiresUnreferencedCode ("AOT")]
    internal static void Initialize ()
    {
        // Cache all classes with configuration properties
        _classesWithConfigProps = GetClassesWithConfigProperties ();

        // Cache all configuration properties
        _allConfigProperties = GetAllConfigProperties ();

        // Sort the properties
        _allConfigProperties = _allConfigProperties.OrderBy (x => x.Key)
                                                   .ToDictionary (
                                                                  x => x.Key,
                                                                  x => x.Value,
                                                                  StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    ///     Un-initializes CM. To be called as a pair with <see cref="Initialize"/>.
    /// </summary>

    internal static void UnInitialize ()
    {
        _allConfigProperties = null;
        _classesWithConfigProps = null;
        _settings = null;
        _themeManager = null;
        _appSettings = null;

        Locations = ConfigLocations.All;

        // TODO: Should _jsonErrors be reset?
    }

    /// <summary>
    ///     Gets whether the <see cref="ConfigurationManager"/> has been initialized.
    /// </summary>
    /// <returns></returns>
    public static bool IsInitialized ()
    {
        return _allConfigProperties is { };
    }

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static readonly SourceGenerationContext SerializerContext = new (new JsonSerializerOptions ()
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

    /// <summary>
    ///     Gets and sets the locations where <see cref="ConfigurationManager"/> will look for config files. The default value is
    ///     <see cref="ConfigLocations.All"/>.
    /// </summary>
    public static ConfigLocations Locations { get; set; } = ConfigLocations.All;

    /// <summary>
    ///     Gets the Sources Manager - manages the loading of configuration sources from files and resources.
    /// </summary>
    public static SourcesManager? SourcesManager { get; internal set; } = new SourcesManager ();

    /// <summary>Name of the running application. By default, this property is set to the application's assembly name.</summary>
    public static string AppName { get; set; } = Assembly.GetEntryAssembly ()?.FullName?.Split (',') [0]?.Trim ()!;

    /// <summary>The backing property for <see cref="AppSettings"/> (config settings of <see cref="AppScope"/>).</summary>
    /// <remarks>
    ///     Is <see langword="null"/> until <see cref="ResetAllSettings"/> is called. Gets set to a new instance by deserialization
    ///     (see <see cref="Load"/>).
    /// </remarks>
    internal static AppScope? _appSettings;

    /// <summary>Application-specific configuration settings (conifg properties with the <see cref="AppScope"/> scope.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    [JsonPropertyName ("AppSettings")]
    public static AppScope? AppSettings
    {
        get => _appSettings;
        set => _appSettings = value;
    }

    /// <summary>The backing property for <see cref="Settings"/> (config settings of <see cref="SettingsScope"/>).</summary>
    /// <remarks>
    ///     Is <see langword="null"/> until <see cref="ResetAllSettings"/> is called. Gets set to a new instance by deserialization
    ///     (see <see cref="Load"/>).
    /// </remarks>
    internal static SettingsScope? _settings;

    /// <summary>
    ///     The root object of Terminal.Gui configuration settings / JSON schema. Contains only properties with the
    ///     <see cref="SettingsScope"/> attribute value.
    /// </summary>
    public static SettingsScope? Settings
    {
        get
        {
            Debug.Assert (IsInitialized ());
            //if (_settings is null)
            //{
            //    // If Settings is null, we need to initialize it.
            //    ResetAllSettings ();
            //}

            return _settings;
        }
        set => _settings = value!;
    }


    /// <summary>The backing property for <see cref="ThemeManager"/> (a Dictionary of named <see cref="ThemeScope"/> objects).</summary>
    /// <remarks>
    ///     Is <see langword="null"/> until <see cref="ResetAllSettings"/> is called. Gets set to a new instance by deserialization
    ///     (see <see cref="Load"/>).
    /// </remarks>
    internal static ThemeManager? _themeManager = new ();

    /// <summary>
    ///     The root object of Terminal.Gui themes manager. ThemeManager is a Dictionary of named <see cref="ThemeScope"/> objects.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    [JsonPropertyName ("Themes")]
    public static ThemeManager? ThemeManager
    {
        get => _themeManager;
        set => _themeManager = value!;
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

    /// <summary>Applies the configuration settings to the running <see cref="Application"/> instance.</summary>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static void Apply ()
    {
        var settings = false;
        var themes = false;
        var appSettings = false;

        try
        {
            if (string.IsNullOrEmpty (ThemeManager.SelectedTheme))
            {
                // First start. Apply settings first. This ensures if a config sets Theme to something other than "Default", it gets used
                settings = Settings?.Apply () ?? false;

                themes = !string.IsNullOrEmpty (ThemeManager.SelectedTheme)
                         && (CM.ThemeManager! [ThemeManager.SelectedTheme]?.Apply () ?? false);
            }
            else
            {
                // Subsequently. Apply Themes first using whatever the SelectedTheme is
                themes = CM.ThemeManager! [ThemeManager.SelectedTheme]?.Apply () ?? false;
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

    /// <summary>Returns an empty Json document with just the $schema tag.</summary>
    /// <returns></returns>
    public static string GetEmptyConfig ()
    {
        var emptyScope = new SettingsScope ();
        emptyScope.Clear ();

        return JsonSerializer.Serialize (emptyScope, typeof (SettingsScope), CM.SerializerContext!);
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
    ///     <see cref="SerializableConfigurationProperty"/> to the defaults.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use <see cref="Apply"/> to cause the loaded settings to be applied to the running application.
    ///     </para>
    /// </remarks>
    /// <param name="reset">
    ///     If <see langword="true"/> the state of <see cref="ConfigurationManager"/> will be reset to the
    ///     defaults (<see cref="ConfigLocations.Default"/>).
    /// </param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static void Load (bool reset = false)
    {
        Logging.Trace ($"reset = {reset}");

        if (reset || Settings is null)
        {
            ResetAllSettings ();
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

        ThemeManager.SelectedTheme = Settings! ["Theme"].PropertyValue as string ?? "Default";
    }

    /// <summary>
    ///     Called when an updated configuration has been applied to the application. Fires the <see cref="Applied"/>
    ///     event.
    /// </summary>
    public static void OnApplied ()
    {
        //Logging.Trace ("");

        Applied?.Invoke (null, new ());

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
        Updated?.Invoke (null, new ());
    }

    /// <summary>Prints any Json deserialization errors that occurred during deserialization to the console.</summary>
    public static void PrintJsonErrors ()
    {
        if (_jsonErrors.Length > 0)
        {
            Console.WriteLine (
                               @"Terminal.Gui ConfigurationManager encountered the following errors while deserializing configuration files:"
                              );
            Console.WriteLine (_jsonErrors.ToString ());
        }
    }

    /// <summary>
    ///     Logs Json deserialization errors that occurred during deserialization.
    /// </summary>
    public static void LogJsonErrors ()
    {
        if (_jsonErrors.Length > 0)
        {
            Logging.Error (
                           @"Encountered the following errors while deserializing configuration files:"
                          );
            Logging.Error (_jsonErrors.ToString ());
        }
    }

    /// <summary>
    ///     Resets all settings managed by <see cref="ConfigurationManager"/> to the values in the <see cref="ConfigLocations.Default"/> resource.
    ///     Should be called whenever a new app session (e.g. in
    ///     <see cref="Application.Init"/> starts. Called by <see cref="Load"/> if the <c>reset</c> parameter is
    ///     <see langword="true"/>.
    /// </summary>
    /// <remarks>If <see cref="Locations"/> does not include <see cref="ConfigLocations.Default"/>, the settings will all be <see langword="null"/> or <see langword="default"/>.</remarks>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static void ResetAllSettings ()
    {
        if (!IsInitialized ())
        {
            throw new InvalidOperationException ("Initialize must be called first.");
        }

        ClearJsonErrors ();

        Settings = new SettingsScope ();
        ThemeManager?.Reset ();
        Settings ["Theme"].PropertyValue = ThemeManager.Theme;
        Settings ["Themes"].PropertyValue = ThemeManager;
        AppSettings = new ();

        // Debug.Assert (Locations.HasFlag (ConfigLocations.Default));

        // To enable some unit tests, we only load from resources if the flag is set
        if (Locations.HasFlag (ConfigLocations.Default))
        {
            SourcesManager?.UpdateFromResource (
                                               Settings,
                                               typeof (ConfigurationManager).Assembly,
                                               $"Terminal.Gui.Resources.{_configFilename}",
                                               ConfigLocations.Default
                                              );
        }

        OnUpdated ();

        Apply ();
        ThemeManager [ThemeManager.SelectedTheme]?.Apply ();
        AppSettings?.Apply ();
    }

    /// <summary>Event fired when the configuration has been updated from a configuration source or reset.</summary>
    public static event EventHandler<ConfigurationManagerEventArgs>? Updated;

    internal static void AddJsonError (string error)
    {
        Logging.Trace ($"error = {error}");
        _jsonErrors.AppendLine (error);
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
        ThemeManager.ResetToCurrentValues ();
        AppSettings?.RetrieveValues ();

        foreach (KeyValuePair<string, ConfigProperty> p in Settings!.Where (cp => cp.Value.PropertyInfo is { }))
        {
            Settings! [p.Key].PropertyValue = p.Value.PropertyInfo?.GetValue (null);
        }
    }

    /// <summary>
    /// Retrieves a dictionary of classes with configuration properties.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    private static Dictionary<string, Type> GetClassesWithConfigProperties ()
    {
        var classesWithConfigProps = new Dictionary<string, Type> (StringComparer.InvariantCultureIgnoreCase);

        var types = from assembly in AppDomain.CurrentDomain.GetAssemblies ()
                    from type in assembly.GetTypes ()
                    where type.GetProperties ()
                              .Any (prop => prop.GetCustomAttribute (typeof (SerializableConfigurationProperty)) != null)
                    select type;

        foreach (var classWithConfig in types)
        {
            classesWithConfigProps.Add (classWithConfig.Name, classWithConfig);
        }

        return classesWithConfigProps;
    }

    /// <summary>
    /// Retrieves all configuration properties
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    private static Dictionary<string, ConfigProperty> GetAllConfigProperties ()
    {
        if (_classesWithConfigProps is null)
        {
            throw new InvalidOperationException ("GetClassesWithConfigProperties must be called first.");
        }

        var allConfigProperties = new Dictionary<string, ConfigProperty> (StringComparer.InvariantCultureIgnoreCase);

        foreach (var property in from c in _classesWithConfigProps
                                 let props = c.Value.GetProperties (
                                     BindingFlags.Instance |
                                     BindingFlags.Static |
                                     BindingFlags.NonPublic |
                                     BindingFlags.Public)
                                 .Where (prop => prop.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is SerializableConfigurationProperty)
                                 from property in props
                                 select property)
        {
            if (property.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is not SerializableConfigurationProperty scp)
            {
                continue;
            }

            if (!property.GetGetMethod (true)!.IsPublic)
            {
                throw new InvalidOperationException (
                                                     $"Property {property.Name} in class {property.DeclaringType?.Name} is not public. SerializableConfigurationProperty properties must be public.");

            }

            if (property.GetGetMethod (true)!.IsStatic)
            {
                var key = scp.OmitClassName
                              ? ConfigProperty.GetJsonPropertyName (property)
                              : $"{property.DeclaringType?.Name}.{property.Name}";

                allConfigProperties.Add (key, new ConfigProperty
                {
                    PropertyInfo = property,
                    PropertyValue = null
                });
            }
            else
            {
                throw new InvalidOperationException (
                                                     $"Property {property.Name} in class {property.DeclaringType?.Name} is not static. SerializableConfigurationProperty properties must be static.");
            }
        }

        return allConfigProperties;
    }

    /// <summary>
    /// Retrieves all configuration properties that belong to a specific scope.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    internal static IEnumerable<KeyValuePair<string, ConfigProperty>> GetConfigPropertiesByScope (Type scopeType)
    {
        Dictionary<string, ConfigProperty>? allProperties = _allConfigProperties;
        if (!IsInitialized ())
        {
            // If CM has not been initialized, we return the a new list
            // PERFORMANCE: This should not be used in situations where perf is important.
            allProperties = GetAllConfigProperties ();
        }
        // Filter properties by scope
        return allProperties!.Where (cp =>
                                         cp.Value.PropertyInfo?.GetCustomAttribute<SerializableConfigurationProperty> ()?.Scope == scopeType);
    }

    private static void ClearJsonErrors () { _jsonErrors.Clear (); }


}

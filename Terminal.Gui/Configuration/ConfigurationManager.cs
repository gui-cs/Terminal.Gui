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
using Microsoft.Extensions.Logging;

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
    ///     A dictionary of all properties in the Terminal.Gui project that are decorated with the
    ///     <see cref="SerializableConfigurationProperty"/> attribute. The keys are the property names pre-pended with the
    ///     class that implements the property (e.g. <c>Application.UseSystemConsole</c>). The values are instances of
    ///     <see cref="ConfigProperty"/> which hold the property's value and the <see cref="PropertyInfo"/> that allows
    ///     <see cref="ConfigurationManager"/> to get and set the property's value.
    /// </summary>
    /// <remarks>Is <see langword="null"/> until <see cref="Initialize"/> is called.</remarks>
    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static Dictionary<string, ConfigProperty>? _allConfigProperties;

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static readonly JsonSerializerOptions SerializerOptions = new ()
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
    };

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static readonly SourceGenerationContext SerializerContext = new (SerializerOptions);

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    internal static StringBuilder _jsonErrors = new ();

    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    private static readonly string _configFilename = "config.json";

    /// <summary>The backing property for <see cref="Settings"/>.</summary>
    /// <remarks>
    ///     Is <see langword="null"/> until <see cref="Reset"/> is called. Gets set to a new instance by deserialization
    ///     (see <see cref="Load"/>).
    /// </remarks>
    private static SettingsScope? _settings;

    /// <summary>Name of the running application. By default, this property is set to the application's assembly name.</summary>
    public static string AppName { get; set; } = Assembly.GetEntryAssembly ()?.FullName?.Split (',') [0]?.Trim ()!;

    /// <summary>Application-specific configuration settings scope.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    [JsonPropertyName ("AppSettings")]
    public static AppScope? AppSettings { get; set; }

    /// <summary>
    ///     Gets and sets the locations where <see cref="ConfigurationManager"/> will look for config files. The value is
    ///     <see cref="ConfigLocations.All"/>.
    /// </summary>
    public static ConfigLocations Locations { get; set; } = ConfigLocations.All;

    /// <summary>
    ///     The root object of Terminal.Gui configuration settings / JSON schema. Contains only properties with the
    ///     <see cref="SettingsScope"/> attribute value.
    /// </summary>
    public static SettingsScope? Settings
    {
        [RequiresUnreferencedCode ("AOT")]
        [RequiresDynamicCode ("AOT")]
        get
        {
            if (_settings is null)
            {
                // If Settings is null, we need to initialize it.
                Reset ();
            }

            return _settings;
        }
        set => _settings = value!;
    }

    /// <summary>
    ///     The root object of Terminal.Gui themes manager. Contains only properties with the <see cref="ThemeScope"/>
    ///     attribute value.
    /// </summary>
    public static ThemeManager? Themes => ThemeManager.Instance;

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
                         && (ThemeManager.Themes? [ThemeManager.SelectedTheme]?.Apply () ?? false);
            }
            else
            {
                // Subsequently. Apply Themes first using whatever the SelectedTheme is
                themes = ThemeManager.Themes? [ThemeManager.SelectedTheme]?.Apply () ?? false;
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
    public static string GetEmptyJson ()
    {
        var emptyScope = new SettingsScope ();
        emptyScope.Clear ();

        return JsonSerializer.Serialize (emptyScope, typeof (SettingsScope), SerializerContext);
    }

    /// <summary>
    ///     Gets or sets the in-memory config.json. See <see cref="ConfigLocations.Runtime"/>.
    /// </summary>
    public static string? RuntimeConfig { get; set; } = """{  }""";

    /// <summary>
    ///     Loads all settings found in the configuration storage locations (<see cref="ConfigLocations"/>). Optionally, resets
    ///     all settings attributed with
    ///     <see cref="SerializableConfigurationProperty"/> to the defaults.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Use <see cref="Apply"/> to cause the loaded settings to be applied to the running application.
    /// </para>
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

        if (reset)
        {
            Reset ();
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

            Settings?.UpdateFromResource (Assembly.GetEntryAssembly ()!, embeddedStylesResourceName!, ConfigLocations.AppResources);
        }

        if (Locations.HasFlag (ConfigLocations.Runtime) && !string.IsNullOrEmpty (RuntimeConfig))
        {
            Settings?.Update (RuntimeConfig, "ConfigurationManager.RuntimeConfig", ConfigLocations.Runtime);
        }

        if (Locations.HasFlag (ConfigLocations.GlobalCurrent))
        {
            Settings?.Update ($"./.tui/{_configFilename}", ConfigLocations.GlobalCurrent);
        }

        if (Locations.HasFlag (ConfigLocations.GlobalHome))
        {
            Settings?.Update ($"~/.tui/{_configFilename}", ConfigLocations.GlobalHome);
        }


        if (Locations.HasFlag (ConfigLocations.AppCurrent))
        {
            Settings?.Update ($"./.tui/{AppName}.{_configFilename}", ConfigLocations.AppCurrent);
        }

        if (Locations.HasFlag (ConfigLocations.AppHome))
        {
            Settings?.Update ($"~/.tui/{AppName}.{_configFilename}", ConfigLocations.AppHome);
        }

        ThemeManager.SelectedTheme = Settings!["Theme"].PropertyValue as string ?? "Default";
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
    ///     Resets the state of <see cref="ConfigurationManager"/>. Should be called whenever a new app session (e.g. in
    ///     <see cref="Application.Init"/> starts. Called by <see cref="Load"/> if the <c>reset</c> parameter is
    ///     <see langword="true"/>.
    /// </summary>
    /// <remarks></remarks>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static void Reset ()
    {
        Logging.Trace ($"_allConfigProperties = {_allConfigProperties}");

        if (_allConfigProperties is null)
        {
            Initialize ();
        }

        ClearJsonErrors ();

        Settings = new ();
        ThemeManager.Reset ();
        AppSettings = new ();

        // To enable some unit tests, we only load from resources if the flag is set
        if (Locations.HasFlag (ConfigLocations.Default))
        {
            Settings.UpdateFromResource (
                                         typeof (ConfigurationManager).Assembly,
                                         $"Terminal.Gui.Resources.{_configFilename}",
                                         ConfigLocations.Default
                                        );
        }

        OnUpdated ();

        Apply ();
        ThemeManager.Themes? [ThemeManager.SelectedTheme]?.Apply ();
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
    ///     System.Text.Json does not support copying a deserialized object to an existing instance. To work around this,
    ///     we implement a 'deep, member-wise copy' method.
    /// </summary>
    /// <remarks>TOOD: When System.Text.Json implements `PopulateObject` revisit https://github.com/dotnet/corefx/issues/37627</remarks>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <returns><paramref name="destination"/> updated from <paramref name="source"/></returns>
    internal static object? DeepMemberWiseCopy (object? source, object? destination)
    {
        ArgumentNullException.ThrowIfNull (destination);

        if (source is null)
        {
            return null!;
        }

        if (source.GetType () == typeof (SettingsScope))
        {
            return ((SettingsScope)destination).Update ((SettingsScope)source);
        }

        if (source.GetType () == typeof (ThemeScope))
        {
            return ((ThemeScope)destination).Update ((ThemeScope)source);
        }

        if (source.GetType () == typeof (AppScope))
        {
            return ((AppScope)destination).Update ((AppScope)source);
        }

        // If value type, just use copy constructor.
        if (source.GetType ().IsValueType || source is string)
        {
            return source;
        }

        // HACK: Key is a class, but we want to treat it as a value type so just _keyCode gets copied.
        if (source.GetType () == typeof (Key))
        {
            return source;
        }

        // Dictionary
        if (source.GetType ().IsGenericType
            && source.GetType ().GetGenericTypeDefinition ().IsAssignableFrom (typeof (Dictionary<,>)))
        {
            foreach (object? srcKey in ((IDictionary)source).Keys)
            {
                if (((IDictionary)destination).Contains (srcKey))
                {
                    ((IDictionary)destination) [srcKey] =
                        DeepMemberWiseCopy (((IDictionary)source) [srcKey], ((IDictionary)destination) [srcKey]);
                }
                else
                {
                    ((IDictionary)destination).Add (srcKey, ((IDictionary)source) [srcKey]);
                }
            }

            return destination;
        }

        // ALl other object types
        List<PropertyInfo>? sourceProps = source?.GetType ().GetProperties ().Where (x => x.CanRead).ToList ();
        List<PropertyInfo>? destProps = destination?.GetType ().GetProperties ().Where (x => x.CanWrite).ToList ()!;

        foreach ((PropertyInfo? sourceProp, PropertyInfo? destProp) in
                 from sourceProp in sourceProps
                 where destProps.Any (x => x.Name == sourceProp.Name)
                 let destProp = destProps.First (x => x.Name == sourceProp.Name)
                 where destProp.CanWrite
                 select (sourceProp, destProp))
        {
            object? sourceVal = sourceProp.GetValue (source);
            object? destVal = destProp.GetValue (destination);

            if (sourceVal is { })
            {
                try
                {
                    if (destVal is { })
                    {
                        // Recurse
                        destProp.SetValue (destination, DeepMemberWiseCopy (sourceVal, destVal));
                    }
                    else
                    {
                        destProp.SetValue (destination, sourceVal);
                    }
                }
                catch (ArgumentException e)
                {
                    throw new JsonException ($"Error Applying Configuration Change: {e.Message}", e);
                }
            }
        }

        return destination;
    }


    /// <summary>
    ///     Retrieves the hard coded default settings (static properites) from the Terminal.Gui library implementation. Used in
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
    internal static void GetHardCodedDefaults ()
    {
        if (_allConfigProperties is null)
        {
            throw new InvalidOperationException ("Initialize must be called first.");
        }

        Settings = new ();
        ThemeManager.GetHardCodedDefaults ();
        AppSettings?.RetrieveValues ();

        foreach (KeyValuePair<string, ConfigProperty> p in Settings!.Where (cp => cp.Value.PropertyInfo is { }))
        {
            Settings! [p.Key].PropertyValue = p.Value.PropertyInfo?.GetValue (null);
        }
    }

    /// <summary>
    ///     Initializes the internal state of ConfigurationManager. Nominally called once as part of application startup
    ///     to initialize global state. Also called from some Unit Tests to ensure correctness (e.g. Reset()).
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    internal static void Initialize ()
    {
        _allConfigProperties = new ();
        _settings = null;

        Dictionary<string, Type> classesWithConfigProps = new (StringComparer.InvariantCultureIgnoreCase);

        // Get Terminal.Gui.dll classes

        IEnumerable<Type> types = from assembly in AppDomain.CurrentDomain.GetAssemblies ()
                                  from type in assembly.GetTypes ()
                                  where type.GetProperties ()
                                            .Any (
                                                  prop => prop.GetCustomAttribute (
                                                                                   typeof (SerializableConfigurationProperty)
                                                                                  )
                                                          != null
                                                 )
                                  select type;

        foreach (Type? classWithConfig in types)
        {
            classesWithConfigProps.Add (classWithConfig.Name, classWithConfig);
        }

        //Logging.Trace ($"ConfigManager.getConfigProperties found {classesWithConfigProps.Count} classes:");
        classesWithConfigProps.ToList ().ForEach (x => Logging.Trace ($"  Class: {x.Key}"));

        foreach (PropertyInfo? p in from c in classesWithConfigProps
                                    let props = c.Value
                                                 .GetProperties (
                                                                 BindingFlags.Instance
                                                                 |
                                                                 BindingFlags.Static
                                                                 |
                                                                 BindingFlags.NonPublic
                                                                 |
                                                                 BindingFlags.Public
                                                                )
                                                 .Where (
                                                         prop =>
                                                             prop.GetCustomAttribute (
                                                                                      typeof (SerializableConfigurationProperty)
                                                                                     ) is
                                                                 SerializableConfigurationProperty
                                                        )
                                    let enumerable = props
                                    from p in enumerable
                                    select p)
        {
            if (p.GetCustomAttribute (typeof (SerializableConfigurationProperty)) is SerializableConfigurationProperty
                scp)
            {
                if (p.GetGetMethod (true)!.IsStatic)
                {
                    // If the class name is omitted, JsonPropertyName is allowed. 
                    _allConfigProperties!.Add (
                                               scp.OmitClassName
                                                   ? ConfigProperty.GetJsonPropertyName (p)
                                                   : $"{p.DeclaringType?.Name}.{p.Name}",
                                               new () { PropertyInfo = p, PropertyValue = null }
                                              );
                }
                else
                {
                    throw new (
                               $"Property {p.Name} in class {p.DeclaringType?.Name} is not static. All SerializableConfigurationProperty properties must be static."
                              );
                }
            }
        }

        _allConfigProperties = _allConfigProperties!.OrderBy (x => x.Key)
                                                    .ToDictionary (
                                                                   x => x.Key,
                                                                   x => x.Value,
                                                                   StringComparer.InvariantCultureIgnoreCase
                                                                  );

        //Logging.Trace ($"Found {_allConfigProperties.Count} properties:");

        //_allConfigProperties.ToList ().ForEach (x => Logging.Trace ($"  Property: {x.Key}"));

        AppSettings = new ();
    }

    /// <summary>Creates a JSON document with the configuration specified.</summary>
    /// <returns></returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal static string ToJson ()
    {
        //Logging.Trace ("ConfigurationManager.ToJson()");

        return JsonSerializer.Serialize (Settings!, typeof (SettingsScope), SerializerContext);
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    internal static Stream ToStream ()
    {
        string json = JsonSerializer.Serialize (Settings!, typeof (SettingsScope), SerializerContext);

        // turn it into a stream
        var stream = new MemoryStream ();
        var writer = new StreamWriter (stream);
        writer.Write (json);
        writer.Flush ();
        stream.Position = 0;

        return stream;
    }

    private static void ClearJsonErrors () { _jsonErrors.Clear (); }
}

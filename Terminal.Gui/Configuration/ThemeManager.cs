#nullable enable
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>Manages Themes.</summary>
/// <remarks>
///     <para>A Theme is a collection of settings that are named. The default theme is named "Default".</para>
///     <para>The <see cref="Theme"/> property is used to determine the currently active theme.</para>
///     <para>The <see cref="Themes"/> property is a dictionary of themes.</para>
/// </remarks>
public static class ThemeManager
{
    /// <summary>
    ///     Convenience method to get the current theme. The current theme is the item in the <see cref="Themes"/> dictionary,
    ///     with the key of <see cref="Theme"/>.
    /// </summary>
    /// <returns></returns>
    public static ThemeScope GetCurrentTheme () { return Themes! [Theme]; }

    /// <summary>
    ///     INTERNAL: Getter for <see cref="Themes"/>.
    ///     Convenience method to get the themes dictionary. The themes dictionary is a dictionary of <see cref="ThemeScope"/>
    ///     objects, with the key being the name of the theme.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static ConcurrentDictionary<string, ThemeScope> GetThemes ()
    {
        if (!ConfigurationManager.IsInitialized ())
        {
            // We're being called from the module initializer.
            // We need to provide a dictionary of themes containing the hard-coded theme.
            return GetHardCodedThemes ()!;
        }

        if (ConfigurationManager.Settings is null)
        {
            throw new InvalidOperationException ("Settings is null.");
        }

        if (ConfigurationManager.Settings.TryGetValue ("Themes", out ConfigProperty? themes))
        {
            if (themes.HasValue)
            {
                return (themes.PropertyValue as ConcurrentDictionary<string, ThemeScope>)!;
            }

            return GetHardCodedThemes ()!;
        }

        throw new InvalidOperationException ("Settings has no Themes property.");
    }

    /// <summary>
    ///    INTERNAL: Convenience method to get a list of theme names.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static ImmutableList<string> GetThemeNames ()
    {
        if (!ConfigurationManager.IsInitialized ())
        {
            // We're being called from the module initializer.
            // We need to provide a dictionary of themes containing the hard-coded theme.
            return GetHardCodedThemes ()!.Keys.ToImmutableList ();
        }

        if (ConfigurationManager.Settings is null)
        {
            throw new InvalidOperationException ("Settings is null.");
        }

        if (!ConfigurationManager.Settings.TryGetValue ("Themes", out ConfigProperty? themes))
        {
            throw new InvalidOperationException ("Settings has no Themes property.");
        }

        ConcurrentDictionary<string, ThemeScope>? returnConcurrentDictionary;

        if (themes.HasValue)
        {
            returnConcurrentDictionary = themes.PropertyValue as ConcurrentDictionary<string, ThemeScope>;
        }
        else
        {
            returnConcurrentDictionary = GetHardCodedThemes ();
        }

        return returnConcurrentDictionary!.Keys
                                          .OrderBy (key => key == DEFAULT_THEME_NAME ? string.Empty : key) // Ensure DEFAULT_THEME_NAME is first
                                          .ToImmutableList ();

    }

    /// <summary>
    ///     Convenience method to get the current theme name. The current theme name is the value of <see cref="Theme"/>.
    /// </summary>
    /// <returns></returns>
    public static string GetCurrentThemeName () { return Theme!; }

    // TODO: Add a lock around Theme and Themes
    // TODO: For now, this test can't run in parallel with other tests that access Theme or Themes.
    // TODO: ThemeScopeList_WithThemes_ClonesSuccessfully

    /// <summary>
    ///     Gets the Themes dictionary. <see cref="GetThemes"/> is preferred.
    ///     The backing store is <c><see cref="ConfigurationManager.Settings"/> ["Themes"]</c>.
    ///     However, if <see cref="ConfigurationManager.IsInitialized"/> is <c>false</c>, this property will return the
    ///     hard-coded themes.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [JsonConverter (typeof (ConcurrentDictionaryJsonConverter<ThemeScope>))]
    [ConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    public static ConcurrentDictionary<string, ThemeScope>? Themes
    {
        // Note: This property getter must be public; DeepClone depends on it.
        get => GetThemes ();
        internal set => SetThemes (value);
    }

    /// <summary>
    ///     INTERNAL: Setter for <see cref="Themes"/>.
    /// </summary>
    /// <param name="dictionary"></param>
    /// <exception cref="InvalidOperationException"></exception>
    private static void SetThemes (ConcurrentDictionary<string, ThemeScope>? dictionary)
    {
        if (dictionary is { } && !dictionary.ContainsKey (DEFAULT_THEME_NAME))
        {
            throw new InvalidOperationException ($"Themes must include an item named {DEFAULT_THEME_NAME}");
        }

        if (ConfigurationManager.Settings is { } && ConfigurationManager.Settings.TryGetValue ("Themes", out ConfigProperty? themes))
        {
            ConfigurationManager.Settings ["Themes"].PropertyValue = dictionary;

            return;
        }

        throw new InvalidOperationException ("Settings is null.");
    }

    /// <summary>
    ///     INTERNAL: Returns the hard-coded Themes dictionary.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static ConcurrentDictionary<string, ThemeScope>? GetHardCodedThemes ()
    {
        ThemeScope? hardCodedThemeScope = GetHardCodedThemeScope ();

        if (hardCodedThemeScope is null)
        {
            throw new InvalidOperationException ("Hard coded theme scope is null.");
        }

        return new (new Dictionary<string, ThemeScope> { { DEFAULT_THEME_NAME, hardCodedThemeScope } }, StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    ///     INTERNAL: Returns the ThemeScope containing the hard-coded Themes.
    /// </summary>
    /// <returns></returns>
    private static ThemeScope GetHardCodedThemeScope ()
    {
        IEnumerable<KeyValuePair<string, ConfigProperty>>? hardCodedThemeProperties = ConfigurationManager.GetHardCodedConfigPropertiesByScope ("ThemeScope");

        if (hardCodedThemeProperties is null)
        {
            throw new InvalidOperationException ("Hard coded theme properties are null.");
        }

        var hardCodedThemeScope = new ThemeScope ();
        foreach (KeyValuePair<string, ConfigProperty> p in hardCodedThemeProperties)
        {
            hardCodedThemeScope.AddValue (p.Key, p.Value.PropertyValue);
        }

        return hardCodedThemeScope;
    }

    /// <summary>
    ///     The name of the default theme ("Default").
    /// </summary>
    public const string DEFAULT_THEME_NAME = "Default";

    /// <summary>
    ///     The currently selected theme. The backing store is <c><see cref="ConfigurationManager.Settings"/> ["Theme"]</c>.
    /// </summary>
    [JsonInclude]
    [ConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    [JsonPropertyName ("Theme")]
    public static string Theme
    {
        get
        {
            if (!ConfigurationManager.IsInitialized ())
            {
                // We're being called from the module initializer.
                // Hard coded default value
                return DEFAULT_THEME_NAME;
            }

            if (ConfigurationManager.Settings is { } && ConfigurationManager.Settings.TryGetValue ("Theme", out ConfigProperty? themeCp))
            {
                if (themeCp.HasValue)
                {
                    return (themeCp.PropertyValue as string)!;
                }

                return DEFAULT_THEME_NAME;
            }

            throw new InvalidOperationException ("Settings is null.");
        }

        [RequiresUnreferencedCode ("Calls Terminal.Gui.ConfigurationManager.Settings")]
        [RequiresDynamicCode ("Calls Terminal.Gui.ConfigurationManager.Settings")]
        set
        {
            if (!ConfigurationManager.IsInitialized ())
            {
                throw new InvalidOperationException ("Theme cannot be set before ConfigurationManager is initialized.");
            }

            if (ConfigurationManager.Settings is null || !ConfigurationManager.Settings.TryGetValue ("Theme", out ConfigProperty? themeCp))
            {
                throw new InvalidOperationException ("Settings is null.");
            }

            if (themeCp is null || !themeCp.HasValue)
            {
                throw new InvalidOperationException ("Theme has no value.");
            }

            if (!ConfigurationManager.Settings.TryGetValue ("Themes", out ConfigProperty? themesCp))
            {
                throw new InvalidOperationException ("Settings has no Themes property.");
            }

            string previousThemeValue = GetCurrentThemeName ();

            if (value == previousThemeValue)
            {
                return;
            }

            if (!Themes!.ContainsKey (value))
            {
                Logging.Warning ($"{value} is not a valid theme name.");
            }

            // Update the backing store
            ConfigurationManager.Settings! ["Theme"].PropertyValue = value;

            OnThemeChanged (previousThemeValue, value);
        }
    }

    /// <summary>
    ///     INTERNAL: Updates <see cref="Themes"/> to the current values of the static
    ///     <see cref="ConfigurationPropertyAttribute"/> properties.
    /// </summary>
    [RequiresUnreferencedCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    internal static void UpdateToCurrentValues ()
    {
        // BUGBUG: This corrupts _hardCodedDefaults. See #4288
        Themes! [Theme].UpdateToCurrentValues ();
    }

    /// <summary>
    ///     INTERNAL: Loads all Themes to their hard-coded default values.
    /// </summary>
    [RequiresUnreferencedCode ("Calls SchemeManager.LoadToHardCodedDefaults")]
    [RequiresDynamicCode ("Calls SchemeManager.LoadToHardCodedDefaults")]

    internal static void LoadHardCodedDefaults ()
    {
        if (!ConfigurationManager.IsInitialized ())
        {
            throw new InvalidOperationException ("ThemeManager is not initialized.");
        }

        if (ConfigurationManager.Settings is null)
        {
            return;
        }

        ThemeScope? hardCodedThemeScope = GetHardCodedThemeScope ();

        if (hardCodedThemeScope is null)
        {
            throw new InvalidOperationException ("Hard coded theme scope is null.");
        }

        ConcurrentDictionary<string, ThemeScope> hardCodedThemes = new (
                                                                        new Dictionary<string, ThemeScope>
                                                                        {
                                                                            { Theme, hardCodedThemeScope }
                                                                        },
                                                                        StringComparer.InvariantCultureIgnoreCase);

        // BUGBUG: SchemeManager is broken and needs to be fixed to not have the hard coded schemes get overwritten.
        // BUGBUG: This is a partial workaround
        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/4288
        SchemeManager.LoadToHardCodedDefaults ();

        ConfigurationManager.Settings ["Themes"].PropertyValue = hardCodedThemes;
        ConfigurationManager.Settings ["Theme"].PropertyValue = DEFAULT_THEME_NAME;

    }

    /// <summary>Called when the selected theme has changed. Fires the <see cref="ThemeChanged"/> event.</summary>
    internal static void OnThemeChanged (string previousThemeName, string newThemeName)
    {
        Logging.Debug ($"Themes.OnThemeChanged({previousThemeName}) -> {Theme}");
        EventArgs<string> args = new (newThemeName);
        ThemeChanged?.Invoke (null, args);
    }

    /// <summary>Raised when the selected theme has changed.</summary>
    public static event EventHandler<EventArgs<string>>? ThemeChanged;
}

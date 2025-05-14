#nullable enable
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Manages Themes.</summary>
/// <remarks>
///     <para>A Theme is a collection of settings that are named. The default theme is named "Default".</para>
///     <para>The <see cref="Theme"/> property is used to determine the currently active theme.</para>
///     <para>The <see cref="Themes"/> property is a dictionary of themes.</para>
/// </remarks>
public static class ThemeManager
{
    /// <summary>
    ///     Convenience method to get the current theme. The current theme is the item in the <see cref="Themes"/> dictionary, with the key of <see cref="Theme"/>.     
    /// </summary>
    /// <returns></returns>
    public static ThemeScope GetCurrentTheme ()
    {
        return Themes! [Theme];
    }

    /// <summary>
    ///     Convenience method to get the themes dictionary. The themes dictionary is a dictionary of <see cref="ThemeScope"/> objects, with the key being the name of the theme.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static ConcurrentDictionary<string, ThemeScope> GetThemes ()
    {
        if (!ConfigurationManager.IsInitialized ())
        {
            // We're being called from the module initializer.
            // We need to provide a dictionary of themes containing the hard-coded theme.
            return HardCodedThemes ()!;
        }

        if (ConfigurationManager.Settings is { })
        {
            if (ConfigurationManager.Settings.TryGetValue ("Themes", out ConfigProperty? themes))
            {
                if (themes.HasValue)
                {
                    return (themes.PropertyValue as ConcurrentDictionary<string, ThemeScope>)!;

                    // new ConcurrentDictionary<string, ThemeScope>(
                    //(themes.PropertyValue as ConcurrentDictionary<string, ThemeScope>)!,
                    //StringComparer.InvariantCultureIgnoreCase);
                }
                return HardCodedThemes ()!;
            }

            throw new InvalidOperationException ("Settings has no Themes property.");
        }

        throw new InvalidOperationException ("Settings is null.");
    }

    /// <summary>
    ///     Convenience method to get a list of theme names.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static ImmutableList<string> GetThemeNames ()
    {
        if (!ConfigurationManager.IsInitialized ())
        {
            // We're being called from the module initializer.
            // We need to provide a dictionary of themes containing the hard-coded theme.
            return HardCodedThemes ()!.Keys.ToImmutableList ();
        }
        if (ConfigurationManager.Settings is { })
        {
            if (ConfigurationManager.Settings.TryGetValue ("Themes", out ConfigProperty? themes))
            {
                if (themes.HasValue)
                {
                    ConcurrentDictionary<string, ThemeScope>? themesValue = themes.PropertyValue as ConcurrentDictionary<string, ThemeScope>;
                    return themesValue!.Keys.ToImmutableList ();
                }
                return HardCodedThemes ()!.Keys.ToImmutableList ();
            }
            throw new InvalidOperationException ("Settings has no Themes property.");
        }
        throw new InvalidOperationException ("Settings is null.");
    }

    /// <summary>
    ///     Convenience method to get the current theme name. The current theme name is the value of <see cref="Theme"/>.
    /// </summary>
    /// <returns></returns>
    public static string GetCurrentThemeName ()
    {
        return Theme!;
    }

    /// <summary>
    ///     Gets the Themes dictionary. <see cref="GetThemes"/> is preferred.
    ///     The backing store is <c><see cref="ConfigurationManager.Settings"/> ["Themes"]</c>.
    ///     However, if <see cref="ConfigurationManager.IsInitialized"/> is <c>false</c>, this property will return the hard-coded themes.
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

    private static void SetThemes (ConcurrentDictionary<string, ThemeScope>? dictionary)
    {
        if (!ConfigurationManager.IsEnabled)
        {
            throw new InvalidOperationException ("Can't set Themes when ConfigurationManager is disabled.");
        }

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

    private static ConcurrentDictionary<string, ThemeScope>? HardCodedThemes ()
    {
        ThemeScope? hardCodedThemeScope = GetHardCodedThemeScope ();
        if (hardCodedThemeScope is null)
        {
            throw new InvalidOperationException ("Hard coded theme scope is null.");
        }

        return new ConcurrentDictionary<string, ThemeScope> (
            new Dictionary<string, ThemeScope>
            {
                { DEFAULT_THEME_NAME, hardCodedThemeScope }
            },
            StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    ///     Returns a dictionary of hard-coded ThemeScope properties.
    /// </summary>
    /// <returns></returns>
    private static ThemeScope? GetHardCodedThemeScope ()
    {
        IEnumerable<KeyValuePair<string, ConfigProperty>>? hardCodedThemeProperties = ConfigurationManager.GetHardCodedConfigPropertiesByScope ("ThemeScope");

        if (hardCodedThemeProperties is null)
        {
            throw new InvalidOperationException ("Hard coded theme properties are null.");
        }

        Dictionary<string, ConfigProperty>? dict = hardCodedThemeProperties?.ToDictionary ();

        ThemeScope? hardCodedThemeScope = new ThemeScope ();
        foreach (KeyValuePair<string, ConfigProperty> p in hardCodedThemeScope)
        {
            p.Value.PropertyValue = dict! [p.Key].PropertyValue;
        }
        return hardCodedThemeScope;
    }

    /// <summary>
    ///     Since Theme is a dynamic property, we need to cache the value of the selected theme for when CM is not enabled.
    /// </summary>
    internal const string DEFAULT_THEME_NAME = "Default";

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

            if (!ConfigurationManager.IsEnabled)
            {
                throw new InvalidOperationException ("Can't set Theme when ConfigurationManager is disabled.");
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

            // Update the backing store
            ConfigurationManager.Settings! ["Theme"].PropertyValue = value;

            if (!Themes!.ContainsKey (value))
            {
                Logging.Warning ($"{value} is not a valid theme name.");
            }

            OnThemeChanged (previousThemeValue);
        }
    }

    /// <summary>Event fired when the selected theme has changed.</summary>
    public static event EventHandler<ThemeManagerEventArgs>? ThemeChanged;

    /// <summary>
    ///   INTERNAL: Updates <see cref="Themes"/> to the current values of the static <see cref="ConfigurationPropertyAttribute"/> properties.
    /// </summary>
    [RequiresUnreferencedCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    internal static void UpdateToCurrentValues ()
    {
        Themes! [Theme].LoadCurrentValues ();
    }

    /// <summary>
    ///     INTERNAL: Resets all themes to the values the <see cref="ConfigurationPropertyAttribute"/> properties contained
    ///     when the module was initialized.
    /// </summary>
    internal static void ResetToHardCodedDefaults ()
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

        ConfigurationManager.Settings ["Themes"].PropertyValue = hardCodedThemes;
        ConfigurationManager.Settings ["Theme"].PropertyValue = DEFAULT_THEME_NAME;
    }

    /// <summary>Called when the selected theme has changed. Fires the <see cref="ThemeChanged"/> event.</summary>
    internal static void OnThemeChanged (string theme)
    {
        Logging.Debug ($"Themes.OnThemeChanged({theme}) -> {Theme}");
        ThemeChanged?.Invoke (null, new (theme));
    }
}

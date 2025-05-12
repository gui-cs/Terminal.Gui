#nullable enable
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    ///     The Themes dictionary. The backing store is <c><see cref="ConfigurationManager.Settings"/> ["Themes"]</c>.
    ///     However, if <see cref="ConfigurationManager.IsInitialized"/> is <c>false</c>, this property will return the hard-coded themes.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [JsonConverter (typeof (DictionaryJsonConverter<ThemeScope>))]
    [ConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    public static Dictionary<string, ThemeScope>? Themes
    {
        get
        {
            if (!IsInitialized ())
            {
                // We're being called from the module initializer.
                // We need to provide a dictionary of themes containing the hard-coded theme.
                return HardCodedThemes ();
            }

            if (Settings is { })
            {
                if (Settings.TryGetValue ("Themes", out ConfigProperty? themes))
                {
                    if (themes.HasValue)
                    {
                        return themes.PropertyValue as Dictionary<string, ThemeScope>;
                    }
                    return HardCodedThemes ();
                }

                throw new InvalidOperationException ("Settings has no Themes property.");
            }

            throw new InvalidOperationException ("Settings is null.");
        }
        set
        {
            // TODO: For better decoupling, perhaps we should use events for this?
            if (Settings is { } && Settings.TryGetValue ("Themes", out ConfigProperty? themes))
            {
                Settings ["Themes"].PropertyValue = value;

                return;
            }

            throw new InvalidOperationException ("Settings is null.");
        }
    }

    private static Dictionary<string, ThemeScope>? HardCodedThemes ()
    {
        ThemeScope? hardCodedThemeScope = GetHardCodedThemeScope ();
        if (hardCodedThemeScope is null)
        {
            throw new InvalidOperationException ("Hard coded theme scope is null.");
        }

        Dictionary<string, ThemeScope> hardCodedThemes = new (StringComparer.InvariantCultureIgnoreCase)
        {
            { DEFAULT_THEME_NAME, hardCodedThemeScope }
        };
        return hardCodedThemes;
    }

    /// <summary>
    ///     Returns a dictionary of hard-coded ThemeScope properties.
    /// </summary>
    /// <returns></returns>
    private static ThemeScope? GetHardCodedThemeScope ()
    {
        IEnumerable<KeyValuePair<string, ConfigProperty>>? hardCodedThemeProperties = GetHardCodedConfigPropertiesByScope ("ThemeScope");

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
    private const string DEFAULT_THEME_NAME = "Default";

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
            if (!IsInitialized ())
            {
                // We're being called from the module initializer.
                // Hard coded default value
                return DEFAULT_THEME_NAME;
            }

            if (Settings is { } && Settings.TryGetValue ("Theme", out ConfigProperty? themeCp))
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
            if (!IsInitialized ())
            {
                throw new InvalidOperationException ("Theme cannot be set before ConfigurationManager is initialized.");
            }

            if (Settings is null || !Settings.TryGetValue ("Theme", out ConfigProperty? themeCp))
            {
                throw new InvalidOperationException ("Settings is null.");
            }

            if (themeCp is null || !themeCp.HasValue)
            {
                throw new InvalidOperationException ("Theme has no value.");
            }

            if (!Settings.TryGetValue ("Themes", out ConfigProperty? themesCp))
            {
                throw new InvalidOperationException ("Settings has no Themes property.");
            }

            // Update the backing store
            Settings! ["Theme"].PropertyValue = value;

            //Instance.OnThemeChanged (prevousThemeValue);
        }
    }

    /// <summary>Event fired he selected theme has changed. application.</summary>
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
        if (!IsInitialized ())
        {
            throw new InvalidOperationException ("ThemeManager is not initialized.");
        }

        if (Settings is null)
        {
            return;
        }

        ThemeScope? hardCodedThemeScope = GetHardCodedThemeScope ();
        if (hardCodedThemeScope is null)
        {
            throw new InvalidOperationException ("Hard coded theme scope is null.");
        }

        Dictionary<string, ThemeScope> hardCodedThemes = new (StringComparer.InvariantCultureIgnoreCase)
        {
            { Theme, hardCodedThemeScope }
        };

        Settings ["Themes"].PropertyValue = hardCodedThemes;
        Settings ["Theme"].PropertyValue = "Default";
    }

    /// <summary>Called when the selected theme has changed. Fires the <see cref="ThemeChanged"/> event.</summary>
    internal static void OnThemeChanged (string theme)
    {
        //Logging.Trace ($"Themes.OnThemeChanged({theme}) -> {Theme}");
        ThemeChanged?.Invoke (null, new ThemeManagerEventArgs (theme));
    }

}

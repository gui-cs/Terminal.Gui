#nullable enable
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Manages Themes.</summary>
/// <remarks>
///     <para>A Theme is a collection of settings that are named. The default theme is named "Default".</para>
///     <para>The <see cref="SelectedTheme"/> property is used to determine the currently active theme.</para>
///     <para>The <see cref="Themes"/> property is a dictionary of themes.</para>
/// </remarks>
public class ThemeManager
{
    private static readonly object _themesLock = new object ();
    /// <summary>
    ///     The Themes dictionary. The backing store is <see cref="ConfigurationManager.Settings"/>` ["Themes"]`.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    [JsonConverter (typeof (DictionaryJsonConverter<ThemeScope>))]
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    public static Dictionary<string, ThemeScope>? Themes
    {
        get
        {
            lock (_themesLock)
            {
                if (Settings is { } && Settings.TryGetValue ("Themes", out ConfigProperty? themes))
                {
                    Debug.Assert (themes.PropertyValue is Dictionary<string, ThemeScope>);

                    return themes.PropertyValue as Dictionary<string, ThemeScope>;
                }
            }

            throw new InvalidOperationException ("Settings is invalid.");
        }
        set
        {
            lock (_themesLock)
            {
                // BUGBUG: We should not be setting Settings here? Instead, Settings should subscrube to something and update
                if (Settings is { } && Settings.TryGetValue ("Themes", out ConfigProperty? themes))
                {
                    Settings ["Themes"].PropertyValue = value;
                }
            }
        }
    }

    //{
    //    [RequiresUnreferencedCode ("AOT")]
    //    [RequiresDynamicCode ("AOT")]
    //    get
    //    {
    //        if (Settings is { } && Settings.TryGetValue ("Themes", out ConfigProperty? themes))
    //        {
    //            return themes.PropertyValue as Dictionary<string, ThemeScope>;
    //        }

    //        return null;
    //    }

    //    // themes ?? new Dictionary<string, ThemeScope> ();

    //    [RequiresUnreferencedCode ("AOT")]
    //    [RequiresDynamicCode ("AOT")]
    //    set
    //    {
    //        //if (themes is null || value is null) {
    //        //	themes = value;
    //        //} else {
    //        //	themes = (Dictionary<string, ThemeScope>)DeepMemberwiseCopy (value!, themes!)!;
    //        //}

    //        // BUGBUG: We should not be setting Settings here. Instead, Settings should subscrube to something and update
    //        if (Settings is { } && Settings.TryGetValue ("Themes", out ConfigProperty? themes))
    //        {
    //            Settings ["Themes"].PropertyValue = value;
    //        }
    //    }
    //}

    private static string _selectedTheme = string.Empty;

    // TODO: Rename to "THeme"
    /// <summary>The currently selected theme. The backing store is <see cref="ConfigurationManager.Settings"/>` ["Theme"]`.</summary>
    [JsonInclude]
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    [JsonPropertyName ("Theme")]
    public static string SelectedTheme
    {
        // BUGBUG: The backing store is supposed to be Settings!
        get => _selectedTheme;

        [RequiresUnreferencedCode ("Calls Terminal.Gui.ConfigurationManager.Settings")]
        [RequiresDynamicCode ("Calls Terminal.Gui.ConfigurationManager.Settings")]
        set
        {
            string prevousThemeValue = _selectedTheme;

            _selectedTheme = value;

            if (Settings is null || !Settings.TryGetValue ("Theme", out ConfigProperty? themeCp))
            {
                return;
            }

            lock (_themesLock)
            {
                if (themeCp.PropertyValue is string { } theme && Settings.TryGetValue ("Themes", out ConfigProperty? themesCp))
                {
                    // Check if the theme is in the themes dictionary
                    if (themesCp.PropertyValue is not Dictionary<string, ThemeScope> themes || !themes.TryGetValue (theme, out _))
                    {
                        return;
                    }

                    if (prevousThemeValue != _selectedTheme || prevousThemeValue != theme)
                    {
                        Settings! ["Theme"].PropertyValue = _selectedTheme;

                        //Instance.OnThemeChanged (prevousThemeValue);
                    }
                }
            }
        }
    }

    /// <summary>Event fired he selected theme has changed. application.</summary>
    public event EventHandler<ThemeManagerEventArgs>? ThemeChanged;

    [RequiresUnreferencedCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    internal void ResetToCurrentValues ()
    {
        //Logging.Debug ("");
        Reset ();

        var theme = new ThemeScope ();
        theme.RetrieveValues ();

        lock (_themesLock)
        {

            Themes! [SelectedTheme] = theme;
        }
    }

    /// <summary>Called when the selected theme has changed. Fires the <see cref="ThemeChanged"/> event.</summary>
    internal void OnThemeChanged (string theme)
    {
        //Logging.Trace ($"Themes.OnThemeChanged({theme}) -> {Theme}");
        ThemeChanged?.Invoke (this, new ThemeManagerEventArgs (theme));
    }

    [RequiresUnreferencedCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    internal void Reset ()
    {
        //Logging.Debug ("");
        lock (_themesLock)
        {

            Settings! ["Themes"].PropertyValue = new Dictionary<string, ThemeScope> (StringComparer.InvariantCultureIgnoreCase);

            Themes?.Add ("Default", new ThemeScope ());
        }

        SelectedTheme = "Default";
    }
}

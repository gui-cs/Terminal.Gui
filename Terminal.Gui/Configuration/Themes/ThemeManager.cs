#nullable enable
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
/// Contains a dictionary of the <see cref="ThemeManager.Theme"/>s for a Terminal.Gui application.
/// </summary>
/// <remarks>
/// <para>A Theme is a collection of settings that are named. The default theme is named "Default".</para>
/// <para>The <see cref="ThemeManager.Theme"/> property is used to determine the currently active theme.</para>
/// </remarks>
/// <para>
/// <see cref="ThemeManager"/> is a singleton class. It is created when the first <see cref="ThemeManager"/> property
/// is accessed. Accessing <see cref="ThemeManager.Instance"/> is the same as accessing
/// <see cref="ConfigurationManager.Themes"/>.
/// </para>
/// <example>
/// <code>
/// "Themes": [
/// {
///     "Default": {
///         "ColorSchemes": [
///         {
///             "TopLevel": {
///                 "Normal": {
///                     "Foreground": "BrightGreen",
///                     "Background": "Black"
///                 },
///                 "Focus": {
///                     "Foreground": "White",
///                     "Background": "Cyan"
///                 },
///                 "HotNormal": {
///                     "Foreground": "Yellow",
///                     "Background": "Black"
///                 },
///                 "HotFocus": {
///                     "Foreground": "Blue",
///                     "Background": "Cyan"
///                 },
///                 "Disabled": {
///                     "Foreground": "DarkGray",
///                     "Background": "Black"
///                 }
///             }
///         }
///     }
/// }
/// </code>
/// </example>
public class ThemeManager : IDictionary<string, ThemeScope>
{
    #region Singleton

    private static ThemeManager? _instance;

    /// <summary>
    /// Class is a singleton.
    /// </summary>
    public static ThemeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ThemeManager ();
            }
            return _instance;
        }
        internal set => _instance = value;
    }

    #endregion

    #region Fields

    private string _selectedTheme = string.Empty;

    #endregion

    #region Constructors

    static ThemeManager () { } // Ensure lazy static constructor
    private ThemeManager () { } // Prevent external instantiation

    #endregion

    #region Properties

    /// <summary>
    /// Static property solely for configuration serialization.
    /// Used by <see cref="ConfigurationManager"/> to load and save themes.
    /// </summary>
    [JsonInclude]
    [JsonConverter (typeof (DictionaryJsonConverter<ThemeScope>))]
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    public static Dictionary<string, ThemeScope>? ThemesConfig
    {
        [RequiresUnreferencedCode ("AOT")]
        [RequiresDynamicCode ("AOT")]
        get => Settings? ["Themes"]?.PropertyValue as Dictionary<string, ThemeScope>;

        [RequiresUnreferencedCode ("AOT")]
        [RequiresDynamicCode ("AOT")]
        set => Settings! ["Themes"].PropertyValue = value;
    }

    /// <summary>
    /// Instance-level runtime Themes used by the application.
    /// </summary>
    public Dictionary<string, ThemeScope> Themes { get; private set; } = new ();

    /// <summary>
    /// Gets or sets the currently selected theme. The value is persisted to the "Theme" property.
    /// </summary>
    [JsonIgnore]
    public string Theme
    {
        get => _selectedTheme;

        [RequiresUnreferencedCode ("AOT")]
        [RequiresDynamicCode ("AOT")]
        set => SelectedTheme = value;
    }

    /// <summary>
    /// The currently selected theme (internal version; see <see cref="Theme"/>).
    /// </summary>
    [JsonInclude]
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    [JsonPropertyName ("Theme")]
    internal string SelectedTheme
    {
        get => _selectedTheme;

        [RequiresUnreferencedCode ("Calls Terminal.Gui.ConfigurationManager.Settings")]
        [RequiresDynamicCode ("Calls Terminal.Gui.ConfigurationManager.Settings")]
        set
        {
            string oldTheme = _selectedTheme;
            _selectedTheme = value;

            if ((oldTheme != _selectedTheme
                 || oldTheme != Settings! ["Theme"].PropertyValue as string)
                 && Settings! ["Themes"]?.PropertyValue is Dictionary<string, ThemeScope> themes
                 && themes.ContainsKey (_selectedTheme))
            {
                Settings! ["Theme"].PropertyValue = _selectedTheme;
                OnThemeChanged (oldTheme);
            }
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Event fired when the selected theme has changed.
    /// </summary>
    public event EventHandler<ThemeManagerEventArgs>? ThemeChanged;

    #endregion

    #region Methods

    /// <summary>
    /// Called when the selected theme has changed. Fires the <see cref="ThemeChanged"/> event.
    /// </summary>
    internal void OnThemeChanged (string oldTheme)
    {
        ThemeChanged?.Invoke (this, new ThemeManagerEventArgs (oldTheme));
    }

    /// <summary>
    /// Initializes the runtime <see cref="Themes"/> from the static <see cref="ThemesConfig"/>.
    /// Must be called after configuration is loaded.
    /// </summary>
    public void InitializeThemes ()
    {
        if (ThemesConfig != null)
        {
            Themes = new Dictionary<string, ThemeScope> (ThemesConfig, StringComparer.InvariantCultureIgnoreCase);
        }
        else
        {
            Themes = new Dictionary<string, ThemeScope> (StringComparer.InvariantCultureIgnoreCase);
        }
    }

    /// <summary>
    /// Sets up hardcoded default themes if no config is found.
    /// </summary>
    [RequiresUnreferencedCode ("Calls Terminal.Gui.ThemeManager.Instance.Themes")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ThemeManager.Instance.Themes")]
    internal void GetHardCodedDefaults ()
    {
        var theme = new ThemeScope ();
        theme.RetrieveValues ();

        Themes = new Dictionary<string, ThemeScope> (StringComparer.InvariantCultureIgnoreCase)
        {
            { "Default", theme }
        };
        _selectedTheme = "Default";
    }

    /// <summary>
    /// Resets all themes and clears the currently selected theme.
    /// </summary>
    [RequiresUnreferencedCode ("Calls Terminal.Gui.ThemeManager.Instance.Themes")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ThemeManager.Instance.Themes")]
    internal void Reset ()
    {
        Colors.Reset ();
        Themes.Clear ();
        _selectedTheme = string.Empty;
    }

    #endregion

    #region IDictionary Implementation

#pragma warning disable 1591

    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "<Pending>")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "<Pending>")]
    public ICollection<string> Keys => Themes.Keys;

    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "<Pending>")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "<Pending>")]
    public ICollection<ThemeScope> Values => Themes.Values;

    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "<Pending>")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "<Pending>")]
    public int Count => Themes.Count;

    [UnconditionalSuppressMessage ("Trimming", "IL2026", Justification = "<Pending>")]
    [UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "<Pending>")]
    public bool IsReadOnly => ((ICollection<KeyValuePair<string, ThemeScope>>)Themes).IsReadOnly;

    public ThemeScope this [string key]
    {
        [RequiresUnreferencedCode ("AOT")]
        [RequiresDynamicCode ("AOT")]
        get => Themes [key];

        [RequiresUnreferencedCode ("AOT")]
        [RequiresDynamicCode ("AOT")]
        set => Themes [key] = value;
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void Add (string key, ThemeScope value)
    {
        Themes.Add (key, value);
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public bool ContainsKey (string key)
    {
        return Themes.ContainsKey (key);
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public bool Remove (string key)
    {
        return Themes.Remove (key);
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public bool TryGetValue (string key, out ThemeScope value)
    {
        return Themes.TryGetValue (key, out value!);
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void Add (KeyValuePair<string, ThemeScope> item)
    {
        ((ICollection<KeyValuePair<string, ThemeScope>>)Themes).Add (item);
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void Clear ()
    {
        ((ICollection<KeyValuePair<string, ThemeScope>>)Themes).Clear ();
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public bool Contains (KeyValuePair<string, ThemeScope> item)
    {
        return ((ICollection<KeyValuePair<string, ThemeScope>>)Themes).Contains (item);
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void CopyTo (KeyValuePair<string, ThemeScope> [] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<string, ThemeScope>>)Themes).CopyTo (array, arrayIndex);
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public bool Remove (KeyValuePair<string, ThemeScope> item)
    {
        return ((ICollection<KeyValuePair<string, ThemeScope>>)Themes).Remove (item);
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public IEnumerator<KeyValuePair<string, ThemeScope>> GetEnumerator ()
    {
        return Themes.GetEnumerator ();
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    IEnumerator IEnumerable.GetEnumerator ()
    {
        return Themes.GetEnumerator ();
    }

#pragma warning restore 1591

    #endregion
}

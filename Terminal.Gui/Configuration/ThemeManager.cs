#nullable enable
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>Contains a dictionary of the <see cref="ThemeManager.Theme"/>s for a Terminal.Gui application.</summary>
/// <remarks>
///     <para>A Theme is a collection of settings that are named. The default theme is named "Default".</para>
///     <para>The <see cref="ThemeManager.Theme"/> property is used to determine the currently active theme.</para>
/// </remarks>
/// <para>
///     <see cref="ThemeManager"/> is a singleton class. It is created when the first <see cref="ThemeManager"/> property
///     is accessed. Accessing <see cref="ThemeManager.Instance"/> is the same as accessing
///     <see cref="ConfigurationManager.Themes"/>.
/// </para>
/// <example>
///     <code>
/// 	"Themes": [
/// 	{
/// 		"Default": {
/// 			"ColorSchemes": [
/// 			{
/// 			"TopLevel": {
/// 			"Normal": {
/// 				"Foreground": "BrightGreen",
/// 				"Background": "Black"
/// 			},
/// 			"Focus": {
/// 			"Foreground": "White",
/// 				"Background": "Cyan"
/// 
/// 			},
/// 			"HotNormal": {
/// 				"Foreground": "Yellow",
/// 				"Background": "Black"
/// 
/// 			},
/// 			"HotFocus": {
/// 				"Foreground": "Blue",
/// 				"Background": "Cyan"
/// 			},
/// 			"Disabled": {
/// 				"Foreground": "DarkGray",
/// 				"Background": "Black"
/// 
/// 			}
/// 		}
/// 	}
/// </code>
/// </example>
public class ThemeManager : IDictionary<string, ThemeScope>
{
    private static string _theme = string.Empty;
    static ThemeManager () { } // Make sure it's truly lazy
    private ThemeManager () { } // Prevent instantiation outside

    /// <summary>Class is a singleton...</summary>
    public static ThemeManager Instance { get; } = new ();

    /// <summary>Gets or sets the currently selected theme. The value is persisted to the "Theme" property.</summary>
    [JsonIgnore]
    public string Theme
    {
        get => SelectedTheme;

        [RequiresUnreferencedCode ("AOT")]
        [RequiresDynamicCode ("AOT")]
        set => SelectedTheme = value;
    }

    /// <summary>Holds the <see cref="ThemeScope"/> definitions.</summary>
    [JsonInclude]
    [JsonConverter (typeof (DictionaryJsonConverter<ThemeScope>))]
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    public static Dictionary<string, ThemeScope>? Themes
    {
        [RequiresUnreferencedCode ("AOT")]
        [RequiresDynamicCode ("AOT")]
        get => Settings? ["Themes"]
                       ?.PropertyValue as
                   Dictionary<string, ThemeScope>; // themes ?? new Dictionary<string, ThemeScope> ();

        [RequiresUnreferencedCode ("AOT")]
        [RequiresDynamicCode ("AOT")]
        set =>

            //if (themes is null || value is null) {
            //	themes = value;
            //} else {
            //	themes = (Dictionary<string, ThemeScope>)DeepMemberwiseCopy (value!, themes!)!;
            //}
            Settings! ["Themes"].PropertyValue = value;
    }

    /// <summary>The currently selected theme. This is the internal version; see <see cref="Theme"/>.</summary>
    [JsonInclude]
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope), OmitClassName = true)]
    [JsonPropertyName ("Theme")]
    internal static string SelectedTheme
    {
        get => _theme;

        [RequiresUnreferencedCode ("Calls Terminal.Gui.ConfigurationManager.Settings")]
        [RequiresDynamicCode ("Calls Terminal.Gui.ConfigurationManager.Settings")]
        set
        {
            string oldTheme = _theme;
            _theme = value;

            if ((oldTheme != _theme
                 || oldTheme != Settings! ["Theme"].PropertyValue as string)
                 && Settings! ["Themes"]?.PropertyValue is Dictionary<string, ThemeScope> themes && themes.ContainsKey (_theme))
            {
                Settings! ["Theme"].PropertyValue = _theme;
                Instance.OnThemeChanged (oldTheme);
            }
        }
    }

    /// <summary>Event fired he selected theme has changed. application.</summary>
    public event EventHandler<ThemeManagerEventArgs>? ThemeChanged;

    [RequiresUnreferencedCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    internal static void GetHardCodedDefaults ()
    {
        //Logging.Trace ("Themes.GetHardCodedDefaults()");
        var theme = new ThemeScope ();
        theme.RetrieveValues ();

        Themes = new Dictionary<string, ThemeScope> (StringComparer.InvariantCultureIgnoreCase)
        {
            { "Default", theme }
        };
        SelectedTheme = "Default";
    }

    /// <summary>Called when the selected theme has changed. Fires the <see cref="ThemeChanged"/> event.</summary>
    internal void OnThemeChanged (string theme)
    {
        //Logging.Trace ($"Themes.OnThemeChanged({theme}) -> {Theme}");
        ThemeChanged?.Invoke (this, new ThemeManagerEventArgs (theme));
    }

    [RequiresUnreferencedCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    internal static void Reset ()
    {
        //Logging.Trace ("Themes.Reset()");
        Colors.Reset ();
        Themes?.Clear ();
        SelectedTheme = string.Empty;
    }

    #region IDictionary

#pragma warning disable 1591
    [UnconditionalSuppressMessage ("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [UnconditionalSuppressMessage ("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public ICollection<string> Keys => ((IDictionary<string, ThemeScope>)Themes!).Keys;

    [UnconditionalSuppressMessage ("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [UnconditionalSuppressMessage ("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public ICollection<ThemeScope> Values => ((IDictionary<string, ThemeScope>)Themes!).Values;

    [UnconditionalSuppressMessage ("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [UnconditionalSuppressMessage ("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public int Count => ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Count;

    [UnconditionalSuppressMessage ("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "<Pending>")]
    [UnconditionalSuppressMessage ("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    public bool IsReadOnly => ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).IsReadOnly;

    public ThemeScope this [string key]
    {
        [RequiresUnreferencedCode ("AOT")]
        [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
        get => ((IDictionary<string, ThemeScope>)Themes!) [key];
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.

        [RequiresUnreferencedCode ("AOT")]
        [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
        set => ((IDictionary<string, ThemeScope>)Themes!) [key] = value;
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public void Add (string key, ThemeScope value) { ((IDictionary<string, ThemeScope>)Themes!).Add (key, value); }
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public bool ContainsKey (string key) { return ((IDictionary<string, ThemeScope>)Themes!).ContainsKey (key); }
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public bool Remove (string key) { return ((IDictionary<string, ThemeScope>)Themes!).Remove (key); }
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public bool TryGetValue (string key, out ThemeScope value) { return ((IDictionary<string, ThemeScope>)Themes!).TryGetValue (key, out value!); }
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public void Add (KeyValuePair<string, ThemeScope> item) { ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Add (item); }
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public void Clear () { ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Clear (); }
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public bool Contains (KeyValuePair<string, ThemeScope> item) { return ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Contains (item); }
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public void CopyTo (KeyValuePair<string, ThemeScope> [] array, int arrayIndex)
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    {
        ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).CopyTo (array, arrayIndex);
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public bool Remove (KeyValuePair<string, ThemeScope> item) { return ((ICollection<KeyValuePair<string, ThemeScope>>)Themes!).Remove (item); }
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public IEnumerator<KeyValuePair<string, ThemeScope>> GetEnumerator () { return ((IEnumerable<KeyValuePair<string, ThemeScope>>)Themes!).GetEnumerator (); }
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.

    [RequiresUnreferencedCode ("Calls Terminal.Gui.ThemeManager.Themes")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ThemeManager.Themes")]
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    IEnumerator IEnumerable.GetEnumerator () { return ((IEnumerable)Themes!).GetEnumerator (); }
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore 1591

    #endregion
}

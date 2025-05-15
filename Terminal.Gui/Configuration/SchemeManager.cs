#nullable enable
using System.Collections;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     Holds the <see cref="Scheme"/>s that define the <see cref="Attribute"/>s that are used by views to render
///     themselves. A Scheme is a mapping from <see cref="VisualRole"/>s (such as <see cref="VisualRole.Focus"/>) to <see cref="Attribute"/>s.
///     A Scheme defines how a `View` should look based on its purpose (e.g. Menu or Dialog).
/// </summary>
public sealed class SchemeManager// : INotifyCollectionChanged, IDictionary<string, Scheme?>
{
    private static readonly object _schemesLock = new ();

    internal static void ResetToHardCodedDefaults ()
    {
        lock (_schemesLock)
        {
            Schemes = GetHardCodedSchemes ();
        }
    }

    /// <summary>
    ///     Gets the hard-coded schemes defined by <see cref="View"/>. These are not loaded from the configuration files,
    ///     but are hard-coded in the source code. Used for unit testing when ConfigurationManager is not initialized.
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, Scheme?>? GetHardCodedSchemes () { return View.GetHardCodedSchemes (); }

    /// <summary>Gets a dictionary of defined <see cref="Scheme"/>s.</summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="Schemes"/> dictionary includes the following keys, by default:
    ///         <list type="table">
    ///             <listheader>
    ///                 <term>Built-in scheme name</term> <description>Description</description>
    ///             </listheader>
    ///             <item>
    ///                 <term>Base</term> <description>The base scheme used for most Views.</description>
    ///             </item>
    ///             <item>
    ///                 <term>TopLevel</term>
    ///                 <description>The application Toplevel scheme; used for the <see cref="Toplevel"/> View.</description>
    ///             </item>
    ///             <item>
    ///                 <term>Dialog</term>
    ///                 <description>
    ///                     The dialog scheme; used for <see cref="Dialog"/>, <see cref="MessageBox"/>, and
    ///                     other views dialog-like views.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <term>Menu</term>
    ///                 <description>
    ///                     The menu scheme; used for <see cref="Menu"/>, <see cref="MenuBar"/>, and
    ///                     <see cref="StatusBar"/>.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <term>Error</term>
    ///                 <description>
    ///                     The scheme for showing errors, such as in
    ///                     <see cref="MessageBox.ErrorQuery(string, string, string[])"/>.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>Changing the values of an entry in this dictionary will affect all views that use the scheme.</para>
    ///     <para>
    ///         <see cref="ConfigurationManager"/> can be used to override the default values for these schemes and add
    ///         additional schemes. See <see cref="ThemeManager.Themes"/>.
    ///     </para>
    /// </remarks>
    [ConfigurationProperty (Scope = typeof (ThemeScope), OmitClassName = true)]
    [JsonConverter (typeof (DictionaryJsonConverter<Scheme?>))]
    [UsedImplicitly]
    public static Dictionary<string, Scheme?>? Schemes
    {
        get
        {
            if (!ConfigurationManager.IsInitialized ())
            {
                // We're being called from the module initializer.
                // Hard coded default value
                return GetHardCodedSchemes ();
            }

            return GetCurrentSchemes ();
        }

        private set
        {
            if (!ConfigurationManager.IsInitialized ())
            {
                throw new InvalidOperationException ("Schemes cannot be set before ConfigurationManager is initialized.");
            }

            Debug.Assert(value is {});

            // Update the backing store
            ThemeManager.Themes! [ThemeManager.DEFAULT_THEME_NAME] ["Schemes"].PropertyValue = value;

            //Instance.OnThemeChanged (prevousValue);
        }
    }

    /// <summary>
    ///     Gets the <see cref="Scheme"/> for the specified <see cref="Gui.Schemes"/>.
    /// </summary>
    /// <param name="schemeName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Scheme GetScheme (Schemes schemeName)
    {
        // Convert schemeName to string via Enum api
        string? schemeNameString = SchemesToSchemeName (schemeName);
        if (schemeNameString is null)
        {
            throw new ArgumentException ($"Invalid scheme name: {schemeName}");
        }

        return Schemes! [schemeNameString]!;
    }

    /// <summary>
    ///     Gets the <see cref="Scheme"/> for the specified string.
    /// </summary>
    /// <param name="schemeName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Scheme GetScheme (string schemeName)
    {
        return Schemes! [schemeName]!;
    }

    /// <summary>
    ///     Gets the name of the specified <see cref="Schemes"/>.
    /// </summary>
    /// <param name="schemeName"></param>
    /// <returns></returns>
    public static string? SchemesToSchemeName (Schemes schemeName)
    {
        return Enum.GetName (typeof (Schemes), schemeName);
    }

    /// <summary>
    ///     Gets the <see cref="Schemes"/> enum value given the name of the scheme.
    /// </summary>
    /// <param name="schemeName"><see langword="null"/> if the schemeName is not a built-in Scheme name.</param>
    /// <returns></returns>
    public static string? SchemeNameToSchemes (string schemeName)
    {
        if (Enum.TryParse (typeof (Schemes), schemeName, out object? value))
        {
            return value?.ToString ();
        }
        return null;
    }

    /// <summary>
    ///     Convenience method to get the schemes from the selected theme loaded from configuration.
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, Scheme?> GetCurrentSchemes ()
    {
        Debug.Assert (ConfigurationManager.IsInitialized ());
        Dictionary<string, Scheme?>? schemes = ThemeManager.GetCurrentTheme () ["Schemes"].PropertyValue as Dictionary<string, Scheme?>;

        return schemes!;
    }

    /// <summary>
    ///     Convenience method to get the names of the schemes.
    /// </summary>
    /// <returns></returns>
    public static ImmutableList<string> GetSchemeNames ()
    {
        lock (_schemesLock)
        {
            if (Schemes is { })
            {
                return Schemes.Keys.ToImmutableList ();
            }
        }

        throw new InvalidOperationException ("Schemes is not set.");
    }
}

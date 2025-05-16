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
            SetSchemes (GetHardCodedSchemes ());
        }
    }

    /// <summary>
    ///     Gets the hard-coded schemes defined by <see cref="View"/>. These are not loaded from the configuration files,
    ///     but are hard-coded in the source code. Used for unit testing when ConfigurationManager is not initialized.
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, Scheme?>? GetHardCodedSchemes () { return View.GetHardCodedSchemes (); }

    /// <summary>
    ///     Use <see cref="AddScheme"/>, <see cref="GetScheme(Terminal.Gui.Schemes)"/>, <see cref="GetSchemeNames"/>, <see cref="GetSchemesForCurrentTheme"/>, etc... instead.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope), OmitClassName = true)]
    [JsonConverter (typeof (DictionaryJsonConverter<Scheme?>))]
    [UsedImplicitly]
    public static Dictionary<string, Scheme?>? Schemes
    {
        get => GetSchemes ();
        private set => SetSchemes (value);
    }

    /// <summary>INTERNAL: Gets the dictionary of defined <see cref="Scheme"/>s. The get method for <see cref="Schemes"/>.</summary>
    internal static Dictionary<string, Scheme?>? GetSchemes ()
    {
        if (!ConfigurationManager.IsInitialized ())
        {
            // We're being called from the module initializer.
            // Hard coded default value
            return GetHardCodedSchemes ();
        }

        return GetSchemesForCurrentTheme ();
    }

    /// <summary>INTERNAL: Gets the dictionary of defined <see cref="Scheme"/>s. The set method for <see cref="Schemes"/>.</summary>
    private static void SetSchemes (Dictionary<string, Scheme?>? value)
    {
        if (!ConfigurationManager.IsInitialized ())
        {
            throw new InvalidOperationException ("Schemes cannot be set before ConfigurationManager is initialized.");
        }

        Debug.Assert (value is { });

        // Update the backing store
        ThemeManager.Themes! [ThemeManager.DEFAULT_THEME_NAME] ["Schemes"].PropertyValue = value;

        //Instance.OnThemeChanged (prevousValue);
    }

    /// <summary>
    ///     Adds a new <see cref="Scheme"/> to <see cref="SchemeManager"/>.
    /// </summary>
    /// <param name="schemeName">The name of the Scheme. This must be unique.</param>
    /// <param name="scheme"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static void AddScheme (string schemeName, Scheme scheme)
    {
        if (GetSchemes () is null)
        {
            throw new InvalidOperationException ("Schemes is not set.");
        }

        if (!GetSchemes ()!.TryAdd (schemeName, scheme))
        {
            throw new ArgumentException ($"Scheme with name {schemeName} already exists.");
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

        return GetSchemesForCurrentTheme ()! [schemeNameString]!;
    }

    /// <summary>
    ///     Gets the <see cref="Scheme"/> for the specified string.
    /// </summary>
    /// <param name="schemeName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Scheme GetScheme (string schemeName)
    {
        return GetSchemesForCurrentTheme ()! [schemeName]!;
    }

    /// <summary>
    ///     Gets the name of the specified <see cref="Schemes"/>. Will throw an exception if <paramref name="schemeName"/>
    ///     is not a built-in Scheme.
    /// </summary>
    /// <param name="schemeName"></param>
    /// <returns>The name of scheme.</returns>
    public static string? SchemesToSchemeName (Schemes schemeName)
    {
        return Enum.GetName (typeof (Schemes), schemeName);
    }

    /// <summary>
    ///     Converts a string to a <see cref="Schemes"/> enum value.
    /// </summary>
    /// <param name="schemeName"><see langword="null"/> if the schemeName is not a built-in Scheme name.</param>
    /// <returns><see langword="null"/> if <paramref name="schemeName"/> is not the name of a built-in Scheme.</returns>
    public static string? SchemeNameToSchemes (string schemeName)
    {
        if (Enum.TryParse (typeof (Schemes), schemeName, out object? value))
        {
            return value?.ToString ();
        }
        return null;
    }

    /// <summary>
    ///     Get the dictionary schemes from the selected theme loaded from configuration.
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, Scheme?> GetSchemesForCurrentTheme ()
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
            if (GetSchemes () is { })
            {
                return GetSchemes ()!.Keys.ToImmutableList ();
            }
        }

        throw new InvalidOperationException ("Schemes is not set.");
    }
}

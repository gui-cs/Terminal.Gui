#nullable enable
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Terminal.Gui.Configuration;

/// <summary>
///     Holds the <see cref="Drawing.Scheme"/>s that define the <see cref="System.Attribute"/>s that are used by views to render
///     themselves. A Scheme is a mapping from <see cref="Drawing.VisualRole"/>s (such as <see cref="Drawing.VisualRole.Focus"/>) to <see cref="System.Attribute"/>s.
///     A Scheme defines how a `View` should look based on its purpose (e.g. Menu or Dialog).
/// </summary>
public sealed class SchemeManager// : INotifyCollectionChanged, IDictionary<string, Scheme?>
{
#pragma warning disable IDE1006 // Naming Styles
    private static readonly object _schemesLock = new ();
#pragma warning restore IDE1006 // Naming Styles

    /// <summary>
    ///     INTERNAL: Gets the hard-coded schemes defined by <see cref="View"/>. These are not loaded from the configuration files,
    ///     but are hard-coded in the source code. Used for unit testing when ConfigurationManager is not initialized.
    /// </summary>
    /// <returns></returns>
    internal static ImmutableSortedDictionary<string, Scheme?>? GetHardCodedSchemes () { return Scheme.GetHardCodedSchemes ()!; }

    /// <summary>
    ///     Use <see cref="AddScheme"/>, <see cref="GetScheme(Drawing.Schemes)"/>, <see cref="GetSchemeNames"/>, <see cref="GetSchemesForCurrentTheme"/>, etc... instead.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope), OmitClassName = true)]
    [JsonConverter (typeof (DictionaryJsonConverter<Scheme?>))]
    [UsedImplicitly]
    public static Dictionary<string, Scheme?>? Schemes
    {
        get => GetSchemes ();
        [RequiresUnreferencedCode ("Calls Terminal.Gui.SchemeManager.SetSchemes(Dictionary<String, Scheme>)")]
        [RequiresDynamicCode ("Calls Terminal.Gui.SchemeManager.SetSchemes(Dictionary<String, Scheme>)")]
        private set => SetSchemes (value);
    }

    /// <summary>INTERNAL: Gets the dictionary of defined <see cref="Scheme"/>s. The get method for <see cref="Schemes"/>.</summary>
    internal static Dictionary<string, Scheme?> GetSchemes ()
    {
        if (!ConfigurationManager.IsInitialized ())
        {
            // We're being called from the module initializer.
            // Hard coded default value
            return GetHardCodedSchemes ()!.ToDictionary (StringComparer.InvariantCultureIgnoreCase);
        }

        return GetSchemesForCurrentTheme ();
    }

    /// <summary>INTERNAL: The set method for <see cref="Schemes"/>.</summary>
    [RequiresUnreferencedCode ("Calls Terminal.Gui.ConfigProperty.UpdateFrom(Object)")]
    [RequiresDynamicCode ("Calls Terminal.Gui.ConfigProperty.UpdateFrom(Object)")]
    private static void SetSchemes (Dictionary<string, Scheme?>? value)
    {
        lock (_schemesLock)
        {
            if (!ConfigurationManager.IsInitialized ())
            {
                throw new InvalidOperationException ("Schemes cannot be set before ConfigurationManager is initialized.");
            }

            Debug.Assert (value is { });

            // Update the backing store
            ThemeManager.GetCurrentTheme () ["Schemes"].UpdateFrom (value);
        }

        //Instance.OnThemeChanged (prevousValue);
    }

    /// <summary>
    ///     Adds a new <see cref="Scheme"/> to <see cref="SchemeManager"/>. If the Scheme has already been added,
    ///     it will be updated to <paramref name="scheme"/>.
    /// </summary>
    /// <param name="schemeName">The name of the Scheme. This must be unique.</param>
    /// <param name="scheme"></param>
    /// <returns></returns>
    public static void AddScheme (string schemeName, Scheme scheme)
    {
        if (!GetSchemes ()!.TryAdd (schemeName, scheme))
        {
            GetSchemes () [schemeName] = scheme;
        }
    }

    /// <summary>
    ///     Removes a Scheme from <see cref="SchemeManager"/>.
    /// </summary>
    /// <param name="schemeName"></param>
    /// <exception cref="InvalidOperationException">If the scheme is a built-in Scheme or was not previously added.</exception>
    public static void RemoveScheme (string schemeName)
    {
        if (SchemeNameToSchemes (schemeName) is { })
        {
            throw new InvalidOperationException ($@"{schemeName}: Cannot remove a built-in Scheme.");
        }

        if (!GetSchemes ().TryGetValue (schemeName, out _))
        {
            throw new InvalidOperationException ($@"{schemeName}: Does not exist in Schemes.");
        }

        GetSchemes ().Remove (schemeName);
    }

    /// <summary>
    ///     Gets the <see cref="Scheme"/> for the specified <see cref="Drawing.Schemes"/>.
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
        lock (_schemesLock)
        {
            if (!ConfigurationManager.IsInitialized ())
            {
                throw new InvalidOperationException ("CM Must be Initialized");
            }

            if (ThemeManager.GetCurrentTheme () ["Schemes"].PropertyValue is not Dictionary<string, Scheme?> schemes)
            {
                // Most likely because "Schemes": was left out of the config
                throw new InvalidOperationException ("Current Theme does not have a Scheme.");
            }

            return schemes!;
        }
    }

    /// <summary>
    ///     Convenience method to get the names of the schemes.
    /// </summary>
    /// <returns></returns>
    public static ImmutableList<string> GetSchemeNames ()
    {
        lock (_schemesLock)
        {
            return GetSchemes ()!.Keys.ToImmutableList ();
        }
    }
}

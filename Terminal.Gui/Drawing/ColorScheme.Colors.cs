#nullable enable
using System.Collections;
using System.Collections.Specialized;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     Holds the <see cref="ColorScheme"/>s that define the <see cref="Attribute"/>s that are used by views to render
///     themselves.
/// </summary>
public sealed class Colors : INotifyCollectionChanged, IDictionary<string, ColorScheme?>
{
    static Colors ()
    {
        ColorSchemes = new (5, StringComparer.InvariantCultureIgnoreCase);
        Reset ();
    }

    /// <summary>Gets a dictionary of defined <see cref="ColorScheme"/> objects.</summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="ColorSchemes"/> dictionary includes the following keys, by default:
    ///         <list type="table">
    ///             <listheader>
    ///                 <term>Built-in Color Scheme</term> <description>Description</description>
    ///             </listheader>
    ///             <item>
    ///                 <term>Base</term> <description>The base color scheme used for most Views.</description>
    ///             </item>
    ///             <item>
    ///                 <term>TopLevel</term>
    ///                 <description>The application Toplevel color scheme; used for the <see cref="Toplevel"/> View.</description>
    ///             </item>
    ///             <item>
    ///                 <term>Dialog</term>
    ///                 <description>
    ///                     The dialog color scheme; used for <see cref="Dialog"/>, <see cref="MessageBox"/>, and
    ///                     other views dialog-like views.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <term>Menu</term>
    ///                 <description>
    ///                     The menu color scheme; used for <see cref="MenuBar"/>, <see cref="ContextMenu"/>, and
    ///                     <see cref="StatusBar"/>.
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <term>Error</term>
    ///                 <description>
    ///                     The color scheme for showing errors, such as in
    ///                     <see cref="MessageBox.ErrorQuery(string, string, string[])"/>.
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>Changing the values of an entry in this dictionary will affect all views that use the scheme.</para>
    ///     <para>
    ///         <see cref="ConfigurationManager"/> can be used to override the default values for these schemes and add
    ///         additional schemes. See <see cref="ConfigurationManager.Themes"/>.
    ///     </para>
    /// </remarks>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope), OmitClassName = true)]
    [JsonConverter (typeof (DictionaryJsonConverter<ColorScheme?>))]
    [UsedImplicitly]
    public static Dictionary<string, ColorScheme?> ColorSchemes { get; private set; }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, ColorScheme?>> GetEnumerator () { return ColorSchemes.GetEnumerator (); }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator () { return GetEnumerator (); }

    /// <inheritdoc/>
    public void Add (KeyValuePair<string, ColorScheme?> item)
    {
        ColorSchemes.Add (item.Key, item.Value);
        CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Add, item));
    }

    /// <inheritdoc/>
    public void Clear ()
    {
        ColorSchemes.Clear ();
        CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Reset));
    }

    /// <inheritdoc/>
    public bool Contains (KeyValuePair<string, ColorScheme?> item) { return ColorSchemes.Contains (item); }

    /// <inheritdoc/>
    public void CopyTo (KeyValuePair<string, ColorScheme?> [] array, int arrayIndex) { ((ICollection)ColorSchemes).CopyTo (array, arrayIndex); }

    /// <inheritdoc/>
    public bool Remove (KeyValuePair<string, ColorScheme?> item)
    {
        if (ColorSchemes.Remove (item.Key))
        {
            CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Remove, item));

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public int Count => ColorSchemes.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public void Add (string key, ColorScheme? value) { Add (new (key, value)); }

    /// <inheritdoc/>
    public bool ContainsKey (string key) { return ColorSchemes.ContainsKey (key); }

    /// <inheritdoc/>
    public bool Remove (string key)
    {
        if (ColorSchemes.Remove (key))
        {
            CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Remove, key));

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool TryGetValue (string key, out ColorScheme? value) { return ColorSchemes.TryGetValue (key, out value); }

    /// <inheritdoc/>
    public ColorScheme? this [string key]
    {
        get => ColorSchemes [key];
        set
        {
            if (ColorSchemes.TryAdd (key, value))
            {
                CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Add, new KeyValuePair<string, ColorScheme?> (key, value)));
            }
            else
            {
                ColorScheme? oldValue = ColorSchemes [key];
                ColorSchemes [key] = value;
                CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Replace, value, oldValue));
            }
        }
    }

    /// <inheritdoc/>
    public ICollection<string> Keys => ColorSchemes.Keys;

    /// <inheritdoc/>
    public ICollection<ColorScheme?> Values => ColorSchemes.Values;

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>Resets the <see cref="ColorSchemes"/> dictionary to the default values.</summary>
    public static Dictionary<string, ColorScheme?> Reset ()
    {
        ColorSchemes.Clear ();
        ColorSchemes.Add ("TopLevel", new ());
        ColorSchemes.Add ("Base", new ());
        ColorSchemes.Add ("Dialog", new ());
        ColorSchemes.Add ("Menu", new ());
        ColorSchemes.Add ("Error", new ());

        return ColorSchemes;
    }
}

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
    private static readonly object _lock = new object ();

    static Colors ()
    {
        ColorSchemes = new Dictionary<string, ColorScheme?> (5, StringComparer.InvariantCultureIgnoreCase);
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

    /// <summary>
    ///     Raised when the collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc />
    public ColorScheme? this [string key]
    {
        get
        {
            lock (_lock)
            {
                return ColorSchemes [key];
            }
        }
        set
        {
            lock (_lock)
            {
                if (ColorSchemes.ContainsKey (key))
                {
                    ColorScheme? oldValue = ColorSchemes [key];
                    ColorSchemes [key] = value;
                    CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Replace, value, oldValue));
                }
                else
                {
                    ColorSchemes.Add (key, value);
                    CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, new KeyValuePair<string, ColorScheme?> (key, value)));
                }
            }
        }
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return ColorSchemes.Count;
            }
        }
    }

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public ICollection<string> Keys
    {
        get
        {
            lock (_lock)
            {
                return new List<string> (ColorSchemes.Keys);
            }
        }
    }

    /// <inheritdoc />
    public ICollection<ColorScheme?> Values
    {
        get
        {
            lock (_lock)
            {
                return new List<ColorScheme?> (ColorSchemes.Values);
            }
        }
    }

    /// <inheritdoc />
    public void Add (KeyValuePair<string, ColorScheme?> item)
    {
        lock (_lock)
        {
            ColorSchemes.Add (item.Key, item.Value);
            CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, item));
        }
    }

    /// <inheritdoc />
    public void Add (string key, ColorScheme? value)
    {
        Add (new KeyValuePair<string, ColorScheme?> (key, value));
    }

    /// <inheritdoc />
    public void Clear ()
    {
        lock (_lock)
        {
            ColorSchemes.Clear ();
            CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
        }
    }

    /// <inheritdoc />
    public bool Contains (KeyValuePair<string, ColorScheme?> item)
    {
        lock (_lock)
        {
            return ColorSchemes.Contains (item);
        }
    }

    /// <inheritdoc />
    public bool ContainsKey (string key)
    {
        lock (_lock)
        {
            return ColorSchemes.ContainsKey (key);
        }
    }

    public void CopyTo (KeyValuePair<string, ColorScheme?> [] array, int arrayIndex)
    {
        lock (_lock)
        {
            ((ICollection)ColorSchemes).CopyTo (array, arrayIndex);
        }
    }

    public IEnumerator<KeyValuePair<string, ColorScheme?>> GetEnumerator ()
    {
        lock (_lock)
        {
            return new List<KeyValuePair<string, ColorScheme?>> (ColorSchemes).GetEnumerator ();
        }
    }

    IEnumerator IEnumerable.GetEnumerator ()
    {
        return GetEnumerator ();
    }

    public bool Remove (KeyValuePair<string, ColorScheme?> item)
    {
        lock (_lock)
        {
            if (ColorSchemes.Remove (item.Key))
            {
                CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, item));
                return true;
            }
            return false;
        }
    }

    public bool Remove (string key)
    {
        lock (_lock)
        {
            if (ColorSchemes.Remove (key))
            {
                CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, key));
                return true;
            }
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryGetValue (string key, out ColorScheme? value)
    {
        lock (_lock)
        {
            return ColorSchemes.TryGetValue (key, out value);
        }
    }


    /// <summary>
    ///     Resets the <see cref="ColorSchemes"/> dictionary to its default values.
    /// </summary>
    /// <returns></returns>
    public static Dictionary<string, ColorScheme?> Reset ()
    {
        lock (_lock)
        {
            ColorSchemes.Clear ();
            ColorSchemes.Add ("TopLevel", new ColorScheme ());
            ColorSchemes.Add ("Base", new ColorScheme ());
            ColorSchemes.Add ("Dialog", new ColorScheme ());
            ColorSchemes.Add ("Menu", new ColorScheme ());
            ColorSchemes.Add ("Error", new ColorScheme ());
            return ColorSchemes;
        }
    }
}
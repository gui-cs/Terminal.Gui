#nullable enable
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     Holds the <see cref="Scheme"/>s that define the <see cref="Attribute"/>s that are used by views to render
///     themselves.
/// </summary>

public sealed class SchemeManager : INotifyCollectionChanged, IDictionary<string, Scheme?>
{
    private readonly object _lock = new object ();

    /// <summary>
    /// 
    /// </summary>
    public SchemeManager ()
    {
        Reset ();
    }

    internal void Reset ()
    {
        Schemes = View.GetHardCodedSchemes ();
    }

    /// <summary>Gets a dictionary of defined <see cref="Scheme"/> objects.</summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="Schemes"/> dictionary includes the following keys, by default:
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
    ///                     The menu color scheme; used for <see cref="Menu"/>, <see cref="MenuBar"/>, and
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
    ///         additional schemes. See <see cref="ConfigurationManager.ThemeManager"/>.
    ///     </para>
    /// </remarks>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope), OmitClassName = true)]
    [JsonConverter (typeof (DictionaryJsonConverter<Scheme?>))]
    [UsedImplicitly]
    public static Dictionary<string, Scheme?>? Schemes { get; private set; }

    /// <summary>
    ///     Raised when the collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc />
    public Scheme? this [string key]
    {
        get
        {
            lock (_lock)
            {
                return Schemes? [key];
            }
        }
        set
        {
            lock (_lock)
            {
                if (Schemes is { } && Schemes.TryGetValue (key, out _))
                {
                    Scheme? oldValue = Schemes [key];
                    Schemes [key] = value;
                    CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Replace, value, oldValue));
                }
                else
                {
                    Schemes?.Add (key, value);
                    CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, new KeyValuePair<string, Scheme?> (key, value)));
                }
            }
        }
    }

    /// <summary>
    ///     Helper to get the default schemes from the default theme loaded from configuration.
    /// </summary>
    /// <returns></returns>
    [RequiresDynamicCode ("AOT")]

    public static Dictionary<string, Scheme?>? GetDefaultSchemes ()
    {
        if (!IsInitialized ())
        {
            // If CM has not been initialized, we return the values that are in the current static members.
            ThemeScope themes = new ThemeScope ();
            themes.RetrieveValues ();

            // If CM has not been initialized, ThemeScope gets loaded with the default values.
            return themes ["Schemes"].PropertyValue as Dictionary<string, Scheme?>;
        }

        Dictionary<string, Scheme?>? schemes = [];
        if (CM.ThemeManager is { })
        {
            schemes = CM.ThemeManager ["Default"] ["Schemes"].PropertyValue as Dictionary<string, Scheme?>;
        }

        return schemes;
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return Schemes!.Count;
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
                return new List<string> (Schemes!.Keys);
            }
        }
    }

    /// <inheritdoc />
    public ICollection<Scheme?> Values
    {
        get
        {
            lock (_lock)
            {
                return new List<Scheme?> (Schemes!.Values);
            }
        }
    }

    /// <inheritdoc />
    public void Add (KeyValuePair<string, Scheme?> item)
    {
        lock (_lock)
        {
            Schemes?.Add (item.Key, item.Value);
            CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Add, item));
        }
    }

    /// <inheritdoc />
    public void Add (string key, Scheme? value)
    {
        Add (new KeyValuePair<string, Scheme?> (key, value));
    }

    /// <inheritdoc />
    public void Clear ()
    {
        lock (_lock)
        {
            Schemes?.Clear ();
            CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
        }
    }

    /// <inheritdoc />
    public bool Contains (KeyValuePair<string, Scheme?> item)
    {
        lock (_lock)
        {
            return Schemes is { } && Schemes.Contains (item);
        }
    }

    /// <inheritdoc />
    public bool ContainsKey (string key)
    {
        lock (_lock)
        {
            return Schemes is { } && Schemes.ContainsKey(key);
        }
    }

    /// <inheritdoc />
    public void CopyTo (KeyValuePair<string, Scheme?> [] array, int arrayIndex)
    {
        lock (_lock)
        {
            if (Schemes is { })
            {
                ((ICollection)Schemes).CopyTo (array, arrayIndex);
            }
        }
    }

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, Scheme?>> GetEnumerator ()
    {
        lock (_lock)
        {
            if (Schemes is { })
            {
                return new List<KeyValuePair<string, Scheme?>> (Schemes).GetEnumerator ();
            }
        }

        return null!;
    }

    IEnumerator IEnumerable.GetEnumerator ()
    {
        return GetEnumerator ();
    }

    /// <inheritdoc />
    public bool Remove (KeyValuePair<string, Scheme?> item)
    {
        lock (_lock)
        {
            if (Schemes is { } && Schemes.Remove (item.Key))
            {
                CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, item));
                return true;
            }
            return false;
        }
    }

    /// <inheritdoc />
    public bool Remove (string key)
    {
        lock (_lock)
        {
            if (Schemes is { } && Schemes.Remove (key))
            {
                CollectionChanged?.Invoke (this, new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Remove, key));
                return true;
            }
            return false;
        }
    }

    /// <inheritdoc />
    public bool TryGetValue (string key, out Scheme? value)
    {
        lock (_lock)
        {
            return Schemes!.TryGetValue (key, out value);
        }
    }
}
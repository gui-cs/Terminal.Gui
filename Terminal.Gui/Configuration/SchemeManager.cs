#nullable enable
using System.Collections;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     Holds the <see cref="Scheme"/>s that define the <see cref="Attribute"/>s that are used by views to render
///     themselves.
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

    /// <summary>Gets a dictionary of defined <see cref="Scheme"/> objects.</summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="Schemes"/> dictionary includes the following keys, by default:
    ///         <list type="table">
    ///             <listheader>
    ///                 <term>Built-in scheme</term> <description>Description</description>
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

            Dictionary<string, Scheme?>? schemes =
                ThemeManager.Themes? [ThemeManager.DEFAULT_THEME_NAME] ["Schemes"].PropertyValue as Dictionary<string, Scheme?>;

            // Update the backing store
            ThemeManager.Themes! [ThemeManager.DEFAULT_THEME_NAME] ["Schemes"].PropertyValue = value;

            //Instance.OnThemeChanged (prevousValue);
        }
    }

    /// <summary>
    ///     Raised when the collection changes.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public Scheme? this [string key]
    {
        get
        {
            lock (_schemesLock)
            {
                return Schemes? [key];
            }
        }
        set
        {
            lock (_schemesLock)
            {
                if (Schemes is { } && Schemes.TryGetValue (key, out _))
                {
                    Scheme? oldValue = Schemes [key];
                    Schemes [key] = value;
                    CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Replace, value, oldValue));
                }
                else
                {
                    Schemes?.Add (key, value);
                    CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Add, new KeyValuePair<string, Scheme?> (key, value)));
                }
            }
        }
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

    ///// <inheritdoc/>
    //public int Count
    //{
    //    get
    //    {
    //        lock (_schemesLock)
    //        {
    //            return Schemes!.Count;
    //        }
    //    }
    //}

    ///// <inheritdoc/>
    //public bool IsReadOnly => false;

    ///// <inheritdoc/>
    //public ICollection<string> Keys
    //{
    //    get
    //    {
    //        lock (_schemesLock)
    //        {
    //            return new List<string> (Schemes!.Keys);
    //        }
    //    }
    //}

    ///// <inheritdoc/>
    //public ICollection<Scheme?> Values
    //{
    //    get
    //    {
    //        lock (_schemesLock)
    //        {
    //            return new List<Scheme?> (Schemes!.Values);
    //        }
    //    }
    //}

    ///// <inheritdoc/>
    //public void Add (KeyValuePair<string, Scheme?> item)
    //{
    //    lock (_schemesLock)
    //    {
    //        Schemes?.Add (item.Key, item.Value);
    //        CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Add, item));
    //    }
    //}

    ///// <inheritdoc/>
    //public void Add (string key, Scheme? value) { Add (new (key, value)); }

    ///// <inheritdoc/>
    //public void Clear ()
    //{
    //    lock (_schemesLock)
    //    {
    //        Schemes?.Clear ();
    //        CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Reset));
    //    }
    //}

    ///// <inheritdoc/>
    //public bool Contains (KeyValuePair<string, Scheme?> item)
    //{
    //    lock (_schemesLock)
    //    {
    //        return Schemes is { } && Schemes.Contains (item);
    //    }
    //}

    ///// <inheritdoc/>
    //public bool ContainsKey (string key)
    //{
    //    lock (_schemesLock)
    //    {
    //        return Schemes is { } && Schemes.ContainsKey (key);
    //    }
    //}

    ///// <inheritdoc/>
    //public void CopyTo (KeyValuePair<string, Scheme?> [] array, int arrayIndex)
    //{
    //    lock (_schemesLock)
    //    {
    //        if (Schemes is { })
    //        {
    //            ((ICollection)Schemes).CopyTo (array, arrayIndex);
    //        }
    //    }
    //}

    ///// <inheritdoc/>
    //public IEnumerator<KeyValuePair<string, Scheme?>> GetEnumerator ()
    //{
    //    lock (_schemesLock)
    //    {
    //        if (Schemes is { })
    //        {
    //            return new List<KeyValuePair<string, Scheme?>> (Schemes).GetEnumerator ();
    //        }
    //    }

    //    return null!;
    //}

    //IEnumerator IEnumerable.GetEnumerator () { return GetEnumerator (); }

    ///// <inheritdoc/>
    //public bool Remove (KeyValuePair<string, Scheme?> item)
    //{
    //    lock (_schemesLock)
    //    {
    //        if (Schemes is { } && Schemes.Remove (item.Key))
    //        {
    //            CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Remove, item));

    //            return true;
    //        }

    //        return false;
    //    }
    //}

    ///// <inheritdoc/>
    //public bool Remove (string key)
    //{
    //    lock (_schemesLock)
    //    {
    //        if (Schemes is { } && Schemes.Remove (key))
    //        {
    //            CollectionChanged?.Invoke (this, new (NotifyCollectionChangedAction.Remove, key));

    //            return true;
    //        }

    //        return false;
    //    }
    //}

    ///// <inheritdoc/>
    //public bool TryGetValue (string key, out Scheme? value)
    //{
    //    lock (_schemesLock)
    //    {
    //        return Schemes!.TryGetValue (key, out value);
    //    }
    //}
}

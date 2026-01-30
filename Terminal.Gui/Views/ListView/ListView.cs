using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides a scrollable list of data where each item can be activated to perform an
///     action.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="ListView"/> displays lists of data and allows the user to scroll through the data. Items in
///         the can be activated firing an event (with the ENTER key or a mouse double-click). If the
///         <see cref="ShowMarks"/> property is true, elements of the list can be marked by the user.
///     </para>
///     <para>
///         By default <see cref="ListView"/> uses <see cref="object.ToString"/> to render the items of any
///         <see cref="ObservableCollection{T}"/> object (e.g. arrays, <see cref="List{T}"/>, and other collections).
///         Alternatively, an
///         object that implements <see cref="IListDataSource"/> can be provided giving full control of what is rendered.
///     </para>
///     <para>
///         <see cref="ListView"/> can display any object that implements the <see cref="IList"/> interface.
///         <see cref="string "/> values are converted into <see cref="string "/> values before rendering, and other values
///         are converted into <see cref="string "/> by calling <see cref="object.ToString"/> and then converting to
///         <see cref="string "/> .
///     </para>
///     <para>
///         To change the contents of the ListView, set the <see cref="Source"/> property (when providing custom
///         rendering via <see cref="IListDataSource"/>) or call <see cref="SetSource{T}"/> an <see cref="IList"/> is being
///         used.
///     </para>
///     <para>
///         When <see cref="ShowMarks"/> is set to true the rendering will prefix the rendered items with [x] or [ ]
///         and bind the SPACE key to toggle the selection. To implement a different marking style set
///         <see cref="ShowMarks"/> to false and implement custom rendering.
///     </para>
///     <para>
///         Searching the ListView with the keyboard is supported. Users type the first characters of an item, and the
///         first item that starts with what the user types will be selected.
///     </para>
/// </remarks>
public partial class ListView : View, IDesignable, IValue<int?>
{
    /// <summary>
    ///     Initializes a new instance of <see cref="ListView"/>. Set the <see cref="Source"/> property to display
    ///     something.
    /// </summary>
    public ListView ()
    {
        CanFocus = true;

        SetupBindingsAndCommands ();
    }

    #region IListDataSource

    /// <summary>
    ///     Use <see cref="IListDataSource.MaxItemLength"/> to get the maximum item length. Note that the behavior is dependent
    ///     on the data source.
    /// </summary>
    public int MaxItemLength => Source?.MaxItemLength ?? 0;

    /// <summary>Sets the source of the <see cref="ListView"/> to an <see cref="IList"/>.</summary>
    /// <value>An object implementing the IList interface.</value>
    /// <remarks>
    ///     Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custom
    ///     rendering.
    /// </remarks>
    public void SetSource<T> (ObservableCollection<T>? source)
    {
        if (source is null && Source is not ListWrapper<T>)
        {
            Source = null;
        }
        else
        {
            Source = new ListWrapper<T> (source);
        }
    }

    /// <summary>Sets the source to an <see cref="IList"/> value asynchronously.</summary>
    /// <value>An item implementing the IList interface.</value>
    /// <remarks>
    ///     Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custom
    ///     rendering.
    /// </remarks>
    public Task SetSourceAsync<T> (ObservableCollection<T>? source) =>
        Task.Factory.StartNew (() =>
                               {
                                   if (source is null && Source is not ListWrapper<T>)
                                   {
                                       Source = null;
                                   }
                                   else
                                   {
                                       Source = new ListWrapper<T> (source);
                                   }

                                   return source;
                               },
                               CancellationToken.None,
                               TaskCreationOptions.DenyChildAttach,
                               TaskScheduler.Default);

    /// <summary>Gets or sets the <see cref="IListDataSource"/> backing this <see cref="ListView"/>, enabling custom rendering.</summary>
    /// <value>The source.</value>
    /// <remarks>Use <see cref="SetSource{T}"/> to set a new <see cref="IList"/> source.</remarks>
    public IListDataSource? Source
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field?.Dispose ();
            field = value;

            if (field is { })
            {
                field.CollectionChanged += SourceOnCollectionChanged;
                SetContentSize (new Size (EffectiveMaxItemLength, field?.Count ?? Viewport.Height));
                KeystrokeNavigator.Collection = field?.ToList ();
            }

            SelectedItem = null;
            _lastSelectedItem = null;
            OnSourceChanged ();
            SourceChanged?.Invoke (this, EventArgs.Empty);
            SetNeedsDraw ();
        }
    }

    // TODO: Make these match the pattern in IListDataSource where it's just a property.
    /// <summary>
    ///     Allow suspending the <see cref="CollectionChanged"/> event from being invoked,
    /// </summary>
    public void SuspendCollectionChangedEvent () => Source?.SuspendCollectionChangedEvent = true;

    /// <summary>
    ///     Allow resume the <see cref="CollectionChanged"/> event from being invoked,
    /// </summary>
    public void ResumeSuspendCollectionChangedEvent () => Source?.SuspendCollectionChangedEvent = false;

    // TODO: Make CollectionChange follow the CWP
    private void SourceOnCollectionChanged (object? sender, NotifyCollectionChangedEventArgs e)
    {
        SetContentSize (new Size (EffectiveMaxItemLength, Source?.Count ?? Viewport.Height));

        if (Source is { Count: > 0 } && SelectedItem.HasValue && SelectedItem > Source.Count - 1)
        {
            SelectedItem = Source.Count - 1;
        }

        SetNeedsDraw ();
        OnCollectionChanged (e);
    }

    /// <summary>
    ///     Called when the <see cref="Source"/> property is changed. Invokes the <see cref="SourceChanged"/> event.
    /// </summary>
    protected virtual void OnSourceChanged () { }

    /// <summary>
    ///     Event to raise when the <see cref="Source"/> data is changed.
    /// </summary>
    public event EventHandler? SourceChanged;

    /// <summary>
    ///     Call the event to raises the <see cref="CollectionChanged"/>.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnCollectionChanged (NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke (this, e);

    /// <summary>
    ///     Event to raise when an item is added, removed, or moved, or the entire list is refreshed.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    #endregion IListDataSource

    #region Viewport/ContentSize Management

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e) => SetContentSize (new Size (EffectiveMaxItemLength, Source?.Count ?? Viewport.Height));

    /// <summary>INTERNAL: Gets the width reserved for mark rendering (checkbox and space).</summary>
    private int MarkWidth => ShowMarks ? 2 : 0;

    /// <summary>INTERNAL: Gets the effective content width including mark columns when <see cref="ShowMarks"/> is true.</summary>
    private int EffectiveMaxItemLength => MaxItemLength + MarkWidth;

    /// <summary>Gets or sets the index of the item that will appear at the top of the <see cref="View.Viewport"/>.</summary>
    /// <remarks>
    ///     This a helper property for accessing <c>listView.Viewport.Y</c>.
    /// </remarks>
    /// <value>The top item.</value>
    /// <remarks>
    ///     Values are clamped to the valid range [0, Count - Viewport.Height].
    /// </remarks>
    [Obsolete ("Used only internally by ComboBox which will be replaced soon. Do not use.")]
    public int TopItem
    {
        get => Viewport.Y;
        set
        {
            if (Source is null)
            {
                return;
            }

            // Clamp to valid range: [0, Count - Viewport.Height]
            int maxTopItem = Math.Max (0, Source.Count - Viewport.Height);
            value = Math.Clamp (value, 0, maxTopItem);
            Viewport = Viewport with { Y = value };
        }
    }

    /// <summary>Gets or sets the leftmost column that is currently visible (when scrolling horizontally).</summary>
    [Obsolete ("Used only internally by ComboBox which will be replaced soon. Do not use.")]
    internal int LeftItem
    {
        get => Viewport.X;
        set
        {
            if (Source is null)
            {
                return;
            }

            // Clamp to valid range: [0, EffectiveMaxItemLength - Viewport.Width]
            int maxLeftItem = Math.Max (0, EffectiveMaxItemLength - Viewport.Width);
            value = Math.Clamp (value, 0, maxLeftItem);
            Viewport = Viewport with { X = value };
            SetNeedsDraw ();
        }
    }

    #endregion Viewport/ContentSize Management

    #region Keystroke Navigation

    /// <summary>
    ///     Gets the <see cref="CollectionNavigator"/> that searches the <see cref="ListView.Source"/> collection as the
    ///     user types.
    /// </summary>
    public IListCollectionNavigator KeystrokeNavigator { get; } = new CollectionNavigator ();

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        // If the key was bound to key command, let normal KeyDown processing happen. This enables overriding the default handling.
        // See: https://github.com/gui-cs/Terminal.Gui/issues/3950#issuecomment-2807350939
        if (KeyBindings.TryGet (key, out _))
        {
            return false;
        }

        // Enable user to find & select an item by typing text
        if (!KeystrokeNavigator.Matcher.IsCompatibleKey (key))
        {
            return false;
        }

        int? newItem = KeystrokeNavigator.GetNextMatchingItem (SelectedItem ?? null, (char)key);

        if (newItem is null or -1)
        {
            return false;
        }

        SelectedItem = (int)newItem;
        EnsureSelectedItemVisible ();
        SetNeedsDraw ();

        return true;
    }

    #endregion Keystroke Navigation

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        ListWrapper<string> source = new (["List Item 1", "List Item two", "List Item Quattro", "Last List Item"]);
        Source = source;

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        Source?.Dispose ();
        base.Dispose (disposing);
    }
}

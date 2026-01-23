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
///         <see cref="AllowsMarking"/> property is true, elements of the list can be marked by the user.
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
///         When <see cref="AllowsMarking"/> is set to true the rendering will prefix the rendered items with [x] or [ ]
///         and bind the SPACE key to toggle the selection. To implement a different marking style set
///         <see cref="AllowsMarking"/> to false and implement custom rendering.
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

        // Things this view knows how to do
        // 
        AddCommand (Command.Up, ctx => RaiseActivating (ctx) == true || MoveUp ());
        AddCommand (Command.Down, ctx => RaiseActivating (ctx) == true || MoveDown ());

        // TODO: add RaiseActivating to all of these
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.PageUp, () => MovePageUp ());
        AddCommand (Command.PageDown, () => MovePageDown ());
        AddCommand (Command.Start, () => MoveHome ());
        AddCommand (Command.End, () => MoveEnd ());
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));
        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));

        // Extend commands for multi-selection
        AddCommand (Command.UpExtend, ctx => RaiseActivating (ctx) == true || MoveUp (true));
        AddCommand (Command.DownExtend, ctx => RaiseActivating (ctx) == true || MoveDown (true));
        AddCommand (Command.PageUpExtend, () => MovePageUp (true));
        AddCommand (Command.PageDownExtend, () => MovePageDown (true));
        AddCommand (Command.StartExtend, () => MoveHome (true));
        AddCommand (Command.EndExtend, () => MoveEnd (true));

        // Accept (Enter key) - Raise Accept event - DO NOT advance state
        AddCommand (Command.Accept, HandleAccept);

        // Activate (Space key and single-click) - If AllowsMarking, change mark and raise Activate event
        AddCommand (Command.Activate, HandleActivate);

        // Hotkey - If none set, activate and raise Activate event. SetFocus. - DO NOT raise Accept
        AddCommand (Command.HotKey, HandleHotKey);

        AddCommand (Command.SelectAll, HandleSelectAll);

        // Default keybindings for all ListViews
        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.P.WithCtrl, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.N.WithCtrl, Command.Down);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.V.WithCtrl, Command.PageDown);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);

        // Shift+Arrow for extending selection
        KeyBindings.Add (Key.CursorUp.WithShift, Command.UpExtend);
        KeyBindings.Add (Key.P.WithCtrl.WithShift, Command.UpExtend);
        KeyBindings.Add (Key.CursorDown.WithShift, Command.DownExtend);
        KeyBindings.Add (Key.N.WithCtrl.WithShift, Command.DownExtend);

        KeyBindings.Add (Key.PageUp.WithShift, Command.PageUpExtend);
        KeyBindings.Add (Key.PageDown.WithShift, Command.PageDownExtend);
        KeyBindings.Add (Key.Home.WithShift, Command.StartExtend);
        KeyBindings.Add (Key.End.WithShift, Command.EndExtend);

        // Key.Space is already bound to Command.Activate; this gives us activate then move down
        KeyBindings.Add (Key.Space.WithShift, Command.Activate, Command.Down);

        // Use the form of Add that lets us pass context to the handler
        KeyBindings.Add (Key.A.WithCtrl, new KeyBinding ([Command.SelectAll], true));
        KeyBindings.Add (Key.U.WithCtrl, new KeyBinding ([Command.SelectAll], false));
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonDoubleClicked, Command.Accept);

        // Shift+Click and Ctrl+Click for multi-selection (overrides base View bindings)
        //MouseBindings.ReplaceCommands (MouseFlags.LeftButtonPressed | MouseFlags.Shift, Command.Activate);
        //MouseBindings.ReplaceCommands (MouseFlags.LeftButtonPressed | MouseFlags.Ctrl, Command.Activate);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Shift, Command.Activate);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Ctrl, Command.Activate);

        MouseBindings.ReplaceCommands (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledRight, Command.ScrollRight);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledLeft, Command.ScrollLeft);
    }

    private bool? HandleSelectAll (ICommandContext? ctx)
    {
        if (ctx?.Binding is not KeyBinding keyBinding)
        {
            return false;
        }

        return keyBinding.Data is { } && MarkAll ((bool)keyBinding.Data);
    }

    private bool? HandleHotKey (ICommandContext? ctx)
    {
        if (SelectedItem is { })
        {
            return !SetFocus ();
        }

        SelectedItem = 0;

        if (RaiseActivating (ctx) == true)
        {
            return true;
        }

        return !SetFocus ();
    }

    private bool? HandleAccept (ICommandContext? ctx)
    {
        if (RaiseAccepting (ctx) == true)
        {
            return true;
        }

        return OnOpenSelectedItem ();
    }

    private bool? HandleActivate (ICommandContext? ctx)
    {
        if (RaiseActivating (ctx) == true)
        {
            return true;
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        // Handle mouse clicks
        if (ctx?.Binding is not MouseBinding { MouseEvent: { } mouse })
        {
            if (AllowsMarking && SelectedItem.HasValue)
            {
                MarkUnmarkSelectedItem ();
            }

            return true;
        }

        // Handle keyboard (Space key) - mark item when AllowsMarking is enabled
        Point position = mouse.Position!.Value;
        int index = Viewport.Y + position.Y;

        if (Source is null || index >= Source.Count)
        {
            return true;
        }
        bool shift = mouse.Flags.HasFlag (MouseFlags.Shift);
        bool ctrl = mouse.Flags.HasFlag (MouseFlags.Ctrl);

        if (ctrl && AllowsMultipleMarking && AllowsMarking)
        {
            // Ctrl+Click: Toggle mark state directly
            Source.SetMark (index, !Source.IsMarked (index));

            // Update SelectedItem to clicked item
            SelectedItem = index;
            SetNeedsDraw ();
        }
        else if (shift && AllowsMultipleMarking)
        {
            // Shift+Click: Extend marking from anchor
            SetSelection (index, true);
        }
        else
        {
            // Normal click: Clear marks and select item
            SetSelection (index, false);

            // Mark item only on Clicked (not Pressed) to avoid double-toggle
            // since both Pressed and Clicked trigger Command.Activate
            if (AllowsMarking && mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked))
            {
                MarkUnmarkSelectedItem ();
            }
        }

        return true;
    }

    /// <summary>Gets or sets whether this <see cref="ListView"/> allows items to be marked.</summary>
    /// <value>Set to <see langword="true"/> to allow marking elements of the list.</value>
    /// <remarks>
    ///     If set to <see langword="true"/>, <see cref="ListView"/> will render items marked items with "[x]", and
    ///     unmarked items with "[ ]". SPACE key will toggle marking. The default is <see langword="false"/>.
    /// </remarks>
    public bool AllowsMarking
    {
        get;
        set
        {
            field = value;

            // Recalculate content size since mark columns affect effective width
            if (Source is { })
            {
                SetContentSize (new Size (EffectiveMaxItemLength, Source.Count));
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Event to raise when an item is added, removed, or moved, or the entire list is refreshed.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

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

    /// <summary>
    ///     If <see cref="AllowsMarking"/> and <see cref="AllowsMultipleMarking"/> are both <see langword="true"/>,
    ///     marks all items.
    /// </summary>
    /// <param name="mark"><see langword="true"/> marks all items; otherwise unmarks all items.</param>
    /// <returns><see langword="true"/> if marking was successful.</returns>
    public bool MarkAll (bool mark)
    {
        if (!AllowsMarking || !AllowsMultipleMarking)
        {
            return false;
        }

        for (var i = 0; i < Source?.Count; i++)
        {
            Source.SetMark (i, mark);
        }

        SetNeedsDraw ();

        return true;
    }

    /// <summary>Gets the widest item in the list.</summary>
    public int MaxItemLength => Source?.MaxItemLength ?? 0;

    /// <summary>Gets the width reserved for mark rendering (checkbox and space).</summary>
    private int MarkWidth => AllowsMarking ? 2 : 0;

    /// <summary>Gets the effective content width including mark columns when <see cref="AllowsMarking"/> is true.</summary>
    private int EffectiveMaxItemLength => MaxItemLength + MarkWidth;

    /// <summary>This event is raised when the user Double-Clicks on an item or presses ENTER to open the selected item.</summary>
    public event EventHandler<ListViewItemEventArgs>? OpenSelectedItem;

    /// <summary>
    ///     Allow resume the <see cref="CollectionChanged"/> event from being invoked,
    /// </summary>
    public void ResumeSuspendCollectionChangedEvent () => Source?.SuspendCollectionChangedEvent = false;

    /// <summary>This event is invoked when this <see cref="ListView"/> is being drawn before rendering.</summary>
    public event EventHandler<ListViewRowEventArgs>? RowRender;

    #region IValue<int?> Implementation

    /// <inheritdoc/>
    public int? Value { get => SelectedItem; set => SelectedItem = value; }

    /// <inheritdoc/>
    object? IValue.GetValue () => SelectedItem;

    /// <summary>
    ///     Called when the <see cref="ListView"/> <see cref="SelectedItem"/> is changing.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<int?> args) => false;

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<int?>>? ValueChanging;

    /// <summary>
    ///     Called when the <see cref="ListView"/> <see cref="SelectedItem"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<int?> args) { }

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<int?>>? ValueChanged;

    #endregion

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
                field.CollectionChanged += Source_CollectionChanged;
                SetContentSize (new Size (EffectiveMaxItemLength, field?.Count ?? Viewport.Height));
                KeystrokeNavigator.Collection = field?.ToList ();
            }

            SelectedItem = null;
            _lastSelectedItem = null;
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Allow suspending the <see cref="CollectionChanged"/> event from being invoked,
    /// </summary>
    public void SuspendCollectionChangedEvent () => Source?.SuspendCollectionChangedEvent = true;

    /// <summary>Gets or sets the index of the item that will appear at the top of the <see cref="View.Viewport"/>.</summary>
    /// <remarks>
    ///     This a helper property for accessing <c>listView.Viewport.Y</c>.
    /// </remarks>
    /// <value>The top item.</value>
    /// <remarks>
    ///     Values are clamped to the valid range [0, Count - Viewport.Height].
    /// </remarks>
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

    /// <summary>
    ///     Call the event to raises the <see cref="CollectionChanged"/>.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnCollectionChanged (NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke (this, e);

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e) => SetContentSize (new Size (EffectiveMaxItemLength, Source?.Count ?? Viewport.Height));

    private void Source_CollectionChanged (object? sender, NotifyCollectionChangedEventArgs e)
    {
        SetContentSize (new Size (EffectiveMaxItemLength, Source?.Count ?? Viewport.Height));

        if (Source is { Count: > 0 } && SelectedItem.HasValue && SelectedItem > Source.Count - 1)
        {
            SelectedItem = Source.Count - 1;
        }

        SetNeedsDraw ();
        OnCollectionChanged (e);
    }

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

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame) => EnsureSelectedItemVisible ();

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? currentFocused, View? newFocused)
    {
        if (newHasFocus && _lastSelectedItem != SelectedItem)
        {
            EnsureSelectedItemVisible ();
        }
    }
}

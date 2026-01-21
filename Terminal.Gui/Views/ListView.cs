using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Terminal.Gui.ViewBase;

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
///         <see cref="string"/> values are converted into <see cref="string"/> values before rendering, and other values
///         are converted into <see cref="string"/> by calling <see cref="object.ToString"/> and then converting to
///         <see cref="string"/> .
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
public class ListView : View, IDesignable, IValue<int?>
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

        // Accept (Enter key) - Raise Accept event - DO NOT advance state
        AddCommand (
                    Command.Accept,
                    ctx =>
                    {
                        if (RaiseAccepting (ctx) == true)
                        {
                            return true;
                        }

                        return OnOpenSelectedItem ();
                    });

        // Activate (Space key and single-click) - If AllowsMarking, change mark and raise Activate event
        AddCommand (
                    Command.Activate,
                    ctx =>
                    {
                        if (RaiseActivating (ctx) == true)
                        {
                            return true;
                        }

                        if (!HasFocus && CanFocus)
                        {
                            SetFocus ();
                        }

                        if (ctx is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } } mouseCommandContext)
                        {
                            Point position = mouseCommandContext.Binding.MouseEventArgs.Position!.Value;
                            SelectedItem = position.Y;
                        }

                        return MarkUnmarkSelectedItem ();
                    });

        // Hotkey - If none set, activate and raise Activate event. SetFocus. - DO NOT raise Accept
        AddCommand (
                    Command.HotKey,
                    ctx =>
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
                    });

        AddCommand (
                    Command.SelectAll,
                    ctx =>
                    {
                        if (ctx is not CommandContext<KeyBinding> keyCommandContext)
                        {
                            return false;
                        }

                        return keyCommandContext.Binding.Data is { } && MarkAll ((bool)keyCommandContext.Binding.Data);
                    });

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

        // Key.Space is already bound to Command.Activate; this gives us activate then move down
        KeyBindings.Add (Key.Space.WithShift, Command.Activate, Command.Down);

        // Use the form of Add that lets us pass context to the handler
        KeyBindings.Add (Key.A.WithCtrl, new KeyBinding ([Command.SelectAll], true));
        KeyBindings.Add (Key.U.WithCtrl, new KeyBinding ([Command.SelectAll], false));

        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonPressed, Command.Activate);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledRight, Command.ScrollRight);
        MouseBindings.ReplaceCommands (MouseFlags.WheeledLeft, Command.ScrollLeft);
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
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     If set to <see langword="true"/> more than one item can be selected. If <see langword="false"/> selecting an
    ///     item will cause all others to be un-selected. The default is <see langword="false"/>.
    /// </summary>
    public bool AllowsMultipleSelection
    {
        get;
        set
        {
            field = value;

            if (Source is { } && !field)
            {
                // Clear all selections except selected
                for (var i = 0; i < Source.Count; i++)
                {
                    if (Source.IsMarked (i) && SelectedItem.HasValue && i != SelectedItem.Value)
                    {
                        Source.SetMark (i, false);
                    }
                }
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Event to raise when an item is added, removed, or moved, or the entire list is refreshed.
    /// </summary>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>Ensures the selected item is always visible on the screen.</summary>
    public void EnsureSelectedItemVisible ()
    {
        if (SelectedItem is null)
        {
            return;
        }

        if (SelectedItem < Viewport.Y)
        {
            Viewport = Viewport with { Y = SelectedItem.Value };
        }
        else if (Viewport.Height > 0 && SelectedItem >= Viewport.Y + Viewport.Height)
        {
            Viewport = Viewport with { Y = SelectedItem.Value - Viewport.Height + 1 };
        }
    }

    /// <summary>
    ///     Gets the <see cref="CollectionNavigator"/> that searches the <see cref="ListView.Source"/> collection as the
    ///     user types.
    /// </summary>
    public IListCollectionNavigator KeystrokeNavigator { get; } = new CollectionNavigator ();

    /// <summary>Gets or sets the leftmost column that is currently visible (when scrolling horizontally).</summary>
    /// <value>The left position.</value>
    public int LeftItem
    {
        get => Viewport.X;
        set
        {
            if (Source is null)
            {
                return;
            }

            if (value < 0 || (MaxLength > 0 && value >= MaxLength))
            {
                throw new ArgumentException ("value");
            }

            Viewport = Viewport with { X = value };
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     If <see cref="AllowsMarking"/> and <see cref="AllowsMultipleSelection"/> are both <see langword="true"/>,
    ///     marks all items.
    /// </summary>
    /// <param name="mark"><see langword="true"/> marks all items; otherwise unmarks all items.</param>
    /// <returns><see langword="true"/> if marking was successful.</returns>
    public bool MarkAll (bool mark)
    {
        if (!AllowsMarking || !AllowsMultipleSelection)
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

    /// <summary>Marks the <see cref="SelectedItem"/> if it is not already marked.</summary>
    /// <returns><see langword="true"/> if the <see cref="SelectedItem"/> was marked.</returns>
    public bool MarkUnmarkSelectedItem ()
    {
        if (!AllowsMarking || Source is null || SelectedItem is null || !UnmarkAllButSelected ())
        {
            return false;
        }

        Source.SetMark (SelectedItem.Value, !Source.IsMarked (SelectedItem.Value));
        SetNeedsDraw ();

        return true;
    }

    /// <summary>Gets the widest item in the list.</summary>
    public int MaxLength => Source?.Length ?? 0;

    /// <summary>Changes the <see cref="SelectedItem"/> to the next item in the list, scrolling the list if needed.</summary>
    /// <returns></returns>
    public bool MoveDown ()
    {
        if (Source is null || Source.Count == 0)
        {
            return false; //Nothing for us to move to
        }

        var moved = false;

        if (SelectedItem is null || SelectedItem >= Source.Count)
        {
            // If SelectedItem is null or for some reason we are currently outside the
            // valid values range, we should select the first or bottommost valid value.
            // This can occur if the backing data source changes.
            SelectedItem = SelectedItem is null ? 0 : Source.Count - 1;
            moved = true;
        }
        else if (SelectedItem + 1 < Source.Count)
        {
            //can move by down by one.
            SelectedItem++;

            if (SelectedItem >= Viewport.Y + Viewport.Height)
            {
                Viewport = Viewport with { Y = Viewport.Y + 1 };
            }
            else if (SelectedItem < Viewport.Y)
            {
                Viewport = Viewport with { Y = SelectedItem.Value };
            }

            moved = true;
        }
        else if (SelectedItem >= Viewport.Y + Viewport.Height)
        {
            Viewport = Viewport with { Y = Source.Count - Viewport.Height };
            moved = true;
        }

        return moved;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to last item in the list, scrolling the list if needed.</summary>
    /// <returns></returns>
    public bool MoveEnd ()
    {
        if (Source is not { Count: > 0 } || SelectedItem == Source.Count - 1)
        {
            return true;
        }

        SelectedItem = Source.Count - 1;

        if (Viewport.Y + SelectedItem > Viewport.Height - 1)
        {
            Viewport = Viewport with
            {
                Y = SelectedItem < Viewport.Height - 1
                        ? Math.Max (Viewport.Height - SelectedItem.Value + 1, 0)
                        : Math.Max (SelectedItem.Value - Viewport.Height + 1, 0)
            };
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the first item in the list, scrolling the list if needed.</summary>
    /// <returns></returns>
    public bool MoveHome ()
    {
        if (SelectedItem == 0)
        {
            return true;
        }

        SelectedItem = 0;
        Viewport = Viewport with { Y = SelectedItem.Value };

        return true;
    }

    /// <summary>
    ///     Changes the <see cref="SelectedItem"/> to the item just below the bottom of the visible list, scrolling if
    ///     needed.
    /// </summary>
    /// <returns></returns>
    public bool MovePageDown ()
    {
        if (Source is null || Source.Count == 0)
        {
            return false;
        }

        int n = (SelectedItem ?? 0) + Viewport.Height;

        if (n >= Source.Count)
        {
            n = Source.Count - 1;
        }

        if (n == SelectedItem)
        {
            return true;
        }

        SelectedItem = n;

        if (Source.Count >= Viewport.Height)
        {
            Viewport = Viewport with { Y = SelectedItem.Value };
        }
        else
        {
            Viewport = Viewport with { Y = 0 };
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the item at the top of the visible list.</summary>
    /// <returns></returns>
    public bool MovePageUp ()
    {
        if (Source is null || Source.Count == 0)
        {
            return false;
        }

        int n = (SelectedItem ?? 0) - Viewport.Height;

        if (n < 0)
        {
            n = 0;
        }

        if (n == SelectedItem || !(n < Source?.Count))
        {
            return true;
        }

        SelectedItem = n;
        Viewport = Viewport with { Y = SelectedItem.Value };

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the previous item in the list, scrolling the list if needed.</summary>
    /// <returns></returns>
    public bool MoveUp ()
    {
        if (Source is null || Source.Count == 0)
        {
            return false; //Nothing for us to move to
        }

        var moved = false;

        if (SelectedItem is null || SelectedItem >= Source.Count)
        {
            // If SelectedItem is null or for some reason we are currently outside the
            // valid values range, we should select the bottommost valid value.
            // This can occur if the backing data source changes.
            SelectedItem = Source.Count - 1;
            moved = true;
        }
        else if (SelectedItem > 0)
        {
            SelectedItem--;

            if (SelectedItem > Source.Count)
            {
                SelectedItem = Source.Count - 1;
            }

            if (SelectedItem < Viewport.Y)
            {
                Viewport = Viewport with { Y = SelectedItem.Value };
            }
            else if (SelectedItem > Viewport.Y + Viewport.Height)
            {
                Viewport = Viewport with { Y = SelectedItem.Value - Viewport.Height + 1 };
            }

            moved = true;
        }
        else if (SelectedItem < Viewport.Y)
        {
            Viewport = Viewport with { Y = SelectedItem.Value };
            moved = true;
        }

        return moved;
    }

    /// <summary>Invokes the <see cref="OpenSelectedItem"/> event if it is defined.</summary>
    /// <returns><see langword="true"/> if the <see cref="OpenSelectedItem"/> event was fired.</returns>
    public bool OnOpenSelectedItem ()
    {
        if (Source is null || SelectedItem is null || Source.Count <= SelectedItem || SelectedItem < 0 || OpenSelectedItem is null)
        {
            return false;
        }

        object? value = Source.ToList () [SelectedItem.Value];

        OpenSelectedItem?.Invoke (this, new (SelectedItem.Value, value!));

        // BUGBUG: this should not blindly return true.
        return true;
    }

    /// <summary>Virtual method that will invoke the <see cref="RowRender"/>.</summary>
    /// <param name="rowEventArgs"></param>
    public virtual void OnRowRender (ListViewRowEventArgs rowEventArgs) { RowRender?.Invoke (this, rowEventArgs); }

    /// <summary>This event is raised when the user Double-Clicks on an item or presses ENTER to open the selected item.</summary>
    public event EventHandler<ListViewItemEventArgs>? OpenSelectedItem;

    /// <summary>
    ///     Allow resume the <see cref="CollectionChanged"/> event from being invoked,
    /// </summary>
    public void ResumeSuspendCollectionChangedEvent () { Source?.SuspendCollectionChangedEvent = false; }

    /// <summary>This event is invoked when this <see cref="ListView"/> is being drawn before rendering.</summary>
    public event EventHandler<ListViewRowEventArgs>? RowRender;

    private int? _lastSelectedItem;

    /// <summary>Gets or sets the index of the currently selected item.</summary>
    /// <value>The selected item or null if no item is selected.</value>
    public int? SelectedItem
    {
        get;
        set
        {
            if (Source is null)
            {
                return;
            }

            if (value.HasValue && (value < 0 || value >= Source.Count))
            {
                throw new ArgumentException (@"SelectedItem must be greater than 0 or less than the number of items.");
            }

            int? oldValue = field;

            if (oldValue == value)
            {
                return;
            }

            ValueChangingEventArgs<int?> changingArgs = new (oldValue, value);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            field = value;
            SetNeedsDraw ();

            if (SelectedItem != _lastSelectedItem)
            {
                _lastSelectedItem = SelectedItem;
                EnsureSelectedItemVisible ();
            }

            ValueChangedEventArgs<int?> changedArgs = new (oldValue, field);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
        }
    }

    #region IValue<int?> Implementation

    /// <inheritdoc/>
    public int? Value
    {
        get => SelectedItem;
        set => SelectedItem = value;
    }

    /// <inheritdoc/>
    object? IValue.GetValue () => SelectedItem;

    /// <summary>
    ///     Called when the <see cref="ListView"/> <see cref="SelectedItem"/> is changing.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<int?> args) { return false; }

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
    public Task SetSourceAsync<T> (ObservableCollection<T>? source)
    {
        return Task.Factory.StartNew (
                                      () =>
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
                                      TaskScheduler.Default
                                     );
    }

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
                SetContentSize (new Size (field?.Length ?? Viewport.Width, field?.Count ?? Viewport.Width));
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
    public void SuspendCollectionChangedEvent () { Source?.SuspendCollectionChangedEvent = true; }

    /// <summary>Gets or sets the index of the item that will appear at the top of the <see cref="View.Viewport"/>.</summary>
    /// <remarks>
    ///     This a helper property for accessing <c>listView.Viewport.Y</c>.
    /// </remarks>
    /// <value>The top item.</value>
    public int TopItem
    {
        get => Viewport.Y;
        set
        {
            if (Source is null)
            {
                return;
            }

            Viewport = Viewport with { Y = value };
        }
    }

    /// <summary>
    ///     If <see cref="AllowsMarking"/> and <see cref="AllowsMultipleSelection"/> are both <see langword="true"/>,
    ///     unmarks all marked items other than <see cref="SelectedItem"/>.
    /// </summary>
    /// <returns><see langword="true"/> if unmarking was successful.</returns>
    public bool UnmarkAllButSelected ()
    {
        if (!AllowsMarking)
        {
            return false;
        }

        if (AllowsMultipleSelection)
        {
            return true;
        }

        for (var i = 0; i < Source?.Count; i++)
        {
            if (Source.IsMarked (i) && i != SelectedItem)
            {
                Source.SetMark (i, false);

                return true;
            }
        }

        return true;
    }

    /// <summary>
    ///     Call the event to raises the <see cref="CollectionChanged"/>.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnCollectionChanged (NotifyCollectionChangedEventArgs e) { CollectionChanged?.Invoke (this, e); }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (Source is null)
        {
            return base.OnDrawingContent (context);
        }

        var current = Attribute.Default;
        Move (0, 0);
        Rectangle f = Viewport;
        int item = Viewport.Y;
        bool focused = HasFocus;
        int col = AllowsMarking ? 2 : 0;
        int start = Viewport.X;

        for (var row = 0; row < f.Height; row++, item++)
        {
            bool isSelected = item == SelectedItem;

            Attribute newAttribute = focused ? isSelected ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal) :
                                     isSelected ? GetAttributeForRole (VisualRole.Active) : GetAttributeForRole (VisualRole.Normal);

            if (newAttribute != current)
            {
                SetAttribute (newAttribute);
                current = newAttribute;
            }

            Move (0, row);

            if (Source is null || item >= Source.Count)
            {
                for (var c = 0; c < f.Width; c++)
                {
                    AddRune ((Rune)' ');
                }
            }
            else
            {
                var rowEventArgs = new ListViewRowEventArgs (item);
                OnRowRender (rowEventArgs);

                if (rowEventArgs.RowAttribute is { } && current != rowEventArgs.RowAttribute)
                {
                    current = (Attribute)rowEventArgs.RowAttribute;
                    SetAttribute (current);
                }

                if (AllowsMarking)
                {
                    AddRune (
                             Source.IsMarked (item) ? AllowsMultipleSelection ? Glyphs.CheckStateChecked : Glyphs.Selected :
                             AllowsMultipleSelection ? Glyphs.CheckStateUnChecked : Glyphs.UnSelected
                            );
                    AddRune ((Rune)' ');
                }

                Source.Render (this, isSelected, item, col, row, f.Width - col, start);
            }
        }

        return true;
    }

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame) { EnsureSelectedItemVisible (); }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? currentFocused, View? newFocused)
    {
        if (newHasFocus && _lastSelectedItem != SelectedItem)
        {
            EnsureSelectedItemVisible ();
        }
    }

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
    protected override void OnViewportChanged (DrawEventArgs e) { SetContentSize (new Size (MaxLength, Source?.Count ?? Viewport.Height)); }

    private void Source_CollectionChanged (object? sender, NotifyCollectionChangedEventArgs e)
    {
        SetContentSize (new Size (Source?.Length ?? Viewport.Width, Source?.Count ?? Viewport.Width));

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
}

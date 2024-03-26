using System.Collections;

namespace Terminal.Gui;

/// <summary>Implement <see cref="IListDataSource"/> to provide custom rendering for a <see cref="ListView"/>.</summary>
public interface IListDataSource
{
    /// <summary>Returns the number of elements to display</summary>
    int Count { get; }

    /// <summary>Returns the maximum length of elements to display</summary>
    int Length { get; }

    /// <summary>Should return whether the specified item is currently marked.</summary>
    /// <returns><see langword="true"/>, if marked, <see langword="false"/> otherwise.</returns>
    /// <param name="item">Item index.</param>
    bool IsMarked (int item);

    /// <summary>This method is invoked to render a specified item, the method should cover the entire provided width.</summary>
    /// <returns>The render.</returns>
    /// <param name="container">The list view to render.</param>
    /// <param name="driver">The console driver to render.</param>
    /// <param name="selected">Describes whether the item being rendered is currently selected by the user.</param>
    /// <param name="item">The index of the item to render, zero for the first item and so on.</param>
    /// <param name="col">The column where the rendering will start</param>
    /// <param name="line">The line where the rendering will be done.</param>
    /// <param name="width">The width that must be filled out.</param>
    /// <param name="start">The index of the string to be displayed.</param>
    /// <remarks>
    ///     The default color will be set before this method is invoked, and will be based on whether the item is selected
    ///     or not.
    /// </remarks>
    void Render (
        ListView container,
        ConsoleDriver driver,
        bool selected,
        int item,
        int col,
        int line,
        int width,
        int start = 0
    );

    /// <summary>Flags the item as marked.</summary>
    /// <param name="item">Item index.</param>
    /// <param name="value">If set to <see langword="true"/> value.</param>
    void SetMark (int item, bool value);

    /// <summary>Return the source as IList.</summary>
    /// <returns></returns>
    IList ToList ();
}

/// <summary>
///     ListView <see cref="View"/> renders a scrollable list of data where each item can be activated to perform an
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
///         <see cref="IList"/> object (e.g. arrays, <see cref="List{T}"/>, and other collections). Alternatively, an
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
///         rendering via <see cref="IListDataSource"/>) or call <see cref="SetSource"/> an <see cref="IList"/> is being
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
public class ListView : View
{
    private bool _allowsMarking;
    private bool _allowsMultipleSelection = true;
    private int _lastSelectedItem = -1;
    private int _selected = -1;
    private IListDataSource _source;
    private int _top, _left;

    /// <summary>
    ///     Initializes a new instance of <see cref="ListView"/>. Set the <see cref="Source"/> property to display
    ///     something.
    /// </summary>
    public ListView ()
    {
        CanFocus = true;

        // Things this view knows how to do
        AddCommand (Command.LineUp, () => MoveUp ());
        AddCommand (Command.LineDown, () => MoveDown ());
        AddCommand (Command.ScrollUp, () => ScrollUp (1));
        AddCommand (Command.ScrollDown, () => ScrollDown (1));
        AddCommand (Command.PageUp, () => MovePageUp ());
        AddCommand (Command.PageDown, () => MovePageDown ());
        AddCommand (Command.TopHome, () => MoveHome ());
        AddCommand (Command.BottomEnd, () => MoveEnd ());
        AddCommand (Command.Accept, () => OnOpenSelectedItem ());
        AddCommand (Command.OpenSelectedItem, () => OnOpenSelectedItem ());
        AddCommand (Command.Select, () => MarkUnmarkRow ());

        // Default keybindings for all ListViews
        KeyBindings.Add (Key.CursorUp, Command.LineUp);
        KeyBindings.Add (Key.P.WithCtrl, Command.LineUp);

        KeyBindings.Add (Key.CursorDown, Command.LineDown);
        KeyBindings.Add (Key.N.WithCtrl, Command.LineDown);

        KeyBindings.Add (Key.PageUp, Command.PageUp);

        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.V.WithCtrl, Command.PageDown);

        KeyBindings.Add (Key.Home, Command.TopHome);

        KeyBindings.Add (Key.End, Command.BottomEnd);

        KeyBindings.Add (Key.Enter, Command.OpenSelectedItem);
    }

    /// <summary>Gets or sets whether this <see cref="ListView"/> allows items to be marked.</summary>
    /// <value>Set to <see langword="true"/> to allow marking elements of the list.</value>
    /// <remarks>
    ///     If set to <see langword="true"/>, <see cref="ListView"/> will render items marked items with "[x]", and
    ///     unmarked items with "[ ]" spaces. SPACE key will toggle marking. The default is <see langword="false"/>.
    /// </remarks>
    public bool AllowsMarking
    {
        get => _allowsMarking;
        set
        {
            _allowsMarking = value;

            if (_allowsMarking)
            {
                KeyBindings.Add (Key.Space, Command.Select);
            }
            else
            {
                KeyBindings.Remove (Key.Space);
            }

            SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     If set to <see langword="true"/> more than one item can be selected. If <see langword="false"/> selecting an
    ///     item will cause all others to be un-selected. The default is <see langword="false"/>.
    /// </summary>
    public bool AllowsMultipleSelection
    {
        get => _allowsMultipleSelection;
        set
        {
            _allowsMultipleSelection = value;

            if (Source is { } && !_allowsMultipleSelection)
            {
                // Clear all selections except selected 
                for (var i = 0; i < Source.Count; i++)
                {
                    if (Source.IsMarked (i) && i != _selected)
                    {
                        Source.SetMark (i, false);
                    }
                }
            }

            SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     Gets the <see cref="CollectionNavigator"/> that searches the <see cref="ListView.Source"/> collection as the
    ///     user types.
    /// </summary>
    public CollectionNavigator KeystrokeNavigator { get; } = new ();

    /// <summary>Gets or sets the leftmost column that is currently visible (when scrolling horizontally).</summary>
    /// <value>The left position.</value>
    public int LeftItem
    {
        get => _left;
        set
        {
            if (_source is null)
            {
                return;
            }

            if (value < 0 || (MaxLength > 0 && value >= MaxLength))
            {
                throw new ArgumentException ("value");
            }

            _left = value;
            SetNeedsDisplay ();
        }
    }

    /// <summary>Gets the widest item in the list.</summary>
    public int MaxLength => _source?.Length ?? 0;

    /// <summary>Gets or sets the index of the currently selected item.</summary>
    /// <value>The selected item.</value>
    public int SelectedItem
    {
        get => _selected;
        set
        {
            if (_source is null || _source.Count == 0)
            {
                return;
            }

            if (value < -1 || value >= _source.Count)
            {
                throw new ArgumentException ("value");
            }

            _selected = value;
            OnSelectedChanged ();
        }
    }

    /// <summary>Gets or sets the <see cref="IListDataSource"/> backing this <see cref="ListView"/>, enabling custom rendering.</summary>
    /// <value>The source.</value>
    /// <remarks>Use <see cref="SetSource"/> to set a new <see cref="IList"/> source.</remarks>
    public IListDataSource Source
    {
        get => _source;
        set
        {
            _source = value;
            KeystrokeNavigator.Collection = _source?.ToList ();
            _top = 0;
            _selected = -1;
            _lastSelectedItem = -1;
            SetNeedsDisplay ();
        }
    }

    /// <summary>Gets or sets the item that is displayed at the top of the <see cref="ListView"/>.</summary>
    /// <value>The top item.</value>
    public int TopItem
    {
        get => _top;
        set
        {
            if (_source is null)
            {
                return;
            }

            if (value < 0 || (_source.Count > 0 && value >= _source.Count))
            {
                throw new ArgumentException ("value");
            }

            _top = Math.Max (value, 0);
            SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     If <see cref="AllowsMarking"/> and <see cref="AllowsMultipleSelection"/> are both <see langword="true"/>,
    ///     unmarks all marked items other than the currently selected.
    /// </summary>
    /// <returns><see langword="true"/> if unmarking was successful.</returns>
    public virtual bool AllowsAll ()
    {
        if (!_allowsMarking)
        {
            return false;
        }

        if (!AllowsMultipleSelection)
        {
            for (var i = 0; i < Source.Count; i++)
            {
                if (Source.IsMarked (i) && i != _selected)
                {
                    Source.SetMark (i, false);

                    return true;
                }
            }
        }

        return true;
    }

    /// <summary>Ensures the selected item is always visible on the screen.</summary>
    public void EnsureSelectedItemVisible ()
    {
        if (SuperView?.IsInitialized == true)
        {
            if (_selected < _top)
            {
                _top = Math.Max (_selected, 0);
            }
            else if (Bounds.Height > 0 && _selected >= _top + Bounds.Height)
            {
                _top = Math.Max (_selected - Bounds.Height + 1, 0);
            }

            LayoutStarted -= ListView_LayoutStarted;
        }
        else
        {
            LayoutStarted += ListView_LayoutStarted;
        }
    }

    /// <summary>Marks the <see cref="SelectedItem"/> if it is not already marked.</summary>
    /// <returns><see langword="true"/> if the <see cref="SelectedItem"/> was marked.</returns>
    public virtual bool MarkUnmarkRow ()
    {
        if (AllowsAll ())
        {
            Source.SetMark (SelectedItem, !Source.IsMarked (SelectedItem));
            SetNeedsDisplay ();

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected internal override bool OnMouseEvent  (MouseEvent me)
    {
        if (!me.Flags.HasFlag (MouseFlags.Button1Clicked)
            && !me.Flags.HasFlag (MouseFlags.Button1DoubleClicked)
            && me.Flags != MouseFlags.WheeledDown
            && me.Flags != MouseFlags.WheeledUp
            && me.Flags != MouseFlags.WheeledRight
            && me.Flags != MouseFlags.WheeledLeft)
        {
            return false;
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        if (_source is null)
        {
            return false;
        }

        if (me.Flags == MouseFlags.WheeledDown)
        {
            ScrollDown (1);

            return true;
        }

        if (me.Flags == MouseFlags.WheeledUp)
        {
            ScrollUp (1);

            return true;
        }

        if (me.Flags == MouseFlags.WheeledRight)
        {
            ScrollRight (1);

            return true;
        }

        if (me.Flags == MouseFlags.WheeledLeft)
        {
            ScrollLeft (1);

            return true;
        }

        if (me.Y + _top >= _source.Count
            || me.Y + _top < 0
            || me.Y + _top > _top + Bounds.Height)
        {
            return true;
        }

        _selected = _top + me.Y;

        if (AllowsAll ())
        {
            Source.SetMark (SelectedItem, !Source.IsMarked (SelectedItem));
            SetNeedsDisplay ();

            return true;
        }

        OnSelectedChanged ();
        SetNeedsDisplay ();

        if (me.Flags == MouseFlags.Button1DoubleClicked)
        {
            OnOpenSelectedItem ();
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the next item in the list, scrolling the list if needed.</summary>
    /// <returns></returns>
    public virtual bool MoveDown ()
    {
        if (_source is null || _source.Count == 0)
        {
            // Do we set lastSelectedItem to -1 here?
            return false; //Nothing for us to move to
        }

        if (_selected >= _source.Count)
        {
            // If for some reason we are currently outside of the
            // valid values range, we should select the bottommost valid value.
            // This can occur if the backing data source changes.
            _selected = _source.Count - 1;
            OnSelectedChanged ();
            SetNeedsDisplay ();
        }
        else if (_selected + 1 < _source.Count)
        {
            //can move by down by one.
            _selected++;

            if (_selected >= _top + Bounds.Height)
            {
                _top++;
            }
            else if (_selected < _top)
            {
                _top = Math.Max (_selected, 0);
            }

            OnSelectedChanged ();
            SetNeedsDisplay ();
        }
        else if (_selected == 0)
        {
            OnSelectedChanged ();
            SetNeedsDisplay ();
        }
        else if (_selected >= _top + Bounds.Height)
        {
            _top = Math.Max (_source.Count - Bounds.Height, 0);
            SetNeedsDisplay ();
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to last item in the list, scrolling the list if needed.</summary>
    /// <returns></returns>
    public virtual bool MoveEnd ()
    {
        if (_source is { Count: > 0 } && _selected != _source.Count - 1)
        {
            _selected = _source.Count - 1;

            if (_top + _selected > Bounds.Height - 1)
            {
                _top = Math.Max (_selected, 0);
            }

            OnSelectedChanged ();
            SetNeedsDisplay ();
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the first item in the list, scrolling the list if needed.</summary>
    /// <returns></returns>
    public virtual bool MoveHome ()
    {
        if (_selected != 0)
        {
            _selected = 0;
            _top = Math.Max (_selected, 0);
            OnSelectedChanged ();
            SetNeedsDisplay ();
        }

        return true;
    }

    /// <summary>
    ///     Changes the <see cref="SelectedItem"/> to the item just below the bottom of the visible list, scrolling if
    ///     needed.
    /// </summary>
    /// <returns></returns>
    public virtual bool MovePageDown ()
    {
        if (_source is null)
        {
            return true;
        }

        int n = _selected + Bounds.Height;

        if (n >= _source.Count)
        {
            n = _source.Count - 1;
        }

        if (n != _selected)
        {
            _selected = n;

            if (_source.Count >= Bounds.Height)
            {
                _top = Math.Max (_selected, 0);
            }
            else
            {
                _top = 0;
            }

            OnSelectedChanged ();
            SetNeedsDisplay ();
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the item at the top of the visible list.</summary>
    /// <returns></returns>
    public virtual bool MovePageUp ()
    {
        int n = _selected - Bounds.Height;

        if (n < 0)
        {
            n = 0;
        }

        if (n != _selected)
        {
            _selected = n;
            _top = Math.Max (_selected, 0);
            OnSelectedChanged ();
            SetNeedsDisplay ();
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the previous item in the list, scrolling the list if needed.</summary>
    /// <returns></returns>
    public virtual bool MoveUp ()
    {
        if (_source is null || _source.Count == 0)
        {
            // Do we set lastSelectedItem to -1 here?
            return false; //Nothing for us to move to
        }

        if (_selected >= _source.Count)
        {
            // If for some reason we are currently outside of the
            // valid values range, we should select the bottommost valid value.
            // This can occur if the backing data source changes.
            _selected = _source.Count - 1;
            OnSelectedChanged ();
            SetNeedsDisplay ();
        }
        else if (_selected > 0)
        {
            _selected--;

            if (_selected > Source.Count)
            {
                _selected = Source.Count - 1;
            }

            if (_selected < _top)
            {
                _top = Math.Max (_selected, 0);
            }
            else if (_selected > _top + Bounds.Height)
            {
                _top = Math.Max (_selected - Bounds.Height + 1, 0);
            }

            OnSelectedChanged ();
            SetNeedsDisplay ();
        }
        else if (_selected < _top)
        {
            _top = Math.Max (_selected, 0);
            SetNeedsDisplay ();
        }

        return true;
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle contentArea)
    {
        base.OnDrawContent (contentArea);

        Attribute current = ColorScheme.Focus;
        Driver.SetAttribute (current);
        Move (0, 0);
        Rectangle f = Bounds;
        int item = _top;
        bool focused = HasFocus;
        int col = _allowsMarking ? 2 : 0;
        int start = _left;

        for (var row = 0; row < f.Height; row++, item++)
        {
            bool isSelected = item == _selected;

            Attribute newcolor = focused ? isSelected ? ColorScheme.Focus : GetNormalColor () :
                                 isSelected ? ColorScheme.HotNormal : GetNormalColor ();

            if (newcolor != current)
            {
                Driver.SetAttribute (newcolor);
                current = newcolor;
            }

            Move (0, row);

            if (_source is null || item >= _source.Count)
            {
                for (var c = 0; c < f.Width; c++)
                {
                    Driver.AddRune ((Rune)' ');
                }
            }
            else
            {
                var rowEventArgs = new ListViewRowEventArgs (item);
                OnRowRender (rowEventArgs);

                if (rowEventArgs.RowAttribute is { } && current != rowEventArgs.RowAttribute)
                {
                    current = (Attribute)rowEventArgs.RowAttribute;
                    Driver.SetAttribute (current);
                }

                if (_allowsMarking)
                {
                    Driver.AddRune (
                                    _source.IsMarked (item) ? AllowsMultipleSelection ? Glyphs.Checked : Glyphs.Selected :
                                    AllowsMultipleSelection ? Glyphs.UnChecked : Glyphs.UnSelected
                                   );
                    Driver.AddRune ((Rune)' ');
                }

                Source.Render (this, Driver, isSelected, item, col, row, f.Width - col, start);
            }
        }
    }

    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        if (IsInitialized)
        {
            Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);
        }

        if (_lastSelectedItem != _selected)
        {
            EnsureSelectedItemVisible ();
        }

        return base.OnEnter (view);
    }

    // TODO: This should be cancelable
    /// <summary>Invokes the <see cref="OpenSelectedItem"/> event if it is defined.</summary>
    /// <returns><see langword="true"/> if the <see cref="OpenSelectedItem"/> event was fired.</returns>
    public bool OnOpenSelectedItem ()
    {
        if (_source is null || _source.Count <= _selected || _selected < 0 || OpenSelectedItem is null)
        {
            return false;
        }

        object value = _source.ToList () [_selected];

        // By default, Command.Accept calls OnAccept, so we need to call it here to ensure that the event is fired.
        if (OnAccept () == true)
        {
            return true;
        }

        OpenSelectedItem?.Invoke (this, new ListViewItemEventArgs (_selected, value));
        return true;
    }

    /// <inheritdoc/>
    public override bool OnProcessKeyDown (Key a)
    {
        // Enable user to find & select an item by typing text
        if (CollectionNavigatorBase.IsCompatibleKey (a))
        {
            int? newItem = KeystrokeNavigator?.GetNextMatchingItem (SelectedItem, (char)a);

            if (newItem is int && newItem != -1)
            {
                SelectedItem = (int)newItem;
                EnsureSelectedItemVisible ();
                SetNeedsDisplay ();

                return true;
            }
        }

        return false;
    }

    /// <summary>Virtual method that will invoke the <see cref="RowRender"/>.</summary>
    /// <param name="rowEventArgs"></param>
    public virtual void OnRowRender (ListViewRowEventArgs rowEventArgs) { RowRender?.Invoke (this, rowEventArgs); }

    /// <summary>Invokes the <see cref="SelectedItemChanged"/> event if it is defined.</summary>
    /// <returns></returns>
    public virtual bool OnSelectedChanged ()
    {
        if (_selected != _lastSelectedItem)
        {
            object value = _source?.Count > 0 ? _source.ToList () [_selected] : null;
            SelectedItemChanged?.Invoke (this, new ListViewItemEventArgs (_selected, value));
            _lastSelectedItem = _selected;
            EnsureSelectedItemVisible ();

            return true;
        }

        return false;
    }

    /// <summary>This event is raised when the user Double Clicks on an item or presses ENTER to open the selected item.</summary>
    public event EventHandler<ListViewItemEventArgs> OpenSelectedItem;

    /// <inheritdoc/>
    public override void PositionCursor ()
    {
        if (_allowsMarking)
        {
            Move (0, _selected - _top);
        }
        else
        {
            Move (Bounds.Width - 1, _selected - _top);
        }
    }

    /// <summary>This event is invoked when this <see cref="ListView"/> is being drawn before rendering.</summary>
    public event EventHandler<ListViewRowEventArgs> RowRender;

    /// <summary>Scrolls the view down by <paramref name="items"/> items.</summary>
    /// <param name="items">Number of items to scroll down.</param>
    public virtual bool ScrollDown (int items)
    {
        _top = Math.Max (Math.Min (_top + items, _source.Count - 1), 0);
        SetNeedsDisplay ();

        return true;
    }

    /// <summary>Scrolls the view left.</summary>
    /// <param name="cols">Number of columns to scroll left.</param>
    public virtual bool ScrollLeft (int cols)
    {
        _left = Math.Max (_left - cols, 0);
        SetNeedsDisplay ();

        return true;
    }

    /// <summary>Scrolls the view right.</summary>
    /// <param name="cols">Number of columns to scroll right.</param>
    public virtual bool ScrollRight (int cols)
    {
        _left = Math.Max (Math.Min (_left + cols, MaxLength - 1), 0);
        SetNeedsDisplay ();

        return true;
    }

    /// <summary>Scrolls the view up by <paramref name="items"/> items.</summary>
    /// <param name="items">Number of items to scroll up.</param>
    public virtual bool ScrollUp (int items)
    {
        _top = Math.Max (_top - items, 0);
        SetNeedsDisplay ();

        return true;
    }

    /// <summary>This event is raised when the selected item in the <see cref="ListView"/> has changed.</summary>
    public event EventHandler<ListViewItemEventArgs> SelectedItemChanged;

    /// <summary>Sets the source of the <see cref="ListView"/> to an <see cref="IList"/>.</summary>
    /// <value>An object implementing the IList interface.</value>
    /// <remarks>
    ///     Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custome
    ///     rendering.
    /// </remarks>
    public void SetSource (IList source)
    {
        if (source is null && (Source is null || !(Source is ListWrapper)))
        {
            Source = null;
        }
        else
        {
            Source = new ListWrapper (source);
        }
    }

    /// <summary>Sets the source to an <see cref="IList"/> value asynchronously.</summary>
    /// <value>An item implementing the IList interface.</value>
    /// <remarks>
    ///     Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custom
    ///     rendering.
    /// </remarks>
    public Task SetSourceAsync (IList source)
    {
        return Task.Factory.StartNew (
                                      () =>
                                      {
                                          if (source is null && (Source is null || !(Source is ListWrapper)))
                                          {
                                              Source = null;
                                          }
                                          else
                                          {
                                              Source = new ListWrapper (source);
                                          }

                                          return source;
                                      },
                                      CancellationToken.None,
                                      TaskCreationOptions.DenyChildAttach,
                                      TaskScheduler.Default
                                     );
    }

    private void ListView_LayoutStarted (object sender, LayoutEventArgs e) { EnsureSelectedItemVisible (); }
}

/// <summary>
///     Provides a default implementation of <see cref="IListDataSource"/> that renders <see cref="ListView"/> items
///     using <see cref="object.ToString()"/>.
/// </summary>
public class ListWrapper : IListDataSource
{
    private readonly int _count;
    private readonly BitArray _marks;
    private readonly IList _source;

    /// <inheritdoc/>
    public ListWrapper (IList source)
    {
        if (source is { })
        {
            _count = source.Count;
            _marks = new BitArray (_count);
            _source = source;
            Length = GetMaxLengthItem ();
        }
    }

    /// <inheritdoc/>
    public int Count => _source is { } ? _source.Count : 0;

    /// <inheritdoc/>
    public int Length { get; }

    /// <inheritdoc/>
    public void Render (
        ListView container,
        ConsoleDriver driver,
        bool marked,
        int item,
        int col,
        int line,
        int width,
        int start = 0
    )
    {
        container.Move (col, line);
        object t = _source? [item];

        if (t is null)
        {
            RenderUstr (driver, "", col, line, width);
        }
        else
        {
            if (t is string u)
            {
                RenderUstr (driver, u, col, line, width, start);
            }
            else if (t is string s)
            {
                RenderUstr (driver, s, col, line, width, start);
            }
            else
            {
                RenderUstr (driver, t.ToString (), col, line, width, start);
            }
        }
    }

    /// <inheritdoc/>
    public bool IsMarked (int item)
    {
        if (item >= 0 && item < _count)
        {
            return _marks [item];
        }

        return false;
    }

    /// <inheritdoc/>
    public void SetMark (int item, bool value)
    {
        if (item >= 0 && item < _count)
        {
            _marks [item] = value;
        }
    }

    /// <inheritdoc/>
    public IList ToList () { return _source; }

    /// <inheritdoc/>
    public int StartsWith (string search)
    {
        if (_source is null || _source?.Count == 0)
        {
            return -1;
        }

        for (var i = 0; i < _source.Count; i++)
        {
            object t = _source [i];

            if (t is string u)
            {
                if (u.ToUpper ().StartsWith (search.ToUpperInvariant ()))
                {
                    return i;
                }
            }
            else if (t is string s)
            {
                if (s.StartsWith (search, StringComparison.InvariantCultureIgnoreCase))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private int GetMaxLengthItem ()
    {
        if (_source is null || _source?.Count == 0)
        {
            return 0;
        }

        var maxLength = 0;

        for (var i = 0; i < _source.Count; i++)
        {
            object t = _source [i];
            int l;

            if (t is string u)
            {
                l = u.GetColumns ();
            }
            else if (t is string s)
            {
                l = s.Length;
            }
            else
            {
                l = t.ToString ().Length;
            }

            if (l > maxLength)
            {
                maxLength = l;
            }
        }

        return maxLength;
    }

    private void RenderUstr (ConsoleDriver driver, string ustr, int col, int line, int width, int start = 0)
    {
        string u = TextFormatter.ClipAndJustify (ustr, width, TextAlignment.Left);
        driver.AddStr (u);
        width -= u.GetColumns ();

        while (width-- > 0)
        {
            driver.AddRune ((Rune)' ');
        }
    }
}

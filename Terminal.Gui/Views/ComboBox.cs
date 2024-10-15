//
// ComboBox.cs: ComboBox control
//
// Authors:
//   Ross Ferguson (ross.c.ferguson@btinternet.com)
//

using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>Provides a drop-down list of items the user can select from.</summary>
public class ComboBox : View, IDesignable
{
    private readonly ComboListView _listview;
    private readonly int _minimumHeight = 2;
    private readonly TextField _search;
    private readonly ObservableCollection<object> _searchSet = [];
    private bool _autoHide = true;
    private bool _hideDropdownListOnClick;
    private int _lastSelectedItem = -1;
    private int _selectedItem = -1;
    private IListDataSource _source;
    private string _text = "";

    /// <summary>Public constructor</summary>
    public ComboBox ()
    {
        CanFocus = true;
        _search = new TextField () { CanFocus = true, TabStop = TabBehavior.NoStop };

        _listview = new ComboListView (this, HideDropdownListOnClick) { CanFocus = true, TabStop = TabBehavior.NoStop };

        _search.TextChanged += Search_Changed;

        _listview.Y = Pos.Bottom (_search);
        _listview.OpenSelectedItem += (sender, a) => SelectText ();
        _listview.Accepting += (sender, args) =>
                              {
                                  // This prevents Accepted from bubbling up to the combobox
                                  args.Cancel = true;

                                  // But OpenSelectedItem won't be fired because of that. So do it here.
                                  SelectText ();
                              };
        _listview.SelectedItemChanged += (sender, e) =>
                                         {
                                             if (!HideDropdownListOnClick && _searchSet.Count > 0)
                                             {
                                                 SetValue (_searchSet [_listview.SelectedItem]);
                                             }
                                         };
        Add (_search, _listview);

        // BUGBUG: This should not be needed; LayoutComplete will handle
        Initialized += (s, e) => ProcessLayout ();

        // On resize
        LayoutComplete += (sender, a) => ProcessLayout ();

        Added += (s, e) =>
                 {
                     // Determine if this view is hosted inside a dialog and is the only control
                     for (View view = SuperView; view != null; view = view.SuperView)
                     {
                         if (view is Dialog && SuperView is { } && SuperView.Subviews.Count == 1 && SuperView.Subviews [0] == this)
                         {
                             _autoHide = false;

                             break;
                         }
                     }

                     SetNeedsLayout ();
                     SetNeedsDisplay ();
                     ShowHideList (Text);
                 };

        // Things this view knows how to do
        AddCommand (Command.Accept, (ctx) =>
                                    {
                                        if (ctx.Data == _search)
                                        {
                                            return null;
                                        }
                                        return ActivateSelected (ctx);
                                    });
        AddCommand (Command.Toggle, () => ExpandCollapse ());
        AddCommand (Command.Expand, () => Expand ());
        AddCommand (Command.Collapse, () => Collapse ());
        AddCommand (Command.Down, () => MoveDown ());
        AddCommand (Command.Up, () => MoveUp ());
        AddCommand (Command.PageDown, () => PageDown ());
        AddCommand (Command.PageUp, () => PageUp ());
        AddCommand (Command.Start, () => MoveHome ());
        AddCommand (Command.End, () => MoveEnd ());
        AddCommand (Command.Cancel, () => CancelSelected ());
        AddCommand (Command.UnixEmulation, () => UnixEmulation ());

        // Default keybindings for this view
        KeyBindings.Add (Key.F4, Command.Toggle);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);
        KeyBindings.Add (Key.Esc, Command.Cancel);
        KeyBindings.Add (Key.U.WithCtrl, Command.UnixEmulation);
    }

    /// <inheritdoc/>
    public new ColorScheme ColorScheme
    {
        get => base.ColorScheme;
        set
        {
            _listview.ColorScheme = value;
            base.ColorScheme = value;
            SetNeedsDisplay ();
        }
    }

    /// <summary>Gets or sets if the drop-down list can be hide with a button click event.</summary>
    public bool HideDropdownListOnClick
    {
        get => _hideDropdownListOnClick;
        set => _hideDropdownListOnClick = _listview.HideDropdownListOnClick = value;
    }

    /// <summary>Gets the drop-down list state, expanded or collapsed.</summary>
    public bool IsShow { get; private set; }

    /// <summary>If set to true, no changes to the text will be allowed.</summary>
    public bool ReadOnly
    {
        get => _search.ReadOnly;
        set
        {
            _search.ReadOnly = value;

            if (_search.ReadOnly)
            {
                if (_search.ColorScheme is { })
                {
                    _search.ColorScheme = new ColorScheme (_search.ColorScheme) { Normal = _search.ColorScheme.Focus };
                }
            }
        }
    }

    /// <summary>Current search text</summary>
    public string SearchText
    {
        get => _search.Text;
        set => SetSearchText (value);
    }

    /// <summary>Gets the index of the currently selected item in the <see cref="Source"/></summary>
    /// <value>The selected item or -1 none selected.</value>
    public int SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem != value
                && (value == -1
                    || (_source is { } && value > -1 && value < _source.Count)))
            {
                _selectedItem = _lastSelectedItem = value;

                if (_selectedItem != -1)
                {
                    SetValue (_source.ToList () [_selectedItem].ToString (), true);
                }
                else
                {
                    SetValue ("", true);
                }

                OnSelectedChanged ();
            }
        }
    }

    /// <summary>Gets or sets the <see cref="IListDataSource"/> backing this <see cref="ComboBox"/>, enabling custom rendering.</summary>
    /// <value>The source.</value>
    /// <remarks>Use <see cref="SetSource{T}"/> to set a new <see cref="ObservableCollection{T}"/> source.</remarks>
    public IListDataSource Source
    {
        get => _source;
        set
        {
            _source = value;

            // Only need to refresh list if its been added to a container view
            if (SuperView is { } && SuperView.Subviews.Contains (this))
            {
                Text = string.Empty;
                SetNeedsDisplay ();
            }
        }
    }

    /// <summary>The text of the currently selected list item</summary>
    public new string Text
    {
        get => _text;
        set => SetSearchText (value);
    }

    /// <summary>
    ///     Collapses the drop-down list.  Returns true if the state changed or false if it was already collapsed and no
    ///     action was taken
    /// </summary>
    public virtual bool Collapse ()
    {
        if (!IsShow)
        {
            return false;
        }

        IsShow = false;
        HideList ();

        return true;
    }

    /// <summary>This event is raised when the drop-down list is collapsed.</summary>
    public event EventHandler Collapsed;

    /// <summary>
    ///     Expands the drop-down list.  Returns true if the state changed or false if it was already expanded and no
    ///     action was taken
    /// </summary>
    public virtual bool Expand ()
    {
        if (IsShow)
        {
            return false;
        }

        SetSearchSet ();
        IsShow = true;
        ShowList ();
        FocusSelectedItem ();

        return true;
    }

    /// <summary>This event is raised when the drop-down list is expanded.</summary>
    public event EventHandler Expanded;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs me)
    {
        if (me.Position.X == Viewport.Right - 1
            && me.Position.Y == Viewport.Top
            && me.Flags == MouseFlags.Button1Pressed
            && _autoHide)
        {
            if (IsShow)
            {
                IsShow = false;
                HideList ();
            }
            else
            {
                SetSearchSet ();

                IsShow = true;
                ShowList ();
                FocusSelectedItem ();
            }

            return me.Handled = true;
        }

        if (me.Flags == MouseFlags.Button1Pressed)
        {
            if (!_search.HasFocus)
            {
                _search.SetFocus ();
            }

            return me.Handled = true;
        }

        return false;
    }

    /// <summary>Virtual method which invokes the <see cref="Collapsed"/> event.</summary>
    public virtual void OnCollapsed () { Collapsed?.Invoke (this, EventArgs.Empty); }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        base.OnDrawContent (viewport);

        if (!_autoHide)
        {
            return;
        }

        Driver.SetAttribute (ColorScheme.Focus);
        Move (Viewport.Right - 1, 0);
        Driver.AddRune (Glyphs.DownArrow);
    }


    /// <summary>Virtual method which invokes the <see cref="Expanded"/> event.</summary>
    public virtual void OnExpanded () { Expanded?.Invoke (this, EventArgs.Empty); }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View previousFocusedView, View view)
    {
        if (newHasFocus)
        {
            if (!_search.HasFocus && !_listview.HasFocus)
            {
                _search.SetFocus ();
            }
            _search.CursorPosition = _search.Text.GetRuneCount ();
        }
        else
        {
            if (_source?.Count > 0
              && _selectedItem > -1
              && _selectedItem < _source.Count - 1
              && _text != _source.ToList () [_selectedItem].ToString ())
            {
                SetValue (_source.ToList () [_selectedItem].ToString ());
            }

            if (_autoHide && IsShow && view != this && view != _search && view != _listview)
            {
                IsShow = false;
                HideList ();
            }
            else if (_listview.TabStop?.HasFlag (TabBehavior.TabStop) ?? false)
            {
                _listview.TabStop = TabBehavior.NoStop;
            }
        }
    }

    /// <summary>Invokes the OnOpenSelectedItem event if it is defined.</summary>
    /// <returns></returns>
    public virtual bool OnOpenSelectedItem ()
    {
        string value = _search.Text;
        _lastSelectedItem = SelectedItem;
        OpenSelectedItem?.Invoke (this, new ListViewItemEventArgs (SelectedItem, value));

        return true;
    }

    /// <summary>Invokes the SelectedChanged event if it is defined.</summary>
    /// <returns></returns>
    public virtual bool OnSelectedChanged ()
    {
        // Note: Cannot rely on "listview.SelectedItem != lastSelectedItem" because the list is dynamic. 
        // So we cannot optimize. Ie: Don't call if not changed
        SelectedItemChanged?.Invoke (this, new ListViewItemEventArgs (SelectedItem, _search.Text));

        return true;
    }

    /// <summary>This event is raised when the user Double Clicks on an item or presses ENTER to open the selected item.</summary>
    public event EventHandler<ListViewItemEventArgs> OpenSelectedItem;

    /// <summary>This event is raised when the selected item in the <see cref="ComboBox"/> has changed.</summary>
    public event EventHandler<ListViewItemEventArgs> SelectedItemChanged;

    /// <summary>Sets the source of the <see cref="ComboBox"/> to an <see cref="ObservableCollection{T}"/>.</summary>
    /// <value>An object implementing the INotifyCollectionChanged and INotifyPropertyChanged interface.</value>
    /// <remarks>
    ///     Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custom
    ///     rendering.
    /// </remarks>
    public void SetSource<T> (ObservableCollection<T> source)
    {
        if (source is null)
        {
            Source = null;
        }
        else
        {
            _listview.SetSource<T> (source);
            Source = _listview.Source;
        }
    }

    private bool ActivateSelected (CommandContext ctx)
    {
        if (HasItems ())
        {
            if (SelectText ())
            {
                return false;
            }

            return RaiseAccepting (ctx) == true;
        }

        return false;
    }

    /// <summary>Internal height of dynamic search list</summary>
    /// <returns></returns>
    private int CalculateHeight ()
    {
        if (!IsInitialized || Viewport.Height == 0)
        {
            return 0;
        }

        return Math.Min (
                         Math.Max (Viewport.Height - 1, _minimumHeight - 1),
                         _searchSet?.Count > 0 ? _searchSet.Count :
                         IsShow ? Math.Max (Viewport.Height - 1, _minimumHeight - 1) : 0
                        );
    }

    private bool CancelSelected ()
    {
        if (HasFocus)
        {
            _search.SetFocus ();
        }

        if (ReadOnly || HideDropdownListOnClick)
        {
            SelectedItem = _lastSelectedItem;

            if (SelectedItem > -1 && _listview.Source?.Count > 0)
            {
                Text = _listview.Source.ToList () [SelectedItem]?.ToString ();
            }
        }
        else if (!ReadOnly)
        {
            Text = string.Empty;
            _selectedItem = _lastSelectedItem;
            OnSelectedChanged ();
        }

        return Collapse ();
    }

    /// <summary>Toggles the expand/collapse state of the sublist in the combo box</summary>
    /// <returns></returns>
    private bool ExpandCollapse ()
    {
        if (_search.HasFocus || _listview.HasFocus)
        {
            if (!IsShow)
            {
                return Expand ();
            }

            return Collapse ();
        }

        return false;
    }

    private void FocusSelectedItem ()
    {
        _listview.SelectedItem = SelectedItem > -1 ? SelectedItem : 0;
        _listview.TabStop = TabBehavior.TabStop;
        _listview.SetFocus ();
        OnExpanded ();
    }

    private int GetSelectedItemFromSource (string searchText)
    {
        if (_source is null)
        {
            return -1;
        }

        for (var i = 0; i < _searchSet.Count; i++)
        {
            if (_searchSet [i].ToString () == searchText)
            {
                return i;
            }
        }

        return -1;
    }

    private bool HasItems () { return Source?.Count > 0; }

    /// <summary>Hide the search list</summary>
    /// Consider making public
    private void HideList ()
    {
        if (_lastSelectedItem != _selectedItem)
        {
            OnOpenSelectedItem ();
        }

        Reset (true);
        _listview.Clear ();
        _listview.TabStop = TabBehavior.NoStop;
        SuperView?.MoveSubviewToStart (this);
        Rectangle rect = _listview.ViewportToScreen (_listview.IsInitialized ? _listview.Viewport : Rectangle.Empty);
        SuperView?.SetNeedsDisplay (rect);
        OnCollapsed ();
    }

    private bool? MoveDown ()
    {
        if (_search.HasFocus)
        {
            // jump to list
            if (_searchSet?.Count > 0)
            {
                _listview.TabStop = TabBehavior.TabStop;
                _listview.SetFocus ();

                if (_listview.SelectedItem > -1)
                {
                    SetValue (_searchSet [_listview.SelectedItem]);
                }
                else
                {
                    _listview.SelectedItem = 0;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        return null;
    }

    private bool? MoveEnd ()
    {
        if (!IsShow && _search.HasFocus)
        {
            return null;
        }

        if (HasItems ())
        {
            _listview.MoveEnd ();
        }

        return true;
    }

    private bool? MoveHome ()
    {
        if (!IsShow && _search.HasFocus)
        {
            return null;
        }

        if (HasItems ())
        {
            _listview.MoveHome ();
        }

        return true;
    }

    private bool? MoveUp ()
    {
        if (HasItems ())
        {
            return _listview.MoveUp ();
        }

        return false;
    }

    private bool? MoveUpList ()
    {
        if (_listview.HasFocus && _listview.SelectedItem == 0 && _searchSet?.Count > 0) // jump back to search
        {
            _search.CursorPosition = _search.Text.GetRuneCount ();
            _search.SetFocus ();
        }
        else
        {
            MoveUp ();
        }

        return true;
    }

    private bool PageDown ()
    {
        if (HasItems ())
        {
            _listview.MovePageDown ();
        }

        return true;
    }

    private bool PageUp ()
    {
        if (HasItems ())
        {
            _listview.MovePageUp ();
        }

        return true;
    }

    // TODO: Upgrade Combobox to use Dim.Auto instead of all this stuff.
    private void ProcessLayout ()
    {
        if (Viewport.Height < _minimumHeight && (Height is null || Height is DimAbsolute))
        {
            Height = _minimumHeight;
        }

        // BUGBUG: This uses Viewport. Should use ContentSize
        if ((!_autoHide && Viewport.Width > 0 && _search.Frame.Width != Viewport.Width)
            || (_autoHide && Viewport.Width > 0 && _search.Frame.Width != Viewport.Width - 1))
        {
            _search.Width = _listview.Width = _autoHide ? Viewport.Width - 1 : Viewport.Width;
            _listview.Height = CalculateHeight ();
            _search.SetRelativeLayout (GetContentSize ());
            _listview.SetRelativeLayout (GetContentSize ());
        }
    }

    /// <summary>Reset to full original list</summary>
    private void Reset (bool keepSearchText = false)
    {
        if (!keepSearchText)
        {
            SetSearchText (string.Empty);
        }

        ResetSearchSet ();

        _listview.SetSource (_searchSet);
        _listview.Height = CalculateHeight ();

        if (Subviews.Count > 0 && HasFocus)
        {
            _search.SetFocus ();
        }
    }

    private void ResetSearchSet (bool noCopy = false)
    {
        _listview.SuspendCollectionChangedEvent ();
        _searchSet.Clear ();
        _listview.ResumeSuspendCollectionChangedEvent ();

        if (_autoHide || noCopy)
        {
            return;
        }

        SetSearchSet ();
    }

    private void Search_Changed (object sender, EventArgs e)
    {
        if (_source is null)
        {
            // Object initialization
            return;
        }

        ShowHideList (Text);
    }

    private void ShowHideList (string oldText)
    {
        if (string.IsNullOrEmpty (_search.Text) && string.IsNullOrEmpty (oldText))
        {
            ResetSearchSet ();
        }
        else if (_search.Text != oldText)
        {
            if (_search.Text.Length < oldText.Length)
            {
                _selectedItem = -1;
            }

            IsShow = true;
            ResetSearchSet (true);

            if (!string.IsNullOrEmpty (_search.Text))
            {
                _listview.SuspendCollectionChangedEvent ();

                foreach (object item in _source.ToList ())
                {
                    // Iterate to preserver object type and force deep copy
                    if (item.ToString ()
                            .StartsWith (
                                         _search.Text,
                                         StringComparison.CurrentCultureIgnoreCase
                                        ))
                    {
                        _searchSet.Add (item);
                    }
                }

                _listview.ResumeSuspendCollectionChangedEvent ();
            }
        }

        if (HasFocus)
        {
            ShowList ();
        }
        else if (_autoHide)
        {
            IsShow = false;
            HideList ();
        }
    }

    private bool SelectText ()
    {
        IsShow = false;
        _listview.TabStop = TabBehavior.NoStop;

        if (_listview.Source.Count == 0 || (_searchSet?.Count ?? 0) == 0)
        {
            _text = "";
            HideList ();
            IsShow = false;

            return false;
        }

        SetValue (_listview.SelectedItem > -1 ? _searchSet [_listview.SelectedItem] : _text);
        _search.CursorPosition = _search.Text.GetColumns ();
        ShowHideList (Text);
        OnOpenSelectedItem ();
        Reset (true);
        HideList ();
        IsShow = false;

        return true;
    }

    private void SetSearchSet ()
    {
        if (Source is null)
        {
            return;
        }

        // PERF: At the request of @dodexahedron in the comment https://github.com/gui-cs/Terminal.Gui/pull/3552#discussion_r1648112410.
        _listview.SuspendCollectionChangedEvent ();

        // force deep copy
        foreach (object item in Source.ToList ())
        {
            _searchSet.Add (item);
        }

        _listview.ResumeSuspendCollectionChangedEvent ();
    }

    // Sets the search text field Text as well as our own Text property
    private void SetSearchText (string value)
    {
        _search.Text = value;
        _text = value;
    }

    private void SetValue (object text, bool isFromSelectedItem = false)
    {
        // TOOD: The fact we have to suspend events to change the text makes this feel very hacky.
        _search.TextChanged -= Search_Changed;
        // Note we set _text, to avoid set_Text from setting _search.Text again
        _text = _search.Text = text.ToString ();
        _search.CursorPosition = 0;
        _search.TextChanged += Search_Changed;

        if (!isFromSelectedItem)
        {
            _selectedItem = GetSelectedItemFromSource (_text);
            OnSelectedChanged ();
        }
    }

    /// <summary>Show the search list</summary>
    /// Consider making public
    private void ShowList ()
    {
        _listview.SuspendCollectionChangedEvent ();
        _listview.SetSource (_searchSet);
        _listview.ResumeSuspendCollectionChangedEvent ();

        _listview.Clear ();
        _listview.Height = CalculateHeight ();
        SuperView?.MoveSubviewToStart (this);
    }

    private bool UnixEmulation ()
    {
        // Unix emulation
        Reset ();

        return true;
    }

    private class ComboListView : ListView
    {
        private ComboBox _container;
        private bool _hideDropdownListOnClick;
        private int _highlighted = -1;
        private bool _isFocusing;
        public ComboListView (ComboBox container, bool hideDropdownListOnClick) { SetInitialProperties (container, hideDropdownListOnClick); }

        public ComboListView (ComboBox container, ObservableCollection<string> source, bool hideDropdownListOnClick)
        {
            Source = new ListWrapper<string> (source);
            SetInitialProperties (container, hideDropdownListOnClick);
        }

        public bool HideDropdownListOnClick
        {
            get => _hideDropdownListOnClick;
            set => _hideDropdownListOnClick = WantContinuousButtonPressed = value;
        }

        protected override bool OnMouseEvent (MouseEventArgs me)
        {
            bool isMousePositionValid = IsMousePositionValid (me);

            var res = false;

            if (isMousePositionValid)
            {
                // We're derived from ListView and it overrides OnMouseEvent, so we need to call it
                res = base.OnMouseEvent (me);
            }

            if (HideDropdownListOnClick && me.Flags == MouseFlags.Button1Clicked)
            {
                if (!isMousePositionValid && !_isFocusing)
                {
                    _container.IsShow = false;
                    _container.HideList ();
                }
                else if (isMousePositionValid)
                {
                    OnOpenSelectedItem ();
                }
                else
                {
                    _isFocusing = false;
                }

                return true;
            }

            if (me.Flags == MouseFlags.ReportMousePosition && HideDropdownListOnClick)
            {
                if (isMousePositionValid)
                {
                    _highlighted = Math.Min (TopItem + me.Position.Y, Source.Count);
                    SetNeedsDisplay ();
                }

                _isFocusing = false;

                return true;
            }

            return res;
        }

        public override void OnDrawContent (Rectangle viewport)
        {
            Attribute current = ColorScheme.Focus;
            Driver.SetAttribute (current);
            Move (0, 0);
            Rectangle f = Frame;
            int item = TopItem;
            bool focused = HasFocus;
            int col = AllowsMarking ? 2 : 0;
            int start = LeftItem;

            for (var row = 0; row < f.Height; row++, item++)
            {
                bool isSelected = item == _container.SelectedItem;
                bool isHighlighted = _hideDropdownListOnClick && item == _highlighted;

                Attribute newcolor;

                if (isHighlighted || (isSelected && !_hideDropdownListOnClick))
                {
                    newcolor = focused ? ColorScheme.Focus : ColorScheme.HotNormal;
                }
                else if (isSelected && _hideDropdownListOnClick)
                {
                    newcolor = focused ? ColorScheme.HotFocus : ColorScheme.HotNormal;
                }
                else
                {
                    newcolor = focused ? GetNormalColor () : GetNormalColor ();
                }

                if (newcolor != current)
                {
                    Driver.SetAttribute (newcolor);
                    current = newcolor;
                }

                Move (0, row);

                if (Source is null || item >= Source.Count)
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

                    if (AllowsMarking)
                    {
                        Driver.AddRune (
                                        Source.IsMarked (item) ? AllowsMultipleSelection ? Glyphs.CheckStateChecked : Glyphs.Selected :
                                        AllowsMultipleSelection ? Glyphs.CheckStateUnChecked : Glyphs.UnSelected
                                       );
                        Driver.AddRune ((Rune)' ');
                    }

                    Source.Render (this, Driver, isSelected, item, col, row, f.Width - col, start);
                }
            }
        }

        protected override void OnHasFocusChanged (bool newHasFocus, [CanBeNull] View previousFocusedView, [CanBeNull] View focusedVew)
        {
            if (newHasFocus)
            {
                if (_hideDropdownListOnClick)
                {
                    _isFocusing = true;
                    _highlighted = _container.SelectedItem;
                    Application.GrabMouse (this);
                }
            }
            else
            {
                if (_hideDropdownListOnClick)
                {
                    _isFocusing = false;
                    _highlighted = _container.SelectedItem;
                    Application.UngrabMouse ();
                }
            }
        }

        public override bool OnSelectedChanged ()
        {
            bool res = base.OnSelectedChanged ();

            _highlighted = SelectedItem;

            return res;
        }

        private bool IsMousePositionValid (MouseEventArgs me)
        {
            if (me.Position.X >= 0 && me.Position.X < Frame.Width && me.Position.Y >= 0 && me.Position.Y < Frame.Height)
            {
                return true;
            }

            return false;
        }

        private void SetInitialProperties (ComboBox container, bool hideDropdownListOnClick)
        {
            _container = container
                         ?? throw new ArgumentNullException (
                                                             nameof (container),
                                                             "ComboBox container cannot be null."
                                                            );
            HideDropdownListOnClick = hideDropdownListOnClick;
            AddCommand (Command.Up, () => _container.MoveUpList ());
        }
    }

    /// <inheritdoc />
    public bool EnableForDesign ()
    {
        var source = new ObservableCollection<string> (["Combo Item 1", "Combo Item two", "Combo Item Quattro", "Last Combo Item"]);
        SetSource (source);
        Height = Dim.Auto (DimAutoStyle.Content, minimumContentDim: source.Count + 1);

        return true;
    }
}

﻿//
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
        _search = new TextField ();
        _listview = new ComboListView (this, HideDropdownListOnClick) { CanFocus = true, TabStop = false };

        _search.TextChanged += Search_Changed;
        _search.Accept += Search_Accept;

        _listview.Y = Pos.Bottom (_search);
        _listview.OpenSelectedItem += (sender, a) => Selected ();

        Add (_search, _listview);

        // BUGBUG: This should not be needed; LayoutComplete will handle
        Initialized += (s, e) => ProcessLayout ();

        // On resize
        LayoutComplete += (sender, a) => ProcessLayout ();
        ;

        _listview.SelectedItemChanged += (sender, e) =>
                                         {
                                             if (!HideDropdownListOnClick && _searchSet.Count > 0)
                                             {
                                                 SetValue (_searchSet [_listview.SelectedItem]);
                                             }
                                         };

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
                     Search_Changed (this, new StateEventArgs<string> (string.Empty, Text));
                 };

        // Things this view knows how to do
        AddCommand (Command.Accept, () => ActivateSelected ());
        AddCommand (Command.ToggleExpandCollapse, () => ExpandCollapse ());
        AddCommand (Command.Expand, () => Expand ());
        AddCommand (Command.Collapse, () => Collapse ());
        AddCommand (Command.LineDown, () => MoveDown ());
        AddCommand (Command.LineUp, () => MoveUp ());
        AddCommand (Command.PageDown, () => PageDown ());
        AddCommand (Command.PageUp, () => PageUp ());
        AddCommand (Command.TopHome, () => MoveHome ());
        AddCommand (Command.BottomEnd, () => MoveEnd ());
        AddCommand (Command.Cancel, () => CancelSelected ());
        AddCommand (Command.UnixEmulation, () => UnixEmulation ());

        // Default keybindings for this view
        KeyBindings.Add (Key.Enter, Command.Accept);
        KeyBindings.Add (Key.F4, Command.ToggleExpandCollapse);
        KeyBindings.Add (Key.CursorDown, Command.LineDown);
        KeyBindings.Add (Key.CursorUp, Command.LineUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.Home, Command.TopHome);
        KeyBindings.Add (Key.End, Command.BottomEnd);
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

    /// <summary>Gets the drop down list state, expanded or collapsed.</summary>
    public bool IsShow { get; private set; }

    /// <summary>If set to true its not allow any changes in the text.</summary>
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
                SelectedItem = -1;
                _search.Text = string.Empty;
                Search_Changed (this, new StateEventArgs<string> (string.Empty, _search.Text));
                SetNeedsDisplay ();
            }
        }
    }

    /// <summary>The currently selected list item</summary>
    public new string Text
    {
        get => _text;
        set => SetSearchText (value);
    }

    /// <summary>
    ///     Collapses the drop down list.  Returns true if the state chagned or false if it was already collapsed and no
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
    ///     Expands the drop down list.  Returns true if the state chagned or false if it was already expanded and no
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
    protected internal override bool OnMouseEvent (MouseEvent me)
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

    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        if (!_search.HasFocus && !_listview.HasFocus)
        {
            _search.SetFocus ();
        }

        _search.CursorPosition = _search.Text.GetRuneCount ();

        return base.OnEnter (view);
    }

    /// <summary>Virtual method which invokes the <see cref="Expanded"/> event.</summary>
    public virtual void OnExpanded () { Expanded?.Invoke (this, EventArgs.Empty); }

    /// <inheritdoc/>
    public override bool OnLeave (View view)
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
        else if (_listview.TabStop)
        {
            _listview.TabStop = false;
        }

        return base.OnLeave (view);
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

    private bool ActivateSelected ()
    {
        if (HasItems ())
        {
            Selected ();

            return true;
        }

        return false;
    }

    /// <summary>Internal height of dynamic search list</summary>
    /// <returns></returns>
    private int CalculatetHeight ()
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
        _search.SetFocus ();

        if (ReadOnly || HideDropdownListOnClick)
        {
            SelectedItem = _lastSelectedItem;

            if (SelectedItem > -1 && _listview.Source?.Count > 0)
            {
                _search.Text = _text = _listview.Source.ToList () [SelectedItem].ToString ();
            }
        }
        else if (!ReadOnly)
        {
            _search.Text = _text = "";
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
        _listview.TabStop = true;
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
        _listview.TabStop = false;
        SuperView?.SendSubviewToBack (this);
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
                _listview.TabStop = true;
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
                _listview.TabStop = false;
                SuperView?.FocusNext ();
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
            _listview.MoveUp ();
        }

        return true;
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
            _listview.Height = CalculatetHeight ();
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
        _listview.Height = CalculatetHeight ();

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

    // Tell TextField to handle Accept Command (Enter)
    void Search_Accept (object sender, CancelEventArgs e) { e.Cancel = true; }

    private void Search_Changed (object sender, StateEventArgs<string> e)
    {
        if (_source is null)
        {
            // Object initialization
            return;
        }

        if (string.IsNullOrEmpty (_search.Text) && string.IsNullOrEmpty (e.OldValue))
        {
            ResetSearchSet ();
        }
        else if (_search.Text != e.OldValue)
        {
            if (_search.Text.Length < e.OldValue.Length)
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

    private void Selected ()
    {
        IsShow = false;
        _listview.TabStop = false;

        if (_listview.Source.Count == 0 || (_searchSet?.Count ?? 0) == 0)
        {
            _text = "";
            HideList ();
            IsShow = false;

            return;
        }

        SetValue (_listview.SelectedItem > -1 ? _searchSet [_listview.SelectedItem] : _text);
        _search.CursorPosition = _search.Text.GetColumns ();
        Search_Changed (this, new StateEventArgs<string> (_search.Text, _search.Text));
        OnOpenSelectedItem ();
        Reset (true);
        HideList ();
        IsShow = false;
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

    private void SetSearchText (string value) { _search.Text = _text = value; }

    private void SetValue (object text, bool isFromSelectedItem = false)
    {
        _search.TextChanged -= Search_Changed;
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
        _listview.Height = CalculatetHeight ();
        SuperView?.BringSubviewToFront (this);
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

        protected internal override bool OnMouseEvent (MouseEvent me)
        {
            var res = false;
            bool isMousePositionValid = IsMousePositionValid (me);

            if (isMousePositionValid)
            {
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
                                        Source.IsMarked (item) ? AllowsMultipleSelection ? Glyphs.Checked : Glyphs.Selected :
                                        AllowsMultipleSelection ? Glyphs.UnChecked : Glyphs.UnSelected
                                       );
                        Driver.AddRune ((Rune)' ');
                    }

                    Source.Render (this, Driver, isSelected, item, col, row, f.Width - col, start);
                }
            }
        }

        public override bool OnEnter (View view)
        {
            if (_hideDropdownListOnClick)
            {
                _isFocusing = true;
                _highlighted = _container.SelectedItem;
                Application.GrabMouse (this);
            }

            return base.OnEnter (view);
        }

        public override bool OnLeave (View view)
        {
            if (_hideDropdownListOnClick)
            {
                _isFocusing = false;
                _highlighted = _container.SelectedItem;
                Application.UngrabMouse ();
            }

            return base.OnLeave (view);
        }

        public override bool OnSelectedChanged ()
        {
            bool res = base.OnSelectedChanged ();

            _highlighted = SelectedItem;

            return res;
        }

        private bool IsMousePositionValid (MouseEvent me)
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
            AddCommand (Command.LineUp, () => _container.MoveUpList ());
        }
    }

    /// <inheritdoc />
    public bool Enable ()
    {
        var source = new ObservableCollection<string> (["Combo Item 1", "Combo Item two", "Combo Item Quattro", "Last Combo Item"]);
        SetSource (source);
        Height = Dim.Auto (DimAutoStyle.Content, minimumContentDim: source.Count + 1);

        return true;
    }
}

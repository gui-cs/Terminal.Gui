//
// ComboBox.cs: ComboBox control
//
// Authors:
//   Ross Ferguson (ross.c.ferguson@btinternet.com)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Terminal.Gui;

/// <summary>
/// Provides a drop-down list of items the user can select from.
/// </summary>
public class ComboBox : View {
	class ComboListView : ListView {
		int _highlighted = -1;
		bool _isFocusing;
		ComboBox _container;
		bool _hideDropdownListOnClick;

		public ComboListView (ComboBox container, bool hideDropdownListOnClick) => SetInitialProperties (container, hideDropdownListOnClick);

		public ComboListView (ComboBox container, Rect rect, IList source, bool hideDropdownListOnClick) : base (rect, source) => SetInitialProperties (container, hideDropdownListOnClick);

		public ComboListView (ComboBox container, IList source, bool hideDropdownListOnClick) : base (source) => SetInitialProperties (container, hideDropdownListOnClick);

		void SetInitialProperties (ComboBox container, bool hideDropdownListOnClick)
		{
			_container = container ?? throw new ArgumentNullException (nameof (container), "ComboBox container cannot be null.");
			HideDropdownListOnClick = hideDropdownListOnClick;
			AddCommand (Command.LineUp, () => _container.MoveUpList ());
		}

		public bool HideDropdownListOnClick {
			get => _hideDropdownListOnClick;
			set => _hideDropdownListOnClick = WantContinuousButtonPressed = value;
		}

		public override bool MouseEvent (MouseEvent me)
		{
			bool res = false;
			bool isMousePositionValid = IsMousePositionValid (me);

			if (isMousePositionValid) {
				res = base.MouseEvent (me);
			}

			if (HideDropdownListOnClick && me.Flags == MouseFlags.Button1Clicked) {
				if (!isMousePositionValid && !_isFocusing) {
					_container._isShow = false;
					_container.HideList ();
				} else if (isMousePositionValid) {
					OnOpenSelectedItem ();
				} else {
					_isFocusing = false;
				}
				return true;
			} else if (me.Flags == MouseFlags.ReportMousePosition && HideDropdownListOnClick) {
				if (isMousePositionValid) {
					_highlighted = Math.Min (TopItem + me.Y, Source.Count);
					SetNeedsDisplay ();
				}
				_isFocusing = false;
				return true;
			}

			return res;
		}

		bool IsMousePositionValid (MouseEvent me)
		{
			if (me.X >= 0 && me.X < Frame.Width && me.Y >= 0 && me.Y < Frame.Height) {
				return true;
			}
			return false;
		}

		public override void OnDrawContent (Rect contentArea)
		{
			var current = ColorScheme.Focus;
			Driver.SetAttribute (current);
			Move (0, 0);
			var f = Frame;
			int item = TopItem;
			bool focused = HasFocus;
			int col = AllowsMarking ? 2 : 0;
			int start = LeftItem;

			for (int row = 0; row < f.Height; row++, item++) {
				bool isSelected = item == _container.SelectedItem;
				bool isHighlighted = _hideDropdownListOnClick && item == _highlighted;

				Attribute newcolor;
				if (isHighlighted || isSelected && !_hideDropdownListOnClick) {
					newcolor = focused ? ColorScheme.Focus : ColorScheme.HotNormal;
				} else if (isSelected && _hideDropdownListOnClick) {
					newcolor = focused ? ColorScheme.HotFocus : ColorScheme.HotNormal;
				} else {
					newcolor = focused ? GetNormalColor () : GetNormalColor ();
				}

				if (newcolor != current) {
					Driver.SetAttribute (newcolor);
					current = newcolor;
				}

				Move (0, row);
				if (Source == null || item >= Source.Count) {
					for (int c = 0; c < f.Width; c++) {
						Driver.AddRune ((Rune)' ');
					}
				} else {
					var rowEventArgs = new ListViewRowEventArgs (item);
					OnRowRender (rowEventArgs);
					if (rowEventArgs.RowAttribute != null && current != rowEventArgs.RowAttribute) {
						current = (Attribute)rowEventArgs.RowAttribute;
						Driver.SetAttribute (current);
					}
					if (AllowsMarking) {
						Driver.AddRune (Source.IsMarked (item) ? AllowsMultipleSelection ? Glyphs.Checked : Glyphs.Selected : AllowsMultipleSelection ? Glyphs.UnChecked : Glyphs.UnSelected);
						Driver.AddRune ((Rune)' ');
					}
					Source.Render (this, Driver, isSelected, item, col, row, f.Width - col, start);
				}
			}
		}

		public override bool OnEnter (View view)
		{
			if (_hideDropdownListOnClick) {
				_isFocusing = true;
				_highlighted = _container.SelectedItem;
				Application.GrabMouse (this);
			}

			return base.OnEnter (view);
		}

		public override bool OnLeave (View view)
		{
			if (_hideDropdownListOnClick) {
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
	}

	IListDataSource _source;

	/// <summary>
	/// Gets or sets the <see cref="IListDataSource"/> backing this <see cref="ComboBox"/>, enabling custom rendering.
	/// </summary>
	/// <value>The source.</value>
	/// <remarks>
	///  Use <see cref="SetSource"/> to set a new <see cref="IList"/> source.
	/// </remarks>
	public IListDataSource Source {
		get => _source;
		set {
			_source = value;

			// Only need to refresh list if its been added to a container view
			if (SuperView != null && SuperView.Subviews.Contains (this)) {
				SelectedItem = -1;
				_search.Text = "";
				Search_Changed (this, new TextChangedEventArgs (""));
				SetNeedsDisplay ();
			}
		}
	}

	/// <summary>
	/// Sets the source of the <see cref="ComboBox"/> to an <see cref="IList"/>.
	/// </summary>
	/// <value>An object implementing the IList interface.</value>
	/// <remarks>
	///  Use the <see cref="Source"/> property to set a new <see cref="IListDataSource"/> source and use custome rendering.
	/// </remarks>
	public void SetSource (IList source)
	{
		if (source == null) {
			Source = null;
		} else {
			_listview.SetSource (source);
			Source = _listview.Source;
		}
	}

	/// <summary>
	/// This event is raised when the selected item in the <see cref="ComboBox"/> has changed.
	/// </summary>
	public event EventHandler<ListViewItemEventArgs> SelectedItemChanged;

	/// <summary>
	/// This event is raised when the drop-down list is expanded.
	/// </summary>
	public event EventHandler Expanded;

	/// <summary>
	/// This event is raised when the drop-down list is collapsed.
	/// </summary>
	public event EventHandler Collapsed;

	/// <summary>
	/// This event is raised when the user Double Clicks on an item or presses ENTER to open the selected item.
	/// </summary>
	public event EventHandler<ListViewItemEventArgs> OpenSelectedItem;

	readonly IList _searchset = new List<object> ();
	string _text = "";
	readonly TextField _search;
	readonly ComboListView _listview;
	bool _autoHide = true;
	readonly int _minimumHeight = 2;

	/// <summary>
	/// Public constructor
	/// </summary>
	public ComboBox () : this (string.Empty) { }

	/// <summary>
	/// Public constructor
	/// </summary>
	/// <param name="text"></param>
	public ComboBox (string text) : base ()
	{
		_search = new TextField ("");
		_listview = new ComboListView (this, HideDropdownListOnClick) { CanFocus = true, TabStop = false };

		SetInitialProperties ();
		Text = text;
	}

	/// <summary>
	/// Public constructor
	/// </summary>
	/// <param name="rect"></param>
	/// <param name="source"></param>
	public ComboBox (Rect rect, IList source) : base (rect)
	{
		_search = new TextField ("") { Width = rect.Width };
		_listview = new ComboListView (this, rect, source, HideDropdownListOnClick) { ColorScheme = Colors.ColorSchemes ["Base"] };

		SetInitialProperties ();
		SetSource (source);
	}

	/// <summary>
	/// Initialize with the source.
	/// </summary>
	/// <param name="source">The source.</param>
	public ComboBox (IList source) : this (string.Empty)
	{
		_search = new TextField ("");
		_listview = new ComboListView (this, source, HideDropdownListOnClick) { ColorScheme = Colors.ColorSchemes ["Base"] };

		SetInitialProperties ();
		SetSource (source);
	}

	void SetInitialProperties ()
	{
		_search.TextChanged += Search_Changed;

		_listview.Y = Pos.Bottom (_search);
		_listview.OpenSelectedItem += (object sender, ListViewItemEventArgs a) => Selected ();

		Add (_search, _listview);

		// On resize
		LayoutComplete += (object sender, LayoutEventArgs a) => {
			if (Bounds.Height < _minimumHeight && (Height == null || Height is Dim.DimAbsolute)) {
				Height = _minimumHeight;
			}
			if (!_autoHide && Bounds.Width > 0 && _search.Frame.Width != Bounds.Width ||
			_autoHide && Bounds.Width > 0 && _search.Frame.Width != Bounds.Width - 1) {
				_search.Width = _listview.Width = _autoHide ? Bounds.Width - 1 : Bounds.Width;
				_listview.Height = CalculatetHeight ();
				_search.SetRelativeLayout (Bounds);
				_listview.SetRelativeLayout (Bounds);
			}
		};

		_listview.SelectedItemChanged += (object sender, ListViewItemEventArgs e) => {

			if (!HideDropdownListOnClick && _searchset.Count > 0) {
				SetValue (_searchset [_listview.SelectedItem]);
			}
		};

		Added += (s, e) => {

			// Determine if this view is hosted inside a dialog and is the only control
			for (var view = SuperView; view != null; view = view.SuperView) {
				if (view is Dialog && SuperView != null && SuperView.Subviews.Count == 1 && SuperView.Subviews [0] == this) {
					_autoHide = false;
					break;
				}
			}

			SetNeedsLayout ();
			SetNeedsDisplay ();
			Search_Changed (this, new TextChangedEventArgs (Text));
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
		KeyBindings.Add (KeyCode.Enter, Command.Accept);
		KeyBindings.Add (KeyCode.F4, Command.ToggleExpandCollapse);
		KeyBindings.Add (KeyCode.CursorDown, Command.LineDown);
		KeyBindings.Add (KeyCode.CursorUp, Command.LineUp);
		KeyBindings.Add (KeyCode.PageDown, Command.PageDown);
		KeyBindings.Add (KeyCode.PageUp, Command.PageUp);
		KeyBindings.Add (KeyCode.Home, Command.TopHome);
		KeyBindings.Add (KeyCode.End, Command.BottomEnd);
		KeyBindings.Add (KeyCode.Esc, Command.Cancel);
		KeyBindings.Add (KeyCode.U | KeyCode.CtrlMask, Command.UnixEmulation);
	}

	bool _isShow = false;
	int _selectedItem = -1;
	int _lastSelectedItem = -1;
	bool _hideDropdownListOnClick;

	/// <summary>
	/// Gets the index of the currently selected item in the <see cref="Source"/>
	/// </summary>
	/// <value>The selected item or -1 none selected.</value>
	public int SelectedItem {
		get => _selectedItem;
		set {
			if (_selectedItem != value && (value == -1
							|| _source != null && value > -1 && value < _source.Count)) {

				_selectedItem = _lastSelectedItem = value;
				if (_selectedItem != -1) {
					SetValue (_source.ToList () [_selectedItem].ToString (), true);
				} else {
					SetValue ("", true);
				}
				OnSelectedChanged ();
			}
		}
	}

	/// <summary>
	/// Gets the drop down list state, expanded or collapsed.
	/// </summary>
	public bool IsShow => _isShow;

	///<inheritdoc/>
	public new ColorScheme ColorScheme {
		get => base.ColorScheme;
		set {
			_listview.ColorScheme = value;
			base.ColorScheme = value;
			SetNeedsDisplay ();
		}
	}

	/// <summary>
	///If set to true its not allow any changes in the text.
	/// </summary>
	public bool ReadOnly {
		get => _search.ReadOnly;
		set {
			_search.ReadOnly = value;
			if (_search.ReadOnly) {
				if (_search.ColorScheme != null) {
					_search.ColorScheme = new ColorScheme (_search.ColorScheme) {
						Normal = _search.ColorScheme.Focus
					};
				}
			}
		}
	}

	/// <summary>
	/// Gets or sets if the drop-down list can be hide with a button click event.
	/// </summary>
	public bool HideDropdownListOnClick {
		get => _hideDropdownListOnClick;
		set => _hideDropdownListOnClick = _listview.HideDropdownListOnClick = value;
	}

	///<inheritdoc/>
	public override bool MouseEvent (MouseEvent me)
	{
		if (me.X == Bounds.Right - 1 && me.Y == Bounds.Top && me.Flags == MouseFlags.Button1Pressed
		&& _autoHide) {

			if (_isShow) {
				_isShow = false;
				HideList ();
			} else {
				SetSearchSet ();

				_isShow = true;
				ShowList ();
				FocusSelectedItem ();
			}

			return true;
		} else if (me.Flags == MouseFlags.Button1Pressed) {
			if (!_search.HasFocus) {
				_search.SetFocus ();
			}

			return true;
		}

		return false;
	}

	void FocusSelectedItem ()
	{
		_listview.SelectedItem = SelectedItem > -1 ? SelectedItem : 0;
		_listview.TabStop = true;
		_listview.SetFocus ();
		OnExpanded ();
	}

	/// <summary>
	/// Virtual method which invokes the <see cref="Expanded"/> event.
	/// </summary>
	public virtual void OnExpanded () => Expanded?.Invoke (this, EventArgs.Empty);

	/// <summary>
	/// Virtual method which invokes the <see cref="Collapsed"/> event.
	/// </summary>
	public virtual void OnCollapsed () => Collapsed?.Invoke (this, EventArgs.Empty);

	///<inheritdoc/>
	public override bool OnEnter (View view)
	{
		if (!_search.HasFocus && !_listview.HasFocus) {
			_search.SetFocus ();
		}

		_search.CursorPosition = _search.Text.GetRuneCount ();

		return base.OnEnter (view);
	}

	///<inheritdoc/>
	public override bool OnLeave (View view)
	{
		if (_source?.Count > 0 && _selectedItem > -1 && _selectedItem < _source.Count - 1
		&& _text != _source.ToList () [_selectedItem].ToString ()) {

			SetValue (_source.ToList () [_selectedItem].ToString ());
		}
		if (_autoHide && _isShow && view != this && view != _search && view != _listview) {
			_isShow = false;
			HideList ();
		} else if (_listview.TabStop) {
			_listview.TabStop = false;
		}

		return base.OnLeave (view);
	}

	/// <summary>
	/// Invokes the SelectedChanged event if it is defined.
	/// </summary>
	/// <returns></returns>
	public virtual bool OnSelectedChanged ()
	{
		// Note: Cannot rely on "listview.SelectedItem != lastSelectedItem" because the list is dynamic. 
		// So we cannot optimize. Ie: Don't call if not changed
		SelectedItemChanged?.Invoke (this, new ListViewItemEventArgs (SelectedItem, _search.Text));

		return true;
	}

	/// <summary>
	/// Invokes the OnOpenSelectedItem event if it is defined.
	/// </summary>
	/// <returns></returns>
	public virtual bool OnOpenSelectedItem ()
	{
		string value = _search.Text;
		_lastSelectedItem = SelectedItem;
		OpenSelectedItem?.Invoke (this, new ListViewItemEventArgs (SelectedItem, value));

		return true;
	}

	///<inheritdoc/>
	public override void OnDrawContent (Rect contentArea)
	{
		base.OnDrawContent (contentArea);

		if (!_autoHide) {
			return;
		}

		Driver.SetAttribute (ColorScheme.Focus);
		Move (Bounds.Right - 1, 0);
		Driver.AddRune (Glyphs.DownArrow);
	}

	bool UnixEmulation ()
	{
		// Unix emulation
		Reset ();
		return true;
	}

	bool CancelSelected ()
	{
		_search.SetFocus ();
		if (ReadOnly || HideDropdownListOnClick) {
			SelectedItem = _lastSelectedItem;
			if (SelectedItem > -1 && _listview.Source?.Count > 0) {
				_search.Text = _text = _listview.Source.ToList () [SelectedItem].ToString ();
			}
		} else if (!ReadOnly) {
			_search.Text = _text = "";
			_selectedItem = _lastSelectedItem;
			OnSelectedChanged ();
		}
		return Collapse ();
	}

	bool? MoveEnd ()
	{
		if (!_isShow && _search.HasFocus) {
			return null;
		}
		if (HasItems ()) {
			_listview.MoveEnd ();
		}
		return true;
	}

	bool? MoveHome ()
	{
		if (!_isShow && _search.HasFocus) {
			return null;
		}
		if (HasItems ()) {
			_listview.MoveHome ();
		}
		return true;
	}

	bool PageUp ()
	{
		if (HasItems ()) {
			_listview.MovePageUp ();
		}
		return true;
	}

	bool PageDown ()
	{
		if (HasItems ()) {
			_listview.MovePageDown ();
		}
		return true;
	}

	bool? MoveUp ()
	{
		if (HasItems ()) {
			_listview.MoveUp ();
		}
		return true;
	}

	bool? MoveUpList ()
	{
		if (_listview.HasFocus && _listview.SelectedItem == 0 && _searchset?.Count > 0) // jump back to search
		{
			_search.CursorPosition = _search.Text.GetRuneCount ();
			_search.SetFocus ();
		} else {
			MoveUp ();
		}

		return true;
	}

	bool? MoveDown ()
	{
		if (_search.HasFocus) {
			// jump to list
			if (_searchset?.Count > 0) {
				_listview.TabStop = true;
				_listview.SetFocus ();
				if (_listview.SelectedItem > -1) {
					SetValue (_searchset [_listview.SelectedItem]);
				} else {
					_listview.SelectedItem = 0;
				}
			} else {
				_listview.TabStop = false;
				SuperView?.FocusNext ();
			}
			return true;
		}
		return null;
	}

	/// <summary>
	/// Toggles the expand/collapse state of the sublist in the combo box
	/// </summary>
	/// <returns></returns>
	bool ExpandCollapse ()
	{
		if (_search.HasFocus || _listview.HasFocus) {
			if (!_isShow) {
				return Expand ();
			} else {
				return Collapse ();
			}
		}
		return false;
	}

	bool ActivateSelected ()
	{
		if (HasItems ()) {
			Selected ();
			return true;
		}
		return false;
	}

	bool HasItems () => Source?.Count > 0;

	/// <summary>
	/// Collapses the drop down list.  Returns true if the state chagned or false
	/// if it was already collapsed and no action was taken
	/// </summary>
	public virtual bool Collapse ()
	{
		if (!_isShow) {
			return false;
		}

		_isShow = false;
		HideList ();
		return true;
	}

	/// <summary>
	/// Expands the drop down list.  Returns true if the state chagned or false
	/// if it was already expanded and no action was taken
	/// </summary>
	public virtual bool Expand ()
	{
		if (_isShow) {
			return false;
		}

		SetSearchSet ();
		_isShow = true;
		ShowList ();
		FocusSelectedItem ();

		return true;
	}

	/// <summary>
	/// The currently selected list item
	/// </summary>
	public new string Text {
		get => _text;
		set => SetSearchText (value);
	}

	/// <summary>
	/// Current search text 
	/// </summary>
	public string SearchText {
		get => _search.Text;
		set => SetSearchText (value);
	}

	void SetValue (object text, bool isFromSelectedItem = false)
	{
		_search.TextChanged -= Search_Changed;
		this._text = _search.Text = text.ToString ();
		_search.CursorPosition = 0;
		_search.TextChanged += Search_Changed;
		if (!isFromSelectedItem) {
			_selectedItem = GetSelectedItemFromSource (this._text);
			OnSelectedChanged ();
		}
	}

	void Selected ()
	{
		_isShow = false;
		_listview.TabStop = false;

		if (_listview.Source.Count == 0 || (_searchset?.Count ?? 0) == 0) {
			_text = "";
			HideList ();
			_isShow = false;
			return;
		}

		SetValue (_listview.SelectedItem > -1 ? _searchset [_listview.SelectedItem] : _text);
		_search.CursorPosition = _search.Text.GetColumns ();
		Search_Changed (this, new TextChangedEventArgs (_search.Text));
		OnOpenSelectedItem ();
		Reset (keepSearchText: true);
		HideList ();
		_isShow = false;
	}

	private int GetSelectedItemFromSource (string searchText)
	{
		if (_source is null) {
			return -1;
		}
		for (int i = 0; i < _searchset.Count; i++) {
			if (_searchset [i].ToString () == searchText) {
				return i;
			}
		}
		return -1;
	}

	/// <summary>
	/// Reset to full original list
	/// </summary>
	void Reset (bool keepSearchText = false)
	{
		if (!keepSearchText) {
			SetSearchText (string.Empty);
		}

		ResetSearchSet ();

		_listview.SetSource (_searchset);
		_listview.Height = CalculatetHeight ();

		if (Subviews.Count > 0 && HasFocus) {
			_search.SetFocus ();
		}
	}

	void SetSearchText (string value) => _search.Text = _text = value;

	void ResetSearchSet (bool noCopy = false)
	{
		_searchset.Clear ();

		if (_autoHide || noCopy) {
			return;
		}
		SetSearchSet ();
	}

	void SetSearchSet ()
	{
		if (Source == null) { return; }
		// force deep copy
		foreach (object item in Source.ToList ()) {
			_searchset.Add (item);
		}
	}

	private void Search_Changed (object sender, TextChangedEventArgs e)
	{
		if (_source is null) { // Object initialization		
			return;
		}

		if (string.IsNullOrEmpty (_search.Text) && string.IsNullOrEmpty (e.OldValue)) {
			ResetSearchSet ();
		} else if (_search.Text != e.OldValue) {
			if (_search.Text.Length < e.OldValue.Length) {
				_selectedItem = -1;
			}
			_isShow = true;
			ResetSearchSet (noCopy: true);

			foreach (object item in _source.ToList ()) {
				// Iterate to preserver object type and force deep copy
				if (item.ToString ().StartsWith (_search.Text, StringComparison.CurrentCultureIgnoreCase)) {
					_searchset.Add (item);
				}
			}
		}

		if (HasFocus) {
			ShowList ();
		} else if (_autoHide) {
			_isShow = false;
			HideList ();
		}
	}

	/// <summary>
	/// Show the search list
	/// </summary>
	/// 
	/// Consider making public
	void ShowList ()
	{
		_listview.SetSource (_searchset);
		_listview.Clear (); // Ensure list shrinks in Dialog as you type
		_listview.Height = CalculatetHeight ();
		SuperView?.BringSubviewToFront (this);
	}

	/// <summary>
	/// Hide the search list
	/// </summary>
	/// 
	/// Consider making public
	void HideList ()
	{
		if (_lastSelectedItem != _selectedItem) {
			OnOpenSelectedItem ();
		}
		var rect = _listview.BoundsToScreen (_listview.IsInitialized ? _listview.Bounds : Rect.Empty);
		Reset (keepSearchText: true);
		_listview.Clear (rect);
		_listview.TabStop = false;
		SuperView?.SendSubviewToBack (this);
		SuperView?.SetNeedsDisplay (rect);
		OnCollapsed ();
	}

	/// <summary>
	/// Internal height of dynamic search list
	/// </summary>
	/// <returns></returns>
	int CalculatetHeight ()
	{
		if (!IsInitialized || Bounds.Height == 0) {
			return 0;
		}

		return Math.Min (Math.Max (Bounds.Height - 1, _minimumHeight - 1), _searchset?.Count > 0 ? _searchset.Count : _isShow ? Math.Max (Bounds.Height - 1, _minimumHeight - 1) : 0);
	}
}
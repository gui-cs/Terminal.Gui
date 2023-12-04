using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Terminal.Gui;

/// <summary>
/// <see cref="MenuBarItem"/> is a menu item on an app's <see cref="MenuBar"/>. 
/// MenuBarItems do not support <see cref="MenuItem.Shortcut"/>.
/// </summary>
public class MenuBarItem : MenuItem {
	/// <summary>
	/// Initializes a new <see cref="MenuBarItem"/> as a <see cref="MenuItem"/>.
	/// </summary>
	/// <param name="title">Title for the menu item.</param>
	/// <param name="help">Help text to display. Will be displayed next to the Title surrounded by parentheses.</param>
	/// <param name="action">Action to invoke when the menu item is activated.</param>
	/// <param name="canExecute">Function to determine if the action can currently be executed.</param>
	/// <param name="parent">The parent <see cref="MenuItem"/> of this if exist, otherwise is null.</param>
	public MenuBarItem (string title, string help, Action action, Func<bool> canExecute = null, MenuItem parent = null) : base (title, help, action, canExecute, parent)
	{
		Initialize (title, null, null, true);
	}

	/// <summary>
	/// Initializes a new <see cref="MenuBarItem"/>.
	/// </summary>
	/// <param name="title">Title for the menu item.</param>
	/// <param name="children">The items in the current menu.</param>
	/// <param name="parent">The parent <see cref="MenuItem"/> of this if exist, otherwise is null.</param>
	public MenuBarItem (string title, MenuItem [] children, MenuItem parent = null)
	{
		Initialize (title, children, parent);
	}

	/// <summary>
	/// Initializes a new <see cref="MenuBarItem"/> with separate list of items.
	/// </summary>
	/// <param name="title">Title for the menu item.</param>
	/// <param name="children">The list of items in the current menu.</param>
	/// <param name="parent">The parent <see cref="MenuItem"/> of this if exist, otherwise is null.</param>
	public MenuBarItem (string title, List<MenuItem []> children, MenuItem parent = null)
	{
		Initialize (title, children, parent);
	}

	/// <summary>
	/// Initializes a new <see cref="MenuBarItem"/>.
	/// </summary>
	/// <param name="children">The items in the current menu.</param>
	public MenuBarItem (MenuItem [] children) : this ("", children) { }

	/// <summary>
	/// Initializes a new <see cref="MenuBarItem"/>.
	/// </summary>
	public MenuBarItem () : this (children: new MenuItem [] { }) { }

	void Initialize (string title, object children, MenuItem parent = null, bool isTopLevel = false)
	{
		if (!isTopLevel && children == null) throw new ArgumentNullException (nameof (children), "The parameter cannot be null. Use an empty array instead.");
		SetTitle (title ?? "");
		if (parent != null) Parent = parent;
		if (children is List<MenuItem []>) {
			var childrens = new MenuItem [] { };
			foreach (var item in (List<MenuItem []>)children) for (int i = 0; i < item.Length; i++) {
					SetChildrensParent (item);
					Array.Resize (ref childrens, childrens.Length + 1);
					childrens [childrens.Length - 1] = item [i];
				}
			Children = childrens;
		} else if (children is MenuItem []) {
			SetChildrensParent ((MenuItem [])children);
			Children = (MenuItem [])children;
		} else Children = null;
	}

	void SetChildrensParent (MenuItem [] childrens)
	{
		foreach (var child in childrens) if (child != null && child.Parent == null) child.Parent = this;
	}

	/// <summary>
	/// Check if the children parameter is a <see cref="MenuBarItem"/>.
	/// </summary>
	/// <param name="children"></param>
	/// <returns>Returns a <see cref="MenuBarItem"/> or null otherwise.</returns>
	public MenuBarItem SubMenu (MenuItem children)
	{
		return children as MenuBarItem;
	}

	/// <summary>
	/// Check if the <see cref="MenuItem"/> parameter is a child of this.
	/// </summary>
	/// <param name="menuItem"></param>
	/// <returns>Returns <c>true</c> if it is a child of this. <c>false</c> otherwise.</returns>
	public bool IsSubMenuOf (MenuItem menuItem)
	{
		foreach (var child in Children) if (child == menuItem && child.Parent == menuItem.Parent) return true;
		return false;
	}

	/// <summary>
	/// Get the index of the <see cref="MenuItem"/> parameter.
	/// </summary>
	/// <param name="children"></param>
	/// <returns>Returns a value bigger than -1 if the <see cref="MenuItem"/> is a child of this.</returns>
	public int GetChildrenIndex (MenuItem children)
	{
		if (Children?.Length == 0) return -1;
		int i = 0;
		foreach (var child in Children) {
			if (child == children) return i;
			i++;
		}
		return -1;
	}

	void SetTitle (string title)
	{
		if (title == null)
			title = string.Empty;
		Title = title;
	}

	/// <summary>
	/// Gets or sets an array of <see cref="MenuItem"/> objects that are the children of this <see cref="MenuBarItem"/>
	/// </summary>
	/// <value>The children.</value>
	public MenuItem [] Children { get; set; }

	internal bool IsTopLevel { get => Parent == null && (Children == null || Children.Length == 0) && Action != null; }
}

/// <summary>
///	<para>
/// Provides a menu bar that spans the top of a <see cref="Toplevel"/> View with drop-down and cascading menus. 
///	</para>
/// <para>
/// By default, any sub-sub-menus (sub-menus of the <see cref="MenuItem"/>s added to <see cref="MenuBarItem"/>s) 
/// are displayed in a cascading manner, where each sub-sub-menu pops out of the sub-menu frame
/// (either to the right or left, depending on where the sub-menu is relative to the edge of the screen). By setting
/// <see cref="UseSubMenusSingleFrame"/> to <see langword="true"/>, this behavior can be changed such that all sub-sub-menus are
/// drawn within a single frame below the MenuBar.
/// </para>
/// </summary>
/// <remarks>
///	<para>
///	The <see cref="MenuBar"/> appears on the first row of the parent <see cref="Toplevel"/> View and uses the full width.
///	</para>
///	<para>
///	The <see cref="MenuBar"/> provides global hotkeys for the application. See <see cref="MenuItem.HotKey"/>.
///	</para>
///	<para>
///	See also: <see cref="ContextMenu"/>
///	</para>
/// </remarks>
public class MenuBar : View {
	internal int _selected;
	internal int _selectedSub;

	/// <summary>
	/// Gets or sets the array of <see cref="MenuBarItem"/>s for the menu. Only set this after the <see cref="MenuBar"/> is visible.
	/// </summary>
	/// <value>The menu array.</value>
	public MenuBarItem [] Menus { get; set; }

	/// <summary>
	/// The default <see cref="LineStyle"/> for <see cref="Menus"/>'s border. The default is <see cref="LineStyle.Single"/>.
	/// </summary>
	public LineStyle MenusBorderStyle { get; set; } = LineStyle.Single;

	private bool _useKeysUpDownAsKeysLeftRight = false;

	/// <summary>
	/// Used for change the navigation key style.
	/// </summary>
	public bool UseKeysUpDownAsKeysLeftRight {
		get => _useKeysUpDownAsKeysLeftRight;
		set {
			_useKeysUpDownAsKeysLeftRight = value;
			if (value && UseSubMenusSingleFrame) {
				UseSubMenusSingleFrame = false;
				SetNeedsDisplay ();
			}
		}
	}

	static Rune _shortcutDelimiter = new Rune ('+');
	/// <summary>
	/// Sets or gets the shortcut delimiter separator. The default is "+".
	/// </summary>
	public static Rune ShortcutDelimiter {
		get => _shortcutDelimiter;
		set {
			if (_shortcutDelimiter != value) {
				_shortcutDelimiter = value == default ? new Rune ('+') : value;
			}
		}
	}

	/// <summary>
	/// The specifier character for the hotkey to all menus.
	/// </summary>
	new public static Rune HotKeySpecifier => (Rune)'_';

	private bool _useSubMenusSingleFrame;

	/// <summary>
	/// Gets or sets if the sub-menus must be displayed in a single or multiple frames.
	/// <para>
	/// By default any sub-sub-menus (sub-menus of the main <see cref="MenuItem"/>s) are displayed in a cascading manner, 
	/// where each sub-sub-menu pops out of the sub-menu frame
	/// (either to the right or left, depending on where the sub-menu is relative to the edge of the screen). By setting
	/// <see cref="UseSubMenusSingleFrame"/> to <see langword="true"/>, this behavior can be changed such that all sub-sub-menus are
	/// drawn within a single frame below the MenuBar.
	/// </para>		
	/// </summary>
	public bool UseSubMenusSingleFrame {
		get => _useSubMenusSingleFrame;
		set {
			_useSubMenusSingleFrame = value;
			if (value && UseKeysUpDownAsKeysLeftRight) {
				_useKeysUpDownAsKeysLeftRight = false;
				SetNeedsDisplay ();
			}
		}
	}

	/// <summary>
	/// The <see cref="Gui.Key"/> used to activate the menu bar by keyboard.
	/// </summary>
	public Key Key {
		get => _key;
		set {
			if (_key == value) {
				return;
			}
			ClearKeyBinding (_key);
			AddKeyBinding (value, Command.Select);
			_key = value;
		}
	}

	///<inheritdoc/>
	public override bool Visible {
		get => base.Visible;
		set {
			base.Visible = value;
			if (!value) {
				CloseAllMenus ();
			}
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MenuBar"/>.
	/// </summary>
	public MenuBar () : this (new MenuBarItem [] { }) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="MenuBar"/> class with the specified set of Toplevel menu items.
	/// </summary>
	/// <param name="menus">Individual menu items; a null item will result in a separator being drawn.</param>
	public MenuBar (MenuBarItem [] menus) : base ()
	{
		X = 0;
		Y = 0;
		Width = Dim.Fill ();
		Height = 1;
		Menus = menus;
		//CanFocus = true;
		_selected = -1;
		_selectedSub = -1;
		ColorScheme = Colors.Menu;
		WantMousePositionReports = true;
		IsMenuOpen = false;

		Added += MenuBar_Added;

		// Things this view knows how to do
		AddCommand (Command.Left, () => { MoveLeft (); return true; });
		AddCommand (Command.Right, () => { MoveRight (); return true; });
		AddCommand (Command.Cancel, () => { CloseMenuBar (); return true; });
		AddCommand (Command.Accept, () => { ProcessMenu (_selected, Menus [_selected]); return true; });
		AddCommand (Command.Select, () => SelectOrRun ());

		// Default key bindings for this view
		AddKeyBinding (Key.CursorLeft, Command.Left);
		AddKeyBinding (Key.CursorRight, Command.Right);
		AddKeyBinding (Key.Esc, Command.Cancel);
		AddKeyBinding (Key.C | Key.CtrlMask, Command.Cancel);
		AddKeyBinding (Key.CursorDown, Command.Accept);
		AddKeyBinding (Key.Enter, Command.Accept);
		AddKeyBinding (this.Key, Command.Select);
		AddKeyBinding (Key.CtrlMask | Key.Space, Command.Select);

		// TODO: Bindings (esp for hotkey) should be added across and then down. This currently does down then across. 
		// TODO: As a result, _File._Save will have precedence over in "_File _Edit _ScrollbarView"
		// TODO: Also: Hotkeys should not work for sub-menus if they are not visible!
		if (Menus != null) {
			foreach (var menu in Menus?.Where (m => m != null)) {
				if (menu.HotKey != default) {
					AddKeyBinding ((Key)menu.HotKey.Value, Command.Select);
					AddKeyBinding ((Key)menu.HotKey.Value | Key.AltMask, Command.Select);
				}
				if (menu.Shortcut != Key.Unknown) {
					AddKeyBinding (menu.Shortcut, Command.Select);
				}
				AddKeyBindings (menu);
			}
		}
	}

	void AddKeyBindings (MenuBarItem menuBarItem)
	{
		if (menuBarItem == null || menuBarItem.Children == null) {
			return;
		}
		foreach (var menuItem in menuBarItem.Children.Where (m => m != null)) {
			if (menuItem.HotKey != default) {
				AddKeyBinding ((Key)menuItem.HotKey.Value, Command.Select);
				AddKeyBinding ((Key)menuItem.HotKey.Value | Key.AltMask, Command.Select);
			}
			if (menuItem.Shortcut != Key.Unknown) {
				AddKeyBinding (menuItem.Shortcut, Command.Select);
			}
			var subMenu = menuBarItem.SubMenu (menuItem);
			AddKeyBindings (subMenu);
		}
	}

	bool SelectOrRun ()
	{
		if (!IsInitialized || !Visible) {
			return true;
		}

		if (_mbItemToActivate != -1) {
			Activate (_mbItemToActivate);
		} else if (_menuItemToActivate != null && _menuItemToActivate.Action != null) {
			Run (_menuItemToActivate.Action);
		} else {
			if (IsMenuOpen) {
				CloseAllMenus ();
			} else {
				OpenMenu ();
			}

		}

		_openedByHotKey = true;
		return true;
	}

	bool _initialCanFocus;

	void MenuBar_Added (object sender, SuperViewChangedEventArgs e)
	{
		_initialCanFocus = CanFocus;
		Added -= MenuBar_Added;
	}

	bool _openedByAltKey;

	bool _isCleaning;

	///<inheritdoc/>
	public override bool OnLeave (View view)
	{
		if ((!(view is MenuBar) && !(view is Menu) || !(view is MenuBar) && !(view is Menu) && _openMenu != null) && !_isCleaning && !_reopen) {
			CleanUp ();
		}
		return base.OnLeave (view);
	}

	///<inheritdoc/>
	public override bool OnKeyUp (KeyEventArgs keyEvent)
	{
		if (keyEvent.Key == Key.AltMask) {
			// User pressed Alt - this may be a precursor to a menu accelerator (e.g. Alt-F)
			if (!_openedByHotKey && !IsMenuOpen && _openMenu == null && (((uint)keyEvent.Key & (uint)Key.CharMask) == 0)) {
				// There's no open menu, the first menu item should be highlight.
				// The right way to do this is to SetFocus(MenuBar), but for some reason
				// that faults.

				var mbar = GetMouseGrabViewInstance (this);
				if (mbar != null) {
					mbar.CleanUp ();
				}

				//Activate (0);
				//StartMenu ();
				IsMenuOpen = true;
				_selected = 0;
				CanFocus = true;
				_lastFocused = SuperView == null ? Application.Current.MostFocused : SuperView.MostFocused;
				SetFocus ();
				SetNeedsDisplay ();
				Application.GrabMouse (this);
			} else if (!_openedByHotKey) {
				// There's an open menu. If this Alt key-up is a pre-cursor to an accelerator
				// we don't want to close the menu because it'll flash.
				// How to deal with that?

				CleanUp ();
			}

			return true;
		}
		return false;
	}

	internal void CleanUp ()
	{
		_isCleaning = true;
		if (_openMenu != null) {
			CloseAllMenus ();
		}
		_openedByAltKey = false;
		IsMenuOpen = false;
		_selected = -1;
		CanFocus = _initialCanFocus;
		if (_lastFocused != null) {
			_lastFocused.SetFocus ();
		}
		SetNeedsDisplay ();
		Application.UngrabMouse ();
		_isCleaning = false;
	}

	// The column where the MenuBar starts
	static int _xOrigin = 0;
	// Spaces before the Title
	static int _leftPadding = 1;
	// Spaces after the Title
	static int _rightPadding = 1;
	// Spaces after the submenu Title, before Help
	static int _parensAroundHelp = 3;
	///<inheritdoc/>
	public override void OnDrawContent (Rect contentArea)
	{
		Move (0, 0);
		Driver.SetAttribute (GetNormalColor ());
		for (int i = 0; i < Frame.Width; i++)
			Driver.AddRune ((Rune)' ');

		Move (1, 0);
		int pos = 0;

		for (int i = 0; i < Menus.Length; i++) {
			var menu = Menus [i];
			Move (pos, 0);
			Attribute hotColor, normalColor;
			if (i == _selected && IsMenuOpen) {
				hotColor = i == _selected ? ColorScheme.HotFocus : ColorScheme.HotNormal;
				normalColor = i == _selected ? ColorScheme.Focus : GetNormalColor ();
			} else {
				hotColor = ColorScheme.HotNormal;
				normalColor = GetNormalColor ();
			}
			// Note Help on MenuBar is drawn with parens around it
			DrawHotString (string.IsNullOrEmpty (menu.Help) ? $" {menu.Title} " : $" {menu.Title} ({menu.Help}) ", hotColor, normalColor);
			pos += _leftPadding + menu.TitleLength + (menu.Help.GetColumns () > 0 ? _leftPadding + menu.Help.GetColumns () + _parensAroundHelp : 0) + _rightPadding;
		}
		PositionCursor ();
	}

	///<inheritdoc/>
	public override void PositionCursor ()
	{
		if (_selected == -1 && HasFocus && Menus.Length > 0) {
			_selected = 0;
		}
		int pos = 0;
		for (int i = 0; i < Menus.Length; i++) {
			if (i == _selected) {
				pos++;
				Move (pos + 1, 0);
				return;
			} else {
				pos += _leftPadding + Menus [i].TitleLength + (Menus [i].Help.GetColumns () > 0 ? Menus [i].Help.GetColumns () + _parensAroundHelp : 0) + _rightPadding;
			}
		}
	}

	void Selected (MenuItem item)
	{
		var action = item.Action;

		if (action == null)
			return;

		Application.UngrabMouse ();
		CloseAllMenus ();
		Application.Refresh ();

		Run (action);
	}

	internal void Run (Action action)
	{
		if (action == null) {
			return;
		}
		Application.MainLoop.AddIdle (() => {
			action ();
			return false;
		});
	}

	/// <summary>
	/// Raised as a menu is opening.
	/// </summary>
	public event EventHandler<MenuOpeningEventArgs> MenuOpening;

	/// <summary>
	/// Raised when a menu is opened.
	/// </summary>
	public event EventHandler<MenuOpenedEventArgs> MenuOpened;

	/// <summary>
	/// Raised when a menu is closing passing <see cref="MenuClosingEventArgs"/>.
	/// </summary>
	public event EventHandler<MenuClosingEventArgs> MenuClosing;

	/// <summary>
	/// Raised when all the menu is closed.
	/// </summary>
	public event EventHandler MenuAllClosed;

	// BUGBUG: Hack
	internal Menu _openMenu;
	Menu ocm;
	internal Menu openCurrentMenu {
		get => ocm;
		set {
			if (ocm != value) {
				ocm = value;
				if (ocm != null && ocm._currentChild > -1) {
					OnMenuOpened ();
				}
			}
		}
	}
	internal List<Menu> _openSubMenu;
	View _previousFocused;
	internal bool _isMenuOpening;
	internal bool _isMenuClosing;

	/// <summary>
	/// <see langword="true"/> if the menu is open; otherwise <see langword="true"/>.
	/// </summary>
	public bool IsMenuOpen { get; protected set; }

	/// <summary>
	/// Virtual method that will invoke the <see cref="MenuOpening"/> event if it's defined.
	/// </summary>
	/// <param name="currentMenu">The current menu to be replaced.</param>
	/// <returns>Returns the <see cref="MenuOpeningEventArgs"/></returns>
	public virtual MenuOpeningEventArgs OnMenuOpening (MenuBarItem currentMenu)
	{
		var ev = new MenuOpeningEventArgs (currentMenu);
		MenuOpening?.Invoke (this, ev);
		return ev;
	}

	/// <summary>
	/// Virtual method that will invoke the <see cref="MenuOpened"/> event if it's defined.
	/// </summary>
	public virtual void OnMenuOpened ()
	{
		MenuItem mi = null;
		MenuBarItem parent;

		if (openCurrentMenu._barItems.Children != null && openCurrentMenu._barItems.Children.Length > 0
			&& openCurrentMenu?._currentChild > -1) {
			parent = openCurrentMenu._barItems;
			mi = parent.Children [openCurrentMenu._currentChild];
		} else if (openCurrentMenu._barItems.IsTopLevel) {
			parent = null;
			mi = openCurrentMenu._barItems;
		} else {
			parent = _openMenu._barItems;
			mi = parent.Children [_openMenu._currentChild];
		}
		MenuOpened?.Invoke (this, new MenuOpenedEventArgs (parent, mi));
	}

	/// <summary>
	/// Virtual method that will invoke the <see cref="MenuClosing"/>.
	/// </summary>
	/// <param name="currentMenu">The current menu to be closed.</param>
	/// <param name="reopen">Whether the current menu will be reopen.</param>
	/// <param name="isSubMenu">Whether is a sub-menu or not.</param>
	public virtual MenuClosingEventArgs OnMenuClosing (MenuBarItem currentMenu, bool reopen, bool isSubMenu)
	{
		var ev = new MenuClosingEventArgs (currentMenu, reopen, isSubMenu);
		MenuClosing?.Invoke (this, ev);
		return ev;
	}

	/// <summary>
	/// Virtual method that will invoke the <see cref="MenuAllClosed"/>.
	/// </summary>
	public virtual void OnMenuAllClosed ()
	{
		MenuAllClosed?.Invoke (this, EventArgs.Empty);
	}

	View _lastFocused;

	/// <summary>
	/// Gets the view that was last focused before opening the menu.
	/// </summary>
	public View LastFocused { get; private set; }

	internal void OpenMenu (int index, int sIndex = -1, MenuBarItem subMenu = null)
	{
		_isMenuOpening = true;
		var newMenu = OnMenuOpening (Menus [index]);
		if (newMenu.Cancel) {
			_isMenuOpening = false;
			return;
		}
		if (newMenu.NewMenuBarItem != null) {
			Menus [index] = newMenu.NewMenuBarItem;
		}
		int pos = 0;
		switch (subMenu) {
		case null:
			// Open a submenu below a MenuBar
			_lastFocused ??= (SuperView == null ? Application.Current?.MostFocused : SuperView.MostFocused);
			if (_openSubMenu != null && !CloseMenu (false, true))
				return;
			if (_openMenu != null) {
				Application.Current.Remove (_openMenu);
				_openMenu.Dispose ();
				_openMenu = null;
			}

			// This positions the submenu horizontally aligned with the first character of the
			// text belonging to the menu 
			for (int i = 0; i < index; i++)
				pos += Menus [i].TitleLength + (Menus [i].Help.GetColumns () > 0 ? Menus [i].Help.GetColumns () + 2 : 0) + _leftPadding + _rightPadding;

			var locationOffset = Point.Empty;
			// if SuperView is null then it's from a ContextMenu
			if (SuperView == null) {
				locationOffset = GetScreenOffset ();
			}
			if (SuperView != null && SuperView != Application.Current) {
				locationOffset.X += SuperView.Border.Thickness.Left;
				locationOffset.Y += SuperView.Border.Thickness.Top;
			}
			_openMenu = new Menu (this, Frame.X + pos + locationOffset.X, Frame.Y + 1 + locationOffset.Y, Menus [index], null, MenusBorderStyle);
			openCurrentMenu = _openMenu;
			openCurrentMenu._previousSubFocused = _openMenu;

			Application.Current.Add (_openMenu);
			_openMenu.SetFocus ();
			break;
		default:
			// Opens a submenu next to another submenu (openSubMenu)
			if (_openSubMenu == null)
				_openSubMenu = new List<Menu> ();
			if (sIndex > -1) {
				RemoveSubMenu (sIndex);
			} else {
				var last = _openSubMenu.Count > 0 ? _openSubMenu.Last () : _openMenu;
				if (!UseSubMenusSingleFrame) {
					locationOffset = GetLocationOffset ();
					openCurrentMenu = new Menu (this, last.Frame.Left + last.Frame.Width + locationOffset.X, last.Frame.Top + locationOffset.Y + last._currentChild, subMenu, last, MenusBorderStyle);
				} else {
					var first = _openSubMenu.Count > 0 ? _openSubMenu.First () : _openMenu;
					// 2 is for the parent and the separator
					var mbi = new MenuItem [2 + subMenu.Children.Length];
					mbi [0] = new MenuItem () { Title = subMenu.Title, Parent = subMenu };
					mbi [1] = null;
					for (int j = 0; j < subMenu.Children.Length; j++) {
						mbi [j + 2] = subMenu.Children [j];
					}
					var newSubMenu = new MenuBarItem (mbi) { Parent = subMenu };
					openCurrentMenu = new Menu (this, first.Frame.Left, first.Frame.Top, newSubMenu, null, MenusBorderStyle);
					last.Visible = false;
					Application.GrabMouse (openCurrentMenu);
				}
				openCurrentMenu._previousSubFocused = last._previousSubFocused;
				_openSubMenu.Add (openCurrentMenu);
				Application.Current.Add (openCurrentMenu);
			}
			_selectedSub = _openSubMenu.Count - 1;
			if (_selectedSub > -1 && SelectEnabledItem (openCurrentMenu._barItems.Children, openCurrentMenu._currentChild, out openCurrentMenu._currentChild)) {
				openCurrentMenu.SetFocus ();
			}
			break;
		}
		_isMenuOpening = false;
		IsMenuOpen = true;
	}

	Point GetLocationOffset ()
	{
		if (MenusBorderStyle != LineStyle.None) {
			return new Point (0, 1);
		}
		return new Point (-2, 0);
	}

	/// <summary>
	/// Opens the Menu programatically, as though the F9 key were pressed.
	/// </summary>
	public void OpenMenu ()
	{
		var mbar = GetMouseGrabViewInstance (this);
		if (mbar != null) {
			mbar.CleanUp ();
		}

		if (_openMenu != null)
			return;
		_selected = 0;
		SetNeedsDisplay ();

		_previousFocused = SuperView == null ? Application.Current.Focused : SuperView.Focused;
		OpenMenu (_selected);
		if (!SelectEnabledItem (openCurrentMenu._barItems.Children, openCurrentMenu._currentChild, out openCurrentMenu._currentChild) && !CloseMenu (false)) {
			return;
		}
		if (!openCurrentMenu.CheckSubMenu ())
			return;
		Application.GrabMouse (this);
	}

	// Activates the menu, handles either first focus, or activating an entry when it was already active
	// For mouse events.
	internal void Activate (int idx, int sIdx = -1, MenuBarItem subMenu = null)
	{
		_selected = idx;
		_selectedSub = sIdx;
		if (_openMenu == null) {
			_previousFocused = SuperView == null ? Application.Current?.Focused ?? null : SuperView.Focused;
		}

		OpenMenu (idx, sIdx, subMenu);
		SetNeedsDisplay ();
	}

	internal bool SelectEnabledItem (IEnumerable<MenuItem> chldren, int current, out int newCurrent, bool forward = true)
	{
		if (chldren == null) {
			newCurrent = -1;
			return true;
		}

		IEnumerable<MenuItem> childrens;
		if (forward) {
			childrens = chldren;
		} else {
			childrens = chldren.Reverse ();
		}
		int count;
		if (forward) {
			count = -1;
		} else {
			count = childrens.Count ();
		}
		foreach (var child in childrens) {
			if (forward) {
				if (++count < current) {
					continue;
				}
			} else {
				if (--count > current) {
					continue;
				}
			}
			if (child == null || !child.IsEnabled ()) {
				if (forward) {
					current++;
				} else {
					current--;
				}
			} else {
				newCurrent = current;
				return true;
			}
		}
		newCurrent = -1;
		return false;
	}

	/// <summary>
	/// Closes the Menu programmatically if open and not canceled (as though F9 were pressed).
	/// </summary>
	public bool CloseMenu (bool ignoreUseSubMenusSingleFrame = false)
	{
		return CloseMenu (false, false, ignoreUseSubMenusSingleFrame);
	}

	bool _reopen;

	internal bool CloseMenu (bool reopen = false, bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
	{
		var mbi = isSubMenu ? openCurrentMenu._barItems : _openMenu?._barItems;
		if (UseSubMenusSingleFrame && mbi != null &&
			!ignoreUseSubMenusSingleFrame && mbi.Parent != null) {
			return false;
		}
		_isMenuClosing = true;
		this._reopen = reopen;
		var args = OnMenuClosing (mbi, reopen, isSubMenu);
		if (args.Cancel) {
			_isMenuClosing = false;
			if (args.CurrentMenu.Parent != null)
				_openMenu._currentChild = ((MenuBarItem)args.CurrentMenu.Parent).Children.IndexOf (args.CurrentMenu);
			return false;
		}
		switch (isSubMenu) {
		case false:
			if (_openMenu != null) {
				Application.Current.Remove (_openMenu);
			}
			SetNeedsDisplay ();
			if (_previousFocused != null && _previousFocused is Menu && _openMenu != null && _previousFocused.ToString () != openCurrentMenu.ToString ())
				_previousFocused.SetFocus ();
			_openMenu?.Dispose ();
			_openMenu = null;
			if (_lastFocused is Menu || _lastFocused is MenuBar) {
				_lastFocused = null;
			}
			LastFocused = _lastFocused;
			_lastFocused = null;
			if (LastFocused != null && LastFocused.CanFocus) {
				if (!reopen) {
					_selected = -1;
				}
				if (_openSubMenu != null) {
					_openSubMenu = null;
				}
				if (openCurrentMenu != null) {
					Application.Current.Remove (openCurrentMenu);
					openCurrentMenu.Dispose ();
					openCurrentMenu = null;
				}
				LastFocused.SetFocus ();
			} else if (_openSubMenu == null || _openSubMenu.Count == 0) {
				CloseAllMenus ();
			} else {
				SetFocus ();
				PositionCursor ();
			}
			IsMenuOpen = false;
			break;

		case true:
			_selectedSub = -1;
			SetNeedsDisplay ();
			RemoveAllOpensSubMenus ();
			openCurrentMenu._previousSubFocused.SetFocus ();
			_openSubMenu = null;
			IsMenuOpen = true;
			break;
		}
		this._reopen = false;
		_isMenuClosing = false;
		return true;
	}

	void RemoveSubMenu (int index, bool ignoreUseSubMenusSingleFrame = false)
	{
		if (_openSubMenu == null || (UseSubMenusSingleFrame
			&& !ignoreUseSubMenusSingleFrame && _openSubMenu.Count == 0))

			return;
		for (int i = _openSubMenu.Count - 1; i > index; i--) {
			_isMenuClosing = true;
			Menu menu;
			if (_openSubMenu.Count - 1 > 0)
				menu = _openSubMenu [i - 1];
			else
				menu = _openMenu;
			if (!menu.Visible)
				menu.Visible = true;
			openCurrentMenu = menu;
			openCurrentMenu.SetFocus ();
			if (_openSubMenu != null) {
				menu = _openSubMenu [i];
				Application.Current.Remove (menu);
				_openSubMenu.Remove (menu);
				menu.Dispose ();
			}
			RemoveSubMenu (i, ignoreUseSubMenusSingleFrame);
		}
		if (_openSubMenu.Count > 0)
			openCurrentMenu = _openSubMenu.Last ();

		_isMenuClosing = false;
	}

	internal void RemoveAllOpensSubMenus ()
	{
		if (_openSubMenu != null) {
			foreach (var item in _openSubMenu) {
				Application.Current.Remove (item);
				item.Dispose ();
			}
		}
	}

	internal void CloseAllMenus ()
	{
		if (!_isMenuOpening && !_isMenuClosing) {
			if (_openSubMenu != null && !CloseMenu (false, true, true))
				return;
			if (!CloseMenu (false))
				return;
			if (LastFocused != null && LastFocused != this)
				_selected = -1;
			Application.UngrabMouse ();
		}
		IsMenuOpen = false;
		_openedByHotKey = false;
		_openedByAltKey = false;
		OnMenuAllClosed ();
	}

	View FindDeepestMenu (View view, ref int count)
	{
		count = count > 0 ? count : 0;
		foreach (var menu in view.Subviews) {
			if (menu is Menu) {
				count++;
				return FindDeepestMenu ((Menu)menu, ref count);
			}
		}
		return view;
	}

	internal void PreviousMenu (bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
	{
		switch (isSubMenu) {
		case false:
			if (_selected <= 0)
				_selected = Menus.Length - 1;
			else
				_selected--;

			if (_selected > -1 && !CloseMenu (true, false, ignoreUseSubMenusSingleFrame))
				return;
			OpenMenu (_selected);
			if (!SelectEnabledItem (openCurrentMenu._barItems.Children, openCurrentMenu._currentChild, out openCurrentMenu._currentChild, false)) {
				openCurrentMenu._currentChild = 0;
			}
			break;
		case true:
			if (_selectedSub > -1) {
				_selectedSub--;
				RemoveSubMenu (_selectedSub, ignoreUseSubMenusSingleFrame);
				SetNeedsDisplay ();
			} else
				PreviousMenu ();

			break;
		}
	}

	internal void NextMenu (bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
	{
		switch (isSubMenu) {
		case false:
			if (_selected == -1)
				_selected = 0;
			else if (_selected + 1 == Menus.Length)
				_selected = 0;
			else
				_selected++;

			if (_selected > -1 && !CloseMenu (true, ignoreUseSubMenusSingleFrame))
				return;
			OpenMenu (_selected);
			SelectEnabledItem (openCurrentMenu._barItems.Children, openCurrentMenu._currentChild, out openCurrentMenu._currentChild);
			break;
		case true:
			if (UseKeysUpDownAsKeysLeftRight) {
				if (CloseMenu (false, true, ignoreUseSubMenusSingleFrame)) {
					NextMenu (false, ignoreUseSubMenusSingleFrame);
				}
			} else {
				var subMenu = openCurrentMenu._currentChild > -1 && openCurrentMenu._barItems.Children.Length > 0
					? openCurrentMenu._barItems.SubMenu (openCurrentMenu._barItems.Children [openCurrentMenu._currentChild])
					: null;
				if ((_selectedSub == -1 || _openSubMenu == null || _openSubMenu?.Count - 1 == _selectedSub) && subMenu == null) {
					if (_openSubMenu != null && !CloseMenu (false, true))
						return;
					NextMenu (false, ignoreUseSubMenusSingleFrame);
				} else if (subMenu != null || (openCurrentMenu._currentChild > -1
					&& !openCurrentMenu._barItems.Children [openCurrentMenu._currentChild].IsFromSubMenu)) {
					_selectedSub++;
					openCurrentMenu.CheckSubMenu ();
				} else {
					if (CloseMenu (false, true, ignoreUseSubMenusSingleFrame)) {
						NextMenu (false, ignoreUseSubMenusSingleFrame);
					}
					return;
				}

				SetNeedsDisplay ();
				if (UseKeysUpDownAsKeysLeftRight)
					openCurrentMenu.CheckSubMenu ();
			}
			break;
		}
	}

	bool _openedByHotKey;
	internal bool FindAndOpenMenuByHotkey (KeyEventArgs a)
	{
		//int pos = 0;
		var c = ((uint)a.Key & (uint)Key.CharMask);
		for (int i = 0; i < Menus.Length; i++) {
			// TODO: this code is duplicated, hotkey should be part of the MenuBarItem
			var mi = Menus [i];
			int p = mi.Title.IndexOf (MenuBar.HotKeySpecifier.ToString ());
			if (p != -1 && p + 1 < mi.Title.GetRuneCount ()) {
				if (Char.ToUpperInvariant ((char)mi.Title [p + 1]) == c) {
					ProcessMenu (i, mi);
					return true;
				} else if (mi.Children?.Length > 0) {
					if (FindAndOpenChildrenMenuByHotkey (a, mi.Children)) {
						return true;
					}
				}
			} else if (mi.Children?.Length > 0) {
				if (FindAndOpenChildrenMenuByHotkey (a, mi.Children)) {
					return true;
				}
			}
		}

		return false;
	}

	bool FindAndOpenChildrenMenuByHotkey (KeyEventArgs a, MenuItem [] children)
	{
		var c = ((uint)a.Key & (uint)Key.CharMask);
		for (int i = 0; i < children.Length; i++) {
			var mi = children [i];

			if (mi == null) {
				continue;
			}

			int p = mi.Title.IndexOf (MenuBar.HotKeySpecifier.ToString ());
			if (p != -1 && p + 1 < mi.Title.GetRuneCount ()) {
				if (Char.ToUpperInvariant ((char)mi.Title [p + 1]) == c) {
					if (mi.IsEnabled ()) {
						var action = mi.Action;
						if (action != null) {
							Run (action);
						}
					}
					return true;
				} else if (mi is MenuBarItem menuBarItem && menuBarItem?.Children.Length > 0) {
					if (FindAndOpenChildrenMenuByHotkey (a, menuBarItem.Children)) {
						return true;
					}
				}
			} else if (mi is MenuBarItem menuBarItem && menuBarItem?.Children.Length > 0) {
				if (FindAndOpenChildrenMenuByHotkey (a, menuBarItem.Children)) {
					return true;
				}
			}
		}
		return false;
	}

	internal bool FindAndOpenMenuByShortcut (KeyEventArgs a, MenuItem [] children = null)
	{
		if (children == null) {
			children = Menus;
		}

		var key = a.KeyValue;
		var keys = a.Key; //ShortcutHelper.GetModifiersKey (a);
		key |= (int)keys;
		for (int i = 0; i < children.Length; i++) {
			var mi = children [i];
			if (mi == null) {
				continue;
			}
			if ((!(mi is MenuBarItem mbiTopLevel) || mbiTopLevel.IsTopLevel) && mi.Shortcut != Key.Null && mi.Shortcut == (Key)key) {
				var action = mi.Action;
				if (action != null) {
					Application.MainLoop.AddIdle (() => {
						action ();
						return false;
					});
				}
				return true;
			}
			if (mi is MenuBarItem menuBarItem && menuBarItem.Children != null && !menuBarItem.IsTopLevel && FindAndOpenMenuByShortcut (a, menuBarItem.Children)) {
				return true;
			}
		}

		return false;
	}

	private void ProcessMenu (int i, MenuBarItem mi)
	{
		if (_selected < 0 && IsMenuOpen) {
			return;
		}

		if (mi.IsTopLevel) {
			BoundsToScreen (i, 0, out int rx, out int ry);
			var menu = new Menu (this, rx, ry, mi, null, MenusBorderStyle);
			menu.Run (mi.Action);
			menu.Dispose ();
		} else {
			_openedByHotKey = true;
			Application.GrabMouse (this);
			_selected = i;
			OpenMenu (i);
			if (!SelectEnabledItem (openCurrentMenu._barItems.Children, openCurrentMenu._currentChild, out openCurrentMenu._currentChild) && !CloseMenu (false)) {
				return;
			}
			if (!openCurrentMenu.CheckSubMenu ())
				return;
		}
		SetNeedsDisplay ();
	}

	/////<inheritdoc/>
	//public override bool OnHotKey (KeyEventArgs a)
	//{
	//	if (a.Key == Key) {
	//		if (Visible && !IsMenuOpen) {
	//			OpenMenu ();
	//		} else {
	//			CloseAllMenus ();
	//		}
	//		return true;
	//	}

	//	a.Key
	//	// To ncurses simulate a AltMask key pressing Alt+Space because
	//	// it can't detect an alone special key down was pressed.
	//	if (a.Key == Key.AltMask && _openMenu == null) {
	//		OnKeyDown (a);
	//		OnKeyUp (a);
	//		return true;
	//	} else if (((a.Key & Key.AltMask) != 0) || a.IsAlt && !a.IsCtrl && !a.IsShift) {
	//		// BUGBUG: Note the test for BOTH AltMask and a.IsAlt above. This is because the
	//		// unit test Separators_Does_Not_Throws_Pressing_Menu_Shortcut calls
	//		// menu.OnHotKey(new KeyEventArgs (Key.AltMask | Key.Q))) which does not
	//		// cause a.IsAlt to be set.
	//		if (FindAndOpenMenuByHotkey (a)) {
	//			return true;
	//		}
	//	}
	//	//var kc = a.KeyValue;

	//	return base.OnHotKey (a);
	//}

	///<inheritdoc/>
	public override bool OnKeyPressed (KeyEventArgs a)
	{
		if (base.OnKeyPressed (a)) {
			return true;
		}
		var key = a.KeyValue;
		if ((key >= 'a' && key <= 'z') || (key >= 'A' && key <= 'Z') || (key >= '0' && key <= '9')) {
			char c = Char.ToUpper ((char)key);

			if (_selected == -1 || Menus [_selected].IsTopLevel)
				return false;

			foreach (var mi in Menus [_selected].Children) {
				if (mi == null)
					continue;
				int p = mi.Title.IndexOf (MenuBar.HotKeySpecifier.ToString ());
				if (p != -1 && p + 1 < mi.Title.GetRuneCount ()) {
					if (mi.Title [p + 1] == c) {
						Selected (mi);
						return true;
					}
				}
			}
		}

		return false;
	}

	int _mbItemToActivate;
	MenuItem _menuItemToActivate;

	/// <inheritdoc/>
	public override bool OnInvokeKeyBindings (KeyEventArgs keyEvent)
	{
		// This is a bit of a hack. We want to handle the key bindings for menu bar but
		// InvokeKeyBindings doesn't pass any context so we can't tell which item it is for.
		// So before we call the base class we set SelectedItem appropriately.

		// Force upper case
		var key = keyEvent.Key;
		var mask = key & Key.CharMask;
		if (mask >= Key.a && mask <= Key.z) {
			key = (Key)((int)key - 32);
		}

		if (key == (Key.D1 | Key.AltMask)) {

		}
		if (ContainsKeyBinding (key)) {
			_mbItemToActivate = -1;
			_menuItemToActivate = null;
			// Search  
			for (var i = 0; i < Menus.Length; i++) {
				var hotKeyValue = Menus [i]?.HotKey.Value;
				if (hotKeyValue != null) {
					var matches = key == ((Key)Menus [i].HotKey.Value);
					if (!IsMenuOpen) {
						matches = (key == (Key)Menus [i].HotKey.Value) || (key == ((Key)Menus [i].HotKey.Value | Key.AltMask));
					}

					if (matches) {
						_mbItemToActivate = i;
						_menuItemToActivate = Menus [i];
						break;
					}
				}
				var shortcut = Menus [i]?.Shortcut;
				if (key == shortcut) {
					_mbItemToActivate = -1;
					_menuItemToActivate = Menus [i];
					break;
				}
				if (FindShortcutInChildMenu (key, Menus [i])) {
					break;
				}
			}
		}
		return base.OnInvokeKeyBindings (keyEvent);
	}

	bool FindShortcutInChildMenu (Key key, MenuBarItem menuBarItem)
	{
		if (menuBarItem?.Children == null) {
			return false;
		}
		
		for (var c = 0; c < menuBarItem.Children.Length; c++) {
			var menuItem = menuBarItem.Children [c];
			if (menuItem?.HotKey.Value != null) {
				var matches = key == ((Key)menuItem?.HotKey.Value);
				if (!IsMenuOpen) {
					matches = (key == (Key)menuItem?.HotKey.Value) || (key == ((Key)menuItem?.HotKey.Value | Key.AltMask));
				}

				if (matches) {
					_mbItemToActivate = -1;
					_menuItemToActivate = menuItem;
					return true;
				}
			}
			if (key == menuItem?.Shortcut) {
				_mbItemToActivate = -1;
				_menuItemToActivate = menuItem;
				return true;
			}
			var subMenu = menuBarItem.SubMenu (menuItem);
			FindShortcutInChildMenu (key, subMenu);
		}
		return false;
	}

	void CloseMenuBar ()
	{
		if (!CloseMenu (false)) {
			return;
		}
		if (_openedByAltKey) {
			_openedByAltKey = false;
			LastFocused?.SetFocus ();
		}
		SetNeedsDisplay ();
	}

	void MoveRight ()
	{
		_selected = (_selected + 1) % Menus.Length;
		OpenMenu (_selected);
		SetNeedsDisplay ();
	}

	void MoveLeft ()
	{
		_selected--;
		if (_selected < 0) {
			_selected = Menus.Length - 1;
		}
		OpenMenu (_selected);
		SetNeedsDisplay ();
	}

	/////<inheritdoc/>
	//public override bool OnColdKey (KeyEventArgs a)
	//{
	//	return FindAndOpenMenuByShortcut (a);
	//}

	///<inheritdoc/>
	public override bool MouseEvent (MouseEvent me)
	{
		if (!_handled && !HandleGrabView (me, this)) {
			return false;
		}
		_handled = false;

		if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked || me.Flags == MouseFlags.Button1TripleClicked || me.Flags == MouseFlags.Button1Clicked ||
			(me.Flags == MouseFlags.ReportMousePosition && _selected > -1) ||
			(me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && _selected > -1)) {
			int pos = _xOrigin;
			Point locationOffset = default;
			if (SuperView != null) {
				locationOffset.X += SuperView.Border.Thickness.Left;
				locationOffset.Y += SuperView.Border.Thickness.Top;
			}
			int cx = me.X - locationOffset.X;
			for (int i = 0; i < Menus.Length; i++) {
				if (cx >= pos && cx < pos + _leftPadding + Menus [i].TitleLength + Menus [i].Help.GetColumns () + _rightPadding) {
					if (me.Flags == MouseFlags.Button1Clicked) {
						if (Menus [i].IsTopLevel) {
							BoundsToScreen (i, 0, out int rx, out int ry);
							var menu = new Menu (this, rx, ry, Menus [i], null, MenusBorderStyle);
							menu.Run (Menus [i].Action);
							menu.Dispose ();
						} else if (!IsMenuOpen) {
							Activate (i);
						}
					} else if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked || me.Flags == MouseFlags.Button1TripleClicked) {
						if (IsMenuOpen && !Menus [i].IsTopLevel) {
							CloseAllMenus ();
						} else if (!Menus [i].IsTopLevel) {
							Activate (i);
						}
					} else if (_selected != i && _selected > -1 && (me.Flags == MouseFlags.ReportMousePosition ||
						me.Flags == MouseFlags.Button1Pressed && me.Flags == MouseFlags.ReportMousePosition)) {
						if (IsMenuOpen) {
							if (!CloseMenu (true, false)) {
								return true;
							}
							Activate (i);
						}
					} else if (IsMenuOpen) {
						if (!UseSubMenusSingleFrame || (UseSubMenusSingleFrame && openCurrentMenu != null
							&& openCurrentMenu._barItems.Parent != null && openCurrentMenu._barItems.Parent.Parent != Menus [i])) {

							Activate (i);
						}
					}
					return true;
				} else if (i == Menus.Length - 1 && me.Flags == MouseFlags.Button1Clicked) {
					if (IsMenuOpen && !Menus [i].IsTopLevel) {
						CloseAllMenus ();
						return true;
					}
				}
				pos += _leftPadding + Menus [i].TitleLength + _rightPadding;
			}
		}
		return false;
	}

	internal bool _handled;
	internal bool _isContextMenuLoading;
	Key _key = Key.F9;

	internal bool HandleGrabView (MouseEvent me, View current)
	{
		if (Application.MouseGrabView != null) {
			if (me.View is MenuBar || me.View is Menu) {
				var mbar = GetMouseGrabViewInstance (me.View);
				if (mbar != null) {
					if (me.Flags == MouseFlags.Button1Clicked) {
						mbar.CleanUp ();
						Application.GrabMouse (me.View);
					} else {
						_handled = false;
						return false;
					}
				}
				if (me.View != current) {
					Application.UngrabMouse ();
					var v = me.View;
					Application.GrabMouse (v);
					MouseEvent nme;
					if (me.Y > -1) {
						var newxy = v.ScreenToFrame (me.X, me.Y);
						nme = new MouseEvent () {
							X = newxy.X,
							Y = newxy.Y,
							Flags = me.Flags,
							OfX = me.X - newxy.X,
							OfY = me.Y - newxy.Y,
							View = v
						};
					} else {
						nme = new MouseEvent () {
							X = me.X + current.Frame.X,
							Y = 0,
							Flags = me.Flags,
							View = v
						};
					}

					v.MouseEvent (nme);
					return false;
				}
			} else if (!_isContextMenuLoading && !(me.View is MenuBar || me.View is Menu)
				&& me.Flags != MouseFlags.ReportMousePosition && me.Flags != 0) {

				Application.UngrabMouse ();
				if (IsMenuOpen)
					CloseAllMenus ();
				_handled = false;
				return false;
			} else {
				_handled = false;
				_isContextMenuLoading = false;
				return false;
			}
		} else if (!IsMenuOpen && (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked
			|| me.Flags == MouseFlags.Button1TripleClicked || me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))) {

			Application.GrabMouse (current);
		} else if (IsMenuOpen && (me.View is MenuBar || me.View is Menu)) {
			Application.GrabMouse (me.View);
		} else {
			_handled = false;
			return false;
		}

		_handled = true;

		return true;
	}

	MenuBar GetMouseGrabViewInstance (View view)
	{
		if (view == null || Application.MouseGrabView == null) {
			return null;
		}

		MenuBar hostView = null;
		if (view is MenuBar) {
			hostView = (MenuBar)view;
		} else if (view is Menu) {
			hostView = ((Menu)view)._host;
		}

		var grabView = Application.MouseGrabView;
		MenuBar hostGrabView = null;
		if (grabView is MenuBar) {
			hostGrabView = (MenuBar)grabView;
		} else if (grabView is Menu) {
			hostGrabView = ((Menu)grabView)._host;
		}

		return hostView != hostGrabView ? hostGrabView : null;
	}

	///<inheritdoc/>
	public override bool OnEnter (View view)
	{
		Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

		return base.OnEnter (view);
	}

	/// <summary>
	/// Gets the superview location offset relative to the <see cref="ConsoleDriver"/> location.
	/// </summary>
	/// <returns>The location offset.</returns>
	internal Point GetScreenOffset ()
	{
		if (Driver == null) {
			return Point.Empty;
		}
		var superViewFrame = SuperView == null ? new Rect (0, 0, Driver.Cols, Driver.Rows) : SuperView.Frame;
		var sv = SuperView == null ? Application.Current : SuperView;
		var boundsOffset = sv.GetBoundsOffset ();
		return new Point (superViewFrame.X - sv.Frame.X - boundsOffset.X,
			superViewFrame.Y - sv.Frame.Y - boundsOffset.Y);
	}

	/// <summary>
	/// Gets the <see cref="Application.Current"/> location offset relative to the <see cref="ConsoleDriver"/> location.
	/// </summary>
	/// <returns>The location offset.</returns>
	internal Point GetScreenOffsetFromCurrent ()
	{
		var screen = new Rect (0, 0, Driver.Cols, Driver.Rows);
		var currentFrame = Application.Current.Frame;
		var boundsOffset = Application.Top.GetBoundsOffset ();
		return new Point (screen.X - currentFrame.X - boundsOffset.X
			, screen.Y - currentFrame.Y - boundsOffset.Y);
	}
}

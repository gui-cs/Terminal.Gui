using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
/// <see cref="MenuBarItem"/> is a menu item on  <see cref="MenuBar"/>. 
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
		if (!isTopLevel && children == null) {
			throw new ArgumentNullException (nameof (children), "The parameter cannot be null. Use an empty array instead.");
		}
		SetTitle (title ?? "");
		if (parent != null) {
			Parent = parent;
		}
		if (children is List<MenuItem []> childrenList) {
			var newChildren = new MenuItem [] { };
			foreach (var grandChild in childrenList) {
				foreach (var child in grandChild) {
					SetParent (grandChild);
					Array.Resize (ref newChildren, newChildren.Length + 1);
					newChildren [newChildren.Length - 1] = child;
				}

			}
			Children = newChildren;
		} else if (children is MenuItem [] items) {
			SetParent (items);
			Children = items;
		} else {
			Children = null;
		}
	}

	void SetParent (MenuItem [] children)
	{
		foreach (var child in children) {
			if (child is { Parent: null }) {
				child.Parent = this;
			}
		}
	}

	/// <summary>
	/// Check if a <see cref="MenuItem"/> is a <see cref="MenuBarItem"/>.
	/// </summary>
	/// <param name="menuItem"></param>
	/// <returns>Returns a <see cref="MenuBarItem"/> or null otherwise.</returns>
	public MenuBarItem SubMenu (MenuItem menuItem)
	{
		return menuItem as MenuBarItem;
	}

	/// <summary>
	/// Check if a <see cref="MenuItem"/> is a submenu of this MenuBar.
	/// </summary>
	/// <param name="menuItem"></param>
	/// <returns>Returns <c>true</c> if it is a submenu. <c>false</c> otherwise.</returns>
	public bool IsSubMenuOf (MenuItem menuItem)
	{
		foreach (var child in Children) {
			if (child == menuItem && child.Parent == menuItem.Parent) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Get the index of a child <see cref="MenuItem"/>.
	/// </summary>
	/// <param name="children"></param>
	/// <returns>Returns a greater than -1 if the <see cref="MenuItem"/> is a child.</returns>
	public int GetChildrenIndex (MenuItem children)
	{
		int i = 0;
		if (Children != null) {
			foreach (var child in Children) {
				if (child == children) {
					return i;
				}
				i++;
			}
		}
		return -1;
	}

	void SetTitle (string title)
	{
		title ??= string.Empty;
		Title = title;
	}

	/// <summary>
	/// Gets or sets an array of <see cref="MenuItem"/> objects that are the children of this <see cref="MenuBarItem"/>
	/// </summary>
	/// <value>The children.</value>
	public MenuItem [] Children { get; set; }

	internal bool IsTopLevel => Parent == null && (Children == null || Children.Length == 0) && Action != null;

	internal void AddKeyBindings (MenuBar menuBar)
	{
		if (Children == null) {
			return;
		}
		foreach (var menuItem in Children.Where (m => m != null)) {
			if (menuItem.HotKey != default) {
				menuBar.KeyBindings.Add ((KeyCode)menuItem.HotKey.Value, Command.ToggleExpandCollapse);
				menuBar.KeyBindings.Add ((KeyCode)menuItem.HotKey.Value | KeyCode.AltMask, KeyBindingScope.HotKey, Command.ToggleExpandCollapse);
			}
			if (menuItem.Shortcut != KeyCode.Null) {
				menuBar.KeyBindings.Add (menuItem.Shortcut, KeyBindingScope.HotKey, Command.Select);
			}
			SubMenu (menuItem)?.AddKeyBindings (menuBar);
		}
	}
}

/// <summary>
/// <para>
/// Provides a menu bar that spans the top of a <see cref="Toplevel"/> View with drop-down and cascading menus.
/// </para>
/// <para>
/// By default, any sub-sub-menus (sub-menus of the <see cref="MenuItem"/>s added to <see cref="MenuBarItem"/>s) 
/// are displayed in a cascading manner, where each sub-sub-menu pops out of the sub-menu frame
/// (either to the right or left, depending on where the sub-menu is relative to the edge of the screen). By setting
/// <see cref="UseSubMenusSingleFrame"/> to <see langword="true"/>, this behavior can be changed such that all sub-sub-menus are
/// drawn within a single frame below the MenuBar.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MenuBar"/> appears on the first row of the <see cref="Toplevel"/> SuperView and uses the full width.
/// </para>
/// <para>
/// See also: <see cref="ContextMenu"/>
/// </para>
/// <para>
/// The <see cref="MenuBar"/> provides global hot keys for the application. See <see cref="MenuItem.HotKey"/>.
/// </para>
/// <para>
/// When the menu is created key bindings for each menu item and its sub-menu items are added for each menu item's
/// hot key (both alone AND with AltMask) and shortcut, if defined.
/// </para>
/// <para>
/// If a key press matches any of the menu item's hot keys or shortcuts, the menu item's action is invoked or
/// sub-menu opened.
/// </para>
/// <para>
/// * If the menu bar is not open
///   * Any shortcut defined within the menu will be invoked
///   * Only hot keys defined for the menu bar items will be invoked, and only if Alt is pressed too.
/// * If the menu bar is open
///   * Un-shifted hot keys defined for the menu bar items will be invoked, only if the menu they belong to is open (the menu bar item's text is visible).
///   * Alt-shifted hot keys defined for the menu bar items will be invoked, only if the menu they belong to is open (the menu bar item's text is visible).
///   * If there is a visible hot key that duplicates a shortcut (e.g. _File and Alt-F), the hot key wins.
/// </para>
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

	bool _useSubMenusSingleFrame;

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
		ColorScheme = Colors.ColorSchemes ["Menu"];
		WantMousePositionReports = true;
		IsMenuOpen = false;

		Added += MenuBar_Added;

		// Things this view knows how to do
		AddCommand (Command.Left, () => {
			MoveLeft ();
			return true;
		});
		AddCommand (Command.Right, () => {
			MoveRight ();
			return true;
		});
		AddCommand (Command.Cancel, () => {
			CloseMenuBar ();
			return true;
		});
		AddCommand (Command.Accept, () => {
			ProcessMenu (_selected, Menus [_selected]);
			return true;
		});

		AddCommand (Command.ToggleExpandCollapse, () => SelectOrRun ());
		AddCommand (Command.Select, () => Run (_menuItemToSelect?.Action));

		// Default key bindings for this view
		KeyBindings.Add (KeyCode.CursorLeft, Command.Left);
		KeyBindings.Add (KeyCode.CursorRight, Command.Right);
		KeyBindings.Add (KeyCode.Esc, Command.Cancel);
		KeyBindings.Add (KeyCode.CursorDown, Command.Accept);
		KeyBindings.Add (KeyCode.Enter, Command.Accept);
		KeyBindings.Add ((KeyCode)Key, KeyBindingScope.HotKey, Command.ToggleExpandCollapse);
		KeyBindings.Add (KeyCode.CtrlMask | KeyCode.Space, KeyBindingScope.HotKey, Command.ToggleExpandCollapse);

		// TODO: Bindings (esp for hotkey) should be added across and then down. This currently does down then across. 
		// TODO: As a result, _File._Save will have precedence over in "_File _Edit _ScrollbarView"
		// TODO: Also: Hotkeys should not work for sub-menus if they are not visible!
		if (Menus != null) {
			foreach (var menuBarItem in Menus?.Where (m => m != null)) {
				if (menuBarItem.HotKey != default) {
					KeyBindings.Add ((KeyCode)menuBarItem.HotKey.Value, Command.ToggleExpandCollapse);
					KeyBindings.Add ((KeyCode)menuBarItem.HotKey.Value | KeyCode.AltMask, KeyBindingScope.HotKey, Command.ToggleExpandCollapse);
				}
				if (menuBarItem.Shortcut != KeyCode.Null) {
					// Technically this will will never run because MenuBarItems don't have shortcuts
					KeyBindings.Add (menuBarItem.Shortcut, KeyBindingScope.HotKey, Command.Select);
				}
				menuBarItem.AddKeyBindings (this);
			}
		}

#if SUPPORT_ALT_TO_ACTIVATE_MENU
		// Enable the Alt key as a menu activator
		Initialized += (s, e) => {
			if (SuperView != null) {
				SuperView.KeyUp += SuperView_KeyUp;
			}
		};
#endif       
	}

#if SUPPORT_ALT_TO_ACTIVATE_MENU
	void SuperView_KeyUp (object sender, KeyEventArgs e)
	{
		if (SuperView == null || SuperView.CanFocus == false || SuperView.Visible == false) {
			return;
		}
		AltKeyUpHandler(e);
	}
#endif
	
	internal void AltKeyUpHandler (Key e)
	{
		if (e.KeyCode == KeyCode.AltMask) {
			e.Handled = true;
			// User pressed Alt 
			if (!IsMenuOpen && _openMenu == null && !_openedByAltKey) {
				// There's no open menu, the first menu item should be highlighted.
				// The right way to do this is to SetFocus(MenuBar), but for some reason
				// that faults.

				GetMouseGrabViewInstance (this)?.CleanUp ();

				IsMenuOpen = true;
				_openedByAltKey = true;
				_selected = 0;
				CanFocus = true;
				_lastFocused = SuperView == null ? Application.Current.MostFocused : SuperView.MostFocused;
				SetFocus ();
				SetNeedsDisplay ();
				Application.GrabMouse (this);
			} else if (!_openedByHotKey) {
				// There's an open menu. Close it.
				CleanUp ();
			} else {
				_openedByAltKey = false;
				_openedByHotKey = false;
			}
		}
	}

	#region Keyboard handling
	Key _key = Key.F9;

	/// <summary>
	/// The <see cref="Key"/> used to activate or close the menu bar by keyboard. The default is <see cref="Key.F9"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// If the user presses any <see cref="MenuItem.HotKey"/>s defined in the <see cref="MenuBarItem"/>s, the menu bar will be activated and the sub-menu will be opened.
	/// </para>
	/// <para>
	/// <see cref="Key.Esc"/> will close the menu bar and any open sub-menus.
	/// </para>
	/// </remarks>
	public Key Key {
		get => _key;
		set {
			if (_key == value) {
				return;
			}
			KeyBindings.Remove (_key);
			KeyBindings.Add (value, KeyBindingScope.HotKey, Command.ToggleExpandCollapse);
			_key = value;
		}
	}


	bool _useKeysUpDownAsKeysLeftRight = false;

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
	/// The specifier character for the hot keys.
	/// </summary>
	public new static Rune HotKeySpecifier => (Rune)'_';

	// Set in OnInvokingKeyBindings. -1 means no menu item is selected for activation.
	int _menuBarItemToActivate;

	// Set in OnInvokingKeyBindings. null means no sub-menu is selected for activation.
	MenuItem _menuItemToSelect;
	bool _openedByAltKey;
	bool _openedByHotKey;

	/// <summary>
	/// Called when a key bound to Command.Select is pressed. Either activates the menu item or runs it, depending on whether it has a sub-menu.
	/// If the menu is open, it will close the menu bar.
	/// </summary>
	/// <returns></returns>
	bool SelectOrRun ()
	{
		if (!IsInitialized || !Visible) {
			return true;
		}

		_openedByHotKey = true;
		if (_menuBarItemToActivate != -1) {
			Activate (_menuBarItemToActivate);
		} else if (_menuItemToSelect != null) {
			Run (_menuItemToSelect.Action);
		} else {
			if (IsMenuOpen && _openMenu != null) {
				CloseAllMenus ();
			} else {
				OpenMenu ();
			}

		}
		return true;
	}

	/// <inheritdoc/>
	public override bool? OnInvokingKeyBindings (Key keyEvent)
	{
		// This is a bit of a hack. We want to handle the key bindings for menu bar but
		// InvokeKeyBindings doesn't pass any context so we can't tell which item it is for.
		// So before we call the base class we set SelectedItem appropriately.
		// TODO: Figure out if there's a way to have KeyBindings pass context instead. Maybe a KeyBindingContext property?

		var key = keyEvent.KeyCode;

		if (KeyBindings.TryGet (key, out _)) {
			_menuBarItemToActivate = -1;
			_menuItemToSelect = null;

			// Search for shortcuts first. If there's a shortcut, we don't want to activate the menu item.
			for (int i = 0; i < Menus.Length; i++) {
				// Recurse through the menu to find one with the shortcut.
				if (FindShortcutInChildMenu (key, Menus [i], out _menuItemToSelect)) {
					_menuBarItemToActivate = i;
					keyEvent.Scope = KeyBindingScope.HotKey;
					return base.OnInvokingKeyBindings (keyEvent);
				}

				// Now see if any of the menu bar items have a hot key that matches
				// Technically this is not possible because menu bar items don't have 
				// shortcuts or Actions. But it's here for completeness. 
				var shortcut = Menus [i]?.Shortcut;
				if (key == shortcut) {
					throw new InvalidOperationException ("Menu bar items cannot have shortcuts");
				}

			}

			// Search for hot keys next.
			for (int i = 0; i < Menus.Length; i++) {
				if (IsMenuOpen) {
					// We don't need to do anything because `Menu` will handle the key binding.
					//break;
				}

				// No submenu item matched (or the menu is closed)

				// Check if one of the menu bar item has a hot key that matches
				int hotKeyValue = Menus [i]?.HotKey.Value ?? default;
				var hotKey = (KeyCode)hotKeyValue;
				if (hotKey != KeyCode.Null) {
					bool matches = key == hotKey || key == (hotKey | KeyCode.AltMask);
					if (IsMenuOpen) {
						// If the menu is open, only match if Alt is not pressed.
						matches = key == hotKey;
					}

					if (matches) {
						_menuBarItemToActivate = i;
						keyEvent.Scope = KeyBindingScope.HotKey;
						break;
					}
				}

			}
		}
		return base.OnInvokingKeyBindings (keyEvent);
	}

	// TODO: Update to use Key instead of KeyCode
	// Recurse the child menus looking for a shortcut that matches the key
	bool FindShortcutInChildMenu (KeyCode key, MenuBarItem menuBarItem, out MenuItem menuItemToSelect)
	{
		menuItemToSelect = null;

		if (key == KeyCode.Null || menuBarItem?.Children == null) {
			return false;
		}

		for (int c = 0; c < menuBarItem.Children.Length; c++) {
			var menuItem = menuBarItem.Children [c];
			if (key == menuItem?.Shortcut) {
				menuItemToSelect = menuItem;
				return true;
			}
			var subMenu = menuBarItem.SubMenu (menuItem);
			if (subMenu != null) {
				if (FindShortcutInChildMenu (key, subMenu, out menuItemToSelect)) {
					return true;
				}
			}
		}
		return false;
	}
#endregion Keyboard handling

	bool _initialCanFocus;

	void MenuBar_Added (object sender, SuperViewChangedEventArgs e)
	{
		_initialCanFocus = CanFocus;
		Added -= MenuBar_Added;
	}

	bool _isCleaning;

	internal void CleanUp ()
	{
		_isCleaning = true;
		if (_openMenu != null) {
			CloseAllMenus ();
		}
		_openedByAltKey = false;
		_openedByHotKey = false;
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
		for (int i = 0; i < Frame.Width; i++) {
			Driver.AddRune ((Rune)' ');
		}

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

	/// <summary>
	/// Called when an item is selected; Runs the action.
	/// </summary>
	/// <param name="item"></param>
	internal bool SelectItem (MenuItem item)
	{
		if (item?.Action == null) {
			return false;
		}

		Application.UngrabMouse ();
		CloseAllMenus ();
		Application.Refresh ();
		_openedByAltKey = true;
		return Run (item?.Action);
	}

	internal bool Run (Action action)
	{
		if (action == null) {
			return false;
		}
		Application.MainLoop.AddIdle (() => {
			action ();
			return false;
		});
		return true;
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
	Menu _ocm;

	internal Menu openCurrentMenu {
		get => _ocm;
		set {
			if (_ocm != value) {
				_ocm = value;
				if (_ocm != null && _ocm._currentChild > -1) {
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
			_lastFocused ??= SuperView == null ? Application.Current?.MostFocused : SuperView.MostFocused;
			if (_openSubMenu != null && !CloseMenu (false, true)) {
				return;
			}
			if (_openMenu != null) {
				Application.Current.Remove (_openMenu);
				_openMenu.Dispose ();
				_openMenu = null;
			}

			// This positions the submenu horizontally aligned with the first character of the
			// text belonging to the menu 
			for (int i = 0; i < index; i++) {
				pos += Menus [i].TitleLength + (Menus [i].Help.GetColumns () > 0 ? Menus [i].Help.GetColumns () + 2 : 0) + _leftPadding + _rightPadding;
			}

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
			if (_openSubMenu == null) {
				_openSubMenu = new List<Menu> ();
			}
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

		if (_openMenu != null) {
			return;
		}
		_selected = 0;
		SetNeedsDisplay ();

		_previousFocused = SuperView == null ? Application.Current.Focused : SuperView.Focused;
		OpenMenu (_selected);
		if (!SelectEnabledItem (openCurrentMenu._barItems.Children, openCurrentMenu._currentChild, out openCurrentMenu._currentChild) && !CloseMenu (false)) {
			return;
		}
		if (!openCurrentMenu.CheckSubMenu ()) {
			return;
		}
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
		_reopen = reopen;
		var args = OnMenuClosing (mbi, reopen, isSubMenu);
		if (args.Cancel) {
			_isMenuClosing = false;
			if (args.CurrentMenu.Parent != null) {
				_openMenu._currentChild = ((MenuBarItem)args.CurrentMenu.Parent).Children.IndexOf (args.CurrentMenu);
			}
			return false;
		}
		switch (isSubMenu) {
		case false:
			if (_openMenu != null) {
				Application.Current.Remove (_openMenu);
			}
			SetNeedsDisplay ();
			if (_previousFocused != null && _previousFocused is Menu && _openMenu != null && _previousFocused.ToString () != openCurrentMenu.ToString ()) {
				_previousFocused.SetFocus ();
			}
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
		_reopen = false;
		_isMenuClosing = false;
		return true;
	}

	void RemoveSubMenu (int index, bool ignoreUseSubMenusSingleFrame = false)
	{
		if (_openSubMenu == null || UseSubMenusSingleFrame
			&& !ignoreUseSubMenusSingleFrame && _openSubMenu.Count == 0) {
			return;
		}
		for (int i = _openSubMenu.Count - 1; i > index; i--) {
			_isMenuClosing = true;
			Menu menu;
			if (_openSubMenu.Count - 1 > 0) {
				menu = _openSubMenu [i - 1];
			} else {
				menu = _openMenu;
			}
			if (!menu.Visible) {
				menu.Visible = true;
			}
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
		if (_openSubMenu.Count > 0) {
			openCurrentMenu = _openSubMenu.Last ();
		}

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
			if (_openSubMenu != null && !CloseMenu (false, true, true)) {
				return;
			}
			if (!CloseMenu (false)) {
				return;
			}
			if (LastFocused != null && LastFocused != this) {
				_selected = -1;
			}
			Application.UngrabMouse ();
		}
		IsMenuOpen = false;
		_openedByAltKey = false;
		_openedByHotKey = false;
		OnMenuAllClosed ();
	}

	internal void PreviousMenu (bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
	{
		switch (isSubMenu) {
		case false:
			if (_selected <= 0) {
				_selected = Menus.Length - 1;
			} else {
				_selected--;
			}

			if (_selected > -1 && !CloseMenu (true, false, ignoreUseSubMenusSingleFrame)) {
				return;
			}
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
			} else {
				PreviousMenu ();
			}

			break;
		}
	}

	internal void NextMenu (bool isSubMenu = false, bool ignoreUseSubMenusSingleFrame = false)
	{
		switch (isSubMenu) {
		case false:
			if (_selected == -1) {
				_selected = 0;
			} else if (_selected + 1 == Menus.Length) {
				_selected = 0;
			} else {
				_selected++;
			}

			if (_selected > -1 && !CloseMenu (true, ignoreUseSubMenusSingleFrame)) {
				return;
			}
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
					if (_openSubMenu != null && !CloseMenu (false, true)) {
						return;
					}
					NextMenu (false, ignoreUseSubMenusSingleFrame);
				} else if (subMenu != null || openCurrentMenu._currentChild > -1
					&& !openCurrentMenu._barItems.Children [openCurrentMenu._currentChild].IsFromSubMenu) {
					_selectedSub++;
					openCurrentMenu.CheckSubMenu ();
				} else {
					if (CloseMenu (false, true, ignoreUseSubMenusSingleFrame)) {
						NextMenu (false, ignoreUseSubMenusSingleFrame);
					}
					return;
				}

				SetNeedsDisplay ();
				if (UseKeysUpDownAsKeysLeftRight) {
					openCurrentMenu.CheckSubMenu ();
				}
			}
			break;
		}
	}

	void ProcessMenu (int i, MenuBarItem mi)
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
			Application.GrabMouse (this);
			_selected = i;
			OpenMenu (i);
			if (!SelectEnabledItem (openCurrentMenu._barItems.Children, openCurrentMenu._currentChild, out openCurrentMenu._currentChild) && !CloseMenu (false)) {
				return;
			}
			if (!openCurrentMenu.CheckSubMenu ()) {
				return;
			}
		}
		SetNeedsDisplay ();
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

	#region Mouse Handling
	///<inheritdoc/>
	public override bool OnEnter (View view)
	{
		Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

		return base.OnEnter (view);
	}

	///<inheritdoc/>
	public override bool OnLeave (View view)
	{
		if ((!(view is MenuBar) && !(view is Menu) || !(view is MenuBar) && !(view is Menu) && _openMenu != null) && !_isCleaning && !_reopen) {
			CleanUp ();
		}
		return base.OnLeave (view);
	}

	///<inheritdoc/>
	public override bool MouseEvent (MouseEvent me)
	{
		if (!_handled && !HandleGrabView (me, this)) {
			return false;
		}
		_handled = false;

		if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked || me.Flags == MouseFlags.Button1TripleClicked || me.Flags == MouseFlags.Button1Clicked ||
		me.Flags == MouseFlags.ReportMousePosition && _selected > -1 ||
		me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && _selected > -1) {
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
						if (!UseSubMenusSingleFrame || UseSubMenusSingleFrame && openCurrentMenu != null
													&& openCurrentMenu._barItems.Parent != null && openCurrentMenu._barItems.Parent.Parent != Menus [i]) {

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
				if (IsMenuOpen) {
					CloseAllMenus ();
				}
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
	#endregion Mouse Handling

	/// <summary>
	/// Gets the superview location offset relative to the <see cref="ConsoleDriver"/> location.
	/// </summary>
	/// <returns>The location offset.</returns>
	internal Point GetScreenOffset ()
	{
		if (Driver == null) {
			return Point.Empty;
		}
		var superViewFrame = SuperView == null ? Driver.Bounds : SuperView.Frame;
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
		var screen = Driver.Bounds;
		var currentFrame = Application.Current.Frame;
		var boundsOffset = Application.Top.GetBoundsOffset ();
		return new Point (screen.X - currentFrame.X - boundsOffset.X
			, screen.Y - currentFrame.Y - boundsOffset.Y);
	}
}
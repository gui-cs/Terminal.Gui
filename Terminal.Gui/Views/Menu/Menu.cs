using System;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Reflection.Metadata;

namespace Terminal.Gui;

/// <summary>
/// Specifies how a <see cref="MenuItem"/> shows selection state. 
/// </summary>
[Flags]
public enum MenuItemCheckStyle {
	/// <summary>
	/// The menu item will be shown normally, with no check indicator. The default.
	/// </summary>
	NoCheck = 0b_0000_0000,

	/// <summary>
	/// The menu item will indicate checked/un-checked state (see <see cref="Checked"/>).
	/// </summary>
	Checked = 0b_0000_0001,

	/// <summary>
	/// The menu item is part of a menu radio group (see <see cref="Checked"/>) and will indicate selected state.
	/// </summary>
	Radio = 0b_0000_0010
};

/// <summary>
/// A <see cref="MenuItem"/> has title, an associated help text, and an action to execute on activation. 
/// MenuItems can also have a checked indicator (see <see cref="Checked"/>).
/// </summary>
public class MenuItem {
	string _title;
	ShortcutHelper _shortcutHelper;
	bool _allowNullChecked;
	MenuItemCheckStyle _checkType;

	internal int TitleLength => GetMenuBarItemLength (Title);

	/// <summary>
	/// Gets or sets arbitrary data for the menu item.
	/// </summary>
	/// <remarks>This property is not used internally.</remarks>
	public object Data { get; set; }

	// TODO: Update to use Key instead of KeyCode
	/// <summary>
	/// Initializes a new instance of <see cref="MenuItem"/>
	/// </summary>
	public MenuItem (KeyCode shortcut = KeyCode.Null) : this ("", "", null, null, null, shortcut) { }

	// TODO: Update to use Key instead of KeyCode
	/// <summary>
	/// Initializes a new instance of <see cref="MenuItem"/>.
	/// </summary>
	/// <param name="title">Title for the menu item.</param>
	/// <param name="help">Help text to display.</param>
	/// <param name="action">Action to invoke when the menu item is activated.</param>
	/// <param name="canExecute">Function to determine if the action can currently be executed.</param>
	/// <param name="parent">The <see cref="Parent"/> of this menu item.</param>
	/// <param name="shortcut">The <see cref="Shortcut"/> keystroke combination.</param>
	public MenuItem (string title, string help, Action action, Func<bool> canExecute = null, MenuItem parent = null, KeyCode shortcut = KeyCode.Null)
	{
		Title = title ?? "";
		Help = help ?? "";
		Action = action;
		CanExecute = canExecute;
		Parent = parent;
		_shortcutHelper = new ShortcutHelper ();
		if (shortcut != KeyCode.Null) {
			Shortcut = shortcut;
		}
	}

	#region Keyboard Handling

	// TODO: Update to use Key instead of Rune
	/// <summary>
	/// The HotKey is used to activate a <see cref="MenuItem"/> with the keyboard. HotKeys are defined by prefixing the <see cref="Title"/>
	/// of a MenuItem with an underscore ('_'). 
	/// <para>
	/// Pressing Alt-Hotkey for a <see cref="MenuBarItem"/> (menu items on the menu bar) works even if the menu is not active). 
	/// Once a menu has focus and is active, pressing just the HotKey will activate the MenuItem.
	/// </para>
	/// <para>
	/// For example for a MenuBar with a "_File" MenuBarItem that contains a "_New" MenuItem, Alt-F will open the File menu.
	/// Pressing the N key will then activate the New MenuItem.
	/// </para>
	/// <para>
	/// See also <see cref="Shortcut"/> which enable global key-bindings to menu items.
	/// </para>
	/// </summary>
	public Rune HotKey { get; set; }

	void GetHotKey ()
	{
		bool nextIsHot = false;
		foreach (char x in _title) {
			if (x == MenuBar.HotKeySpecifier.Value) {
				nextIsHot = true;
			} else {
				if (nextIsHot) {
					HotKey = (Rune)char.ToUpper (x);
					break;
				}
				nextIsHot = false;
				HotKey = default;
			}
		}
	}


	// TODO: Update to use Key instead of KeyCode
	/// <summary>
	/// Shortcut defines a key binding to the MenuItem that will invoke the MenuItem's action globally for the <see cref="View"/> that is
	/// the parent of the <see cref="MenuBar"/> or <see cref="ContextMenu"/> this <see cref="MenuItem"/>.
	/// <para>
	/// The <see cref="KeyCode"/> will be drawn on the MenuItem to the right of the <see cref="Title"/> and <see cref="Help"/> text. See <see cref="ShortcutTag"/>.
	/// </para>
	/// </summary>
	public KeyCode Shortcut {
		get => _shortcutHelper.Shortcut;
		set {

			if (_shortcutHelper.Shortcut != value && (ShortcutHelper.PostShortcutValidation (value) || value == KeyCode.Null)) {
				_shortcutHelper.Shortcut = value;
			}
		}
	}

	/// <summary>
	/// Gets the text describing the keystroke combination defined by <see cref="Shortcut"/>.
	/// </summary>
	public string ShortcutTag => _shortcutHelper.Shortcut == KeyCode.Null ? string.Empty : Key.ToString (_shortcutHelper.Shortcut, MenuBar.ShortcutDelimiter);
	#endregion Keyboard Handling

	/// <summary>
	/// Gets or sets the title of the menu item .
	/// </summary>
	/// <value>The title.</value>
	public string Title {
		get => _title;
		set {
			if (_title != value) {
				_title = value;
				GetHotKey ();
			}
		}
	}

	/// <summary>
	/// Gets or sets the help text for the menu item. The help text is drawn to the right of the <see cref="Title"/>.
	/// </summary>
	/// <value>The help text.</value>
	public string Help { get; set; }

	/// <summary>
	/// Gets or sets the action to be invoked when the menu item is triggered.
	/// </summary>
	/// <value>Method to invoke.</value>
	public Action Action { get; set; }

	/// <summary>
	/// Gets or sets the action to be invoked to determine if the menu can be triggered. If <see cref="CanExecute"/> returns <see langword="true"/>
	/// the menu item will be enabled. Otherwise, it will be disabled. 
	/// </summary>
	/// <value>Function to determine if the action is can be executed or not.</value>
	public Func<bool> CanExecute { get; set; }

	/// <summary>
	/// Returns <see langword="true"/> if the menu item is enabled. This method is a wrapper around <see cref="CanExecute"/>.
	/// </summary>
	public bool IsEnabled ()
	{
		return CanExecute == null ? true : CanExecute ();
	}

	// 
	// ┌─────────────────────────────┐
	// │ Quit  Quit UI Catalog  Ctrl+Q │
	// └─────────────────────────────┘
	// ┌─────────────────┐
	// │ ◌ TopLevel Alt+T │
	// └─────────────────┘
	// TODO: Replace the `2` literals with named constants 
	internal int Width => 1 + // space before Title
				TitleLength +
				2 + // space after Title - BUGBUG: This should be 1 
				(Checked == true || CheckType.HasFlag (MenuItemCheckStyle.Checked) || CheckType.HasFlag (MenuItemCheckStyle.Radio) ? 2 : 0) + // check glyph + space 
				(Help.GetColumns () > 0 ? 2 + Help.GetColumns () : 0) + // Two spaces before Help
				(ShortcutTag.GetColumns () > 0 ? 2 + ShortcutTag.GetColumns () : 0); // Pad two spaces before shortcut tag (which are also aligned right)

	/// <summary>
	/// Sets or gets whether the <see cref="MenuItem"/> shows a check indicator or not. See <see cref="MenuItemCheckStyle"/>.
	/// </summary>
	public bool? Checked { set; get; }

	/// <summary>
	/// Used only if <see cref="CheckType"/> is of <see cref="MenuItemCheckStyle.Checked"/> type.
	/// If <see langword="true"/> allows <see cref="Checked"/> to be null, true or false.
	/// If <see langword="false"/> only allows <see cref="Checked"/> to be true or false.
	/// </summary>
	public bool AllowNullChecked {
		get => _allowNullChecked;
		set {
			_allowNullChecked = value;
			if (Checked == null) {
				Checked = false;
			}
		}
	}

	/// <summary>
	/// Sets or gets the <see cref="MenuItemCheckStyle"/> of a menu item where <see cref="Checked"/> is set to <see langword="true"/>.
	/// </summary>
	public MenuItemCheckStyle CheckType {
		get => _checkType;
		set {
			_checkType = value;
			if (_checkType == MenuItemCheckStyle.Checked && !_allowNullChecked && Checked == null) {
				Checked = false;
			}
		}
	}

	/// <summary>
	/// Gets the parent for this <see cref="MenuItem"/>.
	/// </summary>
	/// <value>The parent.</value>
	public MenuItem Parent { get; set; }

	/// <summary>
	/// Gets if this <see cref="MenuItem"/> is from a sub-menu.
	/// </summary>
	internal bool IsFromSubMenu => Parent != null;

	/// <summary>
	/// Merely a debugging aid to see the interaction with main.
	/// </summary>
	public MenuItem GetMenuItem ()
	{
		return this;
	}

	/// <summary>
	/// Merely a debugging aid to see the interaction with main.
	/// </summary>
	public bool GetMenuBarItem ()
	{
		return IsFromSubMenu;
	}

	/// <summary>
	/// Toggle the <see cref="Checked"/> between three states if <see cref="AllowNullChecked"/> is <see langword="true"/>
	/// or between two states if <see cref="AllowNullChecked"/> is <see langword="false"/>.
	/// </summary>
	public void ToggleChecked ()
	{
		if (_checkType != MenuItemCheckStyle.Checked) {
			throw new InvalidOperationException ("This isn't a Checked MenuItemCheckStyle!");
		}
		bool? previousChecked = Checked;
		if (AllowNullChecked) {
			switch (previousChecked) {
			case null:
				Checked = true;
				break;
			case true:
				Checked = false;
				break;
			case false:
				Checked = null;
				break;
			}
		} else {
			Checked = !Checked;
		}
	}


	int GetMenuBarItemLength (string title)
	{
		int len = 0;
		foreach (var ch in title.EnumerateRunes ()) {
			if (ch == MenuBar.HotKeySpecifier) {
				continue;
			}
			len += Math.Max (ch.GetColumns (), 1);
		}

		return len;
	}
}

/// <summary>
/// An internal class used to represent a menu pop-up menu. Created and managed by <see cref="MenuBar"/> and <see cref="ContextMenu"/>.
/// </summary>
class Menu : View {
	internal MenuBarItem _barItems;
	internal MenuBar _host;
	internal int _currentChild;
	internal View _previousSubFocused;

	internal static Rect MakeFrame (int x, int y, MenuItem [] items, Menu parent = null, LineStyle border = LineStyle.Single)
	{
		if (items == null || items.Length == 0) {
			return new Rect ();
		}
		int minX = x;
		int minY = y;
		int borderOffset = 2; // This 2 is for the space around
		int maxW = (items.Max (z => z?.Width) ?? 0) + borderOffset;
		int maxH = items.Length + borderOffset;
		if (parent != null && x + maxW > Driver.Cols) {
			minX = Math.Max (parent.Frame.Right - parent.Frame.Width - maxW, 0);
		}
		if (y + maxH > Driver.Rows) {
			minY = Math.Max (Driver.Rows - maxH, 0);
		}
		return new Rect (minX, minY, maxW, maxH);
	}

	public Menu (MenuBar host, int x, int y, MenuBarItem barItems, Menu parent = null, LineStyle border = LineStyle.Single)
		: base (MakeFrame (x, y, barItems?.Children, parent, border))
	{
		if (host == null) {
			throw new ArgumentNullException (nameof (host));
		}

		if (barItems == null) {
			throw new ArgumentNullException (nameof (barItems));
		}

		_host = host;
		_barItems = barItems;

		if (barItems is { IsTopLevel: true }) {
			// This is a standalone MenuItem on a MenuBar
			ColorScheme = host.ColorScheme;
			CanFocus = true;
		} else {

			_currentChild = -1;
			for (int i = 0; i < barItems.Children?.Length; i++) {
				if (barItems.Children [i]?.IsEnabled () == true) {
					_currentChild = i;
					break;
				}

			}
			ColorScheme = host.ColorScheme;
			CanFocus = true;
			WantMousePositionReports = host.WantMousePositionReports;
		}

		BorderStyle = host.MenusBorderStyle;

		if (Application.Current != null) {
			Application.Current.DrawContentComplete += Current_DrawContentComplete;
			Application.Current.SizeChanging += Current_TerminalResized;
		}
		Application.MouseEvent += Application_RootMouseEvent;

		// Things this view knows how to do
		AddCommand (Command.LineUp, () => MoveUp ());
		AddCommand (Command.LineDown, () => MoveDown ());
		AddCommand (Command.Left, () => {
			_host.PreviousMenu (true);
			return true;
		});
		AddCommand (Command.Right, () => {
			_host.NextMenu (!_barItems.IsTopLevel || _barItems.Children != null
				&& _barItems.Children.Length > 0 && _currentChild > -1
				&& _currentChild < _barItems.Children.Length && _barItems.Children [_currentChild].IsFromSubMenu,
				_barItems.Children != null && _barItems.Children.Length > 0 && _currentChild > -1
				&& host.UseSubMenusSingleFrame && _barItems.SubMenu (_barItems.Children [_currentChild]) != null);

			return true;
		});
		AddCommand (Command.Cancel, () => {
			CloseAllMenus ();
			return true;
		});
		AddCommand (Command.Accept, () => {
			RunSelected ();
			return true;
		});
		AddCommand (Command.Select, () => _host?.SelectItem (_menuItemToSelect));
		AddCommand (Command.ToggleExpandCollapse, () => SelectOrRun ());
		AddCommand (Command.Default, () => _host?.SelectItem (_menuItemToSelect));

		// Default key bindings for this view
		KeyBindings.Add (KeyCode.CursorUp, Command.LineUp);
		KeyBindings.Add (KeyCode.CursorDown, Command.LineDown);
		KeyBindings.Add (KeyCode.CursorLeft, Command.Left);
		KeyBindings.Add (KeyCode.CursorRight, Command.Right);
		KeyBindings.Add (KeyCode.Esc, Command.Cancel);
		KeyBindings.Add (KeyCode.Enter, Command.Accept);
		KeyBindings.Add (KeyCode.F9, KeyBindingScope.HotKey, Command.ToggleExpandCollapse);
		KeyBindings.Add (KeyCode.CtrlMask | KeyCode.Space, KeyBindingScope.HotKey, Command.ToggleExpandCollapse);

		AddKeyBindings (barItems);
#if SUPPORT_ALT_TO_ACTIVATE_MENU
		Initialized += (s, e) => {
			if (SuperView != null) {
				SuperView.KeyUp += SuperView_KeyUp;
			}
		};
#endif
		// Debugging aid so ToString() is helpful
		Text = _barItems.Title;
	}


#if SUPPORT_ALT_TO_ACTIVATE_MENU
	void SuperView_KeyUp (object sender, KeyEventArgs e)
	{
		if (SuperView == null || SuperView.CanFocus == false || SuperView.Visible == false) {
			return;
		}
		_host.AltKeyUpHandler (e);
	}
#endif

	void AddKeyBindings (MenuBarItem menuBarItem)
	{
		if (menuBarItem == null || menuBarItem.Children == null) {
			return;
		}
		foreach (var menuItem in menuBarItem.Children.Where (m => m != null)) {
			KeyBindings.Add ((KeyCode)menuItem.HotKey.Value, Command.ToggleExpandCollapse);
			KeyBindings.Add ((KeyCode)menuItem.HotKey.Value | KeyCode.AltMask, Command.ToggleExpandCollapse);
			if (menuItem.Shortcut != KeyCode.Null) {
				KeyBindings.Add (menuItem.Shortcut, KeyBindingScope.HotKey, Command.Select);
			}
			var subMenu = menuBarItem.SubMenu (menuItem);
			AddKeyBindings (subMenu);
		}
	}

	int _menuBarItemToActivate = -1;
	MenuItem _menuItemToSelect;

	/// <summary>
	/// Called when a key bound to Command.Select is pressed. This means a hot key was pressed.
	/// </summary>
	/// <returns></returns>
	bool SelectOrRun ()
	{
		if (!IsInitialized || !Visible) {
			return true;
		}

		if (_menuBarItemToActivate != -1) {
			_host.Activate (1, _menuBarItemToActivate);
		} else if (_menuItemToSelect != null) {
			var m = _menuItemToSelect as MenuBarItem;
			if (m?.Children?.Length > 0) {

				var item = _barItems.Children [_currentChild];
				if (item == null) {
					return true;
				}
				bool disabled = item == null || !item.IsEnabled ();
				if (!disabled && (_host.UseSubMenusSingleFrame || !CheckSubMenu ())) {
					SetNeedsDisplay ();
					SetParentSetNeedsDisplay ();
					return true;
				}
				if (!disabled) {
					_host.OnMenuOpened ();
				}

			} else {
				_host.SelectItem (_menuItemToSelect);
			}
		} else if (_host.IsMenuOpen) {
			_host.CloseAllMenus ();
		} else {
			_host.OpenMenu ();
		}
		//_openedByHotKey = true;
		return true;
	}

	/// <inheritdoc/>
	public override bool? OnInvokingKeyBindings (Key keyEvent)
	{
		// This is a bit of a hack. We want to handle the key bindings for menu bar but
		// InvokeKeyBindings doesn't pass any context so we can't tell which item it is for.
		// So before we call the base class we set SelectedItem appropriately.

		var key = keyEvent.KeyCode;

		if (KeyBindings.TryGet (key, out _)) {
			_menuBarItemToActivate = -1;
			_menuItemToSelect = null;

			var children = _barItems.Children;
			if (children == null) {
				return base.OnInvokingKeyBindings (keyEvent);
			}

			// Search for shortcuts first. If there's a shortcut, we don't want to activate the menu item.
			foreach (var c in children) {
				if (key == c?.Shortcut) {
					_menuBarItemToActivate = -1;
					_menuItemToSelect = c;
					keyEvent.Scope = KeyBindingScope.HotKey;
					return base.OnInvokingKeyBindings (keyEvent);
				}
				var subMenu = _barItems.SubMenu (c);
				if (FindShortcutInChildMenu (key, subMenu)) {
					keyEvent.Scope = KeyBindingScope.HotKey;
					return base.OnInvokingKeyBindings (keyEvent);
				}
			}

			// Search for hot keys next.
			for (int c = 0; c < children.Length; c++) {
				int hotKeyValue = children [c]?.HotKey.Value ?? default;
				var hotKey = (KeyCode)hotKeyValue;
				if (hotKey == KeyCode.Null) {
					continue;
				}
				bool matches = key == hotKey || key == (hotKey | KeyCode.AltMask);
				if (!_host.IsMenuOpen) {
					// If the menu is open, only match if Alt is not pressed.
					matches = key == hotKey;
				}

				if (matches) {
					_menuItemToSelect = children [c];
					_currentChild = c;
					return base.OnInvokingKeyBindings (keyEvent);
				}
			}
		}

		var handled = base.OnInvokingKeyBindings (keyEvent);
		if (handled != null && (bool)handled) {
			return true;
		}

		// This supports the case where the menu bar is a context menu
		return _host.OnInvokingKeyBindings (keyEvent);
	}

	bool FindShortcutInChildMenu (KeyCode key, MenuBarItem menuBarItem)
	{
		if (menuBarItem == null || menuBarItem.Children == null) {
			return false;
		}
		foreach (var menuItem in menuBarItem.Children) {
			if (key == menuItem?.Shortcut) {
				_menuBarItemToActivate = -1;
				_menuItemToSelect = menuItem;
				return true;
			}
			var subMenu = menuBarItem.SubMenu (menuItem);
			FindShortcutInChildMenu (key, subMenu);
		}
		return false;
	}

	void Current_TerminalResized (object sender, SizeChangedEventArgs e)
	{
		if (_host.IsMenuOpen) {
			_host.CloseAllMenus ();
		}
	}

	/// <inheritdoc/>
	public override void OnVisibleChanged ()
	{
		base.OnVisibleChanged ();
		if (Visible) {
			Application.MouseEvent += Application_RootMouseEvent;
		} else {
			Application.MouseEvent -= Application_RootMouseEvent;
		}
	}

	void Application_RootMouseEvent (object sender, MouseEventEventArgs a)
	{
		if (a.MouseEvent.View is MenuBar) {
			return;
		}
		var locationOffset = _host.GetScreenOffsetFromCurrent ();
		if (SuperView != null && SuperView != Application.Current) {
			locationOffset.X += SuperView.Border.Thickness.Left;
			locationOffset.Y += SuperView.Border.Thickness.Top;
		}
		var view = FindDeepestView (this, a.MouseEvent.X + locationOffset.X, a.MouseEvent.Y + locationOffset.Y, out int rx, out int ry);
		if (view == this) {
			if (!Visible) {
				throw new InvalidOperationException ("This shouldn't running on a invisible menu!");
			}

			var nme = new MouseEvent () {
				X = rx,
				Y = ry,
				Flags = a.MouseEvent.Flags,
				View = view
			};
			if (MouseEvent (nme) || a.MouseEvent.Flags == MouseFlags.Button1Pressed || a.MouseEvent.Flags == MouseFlags.Button1Released) {
				a.MouseEvent.Handled = true;
			}
		}
	}

	internal Attribute DetermineColorSchemeFor (MenuItem item, int index)
	{
		if (item != null) {
			if (index == _currentChild) {
				return ColorScheme.Focus;
			}
			if (!item.IsEnabled ()) {
				return ColorScheme.Disabled;
			}
		}
		return GetNormalColor ();
	}

	public override void OnDrawContent (Rect contentArea)
	{
		if (_barItems.Children == null) {
			return;
		}
		var savedClip = Driver.Clip;
		Driver.Clip = new Rect (0, 0, Driver.Cols, Driver.Rows);
		Driver.SetAttribute (GetNormalColor ());

		OnDrawAdornments ();
		OnRenderLineCanvas ();

		for (int i = Bounds.Y; i < _barItems.Children.Length; i++) {
			if (i < 0) {
				continue;
			}
			if (BoundsToScreen (Bounds).Y + i >= Driver.Rows) {
				break;
			}
			var item = _barItems.Children [i];
			Driver.SetAttribute (item == null ? GetNormalColor ()
				: i == _currentChild ? ColorScheme.Focus : GetNormalColor ());
			if (item == null && BorderStyle != LineStyle.None) {
				Move (-1, i);
				Driver.AddRune (Glyphs.LeftTee);
			} else if (Frame.X < Driver.Cols) {
				Move (0, i);
			}

			Driver.SetAttribute (DetermineColorSchemeFor (item, i));
			for (int p = Bounds.X; p < Frame.Width - 2; p++) {
				// This - 2 is for the border
				if (p < 0) {
					continue;
				}
				if (BoundsToScreen (Bounds).X + p >= Driver.Cols) {
					break;
				}
				if (item == null) {
					Driver.AddRune (Glyphs.HLine);
				} else if (i == 0 && p == 0 && _host.UseSubMenusSingleFrame && item.Parent.Parent != null) {
					Driver.AddRune (Glyphs.LeftArrow);
				}
				// This `- 3` is left border + right border + one row in from right
				else if (p == Frame.Width - 3 && _barItems.SubMenu (_barItems.Children [i]) != null) {
					Driver.AddRune (Glyphs.RightArrow);
				} else {
					Driver.AddRune ((Rune)' ');
				}
			}

			if (item == null) {
				if (BorderStyle != LineStyle.None && SuperView?.Frame.Right - Frame.X > Frame.Width) {
					Move (Frame.Width - 2, i);
					Driver.AddRune (Glyphs.RightTee);
				}
				continue;
			}

			string textToDraw = null;
			var nullCheckedChar = Glyphs.NullChecked;
			var checkChar = Glyphs.Selected;
			var uncheckedChar = Glyphs.UnSelected;

			if (item.CheckType.HasFlag (MenuItemCheckStyle.Checked)) {
				checkChar = Glyphs.Checked;
				uncheckedChar = Glyphs.UnChecked;
			}

			// Support Checked even though CheckType wasn't set
			if (item.CheckType == MenuItemCheckStyle.Checked && item.Checked == null) {
				textToDraw = $"{nullCheckedChar} {item.Title}";
			} else if (item.Checked == true) {
				textToDraw = $"{checkChar} {item.Title}";
			} else if (item.CheckType.HasFlag (MenuItemCheckStyle.Checked) || item.CheckType.HasFlag (MenuItemCheckStyle.Radio)) {
				textToDraw = $"{uncheckedChar} {item.Title}";
			} else {
				textToDraw = item.Title;
			}

			BoundsToScreen (0, i, out int vtsCol, out int vtsRow, false);
			if (vtsCol < Driver.Cols) {
				Driver.Move (vtsCol + 1, vtsRow);
				if (!item.IsEnabled ()) {
					DrawHotString (textToDraw, ColorScheme.Disabled, ColorScheme.Disabled);
				} else if (i == 0 && _host.UseSubMenusSingleFrame && item.Parent.Parent != null) {
					var tf = new TextFormatter () {
						Alignment = TextAlignment.Centered,
						HotKeySpecifier = MenuBar.HotKeySpecifier,
						Text = textToDraw
					};
					// The -3 is left/right border + one space (not sure what for)
					tf.Draw (BoundsToScreen (new Rect (1, i, Frame.Width - 3, 1)),
						i == _currentChild ? ColorScheme.Focus : GetNormalColor (),
						i == _currentChild ? ColorScheme.HotFocus : ColorScheme.HotNormal,
						SuperView == null ? default : SuperView.BoundsToScreen (SuperView.Bounds));
				} else {
					DrawHotString (textToDraw,
						i == _currentChild ? ColorScheme.HotFocus : ColorScheme.HotNormal,
						i == _currentChild ? ColorScheme.Focus : GetNormalColor ());
				}

				// The help string
				int l = item.ShortcutTag.GetColumns () == 0 ? item.Help.GetColumns () : item.Help.GetColumns () + item.ShortcutTag.GetColumns () + 2;
				int col = Frame.Width - l - 3;
				BoundsToScreen (col, i, out vtsCol, out vtsRow, false);
				if (vtsCol < Driver.Cols) {
					Driver.Move (vtsCol, vtsRow);
					Driver.AddStr (item.Help);

					// The shortcut tag string
					if (!string.IsNullOrEmpty (item.ShortcutTag)) {
						Driver.Move (vtsCol + l - item.ShortcutTag.GetColumns (), vtsRow);
						Driver.AddStr (item.ShortcutTag);
					}
				}
			}
		}
		Driver.Clip = savedClip;

		PositionCursor ();
	}

	void Current_DrawContentComplete (object sender, DrawEventArgs e)
	{
		if (Visible) {
			OnDrawContent (Bounds);
		}
	}

	public override void PositionCursor ()
	{
		if (_host == null || _host.IsMenuOpen) {
			if (_barItems.IsTopLevel) {
				_host.PositionCursor ();
			} else {
				Move (2, 1 + _currentChild);
			}
		} else {
			_host.PositionCursor ();
		}
	}

	public void Run (Action action)
	{
		if (action == null || _host == null) {
			return;
		}

		Application.UngrabMouse ();
		_host.CloseAllMenus ();
		Application.Refresh ();

		_host.Run (action);
	}

	public override bool OnLeave (View view)
	{
		return _host.OnLeave (view);
	}

	void RunSelected ()
	{
		if (_barItems.IsTopLevel) {
			Run (_barItems.Action);
		} else if (_currentChild > -1 && _barItems.Children [_currentChild].Action != null) {
			Run (_barItems.Children [_currentChild].Action);
		} else if (_currentChild == 0 && _host.UseSubMenusSingleFrame && _barItems.Children [_currentChild].Parent.Parent != null) {
			_host.PreviousMenu (_barItems.Children [_currentChild].Parent.IsFromSubMenu, true);
		} else if (_currentChild > -1 && _barItems.SubMenu (_barItems.Children [_currentChild]) != null) {
			CheckSubMenu ();
		}
	}

	void CloseAllMenus ()
	{
		Application.UngrabMouse ();
		_host.CloseAllMenus ();
	}

	bool MoveDown ()
	{
		if (_barItems.IsTopLevel) {
			return true;
		}
		bool disabled;
		do {
			_currentChild++;
			if (_currentChild >= _barItems.Children.Length) {
				_currentChild = 0;
			}
			if (this != _host.openCurrentMenu && _barItems.Children [_currentChild]?.IsFromSubMenu == true && _host._selectedSub > -1) {
				_host.PreviousMenu (true);
				_host.SelectEnabledItem (_barItems.Children, _currentChild, out _currentChild);
				_host.openCurrentMenu = this;
			}
			var item = _barItems.Children [_currentChild];
			if (item?.IsEnabled () != true) {
				disabled = true;
			} else {
				disabled = false;
			}
			if (!_host.UseSubMenusSingleFrame && _host.UseKeysUpDownAsKeysLeftRight && _barItems.SubMenu (_barItems.Children [_currentChild]) != null &&
			!disabled && _host.IsMenuOpen) {
				if (!CheckSubMenu ()) {
					return false;
				}
				break;
			}
			if (!_host.IsMenuOpen) {
				_host.OpenMenu (_host._selected);
			}
		} while (_barItems.Children [_currentChild] == null || disabled);
		SetNeedsDisplay ();
		SetParentSetNeedsDisplay ();
		if (!_host.UseSubMenusSingleFrame) {
			_host.OnMenuOpened ();
		}
		return true;
	}

	bool MoveUp ()
	{
		if (_barItems.IsTopLevel || _currentChild == -1) {
			return true;
		}
		bool disabled;
		do {
			_currentChild--;
			if (_host.UseKeysUpDownAsKeysLeftRight && !_host.UseSubMenusSingleFrame) {
				if ((_currentChild == -1 || this != _host.openCurrentMenu) && _barItems.Children [_currentChild + 1].IsFromSubMenu && _host._selectedSub > -1) {
					_currentChild++;
					_host.PreviousMenu (true);
					if (_currentChild > 0) {
						_currentChild--;
						_host.openCurrentMenu = this;
					}
					break;
				}
			}
			if (_currentChild < 0) {
				_currentChild = _barItems.Children.Length - 1;
			}
			if (!_host.SelectEnabledItem (_barItems.Children, _currentChild, out _currentChild, false)) {
				_currentChild = 0;
				if (!_host.SelectEnabledItem (_barItems.Children, _currentChild, out _currentChild) && !_host.CloseMenu (false)) {
					return false;
				}
				break;
			}
			var item = _barItems.Children [_currentChild];
			if (item?.IsEnabled () != true) {
				disabled = true;
			} else {
				disabled = false;
			}
			if (!_host.UseSubMenusSingleFrame && _host.UseKeysUpDownAsKeysLeftRight &&
			_barItems.SubMenu (_barItems.Children [_currentChild]) != null &&
			!disabled && _host.IsMenuOpen) {
				if (!CheckSubMenu ()) {
					return false;
				}
				break;
			}
		} while (_barItems.Children [_currentChild] == null || disabled);
		SetNeedsDisplay ();
		SetParentSetNeedsDisplay ();
		if (!_host.UseSubMenusSingleFrame) {
			_host.OnMenuOpened ();
		}
		return true;
	}

	void SetParentSetNeedsDisplay ()
	{
		if (_host._openSubMenu != null) {
			foreach (var menu in _host._openSubMenu) {
				menu.SetNeedsDisplay ();
			}
		}

		_host?._openMenu?.SetNeedsDisplay ();
		_host.SetNeedsDisplay ();
	}

	public override bool MouseEvent (MouseEvent me)
	{
		if (!_host._handled && !_host.HandleGrabView (me, this)) {
			return false;
		}
		_host._handled = false;
		bool disabled;
		int meY = me.Y - (Border == null ? 0 : Border.Thickness.Top);
		if (me.Flags == MouseFlags.Button1Clicked) {
			disabled = false;
			if (meY < 0) {
				return true;
			}
			if (meY >= _barItems.Children.Length) {
				return true;
			}
			var item = _barItems.Children [meY];
			if (item == null || !item.IsEnabled ()) {
				disabled = true;
			}
			if (disabled) {
				return true;
			}
			_currentChild = meY;
			if (item != null && !disabled) {
				RunSelected ();
			}
			return true;
		} else if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked ||
			me.Flags == MouseFlags.Button1TripleClicked || me.Flags == MouseFlags.ReportMousePosition ||
			me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {

			disabled = false;
			if (meY < 0 || meY >= _barItems.Children.Length) {
				return true;
			}
			var item = _barItems.Children [meY];
			if (item == null) {
				return true;
			}
			if (item == null || !item.IsEnabled ()) {
				disabled = true;
			}
			if (item != null && !disabled) {
				_currentChild = meY;
			}
			if (_host.UseSubMenusSingleFrame || !CheckSubMenu ()) {
				SetNeedsDisplay ();
				SetParentSetNeedsDisplay ();
				return true;
			}
			_host.OnMenuOpened ();
			return true;
		}
		return false;
	}

	internal bool CheckSubMenu ()
	{
		if (_currentChild == -1 || _barItems.Children [_currentChild] == null) {
			return true;
		}
		var subMenu = _barItems.SubMenu (_barItems.Children [_currentChild]);
		if (subMenu != null) {
			int pos = -1;
			if (_host._openSubMenu != null) {
				pos = _host._openSubMenu.FindIndex (o => o?._barItems == subMenu);
			}
			if (pos == -1 && this != _host.openCurrentMenu && subMenu.Children != _host.openCurrentMenu._barItems.Children
			&& !_host.CloseMenu (false, true)) {
				return false;
			}
			_host.Activate (_host._selected, pos, subMenu);
		} else if (_host._openSubMenu?.Count == 0 || _host._openSubMenu?.Last ()._barItems.IsSubMenuOf (_barItems.Children [_currentChild]) == false) {
			return _host.CloseMenu (false, true);
		} else {
			SetNeedsDisplay ();
			SetParentSetNeedsDisplay ();
		}
		return true;
	}

	int GetSubMenuIndex (MenuBarItem subMenu)
	{
		int pos = -1;
		if (this != null && Subviews.Count > 0) {
			Menu v = null;
			foreach (var menu in Subviews) {
				if (((Menu)menu)._barItems == subMenu) {
					v = (Menu)menu;
				}
			}
			if (v != null) {
				pos = Subviews.IndexOf (v);
			}
		}

		return pos;
	}

	///<inheritdoc/>
	public override bool OnEnter (View view)
	{
		Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

		return base.OnEnter (view);
	}

	protected override void Dispose (bool disposing)
	{
		if (Application.Current != null) {
			Application.Current.DrawContentComplete -= Current_DrawContentComplete;
			Application.Current.SizeChanging -= Current_TerminalResized;
		}
		Application.MouseEvent -= Application_RootMouseEvent;
		base.Dispose (disposing);
	}
}
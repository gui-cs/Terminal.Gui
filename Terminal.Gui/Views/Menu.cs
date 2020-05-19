//
// Menu.cs: application menus and submenus
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// TODO:
//   Add accelerator support, but should also support chords (ShortCut in MenuItem)
//   Allow menus inside menus

using System;
using NStack;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Terminal.Gui {

	/// <summary>
	/// A menu item has a title, an associated help text, and an action to execute on activation.
	/// </summary>
	public class MenuItem {

		/// <summary>
		/// constructor
		/// </summary>
		public MenuItem ()
		{
			Title = "";
			Help = "";
		}

		/// <summary>
		/// Initializes a new <see cref="T:Terminal.Gui.MenuItem"/>.
		/// </summary>
		/// <param name="title">Title for the menu item.</param>
		/// <param name="help">Help text to display.</param>
		/// <param name="action">Action to invoke when the menu item is activated.</param>
		/// <param name="canExecute">Function to determine if the action can currently be executred.</param>
		public MenuItem (ustring title, string help, Action action, Func<bool> canExecute = null)
		{
			Title = title ?? "";
			Help = help ?? "";
			Action = action;
			CanExecute = canExecute;
			bool nextIsHot = false;
			foreach (var x in Title) {
				if (x == '_')
					nextIsHot = true;
				else {
					if (nextIsHot) {
						HotKey = Char.ToUpper ((char)x);
						break;
					}
					nextIsHot = false;
				}
			}
		}

		/// <summary>
		/// Initializes a new <see cref="T:Terminal.Gui.MenuItem"/>.
		/// </summary>
		/// <param name="title">Title for the menu item.</param>
		/// <param name="subMenu">The menu sub-menu.</param>
		public MenuItem (ustring title, MenuBarItem subMenu) : this (title, "", null)
		{
			SubMenu = subMenu;
			IsFromSubMenu = true;
		}

		//
		//

		/// <summary>
		/// The hotkey is used when the menu is active, the shortcut can be triggered when the menu is not active.
		/// For example HotKey would be "N" when the File Menu is open (assuming there is a "_New" entry
		/// if the ShortCut is set to "Control-N", this would be a global hotkey that would trigger as well
		/// </summary>
		public Rune HotKey;

		/// <summary>
		/// This is the global setting that can be used as a global shortcut to invoke the action on the menu.
		/// </summary>
		public Key ShortCut;

		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		/// <value>The title.</value>
		public ustring Title { get; set; }

		/// <summary>
		/// Gets or sets the help text for the menu item.
		/// </summary>
		/// <value>The help text.</value>
		public ustring Help { get; set; }

		/// <summary>
		/// Gets or sets the action to be invoked when the menu is triggered
		/// </summary>
		/// <value>Method to invoke.</value>
		public Action Action { get; set; }

		/// <summary>
		/// Gets or sets the action to be invoked if the menu can be triggered
		/// </summary>
		/// <value>Function to determine if action is ready to be executed.</value>
		public Func<bool> CanExecute { get; set; }

		/// <summary>
		/// Shortcut to check if the menu item is enabled
		/// </summary>
		public bool IsEnabled ()
		{
			return CanExecute == null ? true : CanExecute ();
		}

		internal int Width => Title.Length + Help.Length + 1 + 2;

		/// <summary>
		/// Gets or sets the parent for this MenuBarItem
		/// </summary>
		/// <value>The parent.</value>
		internal MenuBarItem SubMenu { get; set; }
		internal bool IsFromSubMenu { get; set; }

		/// <summary>
		/// Merely a debugging aid to see the interaction with main
		/// </summary>
		public MenuItem GetMenuItem ()
		{
			return this;
		}

		/// <summary>
		/// Merely a debugging aid to see the interaction with main
		/// </summary>
		public bool GetMenuBarItem ()
		{
			return IsFromSubMenu;
		}
	}

	/// <summary>
	/// A menu bar item contains other menu items.
	/// </summary>
	public class MenuBarItem : MenuItem {
		/// <summary>
		/// Initializes a new <see cref="T:Terminal.Gui.MenuBarItem"/> as a <see cref="T:Terminal.Gui.MenuItem"/>.
		/// </summary>
		/// <param name="title">Title for the menu item.</param>
		/// <param name="help">Help text to display.</param>
		/// <param name="action">Action to invoke when the menu item is activated.</param>
		/// <param name="canExecute">Function to determine if the action can currently be executred.</param>
		public MenuBarItem (ustring title, string help, Action action, Func<bool> canExecute = null) : base (title, help, action, canExecute)
		{
			SetTitle (title ?? "");
			Children = null;
		}

		/// <summary>
		/// Initializes a new <see cref="T:Terminal.Gui.MenuBarItem"/>.
		/// </summary>
		/// <param name="title">Title for the menu item.</param>
		/// <param name="children">The items in the current menu.</param>
		public MenuBarItem (ustring title, MenuItem [] children)
		{
			SetTitle (title ?? "");
			Children = children;
		}

		/// <summary>
		/// Initializes a new <see cref="T:Terminal.Gui.MenuBarItem"/>.
		/// </summary>
		/// <param name="children">The items in the current menu.</param>
		public MenuBarItem (MenuItem [] children) : this (new string (' ', GetMaxTitleLength (children)), children)
		{
		}

		static int GetMaxTitleLength (MenuItem [] children)
		{
			int maxLength = 0;
			foreach (var item in children) {
				int len = GetMenuBarItemLength (item.Title);
				if (len > maxLength)
					maxLength = len;
				item.IsFromSubMenu = true;
			}

			return maxLength;
		}

		void SetTitle (ustring title)
		{
			if (title == null)
				title = "";
			Title = title;
			TitleLength = GetMenuBarItemLength (Title);
		}

		static int GetMenuBarItemLength (ustring title)
		{
			int len = 0;
			foreach (var ch in title) {
				if (ch == '_')
					continue;
				len++;
			}

			return len;
		}

		///// <summary>
		///// Gets or sets the title to display.
		///// </summary>
		///// <value>The title.</value>
		//public ustring Title { get; set; }

		/// <summary>
		/// Gets or sets the children for this MenuBarItem
		/// </summary>
		/// <value>The children.</value>
		public MenuItem [] Children { get; set; }
		internal int TitleLength { get; private set; }

		internal bool IsTopLevel { get => (Children == null || Children.Length == 0); }

	}

	class Menu : View {
		internal MenuBarItem barItems;
		MenuBar host;
		internal int current;
		internal View previousSubFocused;

		static Rect MakeFrame (int x, int y, MenuItem [] items)
		{
			if (items == null || items.Length == 0) {
				return new Rect ();
			}
			int maxW = items.Max (z => z?.Width) ?? 0;

			return new Rect (x, y, maxW + 2, items.Length + 2);
		}

		public Menu (MenuBar host, int x, int y, MenuBarItem barItems) : base (MakeFrame (x, y, barItems.Children))
		{
			this.barItems = barItems;
			this.host = host;
			if (barItems.IsTopLevel) {
				// This is a standalone MenuItem on a MenuBar
				ColorScheme = Colors.Menu;
				CanFocus = true;
			} else {

				current = -1;
				for (int i = 0; i < barItems.Children.Length; i++) {
					if (barItems.Children [i] != null) {
						current = i;
						break;
					}
				}
				ColorScheme = Colors.Menu;
				CanFocus = true;
				WantMousePositionReports = host.WantMousePositionReports;
			}

		}

		internal Attribute DetermineColorSchemeFor (MenuItem item, int index)
		{
			if (item != null) {
				if (index == current) return ColorScheme.Focus;
				if (!item.IsEnabled ()) return ColorScheme.Disabled;
			}
			return ColorScheme.Normal;
		}

		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (ColorScheme.Normal);
			DrawFrame (region, padding: 0, fill: true);

			for (int i = 0; i < barItems.Children.Length; i++) {
				var item = barItems.Children [i];
				Driver.SetAttribute (item == null ? ColorScheme.Normal : i == current ? ColorScheme.Focus : ColorScheme.Normal);
				if (item == null) {
					Move (0, i + 1);
					Driver.AddRune (Driver.LeftTee);
				} else
					Move (1, i + 1);

				Driver.SetAttribute (DetermineColorSchemeFor (item, i));
				for (int p = 0; p < Frame.Width - 2; p++)
					if (item == null)
						Driver.AddRune (Driver.HLine);
					else if (p == Frame.Width - 3 && barItems.Children [i].SubMenu != null)
						Driver.AddRune ('>');
					else
						Driver.AddRune (' ');

				if (item == null) {
					Move (Frame.Right - 1, i + 1);
					Driver.AddRune (Driver.RightTee);
					continue;
				}

				Move (2, i + 1);
				if (!item.IsEnabled ())
					DrawHotString (item.Title, ColorScheme.Disabled, ColorScheme.Disabled);
				else
					DrawHotString (item.Title,
					       i == current ? ColorScheme.HotFocus : ColorScheme.HotNormal,
					       i == current ? ColorScheme.Focus : ColorScheme.Normal);

				// The help string
				var l = item.Help.Length;
				Move (Frame.Width - l - 2, 1 + i);
				Driver.AddStr (item.Help);
			}
			PositionCursor ();
		}

		public override void PositionCursor ()
		{
			if (host == null || !host.isMenuClosed)
				if (barItems.IsTopLevel) {
					host.PositionCursor ();
				} else
					Move (2, 1 + current);
			else
				host.PositionCursor ();
		}

		public void Run (Action action)
		{
			if (action == null)
				return;

			Application.UngrabMouse ();
			host.CloseAllMenus ();
			Application.Refresh ();

			Application.MainLoop.AddIdle (() => {
				action ();
				return false;
			});
		}

		public override bool KeyDown (KeyEvent keyEvent)
		{
			if (keyEvent.IsAlt) {
				host.CloseAllMenus ();
				return true;
			}

			return false;
		}

		public override bool ProcessHotKey (KeyEvent keyEvent)
		{
			// To ncurses simulate a AltMask key pressing Alt+Space because
			// it can�t detect an alone special key down was pressed.
			if (keyEvent.IsAlt && keyEvent.Key == Key.AltMask) {
				KeyDown (keyEvent);
				return true;
			}

			return false;
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			bool disabled;
			switch (kb.Key) {
			case Key.CursorUp:
				if (barItems.IsTopLevel || current == -1)
					break;
				do {
					disabled = false;
					current--;
					if (host.UseKeysUpDownAsKeysLeftRight) {
						if (current == -1 && barItems.Children [current + 1].IsFromSubMenu && host.selectedSub > -1) {
							current++;
							host.PreviousMenu (true);
							break;
						}
					}
					if (current < 0)
						current = barItems.Children.Length - 1;
					var item = barItems.Children [current];
					if (item == null || !item.IsEnabled ()) disabled = true;
				} while (barItems.Children [current] == null || disabled);
				SetNeedsDisplay ();
				break;
			case Key.CursorDown:
				if (barItems.IsTopLevel) {
					break;
				}

				do {
					current++;
					disabled = false;
					if (current == barItems.Children.Length)
						current = 0;
					var item = barItems.Children [current];
					if (item == null || !item.IsEnabled ()) disabled = true;
					if (host.UseKeysUpDownAsKeysLeftRight && barItems.Children [current]?.SubMenu != null &&
						!disabled && !host.isMenuClosed) {
						CheckSubMenu ();
						break;
					}
					if (host.isMenuClosed)
						host.OpenMenu (host.selected);
				} while (barItems.Children [current] == null || disabled);
				SetNeedsDisplay ();
				break;
			case Key.CursorLeft:
				host.PreviousMenu (true);
				break;
			case Key.CursorRight:
				host.NextMenu (barItems.IsTopLevel || barItems.Children [current].IsFromSubMenu ? true : false);
				break;
			case Key.Esc:
				Application.UngrabMouse ();
				host.CloseAllMenus ();
				break;
			case Key.Enter:
				if (barItems.IsTopLevel) {
					Run (barItems.Action);
				} else {
					CheckSubMenu ();
					Run (barItems.Children [current].Action);
				}
				break;
			default:
				// TODO: rune-ify
				if (barItems.Children != null && Char.IsLetterOrDigit ((char)kb.KeyValue)) {
					var x = Char.ToUpper ((char)kb.KeyValue);
					foreach (var item in barItems.Children) {
						if (item == null) continue;
						if (item.IsEnabled () && item.HotKey == x) {
							host.CloseMenu ();
							Run (item.Action);
							return true;
						}
					}
				}
				break;
			}
			return true;
		}

		public override bool MouseEvent (MouseEvent me)
		{
			if (!host.handled && !host.HandleGrabView (me, this)) {
				return false;
			}
			host.handled = false;
			bool disabled;
			if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked) {
				disabled = false;
				if (me.Y < 1)
					return true;
				var meY = me.Y - 1;
				if (meY >= barItems.Children.Length)
					return true;
				var item = barItems.Children [meY];
				if (item == null || !item.IsEnabled ()) disabled = true;
				if (item != null && !disabled)
					Run (barItems.Children [meY].Action);
				return true;
			} else if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked ||
				me.Flags == MouseFlags.ReportMousePosition ||
				me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)) {
				disabled = false;
				if (me.Y < 1)
					return true;
				if (me.Y - 1 >= barItems.Children.Length)
					return true;
				var item = barItems.Children [me.Y - 1];
				if (item == null || !item.IsEnabled ()) disabled = true;
				if (item != null && !disabled)
					current = me.Y - 1;
				HasFocus = true;
				SetNeedsDisplay ();
				CheckSubMenu ();
				return true;
			}
			return false;
		}

		internal void CheckSubMenu ()
		{
			if (barItems.Children [current] == null)
				return;
			var subMenu = barItems.Children [current].SubMenu;
			if (subMenu != null) {
				int pos = -1;
				if (host.openSubMenu != null)
					pos = host.openSubMenu.FindIndex (o => o?.barItems == subMenu);
				host.Activate (host.selected, pos, subMenu);
			} else if (host.openSubMenu != null && !barItems.Children [current].IsFromSubMenu)
				host.CloseMenu (false, true);
		}

		int GetSubMenuIndex (MenuBarItem subMenu)
		{
			int pos = -1;
			if (this != null && Subviews.Count > 0) {
				Menu v = null;
				foreach (var menu in Subviews) {
					if (((Menu)menu).barItems == subMenu)
						v = (Menu)menu;
				}
				if (v != null)
					pos = Subviews.IndexOf (v);
			}

			return pos;
		}
	}



	/// <summary>
	/// A menu bar for your application.
	/// </summary>
	public class MenuBar : View {
		/// <summary>
		/// The menus that were defined when the menubar was created.   This can be updated if the menu is not currently visible.
		/// </summary>
		/// <value>The menu array.</value>
		public MenuBarItem [] Menus { get; set; }
		internal int selected;
		internal int selectedSub;

		Action action;

		/// <summary>
		/// Used for change the navigation key style.
		/// </summary>
		public bool UseKeysUpDownAsKeysLeftRight { get; set; } = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.MenuBar"/> class with the specified set of toplevel menu items.
		/// </summary>
		/// <param name="menus">Individual menu items, if one of those contains a null, then a separator is drawn.</param>
		public MenuBar (MenuBarItem [] menus) : base ()
		{
			X = 0;
			Y = 0;
			Width = Dim.Fill ();
			Height = 1;
			Menus = menus;
			//CanFocus = true;
			selected = -1;
			selectedSub = -1;
			ColorScheme = Colors.Menu;
			WantMousePositionReports = true;
			isMenuClosed = true;
		}

		bool openedByAltKey;

		///<inheritdoc cref="KeyDown"/>
		public override bool KeyDown (KeyEvent keyEvent)
		{
			if (keyEvent.IsAlt) {
				openedByAltKey = true;
				SetNeedsDisplay ();
				openedByHotKey = false;
			}
			return false;
		}

		/// <summary>
		/// Track Alt key-up events. On Windows, when a user releases Alt (without another key), the menu gets focus but doesn't open.
		/// We mimic that behavior here.
		/// </summary>
		/// <param name="keyEvent"></param>
		/// <returns></returns>
		public override bool KeyUp (KeyEvent keyEvent)
		{
			if (keyEvent.IsAlt) {
				// User pressed Alt - this may be a precursor to a menu accelerator (e.g. Alt-F)
				if (!keyEvent.IsCtrl && openedByAltKey && isMenuClosed && openMenu == null && ((uint)keyEvent.Key & (uint)Key.CharMask) == 0) {
					// There's no open menu, the first menu item should be highlight.
					// The right way to do this is to SetFocus(MenuBar), but for some reason
					// that faults.

					//Activate (0);
					//StartMenu ();
					isMenuClosed = false;
					selected = 0;
					CanFocus = true;
					lastFocused = SuperView.MostFocused;
					SuperView.SetFocus (this);
					SetNeedsDisplay ();
					Application.GrabMouse (this);
				} else if (!openedByHotKey) {
					// There's an open menu. If this Alt key-up is a pre-cursor to an accelerator
					// we don't want to close the menu because it'll flash.
					// How to deal with that?

					if (openMenu != null)
						CloseAllMenus ();
					openedByAltKey = false;
					isMenuClosed = true;
					selected = -1;
					CanFocus = false;
					if (lastFocused != null)
						SuperView?.SetFocus (lastFocused);
					SetNeedsDisplay ();
					Application.UngrabMouse ();
				}

				return true;
			}
			return false;
		}

		///<inheritdoc cref="Redraw"/>
		public override void Redraw (Rect region)
		{
			Move (0, 0);
			Driver.SetAttribute (Colors.Menu.Normal);
			for (int i = 0; i < Frame.Width; i++)
				Driver.AddRune (' ');

			Move (1, 0);
			int pos = 1;

			for (int i = 0; i < Menus.Length; i++) {
				var menu = Menus [i];
				Move (pos, 0);
				Attribute hotColor, normalColor;
				if (i == selected) {
					hotColor = i == selected ? ColorScheme.HotFocus : ColorScheme.HotNormal;
					normalColor = i == selected ? ColorScheme.Focus : ColorScheme.Normal;
				} else if (openedByAltKey) {
					hotColor = ColorScheme.HotNormal;
					normalColor = ColorScheme.Normal;
				} else {
					hotColor = ColorScheme.Normal;
					normalColor = ColorScheme.Normal;
				}
				DrawHotString ($" {menu.Title}  ", hotColor, normalColor);
				pos += 1 + menu.TitleLength + 2;
			}
			PositionCursor ();
		}

		///<inheritdoc cref="PositionCursor"/>
		public override void PositionCursor ()
		{
			int pos = 0;
			for (int i = 0; i < Menus.Length; i++) {
				if (i == selected) {
					pos++;
					if (!isMenuClosed)
						Move (pos + 1, 0);
					else
						Move (pos + 1, 0);
					return;
				} else {
					if (!isMenuClosed)
						pos += 1 + Menus [i].TitleLength + 2;
					else
						pos += 2 + Menus [i].TitleLength + 1;
				}
			}
			//Move (0, 0);
		}

		void Selected (MenuItem item)
		{
			// TODO: Running = false;
			action = item.Action;
		}

		/// <summary>
		/// Raised as a menu is opened.
		/// </summary>
		public event EventHandler OnOpenMenu;

		/// <summary>
		/// Raised when a menu is closing.
		/// </summary>
		public event EventHandler OnCloseMenu;

		internal Menu openMenu;
		Menu openCurrentMenu;
		internal List<Menu> openSubMenu;
		View previousFocused;
		internal bool isMenuOpening;
		internal bool isMenuClosing;
		internal bool isMenuClosed;

		/// <summary>
		/// True of the menu is open; otherwise false.
		/// </summary>
		public bool MenuOpen { get; set; }

		View lastFocused;

		/// <summary>
		/// Get the lasted focused view before open the menu.
		/// </summary>
		public View LastFocused { get; private set; }

		internal void OpenMenu (int index, int sIndex = -1, MenuBarItem subMenu = null)
		{
			isMenuOpening = true;
			OnOpenMenu?.Invoke (this, null);
			int pos = 0;
			switch (subMenu) {
			case null:
				lastFocused = lastFocused ?? SuperView.MostFocused;
				if (openSubMenu != null)
					CloseMenu (false, true);
				if (openMenu != null)
					SuperView.Remove (openMenu);

				for (int i = 0; i < index; i++)
					pos += Menus [i].Title.Length + 2;
				openMenu = new Menu (this, pos, 1, Menus [index]);
				openCurrentMenu = openMenu;
				openCurrentMenu.previousSubFocused = openMenu;

				SuperView.Add (openMenu);
				SuperView.SetFocus (openMenu);
				break;
			default:
				if (openSubMenu == null)
					openSubMenu = new List<Menu> ();
				if (sIndex > -1) {
					RemoveSubMenu (sIndex);
				} else {
					var last = openSubMenu.Count > 0 ? openSubMenu.Last () : openMenu;
					openCurrentMenu = new Menu (this, last.Frame.Left + last.Frame.Width, last.Frame.Top + 1 + last.current, subMenu);
					openCurrentMenu.previousSubFocused = last.previousSubFocused;
					openSubMenu.Add (openCurrentMenu);
					SuperView.Add (openCurrentMenu);
				}
				selectedSub = openSubMenu.Count - 1;
				SuperView?.SetFocus (openCurrentMenu);
				break;
			}
			isMenuOpening = false;
			isMenuClosed = false;
			MenuOpen = true;
		}

		// Starts the menu from a hotkey
		void StartMenu ()
		{
			if (openMenu != null)
				return;
			selected = 0;
			SetNeedsDisplay ();

			previousFocused = SuperView.Focused;
			OpenMenu (selected);
			Application.GrabMouse (this);
		}

		// Activates the menu, handles either first focus, or activating an entry when it was already active
		// For mouse events.
		internal void Activate (int idx, int sIdx = -1, MenuBarItem subMenu = null)
		{
			selected = idx;
			selectedSub = sIdx;
			if (openMenu == null)
				previousFocused = SuperView.Focused;

			OpenMenu (idx, sIdx, subMenu);
			SetNeedsDisplay ();
		}

		internal void CloseMenu (bool reopen = false, bool isSubMenu = false)
		{
			isMenuClosing = true;
			OnCloseMenu?.Invoke (this, null);
			switch (isSubMenu) {
			case false:
				if (openMenu != null)
					SuperView.Remove (openMenu);
				SetNeedsDisplay ();
				if (previousFocused != null && openMenu != null && previousFocused.ToString () != openCurrentMenu.ToString ())
					previousFocused?.SuperView?.SetFocus (previousFocused);
				openMenu = null;
				if (lastFocused is Menu) {
					lastFocused = null;
				}
				LastFocused = lastFocused;
				lastFocused = null;
				if (LastFocused != null) {
					if (!reopen)
						selected = -1;
					LastFocused.SuperView?.SetFocus (LastFocused);
				} else {
					SuperView.SetFocus (this);
					isMenuClosed = true;
					PositionCursor ();
				}
				isMenuClosed = true;
				break;

			case true:
				selectedSub = -1;
				SetNeedsDisplay ();
				RemoveAllOpensSubMenus ();
				openCurrentMenu.previousSubFocused?.SuperView?.SetFocus (openCurrentMenu.previousSubFocused);
				openSubMenu = null;
				break;
			}
			isMenuClosing = false;
			MenuOpen = false;
		}

		void RemoveSubMenu (int index)
		{
			if (openSubMenu == null)
				return;
			for (int i = openSubMenu.Count - 1; i > index; i--) {
				isMenuClosing = true;
				if (openSubMenu.Count - 1 > 0)
					SuperView.SetFocus (openSubMenu [i - 1]);
				else
					SuperView.SetFocus (openMenu);
				if (openSubMenu != null) {
					SuperView.Remove (openSubMenu [i]);
					openSubMenu.Remove (openSubMenu [i]);
				}
				RemoveSubMenu (i);
			}
			if (openSubMenu.Count > 0)
				openCurrentMenu = openSubMenu.Last ();

			//if (openMenu.Subviews.Count == 0)
			//	return;
			//if (index == 0) {
			//	//SuperView.SetFocus (previousSubFocused);
			//	FocusPrev ();
			//	return;
			//}

			//for (int i = openMenu.Subviews.Count - 1; i > index; i--) {
			//	isMenuClosing = true;
			//	if (openMenu.Subviews.Count - 1 > 0)
			//		SuperView.SetFocus (openMenu.Subviews [i - 1]);
			//	else
			//		SuperView.SetFocus (openMenu);
			//	if (openMenu != null) {
			//		Remove (openMenu.Subviews [i]);
			//		openMenu.Remove (openMenu.Subviews [i]);
			//	}
			//	RemoveSubMenu (i);
			//}
			isMenuClosing = false;
		}

		internal void RemoveAllOpensSubMenus ()
		{
			if (openSubMenu != null) {
				foreach (var item in openSubMenu) {
					SuperView.Remove (item);
				}
			}
		}

		internal void CloseAllMenus ()
		{
			if (!isMenuOpening && !isMenuClosing) {
				if (openSubMenu != null)
					CloseMenu (false, true);
				CloseMenu ();
				if (LastFocused != null && LastFocused != this)
					selected = -1;
			}
			isMenuClosed = true;
			openedByHotKey = false;
			openedByAltKey = false;
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

		internal void PreviousMenu (bool isSubMenu = false)
		{
			switch (isSubMenu) {
			case false:
				if (selected <= 0)
					selected = Menus.Length - 1;
				else
					selected--;

				if (selected > -1)
					CloseMenu (true, false);
				OpenMenu (selected);
				break;
			case true:
				if (selectedSub > -1) {
					selectedSub--;
					RemoveSubMenu (selectedSub);
					SetNeedsDisplay ();
				} else
					PreviousMenu ();

				break;
			}
		}

		internal void NextMenu (bool isSubMenu = false)
		{
			switch (isSubMenu) {
			case false:
				if (selected == -1)
					selected = 0;
				else if (selected + 1 == Menus.Length)
					selected = 0;
				else
					selected++;

				if (selected > -1)
					CloseMenu (true);
				OpenMenu (selected);
				break;
			case true:
				if (UseKeysUpDownAsKeysLeftRight) {
					CloseMenu (false, true);
					NextMenu ();
				} else {
					if ((selectedSub == -1 || openSubMenu == null || openSubMenu?.Count == selectedSub) && openCurrentMenu.barItems.Children [openCurrentMenu.current].SubMenu == null) {
						if (openSubMenu != null)
							CloseMenu (false, true);
						NextMenu ();
					} else if (openCurrentMenu.barItems.Children [openCurrentMenu.current].SubMenu != null ||
						!openCurrentMenu.barItems.Children [openCurrentMenu.current].IsFromSubMenu)
						selectedSub++;
					else
						return;
					SetNeedsDisplay ();
					openCurrentMenu.CheckSubMenu ();
				}
				break;
			}
		}

		bool openedByHotKey;
		internal bool FindAndOpenMenuByHotkey (KeyEvent kb)
		{
			//int pos = 0;
			var c = ((uint)kb.Key & (uint)Key.CharMask);
			for (int i = 0; i < Menus.Length; i++) {
				// TODO: this code is duplicated, hotkey should be part of the MenuBarItem
				var mi = Menus [i];
				int p = mi.Title.IndexOf ('_');
				if (p != -1 && p + 1 < mi.Title.Length) {
					if (Char.ToUpperInvariant ((char)mi.Title [p + 1]) == c) {
						ProcessMenu (i, mi);
						return true;
					}
				}
			}
			return false;
		}

		private void ProcessMenu (int i, MenuBarItem mi)
		{
			if (mi.IsTopLevel) {
				var menu = new Menu (this, i, 0, mi);
				menu.Run (mi.Action);
			} else {
				openedByHotKey = true;
				Application.GrabMouse (this);
				selected = i;
				OpenMenu (i);
			}
		}

		///<inheritdoc cref="ProcessHotKey"/>
		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (kb.Key == Key.F9) {
				if (isMenuClosed)
					StartMenu ();
				else
					CloseAllMenus ();
				return true;
			}

			// To ncurses simulate a AltMask key pressing Alt+Space because
			// it can�t detect an alone special key down was pressed.
			if (kb.IsAlt && kb.Key == Key.AltMask && openMenu == null) {
				KeyDown (kb);
				KeyUp (kb);
				return true;
			} else if (kb.IsAlt) {
				if (FindAndOpenMenuByHotkey (kb)) return true;
			}
			//var kc = kb.KeyValue;

			return base.ProcessHotKey (kb);
		}

		///<inheritdoc cref="ProcessKey"/>
		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.CursorLeft:
				selected--;
				if (selected < 0)
					selected = Menus.Length - 1;
				break;
			case Key.CursorRight:
				selected = (selected + 1) % Menus.Length;
				break;

			case Key.Esc:
			case Key.ControlC:
				//TODO: Running = false;
				CloseMenu ();
				if (openedByAltKey) {
					openedByAltKey = false;
					LastFocused.SuperView?.SetFocus (LastFocused);
				}
				break;

			case Key.CursorDown:
			case Key.Enter:
				ProcessMenu (selected, Menus [selected]);
				break;

			default:
				var key = kb.KeyValue;
				if ((key >= 'a' && key <= 'z') || (key >= 'A' && key <= 'Z') || (key >= '0' && key <= '9')) {
					char c = Char.ToUpper ((char)key);

					if (selected == -1 || Menus [selected].IsTopLevel)
						return false;

					foreach (var mi in Menus [selected].Children) {
						if (mi == null)
							continue;
						int p = mi.Title.IndexOf ('_');
						if (p != -1 && p + 1 < mi.Title.Length) {
							if (mi.Title [p + 1] == c) {
								Selected (mi);
								return true;
							}
						}
					}
				}

				return false;
			}
			SetNeedsDisplay ();
			return true;
		}

		///<inheritdoc cref="MouseEvent"/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!handled && !HandleGrabView (me, this)) {
				return false;
			}
			handled = false;

			if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1Clicked || me.Flags == MouseFlags.Button1DoubleClicked ||
				(me.Flags == MouseFlags.ReportMousePosition && selected > -1) ||
				(me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && selected > -1)) {
				int pos = 1;
				int cx = me.X;
				for (int i = 0; i < Menus.Length; i++) {
					if (cx > pos && me.X < pos + 1 + Menus [i].TitleLength) {
						if (selected == i && (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked) &&
											!isMenuClosed) {
							Application.UngrabMouse ();
							if (Menus [i].IsTopLevel) {
								var menu = new Menu (this, i, 0, Menus [i]);
								menu.Run (Menus [i].Action);
							} else {
								CloseMenu ();
							}
						} else if ((me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked) &&
							isMenuClosed) {
							if (Menus [i].IsTopLevel) {
								var menu = new Menu (this, i, 0, Menus [i]);
								menu.Run (Menus [i].Action);
							} else {
								Activate (i);
							}
						} else if (selected != i && selected > -1 && me.Flags == MouseFlags.ReportMousePosition) {
							if (!isMenuClosed) {
								CloseMenu ();
								Activate (i);
							}
						} else {
							if (!isMenuClosed)
								Activate (i);
						}
						return true;
					}
					pos += 2 + Menus [i].TitleLength + 1;
				}
			}
			return false;
		}

		internal bool handled;

		internal bool HandleGrabView (MouseEvent me, View current)
		{
			if (Application.mouseGrabView != null) {
				if (me.View is MenuBar || me.View is Menu) {
					if (me.View != current) {
						Application.UngrabMouse ();
						Application.GrabMouse (me.View);
						me.View.MouseEvent (me);
					}
				} else if (!(me.View is MenuBar || me.View is Menu) && (me.Flags.HasFlag (MouseFlags.Button1Clicked) ||
					me.Flags == MouseFlags.Button1DoubleClicked || me.Flags == MouseFlags.Button1Pressed)) {
					Application.UngrabMouse ();
					CloseAllMenus ();
					handled = false;
					return false;
				} else {
					handled = false;
					return false;
				}
			} else if (isMenuClosed && (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked ||
				me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))) {
				Application.GrabMouse (current);
			} else {
				handled = false;
				return false;
			}
			//if (me.View != this && (me.Flags != MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked))
			//	return true;
			//else if (me.View != this && (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked)) {
			//	Application.UngrabMouse ();
			//	host.CloseAllMenus ();
			//	return true;
			//}


			//if (!(me.View is MenuBar) && !(me.View is Menu) && (me.Flags != MouseFlags.Button1Pressed ||
			// me.Flags != MouseFlags.Button1DoubleClicked))
			//	return false;

			//if (Application.mouseGrabView != null) {
			//	if (me.View is MenuBar || me.View is Menu) {
			//		me.X -= me.OfX;
			//		me.Y -= me.OfY;
			//		me.View.MouseEvent (me);
			//		return true;
			//	} else if (!(me.View is MenuBar || me.View is Menu) && (me.Flags == MouseFlags.Button1Pressed ||
			//		me.Flags == MouseFlags.Button1DoubleClicked)) {
			//		Application.UngrabMouse ();
			//		CloseAllMenus ();
			//	}
			//} else if (!isMenuClosed && selected == -1 && (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked)) {
			//	Application.GrabMouse (this);
			//	return true;
			//}

			//if (Application.mouseGrabView != null) {
			//	if (Application.mouseGrabView == me.View && me.View == current) {
			//		me.X -= me.OfX;
			//		me.Y -= me.OfY;
			//	} else if (me.View != current && me.View is MenuBar && me.View is Menu) {
			//		Application.UngrabMouse ();
			//		Application.GrabMouse (me.View);
			//	} else if (me.Flags == MouseFlags.Button1Pressed || me.Flags == MouseFlags.Button1DoubleClicked) {
			//		Application.UngrabMouse ();
			//		CloseMenu ();
			//	}
			//} else if ((!isMenuClosed && selected > -1)) {
			//	Application.GrabMouse (current);
			//}

			handled = true;

			return true;
		}
	}

}

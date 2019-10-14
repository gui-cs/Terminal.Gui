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

namespace Terminal.Gui {

	/// <summary>
	/// A menu item has a title, an associated help text, and an action to execute on activation.
	/// </summary>
	public class MenuItem {

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

		public MenuItem(ustring title, MenuBarItem subMenu) : this (title, "", null)
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
	public class MenuBarItem {
		public MenuBarItem (ustring title, MenuItem [] children)
		{
			SetTitle (title ?? "");
			Children = children;
		}

		public MenuBarItem (MenuItem[] children) : this (new string (' ', GetMaxTitleLength (children)), children)
		{
		}

		private static int GetMaxTitleLength (MenuItem[] children)
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
			TitleLength = GetMenuBarItemLength(Title);
		}

		static int GetMenuBarItemLength(ustring title)
		{
			int len = 0;
			foreach (var ch in title) {
				if (ch == '_')
					continue;
				len++;
			}

			return len;
		}

		/// <summary>
		/// Gets or sets the title to display.
		/// </summary>
		/// <value>The title.</value>
		public ustring Title { get; set; }

		/// <summary>
		/// Gets or sets the children for this MenuBarItem
		/// </summary>
		/// <value>The children.</value>
		public MenuItem [] Children { get; set; }
		internal int TitleLength { get; private set; }
	}

	class Menu : View {
		internal MenuBarItem barItems;
		MenuBar host;
		internal int current;

		static Rect MakeFrame (int x, int y, MenuItem [] items)
		{
			int maxW = 0;

			foreach (var item in items) {
				if (item == null) continue;
				var l = item.Width;
				maxW = Math.Max (l, maxW);
			}

			return new Rect (x, y, maxW + 2, items.Length + 2);
		}

		public Menu (MenuBar host, int x, int y, MenuBarItem barItems) : base (MakeFrame (x, y, barItems.Children))
		{
			this.barItems = barItems;
			this.host = host;
			current = -1;
			for (int i = 0; i < barItems.Children.Length; i++) {
				if (barItems.Children[i] != null) {
					current = i;
					break;
				}
			}
			ColorScheme = Colors.Menu;
			CanFocus = true;
			WantMousePositionReports = host.WantMousePositionReports;
			selectedSub = -1;
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
			if ((!HasFocus && openSubMenu == null && GetSubMenuIndex (previousSubFocused) == -1) || !HasFocus && GetSubMenuIndex(previousSubFocused) == -1 && !host.HasFocus && openSubMenu != null) {
				CloseSubMenu ();
				host.CloseMenu ();
				//  Force menu repainting after losing focus through mouse
				host.Redraw (Bounds);
				return;
			}

			Driver.SetAttribute (ColorScheme.Normal);
			DrawFrame (region, padding: 0, fill: true);

			for (int i = 0; i < barItems.Children.Length; i++) {
				var item = barItems.Children [i];
				Driver.SetAttribute (item == null ? ColorScheme.Normal : i == current ? ColorScheme.Focus : ColorScheme.Normal);
				if (item == null) {
					Move (0, i + 1);
					Driver.AddRune (Driver.LeftTee);
				} else
					Move (1, i+1);

				Driver.SetAttribute (DetermineColorSchemeFor (item, i));
				for (int p = 0; p < Frame.Width - 2; p++)
					if (item == null)
						Driver.AddRune (Driver.HLine);
					else if (p == Frame.Width - 3 && barItems.Children[i].SubMenu != null)
						Driver.AddRune ('>');
					else
						Driver.AddRune (' ');

				if (item == null) {
					Move (region.Right - 1, i + 1);
					Driver.AddRune (Driver.RightTee);
					continue;
				}

				Move (2, i + 1);
				if (!item.IsEnabled ())
					DrawHotString (item.Title, ColorScheme.Disabled, ColorScheme.Disabled);
				else
					DrawHotString (item.Title,
				               i == current? ColorScheme.HotFocus : ColorScheme.HotNormal,
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
			Move (2, 1 + current);
		}

		void Run (Action action)
		{
			if (action == null)
				return;

			Application.MainLoop.AddIdle (action);
				CloseSubMenu();
				host.CloseMenu ();
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			bool disabled;
			switch (kb.Key) {
			case Key.CursorUp:
				if (current == -1)
					break;
				do {
					disabled = false;
					current--;
					if (host.UseKeysUpDownAsKeysLeftRight) {
						if (current == -1 && barItems.Children [current + 1].IsFromSubMenu && selectedSub > -1) {
							current++;
							PreviousMenu ();
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
				do {
					current++;
					disabled = false;
					if (current == barItems.Children.Length)
						current = 0;
					var item = barItems.Children [current];
					if (item == null || !item.IsEnabled ()) disabled = true;
					if (host.UseKeysUpDownAsKeysLeftRight && barItems.Children [current] != null && !disabled) {
						CheckSubMenu ();
						break;
					}
				} while (barItems.Children [current] == null || disabled);
				SetNeedsDisplay ();
				break;
			case Key.CursorLeft:
				PreviousMenu ();
				break;
			case Key.CursorRight:
				NextMenu ();
				break;
			case Key.Esc:
				CloseSubMenu ();
				host.CloseMenu ();
				break;
			case Key.Enter:
				CheckSubMenu ();
				Run (barItems.Children [current].Action);
				break;
			default:
				// TODO: rune-ify
				if (Char.IsLetterOrDigit ((char)kb.KeyValue)) {
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

		public override bool MouseEvent(MouseEvent me)
		{
			bool disabled;
			if (me.Flags == MouseFlags.Button1Clicked || me.Flags == MouseFlags.Button1Released) {
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
			}
			if (me.Flags == MouseFlags.Button1Pressed ||
				me.Flags == MouseFlags.ReportMousePosition) {
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

		private void CheckSubMenu ()
		{
			if (barItems.Children [current] == null)
				return;
			var subMenu = barItems.Children [current].SubMenu;
			if (subMenu != null) {
				int pos = -1;
				if (openSubMenu != null)
					pos = openSubMenu.FindIndex (o => o?.barItems == subMenu);
				Activate (pos);
			} else if (openSubMenu != null && !barItems.Children [current].IsFromSubMenu)
				CloseSubMenu ();
			else {

			}
		}

		private int GetSubMenuIndex (object subMenu)
		{
			if (subMenu == null || openSubMenu == null)
				return -1;

			int pos = -1;
			if (host.openMenu?.ToString () == subMenu.ToString ())
				return 0;

			for (int i = 0; i < openSubMenu.Count; i++) {
				if (openSubMenu[i].ToString () == subMenu.ToString ()) {
					pos = i;
					break;
				}
			}
			return pos;
		}

		internal static List<Menu> openSubMenu;
		View previousSubFocused;
		static int selectedSub;

		void Activate (int idx)
		{
			selectedSub = idx;
			if (openSubMenu == null || openSubMenu?.Count == 0 || (openSubMenu.Count > 0 && current == 0))
				previousSubFocused = SuperView.Focused;

			OpenSubMenu (idx);
			SetNeedsDisplay ();
		}

		void OpenSubMenu (int index)
		{
			if (openSubMenu == null)
				openSubMenu = new List<Menu> ();

			if (index > -1) {
				RemoveSubMenu (index);
			} else {
				openSubMenu.Add (new Menu (host, Frame.Left + Frame.Width, Frame.Top + 1 + current, barItems.Children [current].SubMenu));
				SuperView.Add (openSubMenu.Last ());
			}
			selectedSub = openSubMenu.Count - 1;
			SuperView.SetFocus (openSubMenu.Last ());
		}

		private void RemoveSubMenu (int index)
		{
			for (int i = openSubMenu.Count - 1; i > index; i--) {
				if (openSubMenu.Count - 1 > 0)
					SuperView.SetFocus (openSubMenu [i - 1]);
				else
					SuperView.SetFocus (host.openMenu);
				SuperView.Remove (openSubMenu [i]);
				openSubMenu.Remove (openSubMenu [i]);
				RemoveSubMenu (i);
			}
		}

		internal void CloseSubMenu ()
		{
			selectedSub = -1;
			SetNeedsDisplay ();
			RemoveAllOpensSubMenus ();
			previousSubFocused?.SuperView?.SetFocus (previousSubFocused);
			openSubMenu = null;
		}

		private void RemoveAllOpensSubMenus ()
		{
			if (openSubMenu != null) {
				foreach (var item in openSubMenu) {
					SuperView.Remove (item);
				}
			}
		}

		void PreviousMenu ()
		{
			if (selectedSub > -1) {
				selectedSub--;
				RemoveSubMenu (selectedSub);
				SetNeedsDisplay ();
			} else
				host.PreviousMenu ();
		}

		void NextMenu ()
		{
			if (host.UseKeysUpDownAsKeysLeftRight)
				host.NextMenu ();
			else {
				if ((selectedSub == -1 || openSubMenu == null || openSubMenu?.Count == selectedSub) && barItems.Children [current].SubMenu == null) {
					if (openSubMenu != null)
						CloseSubMenu ();
					host.NextMenu ();
				} else
					selectedSub++;
				SetNeedsDisplay ();
				CheckSubMenu ();
			}
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
		int selected;
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
			CanFocus = false;
			selected = -1;
			ColorScheme = Colors.Menu;
			WantMousePositionReports = true;
		}

		public override void Redraw (Rect region)
		{
			if (!HasFocus && openMenu != null && !openMenu.HasFocus && !openMenu.barItems.Children[openMenu.current].IsFromSubMenu) {
				CloseMenu ();
			}

			Move (0, 0);
			Driver.SetAttribute (Colors.Base.Focus);
			for (int i = 0; i < Frame.Width; i++)
				Driver.AddRune (' ');

			Move (1, 0);
			int pos = 1;

			for (int i = 0; i < Menus.Length; i++) {
				var menu = Menus [i];
				Move (pos, 0);
				Attribute hotColor, normalColor;
				if (i == selected){
					hotColor = i == selected ? ColorScheme.HotFocus : ColorScheme.HotNormal;
					normalColor = i == selected ? ColorScheme.Focus : ColorScheme.Normal;
				} else {
					hotColor = Colors.Base.Focus;
					normalColor = Colors.Base.Focus;
				}
				DrawHotString (" " + menu.Title + " " + "   ", hotColor, normalColor);
				pos += menu.TitleLength+ 3;
			}
			PositionCursor ();
		}

		public override void PositionCursor ()
		{
			int pos = 0;
			for (int i = 0; i < Menus.Length; i++) {
				if (i == selected) {
					pos++;
					Move (pos, 0);
					return;
				} else {
					pos += Menus [i].TitleLength + 4;
				}
			}
			Move (0, 0);
		}

		void Selected (MenuItem item)
		{
			// TODO: Running = false;
			action = item.Action;
		}

		public event EventHandler OnOpenMenu;
		internal Menu openMenu;
		View previousFocused;

		void OpenMenu (int index)
		{
			OnOpenMenu?.Invoke (this, null);

			if (openMenu != null) {
				openMenu.CloseSubMenu ();
				SuperView.Remove (openMenu);
			}
			int pos = 0;
			for (int i = 0; i < index; i++) 
				pos += Menus [i].Title.Length + 3;

			openMenu = new Menu (this, pos, 1, Menus [index]);

			SuperView.Add (openMenu);
			SuperView.SetFocus (openMenu);
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
		}

		// Activates the menu, handles either first focus, or activating an entry when it was already active
		// For mouse events.
		void Activate (int idx)
		{
			selected = idx;
			if (openMenu == null) 
				previousFocused = SuperView.Focused;

			OpenMenu (idx);
			SetNeedsDisplay ();
		}

		internal void CloseMenu ()
		{
			selected = -1;
			SetNeedsDisplay ();
			SuperView.Remove (openMenu);
			previousFocused?.SuperView?.SetFocus (previousFocused);
			openMenu = null;
		}

		internal void PreviousMenu ()
		{
			if (selected <= 0)
				selected = Menus.Length - 1;
			else
				selected--;

			OpenMenu (selected);				
		}

		internal void NextMenu ()
		{
			if (selected == -1)
				selected = 0;
			else if (selected + 1 == Menus.Length)
				selected = 0;
			else
				selected++;
			OpenMenu (selected);
		}

                internal bool FindAndOpenMenuByHotkey(KeyEvent kb)
                {
                    int pos = 0;
                    var c = ((uint)kb.Key & (uint)Key.CharMask);
	            for (int i = 0; i < Menus.Length; i++)
                    {
			    // TODO: this code is duplicated, hotkey should be part of the MenuBarItem
                            var mi = Menus[i];
                            int p = mi.Title.IndexOf('_');
                            if (p != -1 && p + 1 < mi.Title.Length) {
                                    if (mi.Title[p + 1] == c) {
			                    OpenMenu(i);
			                    return true;
                                    }
                            }
                    }
	            return false;
                }

	        public override bool ProcessHotKey (KeyEvent kb)
		{
			if (kb.Key == Key.F9) {
				StartMenu ();
				return true;
			}

                        if (kb.IsAlt)
                        {
                            if (FindAndOpenMenuByHotkey(kb)) return true;
                        }
			var kc = kb.KeyValue;

			return base.ProcessHotKey (kb);
		}

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
				break;

			default:
				var key = kb.KeyValue;
				if ((key >= 'a' && key <= 'z') || (key >= 'A' && key <= 'Z') || (key >= '0' && key <= '9')) {
					char c = Char.ToUpper ((char)key);

					if (Menus [selected].Children == null)
						return false;

					foreach (var mi in Menus [selected].Children) {
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

		public override bool MouseEvent(MouseEvent me)
		{
			if (me.Flags == MouseFlags.Button1Clicked ||
				(me.Flags == MouseFlags.ReportMousePosition && selected > -1)) {
 				int pos = 1;
				int cx = me.X;
				for (int i = 0; i < Menus.Length; i++) {
					if (cx > pos && me.X < pos + 1 + Menus [i].TitleLength) {
						if (selected == i && me.Flags == MouseFlags.Button1Clicked) {
							CloseMenu ();
						} else {
							Activate (i);
						}
						return true;
					}
					pos += 2 + Menus [i].TitleLength + 1;
				}
			}
			return false;
		}
	}

}

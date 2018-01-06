//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// TODO:
//   Add accelerator support (ShortCut in MenuItem)
//   Add mouse support
//   Allow menus inside menus

using System;
namespace Terminal {

	/// <summary>
	/// A menu item has a title, an associated help text, and an action to execute on activation.
	/// </summary>
	public class MenuItem {
		public MenuItem (string title, string help, Action action)
		{
			Title = title ?? "";
			Help = help ?? "";
			Action = action;
			bool nextIsHot = false;
			foreach (var x in title) {
				if (x == '_')
					nextIsHot = true;
				else {
					if (nextIsHot) {
						HotKey = x;
						break;
					}
					nextIsHot = false;
				}
			}
		}

		// The hotkey is used when the menu is active, the shortcut can be triggered
		// when the menu is not active.
		// For example HotKey would be "N" when the File Menu is open (assuming there is a "_New" entry
		// if the ShortCut is set to "Control-N", this would be a global hotkey that would trigger as well
		public char HotKey;
		public Key ShortCut;

		public string Title { get; set; }
		public string Help { get; set; }
		public Action Action { get; set; }
		internal int Width => Title.Length + Help.Length + 1 + 2;
	}

	/// <summary>
	/// A menu bar item contains other menu items.
	/// </summary>
	public class MenuBarItem {
		public MenuBarItem (string title, MenuItem [] children)
		{
			Title = title ?? "";
			Children = children;
		}

		public string Title { get; set; }
		public MenuItem [] Children { get; set; }
		public int Current { get; set; }
	}

	class Menu : View {
		MenuBarItem barItems;
		MenuBar host;

		static Rect MakeFrame (int x, int y, MenuItem [] items)
		{
			int maxW = 0;

			foreach (var item in items) {
				var l = item.Width;
				maxW = Math.Max (l, maxW);
			}

			return new Rect (x, y, maxW + 2, items.Length + 2);
		}

		public Menu (MenuBar host, int x, int y, MenuBarItem barItems) : base (MakeFrame (x, y, barItems.Children))
		{
			this.barItems = barItems;
			this.host = host;
			CanFocus = true;
		}

		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (Colors.Menu.Normal);
			DrawFrame (region, true);

			for (int i = 0; i < barItems.Children.Length; i++){
				var item = barItems.Children [i];
				Move (1, i+1);
				Driver.SetAttribute (item == null ? Colors.Base.Focus : i == barItems.Current ? Colors.Menu.Focus : Colors.Menu.Normal);
				for (int p = 0; p < Frame.Width-2; p++)
					if (item == null)
						Driver.AddSpecial (SpecialChar.HLine);
					else
						Driver.AddCh (' ');

				if (item == null)
					continue;

				Move (2, i + 1);
				DrawHotString (item.Title,
				               i == barItems.Current ? Colors.Menu.HotFocus : Colors.Menu.HotNormal,
				               i == barItems.Current ? Colors.Menu.Focus : Colors.Menu.Normal);

				// The help string
				var l = item.Help.Length;
				Move (Frame.Width - l - 2, 1 + i);
				Driver.AddStr (item.Help);
			}
		}

		public override void PositionCursor ()
		{
			Move (2, 1 + barItems.Current);
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.CursorUp:
				barItems.Current--;
				if (barItems.Current < 0)
					barItems.Current = barItems.Children.Length - 1;
				SetNeedsDisplay ();
				break;
			case Key.CursorDown:
				barItems.Current++;
				if (barItems.Current == barItems.Children.Length)
					barItems.Current = 0;
				SetNeedsDisplay ();
				break;
			case Key.CursorLeft:
				host.PreviousMenu ();
				break;
			case Key.CursorRight:
				host.NextMenu ();
				break;
			case Key.Esc:
				host.CloseMenu ();
				break;
			}
			return true;
		}
	}

	/// <summary>
	/// A menu bar for your application.
	/// </summary>
	public class MenuBar : View {
		public MenuBarItem [] Menus { get; set; }
		int selected;
		Action action;
		bool opened;

		public MenuBar (MenuBarItem [] menus) : base (new Rect (0, 0, Application.Driver.Cols, 1))
		{
			Menus = menus;
			CanFocus = false;
			selected = -1;
		}

		/// <summary>
		///   Activates the menubar
		/// </summary>
		public void Activate (int idx)
		{
			if (idx < 0 || idx > Menus.Length)
				throw new ArgumentException ("idx");

			action = null;
			selected = idx;

			foreach (var m in Menus)
				m.Current = 0;

			// TODO: Application.Run (this);
			selected = -1;
			SuperView.SetNeedsDisplay ();

			if (action != null)
				action ();
		}

		void DrawMenu (int idx, int col, int line)
		{
			int max = 0;
			var menu = Menus [idx];

			if (menu.Children == null)
				return;

			foreach (var m in menu.Children) {
				if (m == null)
					continue;

				if (m.Width > max)
					max = m.Width;
			}
			max += 4;
			DrawFrame (new Rect (col, line, max, menu.Children.Length + 2), true);

		}

		public override void Redraw (Rect region)
		{
			Move (0, 0);
			Driver.SetAttribute (Colors.Base.Focus);
			for (int i = 0; i < Frame.Width; i++)
				Driver.AddCh (' ');

			Move (1, 0);
			int pos = 1;

			for (int i = 0; i < Menus.Length; i++) {
				var menu = Menus [i];
				if (i == selected) {
					DrawMenu (i, pos, 1);
				}
				Move (pos, 0);
				Attribute hotColor, normalColor;
				if (opened){
					hotColor = i == selected ? Colors.Menu.HotFocus : Colors.Menu.HotNormal;
					normalColor = i == selected ? Colors.Menu.Focus : Colors.Menu.Normal;
				} else {
					hotColor = Colors.Base.Focus;
					normalColor = Colors.Base.Focus;
				}
				DrawHotString (" " + menu.Title + " " + "   ", hotColor, normalColor);
				pos += menu.Title.Length + 3;
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
					pos += Menus [i].Title.Length + 4;
				}
			}
			Move (0, 0);
		}

		void Selected (MenuItem item)
		{
			// TODO: Running = false;
			action = item.Action;
		}

		Menu openMenu;
		View focusedWhenOpened;

		void OpenMenu ()
		{
			if (openMenu != null)
				return;

			focusedWhenOpened = SuperView.MostFocused;
			openMenu = new Menu (this, 0, 1, Menus [0]);
			// Save most deeply focused chain
			SuperView.Add (openMenu);
			SuperView.SetFocus (openMenu);
		}

		internal void CloseMenu ()
		{
			SetNeedsDisplay ();
			SuperView.Remove (openMenu);
			focusedWhenOpened.SuperView.SetFocus (focusedWhenOpened);
			openMenu = null;
		}

		internal void PreviousMenu ()
		{
		}

		internal void NextMenu ()
		{
			}

		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (kb.Key == Key.F9) {
				OpenMenu ();
				return true;
			}
			return base.ProcessHotKey (kb);
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.CursorUp:
				if (Menus [selected].Children == null)
					return false;

				int current = Menus [selected].Current;
				do {
					current--;
					if (current < 0)
						current = Menus [selected].Children.Length - 1;
				} while (Menus [selected].Children [current] == null);
				Menus [selected].Current = current;

				SetNeedsDisplay ();
				return true;

			case Key.CursorDown:
				if (Menus [selected].Children == null)
					return false;

				do {
					Menus [selected].Current = (Menus [selected].Current + 1) % Menus [selected].Children.Length;
				} while (Menus [selected].Children [Menus [selected].Current] == null);

				SetNeedsDisplay ();
				break;

			case Key.CursorLeft:
				selected--;
				if (selected < 0)
					selected = Menus.Length - 1;
				break;
			case Key.CursorRight:
				selected = (selected + 1) % Menus.Length;
				break;

			case Key.Enter:
				if (Menus [selected].Children == null)
					return false;

				Selected (Menus [selected].Children [Menus [selected].Current]);
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
	}

}

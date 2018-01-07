//
// Menu.cs: application menus and submenus
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// TODO:
//   Add accelerator support, but should also support chords (ShortCut in MenuItem)
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
			SetTitle (title ?? "");
			Children = children;
		}

		void SetTitle (string title)
		{
			if (title == null)
				title = "";
			Title = title;
			int len = 0;
			foreach (var ch in Title) {
				if (ch == '_')
					continue;
				len++;
			}
			TitleLength = len;
		}

		public string Title { get; set; }
		public MenuItem [] Children { get; set; }
		internal int TitleLength { get; private set; }
	}

	class Menu : View {
		MenuBarItem barItems;
		MenuBar host;
		int current;

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
				Driver.SetAttribute (item == null ? Colors.Base.Focus : i == current ? Colors.Menu.Focus : Colors.Menu.Normal);
				for (int p = 0; p < Frame.Width-2; p++)
					if (item == null)
						Driver.AddSpecial (SpecialChar.HLine);
					else
						Driver.AddCh (' ');

				if (item == null)
					continue;

				Move (2, i + 1);
				DrawHotString (item.Title,
				               i == current? Colors.Menu.HotFocus : Colors.Menu.HotNormal,
				               i == current ? Colors.Menu.Focus : Colors.Menu.Normal);

				// The help string
				var l = item.Help.Length;
				Move (Frame.Width - l - 2, 1 + i);
				Driver.AddStr (item.Help);
			}
		}

		public override void PositionCursor ()
		{
			Move (2, 1 + current);
		}

		void Run (Action action)
		{
			if (action == null)
				return;
			
			Application.MainLoop.AddIdle (() => {
				action ();
				return false;
			});
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.CursorUp:
				current--;
				if (current < 0)
					current = barItems.Children.Length - 1;
				SetNeedsDisplay ();
				break;
			case Key.CursorDown:
				current++;
				if (current== barItems.Children.Length)
					current = 0;
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
			case Key.Enter:
				host.CloseMenu ();
				Run (barItems.Children [current].Action);
				break;
			default:
				// TODO: rune-ify
				if (Char.IsLetterOrDigit ((char)kb.KeyValue)) {
					var x = Char.ToUpper ((char)kb.KeyValue);

					foreach (var item in barItems.Children) {
						if (item.HotKey == x) {
							host.CloseMenu ();
							Run (item.Action);
							return true;
						}
					}
				}
				break;
			}
			return false;
		}
	}

	/// <summary>
	/// A menu bar for your application.
	/// </summary>
	public class MenuBar : View {
		public MenuBarItem [] Menus { get; set; }
		int selected;
		Action action;

		public MenuBar (MenuBarItem [] menus) : base (new Rect (0, 0, Application.Driver.Cols, 1))
		{
			Menus = menus;
			CanFocus = false;
			selected = -1;
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
				Move (pos, 0);
				Attribute hotColor, normalColor;
				if (i == selected){
					hotColor = i == selected ? Colors.Menu.HotFocus : Colors.Menu.HotNormal;
					normalColor = i == selected ? Colors.Menu.Focus : Colors.Menu.Normal;
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

		Menu openMenu;
		View previousFocused;

		void OpenMenu (int index)
		{
			if (openMenu != null)
				SuperView.Remove (openMenu);
			
			int pos = 0;
			for (int i = 0; i < index; i++) 
				pos += Menus [i].Title.Length + 3;

			openMenu = new Menu (this, pos, 1, Menus [index]);

			SuperView.Add (openMenu);
			SuperView.SetFocus (openMenu);
		}

		void StartMenu ()
		{
			if (openMenu != null)
				return;
			selected = 0;
			SetNeedsDisplay ();

			previousFocused = SuperView.Focused;
			OpenMenu (selected);
		}

		internal void CloseMenu ()
		{
			selected = -1;
			SetNeedsDisplay ();
			SuperView.Remove (openMenu);
			previousFocused.SuperView.SetFocus (previousFocused);
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

		public override bool ProcessHotKey (KeyEvent kb)
		{
			if (kb.Key == Key.F9) {
				StartMenu ();
				return true;
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
	}

}

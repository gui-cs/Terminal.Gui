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
			Width = Title.Length + Help.Length + 1;
		}
		public string Title { get; set; }
		public string Help { get; set; }
		public Action Action { get; set; }
		public int Width { get; set; }
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
			for (int i = 0; i < menu.Children.Length; i++) {
				var item = menu.Children [i];

				Move (line + 1 + i, col + 1);
				Driver.SetAttribute (item == null ? Colors.Base.Focus : i == menu.Current ? Colors.Menu.MarkedSelected : Colors.Menu.Marked);
				for (int p = 0; p < max - 2; p++)
					if (item == null)
						Driver.AddSpecial (SpecialChar.HLine);
					else
						Driver.AddCh (' ');

				if (item == null)
					continue;

				Move (line + 1 + i, col + 2);
				DrawHotString (item.Title,
				               i == menu.Current ? Colors.Menu.HotFocus: Colors.Menu.HotNormal,
				               i == menu.Current ? Colors.Menu.MarkedSelected : Colors.Menu.Marked);

				// The help string
				var l = item.Help.Length;
				Move (col + max - l - 2, line + 1 + i); 
				Driver.AddStr (item.Help);
			}
		}

		public override void Redraw (Rect region)
		{
			Move (0, 0);
			Driver.SetAttribute (Colors.Base.Focus);
			for (int i = 0; i < Frame.Width; i++)
				Driver.AddCh (' ');

			Move (1, 0);
			int pos = 0;
			for (int i = 0; i < Menus.Length; i++) {
				var menu = Menus [i];
				if (i == selected) {
					DrawMenu (i, pos, 1);
					Driver.SetAttribute (Colors.Menu.MarkedSelected);
				} else
					Driver.SetAttribute (Colors.Menu.Focus);

				Move (pos, 0);
				Driver.AddCh (' ');
				Driver.AddStr(menu.Title);
				Driver.AddCh (' ');
				if (HasFocus && i == selected)
					Driver.SetAttribute (Colors.Menu.MarkedSelected);
				else
					Driver.SetAttribute (Colors.Menu.Marked);
				Driver.AddStr ("  ");

				pos += menu.Title.Length + 4;
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

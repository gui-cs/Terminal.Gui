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
		public MenuItem (ustring title, string help, Action action)
		{
			Title = title ?? "";
			Help = help ?? "";
			Action = action;
			bool nextIsHot = false;
			foreach (var x in Title) {
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
		internal int Width => Title.Length + Help.Length + 1 + 2;
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

		void SetTitle (ustring title)
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
			ColorScheme = Colors.Menu;
			CanFocus = true;
		}

		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (ColorScheme.Normal);
			DrawFrame (region, padding: 0, fill: true);

			for (int i = 0; i < barItems.Children.Length; i++){
				var item = barItems.Children [i];
				Move (1, i+1);
				Driver.SetAttribute (item == null ? Colors.Base.Focus : i == current ? ColorScheme.Focus : ColorScheme.Normal);
				for (int p = 0; p < Frame.Width-2; p++)
					if (item == null)
						Driver.AddRune (Driver.HLine);
					else
						Driver.AddRune (' ');

				if (item == null)
					continue;

				Move (2, i + 1);
				DrawHotString (item.Title,
				               i == current? ColorScheme.HotFocus : ColorScheme.HotNormal,
				               i == current ? ColorScheme.Focus : ColorScheme.Normal);

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
			return true;
		}

		public override bool MouseEvent(MouseEvent me)
		{
			if (me.Flags == MouseFlags.Button1Clicked || me.Flags == MouseFlags.Button1Released) {
				if (me.Y < 1)
					return true;
				var item = me.Y - 1;
				if (item >= barItems.Children.Length)
					return true;
				host.CloseMenu ();
				Run (barItems.Children [item].Action);
				return true;
			}
			if (me.Flags == MouseFlags.Button1Pressed) {
				if (me.Y < 1)
					return true;
				if (me.Y - 1 >= barItems.Children.Length)
					return true;
				current = me.Y - 1;
				SetNeedsDisplay ();
				return true;
			}
			return false;
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
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.MenuBar"/> class with the specified set of toplevel menu items.
		/// </summary>
		/// <param name="menus">Menus.</param>
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
		}

		public override void Redraw (Rect region)
		{
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

		public override bool MouseEvent(MouseEvent me)
		{
			if (me.Flags == MouseFlags.Button1Clicked) {
 				int pos = 1;
				int cx = me.X;
				for (int i = 0; i < Menus.Length; i++) {
					if (cx > pos && me.X < pos + 1 + Menus [i].TitleLength) {
						Activate (i);
						return true;
					}
					pos += 2 + Menus [i].TitleLength + 1;
				}
			}
			return false;
		}
	}

}

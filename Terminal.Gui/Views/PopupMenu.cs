using System;
using NStack;

namespace Terminal.Gui {
	
	/// <summary>
	/// A popup menu window containing menu items.
	/// </summary>
	public class PopupMenu : Toplevel {
		int current;
		//View previousFocused;

		public PopupMenu (int x, int y, ustring title, MenuItem [] children) : base (MakeFrame (x, y, children))
		{
			SetTitle (title ?? "");
			Children = children;
			CanFocus = true;
			current = 0;
			//previousFocused = SuperView.Focused;
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

		public override void Redraw (Rect region)
		{
			Driver.SetAttribute (ColorScheme.Normal);
			DrawFrame (region, padding: 0, fill: true);

			for (int i = 0; i < Children.Length; i++){
				var item = Children [i];
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

		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.CursorUp:
				current--;
				if (current < 0)
					current = Children.Length - 1;
				SetNeedsDisplay ();
				break;
			case Key.CursorDown:
				current++;
				if (current== Children.Length)
					current = 0;
				SetNeedsDisplay ();
				break;
			case Key.Esc:
				CloseMenu ();
				Application.Refresh ();
				break;
			case Key.Enter:
				CloseMenu ();
				Run (Children [current].Action);
				break;
			default:
				// TODO: rune-ify
				if (Char.IsLetterOrDigit ((char)kb.KeyValue)) {
					var x = Char.ToUpper ((char)kb.KeyValue);

					for (var i=0; i<Children.Length; i++) {
						var item = Children[i];
						if (item.HotKey == x) {
							current = i;
							Redraw(Frame);
							CloseMenu ();
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
				if (item >= Children.Length)
					return true;
				current = item;
				Redraw(Frame);
				CloseMenu ();
				Run (Children [item].Action);
				return true;
			}
			if (me.Flags == MouseFlags.Button1Pressed) {
				if (me.Y < 1)
					return true;
				if (me.Y - 1 >= Children.Length)
					return true;
				current = me.Y - 1;
				SetNeedsDisplay ();
				return true;
			}
			return false;
		}		

		internal void CloseMenu ()
		{
			Running = false;
			SetNeedsDisplay ();
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

		static Rect MakeFrame (int x, int y, MenuItem [] items)
		{
			int maxW = 0;

			foreach (var item in items) {
				var l = item.Width;
				maxW = Math.Max (l, maxW);
			}

			return new Rect (x, y, maxW + 2, items.Length + 2);
		}
		
	}

}

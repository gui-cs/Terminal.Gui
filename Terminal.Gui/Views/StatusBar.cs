//
// StatusBar.cs: a statusbar for an application
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
// TODO:
//   Add mouse support
using System;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="StatusItem"/> objects are contained by <see cref="StatusBar"/> <see cref="View"/>s. 
	/// Each <see cref="StatusItem"/> has a title, a shortcut (hotkey), and an <see cref="Action"/> that will be invoked when the 
	/// <see cref="StatusItem.Shortcut"/> is pressed.
	/// The <see cref="StatusItem.Shortcut"/> will be a global hotkey for the application in the current context of the screen.
	/// The colour of the <see cref="StatusItem.Title"/> will be changed after each ~. 
	/// A <see cref="StatusItem.Title"/> set to `~F1~ Help` will render as *F1* using <see cref="ColorScheme.HotNormal"/> and
	/// *Help* as <see cref="ColorScheme.HotNormal"/>.
	/// </summary>
	public class StatusItem {
		/// <summary>
		/// Initializes a new <see cref="StatusItem"/>.
		/// </summary>
		/// <param name="shortcut">Shortcut to activate the <see cref="StatusItem"/>.</param>
		/// <param name="title">Title for the <see cref="StatusItem"/>.</param>
		/// <param name="action">Action to invoke when the <see cref="StatusItem"/> is activated.</param>
		public StatusItem (Key shortcut, ustring title, Action action)
		{
			Title = title ?? "";
			Shortcut = shortcut;
			Action = action;
		}

		/// <summary>
		/// Gets the global shortcut to invoke the action on the menu.
		/// </summary>
		public Key Shortcut { get; }

		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		/// <value>The title.</value>
		/// <remarks>
		/// The colour of the <see cref="StatusItem.Title"/> will be changed after each ~. 
		/// A <see cref="StatusItem.Title"/> set to `~F1~ Help` will render as *F1* using <see cref="ColorScheme.HotNormal"/> and
		/// *Help* as <see cref="ColorScheme.HotNormal"/>.
		/// </remarks>
		public ustring Title { get; set;}

		/// <summary>
		/// Gets or sets the action to be invoked when the statusbar item is triggered
		/// </summary>
		/// <value>Action to invoke.</value>
		public Action Action { get; }
	};

	/// <summary>
	/// A status bar is a <see cref="View"/> that snaps to the bottom of a <see cref="Toplevel"/> displaying set of <see cref="StatusItem"/>s.
	/// The <see cref="StatusBar"/> should be context sensitive. This means, if the main menu and an open text editor are visible, the items probably shown will
	/// be ~F1~ Help ~F2~ Save ~F3~ Load. While a dialog to ask a file to load is executed, the remaining commands will probably be ~F1~ Help.
	/// So for each context must be a new instance of a statusbar.
	/// </summary>
	public class StatusBar : View {
		// After attempting to implement this, I noticed that there are hard dependencies
		// on StatusBar and MenuBars within core. They will need to be refactored for having the
		// StatusBar work at the top
#if SNAP_TO_TOP
		/// <summary>
		/// The style supported by StatusBar
		/// </summary>
		public enum StatusBarStyle {
			Default = 0,
			/// <summary>
			/// The StatusBar will snap at the the bottom line of the Parent view.
			/// If the console window is made larger while the app is runing, the StatusBar
			/// will continue to snap to the bottom line of the Parent, staying visible.
			/// On consoles that support resizing of console apps (e.g. Windows Terminal and ConEmu),
			/// if the console window is subsequently made shorter, the status bar will remain visible
			/// as the Parent view resizes. If Parent is null, the StatusBar will snap to the bottom line
			/// of the console window.
			/// This is the default.
			/// </summary>
			SnapToBottom = Default,

			/// <summary>
			/// The StatusBar will act identically to MenuBar, snapping to the first line of the
			/// console window.
			/// </summary>
			SnapToTop = 1,
		}

		public StatusBarStyle Style { get; set; } = StatusBarStyle.Default;
#endif
		/// <summary>
		/// The parent view of the <see cref="StatusBar"/>.
		/// </summary>
		public View Parent { get; set; }

		/// <summary>
		/// The items that compose the <see cref="StatusBar"/>
		/// </summary>
		public StatusItem [] Items { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="StatusBar"/> class with the specified set of <see cref="StatusItem"/>s.
		/// The <see cref="StatusBar"/> will be drawn on the lowest line of the terminal or <see cref="StatusBar.Parent"/> (if not null).
		/// </summary>
		/// <param name="items">A list of statusbar items.</param>
		public StatusBar (StatusItem [] items) : base ()
		{
			Width = Dim.Fill ();
			Height = 1;
			Items = items;
			CanFocus = false;
			ColorScheme = Colors.Menu;
			X = 0;
			Y = Driver.Rows - 1;
			Width = Dim.Fill ();
			Height = 1;

			LayoutComplete += (sender, e) => {
				X = 0;
				Height = 1;
#if SNAP_TO_TOP
				switch (Style) {
				case StatusBarStyle.SnapToTop:
					X = 0;
					Y = 0;
					break;
				case StatusBarStyle.SnapToBottom:
#endif
					if (Parent == null) {
						Y = Driver.Rows - 1; 
					} else {
						Y = Pos.Bottom (Parent);
					}
#if SNAP_TO_TOP
					break;
				}
#endif
			};
		}

		Attribute ToggleScheme (Attribute scheme)
		{
			var result = scheme == ColorScheme.Normal ? ColorScheme.HotNormal : ColorScheme.Normal;
			Driver.SetAttribute (result);
			return result;
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			//if (Frame.Y != Driver.Rows - 1) {
			//	Frame = new Rect (Frame.X, Driver.Rows - 1, Frame.Width, Frame.Height);
			//	Y = Driver.Rows - 1;
			//	SetNeedsDisplay ();
			//}

			Move (0, 0);
			Driver.SetAttribute (ColorScheme.Normal);
			for (int i = 0; i < Frame.Width; i++)
				Driver.AddRune (' ');

			Move (1, 0);
			var scheme = ColorScheme.Normal;
			Driver.SetAttribute (scheme);
			for (int i = 0; i < Items.Length; i++) {
				var title = Items [i].Title;
				for (int n = 0; n < title.Length; n++) {
					if (title [n] == '~') {
						scheme = ToggleScheme (scheme);
						continue;
					}
					Driver.AddRune (title [n]);
				}
				Driver.AddRune (' ');
			}
		}

		///<inheritdoc/>
		public override bool ProcessHotKey (KeyEvent kb)
		{
			foreach (var item in Items) {
				if (kb.Key == item.Shortcut) {
					item.Action?.Invoke ();
					return true;
				}
			}
			return false;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (me.Flags != MouseFlags.Button1Clicked)
				return false;

			int pos = 1;
			for (int i = 0; i < Items.Length; i++) {
				if (me.X >= pos && me.X < pos + GetItemTitleLength (Items [i].Title)) {
					Run (Items [i].Action);
				}
				pos += GetItemTitleLength (Items [i].Title) + 1;
			}
			return true;
		}

		int GetItemTitleLength (ustring title)
		{
			int len = 0;
			foreach (var ch in title) {
				if (ch == '~')
					continue;
				len++;
			}

			return len;
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
	}
}
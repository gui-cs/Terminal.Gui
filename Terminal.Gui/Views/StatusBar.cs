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
	/// A statusbar item has a title, a shortcut aka hotkey, and an action to execute on activation.
	/// Such an item is ment to be as part of the global hotkeys of the application, which are available in the current context of the screen.
	/// The colour of the text will be changed after each ~. Having an statusbar item with a text of `~F1~ Help` will draw *F1* as shortcut and
	/// *Help* as standard text.
	/// </summary>
	public class StatusItem {
		/// <summary>
		/// Initializes a new <see cref="T:Terminal.Gui.StatusItem"/>.
		/// </summary>
		/// <param name="shortcut">Shortcut to activate the item.</param>
		/// <param name="title">Title for the statusbar item.</param>
		/// <param name="action">Action to invoke when the staturbar item is activated.</param>
		public StatusItem (Key shortcut, ustring title, Action action)
		{
			Title = title ?? "";
			Shortcut = shortcut;
			Action = action;
		}

		/// <summary>
		/// This is the global setting that can be used as a global shortcut to invoke the action on the menu.
		/// </summary>
		public Key Shortcut { get; }

		/// <summary>
		/// Gets or sets the title.
		/// </summary>
		/// <value>The title.</value>
		public ustring Title { get; }

		/// <summary>
		/// Gets or sets the action to be invoked when the statusbar item is triggered
		/// </summary>
		/// <value>Method to invoke.</value>
		public Action Action { get; }
	};

	/// <summary>
	/// A statusbar for your application.
	/// The statusbar should be context sensitive. This means, if the main menu and an open text editor are visible, the items probably shown will
	/// be ~F1~ Help ~F2~ Save ~F3~ Load. While a dialog to ask a file to load is executed, the remaining commands will probably be ~F1~ Help.
	/// So for each context must be a new instance of a statusbar.
	/// </summary>
	public class StatusBar : View {
		public StatusItem [] Items { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.StatusBar"/> class with the specified set of statusbar items.
		/// It will be drawn in the lowest column of the terminal.
		/// </summary>
		/// <param name="items">A list of statusbar items.</param>
		public StatusBar (StatusItem [] items) : base ()
		{
			Width = Dim.Fill ();
			Height = 1;
			Items = items;
			CanFocus = false;
			ColorScheme = Colors.Menu;

			Application.OnLoad += () => {
				this.X = Pos.Left (Application.Top);
				this.Y = Pos.Bottom (Application.Top);
			};
		}

		Attribute ToggleScheme (Attribute scheme)
		{
			var result = scheme == ColorScheme.Normal ? ColorScheme.HotNormal : ColorScheme.Normal;
			Driver.SetAttribute (result);
			return result;
		}

		public override void Redraw (Rect region)
		{
			if (Frame.Y != Driver.Rows - 1) {
				Frame = new Rect (Frame.X, Driver.Rows - 1, Frame.Width, Frame.Height);
				Y = Driver.Rows - 1;
				SetNeedsDisplay ();
			}

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

		public override bool ProcessHotKey (KeyEvent kb)
		{
			foreach (var item in Items) {
				if (kb.Key == item.Shortcut) {
					if (item.Action != null) item.Action ();
					return true;
				}
			}
			return false;
		}
	}
}
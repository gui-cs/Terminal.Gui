//
// Dialog.cs: Dialog box
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Collections.Generic;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// The dialog box is a window that by default is centered and contains one 
	/// or more buttons.
	/// </summary>
	public class Dialog : Window {
		List<Button> buttons = new List<Button> ();
		const int padding = 1;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.Dialog"/> class with an optional set of buttons to display
		/// </summary>
		/// <param name="title">Title for the dialog.</param>
		/// <param name="width">Width for the dialog.</param>
		/// <param name="height">Height for the dialog.</param>
		/// <param name="buttons">Optional buttons to lay out at the bottom of the dialog.</param>
		public Dialog (ustring title, int width, int height, params Button [] buttons) : base (title, padding: padding)
		{
			X = Pos.Center ();
			Y = Pos.Center ();
			Width = width;
			Height = height;
			ColorScheme = Colors.Dialog;

			if (buttons != null) {
				foreach (var b in buttons) {
					this.buttons.Add (b);
					Add (b);
				}
			}
		}

		/// <summary>
		/// Adds a button to the dialog, its layout will be controled by the dialog
		/// </summary>
		/// <param name="button">Button to add.</param>
		public void AddButton (Button button)
		{
			if (button == null)
				return;

			buttons.Add (button);
			Add (button);
		}


		public override void LayoutSubviews ()
		{
			base.LayoutSubviews ();

			int buttonSpace = 0;
			int maxHeight = 0;

			foreach (var b in buttons) {
				buttonSpace += b.Frame.Width + 1;
				maxHeight = Math.Max (maxHeight, b.Frame.Height);
			}
			const int borderWidth = 2;
			var start = (Frame.Width-borderWidth - buttonSpace) / 2;

			var y = Frame.Height - borderWidth  - maxHeight-1-padding;
			foreach (var b in buttons) {
				var bf = b.Frame;

				b.Frame = new Rect (start, y, bf.Width, bf.Height);

				start += bf.Width + 1;
			}
		}

		public override bool ProcessKey (KeyEvent kb)
		{
			switch (kb.Key) {
			case Key.Esc:
				Running = false;
				return true;
			}
			return base.ProcessKey (kb);
		}
	}
}

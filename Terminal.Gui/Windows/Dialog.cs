﻿//
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
	/// The <see cref="Dialog"/> <see cref="View"/> is a <see cref="Window"/> that by default is centered and contains one 
	/// or more <see cref="Button"/>. It defaults to the <see cref="Colors.Dialog"/> color scheme and has a 1 cell padding around the edges.
	/// </summary>
	/// <remarks>
	///  To run the <see cref="Dialog"/> modally, create the <see cref="Dialog"/>, and pass it to <see cref="Application.Run()"/>. 
	///  This will execute the dialog until it terminates via the [ESC] or [CTRL-Q] key, or when one of the views
	///  or buttons added to the dialog calls <see cref="Application.RequestStop"/>.
	/// </remarks>
	public class Dialog : Window {
		List<Button> buttons = new List<Button> ();
		const int padding = 1;

		/// <summary>
		/// Initializes a new instance of the <see cref="Dialog"/> class with an optional set of <see cref="Button"/>s to display
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
			Modal = true;

			if (buttons != null) {
				foreach (var b in buttons) {
					this.buttons.Add (b);
					Add (b);
				}
			}
		}

		/// <summary>
		/// Adds a <see cref="Button"/> to the <see cref="Dialog"/>, its layout will be controled by the <see cref="Dialog"/>
		/// </summary>
		/// <param name="button">Button to add.</param>
		public void AddButton (Button button)
		{
			if (button == null)
				return;

			buttons.Add (button);
			Add (button);
		}

		///<inheritdoc cref="LayoutSubviews"/>
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

		///<inheritdoc cref="ProcessKey"/>
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

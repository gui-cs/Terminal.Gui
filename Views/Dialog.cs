//
// Dialog.cs: Dialog box
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Collections.Generic;

namespace Terminal {
	/// <summary>
	/// The dialog box is a window that by default is centered and contains one 
	/// or more buttons.
	/// </summary>
	public class Dialog : Window {
		List<Button> buttons = new List<Button> ();

		public Dialog (string title, int width, int height, params Button [] buttons) : base (Application.MakeCenteredRect (new Size (width, height)))
		{
			foreach (var b in buttons) {
				this.buttons.Add (b);
				Add (b);
			}
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

			var y = Frame.Height - borderWidth - 2 - maxHeight;
			foreach (var b in buttons) {
				var bf = b.Frame;

				b.Frame = new Rect (start, y, bf.Width, bf.Height);

				start += bf.Width + 1;
			}
		}
	}
}

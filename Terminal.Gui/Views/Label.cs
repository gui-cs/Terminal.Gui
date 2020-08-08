//
// Label.cs: Label control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// The Label <see cref="View"/> displays a string at a given position and supports multiple lines separted by newline characters. Multi-line Labels support word wrap.
	/// </summary>
	/// <remarks>
	/// The <see cref="Label"/> view is functionality identical to <see cref="View"/> and is included for API backwards compatibility.
	/// </remarks>
	public class Label : View {
		/// <inheritdoc/>
		public Label ()
		{
		}

		/// <inheritdoc/>
		public Label (Rect frame) : base (frame)
		{
		}

		/// <inheritdoc/>
		public Label (ustring text) : base (text)
		{
		}

		/// <inheritdoc/>
		public Label (Rect rect, ustring text) : base (rect, text)
		{
		}

		/// <inheritdoc/>
		public Label (int x, int y, ustring text) : base (x, y, text)
		{
		}

		/// <summary>
		///   Clicked <see cref="Action"/>, raised when the user clicks the primary mouse button within the Bounds of this <see cref="View"/>
		///   or if the user presses the action key while this view is focused. (TODO: IsDefault)
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the button is activated either with
		///   the mouse or the keyboard.
		/// </remarks>
		public Action Clicked;

		///// <inheritdoc/>
		//public new ustring Text {
		//	get => base.Text;
		//	set {
		//		base.Text = value;
		//		// This supports Label auto-sizing when Text changes (preserving backwards compat behavior)
		//		if (Frame.Height == 1 && !ustring.IsNullOrEmpty (value)) {
		//			int w = Text.RuneCount;
		//			Width = w;
		//			Frame = new Rect (Frame.Location, new Size (w, Frame.Height));
		//		}
		//		SetNeedsDisplay ();
		//	}
		//}

		/// <summary>
		/// Method invoked when a mouse event is generated
		/// </summary>
		/// <param name="mouseEvent"></param>
		/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
		public override bool OnMouseEvent (MouseEvent mouseEvent)
		{
			MouseEventArgs args = new MouseEventArgs (mouseEvent);
			MouseClick?.Invoke (args);
			if (args.Handled)
				return true;
			if (MouseEvent (mouseEvent))
				return true;


			if (mouseEvent.Flags == MouseFlags.Button1Clicked) {
				if (!HasFocus && SuperView != null) {
					SetFocus ();
					SetNeedsDisplay ();
				}

				Clicked?.Invoke ();
				return true;
			}
			return false;
		}
	}
}

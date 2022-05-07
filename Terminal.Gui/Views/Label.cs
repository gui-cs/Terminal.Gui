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
	/// The Label <see cref="View"/> displays a string at a given position and supports multiple lines separated by newline characters.
	/// Multi-line Labels support word wrap.
	/// </summary>
	/// <remarks>
	/// The <see cref="Label"/> view is functionality identical to <see cref="View"/> and is included for API backwards compatibility.
	/// </remarks>
	public class Label : View {
		/// <inheritdoc/>
		public Label ()
		{
			Initialize ();
		}

		/// <inheritdoc/>
		public Label (Rect frame, bool autosize = false) : base (frame)
		{
			Initialize (autosize);
		}

		/// <inheritdoc/>
		public Label (ustring text, bool autosize = true) : base (text)
		{
			Initialize (autosize);
		}

		/// <inheritdoc/>
		public Label (Rect rect, ustring text, bool autosize = false) : base (rect, text)
		{
			Initialize (autosize);
		}

		/// <inheritdoc/>
		public Label (int x, int y, ustring text, bool autosize = true) : base (x, y, text)
		{
			Initialize (autosize);
		}

		/// <inheritdoc/>
		public Label (ustring text, TextDirection direction, bool autosize = true)
			: base (text, direction)
		{
			Initialize (autosize);
		}

		void Initialize (bool autosize = true)
		{
			AutoSize = autosize;
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
		public event Action Clicked;

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
			if (OnMouseClick (args))
				return true;
			if (MouseEvent (mouseEvent))
				return true;

			if (mouseEvent.Flags == MouseFlags.Button1Clicked) {
				if (!HasFocus && SuperView != null) {
					if (!SuperView.HasFocus) {
						SuperView.SetFocus ();
					}
					SetFocus ();
					SetNeedsDisplay ();
				}

				Clicked?.Invoke ();
				return true;
			}
			return false;
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}
}

﻿//
// Label.cs: Label control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
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

		private bool useEffect3DAnimation;

		/// <summary>
		/// Gets or sets if the button will have a <see cref="Border.effect3D"/> with animation.
		/// </summary>
		public bool UseEffect3DAnimation {
			get => useEffect3DAnimation;
			set {
				useEffect3DAnimation = value;
				if (value && Border == null) {
					Border = new Border {
						Effect3D = true,
						UseHalfEffect3D = true,
						Effect3DBrush = new Attribute (Color.Black, Color.Black)
					};
				} else if (!value && Border != null) {
					Border.Effect3D = false;
				}
			}
		}

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

			if (mouseEvent.Flags == MouseFlags.Button1Pressed) {
				if (!HasFocus && SuperView != null) {
					if (!SuperView.HasFocus) {
						SuperView.SetFocus ();
					}
					SetFocus ();
					SetNeedsDisplay ();
				}

				if (UseEffect3DAnimation) {
					Border.Effect3D = false;
					SetNeedsDisplay ();
					Application.GrabMouse (this);
				}

				return true;
			} else if (mouseEvent.Flags == MouseFlags.Button1Released) {
				if (CanFocus && Enabled) {
					if (UseEffect3DAnimation) {
						Border.Effect3D = true;
						SetNeedsDisplay ();
						Redraw (Bounds);
					}
					if (mouseEvent.View == this) {
						OnClicked ();
					}
					Application.UngrabMouse ();
				}
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

		///<inheritdoc/>
		public override bool ProcessHotKey (KeyEvent ke)
		{
			if (ke.Key == (Key.AltMask | HotKey)) {
				if (!HasFocus) {
					SetFocus ();
				}
				OnClicked ();
				return true;
			}
			return base.ProcessHotKey (ke);
		}

		/// <summary>
		/// Virtual method to invoke the <see cref="Clicked"/> event.
		/// </summary>
		public virtual void OnClicked ()
		{
			Clicked?.Invoke ();
		}
	}
}

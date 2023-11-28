﻿//
// Label.cs: Label control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Text;

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
			SetInitialProperties ();
		}

		/// <inheritdoc/>
		public Label (Rect frame, bool autosize = false) : base (frame)
		{
			SetInitialProperties (autosize);
		}

		/// <inheritdoc/>
		public Label (string text, bool autosize = true) : base (text)
		{
			SetInitialProperties (autosize);
		}

		/// <inheritdoc/>
		public Label (Rect rect, string text, bool autosize = false) : base (rect, text)
		{
			SetInitialProperties (autosize);
		}

		/// <inheritdoc/>
		public Label (int x, int y, string text, bool autosize = true) : base (x, y, text)
		{
			SetInitialProperties (autosize);
		}

		/// <inheritdoc/>
		public Label (string text, TextDirection direction, bool autosize = true)
			: base (text, direction)
		{
			SetInitialProperties (autosize);
		}

		void SetInitialProperties (bool autosize = true)
		{
			Height = 1;
			AutoSize = autosize;
			// Things this view knows how to do
			AddCommand (Command.Accept, () => AcceptKey ());

			// Default keybindings for this view
			AddKeyBinding (Key.Enter, Command.Accept);
			AddKeyBinding (Key.Space, Command.Accept);
			if (HotKey != Key.Null) {
				AddKeyBinding (Key.Space | HotKey, Command.Accept);
			}
		}

		bool AcceptKey ()
		{
			if (!HasFocus) {
				SetFocus ();
			}
			OnClicked ();
			return true;
		}
		
		/// <summary>
		///   The event fired when the user clicks the primary mouse button within the Bounds of this <see cref="View"/>
		///   or if the user presses the action key while this view is focused. (TODO: IsDefault)
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the button is activated either with
		///   the mouse or the keyboard.
		/// </remarks>
		public event EventHandler Clicked;

		/// <summary>
		/// Method invoked when a mouse event is generated
		/// </summary>
		/// <param name="mouseEvent"></param>
		/// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
		public override bool OnMouseEvent (MouseEvent mouseEvent)
		{
			MouseEventEventArgs args = new MouseEventEventArgs (mouseEvent);
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

				OnClicked ();
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

		/// <summary>
		/// Virtual method to invoke the <see cref="Clicked"/> event.
		/// </summary>
		public virtual void OnClicked ()
		{
			Clicked?.Invoke (this, EventArgs.Empty);
		}
	}
}

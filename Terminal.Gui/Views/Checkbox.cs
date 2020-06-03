﻿//
// Checkbox.cs: Checkbox control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using NStack;

namespace Terminal.Gui {

	/// <summary>
	/// The <see cref="CheckBox"/> <see cref="View"/> shows an on/off toggle that the user can set
	/// </summary>
	public class CheckBox : View {
		ustring text;
		int hot_pos = -1;
		Rune hot_key;

		/// <summary>
		///   Toggled event, raised when the <see cref="CheckBox"/>  is toggled.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the <see cref="CheckBox"/> is activated either with
		///   the mouse or the keyboard. The passed <c>bool</c> contains the previous state. 
		/// </remarks>
		public event EventHandler<bool> Toggled;

		/// <summary>
		/// Called when the <see cref="Checked"/> property changes. Invokes the <see cref="Toggled"/> event.
		/// </summary>
		public virtual void OnToggled (bool previousChecked) {
			Toggled?.Invoke (this, previousChecked);
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> based on the given text, uses Computed layout and sets the height and width.
		/// </summary>
		/// <param name="s">S.</param>
		/// <param name="is_checked">If set to <c>true</c> is checked.</param>
		public CheckBox (ustring s, bool is_checked = false) : base ()
		{
			Checked = is_checked;
			Text = s;
			CanFocus = true;
			Height = 1;
			Width = s.Length + 4;
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> based on the given text at the given position and a state.
		/// </summary>
		/// <remarks>
		///   The size of <see cref="CheckBox"/> is computed based on the
		///   text length. This <see cref="CheckBox"/> is not toggled.
		/// </remarks>
		public CheckBox (int x, int y, ustring s) : this (x, y, s, false)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> based on the given text at the given position and a state.
		/// </summary>
		/// <remarks>
		///   The size of <see cref="CheckBox"/> is computed based on the
		///   text length. 
		/// </remarks>
		public CheckBox (int x, int y, ustring s, bool is_checked) : base (new Rect (x, y, s.Length + 4, 1))
		{
			Checked = is_checked;
			Text = s;

			CanFocus = true;
		}

		/// <summary>
		///    The state of the <see cref="CheckBox"/>
		/// </summary>
		public bool Checked { get; set; }

		/// <summary>
		///   The text displayed by this <see cref="CheckBox"/>
		/// </summary>
		public ustring Text {
			get {
				return text;
			}

			set {
				text = value;

				int i = 0;
				hot_pos = -1;
				hot_key = (char)0;
				foreach (Rune c in text) {
					if (Rune.IsUpper (c)) {
						hot_key = c;
						hot_pos = i;
						break;
					}
					i++;
				}
			}
		}

		///<inheritdoc cref="Redraw"/>
		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (HasFocus ? ColorScheme.Focus : ColorScheme.Normal);
			Move (0, 0);
			Driver.AddStr (Checked ? "[x] " : "[ ] ");
			Move (4, 0);
			Driver.AddStr (Text);
			if (hot_pos != -1) {
				Move (4 + hot_pos, 0);
				Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : ColorScheme.HotNormal);
				Driver.AddRune (hot_key);
			}
		}

		///<inheritdoc cref="PositionCursor"/>
		public override void PositionCursor ()
		{
			Move (1, 0);
		}

		///<inheritdoc cref="ProcessKey"/>
		public override bool ProcessKey (KeyEvent kb)
		{
			if (kb.KeyValue == ' ') {
				var previousChecked = Checked;
				Checked = !Checked;
				OnToggled (previousChecked);
				SetNeedsDisplay ();
				return true;
			}
			return base.ProcessKey (kb);
		}

		///<inheritdoc cref="MouseEvent"/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked))
				return false;

			SuperView.SetFocus (this);
			var previousChecked = Checked;
			Checked = !Checked;
			OnToggled (previousChecked);
			SetNeedsDisplay ();

			return true;
		}
	}
}

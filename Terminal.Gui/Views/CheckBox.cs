﻿//
// Checkbox.cs: Checkbox control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Text;

namespace Terminal.Gui {

	/// <summary>
	/// The <see cref="CheckBox"/> <see cref="View"/> shows an on/off toggle that the user can set
	/// </summary>
	public class CheckBox : View {
		Rune charNullChecked;
		Rune charChecked;
		Rune charUnChecked;
		bool? @checked;
		bool allowNullChecked;

		/// <summary>
		///   Toggled event, raised when the <see cref="CheckBox"/>  is toggled.
		/// </summary>
		/// <remarks>
		///   Client code can hook up to this event, it is
		///   raised when the <see cref="CheckBox"/> is activated either with
		///   the mouse or the keyboard. The passed <c>bool</c> contains the previous state. 
		/// </remarks>
		public event EventHandler<ToggleEventArgs> Toggled;

		/// <summary>
		/// Called when the <see cref="Checked"/> property changes. Invokes the <see cref="Toggled"/> event.
		/// </summary>
		public virtual void OnToggled (ToggleEventArgs e)
		{
			Toggled?.Invoke (this, e);
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> based on the given text, using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		public CheckBox () : this (string.Empty) { }

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> based on the given text, using <see cref="LayoutStyle.Computed"/> layout.
		/// </summary>
		/// <param name="s">S.</param>
		/// <param name="is_checked">If set to <c>true</c> is checked.</param>
		public CheckBox (string s, bool is_checked = false) : base ()
		{
			SetInitialProperties (s, is_checked);
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		///   The size of <see cref="CheckBox"/> is computed based on the
		///   text length. This <see cref="CheckBox"/> is not toggled.
		/// </remarks>
		public CheckBox (int x, int y, string s) : this (x, y, s, false)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		///   The size of <see cref="CheckBox"/> is computed based on the
		///   text length. 
		/// </remarks>
		public CheckBox (int x, int y, string s, bool is_checked) : base (new Rect (x, y, s.Length, 1))
		{
			SetInitialProperties (s, is_checked);
		}

		// TODO: v2 - Remove constructors with parameters
		/// <summary>
		/// Private helper to set the initial properties of the View that were provided via constructors.
		/// </summary>
		/// <param name="s"></param>
		/// <param name="is_checked"></param>
		void SetInitialProperties (string s, bool is_checked)
		{
			charNullChecked = CM.Glyphs.NullChecked;
			charChecked = CM.Glyphs.Checked;
			charUnChecked = CM.Glyphs.UnChecked;
			Checked = is_checked;
			HotKeySpecifier = (Rune)'_';
			CanFocus = true;
			AutoSize = true;
			Text = s;

			OnResizeNeeded ();

			// Things this view knows how to do
			AddCommand (Command.ToggleChecked, () => ToggleChecked ());

			// Default keybindings for this view
			AddKeyBinding ((Key)' ', Command.ToggleChecked);
			AddKeyBinding (Key.Space, Command.ToggleChecked);
		}

		/// <inheritdoc/>
		protected override void UpdateTextFormatterText ()
		{
			switch (TextAlignment) {
			case TextAlignment.Left:
			case TextAlignment.Centered:
			case TextAlignment.Justified:
				TextFormatter.Text = $"{GetCheckedState ()} {GetFormatterText ()}";
				break;
			case TextAlignment.Right:
				TextFormatter.Text = $"{GetFormatterText ()} {GetCheckedState ()}";
				break;
			}
		}

		Rune GetCheckedState ()
		{
			return Checked switch {
				true => charChecked,
				false => charUnChecked,
				var _ => charNullChecked
			};
		}

		string GetFormatterText ()
		{
			if (AutoSize || string.IsNullOrEmpty (Text) || Frame.Width <= 2) {
				return Text;
			}
			return Text [..Math.Min (Frame.Width - 2, Text.GetRuneCount ())];
		}

		/// <summary>
		///    The state of the <see cref="CheckBox"/>
		/// </summary>
		public bool? Checked {
			get => @checked;
			set {
				if (value == null && !AllowNullChecked) {
					return;
				}
				@checked = value;
				UpdateTextFormatterText ();
				OnResizeNeeded ();
			}
		}

		/// <summary>
		/// If <see langword="true"/> allows <see cref="Checked"/> to be null, true or false.
		/// If <see langword="false"/> only allows <see cref="Checked"/> to be true or false.
		/// </summary>
		public bool AllowNullChecked {
			get => allowNullChecked;
			set {
				allowNullChecked = value;
				Checked ??= false;
			}
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			Move (0, 0);
		}

		///<inheritdoc/>
		public override bool OnKeyPressed (KeyEventArgs kb)
		{
			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			return base.OnKeyPressed (kb);
		}

		///<inheritdoc/>
		public override bool OnHotKeyPressed (KeyEventArgs kb)
		{
			if (kb.Key == (Key.AltMask | HotKey))
				return ToggleChecked ();

			return false;
		}

		bool ToggleChecked ()
		{
			if (!HasFocus) {
				SetFocus ();
			}
			var previousChecked = Checked;
			if (AllowNullChecked) {
				switch (previousChecked) {
				case null:
					Checked = true;
					break;
				case true:
					Checked = false;
					break;
				case false:
					Checked = null;
					break;
				}
			} else {
				Checked = !Checked;
			}

			OnToggled (new ToggleEventArgs (previousChecked, Checked));
			SetNeedsDisplay ();
			return true;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) || !CanFocus)
				return false;

			ToggleChecked ();

			return true;
		}

		///<inheritdoc/>
		public override bool OnEnter (View view)
		{
			Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

			return base.OnEnter (view);
		}
	}
}

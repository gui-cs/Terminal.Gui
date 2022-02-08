//
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
		public event Action<bool> Toggled;

		/// <summary>
		/// Called when the <see cref="Checked"/> property changes. Invokes the <see cref="Toggled"/> event.
		/// </summary>
		public virtual void OnToggled (bool previousChecked)
		{
			Toggled?.Invoke (previousChecked);
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
		public CheckBox (ustring s, bool is_checked = false) : base ()
		{
			Initialize (s, is_checked);
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		///   The size of <see cref="CheckBox"/> is computed based on the
		///   text length. This <see cref="CheckBox"/> is not toggled.
		/// </remarks>
		public CheckBox (int x, int y, ustring s) : this (x, y, s, false)
		{
		}

		/// <summary>
		/// Initializes a new instance of <see cref="CheckBox"/> using <see cref="LayoutStyle.Absolute"/> layout.
		/// </summary>
		/// <remarks>
		///   The size of <see cref="CheckBox"/> is computed based on the
		///   text length. 
		/// </remarks>
		public CheckBox (int x, int y, ustring s, bool is_checked) : base (new Rect (x, y, s.Length + 4, 1))
		{
			Initialize (s, is_checked);
		}

		void Initialize (ustring s, bool is_checked)
		{
			Checked = is_checked;
			Text = s;
			CanFocus = true;
			Height = 1;
			Width = s.RuneCount + 4;

			// Things this view knows how to do
			AddCommand (Command.ToggleChecked, () => ToggleChecked ());

			// Default keybindings for this view
			AddKeyBinding ((Key)' ', Command.ToggleChecked);
			AddKeyBinding (Key.Space, Command.ToggleChecked);
		}

		/// <summary>
		///    The state of the <see cref="CheckBox"/>
		/// </summary>
		public bool Checked { get; set; }

		/// <summary>
		///   The text displayed by this <see cref="CheckBox"/>
		/// </summary>
		public new ustring Text {
			get {
				return text;
			}

			set {
				text = value;

				int i = 0;
				hot_pos = -1;
				hot_key = (char)0;
				foreach (Rune c in text) {
					//if (Rune.IsUpper (c)) {
					if (c == '_') {
						hot_key = text [i + 1];
						HotKey = (Key)(char)hot_key.ToString ().ToUpper () [0];
						text = text.ToString ().Replace ("_", "");
						hot_pos = i;
						break;
					}
					i++;
				}
			}
		}

		///<inheritdoc/>
		public override void Redraw (Rect bounds)
		{
			Driver.SetAttribute (HasFocus ? ColorScheme.Focus : GetNormalColor ());
			Move (0, 0);
			Driver.AddRune (Checked ? Driver.Checked : Driver.UnChecked);
			Driver.AddRune (' ');
			Move (2, 0);
			Driver.AddStr (Text);
			if (hot_pos != -1) {
				Move (2 + hot_pos, 0);
				Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : Enabled ? ColorScheme.HotNormal : ColorScheme.Disabled);
				Driver.AddRune (hot_key);
			}
		}

		///<inheritdoc/>
		public override void PositionCursor ()
		{
			Move (0, 0);
		}

		///<inheritdoc/>
		public override bool ProcessKey (KeyEvent kb)
		{
			var result = InvokeKeybindings (kb);
			if (result != null)
				return (bool)result;

			return base.ProcessKey (kb);
		}

		///<inheritdoc/>
		public override bool ProcessHotKey (KeyEvent kb)
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
			Checked = !Checked;
			OnToggled (previousChecked);
			SetNeedsDisplay ();
			return true;
		}

		///<inheritdoc/>
		public override bool MouseEvent (MouseEvent me)
		{
			if (!me.Flags.HasFlag (MouseFlags.Button1Clicked) || !CanFocus)
				return false;

			SetFocus ();
			var previousChecked = Checked;
			Checked = !Checked;
			OnToggled (previousChecked);
			SetNeedsDisplay ();

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

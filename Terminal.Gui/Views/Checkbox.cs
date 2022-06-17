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
		Key hotKey = Key.Null;
		Rune hotKeySpecifier;
		Rune charChecked;
		Rune charUnChecked;
		bool @checked;

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
			charChecked = new Rune (Driver != null ? Driver.Checked : '√');
			charUnChecked = new Rune (Driver != null ? Driver.UnChecked : '╴');
			Checked = is_checked;
			HotKeySpecifier = new Rune ('_');
			CanFocus = true;
			AutoSize = true;
			Text = s;
			Update ();

			// Things this view knows how to do
			AddCommand (Command.ToggleChecked, () => ToggleChecked ());

			// Default keybindings for this view
			AddKeyBinding ((Key)' ', Command.ToggleChecked);
			AddKeyBinding (Key.Space, Command.ToggleChecked);
		}

		void Update ()
		{
			switch (TextAlignment) {
			case TextAlignment.Left:
			case TextAlignment.Centered:
			case TextAlignment.Justified:
				if (Checked)
					TextFormatter.Text = ustring.Make (charChecked) + " " + GetFormatterText ();
				else
					TextFormatter.Text = ustring.Make (charUnChecked) + " " + GetFormatterText ();
				break;
			case TextAlignment.Right:
				if (Checked)
					TextFormatter.Text = GetFormatterText () + " " + ustring.Make (charChecked);
				else
					TextFormatter.Text = GetFormatterText () + " " + ustring.Make (charUnChecked);
				break;
			}

			int w = TextFormatter.Size.Width - (TextFormatter.Text.Contains (HotKeySpecifier)
				? Math.Max (Rune.ColumnWidth (HotKeySpecifier), 0) : 0);
			GetCurrentWidth (out int cWidth);
			var canSetWidth = SetWidth (w, out int rWidth);
			if (canSetWidth && (cWidth < rWidth || AutoSize)) {
				Width = rWidth;
				w = rWidth;
			} else if (!canSetWidth || !AutoSize) {
				w = cWidth;
			}
			var layout = LayoutStyle;
			bool layoutChanged = false;
			if (!(Height is Dim.DimAbsolute)) {
				// The height is always equal to 1 and must be Dim.DimAbsolute.
				layoutChanged = true;
				LayoutStyle = LayoutStyle.Absolute;
			}
			Height = 1;
			if (layoutChanged) {
				LayoutStyle = layout;
			}
			Frame = new Rect (Frame.Location, new Size (w, 1));
			SetNeedsDisplay ();
		}

		ustring GetFormatterText ()
		{
			if (AutoSize || ustring.IsNullOrEmpty (text)) {
				return text;
			}
			return text.RuneSubstring (0, Math.Min (Frame.Width - 2, text.RuneCount));
		}

		/// <inheritdoc/>
		public override Key HotKey {
			get => hotKey;
			set {
				if (hotKey != value) {
					var v = value == Key.Unknown ? Key.Null : value;
					hotKey = v;
				}
			}
		}

		/// <inheritdoc/>
		public override Rune HotKeySpecifier {
			get => hotKeySpecifier;
			set {
				hotKeySpecifier = TextFormatter.HotKeySpecifier = value;
			}
		}

		/// <inheritdoc/>
		public override bool AutoSize {
			get => base.AutoSize;
			set {
				base.AutoSize = value;
				Update ();
			}
		}

		/// <summary>
		///    The state of the <see cref="CheckBox"/>
		/// </summary>
		public bool Checked {
			get => @checked;
			set {
				@checked = value;
				Update ();
			}
		}

		/// <summary>
		///   The text displayed by this <see cref="CheckBox"/>
		/// </summary>
		public new ustring Text {
			get {
				return text;
			}

			set {
				text = value;
				TextFormatter.FindHotKey (text, HotKeySpecifier, true, out _, out Key hk);
				if (hotKey != hk) {
					HotKey = hk;
				}
				Update ();
			}
		}

		///<inheritdoc/>
		public override TextAlignment TextAlignment {
			get => base.TextAlignment;
			set {
				base.TextAlignment = value;
				Update ();
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

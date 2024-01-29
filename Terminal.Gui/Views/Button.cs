//
// Button.cs: Button control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

namespace Terminal.Gui;

/// <summary>
///         Button is a <see cref="View" /> that provides an item that invokes raises the <see cref="Clicked" /> event.
/// </summary>
/// <remarks>
///         <para>
///                 Provides a button showing text that raises the <see cref="Clicked" /> event when clicked on with a
///                 mouse
///                 or when the user presses SPACE, ENTER, or the <see cref="View.HotKey" />. The hot key is the first
///                 letter or
///                 digit following the first underscore ('_')
///                 in the button text.
///         </para>
///         <para>
///                 Use <see cref="View.HotKeySpecifier" /> to change the hot key specifier from the default of ('_').
///         </para>
///         <para>
///                 When the button is configured as the default (<see cref="IsDefault" />) and the user presses
///                 the ENTER key, if no other <see cref="View" /> processes the key, the <see cref="Button" />'s
///                 <see cref="Clicked" /> event will will be fired.
///         </para>
/// </remarks>
public class Button : View {
	bool _isDefault;
	readonly Rune _leftBracket;
	readonly Rune _leftDefault;
	readonly Rune _rightBracket;
	readonly Rune _rightDefault;

	/// <summary>
	///         Initializes a new instance of <see cref="Button" /> using <see cref="LayoutStyle.Computed" /> layout.
	/// </summary>
	/// <remarks>
	///         The width of the <see cref="Button" /> is computed based on the
	///         text length. The height will always be 1.
	/// </remarks>
	public Button ()
	{
		TextAlignment = TextAlignment.Centered;
		VerticalTextAlignment = VerticalTextAlignment.Middle;

		HotKeySpecifier = new Rune ('_');

		_leftBracket = Glyphs.LeftBracket;
		_rightBracket = Glyphs.RightBracket;
		_leftDefault = Glyphs.LeftDefaultIndicator;
		_rightDefault = Glyphs.RightDefaultIndicator;

		CanFocus = true;
		AutoSize = true;
		// Ensures a height of 1 if AutoSize is set to false
		Height = 1;

		// Override default behavior of View
		// Command.Default sets focus
		AddCommand (Command.Accept, () => {
			OnClicked ();
			return true;
		});
		KeyBindings.Add (Key.Space, Command.Default, Command.Accept);
		KeyBindings.Add (Key.Enter, Command.Default, Command.Accept);
	}

	/// <summary>
	///         Gets or sets whether the <see cref="Button" /> is the default action to activate in a dialog.
	/// </summary>
	/// <value><c>true</c> if is default; otherwise, <c>false</c>.</value>
	public bool IsDefault {
		get => _isDefault;
		set {
			_isDefault = value;
			UpdateTextFormatterText ();
			OnResizeNeeded ();
		}
	}

	/// <summary>
	/// </summary>
	public bool NoDecorations { get; set; }

	/// <summary>
	/// </summary>
	public bool NoPadding { get; set; }

	/// <inheritdoc />
	protected override void UpdateTextFormatterText ()
	{
		if (NoDecorations) {
			TextFormatter.Text = Text;
		} else if (IsDefault) {
			TextFormatter.Text = $"{_leftBracket}{_leftDefault} {Text} {_rightDefault}{_rightBracket}";
		} else {
			if (NoPadding) {
				TextFormatter.Text = $"{_leftBracket}{Text}{_rightBracket}";
			} else {
				TextFormatter.Text = $"{_leftBracket} {Text} {_rightBracket}";
			}
		}
	}

	/// <summary>
	///         Virtual method to invoke the <see cref="Clicked" /> event.
	/// </summary>
	public virtual void OnClicked () => Clicked?.Invoke (this, EventArgs.Empty);

	/// <summary>
	///         The event fired when the user clicks the primary mouse button within the Bounds of this <see cref="View" />
	///         or if the user presses the action key while this view is focused. (TODO: IsDefault)
	/// </summary>
	/// <remarks>
	///         Client code can hook up to this event, it is
	///         raised when the button is activated either with
	///         the mouse or the keyboard.
	/// </remarks>
	public event EventHandler Clicked;

	/// <inheritdoc />
	public override bool MouseEvent (MouseEvent me)
	{
		if (me.Flags == MouseFlags.Button1Clicked) {
			if (CanFocus && Enabled) {
				if (!HasFocus) {
					SetFocus ();
					SetNeedsDisplay ();
					Draw ();
				}

				OnClicked ();
			}

			return true;
		}

		return false;
	}

	/// <inheritdoc />
	public override void PositionCursor ()
	{
		if (HotKey.IsValid && Text != "") {
			for (var i = 0; i < TextFormatter.Text.GetRuneCount (); i++) {
				if (TextFormatter.Text [i] == Text [0]) {
					Move (i, 0);
					return;
				}
			}
		}

		base.PositionCursor ();
	}

	/// <inheritdoc />
	public override bool OnEnter (View view)
	{
		Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

		return base.OnEnter (view);
	}
}
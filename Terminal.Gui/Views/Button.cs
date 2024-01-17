//
// Button.cs: Button control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Text;

namespace Terminal.Gui;
/// <summary>
/// Button is a <see cref="View"/> that provides an item that invokes raises the <see cref="Clicked"/> event.
/// </summary>
/// <remarks>
///         <para>
///         Provides a button showing text that raises the <see cref="Clicked"/> event when clicked on with a mouse
///         or when the user presses SPACE, ENTER, or the <see cref="View.HotKey"/>. The hot key is the first letter or
///         digit following the first underscore ('_')
///         in the button text.
///         </para>
///         <para>
///         Use <see cref="View.HotKeySpecifier"/> to change the hot key specifier from the default of ('_').
///         </para>
///         <para>
///         When the button is configured as the default (<see cref="IsDefault"/>) and the user presses
///         the ENTER key, if no other <see cref="View"/> processes the key, the <see cref="Button"/>'s
///         <see cref="Clicked"/> event will will be fired.
///         </para>
/// </remarks>
public class Button : View {
	bool _isDefault;
	Rune _leftBracket;
	Rune _leftDefault;
	Rune _rightBracket;
	Rune _rightDefault;

	/// <summary>
	/// Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Computed"/> layout.
	/// </summary>
	/// <remarks>
	/// The width of the <see cref="Button"/> is computed based on the
	/// text length. The height will always be 1.
	/// </remarks>
	public Button () : this (string.Empty, false) { }

	/// <summary>
	/// Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Computed"/> layout.
	/// </summary>
	/// <remarks>
	/// The width of the <see cref="Button"/> is computed based on the
	/// text length. The height will always be 1.
	/// </remarks>
	/// <param name="text">The button's text</param>
	/// <param name="is_default">
	/// If <c>true</c>, a special decoration is used, and the user pressing the enter key
	/// in a <see cref="Dialog"/> will implicitly activate this button.
	/// </param>
	public Button (string text, bool is_default = false) : base (text) => SetInitialProperties (text, is_default);

	/// <summary>
	/// Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Absolute"/> layout, based on the given
	/// text
	/// </summary>
	/// <remarks>
	/// The width of the <see cref="Button"/> is computed based on the
	/// text length. The height will always be 1.
	/// </remarks>
	/// <param name="x">X position where the button will be shown.</param>
	/// <param name="y">Y position where the button will be shown.</param>
	/// <param name="text">The button's text</param>
	public Button (int x, int y, string text) : this (x, y, text, false) { }

	/// <summary>
	/// Initializes a new instance of <see cref="Button"/> using <see cref="LayoutStyle.Absolute"/> layout, based on the given
	/// text.
	/// </summary>
	/// <remarks>
	/// The width of the <see cref="Button"/> is computed based on the
	/// text length. The height will always be 1.
	/// </remarks>
	/// <param name="x">X position where the button will be shown.</param>
	/// <param name="y">Y position where the button will be shown.</param>
	/// <param name="text">The button's text</param>
	/// <param name="is_default">
	/// If <c>true</c>, a special decoration is used, and the user pressing the enter key
	/// in a <see cref="Dialog"/> will implicitly activate this button.
	/// </param>
	public Button (int x, int y, string text, bool is_default)
		: base (new Rect (x, y, text.GetRuneCount () + 4 + (is_default ? 2 : 0), 1), text) => SetInitialProperties (text, is_default);

	/// <summary>
	/// Gets or sets whether the <see cref="Button"/> is the default action to activate in a dialog.
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
	/// 
	/// </summary>
	public bool NoDecorations { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public bool NoPadding { get; set; }

	// TODO: v2 - Remove constructors with parameters
	/// <summary>
	/// Private helper to set the initial properties of the View that were provided via constructors.
	/// </summary>
	/// <param name="text"></param>
	/// <param name="is_default"></param>
	void SetInitialProperties (string text, bool is_default)
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
		_isDefault = is_default;
		Text = text ?? string.Empty;

		// Override default behavior of View
		// Command.Default sets focus
		AddCommand (Command.Accept, () => {
			OnClicked ();
			return true;
		});
		KeyBindings.Add (Key.Space, Command.Default, Command.Accept);
		KeyBindings.Add (Key.Enter, Command.Default, Command.Accept);
	}

	/// <inheritdoc/>
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
	/// Virtual method to invoke the <see cref="Clicked"/> event.
	/// </summary>
	public virtual void OnClicked () => Clicked?.Invoke (this, EventArgs.Empty);

	/// <summary>
	/// The event fired when the user clicks the primary mouse button within the Bounds of this <see cref="View"/>
	/// or if the user presses the action key while this view is focused. (TODO: IsDefault)
	/// </summary>
	/// <remarks>
	/// Client code can hook up to this event, it is
	/// raised when the button is activated either with
	/// the mouse or the keyboard.
	/// </remarks>
	public event EventHandler Clicked;

	///<inheritdoc/>
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

	///<inheritdoc/>
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

	///<inheritdoc/>
	public override bool OnEnter (View view)
	{
		Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

		return base.OnEnter (view);
	}
}
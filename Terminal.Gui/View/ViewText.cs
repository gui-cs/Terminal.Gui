using System;
using System.Collections.Generic;

namespace Terminal.Gui;

public partial class View {
	string _text;

	/// <summary>
	/// The text displayed by the <see cref="View"/>.
	/// </summary>
	/// <remarks>
	///         <para>
	///         The text will be drawn before any subviews are drawn.
	///         </para>
	///         <para>
	///         The text will be drawn starting at the view origin (0, 0) and will be formatted according
	///         to <see cref="TextAlignment"/> and <see cref="TextDirection"/>.
	///         </para>
	///         <para>
	///         The text will word-wrap to additional lines if it does not fit horizontally. If <see cref="Bounds"/>'s height
	///         is 1, the text will be clipped.
	///         </para>
	///         <para>
	///         Set the <see cref="HotKeySpecifier"/> to enable hotkey support. To disable hotkey support set
	///         <see cref="HotKeySpecifier"/> to
	///         <c>(Rune)0xffff</c>.
	///         </para>
	///         <para>
	///         If <see cref="AutoSize"/> is <c>true</c>, the <see cref="Bounds"/> will be adjusted to fit the text.
	///         </para>
	/// </remarks>
	public virtual string Text {
		get => _text;
		set {
			_text = value;
			SetHotKey ();
			UpdateTextFormatterText ();
			OnResizeNeeded ();

#if DEBUG
			if (_text != null && string.IsNullOrEmpty (Id)) {
				Id = _text;
			}
#endif
		}
	}

	/// <summary>
	/// Gets or sets the <see cref="Gui.TextFormatter"/> used to format <see cref="Text"/>.
	/// </summary>
	public TextFormatter TextFormatter { get; set; }

	/// <summary>
	/// Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
	/// or not when <see cref="TextFormatter.WordWrap"/> is enabled.
	/// If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when
	/// <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
	/// </summary>
	public virtual bool PreserveTrailingSpaces {
		get => TextFormatter.PreserveTrailingSpaces;
		set {
			if (TextFormatter.PreserveTrailingSpaces != value) {
				TextFormatter.PreserveTrailingSpaces = value;
				TextFormatter.NeedsFormat = true;
			}
		}
	}

	/// <summary>
	/// Gets or sets how the View's <see cref="Text"/> is aligned horizontally when drawn. Changing this property will
	/// redisplay the <see cref="View"/>.
	/// </summary>
	/// <remarks>
	///         <para>
	///         If <see cref="AutoSize"/> is <c>true</c>, the <see cref="Bounds"/> will be adjusted to fit the text.
	///         </para>
	/// </remarks>
	/// <value>The text alignment.</value>
	public virtual TextAlignment TextAlignment {
		get => TextFormatter.Alignment;
		set {
			TextFormatter.Alignment = value;
			UpdateTextFormatterText ();
			OnResizeNeeded ();
		}
	}

	/// <summary>
	/// Gets or sets how the View's <see cref="Text"/> is aligned vertically when drawn. Changing this property will redisplay
	/// the <see cref="View"/>.
	/// </summary>
	/// <remarks>
	///         <para>
	///         If <see cref="AutoSize"/> is <c>true</c>, the <see cref="Bounds"/> will be adjusted to fit the text.
	///         </para>
	/// </remarks>
	/// <value>The text alignment.</value>
	public virtual VerticalTextAlignment VerticalTextAlignment {
		get => TextFormatter.VerticalAlignment;
		set {
			TextFormatter.VerticalAlignment = value;
			SetNeedsDisplay ();
		}
	}

	/// <summary>
	/// Gets or sets the direction of the View's <see cref="Text"/>. Changing this property will redisplay the
	/// <see cref="View"/>.
	/// </summary>
	/// <remarks>
	///         <para>
	///         If <see cref="AutoSize"/> is <c>true</c>, the <see cref="Bounds"/> will be adjusted to fit the text.
	///         </para>
	/// </remarks>
	/// <value>The text alignment.</value>
	public virtual TextDirection TextDirection {
		get => TextFormatter.Direction;
		set {
			UpdateTextDirection (value);
			TextFormatter.Direction = value;
		}
	}

	/// <summary>
	/// Can be overridden if the <see cref="Terminal.Gui.TextFormatter.Text"/> has
	/// different format than the default.
	/// </summary>
	protected virtual void UpdateTextFormatterText ()
	{
		if (TextFormatter != null) {
			TextFormatter.Text = _text;
		}
	}

	void UpdateTextDirection (TextDirection newDirection)
	{
		var directionChanged = TextFormatter.IsHorizontalDirection (TextFormatter.Direction) != TextFormatter.IsHorizontalDirection (newDirection);
		TextFormatter.Direction = newDirection;

		var isValidOldAutoSize = AutoSize && IsValidAutoSize (out var _);

		UpdateTextFormatterText ();

		if (!ValidatePosDim && directionChanged && AutoSize || ValidatePosDim && directionChanged && AutoSize && isValidOldAutoSize) {
			OnResizeNeeded ();
		} else if (directionChanged && IsAdded) {
			ResizeBoundsToFit (Bounds.Size);
			// BUGBUG: I think this call is redundant.
			SetFrameToFitText ();
		} else {
			SetFrameToFitText ();
		}
		SetTextFormatterSize ();
		SetNeedsDisplay ();
	}


	/// <summary>
	/// Sets the size of the View to the minimum width or height required to fit <see cref="Text"/>.
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if the size was changed; <see langword="false"/> if <see cref="AutoSize"/> ==
	/// <see langword="true"/> or
	/// <see cref="Text"/> will not fit.
	/// </returns>
	/// <remarks>
	/// Always returns <see langword="false"/> if <see cref="AutoSize"/> is <see langword="true"/> or
	/// if <see cref="Height"/> (Horizontal) or <see cref="Width"/> (Vertical) are not not set or zero.
	/// Does not take into account word wrapping.
	/// </remarks>
	bool SetFrameToFitText ()
	{
		// BUGBUG: This API is broken - should not assume Frame.Height == Bounds.Height
		// <summary>
		// Gets the minimum dimensions required to fit the View's <see cref="Text"/>, factoring in <see cref="TextDirection"/>.
		// </summary>
		// <param name="sizeRequired">The minimum dimensions required.</param>
		// <returns><see langword="true"/> if the dimensions fit within the View's <see cref="Bounds"/>, <see langword="false"/> otherwise.</returns>
		// <remarks>
		// Always returns <see langword="false"/> if <see cref="AutoSize"/> is <see langword="true"/> or
		// if <see cref="Height"/> (Horizontal) or <see cref="Width"/> (Vertical) are not not set or zero.
		// Does not take into account word wrapping.
		// </remarks>
		bool GetMinimumSizeOfText (out Size sizeRequired)
		{
			if (!IsInitialized) {
				sizeRequired = new Size (0, 0);
				return false;
			}
			sizeRequired = Bounds.Size;

			if (AutoSize || string.IsNullOrEmpty (TextFormatter.Text)) {
				return false;
			}

			switch (TextFormatter.IsVerticalDirection (TextDirection)) {
			case true:
				var colWidth = TextFormatter.GetSumMaxCharWidth (new List<string> { TextFormatter.Text }, 0, 1);
				// TODO: v2 - This uses frame.Width; it should only use Bounds
				if (_frame.Width < colWidth &&
				    (Width == null ||
				     Bounds.Width >= 0 &&
				     Width is Dim.DimAbsolute &&
				     Width.Anchor (0) >= 0 &&
				     Width.Anchor (0) < colWidth)) {
					sizeRequired = new Size (colWidth, Bounds.Height);
					return true;
				}
				break;
			default:
				if (_frame.Height < 1 &&
				    (Height == null ||
				     Height is Dim.DimAbsolute &&
				     Height.Anchor (0) == 0)) {
					sizeRequired = new Size (Bounds.Width, 1);
					return true;
				}
				break;
			}
			return false;
		}

		if (GetMinimumSizeOfText (out var size)) {
			// TODO: This is a hack.
			//_width  = size.Width;
			//_height = size.Height;
			_frame = new Rect (_frame.Location, size);
			//throw new InvalidOperationException ("This is a hack.");
			return true;
		}
		return false;
	}

	/// <summary>
	/// Gets the width or height of the <see cref="TextFormatter.HotKeySpecifier"/> characters
	/// in the <see cref="Text"/> property.
	/// </summary>
	/// <remarks>
	/// Only the first HotKey specifier found in <see cref="Text"/> is supported.
	/// </remarks>
	/// <param name="isWidth">
	/// If <see langword="true"/> (the default) the width required for the HotKey specifier is returned. Otherwise the height
	/// is returned.
	/// </param>
	/// <returns>
	/// The number of characters required for the <see cref="TextFormatter.HotKeySpecifier"/>. If the text
	/// direction specified
	/// by <see cref="TextDirection"/> does not match the <paramref name="isWidth"/> parameter, <c>0</c> is returned.
	/// </returns>
	public int GetHotKeySpecifierLength (bool isWidth = true)
	{
		if (isWidth) {
			return TextFormatter.IsHorizontalDirection (TextDirection) &&
			       TextFormatter.Text?.Contains ((char)HotKeySpecifier.Value) == true
				? Math.Max (HotKeySpecifier.GetColumns (), 0) : 0;
		}
		return TextFormatter.IsVerticalDirection (TextDirection) &&
		       TextFormatter.Text?.Contains ((char)HotKeySpecifier.Value) == true
			? Math.Max (HotKeySpecifier.GetColumns (), 0) : 0;
	}

	/// <summary>
	/// Gets the dimensions required for <see cref="Text"/> ignoring a <see cref="TextFormatter.HotKeySpecifier"/>.
	/// </summary>
	/// <returns></returns>
	internal Size GetSizeNeededForTextWithoutHotKey () => new (TextFormatter.Size.Width - GetHotKeySpecifierLength (),
		TextFormatter.Size.Height - GetHotKeySpecifierLength (false));

	/// <summary>
	/// Sets <see cref="TextFormatter"/>.Size to the current <see cref="Bounds"/> size, adjusted for
	/// <see cref="TextFormatter.HotKeySpecifier"/>.
	/// </summary>
	/// <remarks>
	/// Use this API to set <see cref="TextFormatter.Size"/> when the view has changed such that the
	/// size required to fit the text has changed.
	/// changes.
	/// </remarks>
	/// <returns></returns>
	internal void SetTextFormatterSize ()
	{
		if (!IsInitialized) {
			TextFormatter.Size = Size.Empty;
			return;
		}

		if (string.IsNullOrEmpty (TextFormatter.Text)) {
			TextFormatter.Size = Bounds.Size;
			return;
		}

		TextFormatter.Size = new Size (Bounds.Size.Width + GetHotKeySpecifierLength (),
			Bounds.Size.Height + GetHotKeySpecifierLength (false));
	}

	/// <summary>
	/// Gets the Frame dimensions required to fit <see cref="Text"/> within <see cref="Bounds"/> using the text
	/// <see cref="Direction"/> specified by the
	/// <see cref="TextFormatter"/> property and accounting for any <see cref="HotKeySpecifier"/> characters.
	/// </summary>
	/// <returns>The <see cref="Size"/> the <see cref="Frame"/> needs to be set to fit the text.</returns>
	public Size GetAutoSize ()
	{
		var x = 0;
		var y = 0;
		if (IsInitialized) {
			x = Bounds.X;
			y = Bounds.Y;
		}
		var rect = TextFormatter.CalcRect (x, y, TextFormatter.Text, TextFormatter.Direction);
		int newWidth = rect.Size.Width - GetHotKeySpecifierLength () + (Margin == null ? 0 : Margin.Thickness.Horizontal + Border.Thickness.Horizontal + Padding.Thickness.Horizontal);
		int newHeight = rect.Size.Height - GetHotKeySpecifierLength (false) + (Margin == null ? 0 : Margin.Thickness.Vertical + Border.Thickness.Vertical + Padding.Thickness.Vertical);
		return new Size (newWidth, newHeight);
	}

	bool IsValidAutoSize (out Size autoSize)
	{
		var rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);
		autoSize = new Size (rect.Size.Width - GetHotKeySpecifierLength (),
			rect.Size.Height - GetHotKeySpecifierLength (false));
		return !(ValidatePosDim && (!(Width is Dim.DimAbsolute) || !(Height is Dim.DimAbsolute)) ||
			 _frame.Size.Width != rect.Size.Width - GetHotKeySpecifierLength () ||
			 _frame.Size.Height != rect.Size.Height - GetHotKeySpecifierLength (false));
	}

	bool IsValidAutoSizeWidth (Dim width)
	{
		var rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);
		var dimValue = width.Anchor (0);
		return !(ValidatePosDim && !(width is Dim.DimAbsolute) || dimValue != rect.Size.Width - GetHotKeySpecifierLength ());
	}

	bool IsValidAutoSizeHeight (Dim height)
	{
		var rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);
		var dimValue = height.Anchor (0);
		return !(ValidatePosDim && !(height is Dim.DimAbsolute) || dimValue != rect.Size.Height - GetHotKeySpecifierLength (false));
	}
}
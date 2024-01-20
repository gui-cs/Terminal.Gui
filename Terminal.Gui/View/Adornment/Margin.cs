using System;
using System.Linq;

namespace Terminal.Gui;

/// <summary>
/// The Margin for a <see cref="View"/>. 
/// </summary>
/// <remarks>
/// <para>
/// See the <see cref="Adornment"/> class. 
/// </para>
/// </remarks>
public class Margin : Adornment {
	/// <inheritdoc />
	public Margin () { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */ }

	/// <inheritdoc />
	public Margin (View parent) : base (parent) { /* Do nothing; View.CreateAdornment requires a constructor that takes a parent */ }

	/// <summary>
	/// The color scheme for the Margin. If set to <see langword="null"/>, gets the <see cref="Adornment.Parent"/>'s <see cref="View.SuperView"/> scheme.
	/// color scheme.
	/// </summary>
	public override ColorScheme ColorScheme {
		get {
			if (base.ColorScheme != null) {
				return base.ColorScheme;
			}
			return Parent?.SuperView?.ColorScheme ?? Colors.ColorSchemes ["TopLevel"];
		}
		set {
			base.ColorScheme = value;
			Parent?.SetNeedsDisplay ();
		}
	}
}

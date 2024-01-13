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
	
	// TODO: Implement specific override behavior: ColorScheme should match the Parent's SuperView
}

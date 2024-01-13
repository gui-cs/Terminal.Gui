using System;
using System.Linq;

namespace Terminal.Gui;

/// <summary>
/// The Padding for a <see cref="View"/>. 
/// </summary>
/// <remarks>
/// <para>
/// See the <see cref="Adornment"/> class. 
/// </para>
/// </remarks>
public class Padding : Adornment {
	/// <inheritdoc />
	public Padding () { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */ }

	/// <inheritdoc />
	public Padding (View parent) : base (parent) { /* Do nothing; View.CreateAdornment requires a constructor that takes a parent */ }

	// TODO: Implement specific override behavior: ColorScheme should match the Parent
}

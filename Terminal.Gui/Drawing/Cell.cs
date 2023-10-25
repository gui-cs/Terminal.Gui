using System.Collections.Generic;
using System.Text;


namespace Terminal.Gui; 

/// <summary>
/// Represents a single row/column in a Terminal.Gui rendering surface
/// (e.g. <see cref="LineCanvas"/> and <see cref="ConsoleDriver"/>).
/// </summary>
public class Cell {
	/// <summary>
	/// The character to display. If <see cref="Rune"/> is <see langword="null"/>, then <see cref="Rune"/> is ignored.
	/// </summary>
	public Rune Rune { get; set; }

	/// <summary>
	/// The combining mark for <see cref="Rune"/> that when combined makes this Cell a combining sequence that could
	/// not be normalized to a single Rune.
	/// If <see cref="CombiningMark"/> is <see langword="null"/>, then <see cref="CombiningMark"/> is ignored.
	/// </summary>
	/// <remarks>
	/// Only valid in the rare case where <see cref="Rune"/> is a combining sequence that could not be normalized to a single Rune.
	/// </remarks>
	internal Rune CombiningMark { get; set; }

	/// <summary>
	/// The attributes to use when drawing the Glyph.
	/// </summary>
	public Attribute? Attribute { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="T:Terminal.Gui.Cell"/> has
	/// been modified since the last time it was drawn.
	/// </summary>
	public bool IsDirty { get; set; }
}

using System.Collections.Generic;
using System.Text;


namespace Terminal.Gui; 

/// <summary>
/// Represents a single row/column in a Terminal.Gui rendering surface
/// (e.g. <see cref="LineCanvas"/> and <see cref="ConsoleDriver"/>).
/// </summary>
public class Cell {
	/// <summary>
	/// The list of Runes to draw in this cell. If the list is empty, the cell is blank. If the list contains
	/// more than one Rune, the cell is a combining sequence.
	/// (See #2616 - Support combining sequences that don't normalize)
	/// </summary>
	public List<Rune> Runes { get; set; } = new List<Rune> ();

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

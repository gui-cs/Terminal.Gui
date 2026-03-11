namespace Terminal.Gui.Drawing;

/// <summary>
///     Defines a gap in a border line where the line should not be drawn.
///     Used by <see cref="Border"/> to create openings in border lines, such as
///     where a selected tab connects to its content area.
/// </summary>
/// <param name="Position">
///     The position along the border side where the gap starts (0-based,
///     relative to the inner edge of the border).
/// </param>
/// <param name="Length">The length of the gap in columns or rows.</param>
public readonly record struct BorderGap (int Position, int Length);

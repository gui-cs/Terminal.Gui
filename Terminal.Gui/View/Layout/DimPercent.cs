#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a dimension that is a percentage of the width or height of the SuperView.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="percent">The percentage.</param>
/// <param name="usePosition">
///     If <see langword="true"/> the dimension is computed using the View's position (<see cref="View.X"/> or
///     <see cref="View.Y"/>).
///     If <see langword="false"/> the dimension is computed using the View's <see cref="View.ContentSize"/>.
/// </param>
public class DimPercent (int percent, bool usePosition = false) : Dim
{
    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is DimPercent f && f.Percent == Percent && f.UsePosition == UsePosition; }

    /// <inheritdoc/>
    public override int GetHashCode () { return Percent.GetHashCode (); }

    /// <summary>
    ///     Gets the percentage.
    /// </summary>
    public new int Percent { get; } = percent;

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override string ToString () { return $"Percent({Percent},{UsePosition})"; }

    /// <summary>
    ///     Gets whether the dimension is computed using the View's position or ContentSize.
    /// </summary>
    public bool UsePosition { get; } = usePosition;

    internal override int GetAnchor (int size) { return (int)(size * (Percent / 100f)); }

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        return UsePosition ? Math.Max (GetAnchor (superviewContentSize - location), 0) : GetAnchor (superviewContentSize);
    }
}
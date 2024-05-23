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
/// <param name="mode">
///     If <see cref="DimPercentMode.Position"/> the dimension is computed using the View's position (<see cref="View.X"/> or
///     <see cref="View.Y"/>); otherwise, the dimension is computed using the View's <see cref="View.ContentSize"/>.
/// </param>
public class DimPercent (int percent, DimPercentMode mode = DimPercentMode.ContentSize) : Dim
{
    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is DimPercent f && f.Percent == Percent && f.Mode == Mode; }

    /// <inheritdoc/>
    public override int GetHashCode () { return Percent.GetHashCode (); }

    /// <summary>
    ///     Gets the percentage.
    /// </summary>
    public new int Percent { get; } = percent;

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override string ToString () { return $"Percent({Percent},{Mode})"; }

    /// <summary>
    ///     Gets whether the dimension is computed using the View's position or ContentSize.
    /// </summary>
    public DimPercentMode Mode { get; } = mode;

    internal override int GetAnchor (int size) { return (int)(size * (Percent / 100f)); }

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        return Mode == DimPercentMode.Position ? Math.Max (GetAnchor (superviewContentSize - location), 0) : GetAnchor (superviewContentSize);
    }
}
#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a position that is a percentage of the width or height of the SuperView.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
///     </para>
/// </remarks>
/// <param name="percent"></param>
public class PosPercent (int percent) : Pos
{
    /// <summary>
    ///     Gets the percentage of the width or height of the SuperView.
    /// </summary>
    public new int Percent { get; } = percent;

    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is PosPercent i && i.Percent == Percent; }

    /// <inheritdoc/>
    public override int GetHashCode () { return Percent.GetHashCode (); }

    /// <inheritdoc/>
    public override string ToString () { return $"Percent({Percent})"; }

    internal override int GetAnchor (int size) { return (int)(size * (Percent / 100f)); }
}
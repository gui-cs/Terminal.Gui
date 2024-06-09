#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents an absolute position in the layout. This is used to specify a fixed position in the layout.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
///     </para>
/// </remarks>
/// <param name="position"></param>
public class PosAbsolute (int position) : Pos
{
    /// <summary>
    ///     The position of the <see cref="View"/> in the layout.
    /// </summary>
    public int Position { get; } = position;

    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is PosAbsolute abs && abs.Position == Position; }

    /// <inheritdoc/>
    public override int GetHashCode () { return Position.GetHashCode (); }

    /// <inheritdoc/>
    public override string ToString () { return $"Absolute({Position})"; }

    internal override int GetAnchor (int size) { return Position; }
}
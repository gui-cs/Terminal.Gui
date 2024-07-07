#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a position that is computed by executing a function that returns an integer position.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
///     </para>
/// </remarks>
/// <param name="pos">The position.</param>
public class PosFunc (Func<int> pos) : Pos
{
    /// <summary>
    ///     Gets the function that computes the position.
    /// </summary>
    public new Func<int> Func { get; } = pos;

    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is PosFunc f && f.Func () == Func (); }

    /// <inheritdoc/>
    public override int GetHashCode () { return Func.GetHashCode (); }

    /// <inheritdoc/>
    public override string ToString () { return $"PosFunc({Func ()})"; }

    internal override int GetAnchor (int size) { return Func (); }
}
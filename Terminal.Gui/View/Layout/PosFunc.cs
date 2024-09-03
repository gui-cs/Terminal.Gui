#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a position that is computed by executing a function that returns an integer position.
/// </summary>
/// <param name="Fn">The function that computes the position.</param>
public record PosFunc (Func<int> Fn) : Pos
{
    /// <inheritdoc/>
    public override string ToString () { return $"PosFunc({Fn ()})"; }

    internal override int GetAnchor (int size) { return Fn (); }
}
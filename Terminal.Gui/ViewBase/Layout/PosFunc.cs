#nullable enable
namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a position that is computed by executing a function that returns an integer position.
/// </summary>
/// <param name="Fn">The function that computes the dimension. If this function throws <see cref="LayoutException"/>... </param>
public record PosFunc (Func<int> Fn) : Pos
{
    /// <inheritdoc/>
    public override string ToString () { return $"PosFunc({Fn ()})"; }

    internal override int GetAnchor (int size) { return Fn (); }
}
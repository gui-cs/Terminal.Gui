#nullable enable
namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a function <see cref="Dim"/> object that computes the dimension by executing the provided function.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="Fn">The function that computes the dimension. If this function throws <see cref="LayoutException"/>... </param>
public record DimFunc (Func<int> Fn) : Dim
{
    /// <summary>
    ///     Gets the function that computes the dimension.
    /// </summary>
    public Func<int> Fn { get; } = Fn;

    /// <inheritdoc/>
    public override string ToString () { return $"DimFunc({Fn ()})"; }

    internal override int GetAnchor (int size) { return Fn (); }
}
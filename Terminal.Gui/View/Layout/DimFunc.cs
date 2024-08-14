#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a function <see cref="Gui.Dim"/> object that computes the dimension by executing the provided function.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Gui.Dim"/> class to create <see cref="Gui.Dim"/> objects instead.
/// </remarks>
/// <param name="Dim"></param>
public record DimFunc (Func<int> Dim) : Dim
{
    /// <summary>
    ///     Gets the function that computes the dimension.
    /// </summary>
    public new Func<int> Func { get; } = Dim;

    /// <inheritdoc/>
    public override int GetHashCode () { return Func.GetHashCode (); }

    /// <inheritdoc/>
    public override string ToString () { return $"DimFunc({Func ()})"; }

    internal override int GetAnchor (int size) { return Func (); }
}
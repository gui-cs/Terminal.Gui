#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a function <see cref="Dim"/> object that computes the dimension by executing the provided function.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="dim"></param>
public record DimFunc (Func<int> dim) : Dim
{
    /// <summary>
    ///     Gets the function that computes the dimension.
    /// </summary>
    public new Func<int> Func { get; } = dim;

    /// <inheritdoc/>
    public override int GetHashCode () { return Func.GetHashCode (); }

    /// <inheritdoc/>
    public override string ToString () { return $"DimFunc({Func ()})"; }

    internal override int GetAnchor (int size) { return Func (); }
}
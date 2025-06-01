#nullable enable
namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a dimension that fills the dimension, leaving the specified margin.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="Margin">The margin to not fill.</param>
public record DimFill (Dim Margin) : Dim
{
    /// <inheritdoc/>
    public override string ToString () { return $"Fill({Margin})"; }

    internal override int GetAnchor (int size) { return size - Margin.GetAnchor(0); }
}
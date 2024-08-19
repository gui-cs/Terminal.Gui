#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a dimension that is a fixed size.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
///     </para>
/// </remarks>
/// <param name="Size"></param>
public record DimAbsolute (int Size) : Dim
{
    /// <summary>
    ///     Gets the size of the dimension.
    /// </summary>
    public int Size { get; } = Size;

    /// <inheritdoc/>
    public override string ToString () { return $"Absolute({Size})"; }

    internal override int GetAnchor (int size) { return Size; }

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        return Math.Max (GetAnchor (0), 0);
    }
}
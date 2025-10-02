#nullable enable
namespace Terminal.Gui.ViewBase;

/// <summary>
///     Represents a position anchored to the end (right side or bottom).
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
///     </para>
/// </remarks>
public record PosAnchorEnd : Pos
{
    /// <summary>
    ///     Gets the offset of the position from the right/bottom.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    ///     Constructs a new position anchored to the end (right side or bottom) of the SuperView,
    ///     minus the respective dimension of the View. This is equivalent to using <see cref="PosAnchorEnd(int)"/>,
    ///     with an offset equivalent to the View's respective dimension.
    /// </summary>
    public PosAnchorEnd () { UseDimForOffset = true; }

    /// <summary>
    ///     Constructs a new position anchored to the end (right side or bottom) of the SuperView,
    /// </summary>
    /// <param name="offset"></param>
    public PosAnchorEnd (int offset) { Offset = offset; }

    /// <summary>
    ///     If true, the offset is the width of the view, if false, the offset is the offset value.
    /// </summary>
    public bool UseDimForOffset { get; }

    /// <inheritdoc/>
    public override string ToString () { return UseDimForOffset ? "AnchorEnd" : $"AnchorEnd({Offset})"; }

    internal override int GetAnchor (int size)
    {
        if (UseDimForOffset)
        {
            return size;
        }

        return size - Offset;
    }

    internal override int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension)
    {
        int newLocation = GetAnchor (superviewDimension);

        if (UseDimForOffset)
        {
            newLocation -= dim.GetAnchor (superviewDimension);
        }

        return newLocation;
    }
}
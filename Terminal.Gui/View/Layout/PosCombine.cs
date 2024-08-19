#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a position that is a combination of two other positions.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
///     </para>
/// </remarks>
/// <param name="Add">
///     Indicates whether the two positions are added or subtracted.
/// </param>
/// <param name="Left">The left position.</param>
/// <param name="Right">The right position.</param>
public record PosCombine (AddOrSubtract Add, Pos Left, Pos Right) : Pos
{
    /// <summary>
    ///     Gets whether the two positions are added or subtracted.
    /// </summary>
    public AddOrSubtract Add { get; } = Add;

    /// <summary>
    ///     Gets the left position.
    /// </summary>
    public new Pos Left { get; } = Left;

    /// <summary>
    ///     Gets the right position.
    /// </summary>
    public new Pos Right { get; } = Right;

    /// <inheritdoc/>
    public override string ToString () { return $"Combine({Left}{(Add == AddOrSubtract.Add ? '+' : '-')}{Right})"; }

    internal override int GetAnchor (int size)
    {
        if (Add == AddOrSubtract.Add)
        {
            return Left.GetAnchor (size) + Right.GetAnchor (size);
        }

        return Left.GetAnchor (size) - Right.GetAnchor (size);
    }

    internal override int Calculate (int superviewDimension, Dim dim, View us, Dimension dimension)
    {
        if (Add == AddOrSubtract.Add)
        {
            return Left.Calculate (superviewDimension, dim, us, dimension) + Right.Calculate (superviewDimension, dim, us, dimension);
        }

        return Left.Calculate (superviewDimension, dim, us, dimension) - Right.Calculate (superviewDimension, dim, us, dimension);
    }

    internal override bool ReferencesOtherViews ()
    {
        if (Left.ReferencesOtherViews ())
        {
            return true;
        }

        if (Right.ReferencesOtherViews ())
        {
            return true;
        }

        return false;
    }
}
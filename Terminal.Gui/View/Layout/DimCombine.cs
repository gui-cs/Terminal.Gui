#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a dimension that is a combination of two other dimensions.
/// </summary>
/// <param name="add">
///     Indicates whether the two dimensions are added or subtracted.
/// </param>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
/// <param name="left">The left dimension.</param>
/// <param name="right">The right dimension.</param>
public class DimCombine (AddOrSubtract add, Dim? left, Dim? right) : Dim
{
    /// <summary>
    ///     Gets whether the two dimensions are added or subtracted.
    /// </summary>
    public AddOrSubtract Add { get; } = add;

    /// <summary>
    ///     Gets the left dimension.
    /// </summary>
    public Dim? Left { get; } = left;

    /// <summary>
    ///     Gets the right dimension.
    /// </summary>
    public Dim? Right { get; } = right;

    /// <inheritdoc/>
    public override string ToString () { return $"Combine({Left}{(Add == AddOrSubtract.Add ? '+' : '-')}{Right})"; }

    internal override int GetAnchor (int size)
    {
        if (Add == AddOrSubtract.Add)
        {
            return Left!.GetAnchor (size) + Right!.GetAnchor (size);
        }

        return Left!.GetAnchor (size) - Right!.GetAnchor (size);
    }

    internal override int Calculate (int location, int superviewContentSize, View us, Dimension dimension)
    {
        int newDimension;

        if (Add == AddOrSubtract.Add)
        {
            newDimension = Left!.Calculate (location, superviewContentSize, us, dimension) + Right!.Calculate (location, superviewContentSize, us, dimension);
        }
        else
        {
            newDimension = Math.Max (
                                     0,
                                     Left!.Calculate (location, superviewContentSize, us, dimension)
                                     - Right!.Calculate (location, superviewContentSize, us, dimension));
        }

        return newDimension;
    }

    /// <summary>
    ///     Diagnostics API to determine if this Dim object references other views.
    /// </summary>
    /// <returns></returns>
    internal override bool ReferencesOtherViews ()
    {
        if (Left!.ReferencesOtherViews ())
        {
            return true;
        }

        if (Right!.ReferencesOtherViews ())
        {
            return true;
        }

        return false;
    }
}

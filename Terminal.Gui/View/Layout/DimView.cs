#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a dimension that tracks the Height or Width of the specified View.
/// </summary>
/// <remarks>
///     This is a low-level API that is typically used internally by the layout system. Use the various static
///     methods on the <see cref="Dim"/> class to create <see cref="Dim"/> objects instead.
/// </remarks>
public class DimView : Dim
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DimView"/> class.
    /// </summary>
    /// <param name="view">The view the dimension is anchored to.</param>
    /// <param name="dimension">Indicates which dimension is tracked.</param>
    public DimView (View? view, Dimension dimension)
    {
        Target = view;
        Dimension = dimension;
    }

    /// <summary>
    ///     Gets the indicated dimension of the View.
    /// </summary>
    public Dimension Dimension { get; }

    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is DimView abs && abs.Target == Target && abs.Dimension == Dimension; }

    /// <inheritdoc/>
    public override int GetHashCode () { return Target!.GetHashCode (); }

    /// <summary>
    ///     Gets the View the dimension is anchored to.
    /// </summary>
    public View? Target { get; init; }

    /// <inheritdoc/>
    public override string ToString ()
    {
        if (Target == null)
        {
            throw new NullReferenceException ();
        }

        return $"View({Dimension},{Target})";
    }

    internal override int GetAnchor (int size)
    {
        return Dimension switch
               {
                   Dimension.Height => Target!.Frame.Height,
                   Dimension.Width => Target!.Frame.Width,
                   _ => 0
               };
    }

    internal override bool ReferencesOtherViews () { return true; }
}
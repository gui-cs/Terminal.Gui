#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Represents a position that is anchored to the side of another view.
/// </summary>
/// <remarks>
///     <para>
///         This is a low-level API that is typically used internally by the layout system. Use the various static
///         methods on the <see cref="Pos"/> class to create <see cref="Pos"/> objects instead.
///     </para>
/// </remarks>
/// <param name="view">The View the position is anchored to.</param>
/// <param name="side">The side of the View the position is anchored to.</param>
public class PosView (View? view, Side side) : Pos
{
    /// <summary>
    ///     Gets the View the position is anchored to.
    /// </summary>
    public View? Target { get; } = view;

    /// <summary>
    ///     Gets the side of the View the position is anchored to.
    /// </summary>
    public Side Side { get; } = side;

    /// <inheritdoc/>
    public override bool Equals (object? other) { return other is PosView abs && abs.Target == Target && abs.Side == Side; }

    /// <inheritdoc/>
    public override int GetHashCode () { return Target!.GetHashCode (); }

    /// <inheritdoc/>
    public override string ToString ()
    {
        string sideString = Side.ToString ();

        if (Target == null)
        {
            throw new NullReferenceException (nameof (Target));
        }

        return $"View(Side={sideString},Target={Target})";
    }

    internal override int GetAnchor (int size)
    {
        return Side switch
               {
                   Side.Left => Target!.Frame.X,
                   Side.Top => Target!.Frame.Y,
                   Side.Right => Target!.Frame.Right,
                   Side.Bottom => Target!.Frame.Bottom,
                   _ => 0
               };
    }

    internal override bool ReferencesOtherViews () { return true; }
}
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
/// <param name="View">The View the position is anchored to.</param>
/// <param name="Side">The side of the View the position is anchored to.</param>
public record PosView (View? View, Side Side) : Pos
{
    /// <summary>
    ///     Gets the View the position is anchored to.
    /// </summary>
    public View? Target { get; } = View;

    /// <summary>
    ///     Gets the side of the View the position is anchored to.
    /// </summary>
    public Side Side { get; } = Side;

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
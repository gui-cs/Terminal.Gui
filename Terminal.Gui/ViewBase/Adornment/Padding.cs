namespace Terminal.Gui.ViewBase;

/// <summary>The Padding for a <see cref="View"/>. Accessed via <see cref="View.Padding"/></summary>
/// <remarks>
///     <para>See the <see cref="Adornment"/> class.</para>
/// </remarks>
public class Padding : Adornment
{
    /// <inheritdoc/>
    public Padding ()
    {
        /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <inheritdoc/>
    public Padding (View parent) : base (parent)
    {
        CanFocus = true;
        TabStop = TabBehavior.NoStop;
    }

    /// <summary>Called when a mouse event occurs within the Padding.</summary>
    /// <remarks>
    ///     <para>
    ///         The coordinates are relative to <see cref="View.Viewport"/>.
    ///     </para>
    ///     <para>
    ///         A mouse click on the Padding will cause the Parent to focus.
    ///     </para>
    /// </remarks>
    /// <param name="mouse"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (Parent is null)
        {
            return false;
        }

        if (mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked))
        {
            if (Parent.CanFocus && !Parent.HasFocus)
            {
                Parent.SetFocus ();
                Parent.SetNeedsDraw ();
                return mouse.Handled = true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Gets all SubViews of this Padding, optionally including SubViews of the Padding's Parent.
    /// </summary>
    /// <param name="includeMargin">
    ///     Ignored.
    /// </param>
    /// <param name="includeBorder">
    ///     Ignored.
    /// </param>
    /// <param name="includePadding">
    ///     If <see langword="true"/>, includes SubViews from <see cref="Padding"/>. If <see langword="false"/> (default),
    ///     returns only the direct SubViews
    ///     of this Padding.
    /// </param>
    /// <returns>
    ///     A read-only collection containing all SubViews. If <paramref name="includePadding"/> is
    ///     <see langword="true"/>, the collection includes SubViews from this Padding's direct SubViews as well
    ///     as SubViews from the Padding's Parent.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method returns a snapshot of the SubViews at the time of the call. The collection is
    ///         safe to iterate even if SubViews are added or removed during iteration.
    ///     </para>
    ///     <para>
    ///         The order of SubViews in the returned collection is:
    ///         <list type="number">
    ///             <item>Direct SubViews of this Padding</item>
    ///             <item>SubViews of Parent (if <paramref name="includePadding"/> is <see langword="true"/>)</item>
    ///         </list>
    ///     </para>
    /// </remarks>
    public override IReadOnlyCollection<View> GetSubViews (bool includeMargin = false, bool includeBorder = false, bool includePadding = false)
    {
        List<View> subViewsOfThisAdornment = new (base.GetSubViews (false, false, includePadding));

        if (includePadding && Parent is { })
        {
            // Include SubViews from Parent. Since we are a Padding of Parent do not
            // request Adornments again to avoid infinite recursion.
            subViewsOfThisAdornment.AddRange (Parent.GetSubViews (false, false, false));
        }

        return subViewsOfThisAdornment;
    }
}

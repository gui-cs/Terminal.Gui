#nullable enable

namespace Terminal.Gui.ViewBase;

/// <summary>The Padding for a <see cref="View"/>. Accessed via <see cref="View.Padding"/></summary>
/// <remarks>
///     <para>See the <see cref="Adornment"/> class.</para>
/// </remarks>
public class Padding : Adornment
{
    /// <inheritdoc/>
    public Padding ()
    { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <inheritdoc/>
    public Padding (View parent) : base (parent)
    {
        /* Do nothing; View.CreateAdornment requires a constructor that takes a parent */
    }


    /// <summary>Called when a mouse event occurs within the Padding.</summary>
    /// <remarks>
    /// <para>
    /// The coordinates are relative to <see cref="View.Viewport"/>.
    /// </para>
    /// <para>
    /// A mouse click on the Padding will cause the Parent to focus.
    /// </para>
    /// </remarks>
    /// <param name="mouseEvent"></param>
    /// <returns><see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        if (Parent is null)
        {
            return false;
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked))
        {
            if (Parent.CanFocus && !Parent.HasFocus)
            {
                Parent.SetFocus ();
                Parent.SetNeedsDraw ();
                return mouseEvent.Handled = true;
            }
        }

        return false;
    }

}

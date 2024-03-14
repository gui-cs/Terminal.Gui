namespace Terminal.Gui;

/// <summary>Draws a single line using the <see cref="LineStyle"/> specified by <see cref="View.BorderStyle"/>.</summary>
public class Line : View
{
    /// <summary>Constructs a Line object.</summary>
    public Line ()
    {
        BorderStyle = LineStyle.Single;
        Border.Thickness = new Thickness (0);
    }

    /// <summary>
    ///     The direction of the line.  If you change this you will need to manually update the Width/Height of the
    ///     control to cover a relevant area based on the new direction.
    /// </summary>
    public Orientation Orientation { get; set; }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle contentArea)
    {
        LineCanvas lc = LineCanvas;

        if (SuperView is Adornment adornment)
        {
            lc = adornment.Parent.LineCanvas;
        }
        lc.AddLine (
                    BoundsToScreen (contentArea).Location,
                    Orientation == Orientation.Horizontal ? Frame.Width : Frame.Height,
                    Orientation,
                    BorderStyle
                   );
    }
}

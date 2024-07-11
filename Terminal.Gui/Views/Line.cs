namespace Terminal.Gui;

/// <summary>Draws a single line using the <see cref="LineStyle"/> specified by <see cref="View.BorderStyle"/>.</summary>
public class Line : View
{
    /// <summary>Constructs a Line object.</summary>
    public Line ()
    {
        BorderStyle = LineStyle.Single;
        Border.Thickness = new Thickness (0);
        SuperViewRendersLineCanvas = true;
    }

    private Orientation _orientation;

    /// <summary>
    ///     The direction of the line.  If you change this you will need to manually update the Width/Height of the
    ///     control to cover a relevant area based on the new direction.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;

            switch (Orientation)
            {
                case Orientation.Horizontal:
                    Height = 1;

                    break;
                case Orientation.Vertical:
                    Width = 1;

                    break;

            }
        }
    }

    /// <inheritdoc/>
    public override void SetBorderStyle (LineStyle value)
    {
        // The default changes the thickness. We don't want that. We just set the style.
        Border.LineStyle = value;
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        if (viewport.Width == 0 || viewport.Height == 0)
        {
            return;
        }

        LineCanvas lc = LineCanvas;

        if (SuperViewRendersLineCanvas)
        {
            lc = SuperView.LineCanvas;
        }

        if (SuperView is Adornment adornment)
        {
            lc = adornment.Parent.LineCanvas;
        }

        Point pos = ViewportToScreen (viewport).Location;
        int length = Orientation == Orientation.Horizontal ? Frame.Width : Frame.Height;

        if (SuperViewRendersLineCanvas && Orientation == Orientation.Horizontal)
        {
            pos.Offset (-SuperView.Border?.Thickness.Left ?? 0, 0);
            length += SuperView.Border?.Thickness.Horizontal ?? 0;
        }

        if (SuperViewRendersLineCanvas && Orientation == Orientation.Vertical)
        {
            pos.Offset (0, -SuperView.Border?.Thickness.Top ?? 0);
            length += SuperView.Border?.Thickness.Vertical ?? 0;
        }
        lc.AddLine (
                    pos,
                    length,
                    Orientation,
                    Border.LineStyle,
                    GetNormalColor()
                   );
    }
}

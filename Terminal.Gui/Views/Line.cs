namespace Terminal.Gui;

/// <summary>Draws a single line using the <see cref="LineStyle"/> specified by <see cref="View.BorderStyle"/>.</summary>
public class Line : View
{
    /// <summary>Constructs a Line object.</summary>
    public Line ()
    {
    }

    private LineStyle _lineStyle = LineStyle.Single;

    public LineStyle LineStyle
    {
        get => _lineStyle;
        set
        {
            if (_lineStyle == value)
            {
                return;
            }

            _lineStyle = value;
        }
    }

    /// <summary>
    ///     The direction of the line.  If you change this you will need to manually update the Width/Height of the
    ///     control to cover a relevant area based on the new direction.
    /// </summary>
    public Orientation Orientation { get; set; }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        LineCanvas lc = LineCanvas;

        if (SuperView is Adornment adornment)
        {
            lc = adornment.Parent.LineCanvas;
        }

        lc.AddLine (
                    ViewportToScreen (viewport).Location,
                    Orientation == Orientation.Horizontal ? Frame.Width : Frame.Height,
                    Orientation,
                    LineStyle,
                    ColorScheme.Normal
                   );
    }
}

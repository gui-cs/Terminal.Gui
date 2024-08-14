namespace Terminal.Gui;

/// <summary>Draws a single line using the <see cref="LineStyle"/> specified by <see cref="View.BorderStyle"/>.</summary>
public class Line : View, IOrientation
{
    private readonly OrientationHelper _orientationHelper;

    /// <summary>Constructs a Line object.</summary>
    public Line ()
    {
        BorderStyle = LineStyle.Single;
        Border.Thickness = new Thickness (0);
        SuperViewRendersLineCanvas = true;

        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Horizontal;
        _orientationHelper.OrientationChanging += (sender, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (sender, e) => OrientationChanged?.Invoke (this, e);
    }


    #region IOrientation members
    /// <summary>
    ///     The direction of the line.  If you change this you will need to manually update the Width/Height of the
    ///     control to cover a relevant area based on the new direction.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientationHelper.Orientation;
        set => _orientationHelper.Orientation = value;
    }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>> OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>> OrientationChanged;

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation)
    {

        switch (newOrientation)
        {
            case Orientation.Horizontal:
                Height = 1;

                break;
            case Orientation.Vertical:
                Width = 1;

                break;

        }
    }
    #endregion

    /// <inheritdoc/>
    public override void SetBorderStyle (LineStyle value)
    {
        // The default changes the thickness. We don't want that. We just set the style.
        Border.LineStyle = value;
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
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
            pos.Offset (-SuperView.Border.Thickness.Left, 0);
            length += SuperView.Border.Thickness.Horizontal;
        }

        if (SuperViewRendersLineCanvas && Orientation == Orientation.Vertical)
        {
            pos.Offset (0, -SuperView.Border.Thickness.Top);
            length += SuperView.Border.Thickness.Vertical;
        }
        lc.AddLine (
                    pos,
                    length,
                    Orientation,
                    BorderStyle
                   );
    }
}

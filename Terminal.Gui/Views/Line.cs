namespace Terminal.Gui;

/// <summary>
///     Draws a single line using the <see cref="LineStyle"/> specified by <see cref="View.BorderStyle"/>.
/// </summary>
/// <remarks>
/// </remarks>
public class Line : View, IOrientation
{
    private readonly OrientationHelper _orientationHelper;

    /// <summary>Constructs a Line object.</summary>
    public Line ()
    {
        CanFocus = false;

        base.SuperViewRendersLineCanvas = true;

        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Horizontal;
        OnOrientationChanged(Orientation);
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

#pragma warning disable CS0067 // The event is never used
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>> OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>> OrientationChanged;
#pragma warning restore CS0067 // The event is never used

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation)
    {

        switch (newOrientation)
        {
            case Orientation.Horizontal:
                Height = 1;
                Width = Dim.Fill ();

                break;
            case Orientation.Vertical:
                Width = 1;
                Height = Dim.Fill ();

                break;

        }
    }
    #endregion

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        Point pos = ViewportToScreen (Viewport).Location;
        int length = Orientation == Orientation.Horizontal ? Frame.Width : Frame.Height;

        LineCanvas?.AddLine (
                    pos,
                    length,
                    Orientation,
                    BorderStyle
                   );

        //SuperView?.SetNeedsDraw ();
        return true;
    }
}

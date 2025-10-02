
namespace Terminal.Gui.Views;

/// <summary>
///     Draws a single line using the <see cref="LineStyle"/> specified by <see cref="Line.Style"/>.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Line"/> is a <see cref="View"/> that renders a single horizontal or vertical line
///         using the <see cref="LineCanvas"/> system. Unlike <see cref="LineView"/>, which directly renders
///         runes, <see cref="Line"/> integrates with the LineCanvas to enable proper box-drawing character
///         selection and line intersection handling.
///     </para>
///     <para>
///         The line's appearance is controlled by the <see cref="Style"/> property, which supports
///         various line styles including Single, Double, Heavy, Rounded, Dashed, and Dotted.
///     </para>
///     <para>
///         When multiple <see cref="Line"/> instances or other LineCanvas-aware views (like <see cref="Border"/>)
///         intersect, the LineCanvas automatically selects the appropriate box-drawing characters for corners,
///         T-junctions, and crosses.
///     </para>
///     <para>
///         <see cref="Line"/> sets <see cref="View.SuperViewRendersLineCanvas"/> to <see langword="true"/>,
///         meaning its parent view is responsible for rendering the line. This allows for proper intersection
///         handling when multiple views contribute lines to the same canvas.
///     </para>
/// </remarks>
/// <example>
///     <code>
///         // Create a horizontal line
///         var hLine = new Line { Y = 5 };
///         
///         // Create a vertical line
///         var vLine = new Line { X = 10, Orientation = Orientation.Vertical };
///         
///         // Create a double-line style horizontal line
///         var doubleLine = new Line { Y = 10, Style = LineStyle.Double };
///     </code>
/// </example>
public class Line : View, IOrientation
{
    private readonly OrientationHelper _orientationHelper;
    private LineStyle _style = LineStyle.Single;
    private Dim? _userSetWidth;
    private Dim? _userSetHeight;

    /// <summary>
    ///     Constructs a new instance of the <see cref="Line"/> class with horizontal orientation.
    /// </summary>
    /// <remarks>
    ///     By default, a horizontal line fills the available width and has a height of 1.
    ///     The line style defaults to <see cref="LineStyle.Single"/>.
    /// </remarks>
    public Line ()
    {
        CanFocus = false;
        SuperViewRendersLineCanvas = true;

        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Horizontal;
        OnOrientationChanged(Orientation);
    }

    /// <summary>
    ///     Sets the width of the line. Once set, it will not be changed by <see cref="Orientation"/> changes.
    /// </summary>
    /// <param name="width">The width dimension.</param>
    /// <returns>The Line instance for fluent API.</returns>
    public new Line SetWidth (Dim width)
    {
        _userSetWidth = width;
        base.Width = width;
        return this;
    }

    /// <summary>
    ///     Sets the height of the line. Once set, it will not be changed by <see cref="Orientation"/> changes.
    /// </summary>
    /// <param name="height">The height dimension.</param>
    /// <returns>The Line instance for fluent API.</returns>
    public new Line SetHeight (Dim height)
    {
        _userSetHeight = height;
        base.Height = height;
        return this;
    }

    /// <summary>
    ///     Gets or sets the style of the line. This controls the visual appearance of the line.
    /// </summary>
    /// <remarks>
    ///     Supports various line styles including Single, Double, Heavy, Rounded, Dashed, and Dotted.
    ///     Note: This is separate from <see cref="View.BorderStyle"/> to avoid conflicts with the View's Border.
    /// </remarks>
    public LineStyle Style
    {
        get => _style;
        set
        {
            if (_style != value)
            {
                _style = value;
                SetNeedsDraw();
            }
        }
    }

    #region IOrientation members
    /// <summary>
    ///     The direction of the line. Changing this property adjusts the Width and Height
    ///     to appropriate values for the new orientation, unless they have been explicitly set using
    ///     <see cref="SetWidth"/> or <see cref="SetHeight"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set to <see cref="Orientation.Horizontal"/>, Width is set to <see cref="Dim.Fill()"/> and Height to 1,
    ///         unless they have been explicitly set using SetWidth or SetHeight.
    ///     </para>
    ///     <para>
    ///         When set to <see cref="Orientation.Vertical"/>, Width is set to 1 and Height to <see cref="Dim.Fill()"/>,
    ///         unless they have been explicitly set using SetWidth or SetHeight.
    ///     </para>
    ///     <para>
    ///         To set Width or Height before Orientation and have them preserved, use <see cref="SetWidth"/> or <see cref="SetHeight"/>.
    ///     </para>
    /// </remarks>
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

    /// <summary>
    ///     Called when <see cref="Orientation"/> has changed. Updates the Width and Height based on the new orientation,
    ///     but only if they haven't been explicitly set using <see cref="SetWidth"/> or <see cref="SetHeight"/>.
    /// </summary>
    /// <param name="newOrientation">The new orientation value.</param>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        switch (newOrientation)
        {
            case Orientation.Horizontal:
                if (_userSetHeight == null)
                {
                    Height = 1;
                }
                if (_userSetWidth == null)
                {
                    Width = Dim.Fill ();
                }
                break;
            case Orientation.Vertical:
                if (_userSetWidth == null)
                {
                    Width = 1;
                }
                if (_userSetHeight == null)
                {
                    Height = Dim.Fill ();
                }
                break;
        }
    }
    #endregion

    /// <inheritdoc/>
    /// <remarks>
    ///     This method adds the line to the parent view's <see cref="View.LineCanvas"/> for rendering.
    ///     The actual rendering is performed by the parent view through <see cref="View.RenderLineCanvas"/>.
    /// </remarks>
    protected override bool OnDrawingContent ()
    {
        Point pos = ViewportToScreen (Viewport).Location;
        int length = Orientation == Orientation.Horizontal ? Frame.Width : Frame.Height;

        SuperView?.LineCanvas?.AddLine (
                    pos,
                    length,
                    Orientation,
                    Style
                   );

        return true;
    }
}

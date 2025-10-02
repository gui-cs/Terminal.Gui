
namespace Terminal.Gui.Views;

/// <summary>
///     Draws a single line using the <see cref="LineStyle"/> specified by <see cref="View.BorderStyle"/>.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Line"/> is a <see cref="View"/> that renders a single horizontal or vertical line
///         using the <see cref="Border"/> adornment. Unlike <see cref="LineView"/>, which directly renders
///         runes, <see cref="Line"/> uses the Border's LineCanvas integration to enable proper box-drawing character
///         selection and line intersection handling.
///     </para>
///     <para>
///         The line's appearance is controlled by the <see cref="View.BorderStyle"/> property, which supports
///         various line styles including Single, Double, Heavy, Rounded, Dashed, and Dotted.
///     </para>
///     <para>
///         When multiple <see cref="Line"/> instances or other Border-based views intersect, the LineCanvas 
///         automatically selects the appropriate box-drawing characters for corners, T-junctions, and crosses.
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
///         var doubleLine = new Line { Y = 10, BorderStyle = LineStyle.Double };
///     </code>
/// </example>
public class Line : View, IOrientation
{
    private readonly OrientationHelper _orientationHelper;

    /// <summary>
    ///     Constructs a new instance of the <see cref="Line"/> class with horizontal orientation.
    /// </summary>
    /// <remarks>
    ///     By default, a horizontal line fills the available width and has a height of 1.
    ///     The line uses the <see cref="View.BorderStyle"/> for rendering (defaults to <see cref="LineStyle.Single"/>).
    /// </remarks>
    public Line ()
    {
        CanFocus = false;
        BorderStyle = LineStyle.Single;

        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Horizontal;
        OnOrientationChanged(Orientation);
    }


    #region IOrientation members
    /// <summary>
    ///     The direction of the line. Changing this property automatically adjusts the Width, Height, and Border.Thickness
    ///     to appropriate values for the new orientation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When set to <see cref="Orientation.Horizontal"/>, Width is set to <see cref="Dim.Fill()"/> and Height to 1,
    ///         with Border.Thickness set to (0, 1, 0, 0) to draw only the top border.
    ///     </para>
    ///     <para>
    ///         When set to <see cref="Orientation.Vertical"/>, Width is set to 1 and Height to <see cref="Dim.Fill()"/>,
    ///         with Border.Thickness set to (1, 0, 0, 0) to draw only the left border.
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
    ///     Called when <see cref="Orientation"/> has changed. Updates the Width, Height, and Border.Thickness 
    ///     based on the new orientation.
    /// </summary>
    /// <param name="newOrientation">The new orientation value.</param>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        switch (newOrientation)
        {
            case Orientation.Horizontal:
                Height = 1;
                Width = Dim.Fill ();
                Border.Thickness = new Thickness (0, 1, 0, 0);

                break;
            case Orientation.Vertical:
                Width = 1;
                Height = Dim.Fill ();
                Border.Thickness = new Thickness (1, 0, 0, 0);

                break;
        }
    }
    #endregion
}

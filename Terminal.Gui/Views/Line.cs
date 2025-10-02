
#nullable enable

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
///         Use the <see cref="Length"/> property to control the extent of the line regardless of its
///         <see cref="Orientation"/>. For horizontal lines, Length controls Width; for vertical lines,
///         it controls Height. The perpendicular dimension is always 1.
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
///         // Create a vertical line with specific length
///         var vLine = new Line { X = 10, Orientation = Orientation.Vertical, Length = 15 };
///         
///         // Create a double-line style horizontal line
///         var doubleLine = new Line { Y = 10, Style = LineStyle.Double };
///     </code>
/// </example>
public class Line : View, IOrientation
{
    private readonly OrientationHelper _orientationHelper;
    private LineStyle _style = LineStyle.Single;

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
        base.SuperViewRendersLineCanvas = true;

        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Horizontal;
        OnOrientationChanged (Orientation);
    }

    /// <summary>
    ///     Gets or sets the length of the line along its orientation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         For a horizontal line, this sets/gets the Width.
    ///         For a vertical line, this sets/gets the Height.
    ///     </para>
    ///     <para>
    ///         This property provides a cleaner API for controlling the line's extent
    ///         without needing to know whether to use Width or Height.
    ///     </para>
    /// </remarks>
    public Dim Length
    {
        get => Orientation == Orientation.Horizontal ? Width : Height;
        set
        {
            if (Orientation == Orientation.Horizontal)
            {
                Width = value;
            }
            else
            {
                Height = value;
            }
        }
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
                SetNeedsDraw ();
            }
        }
    }

    #region IOrientation members
    /// <summary>
    ///     The direction of the line. Changing this property swaps Width and Height to preserve
    ///     the line's dimensions in the new orientation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When orientation changes, the Width and Height values are swapped so that the line
    ///         maintains its visual dimensions. For example, a horizontal line with Width=10, Height=1
    ///         becomes Width=1, Height=10 when changed to vertical.
    ///     </para>
    ///     <para>
    ///         Use the <see cref="Length"/> property to control the extent of the line regardless of orientation.
    ///     </para>
    /// </remarks>
    public Orientation Orientation
    {
        get => _orientationHelper.Orientation;
        set => _orientationHelper.Orientation = value;
    }

#pragma warning disable CS0067 // The event is never used
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
#pragma warning restore CS0067 // The event is never used

    /// <summary>
    ///     Called when <see cref="Orientation"/> has changed. Swaps Width and Height to preserve
    ///     the line's dimensions in the new orientation.
    /// </summary>
    /// <param name="newOrientation">The new orientation value.</param>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        // Get current dimensions before we change anything
        Dim currentWidth = Width;
        Dim currentHeight = Height;
        
        // Check if we're in a default state (just constructed)
        bool isDefaultWidth = currentWidth.ToString () == "Absolute(0)";
        bool isDefaultHeight = currentHeight.ToString () == "Absolute(0)";
        
        switch (newOrientation)
        {
            case Orientation.Horizontal:
                // Set perpendicular dimension first
                Height = 1;
                
                // Set the length dimension
                if (isDefaultWidth && isDefaultHeight)
                {
                    // Just constructed, use default
                    Width = Dim.Fill ();
                }
                else if (currentHeight != 1 && !isDefaultHeight)
                {
                    // Swap - height becomes width (preserve the "length")
                    Width = currentHeight;
                }
                else if (currentWidth == 1)
                {
                    // Width was perpendicular, default to Fill
                    Width = Dim.Fill ();
                }
                // else keep current width
                break;
                
            case Orientation.Vertical:
                // Set perpendicular dimension first
                Width = 1;
                
                // Set the length dimension
                if (isDefaultWidth && isDefaultHeight)
                {
                    // Just constructed, use default
                    Height = Dim.Fill ();
                }
                else if (currentWidth != 1 && !isDefaultWidth)
                {
                    // Swap - width becomes height (preserve the "length")
                    Height = currentWidth;
                }
                else if (currentHeight == 1)
                {
                    // Height was perpendicular, default to Fill
                    Height = Dim.Fill ();
                }
                // else keep current height
                break;
        }
    }
    #endregion

    /// <inheritdoc/>
    /// <remarks>
    ///     This method adds the line to the LineCanvas for rendering.
    ///     The actual rendering is performed by the parent view through <see cref="View.RenderLineCanvas"/>.
    /// </remarks>
    protected override bool OnDrawingContent ()
    {
        Point pos = ViewportToScreen (Viewport).Location;
        int length = Orientation == Orientation.Horizontal ? Frame.Width : Frame.Height;

        LineCanvas.AddLine (
                    pos,
                    length,
                    Orientation,
                    Style
                   );

        return true;
    }
}

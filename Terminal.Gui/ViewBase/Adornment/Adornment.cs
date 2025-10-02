#nullable enable

namespace Terminal.Gui.ViewBase;


/// <summary>
///     Adornments are a special form of <see cref="View"/> that appear outside the <see cref="View.Viewport"/>:
///     <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>. They are defined using the
///     <see cref="Thickness"/> class, which specifies the thickness of the sides of a rectangle.
/// </summary>
/// <remarsk>
///     <para>
///         Each of <see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/> has slightly different
///         behavior relative to <see cref="Scheme"/>, <see cref="View.SetFocus()"/>, keyboard input, and
///         mouse input. Each can be customized by manipulating their SubViews.
///     </para>
/// </remarsk>
public class Adornment : View, IDesignable
{
    /// <inheritdoc/>
    public Adornment ()
    {
        /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <summary>Constructs a new adornment for the view specified by <paramref name="parent"/>.</summary>
    /// <param name="parent"></param>
    public Adornment (View parent)
    {
        // By default, Adornments can't get focus; has to be enabled specifically.
        CanFocus = false;
        TabStop = TabBehavior.NoStop;
        Parent = parent;
    }

    /// <summary>The Parent of this Adornment (the View this Adornment surrounds).</summary>
    /// <remarks>
    ///     Adornments are distinguished from typical View classes in that they are not sub-views, but have a parent/child
    ///     relationship with their containing View.
    /// </remarks>
    public View? Parent { get; set; }

    #region Thickness

    /// <summary>
    ///     Gets or sets whether the Adornment will draw diagnostic information. This is a bit-field of
    ///     <see cref="ViewDiagnosticFlags"/>.
    /// </summary>
    /// <remarks>
    ///     The <see cref="View.Diagnostics"/> static property is used as the default value for this property.
    /// </remarks>
    public new ViewDiagnosticFlags Diagnostics { get; set; } = View.Diagnostics;

    private Thickness _thickness = Thickness.Empty;

    /// <summary>Defines the rectangle that the <see cref="Adornment"/> will use to draw its content.</summary>
    public Thickness Thickness
    {
        get => _thickness;
        set
        {
            Thickness current = _thickness;

            _thickness = value;

            if (current != _thickness)
            {
                Parent?.SetAdornmentFrames ();
                SetNeedsLayout ();
                SetNeedsDraw ();

                OnThicknessChanged ();
            }
        }
    }

    /// <summary>Fired whenever the <see cref="Thickness"/> property changes.</summary>
    public event EventHandler? ThicknessChanged;

    /// <summary>Called whenever the <see cref="Thickness"/> property changes.</summary>
    public void OnThicknessChanged () { ThicknessChanged?.Invoke (this, EventArgs.Empty); }

    #endregion Thickness

    #region View Overrides

    // If a scheme is explicitly set, use that. Otherwise, use the scheme of the parent view.
    private Scheme? _scheme;

    /// <inheritdoc />
    protected override bool OnGettingScheme (out Scheme? scheme)
    {
        scheme = _scheme ?? Parent?.GetScheme () ?? SchemeManager.GetScheme (Schemes.Base);

        return true;
    }

    /// <inheritdoc />
    protected override bool OnSettingScheme (ValueChangingEventArgs<Scheme?> args)
    {
        Parent?.SetNeedsDraw ();

        _scheme = args.NewValue;
        return false;
    }

    /// <summary>
    ///     Gets the rectangle that describes the area of the Adornment. The Location is always (0,0).
    ///     The size is the size of the <see cref="View.Frame"/>.
    /// </summary>
    /// <remarks>
    ///     The Viewport of an Adornment cannot be modified. Attempting to set this property will throw an
    ///     <see cref="InvalidOperationException"/>.
    /// </remarks>
    public override Rectangle Viewport
    {
        get => base.Viewport;
        set => throw new InvalidOperationException (@"The Viewport of an Adornment cannot be modified.");
    }

    /// <inheritdoc/>
    public override Rectangle FrameToScreen ()
    {
        if (Parent is null)
        {
            // While there are no real use cases for an Adornment being a subview, we support it for
            // testing. E.g. in AllViewsTester.
            if (SuperView is { })
            {
                Point super = SuperView.ViewportToScreen (Frame.Location);

                return new (super, Frame.Size);
            }

            return Frame;
        }

        // Adornments are *Children* of a View, not SubViews. Thus View.FrameToScreen will not work.
        // To get the screen-relative coordinates of an Adornment, we need get the parent's Frame
        // in screen coords, ...
        Rectangle parentScreen = Parent.FrameToScreen ();

        // ...and add our Frame location to it.
        return new (new (parentScreen.X + Frame.X, parentScreen.Y + Frame.Y), Frame.Size);
    }

    /// <inheritdoc/>
    public override Point ScreenToFrame (in Point location)
    {
        View? parentOrSuperView = Parent;

        if (parentOrSuperView is null)
        {
            // While there are no real use cases for an Adornment being a subview, we support it for
            // testing. E.g. in AllViewsTester.
            parentOrSuperView = SuperView;

            if (parentOrSuperView is null)
            {
                return Point.Empty;
            }
        }

        return parentOrSuperView.ScreenToFrame (new (location.X - Frame.X, location.Y - Frame.Y));
    }

    /// <summary>
    ///     Called when the <see cref="Thickness"/> of the Adornment is to be cleared.
    /// </summary>
    /// <returns><see langword="true"/> to stop further clearing.</returns>
    protected override bool OnClearingViewport ()
    {
        if (Thickness == Thickness.Empty)
        {
            return true;
        }

        // This just draws/clears the thickness, not the insides.
        Thickness.Draw (ViewportToScreen (Viewport), Diagnostics, ToString ());

        NeedsDraw = true;

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingText () { return Thickness == Thickness.Empty; }

    /// <inheritdoc/>
    protected override bool OnDrawingSubViews () { return Thickness == Thickness.Empty; }


    /// <summary>Does nothing for Adornment</summary>
    /// <returns></returns>
    protected override bool OnRenderingLineCanvas () { return true; }

    /// <summary>
    ///     Adornments only render to their <see cref="Parent"/>'s or Parent's SuperView's LineCanvas, so setting this
    ///     property throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    public override bool SuperViewRendersLineCanvas
    {
        get => false;
        set => throw new InvalidOperationException (@"Adornment can only render to their Parent or Parent's Superview.");
    }

    /// <summary>
    ///     Indicates whether the specified Parent's SuperView-relative coordinates are within the Adornment's Thickness.
    /// </summary>
    /// <remarks>
    ///     The <paramref name="location"/> is relative to the PARENT's SuperView.
    /// </remarks>
    /// <param name="location"></param>
    /// <returns>
    ///     <see langword="true"/> if the specified Parent's SuperView-relative coordinates are within the Adornment's
    ///     Thickness.
    /// </returns>
    public override bool Contains (in Point location)
    {
        View? parentOrSuperView = Parent;

        if (parentOrSuperView is null)
        {
            // While there are no real use cases for an Adornment being a subview, we support it for
            // testing. E.g. in AllViewsTester.
            parentOrSuperView = SuperView;

            if (parentOrSuperView is null)
            {
                return false;
            }
        }

        Rectangle outside = Frame;
        outside.Offset (parentOrSuperView.Frame.Location);

        return Thickness.Contains (outside, location);
    }

    /// <summary>
    ///     INTERNAL: Gets all Views (Subviews and Adornments) in the of <see cref="Adornment"/> hierarchcy that are at <paramref name="screenLocation"/>,
    ///     regardless of whether they will be drawn or see mouse events or not. Views with <see cref="View.Visible"/> set to <see langword="false"/> will not be included.
    ///     The list is ordered by depth. The deepest View is at the end of the list (the topmost View is at element 0).
    /// </summary>
    /// <param name="adornment">The root Adornment from which the search for subviews begins.</param>
    /// <param name="screenLocation">The screen-relative location where the search for views is focused.</param>
    /// <returns>A list of views that are located under the specified point.</returns>
    internal static List<View?> GetViewsAtLocation (Adornment? adornment, in Point screenLocation)
    {
        List<View?> result = [];

        if (adornment is null || adornment.Thickness == Thickness.Empty)
        {
            return result;
        }

        Point superViewRelativeLocation = adornment.Parent!.SuperView?.ScreenToViewport (screenLocation) ?? screenLocation;

        if (adornment.Contains (superViewRelativeLocation))
        {
            List<View?> adornmentResult = GetViewsAtLocation (adornment as View, screenLocation);
            if (adornmentResult.Count > 0)
            {
                result.AddRange (adornmentResult);
            }
        }

        return result;
    }

    #endregion View Overrides

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        // This enables AllViewsTester to show something useful.
        Thickness = new (3);
        Frame = new (0, 0, 10, 10);
        Diagnostics = ViewDiagnosticFlags.Thickness;

        return true;
    }
}

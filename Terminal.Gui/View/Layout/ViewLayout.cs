using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
///     <para>Indicates the LayoutStyle for the <see cref="View"/>.</para>
///     <para>
///         If Absolute, the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and
///         <see cref="View.Height"/> objects are all absolute values and are not relative. The position and size of the
///         view is described by <see cref="View.Frame"/>.
///     </para>
///     <para>
///         If Computed, one or more of the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, or
///         <see cref="View.Height"/> objects are relative to the <see cref="View.SuperView"/> and are computed at layout
///         time.
///     </para>
/// </summary>
public enum LayoutStyle
{
    /// <summary>
    ///     Indicates the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and
    ///     <see cref="View.Height"/> objects are all absolute values and are not relative. The position and size of the view
    ///     is described by <see cref="View.Frame"/>.
    /// </summary>
    Absolute,

    /// <summary>
    ///     Indicates one or more of the <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, or
    ///     <see cref="View.Height"/> objects are relative to the <see cref="View.SuperView"/> and are computed at layout time.
    ///     The position and size of the view will be computed based on these objects at layout time. <see cref="View.Frame"/>
    ///     will provide the absolute computed values.
    /// </summary>
    Computed
}

public partial class View
{
    private bool _autoSize;
    private Point _contentOffset;
    private Size _contentSize;
    private Rectangle _frame;
    private Dim _height = Dim.Sized (0);
    private Dim _width = Dim.Sized (0);
    private Pos _x = Pos.At (0);
    private Pos _y = Pos.At (0);

    /// <summary>
    ///     Gets or sets a flag that determines whether the View will be automatically resized to fit the <see cref="Text"/>
    ///     within <see cref="ContentArea"/>.
    ///     <para>
    ///         The default is <see langword="false"/>. Set to <see langword="true"/> to turn on AutoSize. If
    ///         <see langword="true"/> then <see cref="Width"/> and <see cref="Height"/> will be used if <see cref="Text"/> can
    ///         fit; if <see cref="Text"/> won't fit the view will be resized as needed.
    ///     </para>
    ///     <para>
    ///         If <see cref="AutoSize"/> is set to <see langword="true"/> then <see cref="Width"/> and <see cref="Height"/>
    ///         will be changed to <see cref="Dim.DimAbsolute"/> if they are not already.
    ///     </para>
    ///     <para>
    ///         If <see cref="AutoSize"/> is set to <see langword="false"/> then <see cref="Width"/> and <see cref="Height"/>
    ///         will left unchanged.
    ///     </para>
    /// </summary>
    public virtual bool AutoSize
    {
        get => _autoSize;
        set
        {
            if (Width != Dim.Sized (0) && Height != Dim.Sized (0))
            {
                Debug.WriteLine (
                                 $@"WARNING: {GetType ().Name} - Setting {nameof (AutoSize)} invalidates {nameof (Width)} and {nameof (Height)}."
                                );
            }

            bool v = ResizeView (value);
            TextFormatter.AutoSize = v;

            if (_autoSize != v)
            {
                _autoSize = v;
                TextFormatter.NeedsFormat = true;
                UpdateTextFormatterText ();
                OnResizeNeeded ();
            }
        }
    }

    /// <summary>
    ///     The <see cref="Adornment"/> that offsets the <see cref="ContentArea"/> from the <see cref="Margin"/>.
    ///     The Border provides the space for a visual border (drawn using
    ///     line-drawing glyphs) and the Title. The Border expands inward; in other words if `Border.Thickness.Top == 2` the
    ///     border and title will take up the first row and the second row will be filled with spaces.
    /// </summary>
    /// <remarks>
    ///     <para><see cref="BorderStyle"/> provides a simple helper for turning a simple border frame on or off.</para>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of a frame (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>) will
    ///         change the size of the <see cref="Frame"/> and trigger <see cref="LayoutSubviews"/> to update the layout of the
    ///         <see cref="SuperView"/> and its <see cref="Subviews"/>.
    ///     </para>
    /// </remarks>
    public Border Border { get; private set; }

    /// <summary>Gets or sets whether the view has a one row/col thick border.</summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper for manipulating the view's <see cref="Border"/>. Setting this property to any value other
    ///         than <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s
    ///         <see cref="Adornment.Thickness"/> to `1` and <see cref="BorderStyle"/> to the value.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see cref="LineStyle.None"/> is equivalent to setting <see cref="Border"/>'s
    ///         <see cref="Adornment.Thickness"/> to `0` and <see cref="BorderStyle"/> to <see cref="LineStyle.None"/>.
    ///     </para>
    ///     <para>For more advanced customization of the view's border, manipulate see <see cref="Border"/> directly.</para>
    /// </remarks>
    public LineStyle BorderStyle
    {
        get => Border.LineStyle;
        set
        {
            if (value != LineStyle.None)
            {
                Border.Thickness = new (1);
            }
            else
            {
                Border.Thickness = new (0);
            }

            Border.LineStyle = value;
            LayoutAdornments ();
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     The bounds represent the View-relative rectangle used for this view; the area inside the view where
    ///     subviews and content are presented.
    /// </summary>
    /// <value>The rectangle describing the location and size of the area where the views' subviews and content are drawn.</value>
    /// <remarks>
    ///     <para>
    ///         If <see cref="LayoutStyle"/> is <see cref="LayoutStyle.Computed"/> the value of Bounds is indeterminate until
    ///         the view has been initialized ( <see cref="IsInitialized"/> is true) and <see cref="LayoutSubviews"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Updates to the Bounds updates <see cref="Frame"/>, and has the same effect as updating the
    ///         <see cref="Frame"/>.
    ///     </para>
    ///     <para>
    ///         Altering the Bounds will eventually (when the view is next laid out) cause the
    ///         <see cref="LayoutSubview(View, Rectangle)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    ///     <para>
    ///         Because <see cref="ContentArea"/> coordinates are relative to the upper-left corner of the <see cref="View"/>,
    ///         the
    ///         coordinates of the upper-left corner of the rectangle returned by this property are (0,0) if
    ///         <see cref="UseContentOffset"/> is false.
    ///         If <see cref="UseContentOffset"/> is true can contains negative location given by the
    ///         <see cref="ContentOffset"/> which if it's negative will increment the respective width and height accordingly.
    ///         Use this property to obtain the size of the area of the view for tasks such as drawing the view's contents.
    ///     </para>
    /// </remarks>
    public virtual Rectangle ContentArea
    {
        get
        {
#if DEBUG
            if (LayoutStyle == LayoutStyle.Computed && !IsInitialized)
            {
                Debug.WriteLine (
                                 $"WARNING: Bounds is being accessed before the View has been initialized. This is likely a bug in {this}"
                                );
            }
#endif // DEBUG

            if (Margin is null || Border is null || Padding is null)
            {
                return Rectangle.Empty with { Size = Frame.Size };
            }

            Thickness totalThickness = GetAdornmentsThickness ();
            int width = Math.Max (0, Frame.Size.Width - totalThickness.Horizontal- (UseContentOffset ? ContentOffset.X : 0));
            int height = Math.Max (0, Frame.Size.Height - totalThickness.Vertical- (UseContentOffset ? ContentOffset.Y : 0));

            return new (UseContentOffset ? ContentOffset : Point.Empty, new Size (width, height));
        }
        set
        {
            // TODO: Should we enforce Bounds.X/Y == 0? The code currently ignores value.X/Y which is
            // TODO: correct behavior, but is silent. Perhaps an exception?
#if DEBUG
            if (value.Location != Point.Empty)
            {
                Debug.WriteLine (
                                 $"WARNING: Bounds.Location must always be 0,0. Location ({value.Location}) is ignored. {this}"
                                );
            }
#endif // DEBUG
            Thickness totalThickness = GetAdornmentsThickness ();

            Frame = Frame with
            {
                Size = new (value.Size.Width + totalThickness.Horizontal, value.Size.Height + totalThickness.Vertical)
            };
        }
    }

    /// <summary>
    ///     Represent the content offset if <see cref="UseContentOffset"/> is true.
    /// </summary>
    public virtual Point ContentOffset
    {
        get => _contentOffset;
        set
        {
            _contentOffset = value;

            if (_scrollBar is null)
            {
                return;
            }

            if (_scrollBar.Orientation == Orientation.Horizontal && _scrollBar.Position != -_contentOffset.X)
            {
                _scrollBar.Position = -_contentOffset.X;
            }
            else if (_scrollBar is { OtherScrollBarView.Orientation: Orientation.Horizontal } && _scrollBar?.OtherScrollBarView.Position != -_contentOffset.X)
            {
                _scrollBar!.OtherScrollBarView.Position = -_contentOffset.X;
            }

            if (_scrollBar.Orientation == Orientation.Vertical && _scrollBar.Position != -_contentOffset.Y)
            {
                _scrollBar.Position = -_contentOffset.Y;
            }
            else if (_scrollBar is { OtherScrollBarView.Orientation: Orientation.Vertical } && _scrollBar?.OtherScrollBarView.Position != -_contentOffset.Y)
            {
                _scrollBar!.OtherScrollBarView.Position = -_contentOffset.Y;
            }
        }
    }

    /// <summary>
    ///     Represents the contents size of the data shown inside the <see cref="ContentArea"/>.
    /// </summary>
    public Size ContentSize
    {
        get => _contentSize;
        set
        {
            if (_contentSize != value)
            {
                _contentSize = value;
                SetNeedsDisplay ();
            }

            if (_scrollBar is null)
            {
                return;
            }

            if (_scrollBar.Orientation == Orientation.Vertical)
            {
                if (_scrollBar.Size != ContentSize.Height)
                {
                    _scrollBar.Size = ContentSize.Height;
                }

                if (_scrollBar.OtherScrollBarView is { })
                {
                    if (_scrollBar.OtherScrollBarView.Size != ContentSize.Width)
                    {
                        _scrollBar.OtherScrollBarView.Size = ContentSize.Width;
                    }
                }
            }
            else
            {
                if (_scrollBar.Size != ContentSize.Width)
                {
                    _scrollBar.Size = ContentSize.Width;
                }

                if (_scrollBar.OtherScrollBarView is { })
                {
                    if (_scrollBar.OtherScrollBarView.Size != ContentSize.Height)
                    {
                        _scrollBar.OtherScrollBarView.Size = ContentSize.Height;
                    }
                }
            }
        }
    }

    /// <summary>Gets or sets the absolute location and dimension of the view.</summary>
    /// <value>
    ///     The rectangle describing absolute location and dimension of the view, in coordinates relative to the
    ///     <see cref="SuperView"/>'s <see cref="ContentArea"/>.
    /// </value>
    /// <remarks>
    ///     <para>Frame is relative to the <see cref="SuperView"/>'s <see cref="ContentArea"/>.</para>
    ///     <para>
    ///         Setting Frame will set <see cref="X"/>, <see cref="Y"/>, <see cref="Width"/>, and <see cref="Height"/> to the
    ///         values of the corresponding properties of the <paramref name="value"/> parameter.
    ///     </para>
    ///     <para>This causes <see cref="LayoutStyle"/> to be <see cref="LayoutStyle.Absolute"/>.</para>
    ///     <para>
    ///         Altering the Frame will eventually (when the view hierarchy is next laid out via  see
    ///         cref="LayoutSubviews"/>) cause <see cref="LayoutSubview(View, Rectangle)"/> and
    ///         <see cref="OnDrawContent(Rectangle)"/>
    ///         methods to be called.
    ///     </para>
    /// </remarks>
    public Rectangle Frame
    {
        get => _frame;
        set
        {
            _frame = value with { Width = Math.Max (value.Width, 0), Height = Math.Max (value.Height, 0) };

            // If Frame gets set, by definition, the View is now LayoutStyle.Absolute, so
            // set all Pos/Dim to Absolute values.
            _x = _frame.X;
            _y = _frame.Y;
            _width = _frame.Width;
            _height = _frame.Height;

            // TODO: Figure out if the below can be optimized.
            if (IsInitialized /*|| LayoutStyle == LayoutStyle.Absolute*/)
            {
                LayoutAdornments ();
                SetTextFormatterSize ();
                SetNeedsLayout ();
                SetNeedsDisplay ();
            }
        }
    }

    /// <summary>Gets or sets the height dimension of the view.</summary>
    /// <value>The <see cref="Dim"/> object representing the height of the view (the number of rows).</value>
    /// <remarks>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Dim.Fill(int)"/>) the value is indeterminate until the view has
    ///         been initialized ( <see cref="IsInitialized"/> is true) and <see cref="SetRelativeLayout(Rectangle)"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Changing this property will eventually (when the view is next drawn) cause the
    ///         <see cref="LayoutSubview(View, Rectangle)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated. If the new value is not of type
    ///         <see cref="Dim.DimAbsolute"/> the <see cref="LayoutStyle"/> will change to <see cref="LayoutStyle.Computed"/>.
    ///     </para>
    ///     <para>The default value is <c>Dim.Sized (0)</c>.</para>
    /// </remarks>
    public Dim Height
    {
        get => VerifyIsInitialized (_height, nameof (Height));
        set
        {
            _height = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (Height)} cannot be null");

            if (AutoSize)
            {
                throw new InvalidOperationException (@$"Must set AutoSize to false before setting {nameof (Height)}.");
            }

            //if (ValidatePosDim) {
            bool isValidNewAutoSize = AutoSize && IsValidAutoSizeHeight (_height);

            if (IsAdded && AutoSize && !isValidNewAutoSize)
            {
                throw new InvalidOperationException (
                                                     @$"Must set AutoSize to false before setting the {nameof (Height)}."
                                                    );
            }

            //}
            OnResizeNeeded ();
        }
    }

    /// <summary>
    ///     Controls how the View's <see cref="Frame"/> is computed during <see cref="LayoutSubviews"/>. If the style is
    ///     set to <see cref="LayoutStyle.Absolute"/>, LayoutSubviews does not change the <see cref="Frame"/>. If the style is
    ///     <see cref="LayoutStyle.Computed"/> the <see cref="Frame"/> is updated using the <see cref="X"/>, <see cref="Y"/>,
    ///     <see cref="Width"/>, and <see cref="Height"/> properties.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property to <see cref="LayoutStyle.Absolute"/> will cause <see cref="Frame"/> to determine the
    ///         size and position of the view. <see cref="X"/> and <see cref="Y"/> will be set to <see cref="Dim.DimAbsolute"/>
    ///         using <see cref="Frame"/>.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see cref="LayoutStyle.Computed"/> will cause the view to use the
    ///         <see cref="LayoutSubviews"/> method to size and position of the view. If either of the <see cref="X"/> and
    ///         <see cref="Y"/> properties are `null` they will be set to <see cref="Pos.PosAbsolute"/> using the current value
    ///         of <see cref="Frame"/>. If either of the <see cref="Width"/> and <see cref="Height"/> properties are `null`
    ///         they will be set to <see cref="Dim.DimAbsolute"/> using <see cref="Frame"/>.
    ///     </para>
    /// </remarks>
    /// <value>The layout style.</value>
    public LayoutStyle LayoutStyle
    {
        get
        {
            if (_x is Pos.PosAbsolute
                && _y is Pos.PosAbsolute
                && _width is Dim.DimAbsolute
                && _height is Dim.DimAbsolute)
            {
                return LayoutStyle.Absolute;
            }

            return LayoutStyle.Computed;
        }
    }

    /// <summary>
    ///     The <see cref="Adornment"/> that enables separation of a View from other SubViews of the same
    ///     SuperView. The margin offsets the <see cref="ContentArea"/> from the <see cref="Frame"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of an adornment (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>) will
    ///         change the size of <see cref="Frame"/> and trigger <see cref="LayoutSubviews"/> to update the layout of the
    ///         <see cref="SuperView"/> and its <see cref="Subviews"/>.
    ///     </para>
    /// </remarks>
    public Margin Margin { get; private set; }

    /// <summary>
    ///     The <see cref="Adornment"/> inside of the view that offsets the <see cref="ContentArea"/>
    ///     from the <see cref="Border"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The adornments (<see cref="Margin"/>, <see cref="Border"/>, and <see cref="Padding"/>) are not part of the
    ///         View's content and are not clipped by the View's Clip Area.
    ///     </para>
    ///     <para>
    ///         Changing the size of a frame (<see cref="Margin"/>, <see cref="Border"/>, or <see cref="Padding"/>) will
    ///         change the size of the <see cref="Frame"/> and trigger <see cref="LayoutSubviews"/> to update the layout of the
    ///         <see cref="SuperView"/> and its <see cref="Subviews"/>.
    ///     </para>
    /// </remarks>
    public Padding Padding { get; private set; }

    /// <summary>
    ///     Determines if negative bounds location is allowed for scrolling the <see cref="GetVisibleContentArea"/>.
    /// </summary>
    public bool UseContentOffset { get; set; }

    /// <summary>Gets or sets whether validation of <see cref="Pos"/> and <see cref="Dim"/> occurs.</summary>
    /// <remarks>
    ///     Setting this to <see langword="true"/> will enable validation of <see cref="X"/>, <see cref="Y"/>,
    ///     <see cref="Width"/>, and <see cref="Height"/> during set operations and in <see cref="LayoutSubviews"/>. If invalid
    ///     settings are discovered exceptions will be thrown indicating the error. This will impose a performance penalty and
    ///     thus should only be used for debugging.
    /// </remarks>
    public bool ValidatePosDim { get; set; }

    /// <summary>Gets or sets the width dimension of the view.</summary>
    /// <value>The <see cref="Dim"/> object representing the width of the view (the number of columns).</value>
    /// <remarks>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Dim.Fill(int)"/>) the value is indeterminate until the view has
    ///         been initialized ( <see cref="IsInitialized"/> is true) and <see cref="SetRelativeLayout(Rectangle)"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Changing this property will eventually (when the view is next drawn) cause the
    ///         <see cref="LayoutSubview(View, Rectangle)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated. If the new value is not of type
    ///         <see cref="Dim.DimAbsolute"/> the <see cref="LayoutStyle"/> will change to <see cref="LayoutStyle.Computed"/>.
    ///     </para>
    ///     <para>The default value is <c>Dim.Sized (0)</c>.</para>
    /// </remarks>
    public Dim Width
    {
        get => VerifyIsInitialized (_width, nameof (Width));
        set
        {
            _width = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (Width)} cannot be null");

            if (AutoSize)
            {
                throw new InvalidOperationException (@$"Must set AutoSize to false before setting {nameof (Width)}.");
            }

            bool isValidNewAutoSize = AutoSize && IsValidAutoSizeWidth (_width);

            if (IsAdded && AutoSize && !isValidNewAutoSize)
            {
                throw new InvalidOperationException (@$"Must set AutoSize to false before setting {nameof (Width)}.");
            }

            OnResizeNeeded ();
        }
    }

    /// <summary>Gets or sets the X position for the view (the column).</summary>
    /// <value>The <see cref="Pos"/> object representing the X position.</value>
    /// <remarks>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Pos.Center"/>) the value is indeterminate until the view has been
    ///         initialized ( <see cref="IsInitialized"/> is true) and <see cref="SetRelativeLayout(Rectangle)"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Changing this property will eventually (when the view is next drawn) cause the
    ///         <see cref="LayoutSubview(View, Rectangle)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated. If the new value is not of type
    ///         <see cref="Pos.PosAbsolute"/> the <see cref="LayoutStyle"/> will change to <see cref="LayoutStyle.Computed"/>.
    ///     </para>
    ///     <para>The default value is <c>Pos.At (0)</c>.</para>
    /// </remarks>
    public Pos X
    {
        get => VerifyIsInitialized (_x, nameof (X));
        set
        {
            _x = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (X)} cannot be null");
            OnResizeNeeded ();
        }
    }

    /// <summary>Gets or sets the Y position for the view (the row).</summary>
    /// <value>The <see cref="Pos"/> object representing the Y position.</value>
    /// <remarks>
    ///     <para>
    ///         If set to a relative value (e.g. <see cref="Pos.Center"/>) the value is indeterminate until the view has been
    ///         initialized ( <see cref="IsInitialized"/> is true) and <see cref="SetRelativeLayout(Rectangle)"/> has been
    ///         called.
    ///     </para>
    ///     <para>
    ///         Changing this property will eventually (when the view is next drawn) cause the
    ///         <see cref="LayoutSubview(View, Rectangle)"/> and <see cref="OnDrawContent(Rectangle)"/> methods to be called.
    ///     </para>
    ///     <para>
    ///         Changing this property will cause <see cref="Frame"/> to be updated. If the new value is not of type
    ///         <see cref="Pos.PosAbsolute"/> the <see cref="LayoutStyle"/> will change to <see cref="LayoutStyle.Computed"/>.
    ///     </para>
    ///     <para>The default value is <c>Pos.At (0)</c>.</para>
    /// </remarks>
    public Pos Y
    {
        get => VerifyIsInitialized (_y, nameof (Y));
        set
        {
            _y = value ?? throw new ArgumentNullException (nameof (value), @$"{nameof (Y)} cannot be null");
            OnResizeNeeded ();
        }
    }

    internal bool LayoutNeeded { get; private set; } = true;

    /// <summary>
    ///     Event called only once when the <see cref="View"/> is being initialized for the first time. Allows
    ///     configurations and assignments to be performed before the <see cref="View"/> being shown. This derived from
    ///     <see cref="ISupportInitializeNotification"/> to allow notify all the views that are being initialized.
    /// </summary>
    public event EventHandler Initialized;

    /// <summary>Converts a <see cref="ContentArea"/>-relative rectangle to a screen-relative rectangle.</summary>
    public Rectangle BoundsToScreen (in Rectangle bounds)
    {
        // Translate bounds to Frame (our SuperView's Bounds-relative coordinates)
        Point boundsOffset = GetBoundsOffset ();

        Rectangle screen = FrameToScreen ();
        screen.Offset (boundsOffset.X + bounds.X, boundsOffset.Y + bounds.Y);

        return new (screen.Location, bounds.Size);
    }

#nullable enable
    /// <summary>Finds which view that belong to the <paramref name="start"/> superview at the provided location.</summary>
    /// <param name="start">The superview where to look for.</param>
    /// <param name="x">The column location in the superview.</param>
    /// <param name="y">The row location in the superview.</param>
    /// <param name="findAdornments">TODO: Remove this in PR #3273</param>
    /// <returns>
    ///     The view that was found at the <paramref name="x"/> and <paramref name="y"/> coordinates.
    ///     <see langword="null"/> if no view was found.
    /// </returns>

    // CONCURRENCY: This method is not thread-safe.
    // Undefined behavior and likely program crashes are exposed by unsynchronized access to InternalSubviews.
    public static View? FindDeepestView (View? start, int x, int y, bool findAdornments = false)
    {
        if (start is null || !start.Visible)
        {
            return null;
        }

        if (!start.Frame.Contains (x, y))
        {
            return null;
        }

        if (findAdornments)
        {
            // TODO: This is a temporary hack for PR #3273; it is not actually used anywhere but unit tests at this point.
            if (start.Margin.Thickness.Contains (start.Margin.Frame, x, y))
            {
                return start.Margin;
            }

            if (start.Border.Thickness.Contains (start.Border.Frame, x, y))
            {
                return start.Border;
            }

            if (start.Padding.Thickness.Contains (start.Padding.Frame, x, y))
            {
                return start.Padding;
            }
        }

        if (start.InternalSubviews is { Count: > 0 })
        {
            Point boundsOffset = start.GetBoundsOffset ();
            int rx = x - (start.Frame.X + boundsOffset.X);
            int ry = y - (start.Frame.Y + boundsOffset.Y);

            for (int i = start.InternalSubviews.Count - 1; i >= 0; i--)
            {
                View v = start.InternalSubviews [i];

                if (v.Visible && v.Frame.Contains (rx, ry))
                {
                    View? deep = FindDeepestView (v, rx, ry, findAdornments);

                    return deep ?? v;
                }
            }
        }

        return start;
    }
#nullable restore

    /// <summary>Gets the <see cref="Frame"/> with a screen-relative location.</summary>
    /// <returns>The location and size of the view in screen-relative coordinates.</returns>
    public virtual Rectangle FrameToScreen ()
    {
        Rectangle ret = Frame;
        View super = SuperView;

        while (super is { })
        {
            Point boundsOffset = super.GetBoundsOffset ();
            ret.X += super.Frame.X + boundsOffset.X;
            ret.Y += super.Frame.Y + boundsOffset.Y;
            super = super.SuperView;
        }

        return ret;
    }

    /// <summary>
    ///     <para>Gets the thickness describing the sum of the Adornments' thicknesses.</para>
    /// </summary>
    /// <returns>A thickness that describes the sum of the Adornments' thicknesses.</returns>
    public Thickness GetAdornmentsThickness () { return Margin.Thickness + Border.Thickness + Padding.Thickness; }

    /// <summary>
    ///     Helper to get the X and Y offset of the Bounds from the Frame. This is the sum of the Left and Top properties
    ///     of <see cref="Margin"/>, <see cref="Border"/> and <see cref="Padding"/>.
    /// </summary>
    public virtual Point GetBoundsOffset ()
    {
        if (Padding is null)
        {
            return Point.Empty;
        }

        return Padding.Thickness.GetInside (Padding.Frame).Location;
    }

    /// <summary>
    ///     Get the visible content area represent the View-relative rectangle used for this view. The area inside the view
    ///     where subviews
    ///     and content are presented. The Location is always (0,0). It will be mainly used for clipping a region.
    /// </summary>
    public virtual Rectangle GetVisibleContentArea ()
    {
        {
            if (Margin == null || Border == null || Padding == null)
            {
                return new Rectangle (default (Point), Frame.Size);
            }

            int width = Math.Max (
                                  0,
                                  Frame.Size.Width - Margin.Thickness.Horizontal - Border.Thickness.Horizontal - Padding.Thickness.Horizontal);

            int height = Math.Max (
                                   0,
                                   Frame.Size.Height - Margin.Thickness.Vertical - Border.Thickness.Vertical - Padding.Thickness.Vertical);

            return new Rectangle (Point.Empty, new Size (width, height));
        }
    }

    /// <summary>Fired after the View's <see cref="LayoutSubviews"/> method has completed.</summary>
    /// <remarks>
    ///     Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has
    ///     otherwise changed.
    /// </remarks>
    public event EventHandler<LayoutEventArgs> LayoutComplete;

    /// <summary>Fired after the View's <see cref="LayoutSubviews"/> method has completed.</summary>
    /// <remarks>
    ///     Subscribe to this event to perform tasks when the <see cref="View"/> has been resized or the layout has
    ///     otherwise changed.
    /// </remarks>
    public event EventHandler<LayoutEventArgs> LayoutStarted;

    /// <summary>
    ///     Invoked when a view starts executing or when the dimensions of the view have changed, for example in response
    ///     to the container view or terminal resizing.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The position and dimensions of the view are indeterminate until the view has been initialized. Therefore, the
    ///         behavior of this method is indeterminate if <see cref="IsInitialized"/> is <see langword="false"/>.
    ///     </para>
    ///     <para>Raises the <see cref="LayoutComplete"/> event) before it returns.</para>
    /// </remarks>
    public virtual void LayoutSubviews ()
    {
        if (!IsInitialized)
        {
            Debug.WriteLine (
                             $"WARNING: LayoutSubviews called before view has been initialized. This is likely a bug in {this}"
                            );
        }

        if (!LayoutNeeded)
        {
            return;
        }

        LayoutAdornments ();

        Rectangle oldBounds = ContentArea;
        OnLayoutStarted (new() { OldBounds = oldBounds });

        SetTextFormatterSize ();

        // Sort out the dependencies of the X, Y, Width, Height properties
        HashSet<View> nodes = new ();
        HashSet<(View, View)> edges = new ();
        CollectAll (this, ref nodes, ref edges);
        List<View> ordered = TopologicalSort (SuperView, nodes, edges);

        foreach (View v in ordered)
        {
            LayoutSubview (v, new (GetBoundsOffset (), ContentArea.Size));
        }

        // If the 'to' is rooted to 'from' and the layoutstyle is Computed it's a special-case.
        // Use LayoutSubview with the Frame of the 'from' 
        if (SuperView is { } && GetTopSuperView () is { } && LayoutNeeded && edges.Count > 0)
        {
            foreach ((View from, View to) in edges)
            {
                LayoutSubview (to, from.Frame);
            }
        }

        LayoutNeeded = false;

        OnLayoutComplete (new() { OldBounds = oldBounds });
    }

    /// <summary>Converts a screen-relative coordinate to a bounds-relative coordinate.</summary>
    /// <returns>The coordinate relative to this view's <see cref="ContentArea"/>.</returns>
    /// <param name="x">Screen-relative column.</param>
    /// <param name="y">Screen-relative row.</param>
    public Point ScreenToBounds (int x, int y)
    {
        Point screen = ScreenToFrame (x, y);
        Point boundsOffset = GetBoundsOffset ();

        return new (screen.X - boundsOffset.X, screen.Y - boundsOffset.Y);
    }

    /// <summary>
    ///     Converts a screen-relative coordinate to a Frame-relative coordinate. Frame-relative means relative to the
    ///     View's <see cref="SuperView"/>'s <see cref="ContentArea"/>.
    /// </summary>
    /// <returns>The coordinate relative to the <see cref="SuperView"/>'s <see cref="ContentArea"/>.</returns>
    /// <param name="x">Screen-relative column.</param>
    /// <param name="y">Screen-relative row.</param>
    public Point ScreenToFrame (int x, int y)
    {
        Point superViewBoundsOffset = SuperView?.GetBoundsOffset () ?? Point.Empty;
        var ret = new Point (x - Frame.X - superViewBoundsOffset.X, y - Frame.Y - superViewBoundsOffset.Y);

        if (SuperView is { })
        {
            Point superFrame = SuperView.ScreenToFrame (x - superViewBoundsOffset.X, y - superViewBoundsOffset.Y);
            ret = new (superFrame.X - Frame.X, superFrame.Y - Frame.Y);
        }

        return ret;
    }

    /// <summary>Indicates that the view does not need to be laid out.</summary>
    protected void ClearLayoutNeeded ()
    {
        LayoutNeeded = false;
    }

    internal void CollectAll (View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        // BUGBUG: This should really only work on initialized subviews
        foreach (View v in from.InternalSubviews /*.Where(v => v.IsInitialized)*/)
        {
            nNodes.Add (v);

            if (v.LayoutStyle != LayoutStyle.Computed)
            {
                continue;
            }

            CollectPos (v.X, v, ref nNodes, ref nEdges);
            CollectPos (v.Y, v, ref nNodes, ref nEdges);
            CollectDim (v.Width, v, ref nNodes, ref nEdges);
            CollectDim (v.Height, v, ref nNodes, ref nEdges);
        }
    }

    internal void CollectDim (Dim dim, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        switch (dim)
        {
            case Dim.DimView dv:
                // See #2461
                //if (!from.InternalSubviews.Contains (dv.Target)) {
                //	throw new InvalidOperationException ($"View {dv.Target} is not a subview of {from}");
                //}
                if (dv.Target != this)
                {
                    nEdges.Add ((dv.Target, from));
                }

                return;
            case Dim.DimCombine dc:
                CollectDim (dc._left, from, ref nNodes, ref nEdges);
                CollectDim (dc._right, from, ref nNodes, ref nEdges);

                break;
        }
    }

    internal void CollectPos (Pos pos, View from, ref HashSet<View> nNodes, ref HashSet<(View, View)> nEdges)
    {
        switch (pos)
        {
            case Pos.PosView pv:
                // See #2461
                //if (!from.InternalSubviews.Contains (pv.Target)) {
                //	throw new InvalidOperationException ($"View {pv.Target} is not a subview of {from}");
                //}
                if (pv.Target != this)
                {
                    nEdges.Add ((pv.Target, from));
                }

                return;
            case Pos.PosCombine pc:
                CollectPos (pc._left, from, ref nNodes, ref nEdges);
                CollectPos (pc._right, from, ref nNodes, ref nEdges);

                break;
        }
    }

    /// <summary>
    ///     This internal method is overridden by Adornment to do nothing to prevent recursion during View construction.
    ///     And, because Adornments don't have Adornments. It's internal to support unit tests.
    /// </summary>
    /// <param name="adornmentType"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    internal virtual Adornment CreateAdornment (Type adornmentType)
    {
        void ThicknessChangedHandler (object sender, EventArgs e)
        {
            if (IsInitialized)
            {
                LayoutAdornments ();
            }

            SetNeedsLayout ();
            SetNeedsDisplay ();
        }

        Adornment adornment;

        adornment = Activator.CreateInstance (adornmentType, this) as Adornment;
        adornment.ThicknessChanged += ThicknessChangedHandler;

        return adornment;
    }

    /// <summary>Overriden by <see cref="Adornment"/> to do nothing, as the <see cref="Adornment"/> does not have adornments.</summary>
    internal virtual void LayoutAdornments ()
    {
        if (Margin is null)
        {
            return; // CreateAdornments () has not been called yet
        }

        if (Margin.Frame.Size != Frame.Size)
        {
            Margin._frame = Rectangle.Empty with { Size = Frame.Size };
            Margin.X = 0;
            Margin.Y = 0;
            Margin.Width = Frame.Size.Width;
            Margin.Height = Frame.Size.Height;
            Margin.SetNeedsLayout ();
            Margin.SetNeedsDisplay ();
        }

        Rectangle border = Margin.Thickness.GetInside (Margin.Frame);

        if (border != Border.Frame)
        {
            Border._frame = border;
            Border.X = border.Location.X;
            Border.Y = border.Location.Y;
            Border.Width = border.Size.Width;
            Border.Height = border.Size.Height;
            Border.SetNeedsLayout ();
            Border.SetNeedsDisplay ();
        }

        Rectangle padding = Border.Thickness.GetInside (Border.Frame);

        if (padding != Padding.Frame)
        {
            Padding._frame = padding;
            Padding.X = padding.Location.X;
            Padding.Y = padding.Location.Y;
            Padding.Width = padding.Size.Width;
            Padding.Height = padding.Size.Height;
            Padding.SetNeedsLayout ();
            Padding.SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     Raises the <see cref="LayoutComplete"/> event. Called from  <see cref="LayoutSubviews"/> before all sub-views
    ///     have been laid out.
    /// </summary>
    internal virtual void OnLayoutComplete (LayoutEventArgs args) { LayoutComplete?.Invoke (this, args); }

    /// <summary>
    ///     Raises the <see cref="LayoutStarted"/> event. Called from  <see cref="LayoutSubviews"/> before any subviews
    ///     have been laid out.
    /// </summary>
    internal virtual void OnLayoutStarted (LayoutEventArgs args) { LayoutStarted?.Invoke (this, args); }

    /// <summary>
    ///     Called whenever the view needs to be resized. This is called whenever <see cref="Frame"/>,
    ///     <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, or <see cref="View.Height"/> changes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Determines the relative bounds of the <see cref="View"/> and its <see cref="Frame"/>s, and then calls
    ///         <see cref="SetRelativeLayout(Rectangle)"/> to update the view.
    ///     </para>
    /// </remarks>
    internal void OnResizeNeeded ()
    {
        // TODO: Identify a real-world use-case where this API should be virtual. 
        // TODO: Until then leave it `internal` and non-virtual
        // First try SuperView.Bounds, then Application.Top, then Driver.Bounds.
        // Finally, if none of those are valid, use int.MaxValue (for Unit tests).
        Rectangle relativeBounds = SuperView is { IsInitialized: true } ? SuperView.ContentArea :
                                   Application.Top is { } && Application.Top.IsInitialized ? Application.Top.ContentArea :
                                   Application.Driver?.Bounds ?? new Rectangle (0, 0, int.MaxValue, int.MaxValue);
        SetRelativeLayout (relativeBounds);

        // TODO: Determine what, if any of the below is actually needed here.
        if (IsInitialized)
        {
            if (AutoSize)
            {
                SetFrameToFitText ();
                SetTextFormatterSize ();
            }

            LayoutAdornments ();
            SetNeedsLayout ();
            SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     Sets the internal <see cref="LayoutNeeded"/> flag for this View and all of it's subviews and it's SuperView.
    ///     The main loop will call SetRelativeLayout and LayoutSubviews for any view with <see cref="LayoutNeeded"/> set.
    /// </summary>
    internal void SetNeedsLayout ()
    {
        if (LayoutNeeded)
        {
            return;
        }

        LayoutNeeded = true;

        foreach (View view in Subviews)
        {
            view.SetNeedsLayout ();
        }

        if (Margin != null)
        {
            for (var i = 0; i < 3; i++)
            {
                Adornment adornment = i switch
                                      {
                                          0 => Margin,
                                          1 => Border,
                                          2 => Padding,
                                          _ => null
                                      };

                if (adornment != null)
                {
                    adornment.SetNeedsLayout ();

                    foreach (View view in adornment.Subviews)
                    {
                        view.SetNeedsLayout ();
                    }
                }
            }
        }

        TextFormatter.NeedsFormat = true;
        SuperView?.SetNeedsLayout ();
    }

    /// <summary>
    ///     Applies the view's position (<see cref="X"/>, <see cref="Y"/>) and dimension (<see cref="Width"/>, and
    ///     <see cref="Height"/>) to <see cref="Frame"/>, given a rectangle describing the SuperView's Bounds (nominally the
    ///     same as <c>this.SuperView.Bounds</c>).
    /// </summary>
    /// <param name="superviewBounds">
    ///     The rectangle describing the SuperView's Bounds (nominally the same as
    ///     <c>this.SuperView.Bounds</c>).
    /// </param>
    internal void SetRelativeLayout (Rectangle superviewBounds)
    {
        Debug.Assert (_x is { });
        Debug.Assert (_y is { });
        Debug.Assert (_width is { });
        Debug.Assert (_height is { });

        int newX, newW, newY, newH;
        var autosize = Size.Empty;

        if (AutoSize)
        {
            // Note this is global to this function and used as such within the local functions defined
            // below. In v2 AutoSize will be re-factored to not need to be dealt with in this function.
            autosize = GetAutoSize ();
        }

        // TODO: Since GetNewLocationAndDimension does not depend on View, it can be moved into PosDim.cs
        // TODO: to make architecture more clean. Do this after DimAuto is implemented and the 
        // TODO: View.AutoSize stuff is removed.

        // Returns the new dimension (width or height) and location (x or y) for the View given
        //   the superview's Bounds
        //   the current Pos (View.X or View.Y)
        //   the current Dim (View.Width or View.Height)
        // This method is called recursively if pos is Pos.PosCombine
        (int newLocation, int newDimension) GetNewLocationAndDimension (
            bool width,
            Rectangle superviewBounds,
            Pos pos,
            Dim dim,
            int autosizeDimension
        )
        {
            // Gets the new dimension (width or height, dependent on `width`) of the given Dim given:
            //   location: the current location (x or y)
            //   dimension: the new dimension (width or height) (if relevant for Dim type)
            //   autosize: the size to use if autosize = true
            // This method is recursive if d is Dim.DimCombine
            int GetNewDimension (Dim d, int location, int dimension, int autosize)
            {
                int newDimension;

                switch (d)
                {
                    case Dim.DimCombine combine:
                        // TODO: Move combine logic into DimCombine?
                        int leftNewDim = GetNewDimension (combine._left, location, dimension, autosize);
                        int rightNewDim = GetNewDimension (combine._right, location, dimension, autosize);

                        if (combine._add)
                        {
                            newDimension = leftNewDim + rightNewDim;
                        }
                        else
                        {
                            newDimension = leftNewDim - rightNewDim;
                        }

                        newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;

                        break;

                    case Dim.DimFactor factor when !factor.IsFromRemaining ():
                        newDimension = d.Anchor (dimension);
                        newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;

                        break;

                    case Dim.DimAbsolute:
                        // DimAbsolute.Anchor (int width) ignores width and returns n
                        newDimension = Math.Max (d.Anchor (0), 0);

                        // BUGBUG: AutoSize does two things: makes text fit AND changes the view's dimensions
                        newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;

                        break;

                    case Dim.DimFill:
                    default:
                        newDimension = Math.Max (d.Anchor (dimension - location), 0);
                        newDimension = AutoSize && autosize > newDimension ? autosize : newDimension;

                        break;
                }

                return newDimension;
            }

            int newDimension, newLocation;
            int superviewDimension = width ? superviewBounds.Width : superviewBounds.Height;

            // Determine new location
            switch (pos)
            {
                case Pos.PosCenter posCenter:
                    // For Center, the dimension is dependent on location, but we need to force getting the dimension first
                    // using a location of 0
                    newDimension = Math.Max (GetNewDimension (dim, 0, superviewDimension, autosizeDimension), 0);
                    newLocation = posCenter.Anchor (superviewDimension - newDimension);

                    newDimension = Math.Max (
                                             GetNewDimension (dim, newLocation, superviewDimension, autosizeDimension),
                                             0
                                            );

                    break;

                case Pos.PosCombine combine:
                    // TODO: Move combine logic into PosCombine?
                    int left, right;

                    (left, newDimension) = GetNewLocationAndDimension (
                                                                       width,
                                                                       superviewBounds,
                                                                       combine._left,
                                                                       dim,
                                                                       autosizeDimension
                                                                      );

                    (right, newDimension) = GetNewLocationAndDimension (
                                                                        width,
                                                                        superviewBounds,
                                                                        combine._right,
                                                                        dim,
                                                                        autosizeDimension
                                                                       );

                    if (combine._add)
                    {
                        newLocation = left + right;
                    }
                    else
                    {
                        newLocation = left - right;
                    }

                    newDimension = Math.Max (
                                             GetNewDimension (dim, newLocation, superviewDimension, autosizeDimension),
                                             0
                                            );

                    break;

                case Pos.PosAnchorEnd:
                case Pos.PosAbsolute:
                case Pos.PosFactor:
                case Pos.PosFunc:
                case Pos.PosView:
                default:
                    newLocation = pos?.Anchor (superviewDimension) ?? 0;

                    newDimension = Math.Max (
                                             GetNewDimension (dim, newLocation, superviewDimension, autosizeDimension),
                                             0
                                            );

                    break;
            }

            return (newLocation, newDimension);
        }

        // horizontal/width
        (newX, newW) = GetNewLocationAndDimension (true, superviewBounds, _x, _width, autosize.Width);

        // vertical/height
        (newY, newH) = GetNewLocationAndDimension (false, superviewBounds, _y, _height, autosize.Height);

        Rectangle r = new (newX, newY, newW, newH);

        if (Frame != r)
        {
            // Set the frame. Do NOT use `Frame` as it overwrites X, Y, Width, and Height, making
            // the view LayoutStyle.Absolute.
            _frame = r;

            if (_x is Pos.PosAbsolute)
            {
                _x = Frame.X;
            }

            if (_y is Pos.PosAbsolute)
            {
                _y = Frame.Y;
            }

            if (_width is Dim.DimAbsolute)
            {
                _width = Frame.Width;
            }

            if (_height is Dim.DimAbsolute)
            {
                _height = Frame.Height;
            }

            SetNeedsLayout ();
            SetNeedsDisplay ();
        }

        if (AutoSize)
        {
            if (autosize.Width == 0 || autosize.Height == 0)
            {
                // Set the frame. Do NOT use `Frame` as it overwrites X, Y, Width, and Height, making
                // the view LayoutStyle.Absolute.
                _frame = _frame with { Size = autosize };

                if (autosize.Width == 0)
                {
                    _width = 0;
                }

                if (autosize.Height == 0)
                {
                    _height = 0;
                }
            }
            else if (!SetFrameToFitText ())
            {
                SetTextFormatterSize ();
            }

            SetNeedsLayout ();
            SetNeedsDisplay ();
        }
    }

    // https://en.wikipedia.org/wiki/Topological_sorting
    internal static List<View> TopologicalSort (
        View superView,
        IEnumerable<View> nodes,
        ICollection<(View From, View To)> edges
    )
    {
        List<View> result = new ();

        // Set of all nodes with no incoming edges
        HashSet<View> noEdgeNodes = new (nodes.Where (n => edges.All (e => !e.To.Equals (n))));

        while (noEdgeNodes.Any ())
        {
            //  remove a node n from S
            View n = noEdgeNodes.First ();
            noEdgeNodes.Remove (n);

            // add n to tail of L
            if (n != superView)
            {
                result.Add (n);
            }

            // for each node m with an edge e from n to m do
            foreach ((View From, View To) e in edges.Where (e => e.From.Equals (n)).ToArray ())
            {
                View m = e.To;

                // remove edge e from the graph
                edges.Remove (e);

                // if m has no other incoming edges then
                if (edges.All (me => !me.To.Equals (m)) && m != superView)
                {
                    // insert m into S
                    noEdgeNodes.Add (m);
                }
            }
        }

        if (!edges.Any ())
        {
            return result;
        }

        foreach ((View from, View to) in edges)
        {
            if (from == to)
            {
                // if not yet added to the result, add it and remove from edge
                if (result.Find (v => v == from) is null)
                {
                    result.Add (from);
                }

                edges.Remove ((from, to));
            }
            else if (from.SuperView == to.SuperView)
            {
                // if 'from' is not yet added to the result, add it
                if (result.Find (v => v == from) is null)
                {
                    result.Add (from);
                }

                // if 'to' is not yet added to the result, add it
                if (result.Find (v => v == to) is null)
                {
                    result.Add (to);
                }

                // remove from edge
                edges.Remove ((from, to));
            }
            else if (from != superView?.GetTopSuperView (to, from) && !ReferenceEquals (from, to))
            {
                if (ReferenceEquals (from.SuperView, to))
                {
                    throw new InvalidOperationException (
                                                         $"ComputedLayout for \"{superView}\": \"{to}\" references a SubView (\"{from}\")."
                                                        );
                }

                throw new InvalidOperationException (
                                                     $"ComputedLayout for \"{superView}\": \"{from}\" linked with \"{to}\" was not found. Did you forget to add it to {superView}?"
                                                    );
            }
        }

        // return L (a topologically sorted order)
        return result;
    } // TopologicalSort

    /// <summary>Determines if the View's <see cref="Height"/> can be set to a new value.</summary>
    /// <remarks>TrySetHeight can only be called when AutoSize is true (or being set to true).</remarks>
    /// <param name="desiredHeight"></param>
    /// <param name="resultHeight">
    ///     Contains the width that would result if <see cref="Height"/> were set to
    ///     <paramref name="desiredHeight"/>"/>
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if the View's <see cref="Height"/> can be changed to the specified value. False
    ///     otherwise.
    /// </returns>
    internal bool TrySetHeight (int desiredHeight, out int resultHeight)
    {
        int h = desiredHeight;
        bool canSetHeight;

        switch (Height)
        {
            case Dim.DimCombine _:
            case Dim.DimView _:
            case Dim.DimFill _:
                // It's a Dim.DimCombine and so can't be assigned. Let it have it's height anchored.
                h = Height.Anchor (h);
                canSetHeight = !ValidatePosDim;

                break;
            case Dim.DimFactor factor:
                // Tries to get the SuperView height otherwise the view height.
                int sh = SuperView is { } ? SuperView.Frame.Height : h;

                if (factor.IsFromRemaining ())
                {
                    sh -= Frame.Y;
                }

                h = Height.Anchor (sh);
                canSetHeight = !ValidatePosDim;

                break;
            default:
                canSetHeight = true;

                break;
        }

        resultHeight = h;

        return canSetHeight;
    }

    /// <summary>Determines if the View's <see cref="Width"/> can be set to a new value.</summary>
    /// <remarks>TrySetWidth can only be called when AutoSize is true (or being set to true).</remarks>
    /// <param name="desiredWidth"></param>
    /// <param name="resultWidth">
    ///     Contains the width that would result if <see cref="Width"/> were set to
    ///     <paramref name="desiredWidth"/>"/>
    /// </param>
    /// <returns>
    ///     <see langword="true"/> if the View's <see cref="Width"/> can be changed to the specified value. False
    ///     otherwise.
    /// </returns>
    internal bool TrySetWidth (int desiredWidth, out int resultWidth)
    {
        int w = desiredWidth;
        bool canSetWidth;

        switch (Width)
        {
            case Dim.DimCombine _:
            case Dim.DimView _:
            case Dim.DimFill _:
                // It's a Dim.DimCombine and so can't be assigned. Let it have it's Width anchored.
                w = Width.Anchor (w);
                canSetWidth = !ValidatePosDim;

                break;
            case Dim.DimFactor factor:
                // Tries to get the SuperView Width otherwise the view Width.
                int sw = SuperView is { } ? SuperView.Frame.Width : w;

                if (factor.IsFromRemaining ())
                {
                    sw -= Frame.X;
                }

                w = Width.Anchor (sw);
                canSetWidth = !ValidatePosDim;

                break;
            default:
                canSetWidth = true;

                break;
        }

        resultWidth = w;

        return canSetWidth;
    }

    private void LayoutSubview (View v, Rectangle contentArea)
    {
        //if (v.LayoutStyle == LayoutStyle.Computed) {
        v.SetRelativeLayout (contentArea);

        //}

        v.LayoutSubviews ();
        v.LayoutNeeded = false;
    }

    /// <summary>Resizes the View to fit the specified size. Factors in the HotKey.</summary>
    /// <remarks>ResizeBoundsToFit can only be called when AutoSize is true (or being set to true).</remarks>
    /// <param name="size"></param>
    /// <returns>whether the Bounds was changed or not</returns>
    private bool ResizeBoundsToFit (Size size)
    {
        //if (AutoSize == false) {
        //	throw new InvalidOperationException ("ResizeBoundsToFit can only be called when AutoSize is true");
        //}

        var boundsChanged = false;
        bool canSizeW = TrySetWidth (size.Width - GetHotKeySpecifierLength (), out int rW);
        bool canSizeH = TrySetHeight (size.Height - GetHotKeySpecifierLength (false), out int rH);

        if (canSizeW)
        {
            boundsChanged = true;
            _width = rW;
        }

        if (canSizeH)
        {
            boundsChanged = true;
            _height = rH;
        }

        if (boundsChanged)
        {
            ContentArea = new (ContentArea.X, ContentArea.Y, canSizeW ? rW : ContentArea.Width, canSizeH ? rH : ContentArea.Height);
        }

        return boundsChanged;
    }

    /// <summary>If <paramref name="autoSize"/> is true, resizes the view.</summary>
    /// <param name="autoSize"></param>
    /// <returns></returns>
    private bool ResizeView (bool autoSize)
    {
        if (!autoSize)
        {
            return false;
        }

        var boundsChanged = true;
        Size newFrameSize = GetAutoSize ();

        if (IsInitialized && newFrameSize != Frame.Size)
        {
            if (ValidatePosDim)
            {
                // BUGBUG: This ain't right, obviously.  We need to figure out how to handle this.
                boundsChanged = ResizeBoundsToFit (newFrameSize);
            }
            else
            {
                Height = newFrameSize.Height;
                Width = newFrameSize.Width;
            }
        }

        return boundsChanged;
    }

    // Diagnostics to highlight when Width or Height is read before the view has been initialized
    private Dim VerifyIsInitialized (Dim dim, string member)
    {
#if DEBUG
        if (LayoutStyle == LayoutStyle.Computed && !IsInitialized)
        {
            Debug.WriteLine (
                             $"WARNING: \"{this}\" has not been initialized; {member} is indeterminate: {dim}. This is potentially a bug."
                            );
        }
#endif // DEBUG		
        return dim;
    }

    // Diagnostics to highlight when X or Y is read before the view has been initialized
    private Pos VerifyIsInitialized (Pos pos, string member)
    {
#if DEBUG
        if (LayoutStyle == LayoutStyle.Computed && !IsInitialized)
        {
            Debug.WriteLine (
                             $"WARNING: \"{this}\" has not been initialized; {member} is indeterminate {pos}. This is potentially a bug."
                            );
        }
#endif // DEBUG
        return pos;
    }
}

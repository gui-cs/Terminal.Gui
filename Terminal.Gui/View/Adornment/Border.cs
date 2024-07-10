#define SUBVIEW_BASED_BORDER 
namespace Terminal.Gui;

/// <summary>The Border for a <see cref="View"/>.</summary>
/// <remarks>
///     <para>
///         Renders a border around the view with the <see cref="View.Title"/>. A border using <see cref="LineStyle"/>
///         will be drawn on the sides of <see cref="Thickness"/> that are greater than zero.
///     </para>
///     <para>
///         The <see cref="View.Title"/> of <see cref="Adornment.Parent"/> will be drawn based on the value of
///         <see cref="Thickness.Top"/>:
///     </para>
///     <para>
///         If <c>1</c>:
///         <code>
/// ┌┤1234├──┐
/// │        │
/// └────────┘
/// </code>
///     </para>
///     <para>
///         If <c>2</c>:
///         <code>
///  ┌────┐
/// ┌┤1234├──┐
/// │        │
/// └────────┘
/// </code>
///     </para>
///     <para>
///         If <c>3</c>:
///         <code>
///  ┌────┐
/// ┌┤1234├──┐
/// │└────┘  │
/// │        │
/// └────────┘
/// </code>
///     </para>
///     <para/>
///     <para>See the <see cref="Adornment"/> class.</para>
/// </remarks>
public class Border : Adornment
{
    private LineStyle? _lineStyle;

    /// <inheritdoc/>
    public Border ()
    { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <inheritdoc/>
    public Border (View parent) : base (parent)
    {
        /* Do nothing; View.CreateAdornment requires a constructor that takes a parent */
        Parent = parent;
        Application.GrabbingMouse += Application_GrabbingMouse;
        Application.UnGrabbingMouse += Application_UnGrabbingMouse;

        HighlightStyle |= HighlightStyle.Pressed;
        Highlight += Border_Highlight;
    }

#if SUBVIEW_BASED_BORDER
    public Line TopLeft { get; internal set; }
    public Line TopRight { get; internal set; }
    public Line Left { get; internal set; }
    public Line Right { get; internal set; }
    public Line Bottom { get; internal set; }
    public View TitleLabel { get; internal set; }


    /// <summary>
    ///    The close button for the border. Set to <see cref="View.Visible"/>, to <see langword="true"/> to enable.
    /// </summary>
    public Button CloseButton { get; internal set; }
#endif

    /// <inheritdoc/>
    public override void BeginInit ()
    {
#if HOVER
        // TOOD: Hack - make Arragnement overidable
        if ((Parent?.Arrangement & ViewArrangement.Movable) != 0)
        {
            HighlightStyle |= HighlightStyle.Hover;
        }
#endif

        base.BeginInit ();

#if SUBVIEW_BASED_BORDER
        SuperViewRendersLineCanvas = false;

        if (Parent is { })
        {
            TopLeft = new ()
            {
                Orientation = Orientation.Horizontal,
                SuperViewRendersLineCanvas = true,
                BorderStyle = LineStyle
            };
            Add (TopLeft);

            TopRight = new ()
            {
                Orientation = Orientation.Horizontal,
                SuperViewRendersLineCanvas = true,
                BorderStyle = LineStyle
            };
            Add (TopRight);

            Left = new ()
            {
                Orientation = Orientation.Vertical,
                SuperViewRendersLineCanvas = true,
                BorderStyle = LineStyle,
            };
            Add (Left);

            Right = new ()
            {
                Orientation = Orientation.Vertical,
                SuperViewRendersLineCanvas = true,
                BorderStyle = LineStyle,
            };

            Add (Right);

            Bottom = new ()
            {
                Orientation = Orientation.Horizontal,
                SuperViewRendersLineCanvas = true,
                BorderStyle = LineStyle,
            };
            Add (Bottom);

            TitleLabel = new View ()
            {
                Id = "TitleLabel",
                Text = Parent.Title,
                CanFocus = false,
                SuperViewRendersLineCanvas = true,
                TextAlignment = Alignment.Center,
                VerticalTextAlignment = Alignment.Center,
            };
            Add (TitleLabel);

            //CloseButton = new Button ()
            //{
            //    Text = "X",
            //    CanFocus = true,
            //    Visible = false,
            //    NoPadding = true,
            //    NoDecorations = true,
            //    WantContinuousButtonPressed = true,
            //    SuperViewRendersLineCanvas = true,
            //};

            //CloseButton.Accept += (s, e) =>
            //{
            //    e.Handled = Parent.InvokeCommand (Command.QuitToplevel) == true;
            //};
            //Add (CloseButton);

            SetSubviewLayout ();
        }
#endif
    }

    private void SetSubviewLayout ()
    {
        TopLeft.X = Pos.Func (() => Thickness.Left / 2);
        TopLeft.Y = Pos.Func (() => Thickness.Top / 2);
        TopLeft.Width = 2;
        TopLeft.Height = 1;
        TopLeft.Visible = Thickness.Top > 0;

        TopRight.X = Pos.Right (TitleLabel);
        TopRight.Y = Pos.Func (() => Thickness.Top / 2);
        TopRight.Width = Dim.Fill () - Dim.Func (() => Thickness.Right / 2);
        TopRight.Height = 1;
        TopRight.Visible = Thickness.Top > 0;

        Left.X = Pos.Func (() => Thickness.Left / 2);
        Left.Y = Pos.Top (TopRight);
        Left.Height = Dim.Fill () - Dim.Func (() => Thickness.Bottom / 2);
        Left.Width = 1;
        Left.Visible = Thickness.Left > 0;

        Right.X = Pos.Right (TopRight) - 1;
        Right.Y = Pos.Top (TopRight);
        Right.Height = Dim.Fill () - Dim.Func (() => Thickness.Bottom / 2);
        Right.Width = 1;
        Right.Visible = Thickness.Right > 0;

        Bottom.X = Pos.Func (() => Thickness.Left / 2);
        Bottom.Y = Pos.Bottom (Left) - 1;
        Bottom.Width = Dim.Fill () - Dim.Func (() => Thickness.Right / 2);
        Bottom.Height = 1;
        Bottom.Visible = Thickness.Bottom > 0;

        TitleLabel.X = Pos.Right (TopLeft);
        TitleLabel.Y = Pos.Func (() => Thickness.Top / 2 - TitleLabel.Frame.Height / 2);
        TitleLabel.Height = _settings.FastHasFlags (BorderSettings.Title) ? Thickness.Top : 0;
        TitleLabel.Width = Dim.Func (() => _settings.FastHasFlags (BorderSettings.Title) ? TitleLabel.TextFormatter.GetAutoSize ().Width + TitleLabel.GetAdornmentsThickness ().Horizontal : 0);
        TitleLabel.Border.Thickness = new (1, 0, 1, 0);
        TitleLabel.Border.LineStyle = LineStyle.Dotted;
        TitleLabel.SuperViewRendersLineCanvas = true;

        //CloseButton.X = Pos.Left (Right) - 1;
        //CloseButton.Y = Pos.Func (() => Thickness.Top / 2);
        //CloseButton.Width = 1;
        //CloseButton.Height = 1;
        //CloseButton.Visible = false;
    }

    /// <summary>
    ///     The color scheme for the Border. If set to <see langword="null"/>, gets the <see cref="Adornment.Parent"/>
    ///     scheme. color scheme.
    /// </summary>
    public override ColorScheme ColorScheme
    {
        get
        {
            if (base.ColorScheme is { })
            {
                return base.ColorScheme;
            }

            return Parent?.ColorScheme;
        }
        set
        {
            base.ColorScheme = value;
#if SUBVIEW_BASED_BORDER
            if (IsInitialized && TopLeft is { })
            {
                TopLeft.ColorScheme = value;
                TopRight.ColorScheme = value;
                Left.ColorScheme = value;
                Right.ColorScheme = value;
                Bottom.ColorScheme = value;
                TitleLabel.ColorScheme = value;
            }
#endif
            Parent?.SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     Sets the style of the border by changing the <see cref="Thickness"/>. This is a helper API for setting the
    ///     <see cref="Thickness"/> to <c>(1,1,1,1)</c> and setting the line style of the views that comprise the border. If
    ///     set to <see cref="LineStyle.None"/> no border will be drawn.
    /// </summary>
    public LineStyle LineStyle
    {
        get
        {
            if (_lineStyle.HasValue)
            {
                return _lineStyle.Value;
            }

            // TODO: Make Border.LineStyle inherit from the SuperView hierarchy
            // TODO: Right now, Window and FrameView use CM to set BorderStyle, which negates
            // TODO: all this.
            return Parent.SuperView?.BorderStyle ?? LineStyle.None;
        }
        set
        {
            if (_lineStyle == value)
            {
                return;
            }

            _lineStyle = value;
#if SUBVIEW_BASED_BORDER
            if (IsInitialized && Parent is { } && TopLeft is { })
            {
                TopLeft.BorderStyle = value;
                TopRight.BorderStyle = value;
                Left.BorderStyle = value;
                Right.BorderStyle = value;
                Bottom.BorderStyle = value;
            }
#endif
        }
    }

    private BorderSettings _settings = BorderSettings.Title;

    /// <summary>
    ///     Gets or sets the settings for the border.
    /// </summary>
    public BorderSettings Settings
    {
        get => _settings;
        set
        {
            if (value == _settings)
            {
                return;
            }

            _settings = value;

            SetSubviewLayout ();

            Parent?.SetNeedsDisplay ();
        }
    }

    #region Mouse Support

    private Color? _savedForeColor;

    private void Border_Highlight (object sender, CancelEventArgs<HighlightStyle> e)
    {
        if (!Parent.Arrangement.HasFlag (ViewArrangement.Movable))
        {
            e.Cancel = true;

            return;
        }

        if (e.NewValue.HasFlag (HighlightStyle.Pressed))
        {
            if (!_savedForeColor.HasValue)
            {
                _savedForeColor = ColorScheme.Normal.Foreground;
            }

            var cs = new ColorScheme (ColorScheme)
            {
                Normal = new (ColorScheme.Normal.Foreground.GetHighlightColor (), ColorScheme.Normal.Background)
            };
            ColorScheme = cs;
        }
#if HOVER
        else if (e.HighlightStyle.HasFlag (HighlightStyle.Hover))
        {
            if (!_savedHighlightLineStyle.HasValue)
            {
                _savedHighlightLineStyle = Parent?.BorderStyle ?? LineStyle;
            }
            BorderStyle = BorderStyle.Double;
        }
#endif

        if (e.NewValue == HighlightStyle.None && _savedForeColor.HasValue)
        {
            var cs = new ColorScheme (ColorScheme)
            {
                Normal = new (_savedForeColor.Value, ColorScheme.Normal.Background)
            };
            ColorScheme = cs;
        }

        Parent?.SetNeedsDisplay ();
        e.Cancel = true;
    }

    private Point? _dragPosition;
    private Point _startGrabPoint;

    /// <inheritdoc/>
    protected internal override bool OnMouseEvent (MouseEvent mouseEvent)
    {
        if (base.OnMouseEvent (mouseEvent))
        {
            return true;
        }

        if (!Parent.CanFocus)
        {
            return false;
        }

        if (!Parent.Arrangement.HasFlag (ViewArrangement.Movable))
        {
            return false;
        }

        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/3312
        if (!_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            Parent.SetFocus ();
            Application.BringOverlappedTopToFront ();

            // Only start grabbing if the user clicks in the Thickness area
            // Adornment.Contains takes Parent SuperView=relative coords.
            if (Contains (new (mouseEvent.Position.X + Parent.Frame.X + Frame.X, mouseEvent.Position.Y + Parent.Frame.Y + Frame.Y)))
            {
                // Set the start grab point to the Frame coords
                _startGrabPoint = new (mouseEvent.Position.X + Frame.X, mouseEvent.Position.Y + Frame.Y);
                _dragPosition = mouseEvent.Position;
                Application.GrabMouse (this);

                SetHighlight (HighlightStyle);
            }

            return true;
        }

        if (mouseEvent.Flags is (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
        {
            if (Application.MouseGrabView == this && _dragPosition.HasValue)
            {
                if (Parent.SuperView is null)
                {
                    // Redraw the entire app window.
                    Application.Top.SetNeedsDisplay ();
                }
                else
                {
                    Parent.SuperView.SetNeedsDisplay ();
                }

                _dragPosition = mouseEvent.Position;

                Point parentLoc = Parent.SuperView?.ScreenToViewport (new (mouseEvent.ScreenPosition.X, mouseEvent.ScreenPosition.Y))
                                  ?? mouseEvent.ScreenPosition;

                GetLocationEnsuringFullVisibility (
                                                   Parent,
                                                   parentLoc.X - _startGrabPoint.X,
                                                   parentLoc.Y - _startGrabPoint.Y,
                                                   out int nx,
                                                   out int ny,
                                                   out _
                                                  );

                Parent.X = nx;
                Parent.Y = ny;

                return true;
            }
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) && _dragPosition.HasValue)
        {
            _dragPosition = null;
            Application.UngrabMouse ();
            SetHighlight (HighlightStyle.None);

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        Application.GrabbingMouse -= Application_GrabbingMouse;
        Application.UnGrabbingMouse -= Application_UnGrabbingMouse;

        _dragPosition = null;
        base.Dispose (disposing);
    }

    private void Application_GrabbingMouse (object sender, GrabMouseEventArgs e)
    {
        if (Application.MouseGrabView == this && _dragPosition.HasValue)
        {
            e.Cancel = true;
        }
    }

    private void Application_UnGrabbingMouse (object sender, GrabMouseEventArgs e)
    {
        if (Application.MouseGrabView == this && _dragPosition.HasValue)
        {
            e.Cancel = true;
        }
    }

    #endregion Mouse Support

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {

        // TODO: This should not be done on each draw?
        if (Settings.FastHasFlags (BorderSettings.Gradient))
        {
            SetupGradientLineCanvas (Parent.LineCanvas, ViewportToScreen (viewport));
        }
        else
        {
            Parent.LineCanvas.Fill = null;
        }
        base.OnDrawContent (viewport);
    }


    private void SetupGradientLineCanvas (LineCanvas lc, Rectangle rect)
    {
        GetAppealingGradientColors (out List<Color> stops, out List<int> steps);

        var g = new Gradient (stops, steps);

        var fore = new GradientFill (rect, g, GradientDirection.Diagonal);
        var back = new SolidFill (GetNormalColor ().Background);

        lc.Fill = new (fore, back);
    }

    private static void GetAppealingGradientColors (out List<Color> stops, out List<int> steps)
    {
        // Define the colors of the gradient stops with more appealing colors
        stops = new ()
        {
            new (0, 128, 255), // Bright Blue
            new (0, 255, 128), // Bright Green
            new (255, 255), // Bright Yellow
            new (255, 128), // Bright Orange
            new (255, 0, 128) // Bright Pink
        };

        // Define the number of steps between each color for smoother transitions
        // If we pass only a single value then it will assume equal steps between all pairs
        steps = new () { 15 };
    }
}

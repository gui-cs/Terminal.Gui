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

    public View TitleLabel { get; internal set; }

    /// <summary>
    ///    The close button for the border. Set to <see cref="View.Visible"/>, to <see langword="true"/> to enable.
    /// </summary>
    public Button CloseButton { get; internal set; }

    public Line TopLeft { get; internal set; }
    public Line TopRight { get; internal set; }
    public Line Left { get; internal set; }
    public Line Right { get; internal set; }
    public Line Bottom { get; internal set; }

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

        SuperViewRendersLineCanvas = false;

        if (Parent is { })
        {
            TopLeft = new ()
            {
                Orientation = Orientation.Horizontal,
                SuperViewRendersLineCanvas = true,
                LineStyle = LineStyle
            };
            Add (TopLeft);

            TopRight = new ()
            {
                Orientation = Orientation.Horizontal,
                SuperViewRendersLineCanvas = true,
                LineStyle = LineStyle
            };
            Add (TopRight);

            Left = new ()
            {
                Orientation = Orientation.Vertical,
                SuperViewRendersLineCanvas = true,
                LineStyle = LineStyle,
            };
            Add (Left);

            Right = new ()
            {
                Orientation = Orientation.Vertical,
                SuperViewRendersLineCanvas = true,
                LineStyle = LineStyle,
            };

            Add (Right);

            Bottom = new ()
            {
                Orientation = Orientation.Horizontal,
                SuperViewRendersLineCanvas = true,
                LineStyle = LineStyle,
            };
            Add (Bottom);

            TitleLabel = new View ()
            {
                Id = "TitleLabel",
                Text = Parent.Title,
                CanFocus = false,
                SuperViewRendersLineCanvas = true,
                TextAlignment = TextAlignment.Centered,
                VerticalTextAlignment = VerticalTextAlignment.Middle,
            };
            //TitleLabel.Border.Thickness = new (1);
            //TitleLabel.Border.LineStyle = LineStyle.Dotted;
            Add (TitleLabel);

            CloseButton = new Button ()
            {
                Text = "X",
                CanFocus = true,
                Visible = false,
                NoPadding = true,
                NoDecorations = true,
                WantContinuousButtonPressed = true,
                SuperViewRendersLineCanvas = true,
            };

            CloseButton.Accept += (s, e) =>
            {
                e.Cancel = Parent.InvokeCommand (Command.QuitToplevel) == true;
            };
            Add (CloseButton);

            SetSubviewLayout ();
        }

        return;

        void SetSubviewLayout ()
        {
            TopLeft.X = Pos.Function (() => Thickness.Left / 2);
            TopLeft.Y = Pos.Function (() => Thickness.Top / 2);
            TopLeft.Width = 2;
            TopLeft.Height = 1;

            TopRight.X = Pos.Right (TitleLabel);
            TopRight.Y = Pos.Function (() => Thickness.Top / 2);
            TopRight.Width = Dim.Fill () - Dim.Function (() => Thickness.Right / 2);
            TopRight.Height = 1;

            Left.X = Pos.Function (() => Thickness.Left / 2);
            Left.Y = Pos.Top (TopRight);
            Left.Height = Dim.Fill () - Dim.Function (() => Thickness.Bottom / 2);
            Left.Width = 1;

            Right.X = Pos.Right (TopRight) - 1;
            Right.Y = Pos.Top (TopRight);
            Right.Height = Dim.Fill () - Dim.Function (() => Thickness.Bottom / 2);
            Right.Width = 1;

            Bottom.X = Pos.Function (() => Thickness.Left / 2);
            Bottom.Y = Pos.Bottom (Left) - 1;
            Bottom.Width = Dim.Fill () - Dim.Function (() => Thickness.Right / 2);
            Bottom.Height = 1;

            TitleLabel.X = Pos.Right (TopLeft);
            TitleLabel.Y = Pos.Function (() => Thickness.Top / 2 - TitleLabel.Frame.Height / 2);
            TitleLabel.Height = Thickness.Top;
            TitleLabel.Width = Dim.Function (() => TitleLabel.Text.GetColumns() + TitleLabel.GetAdornmentsThickness ().Horizontal);

            //if (Parent.Id == "TitleLabel")
            //{
            //    TitleLabel.Visible = false;
            //    TitleLabel.X = Pos.Right (TopLeft);
            //    TitleLabel.Width = 0;
            //}

            CloseButton.X = Pos.Left (Right) - 1;
            CloseButton.Y = Pos.Function (() => Thickness.Top / 2);
            CloseButton.Width = 1;
            CloseButton.Height = 1;
        }
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
            if (IsInitialized)
            {
                TopLeft.ColorScheme = value;
                TopRight.ColorScheme = value;
                Left.ColorScheme = value;
                Right.ColorScheme = value;
                Bottom.ColorScheme = value;
                TitleLabel.ColorScheme = value;
            }
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

            if (IsInitialized)
            {
                TopLeft.LineStyle = value;
                TopRight.LineStyle = value;
                Left.LineStyle = value;
                Right.LineStyle = value;
                Bottom.LineStyle = value;
            }
        }
    }

    #region Mouse Support

    private Color? _savedForeColor;

    private void Border_Highlight (object sender, HighlightEventArgs e)
    {
        if (!Parent.Arrangement.HasFlag (ViewArrangement.Movable))
        {
            e.Cancel = true;
            return;
        }

        if (e.HighlightStyle.HasFlag (HighlightStyle.Pressed))
        {
            if (!_savedForeColor.HasValue)
            {
                _savedForeColor = ColorScheme.Normal.Foreground;
            }

            ColorScheme cs = new ColorScheme (ColorScheme)
            {
                Normal = new Attribute (ColorScheme.Normal.Foreground.GetHighlightColor (), ColorScheme.Normal.Background)
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
            LineStyle = LineStyle.Double;
        }
#endif

        if (e.HighlightStyle == HighlightStyle.None && _savedForeColor.HasValue)
        {
            ColorScheme cs = new ColorScheme (ColorScheme)
            {
                Normal = new Attribute (_savedForeColor.Value, ColorScheme.Normal.Background)
            };
            ColorScheme = cs;
        }
        Parent?.SetNeedsDisplay ();
        e.Cancel = true;
    }

    private Point? _dragPosition;
    private Point _startGrabPoint;

    /// <inheritdoc />
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
            if (Contains (mouseEvent.X + Parent.Frame.X + Frame.X, mouseEvent.Y + Parent.Frame.Y + Frame.Y))
            {
                // Set the start grab point to the Frame coords
                _startGrabPoint = new (mouseEvent.X + Frame.X, mouseEvent.Y + Frame.Y);
                _dragPosition = new (mouseEvent.X, mouseEvent.Y);
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

                _dragPosition = new Point (mouseEvent.X, mouseEvent.Y);

                Point parentLoc = Parent.SuperView?.ScreenToViewport (mouseEvent.ScreenPosition.X, mouseEvent.ScreenPosition.Y) ?? mouseEvent.ScreenPosition;

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
        base.OnDrawContent (viewport);


        //// TODO: This should be moved to LineCanvas as a new BorderStyle.Ruler
        //if (View.Diagnostics.HasFlag (ViewDiagnosticFlags.Ruler))
        //{
        //    // Top
        //    var hruler = new Ruler { Length = screenBounds.Width, Orientation = Orientation.Horizontal };

        //    if (drawTop)
        //    {
        //        hruler.Draw (new (screenBounds.X, screenBounds.Y));
        //    }

        //    // Redraw title 
        //    if (drawTop && maxTitleWidth > 0 && !string.IsNullOrEmpty (Parent?.Title))
        //    {
        //        Parent.TitleTextFormatter.Draw (
        //                                        new (borderBounds.X + 2, titleY, maxTitleWidth, 1),
        //                                        Parent.HasFocus ? Parent.GetFocusColor () : Parent.GetNormalColor (),
        //                                        Parent.HasFocus ? Parent.GetFocusColor () : Parent.GetNormalColor ());
        //    }

        //    //Left
        //    var vruler = new Ruler { Length = screenBounds.Height - 2, Orientation = Orientation.Vertical };

        //    if (drawLeft)
        //    {
        //        vruler.Draw (new (screenBounds.X, screenBounds.Y + 1), 1);
        //    }

        //    // Bottom
        //    if (drawBottom)
        //    {
        //        hruler.Draw (new (screenBounds.X, screenBounds.Y + screenBounds.Height - 1));
        //    }

        //    // Right
        //    if (drawRight)
        //    {
        //        vruler.Draw (new (screenBounds.X + screenBounds.Width - 1, screenBounds.Y + 1), 1);
        //    }
        //}

    }
}

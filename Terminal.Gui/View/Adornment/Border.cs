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
    private Line _left;

    /// <summary>
    ///    The close button for the border. Set to <see cref="View.Visible"/>, to <see langword="true"/> to enable.
    /// </summary>
    public Button CloseButton { get; internal set; }
#endif

    /// <inheritdoc/>
    public override void BeginInit ()
    {
#if HOVER
        // TOOD: Hack - make Arrangement overridable
        if ((Parent?.Arrangement & ViewArrangement.Movable) != 0)
        {
            HighlightStyle |= HighlightStyle.Hover;
        }
#endif

        base.BeginInit ();

#if SUBVIEW_BASED_BORDER
        if (Parent is { })
        {
            // Left
            _left = new ()
            {
                Orientation = Orientation.Vertical,
            };
            Add (_left);

            CloseButton = new Button ()
            {
                Text = "X",
                CanFocus = true,
                Visible = false,
            };
            CloseButton.Accept += (s, e) =>
            {
                e.Cancel = Parent.InvokeCommand (Command.QuitToplevel) == true;
            };
            Add (CloseButton);

            LayoutStarted += OnLayoutStarted;
    }
#endif
    }

#if SUBVIEW_BASED_BORDER
    private void OnLayoutStarted (object sender, LayoutEventArgs e)
    {
        _left.Border.LineStyle = LineStyle;

        _left.X = Thickness.Left - 1;
        _left.Y = Thickness.Top - 1;
        _left.Width = 1;
        _left.Height = Height;

        CloseButton.X = Pos.AnchorEnd (Thickness.Right / 2 + 1) -
                        (Pos.Right (CloseButton) -
                         Pos.Left (CloseButton));
        CloseButton.Y = 0;
}
#endif

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
            Parent?.SetNeedsDisplay ();
        }
    }

    internal Rectangle GetBorderRectangle ()
    {
        Rectangle screenRect = ViewportToScreen (Viewport);
        return new (
                    screenRect.X + Math.Max (0, Thickness.Left - 1),
                    screenRect.Y + Math.Max (0, Thickness.Top - 1),
                    Math.Max (
                              0,
                              screenRect.Width
                              - Math.Max (
                                          0,
                                          Math.Max (0, Thickness.Left - 1)
                                          + Math.Max (0, Thickness.Right - 1)
                                         )
                             ),
                    Math.Max (
                              0,
                              screenRect.Height
                              - Math.Max (
                                          0,
                                          Math.Max (0, Thickness.Top - 1)
                                          + Math.Max (0, Thickness.Bottom - 1)
                                         )
                             )
                   );
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
        set => _lineStyle = value;
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
            LineStyle = LineStyle.Double;
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

        // BUGBUG: Shouldn't non-focusable views be draggable??
        //if (!Parent.CanFocus)
        //{
        //    return false;
        //}

        if (!Parent.Arrangement.HasFlag (ViewArrangement.Movable))
        {
            return false;
        }

        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/3312
        if (!_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            Parent.SetFocus ();
            ApplicationOverlapped.BringOverlappedTopToFront ();

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
        base.OnDrawContent (viewport);

        if (Thickness == Thickness.Empty)
        {
            return;
        }

        //Driver.SetAttribute (Colors.ColorSchemes ["Error"].Normal);
        Rectangle screenBounds = ViewportToScreen (viewport);

        //OnDrawSubviews (bounds); 

        // TODO: v2 - this will eventually be two controls: "BorderView" and "Label" (for the title)

        // The border adornment (and title) are drawn at the outermost edge of border;
        // For Border
        // ...thickness extends outward (border/title is always as far in as possible)
        // PERF: How about a call to Rectangle.Offset?

        Rectangle borderBounds = GetBorderRectangle ();
        int topTitleLineY = borderBounds.Y;
        int titleY = borderBounds.Y;
        var titleBarsLength = 0; // the little vertical thingies

        int maxTitleWidth = Math.Max (
                                      0,
                                      Math.Min (
                                                Parent.TitleTextFormatter.FormatAndGetSize ().Width,
                                                Math.Min (screenBounds.Width - 4, borderBounds.Width - 4)
                                               )
                                     );

        Parent.TitleTextFormatter.ConstrainToSize = new (maxTitleWidth, 1);

        int sideLineLength = borderBounds.Height;
        bool canDrawBorder = borderBounds is { Width: > 0, Height: > 0 };

        if (Settings.FastHasFlags (BorderSettings.Title))
        {
            if (Thickness.Top == 2)
            {
                topTitleLineY = borderBounds.Y - 1;
                titleY = topTitleLineY + 1;
                titleBarsLength = 2;
            }

            // ┌────┐
            //┌┘View└
            //│
            if (Thickness.Top == 3)
            {
                topTitleLineY = borderBounds.Y - (Thickness.Top - 1);
                titleY = topTitleLineY + 1;
                titleBarsLength = 3;
                sideLineLength++;
            }

            // ┌────┐
            //┌┘View└
            //│
            if (Thickness.Top > 3)
            {
                topTitleLineY = borderBounds.Y - 2;
                titleY = topTitleLineY + 1;
                titleBarsLength = 3;
                sideLineLength++;
            }
        }

        if (canDrawBorder && Thickness.Top > 0 && maxTitleWidth > 0 && Settings.FastHasFlags (BorderSettings.Title) && !string.IsNullOrEmpty (Parent?.Title))
        {
            Attribute focus = Parent.GetNormalColor ();

            if (Parent.SuperView is { } && Parent.SuperView?.Subviews!.Count (s => s.CanFocus) > 1)
            {
                // Only use focus color if there are multiple focusable views
                focus = Parent.GetFocusColor ();
            }

            Parent.TitleTextFormatter.Draw (
                                            new (borderBounds.X + 2, titleY, maxTitleWidth, 1),
                                            Parent.HasFocus ? focus : Parent.GetNormalColor (),
                                            Parent.HasFocus ? focus : Parent.GetHotNormalColor ());
        }

        if (canDrawBorder && LineStyle != LineStyle.None)
        {
            LineCanvas lc = Parent?.LineCanvas;

            bool drawTop = Thickness.Top > 0 && Frame.Width > 1 && Frame.Height >= 1;
            bool drawLeft = Thickness.Left > 0 && (Frame.Height > 1 || Thickness.Top == 0);
            bool drawBottom = Thickness.Bottom > 0 && Frame.Width > 1 && Frame.Height > 1;
            bool drawRight = Thickness.Right > 0 && (Frame.Height > 1 || Thickness.Top == 0);

            Attribute prevAttr = Driver.GetAttribute ();

            if (ColorScheme is { })
            {
                Driver.SetAttribute (GetNormalColor ());
            }
            else
            {
                Driver.SetAttribute (Parent.GetNormalColor ());
            }

            if (drawTop)
            {
                // ╔╡Title╞═════╗
                // ╔╡╞═════╗
                if (borderBounds.Width < 4 || !Settings.FastHasFlags (BorderSettings.Title) || string.IsNullOrEmpty (Parent?.Title))
                {
                    // ╔╡╞╗ should be ╔══╗
                    lc.AddLine (
                                new (borderBounds.Location.X, titleY),
                                borderBounds.Width,
                                Orientation.Horizontal,
                                LineStyle,
                                Driver.GetAttribute ()
                               );
                }
                else
                {
                    // ┌────┐
                    //┌┘View└
                    //│
                    if (Thickness.Top == 2)
                    {
                        lc.AddLine (
                                    new (borderBounds.X + 1, topTitleLineY),
                                    Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                    Orientation.Horizontal,
                                    LineStyle,
                                    Driver.GetAttribute ()
                                   );
                    }

                    // ┌────┐
                    //┌┘View└
                    //│
                    if (borderBounds.Width >= 4 && Thickness.Top > 2)
                    {
                        lc.AddLine (
                                    new (borderBounds.X + 1, topTitleLineY),
                                    Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                    Orientation.Horizontal,
                                    LineStyle,
                                    Driver.GetAttribute ()
                                   );

                        lc.AddLine (
                                    new (borderBounds.X + 1, topTitleLineY + 2),
                                    Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                    Orientation.Horizontal,
                                    LineStyle,
                                    Driver.GetAttribute ()
                                   );
                    }

                    // ╔╡Title╞═════╗
                    // Add a short horiz line for ╔╡
                    lc.AddLine (
                                new (borderBounds.Location.X, titleY),
                                2,
                                Orientation.Horizontal,
                                LineStyle,
                                Driver.GetAttribute ()
                               );

                    // Add a vert line for ╔╡
                    lc.AddLine (
                                new (borderBounds.X + 1, topTitleLineY),
                                titleBarsLength,
                                Orientation.Vertical,
                                LineStyle.Single,
                                Driver.GetAttribute ()
                               );

                    // Add a vert line for ╞
                    lc.AddLine (
                                new (
                                     borderBounds.X
                                     + 1
                                     + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2)
                                     - 1,
                                     topTitleLineY
                                    ),
                                titleBarsLength,
                                Orientation.Vertical,
                                LineStyle.Single,
                                Driver.GetAttribute ()
                               );

                    // Add the right hand line for ╞═════╗
                    lc.AddLine (
                                new (
                                     borderBounds.X
                                     + 1
                                     + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2)
                                     - 1,
                                     titleY
                                    ),
                                borderBounds.Width - Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                Orientation.Horizontal,
                                LineStyle,
                                Driver.GetAttribute ()
                               );
                }
            }

#if !SUBVIEW_BASED_BORDER

            if (drawLeft)
            {
                lc.AddLine (
                            new (borderBounds.Location.X, titleY),
                            sideLineLength,
                            Orientation.Vertical,
                            LineStyle,
                            Driver.GetAttribute ()
                           );
            }
#endif

            if (drawBottom)
            {
                lc.AddLine (
                            new (borderBounds.X, borderBounds.Y + borderBounds.Height - 1),
                            borderBounds.Width,
                            Orientation.Horizontal,
                            LineStyle,
                            Driver.GetAttribute ()
                           );
            }

            if (drawRight)
            {
                lc.AddLine (
                            new (borderBounds.X + borderBounds.Width - 1, titleY),
                            sideLineLength,
                            Orientation.Vertical,
                            LineStyle,
                            Driver.GetAttribute ()
                           );
            }

            Driver.SetAttribute (prevAttr);

            // TODO: This should be moved to LineCanvas as a new BorderStyle.Ruler
            if (Diagnostics.HasFlag (ViewDiagnosticFlags.Ruler))
            {
                // Top
                var hruler = new Ruler { Length = screenBounds.Width, Orientation = Orientation.Horizontal };

                if (drawTop)
                {
                    hruler.Draw (new (screenBounds.X, screenBounds.Y));
                }

                // Redraw title 
                if (drawTop && maxTitleWidth > 0 && Settings.FastHasFlags (BorderSettings.Title))
                {
                    Parent.TitleTextFormatter.Draw (
                                                    new (borderBounds.X + 2, titleY, maxTitleWidth, 1),
                                                    Parent.HasFocus ? Parent.GetFocusColor () : Parent.GetNormalColor (),
                                                    Parent.HasFocus ? Parent.GetFocusColor () : Parent.GetNormalColor ());
                }

                //Left
                var vruler = new Ruler { Length = screenBounds.Height - 2, Orientation = Orientation.Vertical };

                if (drawLeft)
                {
                    vruler.Draw (new (screenBounds.X, screenBounds.Y + 1), 1);
                }

                // Bottom
                if (drawBottom)
                {
                    hruler.Draw (new (screenBounds.X, screenBounds.Y + screenBounds.Height - 1));
                }

                // Right
                if (drawRight)
                {
                    vruler.Draw (new (screenBounds.X + screenBounds.Width - 1, screenBounds.Y + 1), 1);
                }
            }

            // TODO: This should not be done on each draw?
            if (Settings.FastHasFlags (BorderSettings.Gradient))
            {
                SetupGradientLineCanvas (lc, screenBounds);
            }
            else
            {
                lc.Fill = null;
            }
        }
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
        stops = new()
        {
            new (0, 128, 255), // Bright Blue
            new (0, 255, 128), // Bright Green
            new (255, 255), // Bright Yellow
            new (255, 128), // Bright Orange
            new (255, 0, 128) // Bright Pink
        };

        // Define the number of steps between each color for smoother transitions
        // If we pass only a single value then it will assume equal steps between all pairs
        steps = new() { 15 };
    }
}

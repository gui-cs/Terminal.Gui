#nullable enable
using System.Diagnostics;

namespace Terminal.Gui.ViewBase;

/// <summary>The Border for a <see cref="View"/>. Accessed via <see cref="View.Border"/></summary>
/// <remarks>
///     <para>
///         Renders a border around the view with the <see cref="View.Title"/>. A border using <see cref="LineStyle"/>
///         will be drawn on the sides of <see cref="Drawing.Thickness"/> that are greater than zero.
///     </para>
///     <para>
///         The <see cref="View.Title"/> of <see cref="Adornment.Parent"/> will be drawn based on the value of
///         <see cref="Drawing.Thickness.Top"/>:
///         <example>
///             // If Thickness.Top is 1:
///             ┌┤1234├──┐
///             │        │
///             └────────┘
///             // If Thickness.Top is 2:
///              ┌────┐
///             ┌┤1234├──┐
///             │        │
///             └────────┘
///             If Thickness.Top is 3:
///              ┌────┐
///             ┌┤1234├──┐
///             │└────┘  │
///             │        │
///             └────────┘
///         </example>
///     </para>
///     <para>
///         The Border provides keyboard and mouse support for moving and resizing the View. See <see cref="ViewArrangement"/>.
///     </para>
/// </remarks>
public partial class Border : Adornment
{
    private LineStyle? _lineStyle;

    /// <inheritdoc/>
    public Border ()
    { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <inheritdoc/>
    public Border (View parent) : base (parent)
    {
        Parent = parent;
        CanFocus = false;
        TabStop = TabBehavior.TabGroup;

        Application.MouseGrabHandler.GrabbingMouse += Application_GrabbingMouse;
        Application.MouseGrabHandler.UnGrabbingMouse += Application_UnGrabbingMouse;

        ThicknessChanged += OnThicknessChanged;
    }

    // TODO: Move DrawIndicator out of Border and into View
    private void OnThicknessChanged (object? sender, EventArgs e)
    {
        if (IsInitialized)
        {
            ShowHideDrawIndicator ();
        }
    }

    private void ShowHideDrawIndicator ()
    {
        if (View.Diagnostics.HasFlag (ViewDiagnosticFlags.DrawIndicator) && Thickness != Thickness.Empty)
        {
            if (DrawIndicator is null)
            {
                DrawIndicator = new ()
                {
                    Id = "DrawIndicator",
                    X = 1,
                    Style = new SpinnerStyle.Dots2 (),
                    SpinDelay = 0,
                    Visible = false
                };
                Add (DrawIndicator);
            }
        }
        else if (DrawIndicator is { })
        {
            Remove (DrawIndicator);
            DrawIndicator!.Dispose ();
            DrawIndicator = null;
        }
    }

    internal void AdvanceDrawIndicator ()
    {
        if (View.Diagnostics.HasFlag (ViewDiagnosticFlags.DrawIndicator) && DrawIndicator is { })
        {
            DrawIndicator.AdvanceAnimation (false);
            DrawIndicator.Render ();
        }
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
        base.BeginInit ();

        if (Parent is null)
        {
            return;
        }

        ShowHideDrawIndicator ();

        HighlightStates |= (Parent.Arrangement != ViewArrangement.Fixed ? MouseState.Pressed : MouseState.None);

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
                e.Handled = Parent.InvokeCommand (Command.QuitToplevel) == true;
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

    // TODO: Make LineStyle nullable https://github.com/gui-cs/Terminal.Gui/issues/4021
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
            return Parent?.SuperView?.BorderStyle ?? LineStyle.None;
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

            Parent?.SetNeedsDraw ();
        }
    }


    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        if (Thickness == Thickness.Empty)
        {
            return true;
        }

        Rectangle screenBounds = ViewportToScreen (Viewport);

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
                                                Parent?.TitleTextFormatter.FormatAndGetSize ().Width ?? 0,
                                                Math.Min (screenBounds.Width - 4, borderBounds.Width - 4)
                                               )
                                     );

        if (Parent is { })
        {
            Parent.TitleTextFormatter.ConstrainToSize = new (maxTitleWidth, 1);
        }

        int sideLineLength = borderBounds.Height;
        bool canDrawBorder = borderBounds is { Width: > 0, Height: > 0 };

        LineStyle lineStyle = LineStyle;


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

        if (Parent is { }
            && canDrawBorder
            && Thickness.Top > 0
            && maxTitleWidth > 0
            && Settings.FastHasFlags (BorderSettings.Title)
            && !string.IsNullOrEmpty (Parent?.Title))
        {
            Rectangle titleRect = new (borderBounds.X + 2, titleY, maxTitleWidth, 1);

            Parent.TitleTextFormatter.Draw (
                                            titleRect,
                                            GetAttributeForRole (Parent.HasFocus ? VisualRole.Focus : VisualRole.Normal),
                                            GetAttributeForRole (Parent.HasFocus ? VisualRole.HotFocus : VisualRole.HotNormal));
            Parent?.LineCanvas.Exclude (new (titleRect));
        }

        if (canDrawBorder && LineStyle != LineStyle.None)
        {
            LineCanvas? lc = Parent?.LineCanvas;

            bool drawTop = Thickness.Top > 0 && Frame.Width > 1 && Frame.Height >= 1;
            bool drawLeft = Thickness.Left > 0 && (Frame.Height > 1 || Thickness.Top == 0);
            bool drawBottom = Thickness.Bottom > 0 && Frame.Width > 1 && Frame.Height > 1;
            bool drawRight = Thickness.Right > 0 && (Frame.Height > 1 || Thickness.Top == 0);

            //Attribute prevAttr = Driver?.GetAttribute () ?? Attribute.Default;

            Attribute normalAttribute = GetAttributeForRole (VisualRole.Normal);

            if (MouseState.HasFlag (MouseState.Pressed))
            {
                normalAttribute = GetAttributeForRole (VisualRole.Highlight);
            }

            SetAttribute (normalAttribute);

            if (drawTop)
            {
                // ╔╡Title╞═════╗
                // ╔╡╞═════╗
                if (borderBounds.Width < 4 || !Settings.FastHasFlags (BorderSettings.Title) || string.IsNullOrEmpty (Parent?.Title))
                {
                    // ╔╡╞╗ should be ╔══╗
                    lc?.AddLine (
                                 new (borderBounds.Location.X, titleY),
                                 borderBounds.Width,
                                 Orientation.Horizontal,
                                 lineStyle,
                                 normalAttribute
                                );
                }
                else
                {
                    // ┌────┐
                    //┌┘View└
                    //│
                    if (Thickness.Top == 2)
                    {
                        lc?.AddLine (
                                     new (borderBounds.X + 1, topTitleLineY),
                                     Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                     Orientation.Horizontal,
                                     lineStyle,
                                     normalAttribute
                                    );
                    }

                    // ┌────┐
                    //┌┘View└
                    //│
                    if (borderBounds.Width >= 4 && Thickness.Top > 2)
                    {
                        lc?.AddLine (
                                     new (borderBounds.X + 1, topTitleLineY),
                                     Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                     Orientation.Horizontal,
                                     lineStyle,
                                     normalAttribute
                                    );

                        lc?.AddLine (
                                     new (borderBounds.X + 1, topTitleLineY + 2),
                                     Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                     Orientation.Horizontal,
                                     lineStyle,
                                     normalAttribute
                                    );
                    }

                    // ╔╡Title╞═════╗
                    // Add a short horiz line for ╔╡
                    lc?.AddLine (
                                 new (borderBounds.Location.X, titleY),
                                 2,
                                 Orientation.Horizontal,
                                 lineStyle,
                                 normalAttribute
                                );

                    // Add a vert line for ╔╡
                    lc?.AddLine (
                                 new (borderBounds.X + 1, topTitleLineY),
                                 titleBarsLength,
                                 Orientation.Vertical,
                                 LineStyle.Single,
                                 normalAttribute
                                );

                    // Add a vert line for ╞
                    lc?.AddLine (
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
                                 normalAttribute
                                );

                    // Add the right hand line for ╞═════╗
                    lc?.AddLine (
                                 new (
                                      borderBounds.X
                                      + 1
                                      + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2)
                                      - 1,
                                      titleY
                                     ),
                                 borderBounds.Width - Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 lineStyle,
                                 normalAttribute
                                );
                }
            }

#if !SUBVIEW_BASED_BORDER

            if (drawLeft)
            {
                lc?.AddLine (
                             new (borderBounds.Location.X, titleY),
                             sideLineLength,
                             Orientation.Vertical,
                             lineStyle,
                             normalAttribute
                            );
            }
#endif

            if (drawBottom)
            {
                lc?.AddLine (
                             new (borderBounds.X, borderBounds.Y + borderBounds.Height - 1),
                             borderBounds.Width,
                             Orientation.Horizontal,
                             lineStyle,
                             normalAttribute
                            );
            }

            if (drawRight)
            {
                lc?.AddLine (
                             new (borderBounds.X + borderBounds.Width - 1, titleY),
                             sideLineLength,
                             Orientation.Vertical,
                             lineStyle,
                             normalAttribute
                            );
            }

            // SetAttribute (prevAttr);

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
                    Parent!.TitleTextFormatter.Draw (
                                                     new (borderBounds.X + 2, titleY, maxTitleWidth, 1),
                                                     Parent.HasFocus ? Parent.GetAttributeForRole (VisualRole.Focus) : Parent.GetAttributeForRole (VisualRole.Normal),
                                                     Parent.HasFocus ? Parent.GetAttributeForRole (VisualRole.Focus) : Parent.GetAttributeForRole (VisualRole.Normal));
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
                SetupGradientLineCanvas (lc!, screenBounds);
            }
            else
            {
                lc!.Fill = null;
            }
        }

        return true;

        ;
    }

    /// <summary>
    ///     Gets the subview used to render <see cref="ViewDiagnosticFlags.DrawIndicator"/>.
    /// </summary>
    public SpinnerView? DrawIndicator { get; private set; }

    private void SetupGradientLineCanvas (LineCanvas lc, Rectangle rect)
    {
        GetAppealingGradientColors (out List<Color> stops, out List<int> steps);

        var g = new Gradient (stops, steps);

        var fore = new GradientFill (rect, g, GradientDirection.Diagonal);
        var back = new SolidFill (GetAttributeForRole (VisualRole.Normal).Background);

        lc.Fill = new (fore, back);
    }

    private static void GetAppealingGradientColors (out List<Color> stops, out List<int> steps)
    {
        // Define the colors of the gradient stops with more appealing colors
        stops =
        [
            new (0, 128, 255), // Bright Blue
            new (0, 255, 128), // Bright Green
            new (255, 255), // Bright Yellow
            new (255, 128), // Bright Orange
            new (255, 0, 128)
        ];

        // Define the number of steps between each color for smoother transitions
        // If we pass only a single value then it will assume equal steps between all pairs
        steps = [15];
    }

}

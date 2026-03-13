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
///             ┌────┐
///             ┌┤1234├──┐
///             │        │
///             └────────┘
///             If Thickness.Top is 3:
///             ┌────┐
///             ┌┤1234├──┐
///             │└────┘  │
///             │        │
///             └────────┘
///         </example>
///     </para>
///     <para>
///         The Border provides keyboard and mouse support for moving and resizing the View. See
///         <see cref="ViewArrangement"/>.
///     </para>
/// </remarks>
public partial class Border : Adornment
{
    private LineStyle? _lineStyle;

    /// <summary>Gets the list of gaps on the top border line.</summary>
    public List<BorderGap> TopGaps { get; } = [];

    /// <summary>Gets the list of gaps on the bottom border line.</summary>
    public List<BorderGap> BottomGaps { get; } = [];

    /// <summary>Gets the list of gaps on the left border line.</summary>
    public List<BorderGap> LeftGaps { get; } = [];

    /// <summary>Gets the list of gaps on the right border line.</summary>
    public List<BorderGap> RightGaps { get; } = [];

    /// <summary>Clears all gap lists.</summary>
    public void ClearAllGaps ()
    {
        TopGaps.Clear ();
        BottomGaps.Clear ();
        LeftGaps.Clear ();
        RightGaps.Clear ();
    }

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
        ThicknessChanged += OnThicknessChanged;
        Settings = BorderSettings.Title;
        base.SuperViewRendersLineCanvas = true;
    }

    // TODO: Move DrawIndicator out of Border and into View
    private void OnThicknessChanged (object? sender, EventArgs e)
    {
        if (IsInitialized)
        {
            ShowHideDrawIndicator ();
        }
    }
    
    /// <summary>
    ///     INTERNAL API - Draws the transparent margins for the specified views. This is called from
    ///     <see cref="View.Draw(DrawContext)"/> on each
    ///     iteration of the main loop after all Views have been drawn.
    /// </summary>
    /// <remarks>
    ///     Non-transparent margins are drawn as-normal in <see cref="View.DrawAdornments"/>.
    /// </remarks>
    /// <param name="views"></param>
    /// <returns>
    ///     <see langword="true"/>
    /// </returns>
    internal static bool DrawBorders (IEnumerable<View> views)
    {
        Stack<View> stack = new (views);

        while (stack.Count > 0)
        {
            View view = stack.Pop ();

            if (view.Border is { } border
                && border.Thickness != Thickness.Empty
                && border.ViewportSettings.HasFlag (ViewportSettingsFlags.Transparent)
                && border.GetCachedClip () != null)
            {
                border.SetNeedsDraw ();
                Region? saved = view.GetClip ();
                view.SetClip (border.GetCachedClip ());
                border.Draw ();
                view.SetClip (saved);
                border.ClearCachedClip ();
            }

            // Do not include Margin views of subviews; not supported
            foreach (View subview in view.GetSubViews (false, includePadding: true, includeBorder: true)
                                         .OrderBy (v => v.ShadowStyle != ShadowStyle.None)
                                         .Reverse ())
            {
                stack.Push (subview);
            }
        }

        return true;
    }

    protected override bool OnRenderingLineCanvas () => false;


    // When the Parent is drawn, we cache the clip region so we can draw the Margin after all other Views
    private Region? _cachedClip;

    internal Region? GetCachedClip () => _cachedClip;

    internal void ClearCachedClip () { _cachedClip = null; }

    internal void CacheClip ()
    {
        if (Thickness != Thickness.Empty)
        {
            // PERFORMANCE: How expensive are these clones?
            _cachedClip = GetClip ()?.Clone ();
        }
    }

    private void ShowHideDrawIndicator ()
    {
        if (View.Diagnostics.HasFlag (ViewDiagnosticFlags.DrawIndicator) && Thickness != Thickness.Empty)
        {
            if (DrawIndicator is { })
            {
                return;
            }

            DrawIndicator = new SpinnerView
            {
#if DEBUG
                Id = "DrawIndicator",
#endif
                X = 1,
                Style = new SpinnerStyle.Dots2 (),
                SpinDelay = 0,
                Visible = false
            };
            Add (DrawIndicator);
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
        if (!View.Diagnostics.HasFlag (ViewDiagnosticFlags.DrawIndicator) || DrawIndicator is null)
        {
            return;
        }
        DrawIndicator.AdvanceAnimation (false);
        DrawIndicator.Render ();
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

        MouseHighlightStates |= Parent.Arrangement != ViewArrangement.Fixed ? MouseState.Pressed : MouseState.None;

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
                e.Handled = Parent.InvokeCommand (Command.Quit) == true;
            };
            Add (CloseButton);

            LayoutStarted += OnLayoutStarted;
    }
#endif
    }

#if SUBVIEW_BASED_BORDER
    private void OnLayoutStarted (object sender, LayoutEventArgs e)
    {
        _left.Border!.LineStyle = LineStyle;

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

        return new Rectangle (screenRect.X + Math.Max (0, Thickness.Left - 1),
                              screenRect.Y + Math.Max (0, Thickness.Top - 1),
                              Math.Max (0, screenRect.Width - Math.Max (0, Math.Max (0, Thickness.Left - 1) + Math.Max (0, Thickness.Right - 1))),
                              Math.Max (0, screenRect.Height - Math.Max (0, Math.Max (0, Thickness.Top - 1) + Math.Max (0, Thickness.Bottom - 1))));
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

        // BUGBUG: Setting LineStyle should SetNeedsDraw
        set => _lineStyle = value;
    }

    /// <summary>
    ///     Gets or sets the settings for the border.
    /// </summary>
    public BorderSettings Settings
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            field = value;

            Parent?.SetNeedsDraw ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
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

        int maxTitleWidth = Math.Max (0,
                                      Math.Min (Parent?.TitleTextFormatter.FormatAndGetSize ().Width ?? 0,
                                                Math.Min (screenBounds.Width - 4, borderBounds.Width - 4)));

        Parent?.TitleTextFormatter.ConstrainToSize = new Size (maxTitleWidth, 1);

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

        if (Driver is { }
            && Parent is { }
            && canDrawBorder
            && Thickness.Top > 0
            && maxTitleWidth > 0
            && Settings.FastHasFlags (BorderSettings.Title)
            && !string.IsNullOrEmpty (Parent?.Title))
        {
            Rectangle titleRect = new (borderBounds.X + 2, titleY, maxTitleWidth, 1);

            Parent.TitleTextFormatter.Draw (Driver,
                                            titleRect,
                                            GetAttributeForRole (Parent.HasFocus ? VisualRole.Focus : VisualRole.Normal),
                                            GetAttributeForRole (Parent.HasFocus ? VisualRole.HotFocus : VisualRole.HotNormal));
            Parent?.LineCanvas.Exclude (new Region (titleRect));
        }

        if (!canDrawBorder || LineStyle == LineStyle.None)
        {
            return true;
        }
        LineCanvas? lc = LineCanvas;

        bool drawTop = Thickness.Top > 0 && Frame is { Width: > 1, Height: >= 1 };
        bool drawLeft = Thickness.Left > 0 && (Frame.Height > 1 || Thickness.Top == 0);
        bool drawBottom = Thickness.Bottom > 0 && Frame is { Width: > 1, Height: > 1 };
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
                lc?.AddLine (new Point (borderBounds.Location.X, titleY), borderBounds.Width, Orientation.Horizontal, lineStyle, normalAttribute);
            }
            else
            {
                // ┌────┐
                //┌┘View└
                //│
                if (Thickness.Top == 2)
                {
                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 lineStyle,
                                 normalAttribute);
                }

                // ┌────┐
                //┌┘View└
                //│
                if (borderBounds.Width >= 4 && Thickness.Top > 2)
                {
                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 lineStyle,
                                 normalAttribute);

                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY + 2),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 lineStyle,
                                 normalAttribute);
                }

                // ╔╡Title╞═════╗
                // Add a short horiz line for ╔╡
                lc?.AddLine (borderBounds.Location with { Y = titleY }, 2, Orientation.Horizontal, lineStyle, normalAttribute);

                // Add a vert line for ╔╡
                lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY), titleBarsLength, Orientation.Vertical, LineStyle.Single, normalAttribute);

                // Add a vert line for ╞
                lc?.AddLine (new Point (borderBounds.X + 1 + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2) - 1, topTitleLineY),
                             titleBarsLength,
                             Orientation.Vertical,
                             LineStyle.Single,
                             normalAttribute);

                // Add the right hand line for ╞═════╗
                lc?.AddLine (new Point (borderBounds.X + 1 + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2) - 1, titleY),
                             borderBounds.Width - Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                             Orientation.Horizontal,
                             lineStyle,
                             normalAttribute);
            }
        }

#if !SUBVIEW_BASED_BORDER

        if (drawLeft)
        {
            if (LeftGaps.Count > 0)
            {
                // Draw segmented left line, skipping gap regions.
                DrawSegmentedVerticalLine (lc, borderBounds.X, titleY, sideLineLength, LeftGaps, lineStyle, normalAttribute);
            }
            else
            {
                lc?.AddLine (borderBounds.Location with { Y = titleY }, sideLineLength, Orientation.Vertical, lineStyle, normalAttribute);
            }
        }
#endif

        if (drawBottom)
        {
            lc?.AddLine (new Point (borderBounds.X, borderBounds.Y + borderBounds.Height - 1),
                         borderBounds.Width,
                         Orientation.Horizontal,
                         lineStyle,
                         normalAttribute);
        }

        if (drawRight)
        {
            int rightX = borderBounds.X + borderBounds.Width - 1;

            if (RightGaps.Count > 0)
            {
                // Draw segmented right line, skipping gap regions.
                // This produces correct auto-join at segment boundaries (e.g., ╮ instead of ┤).
                DrawSegmentedVerticalLine (lc, rightX, titleY, sideLineLength, RightGaps, lineStyle, normalAttribute);
            }
            else
            {
                lc?.AddLine (new Point (rightX, titleY), sideLineLength, Orientation.Vertical, lineStyle, normalAttribute);
            }
        }

        // SetAttribute (prevAttr);

        // Apply border gaps — exclude regions where border lines should not be drawn
        ApplyGaps (lc!, borderBounds);

        // TODO: This should be moved to LineCanvas as a new BorderStyle.Ruler
        if (Diagnostics.HasFlag (ViewDiagnosticFlags.Ruler))
        {
            // Top
            var hRuler = new Ruler { Length = screenBounds.Width, Orientation = Orientation.Horizontal };

            if (drawTop)
            {
                hRuler.Draw (Driver, new Point (screenBounds.X, screenBounds.Y));
            }

            // Redraw title
            if (drawTop && maxTitleWidth > 0 && Settings.FastHasFlags (BorderSettings.Title))
            {
                Parent!.TitleTextFormatter.Draw (Driver,
                                                 new Rectangle (borderBounds.X + 2, titleY, maxTitleWidth, 1),
                                                 Parent.HasFocus
                                                     ? Parent.GetAttributeForRole (VisualRole.Focus)
                                                     : Parent.GetAttributeForRole (VisualRole.Normal),
                                                 Parent.HasFocus
                                                     ? Parent.GetAttributeForRole (VisualRole.Focus)
                                                     : Parent.GetAttributeForRole (VisualRole.Normal));
            }

            //Left
            var vRuler = new Ruler { Length = screenBounds.Height - 2, Orientation = Orientation.Vertical };

            if (drawLeft)
            {
                vRuler.Draw (Driver, new Point (screenBounds.X, screenBounds.Y + 1), 1);
            }

            // Bottom
            if (drawBottom)
            {
                hRuler.Draw (Driver, new Point (screenBounds.X, screenBounds.Y + screenBounds.Height - 1));
            }

            // Right
            if (drawRight)
            {
                vRuler.Draw (Driver, new Point (screenBounds.X + screenBounds.Width - 1, screenBounds.Y + 1), 1);
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

        return true;
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

        lc.Fill = new FillPair (fore, back);
    }

    private static void GetAppealingGradientColors (out List<Color> stops, out List<int> steps)
    {
        // Define the colors of the gradient stops with more appealing colors
        stops =
        [
            new Color (0, 128, 255), // Bright Blue
            new Color (0, 255, 128), // Bright Green
            new Color (255, 255), // Bright Yellow
            new Color (255, 128), // Bright Orange
            new Color (255, 0, 128)
        ];

        // Define the number of steps between each color for smoother transitions
        // If we pass only a single value then it will assume equal steps between all pairs
        steps = [15];
    }

    /// <summary>
    ///     Draws a vertical line at the specified X coordinate, splitting it into segments that skip over gaps.
    ///     Each segment is an independent <see cref="LineCanvas.AddLine(StraightLine)"/> call, so auto-join at segment
    ///     boundaries produces correct corner glyphs (e.g., ╮) instead of T-junctions (e.g., ┤).
    /// </summary>
    private static void DrawSegmentedVerticalLine (
        LineCanvas? lc,
        int x,
        int startY,
        int totalLength,
        List<BorderGap> gaps,
        LineStyle lineStyle,
        Attribute attribute)
    {
        if (lc is null || totalLength <= 0)
        {
            return;
        }

        // Build sorted list of gap intervals (relative to startY)
        List<BorderGap> sortedGaps = [.. gaps.OrderBy (g => g.Position)];
        var currentY = 0;

        foreach (BorderGap gap in sortedGaps)
        {
            int segmentLength = gap.Position - currentY;

            if (segmentLength > 0)
            {
                lc.AddLine (new Point (x, startY + currentY), segmentLength, Orientation.Vertical, lineStyle, attribute);
            }

            currentY = gap.Position + gap.Length;
        }

        // Draw remaining segment after the last gap
        int remainingLength = totalLength - currentY;

        if (remainingLength > 0)
        {
            lc.AddLine (new Point (x, startY + currentY), remainingLength, Orientation.Vertical, lineStyle, attribute);
        }
    }

    private void ApplyGaps (LineCanvas lc, Rectangle borderBounds)
    {
        foreach (BorderGap gap in TopGaps)
        {
            lc.Exclude (new Region (new Rectangle (borderBounds.X + gap.Position, borderBounds.Y, gap.Length, 1)));
        }

        foreach (BorderGap gap in BottomGaps)
        {
            lc.Exclude (new Region (new Rectangle (borderBounds.X + gap.Position, borderBounds.Y + borderBounds.Height - 1, gap.Length, 1)));
        }

        // LeftGaps and RightGaps are handled via segmented line drawing in OnDrawingContent,
        // not via Exclude. This allows other views (e.g., TabRow) to draw lines at gap positions
        // without being suppressed by the exclusion region.
    }
}

namespace Terminal.Gui.ViewBase;

/// <summary>
///     The View-backed rendering layer for the Border adornment.
///     Created lazily by <see cref="Border"/> (via <see cref="AdornmentImpl.GetOrCreateView"/>)
///     when rendering, arrangement, or other View-level functionality is needed.
/// </summary>
/// <remarks>
///     <para>
///         Renders a border around the view with the <see cref="View.Title"/>. A border using <see cref="LineStyle"/>
///         will be drawn on the sides of <see cref="Drawing.Thickness"/> that are greater than zero.
///     </para>
///     <para>
///         The <see cref="View.Title"/> of <see cref="IAdornment.Parent"/> will be drawn based on the value of
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
public partial class BorderView : AdornmentView
{
    /// <inheritdoc/>
    public BorderView ()
    { /* Do nothing; A parameter-less constructor is required to support all views unit tests. */
    }

    /// <inheritdoc/>
    public BorderView (Border border) : base (border)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (border == null)
        {
            // Supports AllViews_Tests_All_Constructors which uses reflection
            return;
        }
        CanFocus = false;
        TabStop = TabBehavior.TabGroup;

        if (border.Parent is { })
        {
            Frame = border.Parent.Margin.Thickness.GetInside (border.Parent.Margin.GetFrame ());
        }
        border.ThicknessChanged += OnThicknessChanged;
        border.Parent?.Margin.ThicknessChanged += OnThicknessChanged;
    }

    /// <inheritdoc/>
    public override void OnParentFrameChanged (Rectangle newParentFrame)
    {
        if (Adornment?.Parent is { })
        {
            Frame = Adornment.Parent.Margin.Thickness.GetInside (Adornment.Parent.Margin.GetFrame ());
        }
    }

    // TODO: Move DrawIndicator out of Border and into View
    private void OnThicknessChanged (object? sender, EventArgs e)
    {
        if (Adornment is { Parent: { } })
        {
            OnParentFrameChanged (Adornment.Parent.Frame);
        }

        if (IsInitialized)
        {
            ShowHideDrawIndicator ();
        }
    }

    private void ShowHideDrawIndicator ()
    {
        if (View.Diagnostics.HasFlag (ViewDiagnosticFlags.DrawIndicator) && Adornment!.Thickness != Thickness.Empty)
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

        if (Adornment?.Parent is null)
        {
            return;
        }

        ShowHideDrawIndicator ();

        MouseHighlightStates |= Adornment.Parent.Arrangement != ViewArrangement.Fixed ? MouseState.Pressed : MouseState.None;

#if SUBVIEW_BASED_BORDER
        if (Adornment.Parent is { })
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
        _left.Border.LineStyle = LineStyle;

        _left.X = Adornment!.Thickness.Left - 1;
        _left.Y = Adornment!.Thickness.Top - 1;
        _left.Width = 1;
        _left.Height = Height;

        CloseButton.X = Pos.AnchorEnd (Adornment!.Thickness.Right / 2 + 1) -
                        (Pos.Right (CloseButton) -
                         Pos.Left (CloseButton));
        CloseButton.Y = 0;
}
#endif

    internal Rectangle GetBorderBounds ()
    {
        Rectangle screenRect = ViewportToScreen (Viewport);

        return new Rectangle (screenRect.X + Math.Max (0, Adornment!.Thickness.Left - 1),
                              screenRect.Y + Math.Max (0, Adornment!.Thickness.Top - 1),
                              Math.Max (0,
                                        screenRect.Width
                                        - Math.Max (0, Math.Max (0, Adornment!.Thickness.Left - 1) + Math.Max (0, Adornment!.Thickness.Right - 1))),
                              Math.Max (0,
                                        screenRect.Height
                                        - Math.Max (0, Math.Max (0, Adornment!.Thickness.Top - 1) + Math.Max (0, Adornment!.Thickness.Bottom - 1))));
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (Adornment is null || Adornment.Thickness == Thickness.Empty)
        {
            return true;
        }

        if (Adornment is not Border border)
        {
            throw new InvalidOperationException ("Adornment must be of type Border");
        }

        Rectangle screenBounds = ViewportToScreen (Viewport);

        Rectangle borderBounds = GetBorderBounds ();
        int topTitleLineY = borderBounds.Y;
        int titleY = borderBounds.Y;
        var titleBarsLength = 0;

        int maxTitleWidth = Math.Max (0,
                                      Math.Min (Adornment.Parent?.TitleTextFormatter.FormatAndGetSize ().Width ?? 0,
                                                Math.Min (screenBounds.Width - 4, borderBounds.Width - 4)));

        Adornment.Parent?.TitleTextFormatter.ConstrainToSize = new Size (maxTitleWidth, 1);

        int sideLineLength = borderBounds.Height;
        bool canDrawBorder = borderBounds is { Width: > 0, Height: > 0 };

        if (border.Settings.FastHasFlags (BorderSettings.Title))
        {
            switch (Adornment!.Thickness.Top)
            {
                case 2:
                    topTitleLineY = borderBounds.Y - 1;
                    titleY = topTitleLineY + 1;
                    titleBarsLength = 2;

                    break;

                case 3:
                    topTitleLineY = borderBounds.Y - (Adornment!.Thickness.Top - 1);
                    titleY = topTitleLineY + 1;
                    titleBarsLength = 3;
                    sideLineLength++;

                    break;

                case > 3:
                    topTitleLineY = borderBounds.Y - 2;
                    titleY = topTitleLineY + 1;
                    titleBarsLength = 3;
                    sideLineLength++;

                    break;
            }
        }

        if (Driver is { }
            && Adornment.Parent is { }
            && canDrawBorder
            && Adornment!.Thickness.Top > 0
            && maxTitleWidth > 0
            && border.Settings.FastHasFlags (BorderSettings.Title)
            && !string.IsNullOrEmpty (Adornment.Parent?.Title))
        {
            Rectangle titleRect = new (borderBounds.X + 2, titleY, maxTitleWidth, 1);

            Adornment.Parent.TitleTextFormatter.Draw (Driver,
                                                      titleRect,
                                                      GetAttributeForRole (Adornment.Parent.HasFocus ? VisualRole.Focus : VisualRole.Normal),
                                                      GetAttributeForRole (Adornment.Parent.HasFocus ? VisualRole.HotFocus : VisualRole.HotNormal));

            // Cache the title rect for the parent's DoDrawComplete to use when building
            // drawn region for transparent borders. The title is drawn directly (not via LineCanvas),
            // so it won't be captured by RenderLineCanvas's region reporting.
            LastTitleRect = titleRect;

            // Report the title rect to the DrawContext so it participates in clip exclusion.
            // This ensures the title occludes peer subviews when the Border is transparent.
            context?.AddDrawnRectangle (titleRect);
            Adornment.Parent?.LineCanvas.Exclude (new Region (titleRect));
        }

        if (!canDrawBorder || (Adornment as Border)?.LineStyle is null || (Adornment as Border)?.LineStyle == LineStyle.None)
        {
            return true;
        }
        LineCanvas? lc = Adornment.Parent?.LineCanvas;

        bool drawTop = Adornment!.Thickness.Top > 0 && Frame is { Width: > 1, Height: >= 1 };
        bool drawLeft = Adornment!.Thickness.Left > 0 && (Frame.Height > 1 || Adornment!.Thickness.Top == 0);
        bool drawBottom = Adornment!.Thickness.Bottom > 0 && Frame is { Width: > 1, Height: > 1 };
        bool drawRight = Adornment!.Thickness.Right > 0 && (Frame.Height > 1 || Adornment!.Thickness.Top == 0);

        Attribute normalAttribute = GetAttributeForRole (VisualRole.Normal);

        if (MouseState.HasFlag (MouseState.Pressed))
        {
            normalAttribute = GetAttributeForRole (VisualRole.Highlight);
        }

        SetAttribute (normalAttribute);

        if (drawTop)
        {
            if (borderBounds.Width < 4 || !border.Settings.FastHasFlags (BorderSettings.Title) || string.IsNullOrEmpty (Adornment.Parent?.Title))
            {
                if (border.LineStyle is { })
                {
                    lc?.AddLine (new Point (borderBounds.Location.X, titleY),
                                 borderBounds.Width,
                                 Orientation.Horizontal,
                                 border.LineStyle.Value,
                                 normalAttribute);
                }
            }
            else
            {
                if (Adornment!.Thickness.Top == 2)
                {
                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 border.LineStyle!.Value,
                                 normalAttribute);
                }

                if (borderBounds.Width >= 4 && Adornment!.Thickness.Top > 2)
                {
                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 border.LineStyle!.Value,
                                 normalAttribute);

                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY + 2),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 border.LineStyle!.Value,
                                 normalAttribute);
                }

                lc?.AddLine (borderBounds.Location with { Y = titleY }, 2, Orientation.Horizontal, border.LineStyle!.Value, normalAttribute);

                lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY), titleBarsLength, Orientation.Vertical, LineStyle.Single, normalAttribute);

                lc?.AddLine (new Point (borderBounds.X + 1 + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2) - 1, topTitleLineY),
                             titleBarsLength,
                             Orientation.Vertical,
                             LineStyle.Single,
                             normalAttribute);

                lc?.AddLine (new Point (borderBounds.X + 1 + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2) - 1, titleY),
                             borderBounds.Width - Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                             Orientation.Horizontal,
                             border.LineStyle!.Value,
                             normalAttribute);
            }
        }

#if !SUBVIEW_BASED_BORDER

        if (drawLeft)
        {
            lc?.AddLine (borderBounds.Location with { Y = titleY }, sideLineLength, Orientation.Vertical, border.LineStyle!.Value, normalAttribute);
        }
#endif

        if (drawBottom)
        {
            lc?.AddLine (new Point (borderBounds.X, borderBounds.Y + borderBounds.Height - 1),
                         borderBounds.Width,
                         Orientation.Horizontal,
                         border.LineStyle!.Value,
                         normalAttribute);
        }

        if (drawRight)
        {
            lc?.AddLine (new Point (borderBounds.X + borderBounds.Width - 1, titleY),
                         sideLineLength,
                         Orientation.Vertical,
                         border.LineStyle!.Value,
                         normalAttribute);
        }

        // TODO: This should be moved to LineCanvas as a new BorderStyle.Ruler
        if (Diagnostics.HasFlag (ViewDiagnosticFlags.Ruler))
        {
            var hRuler = new Ruler { Length = screenBounds.Width, Orientation = Orientation.Horizontal };

            if (drawTop)
            {
                hRuler.Draw (Driver, new Point (screenBounds.X, screenBounds.Y));
            }

            if (drawTop && maxTitleWidth > 0 && border.Settings.FastHasFlags (BorderSettings.Title))
            {
                Adornment.Parent!.TitleTextFormatter.Draw (Driver,
                                                           new Rectangle (borderBounds.X + 2, titleY, maxTitleWidth, 1),
                                                           Adornment.Parent.HasFocus
                                                               ? Adornment.Parent.GetAttributeForRole (VisualRole.Focus)
                                                               : Adornment.Parent.GetAttributeForRole (VisualRole.Normal),
                                                           Adornment.Parent.HasFocus
                                                               ? Adornment.Parent.GetAttributeForRole (VisualRole.Focus)
                                                               : Adornment.Parent.GetAttributeForRole (VisualRole.Normal));
            }

            var vRuler = new Ruler { Length = screenBounds.Height - 2, Orientation = Orientation.Vertical };

            if (drawLeft)
            {
                vRuler.Draw (Driver, new Point (screenBounds.X, screenBounds.Y + 1), 1);
            }

            if (drawBottom)
            {
                hRuler.Draw (Driver, new Point (screenBounds.X, screenBounds.Y + screenBounds.Height - 1));
            }

            if (drawRight)
            {
                vRuler.Draw (Driver, new Point (screenBounds.X + screenBounds.Width - 1, screenBounds.Y + 1), 1);
            }
        }

        if (border.Settings.FastHasFlags (BorderSettings.Gradient))
        {
            if (_cachedGradientFill is null || _cachedGradientRect != screenBounds)
            {
                SetupGradientLineCanvas (lc!, screenBounds);
            }
            else
            {
                lc!.Fill = _cachedGradientFill;
            }
        }
        else
        {
            lc?.Fill = null;
            _cachedGradientFill = null;
        }

        return true;
    }

    /// <summary>
    /// <summary>
    ///     Gets the screen-coordinate rectangle of the title text from the last draw pass.
    ///     Used by the parent view to build drawn region for transparent border clip exclusion.
    /// </summary>
    internal Rectangle? LastTitleRect { get; set; }

    /// <summary>
    ///     Gets the subview used to render <see cref="ViewDiagnosticFlags.DrawIndicator"/>.
    /// </summary>
    public SpinnerView? DrawIndicator { get; private set; }

    private FillPair? _cachedGradientFill;
    private Rectangle _cachedGradientRect;

    private void SetupGradientLineCanvas (LineCanvas lc, Rectangle rect)
    {
        GetAppealingGradientColors (out List<Color> stops, out List<int> steps);

        Gradient g = new (stops, steps);

        GradientFill fore = new (rect, g, GradientDirection.Diagonal);
        SolidFill back = new (GetAttributeForRole (VisualRole.Normal).Background);

        _cachedGradientFill = new FillPair (fore, back);
        _cachedGradientRect = rect;
        lc.Fill = _cachedGradientFill;
    }

    private static void GetAppealingGradientColors (out List<Color> stops, out List<int> steps)
    {
        stops =
        [
            new Color (0, 128, 255), // Bright Blue
            new Color (0, 255, 128), // Bright Green
            new Color (255, 255), // Bright Yellow
            new Color (255, 128), // Bright Orange
            new Color (255, 0, 128)
        ];

        steps = [15];
    }
}

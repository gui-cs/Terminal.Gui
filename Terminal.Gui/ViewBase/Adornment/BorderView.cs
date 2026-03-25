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

    private Rectangle GetBorderBounds ()
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

    /// <summary>
    ///     Computes the content border rectangle when <see cref="BorderSettings.Tab"/> is set.
    ///     Non-title sides use the outer edge of the thickness. The title side uses <c>thickness - 1</c>
    ///     from the outer edge, leaving a tab header region between the outer edge and the content border line.
    /// </summary>
    private Rectangle GetTabBorderBounds (Border border)
    {
        Rectangle screenRect = ViewportToScreen (Viewport);

        int left = screenRect.X;
        int top = screenRect.Y;
        int right = screenRect.Right;
        int bottom = screenRect.Bottom;

        // Title side: content border at thickness - 1 from outer edge
        switch (border.TabSide)
        {
            case Side.Top:
                top += Math.Max (0, Adornment!.Thickness.Top - 1);

                break;

            case Side.Bottom:
                bottom -= Math.Max (0, Adornment!.Thickness.Bottom - 1);

                break;

            case Side.Left:
                left += Math.Max (0, Adornment!.Thickness.Left - 1);

                break;

            case Side.Right:
                right -= Math.Max (0, Adornment!.Thickness.Right - 1);

                break;
        }

        return new Rectangle (left, top, Math.Max (0, right - left), Math.Max (0, bottom - top));
    }

    #region Tab Title Label

    private Label? _tabTitleLabel;

    /// <summary>
    ///     Gets or lazily creates the <see cref="Label"/> SubView used to render the tab title text.
    ///     The Label handles text rendering (with hotkey and vertical direction support).
    ///     The tab box lines are drawn manually on the parent's <see cref="View.LineCanvas"/> for correct auto-join timing.
    /// </summary>
    private Label EnsureTabTitleLabel ()
    {
        if (_tabTitleLabel is null)
        {
            _tabTitleLabel = new Label
            {
#if DEBUG
                Id = "TabTitleLabel",
#endif
                CanFocus = false,
                TabStop = TabBehavior.NoStop,
            };
            _tabTitleLabel.Border.Thickness = Thickness.Empty;
            _tabTitleLabel.Border.Settings = BorderSettings.None;
            Add (_tabTitleLabel);
        }

        return _tabTitleLabel;
    }

    /// <summary>
    ///     Computes the unclipped header rectangle for the given side, offset, length, and depth.
    ///     Moved from <c>TabHeaderRenderer</c>.
    /// </summary>
    private static Rectangle ComputeHeaderRect (Rectangle contentBorderRect, Side side, int offset, int length, int depth) =>
        side switch
        {
            Side.Top => new Rectangle (contentBorderRect.X + offset, contentBorderRect.Y - (depth - 1), length, depth),
            Side.Bottom => new Rectangle (contentBorderRect.X + offset, contentBorderRect.Bottom - 1, length, depth),
            Side.Left => new Rectangle (contentBorderRect.X - (depth - 1), contentBorderRect.Y + offset, depth, length),
            Side.Right => new Rectangle (contentBorderRect.Right - 1, contentBorderRect.Y + offset, depth, length),
            _ => Rectangle.Empty
        };

    /// <summary>
    ///     Computes the full view bounds (content border + header protrusion area).
    ///     Moved from <c>TabHeaderRenderer</c>.
    /// </summary>
    private static Rectangle ComputeViewBounds (Rectangle contentBorderRect, Side side, int depth) =>
        side switch
        {
            Side.Top => new Rectangle (contentBorderRect.X, contentBorderRect.Y - (depth - 1), contentBorderRect.Width, contentBorderRect.Height + (depth - 1)),
            Side.Bottom => new Rectangle (contentBorderRect.X, contentBorderRect.Y, contentBorderRect.Width, contentBorderRect.Height + (depth - 1)),
            Side.Left => new Rectangle (contentBorderRect.X - (depth - 1), contentBorderRect.Y, contentBorderRect.Width + (depth - 1), contentBorderRect.Height),
            Side.Right => new Rectangle (contentBorderRect.X, contentBorderRect.Y, contentBorderRect.Width + (depth - 1), contentBorderRect.Height),
            _ => contentBorderRect
        };

    /// <summary>
    ///     Computes the <see cref="Thickness"/> for the tab title Label's border based on
    ///     depth, focus state, and which side the tab is on.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         "Cap" is the outward edge (away from content). "Content" is the inward edge (toward content area).
    ///         For depth ≥ 3, the content-side thickness toggles with focus to create the open gap / separator.
    ///         For depth &lt; 3, no focus distinction in border lines.
    ///     </para>
    /// </remarks>
    private static Thickness ComputeTabLabelThickness (Side tabSide, int depth, bool hasFocus)
    {
        int cap = depth >= 2 ? 1 : 0;
        int contentSide = depth >= 3 && !hasFocus ? 1 : 0;

        return tabSide switch
        {
            Side.Top => new Thickness (1, cap, 1, contentSide),
            Side.Bottom => new Thickness (1, contentSide, 1, cap),
            Side.Left => new Thickness (cap, 1, contentSide, 1),
            Side.Right => new Thickness (contentSide, 1, cap, 1),
            _ => Thickness.Empty
        };
    }

    #endregion Tab Title Label

    private int GetTabDepth (Border border)
    {
        int thickness = border.TabSide switch
                        {
                            Side.Top => Adornment!.Thickness.Top,
                            Side.Bottom => Adornment!.Thickness.Bottom,
                            Side.Left => Adornment!.Thickness.Left,
                            Side.Right => Adornment!.Thickness.Right,
                            _ => 3
                        };

        return Math.Min (thickness, 3);
    }

    /// <summary>
    ///     Draws the border and tab header when <see cref="BorderSettings.Tab"/> is set.
    ///     Uses a <see cref="Label"/> SubView for the tab header box and title.
    ///     The Label's border lines auto-join with the content border via <see cref="View.LineCanvas"/>.
    /// </summary>
    private bool DrawTabBorder (Border border, DrawContext? context)
    {
        if (Adornment?.Parent is null || Driver is null)
        {
            return true;
        }

        Rectangle screenBounds = ViewportToScreen (Viewport);
        Rectangle borderBounds = GetTabBorderBounds (border);

        if (borderBounds is not { Width: > 0, Height: > 0 })
        {
            return true;
        }

        if (border.LineStyle is null or LineStyle.None)
        {
            return true;
        }

        Attribute normalAttribute = GetAttributeForRole (VisualRole.Normal);

        if (MouseState.HasFlag (MouseState.Pressed))
        {
            normalAttribute = GetAttributeForRole (VisualRole.Highlight);
        }

        SetAttribute (normalAttribute);

        LineCanvas? lc = Adornment.Parent?.LineCanvas;

        if (lc is null)
        {
            return true;
        }

        int tabDepth = GetTabDepth (border);
        int tabLength = border.TabLength!.Value;
        LineStyle lineStyle = border.LineStyle.Value;
        bool hasFocus = border.Parent!.HasFocus;

        // Compute tab header geometry
        Rectangle headerRect = ComputeHeaderRect (borderBounds, border.TabSide, border.TabOffset, tabLength, tabDepth);
        Rectangle viewBounds = ComputeViewBounds (borderBounds, border.TabSide, tabDepth);
        Rectangle clipped = Rectangle.Intersect (headerRect, viewBounds);
        bool tabVisible = !clipped.IsEmpty;

        // Draw the 3 non-tab-side content border lines (always drawn).
        // The tab-side line is handled below — conditionally, based on whether the tab is visible.
        if (Adornment!.Thickness.Top > 0 && (border.TabSide != Side.Top || !tabVisible))
        {
            lc.AddLine (new Point (borderBounds.X, borderBounds.Y), borderBounds.Width, Orientation.Horizontal, lineStyle, normalAttribute);
        }

        if (Adornment!.Thickness.Bottom > 0 && (border.TabSide != Side.Bottom || !tabVisible))
        {
            lc.AddLine (new Point (borderBounds.X, borderBounds.Bottom - 1), borderBounds.Width, Orientation.Horizontal, lineStyle, normalAttribute);
        }

        if (Adornment!.Thickness.Left > 0 && (border.TabSide != Side.Left || !tabVisible))
        {
            lc.AddLine (new Point (borderBounds.X, borderBounds.Y), borderBounds.Height, Orientation.Vertical, lineStyle, normalAttribute);
        }

        if (Adornment!.Thickness.Right > 0 && (border.TabSide != Side.Right || !tabVisible))
        {
            lc.AddLine (new Point (borderBounds.Right - 1, borderBounds.Y), borderBounds.Height, Orientation.Vertical, lineStyle, normalAttribute);
        }

        // Draw tab header box lines and position the title Label
        if (tabVisible)
        {
            Thickness tabThickness = ComputeTabLabelThickness (border.TabSide, tabDepth, hasFocus);

            // Draw the tab box lines on the parent's LineCanvas (for correct auto-join timing).
            // For depth 1, skip — the extension lines below handle the side edges instead.
            if (tabDepth > 1)
            {
                AddTabBoxLines (lc, headerRect, clipped, border.TabSide, tabThickness, lineStyle, normalAttribute);
            }

            // Draw the tab-side content border (gap or separator)
            AddTabSideContentBorder (lc, clipped, headerRect, borderBounds, border.TabSide, hasFocus, tabDepth, lineStyle, normalAttribute);

            // For depth 1, add 1-cell extension lines for corner auto-join
            if (tabDepth == 1)
            {
                AddDepth1CornerExtensions (lc, clipped, border.TabSide, lineStyle, normalAttribute);
            }

            // Position the Label for text rendering (no border on Label itself).
            // Use the UNCLIPPED headerRect for label placement so the View system's
            // natural clipping handles partial visibility (negative offsets, overflow).
            Rectangle unclippedContent = ComputeTabContentArea (headerRect, headerRect, border.TabSide, tabThickness, tabDepth);
            Rectangle visibleContent = ComputeTabContentArea (clipped, headerRect, border.TabSide, tabThickness, tabDepth);

            if (!unclippedContent.IsEmpty && !visibleContent.IsEmpty)
            {
                Label label = EnsureTabTitleLabel ();
                label.Visible = true;
                label.HotKeySpecifier = Adornment.Parent!.HotKeySpecifier;
                label.Text = Adornment.Parent!.Title;

                // For Left/Right, render text vertically
                label.TextFormatter.Direction = border.TabSide is Side.Left or Side.Right
                                                    ? TextDirection.TopBottom_LeftRight
                                                    : TextDirection.LeftRight_TopBottom;

                // Convert unclipped content area from screen to BorderView viewport coords.
                // The Label extends beyond the visible area — BorderView's viewport clips it.
                Point screenOrigin = ViewportToScreen (Point.Empty);
                Rectangle labelFrame = new (
                    unclippedContent.X - screenOrigin.X,
                    unclippedContent.Y - screenOrigin.Y,
                    unclippedContent.Width,
                    unclippedContent.Height);
                label.Frame = labelFrame;
                label.Width = labelFrame.Width;
                label.Height = labelFrame.Height;

                // Set LastTitleRect to the visible portion for clip exclusion
                LastTitleRect = visibleContent;
            }
            else if (_tabTitleLabel is { })
            {
                _tabTitleLabel.Visible = false;
                LastTitleRect = null;
            }
        }
        else
        {
            // Tab is off-screen — hide Label
            if (_tabTitleLabel is { })
            {
                _tabTitleLabel.Visible = false;
            }

            LastTitleRect = null;
        }

        // Gradient support
        if (border.Settings.FastHasFlags (BorderSettings.Gradient))
        {
            if (_cachedGradientFill is null || _cachedGradientRect != screenBounds)
            {
                SetupGradientLineCanvas (lc, screenBounds);
            }
            else
            {
                lc.Fill = _cachedGradientFill;
            }
        }
        else
        {
            lc.Fill = null;
            _cachedGradientFill = null;
        }

        return true;
    }

    /// <summary>
    ///     For depth-1 tabs, adds 1-cell line extensions at the side edges of the tab header so that
    ///     LineCanvas auto-join produces curved corner glyphs where the tab meets the content border.
    /// </summary>
    private static void AddDepth1CornerExtensions (LineCanvas lc, Rectangle clipped, Side side, LineStyle lineStyle, Attribute? attribute)
    {
        switch (side)
        {
            case Side.Top:
                // Extend left/right edges 1 cell upward (outward)
                if (clipped.Width >= 1)
                {
                    lc.AddLine (new Point (clipped.X, clipped.Y - 1), 2, Orientation.Vertical, lineStyle, attribute);
                    lc.AddLine (new Point (clipped.Right - 1, clipped.Y - 1), 2, Orientation.Vertical, lineStyle, attribute);
                }

                break;

            case Side.Bottom:
                // Extend left/right edges 1 cell downward (outward)
                if (clipped.Width >= 1)
                {
                    lc.AddLine (new Point (clipped.X, clipped.Y), 2, Orientation.Vertical, lineStyle, attribute);
                    lc.AddLine (new Point (clipped.Right - 1, clipped.Y), 2, Orientation.Vertical, lineStyle, attribute);
                }

                break;

            case Side.Left:
                // Extend top/bottom edges 1 cell leftward (outward)
                if (clipped.Height >= 1)
                {
                    lc.AddLine (new Point (clipped.X - 1, clipped.Y), 2, Orientation.Horizontal, lineStyle, attribute);
                    lc.AddLine (new Point (clipped.X - 1, clipped.Bottom - 1), 2, Orientation.Horizontal, lineStyle, attribute);
                }

                break;

            case Side.Right:
                // Extend top/bottom edges 1 cell rightward (outward)
                if (clipped.Height >= 1)
                {
                    lc.AddLine (new Point (clipped.X, clipped.Y), 2, Orientation.Horizontal, lineStyle, attribute);
                    lc.AddLine (new Point (clipped.X, clipped.Bottom - 1), 2, Orientation.Horizontal, lineStyle, attribute);
                }

                break;
        }
    }

    /// <summary>
    ///     Adds the tab box border lines (cap, side edges) to the parent's <see cref="View.LineCanvas"/>.
    ///     These are the lines that would be drawn by the Label's border if the draw pipeline timing allowed it.
    /// </summary>
    private static void AddTabBoxLines (
        LineCanvas lc,
        Rectangle headerRect,
        Rectangle clipped,
        Side side,
        Thickness tabThickness,
        LineStyle lineStyle,
        Attribute? attribute)
    {
        switch (side)
        {
            case Side.Top:
            {
                // Cap line (top edge of tab box)
                if (tabThickness.Top > 0 && clipped.Y == headerRect.Y)
                {
                    int capX = clipped.X > headerRect.X ? clipped.X - 1 : clipped.X;
                    int capW = clipped.Width + (clipped.X > headerRect.X ? 1 : 0) + (clipped.Right < headerRect.Right ? 1 : 0);
                    lc.AddLine (new Point (capX, clipped.Y), capW, Orientation.Horizontal, lineStyle, attribute);
                }

                // Left edge
                if (tabThickness.Left > 0 && clipped.X == headerRect.X)
                {
                    lc.AddLine (new Point (clipped.X, clipped.Y), clipped.Height, Orientation.Vertical, lineStyle, attribute);
                }

                // Right edge
                if (tabThickness.Right > 0 && clipped.Right == headerRect.Right)
                {
                    lc.AddLine (new Point (clipped.Right - 1, clipped.Y), clipped.Height, Orientation.Vertical, lineStyle, attribute);
                }

                break;
            }

            case Side.Bottom:
            {
                // Cap line (bottom edge of tab box)
                if (tabThickness.Bottom > 0 && clipped.Bottom == headerRect.Bottom)
                {
                    int capX = clipped.X > headerRect.X ? clipped.X - 1 : clipped.X;
                    int capW = clipped.Width + (clipped.X > headerRect.X ? 1 : 0) + (clipped.Right < headerRect.Right ? 1 : 0);
                    lc.AddLine (new Point (capX, clipped.Bottom - 1), capW, Orientation.Horizontal, lineStyle, attribute);
                }

                // Left edge
                if (tabThickness.Left > 0 && clipped.X == headerRect.X)
                {
                    lc.AddLine (new Point (clipped.X, clipped.Y), clipped.Height, Orientation.Vertical, lineStyle, attribute);
                }

                // Right edge
                if (tabThickness.Right > 0 && clipped.Right == headerRect.Right)
                {
                    lc.AddLine (new Point (clipped.Right - 1, clipped.Y), clipped.Height, Orientation.Vertical, lineStyle, attribute);
                }

                break;
            }

            case Side.Left:
            {
                // Cap line (left edge of tab box)
                if (tabThickness.Left > 0 && clipped.X == headerRect.X)
                {
                    int capY = clipped.Y > headerRect.Y ? clipped.Y - 1 : clipped.Y;
                    int capH = clipped.Height + (clipped.Y > headerRect.Y ? 1 : 0) + (clipped.Bottom < headerRect.Bottom ? 1 : 0);
                    lc.AddLine (new Point (clipped.X, capY), capH, Orientation.Vertical, lineStyle, attribute);
                }

                // Top edge
                if (tabThickness.Top > 0 && clipped.Y == headerRect.Y)
                {
                    lc.AddLine (new Point (clipped.X, clipped.Y), clipped.Width, Orientation.Horizontal, lineStyle, attribute);
                }

                // Bottom edge
                if (tabThickness.Bottom > 0 && clipped.Bottom == headerRect.Bottom)
                {
                    lc.AddLine (new Point (clipped.X, clipped.Bottom - 1), clipped.Width, Orientation.Horizontal, lineStyle, attribute);
                }

                break;
            }

            case Side.Right:
            {
                // Cap line (right edge of tab box)
                if (tabThickness.Right > 0 && clipped.Right == headerRect.Right)
                {
                    int capY = clipped.Y > headerRect.Y ? clipped.Y - 1 : clipped.Y;
                    int capH = clipped.Height + (clipped.Y > headerRect.Y ? 1 : 0) + (clipped.Bottom < headerRect.Bottom ? 1 : 0);
                    lc.AddLine (new Point (clipped.Right - 1, capY), capH, Orientation.Vertical, lineStyle, attribute);
                }

                // Top edge
                if (tabThickness.Top > 0 && clipped.Y == headerRect.Y)
                {
                    lc.AddLine (new Point (clipped.X, clipped.Y), clipped.Width, Orientation.Horizontal, lineStyle, attribute);
                }

                // Bottom edge
                if (tabThickness.Bottom > 0 && clipped.Bottom == headerRect.Bottom)
                {
                    lc.AddLine (new Point (clipped.X, clipped.Bottom - 1), clipped.Width, Orientation.Horizontal, lineStyle, attribute);
                }

                break;
            }
        }
    }

    /// <summary>
    ///     Draws the tab-side content border line. For focused depth ≥ 3, draws split segments
    ///     around the gap. For unfocused depth ≥ 3, draws the full line (auto-join creates junctions).
    ///     For depth &lt; 3, draws the full line.
    /// </summary>
    private static void AddTabSideContentBorder (
        LineCanvas lc,
        Rectangle clipped,
        Rectangle headerRect,
        Rectangle contentBorderRect,
        Side side,
        bool hasFocus,
        int depth,
        LineStyle lineStyle,
        Attribute? attribute)
    {
        // Open gap when: focused at depth ≥ 3 (no content-side border on tab), or
        // depth < 3 (content border coincides with tab title row — must not overwrite it).
        bool openGap = (hasFocus && depth >= 3) || depth < 3;

        switch (side)
        {
            case Side.Top:
            {
                int borderY = contentBorderRect.Y;

                if (!openGap)
                {
                    lc.AddLine (new Point (contentBorderRect.X, borderY), contentBorderRect.Width, Orientation.Horizontal, lineStyle, attribute);
                }
                else
                {
                    if (clipped.X > contentBorderRect.X)
                    {
                        lc.AddLine (new Point (contentBorderRect.X, borderY), clipped.X - contentBorderRect.X + 1, Orientation.Horizontal, lineStyle, attribute);
                    }

                    if (clipped.Right - 1 < contentBorderRect.Right - 1)
                    {
                        lc.AddLine (new Point (clipped.Right - 1, borderY), contentBorderRect.Right - (clipped.Right - 1), Orientation.Horizontal, lineStyle, attribute);
                    }
                }

                break;
            }

            case Side.Bottom:
            {
                int borderY = contentBorderRect.Bottom - 1;

                if (!openGap)
                {
                    lc.AddLine (new Point (contentBorderRect.X, borderY), contentBorderRect.Width, Orientation.Horizontal, lineStyle, attribute);
                }
                else
                {
                    if (clipped.X > contentBorderRect.X)
                    {
                        lc.AddLine (new Point (contentBorderRect.X, borderY), clipped.X - contentBorderRect.X + 1, Orientation.Horizontal, lineStyle, attribute);
                    }

                    if (clipped.Right - 1 < contentBorderRect.Right - 1)
                    {
                        lc.AddLine (new Point (clipped.Right - 1, borderY), contentBorderRect.Right - (clipped.Right - 1), Orientation.Horizontal, lineStyle, attribute);
                    }
                }

                break;
            }

            case Side.Left:
            {
                int borderX = contentBorderRect.X;

                if (!openGap)
                {
                    lc.AddLine (new Point (borderX, contentBorderRect.Y), contentBorderRect.Height, Orientation.Vertical, lineStyle, attribute);
                }
                else
                {
                    if (clipped.Y > contentBorderRect.Y)
                    {
                        lc.AddLine (new Point (borderX, contentBorderRect.Y), clipped.Y - contentBorderRect.Y + 1, Orientation.Vertical, lineStyle, attribute);
                    }
                    else if (clipped.Y > headerRect.Y)
                    {
                        // Header clipped at top (overflow) — suppress corner glyph
                        lc.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Y, 1, 1)));
                    }

                    if (clipped.Bottom - 1 < contentBorderRect.Bottom - 1)
                    {
                        lc.AddLine (new Point (borderX, clipped.Bottom - 1), contentBorderRect.Bottom - (clipped.Bottom - 1), Orientation.Vertical, lineStyle, attribute);
                    }
                    else if (clipped.Bottom < headerRect.Bottom)
                    {
                        // Header clipped at bottom (overflow) — suppress corner glyph
                        lc.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Bottom - 1, 1, 1)));
                    }
                }

                break;
            }

            case Side.Right:
            {
                int borderX = contentBorderRect.Right - 1;

                if (!openGap)
                {
                    lc.AddLine (new Point (borderX, contentBorderRect.Y), contentBorderRect.Height, Orientation.Vertical, lineStyle, attribute);
                }
                else
                {
                    if (clipped.Y > contentBorderRect.Y)
                    {
                        lc.AddLine (new Point (borderX, contentBorderRect.Y), clipped.Y - contentBorderRect.Y + 1, Orientation.Vertical, lineStyle, attribute);
                    }
                    else if (clipped.Y > headerRect.Y)
                    {
                        // Header clipped at top (overflow) — suppress corner glyph
                        lc.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Y, 1, 1)));
                    }

                    if (clipped.Bottom - 1 < contentBorderRect.Bottom - 1)
                    {
                        lc.AddLine (new Point (borderX, clipped.Bottom - 1), contentBorderRect.Bottom - (clipped.Bottom - 1), Orientation.Vertical, lineStyle, attribute);
                    }
                    else if (clipped.Bottom < headerRect.Bottom)
                    {
                        // Header clipped at bottom (overflow) — suppress corner glyph
                        lc.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Bottom - 1, 1, 1)));
                    }
                }

                break;
            }
        }
    }

    /// <summary>
    ///     Computes the content area within the tab header where the title text is drawn.
    ///     For depth ≥ 3, always reserves 1 cell on each side (cap, content-side closing edge,
    ///     side edges) — even when the content-side thickness is 0 (focused gap).
    ///     For depth &lt; 3, uses the actual tab thickness.
    /// </summary>
    private static Rectangle ComputeTabContentArea (Rectangle clipped, Rectangle headerRect, Side side, Thickness tabThickness, int depth)
    {
        // For depth >= 3, always reserve 1 cell on the content side for the closing edge/gap
        Thickness effectiveThickness = depth >= 3
                                           ? side switch
                                             {
                                                 Side.Top => tabThickness with { Bottom = 1 },
                                                 Side.Bottom => tabThickness with { Top = 1 },
                                                 Side.Left => tabThickness with { Right = 1 },
                                                 Side.Right => tabThickness with { Left = 1 },
                                                 _ => tabThickness
                                             }
                                           : tabThickness;

        int left = clipped.X + (clipped.X == headerRect.X ? effectiveThickness.Left : 0);
        int top = clipped.Y + (clipped.Y == headerRect.Y ? effectiveThickness.Top : 0);
        int right = clipped.Right - (clipped.Right == headerRect.Right ? effectiveThickness.Right : 0);
        int bottom = clipped.Bottom - (clipped.Bottom == headerRect.Bottom ? effectiveThickness.Bottom : 0);

        int w = right - left;
        int h = bottom - top;

        if (w <= 0 || h <= 0)
        {
            return Rectangle.Empty;
        }

        return new Rectangle (left, top, w, h);
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

        // Tab mode: completely separate codepath with edge-based border positioning
        if (border.Settings.FastHasFlags (BorderSettings.Tab))
        {
            return DrawTabBorder (border, context);
        }

        // ═══════════════════════════════════════════════════════════════
        //  Legacy path — no Tab. Title bar decoration and standard borders.
        // ═══════════════════════════════════════════════════════════════
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
        bool hasTitle = border.Settings.FastHasFlags (BorderSettings.Title);

        if (hasTitle)
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

        // Draw title text
        if (Driver is { }
            && Adornment.Parent is { }
            && canDrawBorder
            && maxTitleWidth > 0
            && hasTitle
            && !string.IsNullOrEmpty (Adornment.Parent?.Title)
            && Adornment!.Thickness.Top > 0)
        {
            Rectangle titleRect = new (borderBounds.X + 2, titleY, maxTitleWidth, 1);

            Adornment.Parent.TitleTextFormatter.Draw (Driver,
                                                      titleRect,
                                                      GetAttributeForRole (Adornment.Parent.HasFocus ? VisualRole.Focus : VisualRole.Normal),
                                                      GetAttributeForRole (Adornment.Parent.HasFocus ? VisualRole.HotFocus : VisualRole.HotNormal));

            LastTitleRect = titleRect;
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
            if (borderBounds.Width < 4 || !hasTitle || string.IsNullOrEmpty (Adornment.Parent?.Title))
            {
                if (border.LineStyle is { })
                {
                    lc?.AddLine (new Point (borderBounds.Location.X, borderBounds.Y),
                                 borderBounds.Width,
                                 Orientation.Horizontal,
                                 border.LineStyle.Value,
                                 normalAttribute);
                }
            }
            else
            {
                // Title bar decoration
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

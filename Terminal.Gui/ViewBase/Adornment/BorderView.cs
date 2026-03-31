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
        border.SettingsChanged += OnSettingsChanged;
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

        ConfigureForTabMode ();
    }

    private void OnSettingsChanged (object? sender, EventArgs e) => ConfigureForTabMode ();

    private bool _tabModeSetTransparent;

    /// <summary>
    ///     Configures persistent state for tab mode. Called when <see cref="Border.Settings"/> or
    ///     <see cref="IAdornment.Thickness"/> changes. Sets <see cref="View.ViewportSettings"/> and
    ///     ensures the <see cref="TabTitleView"/> SubView exists with the correct static properties.
    /// </summary>
    private void ConfigureForTabMode ()
    {
        if (Adornment is not Border border)
        {
            return;
        }

        if (border.Settings.FastHasFlags (BorderSettings.Tab))
        {
            ViewportSettings |= ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;
            _tabModeSetTransparent = true;

            TabTitleView label = EnsureTabTitleView ();

            if (border.LineStyle is { } ls)
            {
                label.BorderStyle = ls;
            }

            label.TextFormatter.Direction = border.TabSide is Side.Left or Side.Right ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;
        }
        else
        {
            // Only clear flags if we set them for tab mode
            if (_tabModeSetTransparent)
            {
                ViewportSettings &= ~(ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse);
                _tabModeSetTransparent = false;
            }

            _tabTitleView?.Visible = false;
        }
    }

    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs args) => UpdateTabTitleViewLayout ();

    /// <summary>
    ///     Computes and sets the <see cref="TabTitleView"/>'s frame, size, border thickness,
    ///     text, and visibility during the layout pass. Called via <see cref="View.SubViewLayout"/>.
    /// </summary>
    private void UpdateTabTitleViewLayout ()
    {
        if (Adornment is not Border border || !border.Settings.FastHasFlags (BorderSettings.Tab))
        {
            return;
        }

        if (_tabTitleView is null)
        {
            return;
        }

        Rectangle borderBounds = GetTabBorderBounds (border);

        if (borderBounds is not { Width: > 0, Height: > 0 })
        {
            _tabTitleView.Visible = false;

            return;
        }

        int tabDepth = GetTabDepth (border);

        if (border.TabLength is null)
        {
            return;
        }
        int tabLength = border.TabLength.Value;
        bool hasFocus = IsFocusedOrLastTab ();

        Rectangle headerRect = ComputeHeaderRect (borderBounds, border.TabSide, border.TabOffset, tabLength, tabDepth);
        Rectangle viewBounds = ComputeViewBounds (borderBounds, border.TabSide, tabDepth);
        Rectangle clipped = Rectangle.Intersect (headerRect, viewBounds);
        bool tabVisible = !clipped.IsEmpty;

        if (!tabVisible)
        {
            _tabTitleView.Visible = false;

            return;
        }

        _tabTitleView.Visible = true;
        _tabTitleView.HotKeySpecifier = Adornment.Parent?.HotKeySpecifier ?? default (Rune);
        _tabTitleView.Text = Adornment.Parent?.Title ?? string.Empty;

        if (border.LineStyle is { } ls)
        {
            _tabTitleView.BorderStyle = ls;
        }

        // Configure the label's border thickness based on depth and focus
        _tabTitleView.Border.Thickness = ComputeTabLabelThickness (border.TabSide, tabDepth, hasFocus, clipped);

        // For Left/Right, render text vertically
        _tabTitleView.TextFormatter.Direction = border.TabSide is Side.Left or Side.Right
                                                    ? TextDirection.TopBottom_LeftRight
                                                    : TextDirection.LeftRight_TopBottom;

        // Convert header rect from screen to BorderView viewport coords
        Point screenOrigin = ViewportToScreen (Point.Empty);
        Rectangle labelFrame = headerRect with { X = headerRect.X - screenOrigin.X, Y = headerRect.Y - screenOrigin.Y };
        _tabTitleView.Frame = labelFrame;

        if (hasFocus && border is { TabSide: Side.Bottom, Thickness.Bottom: > 2 })
        {
            _tabTitleView.Padding.Thickness = new Thickness (0, 1, 0, 0);
        }
        else if (hasFocus && border is { TabSide: Side.Right, Thickness.Right: > 2 })
        {
            _tabTitleView.Padding.Thickness = new Thickness (1, 0, 0, 0);
        }
        else
        {
            _tabTitleView.Padding.Thickness = new Thickness (0);
        }
    }

    private void ShowHideDrawIndicator ()
    {
        if (View.Diagnostics.HasFlag (ViewDiagnosticFlags.DrawIndicator) && Adornment?.Thickness != Thickness.Empty)
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
            DrawIndicator?.Dispose ();
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

    /// <inheritdoc/>
    public override void BeginInit ()
    {
        base.BeginInit ();

        if (Adornment?.Parent is null)
        {
            return;
        }

        ShowHideDrawIndicator ();
        ConfigureForTabMode ();

        MouseHighlightStates |= Adornment.Parent.Arrangement != ViewArrangement.Fixed ? MouseState.Pressed : MouseState.None;
    }

    private Rectangle GetBorderBounds ()
    {
        if (Adornment is null)
        {
            return ViewportToScreen (Viewport);
        }

        Rectangle screenRect = ViewportToScreen (Viewport);

        return new Rectangle (screenRect.X + Math.Max (0, Adornment.Thickness.Left - 1),
                              screenRect.Y + Math.Max (0, Adornment.Thickness.Top - 1),
                              Math.Max (0,
                                        screenRect.Width
                                        - Math.Max (0, Math.Max (0, Adornment.Thickness.Left - 1) + Math.Max (0, Adornment.Thickness.Right - 1))),
                              Math.Max (0,
                                        screenRect.Height
                                        - Math.Max (0, Math.Max (0, Adornment.Thickness.Top - 1) + Math.Max (0, Adornment.Thickness.Bottom - 1))));
    }

    /// <summary>
    ///     Computes the content border rectangle when <see cref="BorderSettings.Tab"/> is set.
    ///     Non-title sides use the outer edge of the thickness. The title side uses <c>thickness - 1</c>
    ///     from the outer edge, leaving a tab header region between the outer edge and the content border line.
    /// </summary>
    private Rectangle GetTabBorderBounds (Border border)
    {
        Rectangle screenRect = ViewportToScreen (Viewport);

        if (Adornment is null)
        {
            return screenRect;
        }

        int left = screenRect.X;
        int top = screenRect.Y;
        int right = screenRect.Right;
        int bottom = screenRect.Bottom;

        // Title side: content border at thickness - 1 from outer edge
        switch (border.TabSide)
        {
            case Side.Top:
                top += Math.Max (0, Adornment.Thickness.Top - 1);

                break;

            case Side.Bottom:
                bottom -= Math.Max (0, Adornment.Thickness.Bottom - 1);

                break;

            case Side.Left:
                left += Math.Max (0, Adornment.Thickness.Left - 1);

                break;

            case Side.Right:
                right -= Math.Max (0, Adornment.Thickness.Right - 1);

                break;
        }

        return new Rectangle (left, top, Math.Max (0, right - left), Math.Max (0, bottom - top));
    }

    private TabTitleView? _tabTitleView;

    /// <summary>Gets the tab title <see cref="View"/>, or <see langword="null"/> if not yet created.</summary>
    public View? TabTitleView => _tabTitleView;

    /// <summary>
    ///     Gets or lazily creates the <see cref="ViewBase.TabTitleView"/> SubView used to render the tab header.
    ///     The view has its own border with <see cref="View.SuperViewRendersLineCanvas"/> = true,
    ///     so its border lines auto-join with the View's content border via <see cref="View.LineCanvas"/>.
    /// </summary>
    private TabTitleView EnsureTabTitleView ()
    {
        if (_tabTitleView is { })
        {
            return _tabTitleView;
        }

        _tabTitleView = new TabTitleView
        {
#if DEBUG
            Id = "TabTitleView",
#endif
        };
        Add (_tabTitleView);

        return _tabTitleView;
    }

    /// <summary>
    ///     Computes the unclipped header rectangle for the given side, offset, length, and depth. In content coordinates.
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
    ///     Computes the full view bounds (content border + header protrusion area). In content coordinates.
    /// </summary>
    private static Rectangle ComputeViewBounds (Rectangle contentBorderRect, Side side, int depth) =>
        side switch
        {
            Side.Top => contentBorderRect with { Y = contentBorderRect.Y - (depth - 1), Height = contentBorderRect.Height + (depth - 1) },
            Side.Bottom => contentBorderRect with { Height = contentBorderRect.Height + (depth - 1) },
            Side.Left => contentBorderRect with { X = contentBorderRect.X - (depth - 1), Width = contentBorderRect.Width + (depth - 1) },
            Side.Right => contentBorderRect with { Width = contentBorderRect.Width + (depth - 1) },
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
    private static Thickness ComputeTabLabelThickness (Side tabSide, int depth, bool hasFocus, Rectangle clipped)
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

    private int GetTabDepth (Border border)
    {
        if (Adornment is null)
        {
            return 0;
        }

        int thickness = border.TabSide switch
        {
            Side.Top => Adornment.Thickness.Top,
            Side.Bottom => Adornment.Thickness.Bottom,
            Side.Left => Adornment.Thickness.Left,
            Side.Right => Adornment.Thickness.Right,
            _ => 3
        };

        return Math.Min (thickness, 3);
    }

    /// <summary>
    ///     Draws the border and tab header when <see cref="BorderSettings.Tab"/> is set.
    ///     Uses a <see cref="Label"/> SubView with its own border and
    ///     <see cref="View.SuperViewRendersLineCanvas"/> = true for the tab header.
    ///     The Label's border lines auto-join with the content border via <see cref="View.LineCanvas"/>.
    /// </summary>
    private bool DrawTabBorder (Border border)
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

        if (border.TabLength is { })
        {
            int tabLength = border.TabLength.Value;
            LineStyle lineStyle = border.LineStyle.Value;
            bool hasFocus = IsFocusedOrLastTab ();

            // Compute tab header geometry
            Rectangle headerRect = ComputeHeaderRect (borderBounds, border.TabSide, border.TabOffset, tabLength, tabDepth);
            Rectangle viewBounds = ComputeViewBounds (borderBounds, border.TabSide, tabDepth);
            Rectangle clipped = Rectangle.Intersect (headerRect, viewBounds);
            bool tabVisible = !clipped.IsEmpty;

            // Draw the 3 non-tab-side content border lines (always drawn).
            // The tab-side line is handled below — conditionally, based on whether the tab is visible.
            if (Adornment.Thickness.Top > 0 && (border.TabSide != Side.Top || !tabVisible))
            {
                lc.AddLine (new Point (borderBounds.X, borderBounds.Y), borderBounds.Width, Orientation.Horizontal, lineStyle, normalAttribute);
            }

            if (Adornment.Thickness.Bottom > 0 && (border.TabSide != Side.Bottom || !tabVisible))
            {
                lc.AddLine (new Point (borderBounds.X, borderBounds.Bottom - 1), borderBounds.Width, Orientation.Horizontal, lineStyle, normalAttribute);
            }

            if (Adornment.Thickness.Left > 0 && (border.TabSide != Side.Left || !tabVisible))
            {
                lc.AddLine (new Point (borderBounds.X, borderBounds.Y), borderBounds.Height, Orientation.Vertical, lineStyle, normalAttribute);
            }

            if (Adornment.Thickness.Right > 0 && (border.TabSide != Side.Right || !tabVisible))
            {
                lc.AddLine (new Point (borderBounds.Right - 1, borderBounds.Y), borderBounds.Height, Orientation.Vertical, lineStyle, normalAttribute);
            }

            // Draw the tab-side content border (gap segments around the tab)
            if (tabVisible)
            {
                AddTabSideContentBorder (lc,
                                         clipped,
                                         headerRect,
                                         borderBounds,
                                         border.TabSide,
                                         hasFocus,
                                         tabDepth,
                                         lineStyle,
                                         normalAttribute);
            }
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

    private bool IsFocusedOrLastTab ()
    {
        if (Adornment is not Border border || !border.Settings.FastHasFlags (BorderSettings.Tab))
        {
            return false;
        }

        // If the Parent is a Tab, and it is the last subview of it's SuperView, treat it as though it is focused
        if (border.Parent is Tab { SuperView: { } } tab && tab.SuperView?.SubViews.LastOrDefault () == tab)
        {
            return true;
        }

        return border.Parent?.HasFocus ?? false;
    }

    /// <summary>
    ///     Draws the tab-side content border line. For focused depth ≥ 3, draws split segments
    ///     around the gap. For unfocused depth ≥ 3, draws the full line (auto-join creates junctions).
    ///     For depth &lt; 3, draws the full line.
    /// </summary>
    private static void AddTabSideContentBorder (LineCanvas lc,
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
                        // Reserve the gap cells so overlapped compositing suppresses
                        // lower-Z views' content border lines at these positions.
                        int gapStart = clipped.X + 1;
                        int gapEnd = clipped.Right - 1;

                        if (gapEnd > gapStart)
                        {
                            lc.Reserve (new Rectangle (gapStart, borderY, gapEnd - gapStart, 1));
                        }

                        if (clipped.X > contentBorderRect.X)
                        {
                            lc.AddLine (new Point (contentBorderRect.X, borderY),
                                        clipped.X - contentBorderRect.X + 1,
                                        Orientation.Horizontal,
                                        lineStyle,
                                        attribute);
                        }

                        if (clipped.Right - 1 < contentBorderRect.Right - 1)
                        {
                            lc.AddLine (new Point (clipped.Right - 1, borderY),
                                        contentBorderRect.Right - (clipped.Right - 1),
                                        Orientation.Horizontal,
                                        lineStyle,
                                        attribute);
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
                        int gapStart = clipped.X + 1;
                        int gapEnd = clipped.Right - 1;

                        if (gapEnd > gapStart)
                        {
                            lc.Reserve (new Rectangle (gapStart, borderY, gapEnd - gapStart, 1));
                        }

                        if (clipped.X > contentBorderRect.X)
                        {
                            lc.AddLine (new Point (contentBorderRect.X, borderY),
                                        clipped.X - contentBorderRect.X + 1,
                                        Orientation.Horizontal,
                                        lineStyle,
                                        attribute);
                        }

                        if (clipped.Right - 1 < contentBorderRect.Right - 1)
                        {
                            lc.AddLine (new Point (clipped.Right - 1, borderY),
                                        contentBorderRect.Right - (clipped.Right - 1),
                                        Orientation.Horizontal,
                                        lineStyle,
                                        attribute);
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
                        int gapStart = clipped.Y + 1;
                        int gapEnd = clipped.Bottom - 1;

                        if (gapEnd > gapStart)
                        {
                            lc.Reserve (new Rectangle (borderX, gapStart, 1, gapEnd - gapStart));
                        }

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
                            lc.AddLine (new Point (borderX, clipped.Bottom - 1),
                                        contentBorderRect.Bottom - (clipped.Bottom - 1),
                                        Orientation.Vertical,
                                        lineStyle,
                                        attribute);
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
                        int gapStart = clipped.Y + 1;
                        int gapEnd = clipped.Bottom - 1;

                        if (gapEnd > gapStart)
                        {
                            lc.Reserve (new Rectangle (borderX, gapStart, 1, gapEnd - gapStart));
                        }

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
                            lc.AddLine (new Point (borderX, clipped.Bottom - 1),
                                        contentBorderRect.Bottom - (clipped.Bottom - 1),
                                        Orientation.Vertical,
                                        lineStyle,
                                        attribute);
                        }
                        else if (clipped.Bottom < headerRect.Bottom)
                        {
                            // Header clipped at bottom (overflow) — suppress corner glyph
                            lc.Exclude (new Region (new Rectangle (borderX, contentBorderRect.Bottom - 1, 1, 1)));
                        }
                    }

                    break;
                }

            default: throw new ArgumentOutOfRangeException (nameof (side), side, null);
        }
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
            return DrawTabBorder (border);
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
            switch (Adornment.Thickness.Top)
            {
                case 2:
                    topTitleLineY = borderBounds.Y - 1;
                    titleY = topTitleLineY + 1;
                    titleBarsLength = 2;

                    break;

                case 3:
                    topTitleLineY = borderBounds.Y - (Adornment.Thickness.Top - 1);
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
            && Adornment.Thickness.Top > 0)
        {
            Rectangle titleRect = new (borderBounds.X + 2, titleY, maxTitleWidth, 1);

            Adornment.Parent.TitleTextFormatter.Draw (Driver,
                                                      titleRect,
                                                      GetAttributeForRole (Adornment.Parent.HasFocus ? VisualRole.Focus : VisualRole.Normal),
                                                      GetAttributeForRole (Adornment.Parent.HasFocus ? VisualRole.HotFocus : VisualRole.HotNormal));

            context?.AddDrawnRectangle (titleRect);
            Adornment.Parent?.LineCanvas.Exclude (new Region (titleRect));
        }

        if (!canDrawBorder || (Adornment as Border)?.LineStyle is null || (Adornment as Border)?.LineStyle == LineStyle.None)
        {
            return true;
        }

        LineCanvas? lc = Adornment.Parent?.LineCanvas;

        bool drawTop = Adornment.Thickness.Top > 0 && Frame is { Width: > 1, Height: >= 1 };
        bool drawLeft = Adornment.Thickness.Left > 0 && (Frame.Height > 1 || Adornment.Thickness.Top == 0);
        bool drawBottom = Adornment.Thickness.Bottom > 0 && Frame is { Width: > 1, Height: > 1 };
        bool drawRight = Adornment.Thickness.Right > 0 && (Frame.Height > 1 || Adornment.Thickness.Top == 0);

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
                    lc?.AddLine (borderBounds.Location with { Y = borderBounds.Y },
                                 borderBounds.Width,
                                 Orientation.Horizontal,
                                 border.LineStyle.Value,
                                 normalAttribute);
                }
            }
            else
            {
                // Title bar decoration
                if (Adornment.Thickness.Top == 2)
                {
                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 border.LineStyle ?? LineStyle.None,
                                 normalAttribute);
                }

                if (borderBounds.Width >= 4 && Adornment.Thickness.Top > 2)
                {
                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 border.LineStyle ?? LineStyle.None,
                                 normalAttribute);

                    lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY + 2),
                                 Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                 Orientation.Horizontal,
                                 border.LineStyle ?? LineStyle.None,
                                 normalAttribute);
                }

                lc?.AddLine (borderBounds.Location with { Y = titleY }, 2, Orientation.Horizontal, border.LineStyle ?? LineStyle.None, normalAttribute);

                lc?.AddLine (new Point (borderBounds.X + 1, topTitleLineY), titleBarsLength, Orientation.Vertical, LineStyle.Single, normalAttribute);

                lc?.AddLine (new Point (borderBounds.X + 1 + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2) - 1, topTitleLineY),
                             titleBarsLength,
                             Orientation.Vertical,
                             LineStyle.Single,
                             normalAttribute);

                lc?.AddLine (new Point (borderBounds.X + 1 + Math.Min (borderBounds.Width - 2, maxTitleWidth + 2) - 1, titleY),
                             borderBounds.Width - Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                             Orientation.Horizontal,
                             border.LineStyle ?? LineStyle.None,
                             normalAttribute);
            }
        }

#if !SUBVIEW_BASED_BORDER

        if (drawLeft)
        {
            lc?.AddLine (borderBounds.Location with { Y = titleY }, sideLineLength, Orientation.Vertical, border.LineStyle ?? LineStyle.None, normalAttribute);
        }
#endif

        if (drawBottom)
        {
            lc?.AddLine (new Point (borderBounds.X, borderBounds.Y + borderBounds.Height - 1),
                         borderBounds.Width,
                         Orientation.Horizontal,
                         border.LineStyle ?? LineStyle.None,
                         normalAttribute);
        }

        if (drawRight)
        {
            lc?.AddLine (new Point (borderBounds.X + borderBounds.Width - 1, titleY),
                         sideLineLength,
                         Orientation.Vertical,
                         border.LineStyle ?? LineStyle.None,
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
                Adornment.Parent?.TitleTextFormatter.Draw (Driver,
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

        if (lc is { } && border.Settings.FastHasFlags (BorderSettings.Gradient))
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
            lc?.Fill = null;
            _cachedGradientFill = null;
        }

        return true;
    }

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

#nullable enable
using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>The Border for a <see cref="View"/>. Accessed via <see cref="View.Border"/></summary>
/// <remarks>
///     <para>
///         Renders a border around the view with the <see cref="View.Title"/>. A border using <see cref="LineStyle"/>
///         will be drawn on the sides of <see cref="Thickness"/> that are greater than zero.
///     </para>
///     <para>
///         The <see cref="View.Title"/> of <see cref="Adornment.Parent"/> will be drawn based on the value of
///         <see cref="Thickness.Top"/>:
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
        Parent = parent;
        CanFocus = false;
        TabStop = TabBehavior.TabGroup;

        Application.GrabbingMouse += Application_GrabbingMouse;
        Application.UnGrabbingMouse += Application_UnGrabbingMouse;

        HighlightStyle |= HighlightStyle.Pressed;
        Highlight += Border_Highlight;

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
                DrawIndicator = new()
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

        ShowHideDrawIndicator ();
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
    public override ColorScheme? ColorScheme
    {
        get => base.ColorScheme ?? Parent?.ColorScheme;
        set
        {
            base.ColorScheme = value;
            Parent?.SetNeedsDraw ();
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

    #region Mouse Support

    private Color? _savedForeColor;

    private void Border_Highlight (object? sender, CancelEventArgs<HighlightStyle> e)
    {
        if (!Parent!.Arrangement.HasFlag (ViewArrangement.Movable))
        {
            e.Cancel = true;

            return;
        }

        if (e.NewValue.HasFlag (HighlightStyle.Pressed))
        {
            if (!_savedForeColor.HasValue)
            {
                _savedForeColor = ColorScheme!.Normal.Foreground;
            }

            var cs = new ColorScheme (ColorScheme)
            {
                Normal = new (ColorScheme!.Normal.Foreground.GetHighlightColor (), ColorScheme.Normal.Background)
            };
            ColorScheme = cs;
        }

        if (e.NewValue == HighlightStyle.None && _savedForeColor.HasValue)
        {
            var cs = new ColorScheme (ColorScheme)
            {
                Normal = new (_savedForeColor.Value, ColorScheme!.Normal.Background)
            };
            ColorScheme = cs;
        }

        Parent?.SetNeedsDraw ();
        e.Cancel = true;
    }

    private Point? _dragPosition;
    private Point _startGrabPoint;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        // BUGBUG: See https://github.com/gui-cs/Terminal.Gui/issues/3312
        if (!_dragPosition.HasValue && mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed)

            // HACK: Prevents Window from being draggable if it's Top
            //&& Parent is Toplevel { Modal: true }
           )
        {
            Parent!.SetFocus ();

            if (!Parent!.Arrangement.HasFlag (ViewArrangement.Movable)
                && !Parent!.Arrangement.HasFlag (ViewArrangement.BottomResizable)
                && !Parent!.Arrangement.HasFlag (ViewArrangement.TopResizable)
                && !Parent!.Arrangement.HasFlag (ViewArrangement.LeftResizable)
                && !Parent!.Arrangement.HasFlag (ViewArrangement.RightResizable)
               )
            {
                return false;
            }

            // Only start grabbing if the user clicks in the Thickness area
            // Adornment.Contains takes Parent SuperView=relative coords.
            if (Contains (new (mouseEvent.Position.X + Parent.Frame.X + Frame.X, mouseEvent.Position.Y + Parent.Frame.Y + Frame.Y)))
            {
                if (Arranging != ViewArrangement.Fixed)
                {
                    EndArrangeMode ();
                }

                // Set the start grab point to the Frame coords
                _startGrabPoint = new (mouseEvent.Position.X + Frame.X, mouseEvent.Position.Y + Frame.Y);
                _dragPosition = mouseEvent.Position;
                Application.GrabMouse (this);

                SetPressedHighlight (HighlightStyle);

                // Arrange Mode -
                // TODO: This code can be refactored to be more readable and maintainable.

                // If not resizable, but movable: Drag anywhere is move
                // If resizable and movable: Drag on top is move, other 3 sides are size
                // If not movable, but resizable: Drag on any side sizes.

                // Get rectangle representing Thickness.Top
                // If mouse is in that rectangle, set _arranging to ViewArrangement.Movable
                Rectangle sideRect;

                // If mouse is in any other rectangle, set _arranging to ViewArrangement.<side>
                if (Parent!.Arrangement.HasFlag (ViewArrangement.LeftResizable))
                {
                    sideRect = new (Frame.X, Frame.Y + Thickness.Top, Thickness.Left, Frame.Height - Thickness.Top - Thickness.Bottom);

                    if (sideRect.Contains (_startGrabPoint))
                    {
                        EnterArrangeMode (ViewArrangement.LeftResizable);

                        return true;
                    }
                }

                if (Parent!.Arrangement.HasFlag (ViewArrangement.RightResizable))
                {
                    sideRect = new (
                                    Frame.X + Frame.Width - Thickness.Right,
                                    Frame.Y + Thickness.Top,
                                    Thickness.Right,
                                    Frame.Height - Thickness.Top - Thickness.Bottom);

                    if (sideRect.Contains (_startGrabPoint))
                    {
                        EnterArrangeMode (ViewArrangement.RightResizable);

                        return true;
                    }
                }

                if (Parent!.Arrangement.HasFlag (ViewArrangement.TopResizable) && !Parent!.Arrangement.HasFlag (ViewArrangement.Movable))
                {
                    sideRect = new (Frame.X + Thickness.Left, Frame.Y, Frame.Width - Thickness.Left - Thickness.Right, Thickness.Top);

                    if (sideRect.Contains (_startGrabPoint))
                    {
                        EnterArrangeMode (ViewArrangement.TopResizable);

                        return true;
                    }
                }

                if (Parent!.Arrangement.HasFlag (ViewArrangement.BottomResizable))
                {
                    sideRect = new (
                                    Frame.X + Thickness.Left,
                                    Frame.Y + Frame.Height - Thickness.Bottom,
                                    Frame.Width - Thickness.Left - Thickness.Right,
                                    Thickness.Bottom);

                    if (sideRect.Contains (_startGrabPoint))
                    {
                        EnterArrangeMode (ViewArrangement.BottomResizable);

                        return true;
                    }
                }

                if (Parent!.Arrangement.HasFlag (ViewArrangement.BottomResizable) && Parent!.Arrangement.HasFlag (ViewArrangement.LeftResizable))
                {
                    sideRect = new (Frame.X, Frame.Height - Thickness.Top, Thickness.Left, Thickness.Bottom);

                    if (sideRect.Contains (_startGrabPoint))
                    {
                        EnterArrangeMode (ViewArrangement.BottomResizable | ViewArrangement.LeftResizable);

                        return true;
                    }
                }

                if (Parent!.Arrangement.HasFlag (ViewArrangement.BottomResizable) && Parent!.Arrangement.HasFlag (ViewArrangement.RightResizable))
                {
                    sideRect = new (Frame.X + Frame.Width - Thickness.Right, Frame.Height - Thickness.Top, Thickness.Right, Thickness.Bottom);

                    if (sideRect.Contains (_startGrabPoint))
                    {
                        EnterArrangeMode (ViewArrangement.BottomResizable | ViewArrangement.RightResizable);

                        return true;
                    }
                }

                if (Parent!.Arrangement.HasFlag (ViewArrangement.TopResizable) && Parent!.Arrangement.HasFlag (ViewArrangement.RightResizable))
                {
                    sideRect = new (Frame.X + Frame.Width - Thickness.Right, Frame.Y, Thickness.Right, Thickness.Top);

                    if (sideRect.Contains (_startGrabPoint))
                    {
                        EnterArrangeMode (ViewArrangement.TopResizable | ViewArrangement.RightResizable);

                        return true;
                    }
                }

                if (Parent!.Arrangement.HasFlag (ViewArrangement.TopResizable) && Parent!.Arrangement.HasFlag (ViewArrangement.LeftResizable))
                {
                    sideRect = new (Frame.X, Frame.Y, Thickness.Left, Thickness.Top);

                    if (sideRect.Contains (_startGrabPoint))
                    {
                        EnterArrangeMode (ViewArrangement.TopResizable | ViewArrangement.LeftResizable);

                        return true;
                    }
                }

                if (Parent!.Arrangement.HasFlag (ViewArrangement.Movable))
                {
                    //sideRect = new (Frame.X + Thickness.Left, Frame.Y, Frame.Width - Thickness.Left - Thickness.Right, Thickness.Top);

                    //if (sideRect.Contains (_startGrabPoint))
                    {
                        EnterArrangeMode (ViewArrangement.Movable);

                        return true;
                    }
                }
            }

            return true;
        }

        if (mouseEvent.Flags is (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && Application.MouseGrabView == this)
        {
            if (_dragPosition.HasValue)
            {
                if (Parent!.SuperView is null)
                {
                    // Redraw the entire app window.
                    Application.Top!.SetNeedsDraw ();
                }
                else
                {
                    Parent.SuperView.SetNeedsDraw ();
                }

                _dragPosition = mouseEvent.Position;

                Point parentLoc = Parent.SuperView?.ScreenToViewport (new (mouseEvent.ScreenPosition.X, mouseEvent.ScreenPosition.Y))
                                  ?? mouseEvent.ScreenPosition;

                int minHeight = Thickness.Vertical + Parent!.Margin!.Thickness.Bottom;
                int minWidth = Thickness.Horizontal + Parent!.Margin!.Thickness.Right;

                // TODO: This code can be refactored to be more readable and maintainable.
                switch (Arranging)
                {
                    case ViewArrangement.Movable:

                        GetLocationEnsuringFullVisibility (
                                                           Parent,
                                                           parentLoc.X - _startGrabPoint.X,
                                                           parentLoc.Y - _startGrabPoint.Y,
                                                           out int nx,
                                                           out int ny

                                                           //,
                                                           // out _
                                                          );

                        Parent.X = parentLoc.X - _startGrabPoint.X;
                        Parent.Y = parentLoc.Y - _startGrabPoint.Y;

                        break;

                    case ViewArrangement.TopResizable:
                        // Get how much the mouse has moved since the start of the drag
                        // and adjust the height of the parent by that amount
                        int deltaY = parentLoc.Y - Parent.Frame.Y;
                        int newHeight = Math.Max (minHeight, Parent.Frame.Height - deltaY);

                        if (newHeight != Parent.Frame.Height)
                        {
                            Parent.Height = newHeight;
                            Parent.Y = parentLoc.Y - _startGrabPoint.Y;
                        }

                        break;

                    case ViewArrangement.BottomResizable:
                        Parent.Height = Math.Max (minHeight, parentLoc.Y - Parent.Frame.Y + Parent!.Margin.Thickness.Bottom + 1);

                        break;

                    case ViewArrangement.LeftResizable:
                        // Get how much the mouse has moved since the start of the drag
                        // and adjust the height of the parent by that amount
                        int deltaX = parentLoc.X - Parent.Frame.X;
                        int newWidth = Math.Max (minWidth, Parent.Frame.Width - deltaX);

                        if (newWidth != Parent.Frame.Width)
                        {
                            Parent.Width = newWidth;
                            Parent.X = parentLoc.X - _startGrabPoint.X;
                        }

                        break;

                    case ViewArrangement.RightResizable:
                        Parent.Width = Math.Max (minWidth, parentLoc.X - Parent.Frame.X + Parent!.Margin.Thickness.Right + 1);

                        break;

                    case ViewArrangement.BottomResizable | ViewArrangement.RightResizable:
                        Parent.Width = Math.Max (minWidth, parentLoc.X - Parent.Frame.X + Parent!.Margin.Thickness.Right + 1);
                        Parent.Height = Math.Max (minHeight, parentLoc.Y - Parent.Frame.Y + Parent!.Margin.Thickness.Bottom + 1);

                        break;

                    case ViewArrangement.BottomResizable | ViewArrangement.LeftResizable:
                        int dX = parentLoc.X - Parent.Frame.X;
                        int newW = Math.Max (minWidth, Parent.Frame.Width - dX);

                        if (newW != Parent.Frame.Width)
                        {
                            Parent.Width = newW;
                            Parent.X = parentLoc.X - _startGrabPoint.X;
                        }

                        Parent.Height = Math.Max (minHeight, parentLoc.Y - Parent.Frame.Y + Parent!.Margin.Thickness.Bottom + 1);

                        break;

                    case ViewArrangement.TopResizable | ViewArrangement.RightResizable:
                        int dY = parentLoc.Y - Parent.Frame.Y;
                        int newH = Math.Max (minHeight, Parent.Frame.Height - dY);

                        if (newH != Parent.Frame.Height)
                        {
                            Parent.Height = newH;
                            Parent.Y = parentLoc.Y - _startGrabPoint.Y;
                        }

                        Parent.Width = Math.Max (minWidth, parentLoc.X - Parent.Frame.X + Parent!.Margin.Thickness.Right + 1);

                        break;

                    case ViewArrangement.TopResizable | ViewArrangement.LeftResizable:
                        int dY2 = parentLoc.Y - Parent.Frame.Y;
                        int newH2 = Math.Max (minHeight, Parent.Frame.Height - dY2);

                        if (newH2 != Parent.Frame.Height)
                        {
                            Parent.Height = newH2;
                            Parent.Y = parentLoc.Y - _startGrabPoint.Y;
                        }

                        int dX2 = parentLoc.X - Parent.Frame.X;
                        int newW2 = Math.Max (minWidth, Parent.Frame.Width - dX2);

                        if (newW2 != Parent.Frame.Width)
                        {
                            Parent.Width = newW2;
                            Parent.X = parentLoc.X - _startGrabPoint.X;
                        }

                        break;
                }

                return true;
            }
        }

        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Released) && _dragPosition.HasValue)
        {
            _dragPosition = null;
            Application.UngrabMouse ();
            SetPressedHighlight (HighlightStyle.None);

            EndArrangeMode ();

            return true;
        }

        return false;
    }

    private void Application_GrabbingMouse (object? sender, GrabMouseEventArgs e)
    {
        if (Application.MouseGrabView == this && _dragPosition.HasValue)
        {
            e.Cancel = true;
        }
    }

    private void Application_UnGrabbingMouse (object? sender, GrabMouseEventArgs e)
    {
        if (Application.MouseGrabView == this && _dragPosition.HasValue)
        {
            e.Cancel = true;
        }
    }

    #endregion Mouse Support

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
            Attribute focus = Parent.GetNormalColor ();

            if (Parent.SuperView is { } && Parent.SuperView?.Subviews!.Count (s => s.CanFocus) > 1)
            {
                // Only use focus color if there are multiple focusable views
                focus = GetFocusColor ();
            }

            Rectangle titleRect = new (borderBounds.X + 2, titleY, maxTitleWidth, 1);

            Parent.TitleTextFormatter.Draw (
                                            titleRect,
                                            Parent.HasFocus ? focus : GetNormalColor (),
                                            Parent.HasFocus ? focus : GetHotNormalColor ());
            Parent?.LineCanvas.Exclude (new (titleRect));
        }

        if (canDrawBorder && LineStyle != LineStyle.None)
        {
            LineCanvas? lc = Parent?.LineCanvas;

            bool drawTop = Thickness.Top > 0 && Frame.Width > 1 && Frame.Height >= 1;
            bool drawLeft = Thickness.Left > 0 && (Frame.Height > 1 || Thickness.Top == 0);
            bool drawBottom = Thickness.Bottom > 0 && Frame.Width > 1 && Frame.Height > 1;
            bool drawRight = Thickness.Right > 0 && (Frame.Height > 1 || Thickness.Top == 0);

            Attribute prevAttr = Driver?.GetAttribute () ?? Attribute.Default;

            if (ColorScheme is { })
            {
                SetAttribute (GetNormalColor ());
            }
            else
            {
                SetAttribute (Parent!.GetNormalColor ());
            }

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
                                 Driver?.GetAttribute ()
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
                                     Driver?.GetAttribute ()
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
                                     Driver?.GetAttribute ()
                                    );

                        lc?.AddLine (
                                     new (borderBounds.X + 1, topTitleLineY + 2),
                                     Math.Min (borderBounds.Width - 2, maxTitleWidth + 2),
                                     Orientation.Horizontal,
                                     lineStyle,
                                     Driver?.GetAttribute ()
                                    );
                    }

                    // ╔╡Title╞═════╗
                    // Add a short horiz line for ╔╡
                    lc?.AddLine (
                                 new (borderBounds.Location.X, titleY),
                                 2,
                                 Orientation.Horizontal,
                                 lineStyle,
                                 Driver?.GetAttribute ()
                                );

                    // Add a vert line for ╔╡
                    lc?.AddLine (
                                 new (borderBounds.X + 1, topTitleLineY),
                                 titleBarsLength,
                                 Orientation.Vertical,
                                 LineStyle.Single,
                                 Driver?.GetAttribute ()
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
                                 Driver?.GetAttribute ()
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
                                 Driver?.GetAttribute ()
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
                             Driver?.GetAttribute ()
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
                             Driver?.GetAttribute ()
                            );
            }

            if (drawRight)
            {
                lc?.AddLine (
                             new (borderBounds.X + borderBounds.Width - 1, titleY),
                             sideLineLength,
                             Orientation.Vertical,
                             lineStyle,
                             Driver?.GetAttribute ()
                            );
            }

            SetAttribute (prevAttr);

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
        var back = new SolidFill (GetNormalColor ().Background);

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

    internal ViewArrangement Arranging { get; set; }

    private Button? _moveButton; // always top-left
    private Button? _allSizeButton;
    private Button? _leftSizeButton;
    private Button? _rightSizeButton;
    private Button? _topSizeButton;
    private Button? _bottomSizeButton;

    /// <summary>
    ///     Starts "Arrange Mode" where <see cref="Adornment.Parent"/> can be moved and/or resized using the mouse
    ///     or keyboard. If <paramref name="arrangement"/> is <see cref="ViewArrangement.Fixed"/> keyboard mode is enabled.
    /// </summary>
    /// <remarks>
    ///     Arrange Mode is exited by the user pressing <see cref="Application.ArrangeKey"/>, <see cref="Key.Esc"/>, or by
    ///     clicking
    ///     the mouse out of the <see cref="Adornment.Parent"/>'s Frame.
    /// </remarks>
    /// <returns></returns>
    public bool? EnterArrangeMode (ViewArrangement arrangement)
    {
        Debug.Assert (Arranging == ViewArrangement.Fixed);

        if (!Parent!.Arrangement.HasFlag (ViewArrangement.Movable)
            && !Parent!.Arrangement.HasFlag (ViewArrangement.BottomResizable)
            && !Parent!.Arrangement.HasFlag (ViewArrangement.TopResizable)
            && !Parent!.Arrangement.HasFlag (ViewArrangement.LeftResizable)
            && !Parent!.Arrangement.HasFlag (ViewArrangement.RightResizable)
           )
        {
            return false;
        }

        // Add Commands and Keybindigs - Note it's ok these get added each time. KeyBindings are cleared in EndArrange()
        AddArrangeModeKeyBindings ();

        Application.MouseEvent += ApplicationOnMouseEvent;

        // TODO: This code can be refactored to be more readable and maintainable.

        // Create buttons for resizing and moving
        if (Parent!.Arrangement.HasFlag (ViewArrangement.Movable))
        {
            Debug.Assert (_moveButton is null);

            _moveButton = new ()
            {
                Id = "moveButton",
                CanFocus = true,
                Width = 1,
                Height = 1,
                NoDecorations = true,
                NoPadding = true,
                ShadowStyle = ShadowStyle.None,
                Text = $"{Glyphs.Move}",
                Visible = false,
                Data = ViewArrangement.Movable
            };
            Add (_moveButton);
        }

        if (Parent!.Arrangement.HasFlag (ViewArrangement.Resizable))
        {
            Debug.Assert (_allSizeButton is null);

            _allSizeButton = new ()
            {
                Id = "allSizeButton",
                CanFocus = true,
                Width = 1,
                Height = 1,
                NoDecorations = true,
                NoPadding = true,
                ShadowStyle = ShadowStyle.None,
                Text = $"{Glyphs.SizeBottomRight}",
                X = Pos.AnchorEnd (),
                Y = Pos.AnchorEnd (),
                Visible = false,
                Data = ViewArrangement.Resizable
            };
            Add (_allSizeButton);
        }

        if (Parent!.Arrangement.HasFlag (ViewArrangement.TopResizable))
        {
            Debug.Assert (_topSizeButton is null);

            _topSizeButton = new ()
            {
                Id = "topSizeButton",
                CanFocus = true,
                Width = 1,
                Height = 1,
                NoDecorations = true,
                NoPadding = true,
                ShadowStyle = ShadowStyle.None,
                Text = $"{Glyphs.SizeVertical}",
                X = Pos.Center () + Parent!.Margin!.Thickness.Horizontal,
                Y = 0,
                Visible = false,
                Data = ViewArrangement.TopResizable
            };
            Add (_topSizeButton);
        }

        if (Parent!.Arrangement.HasFlag (ViewArrangement.RightResizable))
        {
            Debug.Assert (_rightSizeButton is null);

            _rightSizeButton = new ()
            {
                Id = "rightSizeButton",
                CanFocus = true,
                Width = 1,
                Height = 1,
                NoDecorations = true,
                NoPadding = true,
                ShadowStyle = ShadowStyle.None,
                Text = $"{Glyphs.SizeHorizontal}",
                X = Pos.AnchorEnd (),
                Y = Pos.Center () + Parent!.Margin!.Thickness.Vertical / 2,
                Visible = false,
                Data = ViewArrangement.RightResizable
            };
            Add (_rightSizeButton);
        }

        if (Parent!.Arrangement.HasFlag (ViewArrangement.LeftResizable))
        {
            Debug.Assert (_leftSizeButton is null);

            _leftSizeButton = new ()
            {
                Id = "leftSizeButton",
                CanFocus = true,
                Width = 1,
                Height = 1,
                NoDecorations = true,
                NoPadding = true,
                ShadowStyle = ShadowStyle.None,
                Text = $"{Glyphs.SizeHorizontal}",
                X = 0,
                Y = Pos.Center () + Parent!.Margin!.Thickness.Vertical / 2,
                Visible = false,
                Data = ViewArrangement.LeftResizable
            };
            Add (_leftSizeButton);
        }

        if (Parent!.Arrangement.HasFlag (ViewArrangement.BottomResizable))
        {
            Debug.Assert (_bottomSizeButton is null);

            _bottomSizeButton = new ()
            {
                Id = "bottomSizeButton",
                CanFocus = true,
                Width = 1,
                Height = 1,
                NoDecorations = true,
                NoPadding = true,
                ShadowStyle = ShadowStyle.None,
                Text = $"{Glyphs.SizeVertical}",
                X = Pos.Center () + Parent!.Margin!.Thickness.Horizontal / 2,
                Y = Pos.AnchorEnd (),
                Visible = false,
                Data = ViewArrangement.BottomResizable
            };
            Add (_bottomSizeButton);
        }

        if (arrangement == ViewArrangement.Fixed)
        {
            // Keyboard mode
            if (Parent!.Arrangement.HasFlag (ViewArrangement.Movable))
            {
                _moveButton!.Visible = true;
            }

            if (Parent!.Arrangement.HasFlag (ViewArrangement.Resizable))
            {
                _allSizeButton!.Visible = true;
            }

            Arranging = ViewArrangement.Movable;
            CanFocus = true;
            SetFocus ();
        }
        else
        {
            // Mouse mode
            Arranging = arrangement;

            switch (Arranging)
            {
                case ViewArrangement.Movable:
                    _moveButton!.Visible = true;

                    break;

                case ViewArrangement.RightResizable | ViewArrangement.BottomResizable:
                case ViewArrangement.Resizable:
                    _rightSizeButton!.Visible = true;
                    _bottomSizeButton!.Visible = true;

                    if (_allSizeButton is { })
                    {
                        _allSizeButton!.X = Pos.AnchorEnd ();
                        _allSizeButton!.Y = Pos.AnchorEnd ();
                        _allSizeButton!.Visible = true;
                    }

                    break;

                case ViewArrangement.LeftResizable:
                    _leftSizeButton!.Visible = true;

                    break;

                case ViewArrangement.RightResizable:
                    _rightSizeButton!.Visible = true;

                    break;

                case ViewArrangement.TopResizable:
                    _topSizeButton!.Visible = true;

                    break;

                case ViewArrangement.BottomResizable:
                    _bottomSizeButton!.Visible = true;

                    break;

                case ViewArrangement.LeftResizable | ViewArrangement.BottomResizable:
                    _rightSizeButton!.Visible = true;
                    _bottomSizeButton!.Visible = true;

                    if (_allSizeButton is { })
                    {
                        _allSizeButton.X = 0;
                        _allSizeButton.Y = Pos.AnchorEnd ();
                        _allSizeButton.Visible = true;
                    }

                    break;

                case ViewArrangement.LeftResizable | ViewArrangement.TopResizable:
                    _leftSizeButton!.Visible = true;
                    _topSizeButton!.Visible = true;

                    break;

                case ViewArrangement.RightResizable | ViewArrangement.TopResizable:
                    _rightSizeButton!.Visible = true;
                    _topSizeButton!.Visible = true;

                    if (_allSizeButton is { })
                    {
                        _allSizeButton.X = Pos.AnchorEnd ();
                        _allSizeButton.Y = 0;
                        _allSizeButton.Visible = true;
                    }

                    break;
            }
        }

        if (Arranging != ViewArrangement.Fixed)
        {
            if (arrangement == ViewArrangement.Fixed)
            {
                // Keyboard mode - enable nav
                // TODO: Keyboard mode only supports sizing from bottom/right.
                Arranging = (ViewArrangement)(Focused?.Data ?? ViewArrangement.Fixed);
            }

            return true;
        }

        // Hack for now
        EndArrangeMode ();

        return false;
    }

    private void AddArrangeModeKeyBindings ()
    {
        AddCommand (Command.Quit, EndArrangeMode);

        AddCommand (
                    Command.Up,
                    () =>
                    {
                        if (Parent is null)
                        {
                            return false;
                        }

                        if (Arranging == ViewArrangement.Movable)
                        {
                            Parent!.Y = Parent.Y - 1;
                        }

                        if (Arranging == ViewArrangement.Resizable)
                        {
                            if (Parent!.Viewport.Height > 0)
                            {
                                Parent!.Height = Parent.Height! - 1;
                            }
                        }

                        return true;
                    });

        AddCommand (
                    Command.Down,
                    () =>
                    {
                        if (Parent is null)
                        {
                            return false;
                        }

                        if (Arranging == ViewArrangement.Movable)
                        {
                            Parent!.Y = Parent.Y + 1;
                        }

                        if (Arranging == ViewArrangement.Resizable)
                        {
                            Parent!.Height = Parent.Height! + 1;
                        }

                        return true;
                    });

        AddCommand (
                    Command.Left,
                    () =>
                    {
                        if (Parent is null)
                        {
                            return false;
                        }

                        if (Arranging == ViewArrangement.Movable)
                        {
                            Parent!.X = Parent.X - 1;
                        }

                        if (Arranging == ViewArrangement.Resizable)
                        {
                            if (Parent!.Viewport.Width > 0)
                            {
                                Parent!.Width = Parent.Width! - 1;
                            }
                        }

                        return true;
                    });

        AddCommand (
                    Command.Right,
                    () =>
                    {
                        if (Parent is null)
                        {
                            return false;
                        }

                        if (Arranging == ViewArrangement.Movable)
                        {
                            Parent!.X = Parent.X + 1;
                        }

                        if (Arranging == ViewArrangement.Resizable)
                        {
                            Parent!.Width = Parent.Width! + 1;
                        }

                        return true;
                    });

        AddCommand (
                    Command.Tab,
                    () =>
                    {
                        // BUGBUG: If an arrangable view has only arrangable subviews, it's not possible to activate
                        // BUGBUG: ArrangeMode with keyboard for the superview.
                        // BUGBUG: AdvanceFocus should be wise to this and when in ArrangeMode, should move across
                        // BUGBUG: the view hierachy.

                        AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);
                        Arranging = (ViewArrangement)(Focused?.Data ?? ViewArrangement.Fixed);

                        return true; // Always eat
                    });

        AddCommand (
                    Command.BackTab,
                    () =>
                    {
                        AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop);
                        Arranging = (ViewArrangement)(Focused?.Data ?? ViewArrangement.Fixed);

                        return true; // Always eat
                    });

        HotKeyBindings.Add (Key.Esc, Command.Quit);
        HotKeyBindings.Add (Application.ArrangeKey, Command.Quit);
        HotKeyBindings.Add (Key.CursorUp, Command.Up);
        HotKeyBindings.Add (Key.CursorDown, Command.Down);
        HotKeyBindings.Add (Key.CursorLeft, Command.Left);
        HotKeyBindings.Add (Key.CursorRight, Command.Right);

        HotKeyBindings.Add (Key.Tab, Command.Tab);
        HotKeyBindings.Add (Key.Tab.WithShift, Command.BackTab);
    }

    private void ApplicationOnMouseEvent (object? sender, MouseEventArgs e)
    {
        if (e.Flags != MouseFlags.Button1Clicked)
        {
            return;
        }

        // If mouse click is outside of Border.Thickness then exit Arrange Mode
        // e.Position is screen relative
        Point framePos = ScreenToFrame (e.ScreenPosition);

        if (!Thickness.Contains (Frame, framePos))
        {
            EndArrangeMode ();
        }
    }

    private bool? EndArrangeMode ()
    {
        // Debug.Assert (_arranging != ViewArrangement.Fixed);
        Arranging = ViewArrangement.Fixed;

        Application.MouseEvent -= ApplicationOnMouseEvent;

        if (Application.MouseGrabView == this && _dragPosition.HasValue)
        {
            Application.UngrabMouse ();
        }

        if (_moveButton is { })
        {
            Remove (_moveButton);
            _moveButton.Dispose ();
            _moveButton = null;
        }

        if (_allSizeButton is { })
        {
            Remove (_allSizeButton);
            _allSizeButton.Dispose ();
            _allSizeButton = null;
        }

        if (_leftSizeButton is { })
        {
            Remove (_leftSizeButton);
            _leftSizeButton.Dispose ();
            _leftSizeButton = null;
        }

        if (_rightSizeButton is { })
        {
            Remove (_rightSizeButton);
            _rightSizeButton.Dispose ();
            _rightSizeButton = null;
        }

        if (_topSizeButton is { })
        {
            Remove (_topSizeButton);
            _topSizeButton.Dispose ();
            _topSizeButton = null;
        }

        if (_bottomSizeButton is { })
        {
            Remove (_bottomSizeButton);
            _bottomSizeButton.Dispose ();
            _bottomSizeButton = null;
        }

        HotKeyBindings.Clear ();

        if (CanFocus)
        {
            CanFocus = false;
        }

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        Application.GrabbingMouse -= Application_GrabbingMouse;
        Application.UnGrabbingMouse -= Application_UnGrabbingMouse;

        _dragPosition = null;
        base.Dispose (disposing);
    }
}

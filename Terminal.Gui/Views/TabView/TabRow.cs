#nullable enable

namespace Terminal.Gui.Views;

internal class TabRow : View
{
    private readonly TabView _host;
    private readonly View _leftUpScrollIndicator;
    private readonly View _rightDownScrollIndicator;

    public TabRow (TabView host)
    {
        _host = host;
        Id = "tabRow";

        CanFocus = true;
        // Because TabRow has focusable subviews, it must be a TabGroup
        TabStop = TabBehavior.TabGroup;
        Width = Dim.Fill ();

        _rightDownScrollIndicator = new View
        {
            Id = "rightDownScrollIndicator",
            Width = 1,
            Height = 1,
            Visible = false
        };
        _rightDownScrollIndicator.MouseClick += _host.Tab_MouseClick!;

        _leftUpScrollIndicator = new View
        {
            Id = "leftUpScrollIndicator",
            Width = 1,
            Height = 1,
            Visible = false
        };
        _leftUpScrollIndicator.MouseClick += _host.Tab_MouseClick!;

        Add (_rightDownScrollIndicator, _leftUpScrollIndicator);
    }

    /// <inheritdoc />
    public override void EndInit ()
    {
        _host._tabLocations = _host.CalculateViewport (Viewport);

        base.EndInit ();
    }

    protected override bool OnMouseEvent (MouseEventArgs me)
    {
        View? parent = me.View is Adornment adornment ? adornment.Parent : me.View;
        Tab? hit = parent as Tab;

        if (me.IsSingleClicked)
        {
            _host.OnTabClicked (new TabMouseEventArgs (hit!, me));

            // user canceled click
            if (me.Handled)
            {
                return true;
            }

            if (parent == _host.SelectedTab)
            {
                _host.SelectedTab?.SetFocus ();
            }
        }

        if (me.IsWheel && !HasFocus && CanFocus)
        {
            SetFocus ();
        }

        if (me is { IsSingleDoubleOrTripleClicked: false, IsWheel: false })
        {
            return false;
        }

        if (me.IsSingleDoubleOrTripleClicked || me.IsWheel)
        {
            var scrollIndicatorHit = 0;

            if (me.View is { Id: "rightDownScrollIndicator" } || me.Flags.HasFlag (MouseFlags.WheeledDown) || me.Flags.HasFlag (MouseFlags.WheeledRight))
            {
                scrollIndicatorHit = 1;
            }
            else if (me.View is { Id: "leftUpScrollIndicator" } || me.Flags.HasFlag (MouseFlags.WheeledUp) || me.Flags.HasFlag (MouseFlags.WheeledLeft))
            {
                scrollIndicatorHit = -1;
            }

            if (scrollIndicatorHit != 0)
            {
                _host.SwitchTabBy (scrollIndicatorHit);

                return true;
            }

            if (hit is { })
            {
                _host.SelectedTab = hit;

                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        if (_host.SelectedTab is { HasFocus: false, CanFocus: true } && focusedView == this)
        {
            _host.SelectedTab?.SetFocus ();

            return;
        }

        base.OnHasFocusChanged (newHasFocus, previousFocusedView, focusedView);
    }

    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        if (_host._tabLocations is null)
        {
            return;
        }

        if (_host is { SelectedTab: { }, _tabLocations: { } } && !_host._tabLocations!.Contains (_host.SelectedTab))
        {
            _host.SelectedTab = _host._tabLocations [0];
            Application.Invoke (() => _host.SetNeedsLayout ());
        }

        RenderUnderline ();

        base.OnSubViewLayout (args);
    }

    /// <inheritdoc />
    protected override bool OnRenderingLineCanvas ()
    {
        RenderTabLineCanvas ();

        return false;
    }

    private void RenderTabLineCanvas ()
    {
        if (_host._tabLocations is null)
        {
            return;
        }

        Tab [] tabLocations = _host._tabLocations;
        int selectedTab = -1;
        var lc = new LineCanvas ();

        for (var i = 0; i < tabLocations.Length; i++)
        {
            View tab = tabLocations [i];
            Rectangle vts = tab.ViewportToScreen (tab.Viewport);
            int selectedOffset = _host.Style.ShowInitialLine && tabLocations [i] == _host.SelectedTab ? 0 : 1;

            if (tabLocations [i] == _host.SelectedTab)
            {
                selectedTab = i;

                if (i == 0 && _host.TabScrollOffset == 0)
                {
                    switch (_host.Style.TabsSide)
                    {
                        case TabSide.Top:
                            // Lower left vertical line
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Bottom - selectedOffset),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Bottom:
                            // Upper left vertical line
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            break;
                        case TabSide.Right:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException ();
                    }
                }
                else if (i > 0 && i <= tabLocations.Length - 1)
                {
                    switch (_host.Style.TabsSide)
                    {
                        case TabSide.Top:
                            // LRCorner
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Bottom - selectedOffset),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Bottom - selectedOffset),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Bottom:
                            // URCorner
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            break;
                        case TabSide.Right:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException ();
                    }

                    if (_host.Style.ShowInitialLine)
                    {
                        switch (_host.Style.TabsSide)
                        {
                            case TabSide.Top:
                                // Upper left tee
                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Y - 1),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Bottom:
                                // Lower left tee
                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Bottom),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Bottom),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Left:
                                break;
                            case TabSide.Right:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException ();
                        }
                    }
                }

                if (i < tabLocations.Length - 1)
                {
                    if (_host.Style.ShowInitialLine)
                    {
                        switch (_host.Style.TabsSide)
                        {
                            case TabSide.Top:
                                // Upper right tee
                                lc.AddLine (
                                            new Point (vts.Right, vts.Y - 1),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.Right, vts.Y - 1),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Bottom:
                                // Lower right tee
                                lc.AddLine (
                                            new Point (vts.Right, vts.Bottom),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.Right, vts.Bottom),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Left:
                                break;
                            case TabSide.Right:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException ();
                        }
                    }
                }

                switch (_host.Style.TabsSide)
                {
                    case TabSide.Top:
                        //LLCorner
                        lc.AddLine (
                                    new Point (vts.Right, vts.Bottom - selectedOffset),
                                    -1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.Right, vts.Bottom - selectedOffset),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Bottom:
                        //URCorner
                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Left:
                        break;
                    case TabSide.Right:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException ();
                }
            }
            else if (selectedTab == -1)
            {
                if (i == 0 && string.IsNullOrEmpty (tab.Text))
                {
                    switch (_host.Style.TabsSide)
                    {
                        case TabSide.Top:
                            if (_host.Style.ShowInitialLine)
                            {
                                // ULCorner
                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

                            // LLCorner
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Bottom),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Bottom),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Bottom:
                            if (_host.Style.ShowInitialLine)
                            {
                                // LLCorner
                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Bottom),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new Point (vts.X - 1, vts.Bottom),
                                            1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

                            // ULCorner
                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new Point (vts.X - 1, vts.Y - 1),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            break;
                        case TabSide.Right:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException ();
                    }
                }
                else if (i > 0)
                {
                    if (_host.Style.ShowInitialLine || _host.Style.TabsSide == TabSide.Bottom)
                    {
                        // Upper left tee
                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Y - 1),
                                    1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Y - 1),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }

                    // Lower left tee
                    lc.AddLine (
                                new Point (vts.X - 1, vts.Bottom),
                                -1,
                                Orientation.Vertical,
                                tab.BorderStyle
                               );

                    lc.AddLine (
                                new Point (vts.X - 1, vts.Bottom),
                                0,
                                Orientation.Horizontal,
                                tab.BorderStyle
                               );
                }
            }
            else if (i < tabLocations.Length - 1)
            {
                if (_host.Style.ShowInitialLine)
                {
                    // Upper right tee
                    lc.AddLine (
                                new Point (vts.Right, vts.Y - 1),
                                1,
                                Orientation.Vertical,
                                tab.BorderStyle
                               );

                    lc.AddLine (
                                new Point (vts.Right, vts.Y - 1),
                                0,
                                Orientation.Horizontal,
                                tab.BorderStyle
                               );
                }

                if (_host.Style.ShowInitialLine || _host.Style.TabsSide == TabSide.Top)
                {
                    // Lower right tee
                    lc.AddLine (
                                new Point (vts.Right, vts.Bottom),
                                -1,
                                Orientation.Vertical,
                                tab.BorderStyle
                               );

                    lc.AddLine (
                                new Point (vts.Right, vts.Bottom),
                                0,
                                Orientation.Horizontal,
                                tab.BorderStyle
                               );
                }
                else
                {
                    // Upper right tee
                    lc.AddLine (
                                new Point (vts.Right, vts.Y - 1),
                                1,
                                Orientation.Vertical,
                                tab.BorderStyle
                               );

                    lc.AddLine (
                                new Point (vts.Right, vts.Y - 1),
                                0,
                                Orientation.Horizontal,
                                tab.BorderStyle
                               );
                }
            }

            if (i == 0 && i != selectedTab && _host is { TabScrollOffset: 0, Style.ShowBorder: true })
            {
                switch (_host.Style.TabsSide)
                {
                    case TabSide.Top:
                        // Lower left vertical line
                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Bottom),
                                    0,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Bottom),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Bottom:
                        // Upper left vertical line
                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Y - 1),
                                    0,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Y - 1),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Left:
                        break;
                    case TabSide.Right:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException ();
                }
            }

            if (i == tabLocations.Length - 1 && i != selectedTab)
            {
                switch (_host.Style.TabsSide)
                {
                    case TabSide.Top:
                        // Lower right tee
                        lc.AddLine (
                                    new Point (vts.Right, vts.Bottom),
                                    -1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.Right, vts.Bottom),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Bottom:
                        // Upper right tee
                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - 1),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Left:
                        break;
                    case TabSide.Right:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException ();
                }
            }

            if (i == tabLocations.Length - 1)
            {
                var arrowOffset = 1;

                int lastSelectedTab = !_host.Style.ShowInitialLine && i == selectedTab ? 1 :
                                      _host.Style.TabsSide == TabSide.Bottom ? 1 : 0;
                Rectangle tabsBarVts = ViewportToScreen (Viewport);
                int lineLength = tabsBarVts.Right - vts.Right;

                // Right horizontal line
                if (ShouldDrawRightDownScrollIndicator ())
                {
                    if (lineLength - arrowOffset > 0)
                    {
                        switch (_host.Style.TabsSide)
                        {
                            case TabSide.Top:
                                lc.AddLine (
                                            new Point (
                                                       vts.Right,
                                                       vts.Bottom - lastSelectedTab
                                                      ),
                                            lineLength - arrowOffset,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Bottom:
                                lc.AddLine (
                                            new Point (vts.Right, vts.Y - lastSelectedTab),
                                            lineLength - arrowOffset,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Left:
                                break;
                            case TabSide.Right:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException ();
                        }
                    }
                }
                else
                {
                    // Right corner
                    switch (_host.Style.TabsSide)
                    {
                        case TabSide.Top:
                            lc.AddLine (
                                        new Point (vts.Right, vts.Bottom - lastSelectedTab),
                                        lineLength,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Bottom:
                            lc.AddLine (
                                        new Point (vts.Right, vts.Y - lastSelectedTab),
                                        lineLength,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            break;
                        case TabSide.Right:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException ();
                    }

                    if (_host.Style.ShowBorder)
                    {
                        switch (_host.Style.TabsSide)
                        {
                            case TabSide.Top:
                                // More URCorner
                                lc.AddLine (
                                            new Point (
                                                       tabsBarVts.Right - 1,
                                                       vts.Bottom - lastSelectedTab
                                                      ),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Bottom:
                                // More LRCorner
                                lc.AddLine (
                                            new Point (
                                                       tabsBarVts.Right - 1,
                                                       vts.Y - lastSelectedTab
                                                      ),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Left:
                                break;
                            case TabSide.Right:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException ();
                        }
                    }
                }
            }
        }

        LineCanvas.Merge (lc);
    }

    private int GetUnderlineXOrYPosition ()
    {
        switch (_host.Style.TabsSide)
        {
            case TabSide.Top:

                return _host.Style.ShowInitialLine ? 2 : 1;
            case TabSide.Bottom:

                return 0;
            case TabSide.Left:

                return _host.Style.ShowInitialLine ? Frame.Right - 1 : Frame.Right;
            case TabSide.Right:
                return 0;
            default:
                throw new ArgumentOutOfRangeException ();
        }
    }

    /// <summary>Renders the line of the tab that adjoins the content of the tab.</summary>
    private void RenderUnderline ()
    {
        int xOrY = GetUnderlineXOrYPosition ();

        Tab? selected = _host._tabLocations?.FirstOrDefault (t => t == _host.SelectedTab);

        if (selected is null)
        {
            return;
        }

        // Set the correct glyphs for scroll indicators
        switch (_host.Style.TabsSide)
        {
            case TabSide.Top:
            case TabSide.Bottom:
                _rightDownScrollIndicator.Text = Glyphs.RightArrow.ToString ();
                _leftUpScrollIndicator.Text = Glyphs.LeftArrow.ToString ();

                break;
            case TabSide.Left:
            case TabSide.Right:
                _rightDownScrollIndicator.Text = Glyphs.DownArrow.ToString ();
                _leftUpScrollIndicator.Text = Glyphs.UpArrow.ToString ();

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        // position scroll indicators

        // if there are more tabs to the left not visible
        if (_host.TabScrollOffset > 0)
        {
            switch (_host.Style.TabsSide)
            {
                case TabSide.Top:
                case TabSide.Bottom:
                    _leftUpScrollIndicator.X = 0;
                    _leftUpScrollIndicator.Y = xOrY;

                    break;
                case TabSide.Left:
                case TabSide.Right:
                    _leftUpScrollIndicator.X = xOrY;
                    _leftUpScrollIndicator.Y = 0;

                    break;
                default:
                    throw new ArgumentOutOfRangeException ();
            }

            // indicate that
            _leftUpScrollIndicator.Visible = true;

            // Ensures this is clicked instead of the first tab
            MoveSubViewToEnd (_leftUpScrollIndicator);
        }
        else
        {
            _leftUpScrollIndicator.Visible = false;
        }

        // if there are more tabs to the right not visible
        if (ShouldDrawRightDownScrollIndicator ())
        {
            switch (_host.Style.TabsSide)
            {
                case TabSide.Top:
                case TabSide.Bottom:
                    _rightDownScrollIndicator.X = Viewport.Width - 1;
                    _rightDownScrollIndicator.Y = xOrY;

                    break;
                case TabSide.Left:
                case TabSide.Right:
                    _rightDownScrollIndicator.X = xOrY;
                    _rightDownScrollIndicator.Y = Viewport.Height - 1;

                    break;
                default:
                    throw new ArgumentOutOfRangeException ();
            }

            // indicate that
            _rightDownScrollIndicator.Visible = true;

            // Ensures this is clicked instead of the last tab if under this
            MoveSubViewToStart (_rightDownScrollIndicator);
        }
        else
        {
            _rightDownScrollIndicator.Visible = false;
        }
    }

    private bool ShouldDrawRightDownScrollIndicator () { return _host._tabLocations!.LastOrDefault () != _host.Tabs.LastOrDefault (); }
}

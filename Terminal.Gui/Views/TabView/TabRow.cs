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

        _rightDownScrollIndicator = new ()
        {
            Id = "rightDownScrollIndicator",
            Width = 1,
            Height = 1,
            Visible = false
        };
        _rightDownScrollIndicator.MouseClick += _host.Tab_MouseClick!;

        _leftUpScrollIndicator = new ()
        {
            Id = "leftUpScrollIndicator",
            Width = 1,
            Height = 1,
            Visible = false
        };
        _leftUpScrollIndicator.MouseClick += _host.Tab_MouseClick!;

        Add (_rightDownScrollIndicator, _leftUpScrollIndicator);
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        _host._tabLocations = _host.CalculateViewport (Viewport);

        base.EndInit ();
    }

    protected override bool OnMouseEvent (MouseEventArgs me)
    {
        View? parent = me.View is Adornment adornment ? adornment.Parent : me.View;
        var hit = parent as Tab;

        if (me.IsSingleClicked)
        {
            _host.OnTabClicked (new (hit!, me));

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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
                                        new (vts.X - 1, vts.Bottom),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Bottom:
                            // Upper left vertical line
                            lc.AddLine (
                                        new (vts.X - 1, vts.Y - 1),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            // Upper horizontal line
                            lc.AddLine (
                                        new (vts.Right, vts.Y - 1),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Right:
                            // Upper horizontal line
                            lc.AddLine (
                                        new (vts.X - 1, vts.Y - 1),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

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
                                        new (vts.X - 1, vts.Bottom),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Bottom:
                            // URCorner
                            lc.AddLine (
                                        new (vts.X - 1, vts.Y - 1),
                                        1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Y - 1),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            if (Frame.Bottom > tab.Frame.Bottom)
                            {
                                // LRCorner
                                lc.AddLine (
                                            new (vts.Right, vts.Bottom),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.Right, vts.Bottom),
                                            -1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

                            break;
                        case TabSide.Right:
                            if (Frame.Bottom > tab.Frame.Bottom)
                            {
                                // LRCorner
                                lc.AddLine (
                                            new (vts.X - 1, vts.Bottom),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.X - 1, vts.Bottom),
                                            1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

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
                                            new (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.X - 1, vts.Y - 1),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Bottom:
                                // Lower left tee
                                lc.AddLine (
                                            new (vts.X - 1, vts.Bottom),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.X - 1, vts.Bottom),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Left:
                                // Upper left tee
                                lc.AddLine (
                                            new (vts.X - 1, vts.Y - 1),
                                            0,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Right:
                                // Upper left tee
                                lc.AddLine (
                                            new (vts.Right, vts.Y - 1),
                                            0,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.Right, vts.Y - 1),
                                            -1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

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
                                            new (vts.Right, vts.Y - 1),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.Right, vts.Y - 1),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Bottom:
                                // Lower right tee
                                lc.AddLine (
                                            new (vts.Right, vts.Bottom),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.Right, vts.Bottom),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Left:
                                // Upper right tee
                                lc.AddLine (
                                            new (vts.X - 1, vts.Bottom),
                                            0,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.X - 1, vts.Bottom),
                                            1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Right:
                                // Upper right tee
                                lc.AddLine (
                                            new (vts.Right, vts.Bottom),
                                            0,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.Right, vts.Bottom),
                                            -1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            default:
                                throw new ArgumentOutOfRangeException ();
                        }
                    }
                }

                switch (_host.Style.TabsSide)
                {
                    case TabSide.Top:
                        //LRCorner
                        lc.AddLine (
                                    new (vts.Right, vts.Bottom),
                                    -1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new (vts.Right, vts.Bottom),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Bottom:
                        //URCorner
                        lc.AddLine (
                                    new (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Left:
                        if (Frame.Bottom > tab.Frame.Bottom)
                        {
                            //LRCorner
                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }

                        break;
                    case TabSide.Right:
                        if (Frame.Bottom > tab.Frame.Bottom)
                        {
                            //LRCorner
                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }

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
                                            new (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

                            // LLCorner
                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
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
                                            new (vts.X - 1, vts.Bottom),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.X - 1, vts.Bottom),
                                            1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

                            // ULCorner
                            lc.AddLine (
                                        new (vts.X - 1, vts.Y - 1),
                                        1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Y - 1),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            if (_host.Style.ShowInitialLine)
                            {
                                // ULCorner
                                lc.AddLine (
                                            new (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

                            // LLCorner
                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Right:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException ();
                    }
                }
                else if (i > 0)
                {
                    switch (_host.Style.TabsSide)
                    {
                        case TabSide.Top:
                        case TabSide.Bottom:
                            if (_host.Style.ShowInitialLine || _host.Style.TabsSide == TabSide.Bottom)
                            {
                                // Upper left tee
                                lc.AddLine (
                                            new (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.X - 1, vts.Y - 1),
                                            0,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

                            // Lower left tee
                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        0,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            if (_host.Style.ShowInitialLine || _host.Style.TabsSide == TabSide.Right)
                            {
                                // Upper left tee
                                lc.AddLine (
                                            new (vts.X - 1, vts.Y - 1),
                                            0,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.X - 1, vts.Y - 1),
                                            1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

                            // Lower left tee
                            lc.AddLine (
                                        new (vts.Right, vts.Y - 1),
                                        0,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.Right, vts.Y - 1),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Right:
                            if (_host.Style.ShowInitialLine || _host.Style.TabsSide == TabSide.Right)
                            {
                                // Upper left tee
                                lc.AddLine (
                                            new (vts.Right, vts.Y - 1),
                                            0,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                lc.AddLine (
                                            new (vts.Right, vts.Y - 1),
                                            -1,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );
                            }

                            // Lower left tee
                            lc.AddLine (
                                        new (vts.X - 1, vts.Y - 1),
                                        0,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Y - 1),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        default:
                            throw new ArgumentOutOfRangeException ();
                    }
                }
            }
            else if (i < tabLocations.Length - 1)
            {
                if (_host.Style.ShowInitialLine)
                {
                    switch (_host.Style.TabsSide)
                    {
                        case TabSide.Top:
                        case TabSide.Bottom:
                            // Upper right tee
                            lc.AddLine (
                                        new (vts.Right, vts.Y - 1),
                                        1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.Right, vts.Y - 1),
                                        0,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            // Upper right tee
                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        0,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Right:
                            // Upper right tee
                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        0,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        default:
                            throw new ArgumentOutOfRangeException ();
                    }
                }

                if (_host.Style.ShowInitialLine)
                {
                    switch (_host.Style.TabsSide)
                    {
                        case TabSide.Top:
                        case TabSide.Bottom:
                            // Lower right tee
                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        0,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            // Lower right tee
                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        0,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Right:
                            // Lower right tee
                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        0,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        default:
                            throw new ArgumentOutOfRangeException ();
                    }
                }
                else
                {
                    switch (_host.Style.TabsSide)
                    {
                        case TabSide.Top:
                            // Lower right tee
                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        -1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        0,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Bottom:
                            // Upper right tee
                            lc.AddLine (
                                        new (vts.Right, vts.Y - 1),
                                        1,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.Right, vts.Y - 1),
                                        0,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            // Lower right tee
                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        0,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Right:
                            // Lower right tee
                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        0,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        default:
                            throw new ArgumentOutOfRangeException ();
                    }
                }
            }

            if (i == 0 && i != selectedTab && _host is { TabScrollOffset: 0, Style.ShowBorder: true })
            {
                switch (_host.Style.TabsSide)
                {
                    case TabSide.Top:
                        // Lower left vertical line
                        lc.AddLine (
                                    new (vts.X - 1, vts.Bottom),
                                    0,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new (vts.X - 1, vts.Bottom),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Bottom:
                        // Upper left vertical line
                        lc.AddLine (
                                    new (vts.X - 1, vts.Y - 1),
                                    0,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new (vts.X - 1, vts.Y - 1),
                                    1,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Left:
                        // Upper horizontal line
                        lc.AddLine (
                                    new (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new (vts.Right, vts.Y - 1),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Right:
                        // Upper horizontal line
                        lc.AddLine (
                                    new (vts.X - 1, vts.Y - 1),
                                    1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new (vts.X - 1, vts.Y - 1),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

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
                                    new (vts.Right, vts.Bottom),
                                    -1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new (vts.Right, vts.Bottom),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Bottom:
                        // Upper right tee
                        lc.AddLine (
                                    new (vts.Right, vts.Y - 1),
                                    1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );

                        lc.AddLine (
                                    new (vts.Right, vts.Y - 1),
                                    0,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );

                        break;
                    case TabSide.Left:
                        if (Frame.Bottom > tab.Frame.Bottom)
                        {
                            // Lower right tee
                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        0,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.Right, vts.Bottom),
                                        -1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }

                        break;
                    case TabSide.Right:
                        if (Frame.Bottom > tab.Frame.Bottom)
                        {
                            // Lower right tee
                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        0,
                                        Orientation.Vertical,
                                        tab.BorderStyle
                                       );

                            lc.AddLine (
                                        new (vts.X - 1, vts.Bottom),
                                        1,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException ();
                }
            }

            if (i == tabLocations.Length - 1)
            {
                var arrowOffset = 1;

                Rectangle tabsBarVts = ViewportToScreen (Viewport);
                int lineLength;

                switch (_host.Style.TabsSide)
                {
                    case TabSide.Top:
                    case TabSide.Bottom:
                        lineLength = tabsBarVts.Right - vts.Right;

                        break;
                    case TabSide.Left:
                    case TabSide.Right:
                        lineLength = tabsBarVts.Bottom - vts.Bottom;

                        break;
                    default:
                        throw new ArgumentOutOfRangeException ();
                }

                // Right horizontal/vertical line
                if (ShouldDrawRightDownScrollIndicator ())
                {
                    if (lineLength - arrowOffset > 0)
                    {
                        switch (_host.Style.TabsSide)
                        {
                            case TabSide.Top:
                                lc.AddLine (
                                            new (
                                                 vts.Right,
                                                 vts.Bottom
                                                ),
                                            lineLength - arrowOffset,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Bottom:
                                lc.AddLine (

                                            new (vts.Right, vts.Y - 1),
                                            lineLength - arrowOffset,
                                            Orientation.Horizontal,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Left:
                                lc.AddLine (
                                            new (
                                                 vts.Right,
                                                 vts.Bottom
                                                ),
                                            lineLength - arrowOffset,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Right:
                                lc.AddLine (
                                            new (
                                                 vts.X - 1,
                                                 vts.Bottom
                                                ),
                                            lineLength - arrowOffset,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

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
                                        new (vts.Right, vts.Bottom),
                                        lineLength,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Bottom:
                            lc.AddLine (

                                        new (vts.Right, vts.Y - 1),
                                        lineLength,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );

                            break;
                        case TabSide.Left:
                            if (i == selectedTab)
                            {
                                if (Frame.Bottom == tab.Frame.Bottom)
                                {
                                    // Lower right horizontal line
                                    lc.AddLine (
                                                new (vts.Right, vts.Bottom),
                                                0,
                                                Orientation.Horizontal,
                                                tab.BorderStyle
                                               );
                                }
                                else
                                {
                                    lc.AddLine (
                                                new (vts.Right, vts.Bottom),
                                                lineLength,
                                                Orientation.Vertical,
                                                tab.BorderStyle
                                               );
                                }
                            }
                            else
                            {
                                if (Frame.Bottom == tab.Frame.Bottom)
                                {
                                    lc.AddLine (
                                                new (vts.Right, vts.Bottom),
                                                -1,
                                                Orientation.Vertical,
                                                tab.BorderStyle
                                               );

                                    lc.AddLine (
                                                new (vts.Right, vts.Bottom),
                                                0,
                                                Orientation.Horizontal,
                                                tab.BorderStyle
                                               );

                                }
                                else
                                {
                                    lc.AddLine (
                                                new (vts.Right, vts.Bottom),
                                                lineLength,
                                                Orientation.Vertical,
                                                tab.BorderStyle
                                               );

                                    lc.AddLine (
                                                new (vts.Right, tabsBarVts.Bottom),
                                                1,
                                                Orientation.Horizontal,
                                                tab.BorderStyle
                                               );
                                }
                            }

                            break;
                        case TabSide.Right:
                            if (i == selectedTab)
                            {
                                if (Frame.Bottom == tab.Frame.Bottom)
                                {
                                    // Lower right horizontal line
                                    lc.AddLine (
                                                new (vts.X - 1, vts.Bottom),
                                                0,
                                                Orientation.Horizontal,
                                                tab.BorderStyle
                                               );
                                }
                                else
                                {
                                    lc.AddLine (
                                                new (vts.X - 1, vts.Bottom),
                                                lineLength,
                                                Orientation.Vertical,
                                                tab.BorderStyle
                                               );
                                }
                            }
                            else
                            {
                                if (Frame.Bottom == tab.Frame.Bottom)
                                {
                                    lc.AddLine (
                                                new (vts.X - 1, vts.Bottom),
                                                -1,
                                                Orientation.Vertical,
                                                tab.BorderStyle
                                               );

                                    lc.AddLine (
                                                new (vts.X - 1, vts.Bottom),
                                                0,
                                                Orientation.Horizontal,
                                                tab.BorderStyle
                                               );

                                }
                                else
                                {
                                    lc.AddLine (
                                                new (vts.X - 1, vts.Bottom),
                                                lineLength,
                                                Orientation.Vertical,
                                                tab.BorderStyle
                                               );

                                    lc.AddLine (
                                                new (vts.X - 1, tabsBarVts.Bottom),
                                                1,
                                                Orientation.Horizontal,
                                                tab.BorderStyle
                                               );
                                }
                            }

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
                                            new (
                                                 tabsBarVts.Right - 1,
                                                 vts.Bottom
                                                ),
                                            1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Bottom:
                                // More LRCorner
                                lc.AddLine (
                                            new (
                                                 tabsBarVts.Right - 1,
                                                 vts.Y - 1
                                                ),
                                            -1,
                                            Orientation.Vertical,
                                            tab.BorderStyle
                                           );

                                break;
                            case TabSide.Left:
                                if (Frame.Bottom > tab.Frame.Bottom)
                                {
                                    // More URCorner
                                    lc.AddLine (
                                                new (
                                                     vts.Right,
                                                     tabsBarVts.Bottom - 1
                                                    ),
                                                1,
                                                Orientation.Horizontal,
                                                tab.BorderStyle
                                               );
                                }

                                break;
                            case TabSide.Right:
                                if (Frame.Bottom > tab.Frame.Bottom)
                                {
                                    // More URCorner
                                    lc.AddLine (
                                                new (
                                                     vts.X - 1,
                                                     tabsBarVts.Bottom - 1
                                                    ),
                                                -1,
                                                Orientation.Horizontal,
                                                tab.BorderStyle
                                               );
                                }

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

    /// <summary>Renders the line of the tab that adjoins the content of the tab.</summary>
    private void RenderUnderline ()
    {
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
                    _leftUpScrollIndicator.X = 0;
                    _leftUpScrollIndicator.Y = Pos.AnchorEnd (1);

                    break;
                case TabSide.Bottom:
                case TabSide.Right:
                    _leftUpScrollIndicator.X = 0;
                    _leftUpScrollIndicator.Y = 0;

                    break;
                case TabSide.Left:
                    _leftUpScrollIndicator.X = Pos.AnchorEnd (1);
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
                case TabSide.Left:
                    _rightDownScrollIndicator.X = Pos.AnchorEnd (1);
                    _rightDownScrollIndicator.Y = Pos.AnchorEnd (1);

                    break;
                case TabSide.Bottom:
                    _rightDownScrollIndicator.X = Pos.AnchorEnd (1);
                    _rightDownScrollIndicator.Y = 0;

                    break;
                case TabSide.Right:
                    _rightDownScrollIndicator.X = 0;
                    _rightDownScrollIndicator.Y = Pos.AnchorEnd (1);

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

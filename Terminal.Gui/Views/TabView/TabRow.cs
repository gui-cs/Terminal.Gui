#nullable enable

namespace Terminal.Gui.Views;

internal class TabRow : View
{
    private readonly TabView _host;
    private readonly View _leftScrollIndicator;
    private readonly View _rightScrollIndicator;

    public TabRow (TabView host)
    {
        _host = host;
        Id = "tabRow";

        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        Width = Dim.Fill ();

        _rightScrollIndicator = new View
        {
            Id = "rightScrollIndicator",
            Width = 1,
            Height = 1,
            Visible = false,
            Text = Glyphs.RightArrow.ToString ()
        };
        _rightScrollIndicator.MouseClick += _host.Tab_MouseClick!;

        _leftScrollIndicator = new View
        {
            Id = "leftScrollIndicator",
            Width = 1,
            Height = 1,
            Visible = false,
            Text = Glyphs.LeftArrow.ToString ()
        };
        _leftScrollIndicator.MouseClick += _host.Tab_MouseClick!;

        Add (_rightScrollIndicator, _leftScrollIndicator);
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

            if (me.View is { Id: "rightScrollIndicator" } || me.Flags.HasFlag (MouseFlags.WheeledDown) || me.Flags.HasFlag (MouseFlags.WheeledRight))
            {
                scrollIndicatorHit = 1;
            }
            else if (me.View is { Id: "leftScrollIndicator" } || me.Flags.HasFlag (MouseFlags.WheeledUp) || me.Flags.HasFlag (MouseFlags.WheeledLeft))
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
        _host._tabLocations = _host.CalculateViewport (Viewport).ToArray ();

        RenderTabLine ();

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
            int selectedOffset = _host.Style.ShowTopLine && tabLocations [i] == _host.SelectedTab ? 0 : 1;

            if (tabLocations [i] == _host.SelectedTab)
            {
                selectedTab = i;

                if (i == 0 && _host.TabScrollOffset == 0)
                {
                    if (_host.Style.TabsOnBottom)
                    {
                        // Upper left vertical line
                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Y - 1),
                                    -1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );
                    }
                    else
                    {
                        // Lower left vertical line
                        lc.AddLine (
                                    new Point (vts.X - 1, vts.Bottom - selectedOffset),
                                    -1,
                                    Orientation.Vertical,
                                    tab.BorderStyle
                                   );
                    }
                }
                else if (i > 0 && i <= tabLocations.Length - 1)
                {
                    if (_host.Style.TabsOnBottom)
                    {
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
                    }
                    else
                    {
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
                    }

                    if (_host.Style.ShowTopLine)
                    {
                        if (_host.Style.TabsOnBottom)
                        {
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
                        else
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
                    }
                }

                if (i < tabLocations.Length - 1)
                {
                    if (_host.Style.ShowTopLine)
                    {
                        if (_host.Style.TabsOnBottom)
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
                }

                if (_host.Style.TabsOnBottom)
                {
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
                }
                else
                {
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
                }
            }
            else if (selectedTab == -1)
            {
                if (i == 0 && string.IsNullOrEmpty (tab.Text))
                {
                    if (_host.Style.TabsOnBottom)
                    {
                        if (_host.Style.ShowTopLine)
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
                    }
                    else
                    {
                        if (_host.Style.ShowTopLine)
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
                    }
                }
                else if (i > 0)
                {
                    if (_host.Style.ShowTopLine || _host.Style.TabsOnBottom)
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
                if (_host.Style.ShowTopLine)
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

                if (_host.Style.ShowTopLine || !_host.Style.TabsOnBottom)
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
                if (_host.Style.TabsOnBottom)
                {
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
                }
                else
                {
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
                }
            }

            if (i == tabLocations.Length - 1 && i != selectedTab)
            {
                if (_host.Style.TabsOnBottom)
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
                else
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
            }

            if (i == tabLocations.Length - 1)
            {
                var arrowOffset = 1;

                int lastSelectedTab = !_host.Style.ShowTopLine && i == selectedTab ? 1 :
                                      _host.Style.TabsOnBottom ? 1 : 0;
                Rectangle tabsBarVts = ViewportToScreen (Viewport);
                int lineLength = tabsBarVts.Right - vts.Right;

                // Right horizontal line
                if (ShouldDrawRightScrollIndicator ())
                {
                    if (lineLength - arrowOffset > 0)
                    {
                        if (_host.Style.TabsOnBottom)
                        {
                            lc.AddLine (
                                        new Point (vts.Right, vts.Y - lastSelectedTab),
                                        lineLength - arrowOffset,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }
                        else
                        {
                            lc.AddLine (
                                        new Point (
                                                   vts.Right,
                                                   vts.Bottom - lastSelectedTab
                                                  ),
                                        lineLength - arrowOffset,
                                        Orientation.Horizontal,
                                        tab.BorderStyle
                                       );
                        }
                    }
                }
                else
                {
                    // Right corner
                    if (_host.Style.TabsOnBottom)
                    {
                        lc.AddLine (
                                    new Point (vts.Right, vts.Y - lastSelectedTab),
                                    lineLength,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }
                    else
                    {
                        lc.AddLine (
                                    new Point (vts.Right, vts.Bottom - lastSelectedTab),
                                    lineLength,
                                    Orientation.Horizontal,
                                    tab.BorderStyle
                                   );
                    }

                    if (_host.Style.ShowBorder)
                    {
                        if (_host.Style.TabsOnBottom)
                        {
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
                        }
                        else
                        {
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
                        }
                    }
                }
            }
        }

        _host.LineCanvas.Merge (lc);
    }

    private int GetUnderlineYPosition ()
    {
        if (_host.Style.TabsOnBottom)
        {
            return 0;
        }

        return _host.Style.ShowTopLine ? 2 : 1;
    }

    /// <summary>Renders the line with the tab names in it.</summary>
    private void RenderTabLine ()
    {
        if (_host._tabLocations is null)
        {
            return;
        }

        View? selected = null;
        int topLine = _host.Style.ShowTopLine ? 1 : 0;

        foreach (Tab toRender in _host._tabLocations)
        {
            Tab tab = toRender;

            if (toRender == _host.SelectedTab)
            {
                selected = tab;

                if (_host.Style.TabsOnBottom)
                {
                    tab.Border!.Thickness = new (1, 0, 1, topLine);
                    tab.Margin!.Thickness = new (0, 1, 0, 0);
                }
                else
                {
                    tab.Border!.Thickness = new (1, topLine, 1, 0);
                    tab.Margin!.Thickness = new (0, 0, 0, topLine);
                }
            }
            else if (selected is null)
            {
                if (_host.Style.TabsOnBottom)
                {
                    tab.Border!.Thickness = new (1, 1, 1, topLine);
                    tab.Margin!.Thickness = new (0, 0, 0, 0);
                }
                else
                {
                    tab.Border!.Thickness = new (1, topLine, 1, 1);
                    tab.Margin!.Thickness = new (0, 0, 0, 0);
                }
            }
            else
            {
                if (_host.Style.TabsOnBottom)
                {
                    tab.Border!.Thickness = new (1, 1, 1, topLine);
                    tab.Margin!.Thickness = new (0, 0, 0, 0);
                }
                else
                {
                    tab.Border!.Thickness = new (1, topLine, 1, 1);
                    tab.Margin!.Thickness = new (0, 0, 0, 0);
                }
            }

            // Ensures updating TextFormatter constrains
            tab.TextFormatter.ConstrainToWidth = tab.GetContentSize ().Width;
            tab.TextFormatter.ConstrainToHeight = tab.GetContentSize ().Height;
        }
    }

    /// <summary>Renders the line of the tab that adjoins the content of the tab.</summary>
    private void RenderUnderline ()
    {
        int y = GetUnderlineYPosition ();

        Tab? selected = _host._tabLocations?.FirstOrDefault (t => t == _host.SelectedTab);

        if (selected is null)
        {
            return;
        }

        // draw scroll indicators

        // if there are more tabs to the left not visible
        if (_host.TabScrollOffset > 0)
        {
            _leftScrollIndicator.X = 0;
            _leftScrollIndicator.Y = y;

            // indicate that
            _leftScrollIndicator.Visible = true;

            // Ensures this is clicked instead of the first tab
            MoveSubViewToEnd (_leftScrollIndicator);
        }
        else
        {
            _leftScrollIndicator.Visible = false;
        }

        // if there are more tabs to the right not visible
        if (ShouldDrawRightScrollIndicator ())
        {
            _rightScrollIndicator.X = Viewport.Width - 1;
            _rightScrollIndicator.Y = y;

            // indicate that
            _rightScrollIndicator.Visible = true;

            // Ensures this is clicked instead of the last tab if under this
            MoveSubViewToStart (_rightScrollIndicator);
        }
        else
        {
            _rightScrollIndicator.Visible = false;
        }
    }

    private bool ShouldDrawRightScrollIndicator () { return _host._tabLocations!.LastOrDefault () != _host.Tabs.LastOrDefault (); }
}

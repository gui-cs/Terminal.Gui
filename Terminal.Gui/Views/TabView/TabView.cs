#nullable enable

namespace Terminal.Gui.Views;

/// <summary>Control that hosts multiple sub views, presenting a single one at once.</summary>
public class TabView : View
{
    /// <summary>The default <see cref="MaxTabTextWidth"/> to set on new <see cref="TabView"/> controls.</summary>
    public const uint DefaultMaxTabTextWidth = 30;

    /// <summary>
    ///     This sub view is the main client area of the current tab.  It hosts the <see cref="Tab.View"/> of the tab, the
    ///     <see cref="SelectedTab"/>.
    /// </summary>
    private readonly View _containerView;

    private readonly List<Tab> _tabs = new ();

    /// <summary>This sub view is the 2 or 3 line control that represents the actual tabs themselves.</summary>
    private readonly TabRow _tabsBar;

    private Tab? _selectedTab;

    internal Tab []? _tabLocations;
    private int _tabScrollOffset;

    /// <summary>Initializes a <see cref="TabView"/> class.</summary>
    public TabView ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabStop;
        _tabsBar = new (this);
        _containerView = new ();
        ApplyStyleChanges ();

        base.Add (_tabsBar);
        base.Add (_containerView);

        // Things this view knows how to do
        AddCommand (
                    Command.Left,
                    () =>
                    {
                        if (Style.TabsSide is TabSide.Top or TabSide.Bottom)
                        {
                            return SwitchTabBy (-1);
                        }

                        return false;
                    });

        AddCommand (
                    Command.Right,
                    () =>
                    {
                        if (Style.TabsSide is TabSide.Top or TabSide.Bottom)
                        {
                            return SwitchTabBy (1);
                        }

                        return false;
                    });

        AddCommand (
                    Command.Up,
                    () =>
                    {
                        if (Style.TabsSide is TabSide.Left or TabSide.Right)
                        {
                            return SwitchTabBy (-1);
                        }

                        return false;
                    });

        AddCommand (
                    Command.Down,
                    () =>
                    {
                        if (Style.TabsSide is TabSide.Left or TabSide.Right)
                        {
                            return SwitchTabBy (1);
                        }

                        return false;
                    });

        AddCommand (
                    Command.LeftStart,
                    () =>
                    {
                        TabScrollOffset = 0;
                        SelectedTab = Tabs.FirstOrDefault ()!;

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightEnd,
                    () =>
                    {
                        TabScrollOffset = Tabs.Count - 1;
                        SelectedTab = Tabs.LastOrDefault ()!;

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageDown,
                    () =>
                    {
                        TabScrollOffset += _tabLocations!.Length;
                        SelectedTab = Tabs.ElementAt (TabScrollOffset);

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageUp,
                    () =>
                    {
                        TabScrollOffset -= _tabLocations!.Length;
                        SelectedTab = Tabs.ElementAt (TabScrollOffset);

                        return true;
                    }
                   );

        AddCommand (
                    Command.Up,
                    () =>
                    {
                        if (_style.TabsOnBottom)
                        {
                            if (_tabsBar is { HasFocus: true } && _containerView.CanFocus)
                            {
                                _containerView.SetFocus ();

                                return true;
                            }
                        }
                        else
                        {
                            if (_containerView is { HasFocus: true })
                            {
                                var mostFocused = _containerView.MostFocused;

                                if (mostFocused is { })
                                {
                                    for (int? i = mostFocused.SuperView?.SubViews.IndexOf (mostFocused) - 1; i > -1; i--)
                                    {
                                        var view = mostFocused.SuperView?.SubViews.ElementAt ((int)i);

                                        if (view is { CanFocus: true, Enabled: true, Visible: true })
                                        {
                                            // Let toplevel handle it
                                            return false;
                                        }
                                    }
                                }

                                SelectedTab?.SetFocus ();

                                return true;
                            }
                        }

                        return false;
                    }
                   );

        AddCommand (
                    Command.Down,
                    () =>
                    {
                        if (_style.TabsOnBottom)
                        {
                            if (_containerView is { HasFocus: true })
                            {
                                var mostFocused = _containerView.MostFocused;

                                if (mostFocused is { })
                                {
                                    for (int? i = mostFocused.SuperView?.SubViews.IndexOf (mostFocused) + 1; i < mostFocused.SuperView?.SubViews.Count; i++)
                                    {
                                        var view = mostFocused.SuperView?.SubViews.ElementAt ((int)i);

                                        if (view is { CanFocus: true, Enabled: true, Visible: true })
                                        {
                                            // Let toplevel handle it
                                            return false;
                                        }
                                    }
                                }

                                SelectedTab?.SetFocus ();

                                return true;
                            }
                        }
                        else
                        {
                            if (_tabsBar is { HasFocus: true } && _containerView.CanFocus)
                            {
                                _containerView.SetFocus ();

                                return true;
                            }
                        }

                        return false;
                    }
                   );

        // Default keybindings for this view
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.Home, Command.LeftStart);
        KeyBindings.Add (Key.End, Command.RightEnd);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
    }

    /// <summary>
    ///     The maximum number of characters to render in a Tab header.  This prevents one long tab from pushing out all
    ///     the others.
    /// </summary>
    public uint MaxTabTextWidth { get; set; } = DefaultMaxTabTextWidth;

    // This is needed to hold initial value because it may change during the setter process
    private bool _selectedTabHasFocus;

    /// <summary>The currently selected member of <see cref="Tabs"/> chosen by the user.</summary>
    /// <value></value>
    public Tab? SelectedTab
    {
        get => _selectedTab;
        set
        {
            if (value == _selectedTab)
            {
                return;
            }

            Tab? old = _selectedTab;
            _selectedTabHasFocus = old is { } && (old.HasFocus || !_containerView.CanFocus);

            if (_selectedTab is { })
            {
                if (_selectedTab.View is { })
                {
                    _selectedTab.View.CanFocusChanged -= ContainerViewCanFocus!;

                    // remove old content
                    _containerView.Remove (_selectedTab.View);
                }
            }

            _selectedTab = value;

            // add new content
            if (_selectedTab?.View != null)
            {
                _selectedTab.View.CanFocusChanged += ContainerViewCanFocus!;
                _containerView.Add (_selectedTab.View);
            }

            ContainerViewCanFocus (null!, null!);

            EnsureSelectedTabIsVisible ();

            if (old != _selectedTab)
            {
                if (TabCanSetFocus ())
                {
                    SelectedTab?.SetFocus ();
                }

                OnSelectedTabChanged (old!, _selectedTab!);
            }

            SetNeedsLayout ();
        }
    }

    private bool TabCanSetFocus ()
    {
        return IsInitialized && SelectedTab is { } && (HasFocus || (bool)_containerView?.HasFocus) && (_selectedTabHasFocus || !_containerView.CanFocus);
    }

    private void ContainerViewCanFocus (object sender, EventArgs eventArgs)
    {
        _containerView.CanFocus = _containerView.SubViews.Count (v => v.CanFocus) > 0;
    }

    private TabStyle _style = new ();

    /// <summary>Render choices for how to display tabs.  After making changes, call <see cref="ApplyStyleChanges()"/>.</summary>
    /// <value></value>
    public TabStyle Style
    {
        get => _style;
        set
        {
            if (_style == value)
            {
                return;
            }

            _style = value;
            SetNeedsLayout ();
        }
    }

    /// <summary>All tabs currently hosted by the control.</summary>
    /// <value></value>
    public IReadOnlyCollection<Tab> Tabs => _tabs.AsReadOnly ();

    /// <summary>When there are too many tabs to render, this indicates the first tab to render on the screen.</summary>
    /// <value></value>
    public int TabScrollOffset
    {
        get => _tabScrollOffset;
        set
        {
            _tabScrollOffset = EnsureValidScrollOffsets (value);
            SetNeedsLayout ();
        }
    }

    /// <summary>Adds the given <paramref name="tab"/> to <see cref="Tabs"/>.</summary>
    /// <param name="tab"></param>
    /// <param name="andSelect">True to make the newly added Tab the <see cref="SelectedTab"/>.</param>
    public void AddTab (Tab tab, bool andSelect)
    {
        if (_tabs.Contains (tab))
        {
            return;
        }

        _tabs.Add (tab);
        _tabsBar.Add (tab);

        if (SelectedTab is null || andSelect)
        {
            SelectedTab = tab;

            EnsureSelectedTabIsVisible ();

            tab.View?.SetFocus ();
        }

        SetNeedsLayout ();
    }

    /// <summary>
    ///     Updates the control to use the latest state settings in <see cref="Style"/>. This can change the size of the
    ///     client area of the tab (for rendering the selected tab's content).  This method includes a call to
    ///     <see cref="View.SetNeedsDraw()"/>.
    /// </summary>
    public void ApplyStyleChanges ()
    {
        _tabLocations = CalculateViewport (Viewport);

        _containerView.BorderStyle = Style.ShowBorder ? LineStyle.Single : LineStyle.None;

        switch (Style.TabsSide)
        {
            case TabSide.Top:
                // Tabs are along the top
                if (Style.ShowBorder)
                {
                    _containerView.Border!.Thickness = new (1, 0, 1, 1);
                }

                _tabsBar.X = 0;
                _tabsBar.Y = 0;
                _tabsBar.Width = Dim.Fill ();
                _tabsBar.Height = GetTabHeight (true);

                _containerView.X = 0;

                //move content down to make space for tabs
                _containerView.Y = Pos.Bottom (_tabsBar);
                _containerView.Width = Dim.Fill ();
                _containerView.Height = Dim.Fill ();

                break;
            case TabSide.Bottom:
                // Tabs are along the bottom so just dodge the border
                if (Style.ShowBorder)
                {
                    _containerView.Border!.Thickness = new (1, 1, 1, 0);
                }

                _tabsBar.X = 0;
                _tabsBar.Width = Dim.Fill ();
                int tabHeight = GetTabHeight (false);
                _tabsBar.Height = tabHeight;

                _containerView.X = 0;
                _containerView.Y = 0;
                _containerView.Width = Dim.Fill ();

                // Fill client area leaving space at bottom for tabs
                _containerView.Height = Dim.Fill (tabHeight);

                _tabsBar.Y = Pos.Bottom (_containerView);

                break;
            case TabSide.Left:
                // Tabs are along the left
                if (Style.ShowBorder)
                {
                    _containerView.Border!.Thickness = new (0, 1, 1, 1);
                }

                _tabsBar.X = 0;
                _tabsBar.Y = 0;
                _tabsBar.Height = Dim.Fill ();

                //move content right to make space for tabs
                _containerView.X = Pos.Right (_tabsBar);
                _containerView.Y = 0;

                // Fill client area leaving space at left for tabs
                _containerView.Width = Dim.Fill ();
                _containerView.Height = Dim.Fill ();

                break;
            case TabSide.Right:
                // Tabs are along the right
                if (Style.ShowBorder)
                {
                    _containerView.Border!.Thickness = new (1, 1, 0, 1);
                }

                _tabsBar.Y = 0;
                _tabsBar.Height = Dim.Fill ();

                //move content left to make space for tabs
                _containerView.X = 0;
                _containerView.Y = 0;

                _containerView.Height = Dim.Fill ();

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        SetNeedsLayout ();
    }

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        _tabLocations = CalculateViewport (Viewport);

        base.OnViewportChanged (e);
    }

    /// <summary>Updates <see cref="TabScrollOffset"/> to ensure that <see cref="SelectedTab"/> is visible.</summary>
    public void EnsureSelectedTabIsVisible ()
    {
        if (!IsInitialized || SelectedTab is null)
        {
            return;
        }

        // if current viewport does not include the selected tab
        if (_tabLocations is null || (_tabLocations is { } && !_tabLocations.Any (t => Equals (SelectedTab, t))))
        {
            // Set scroll offset so the first tab rendered is the
            TabScrollOffset = Math.Max (0, Tabs.IndexOf (SelectedTab));
            _tabLocations = CalculateViewport (Viewport);
        }
        else
        {
            RenderTabLine (_tabLocations);
        }
    }

    /// <summary>Updates <see cref="TabScrollOffset"/> to be a valid index of <see cref="Tabs"/>.</summary>
    /// <param name="value">The value to validate.</param>
    /// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDraw()"/>.</remarks>
    /// <returns>The valid <see cref="TabScrollOffset"/> for the given value.</returns>
    public int EnsureValidScrollOffsets (int value) { return Math.Max (Math.Min (value, Tabs.Count - 1), 0); }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedView)
    {
        if (SelectedTab is { HasFocus: false } && !_containerView.CanFocus && focusedView == this)
        {
            SelectedTab?.SetFocus ();

            return;
        }

        base.OnHasFocusChanged (newHasFocus, previousFocusedView, focusedView);
    }

    /// <summary>
    ///     Removes the given <paramref name="tab"/> from <see cref="Tabs"/>. Caller is responsible for disposing the
    ///     tab's hosted <see cref="Tab.View"/> if appropriate.
    /// </summary>
    /// <param name="tab"></param>
    public void RemoveTab (Tab? tab)
    {
        if (tab is null || !_tabs.Contains (tab))
        {
            return;
        }

        // what tab was selected before closing
        int idx = _tabs.IndexOf (tab);

        _tabs.Remove (tab);

        // if the currently selected tab is no longer a member of Tabs
        if (SelectedTab is null || !Tabs.Contains (SelectedTab))
        {
            // select the tab closest to the one that disappeared
            int toSelect = Math.Max (idx - 1, 0);

            if (toSelect < Tabs.Count)
            {
                SelectedTab = Tabs.ElementAt (toSelect);
            }
            else
            {
                SelectedTab = Tabs.LastOrDefault ();
            }
        }

        EnsureSelectedTabIsVisible ();
        SetNeedsLayout ();
    }

    /// <summary>Event for when <see cref="SelectedTab"/> changes.</summary>
    public event EventHandler<TabChangedEventArgs>? SelectedTabChanged;

    /// <summary>
    ///     Changes the <see cref="SelectedTab"/> by the given <paramref name="amount"/>. Positive for right, negative for
    ///     left.  If no tab is currently selected then the first tab will become selected.
    /// </summary>
    /// <param name="amount"></param>
    public bool SwitchTabBy (int amount)
    {
        if (Tabs.Count == 0)
        {
            return false;
        }

        // if there is only one tab anyway or nothing is selected
        if (Tabs.Count == 1 || SelectedTab is null)
        {
            SelectedTab = Tabs.ElementAt (0);

            return SelectedTab is { };
        }

        int currentIdx = Tabs.IndexOf (SelectedTab);

        // Currently selected tab has vanished!
        if (currentIdx == -1)
        {
            SelectedTab = Tabs.ElementAt (0);

            return true;
        }

        int newIdx = Math.Max (0, Math.Min (currentIdx + amount, Tabs.Count - 1));

        if (newIdx == currentIdx)
        {
            return false;
        }

        SelectedTab = _tabs [newIdx];

        EnsureSelectedTabIsVisible ();

        return true;
    }

    /// <summary>
    ///     Event fired when a <see cref="Tab"/> is clicked.  Can be used to cancel navigation, show context menu (e.g. on
    ///     right click) etc.
    /// </summary>
    public event EventHandler<TabMouseEventArgs>? TabClicked;

    /// <summary>Disposes the control and all <see cref="Tabs"/>.</summary>
    /// <param name="disposing"></param>
    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        // The selected tab will automatically be disposed but
        // any tabs not visible will need to be manually disposed

        foreach (Tab tab in Tabs)
        {
            if (!Equals (SelectedTab, tab))
            {
                tab.View?.Dispose ();
            }
        }
    }

    /// <summary>Raises the <see cref="SelectedTabChanged"/> event.</summary>
    protected virtual void OnSelectedTabChanged (Tab oldTab, Tab newTab) { SelectedTabChanged?.Invoke (this, new (oldTab, newTab)); }

    /// <summary>Returns which tabs to render at each x location.</summary>
    /// <returns></returns>
    internal Tab []? CalculateViewport (Rectangle bounds)
    {
        UnSetCurrentTabs ();

        List<Tab> tabs = [];
        var i = 1;
        View? prevTab = null;

        switch (Style.TabsSide)
        {
            case TabSide.Top:
            case TabSide.Bottom:
                // Starting at the first or scrolled to tab
                foreach (Tab tab in Tabs.Skip (TabScrollOffset))
                {
                    if (prevTab is { })
                    {
                        tab.X = Pos.Right (prevTab) - 1;
                    }
                    else
                    {
                        tab.X = 0;
                    }

                    tab.Y = 0;

                    // while there is space for the tab
                    int tabTextWidth = tab.DisplayText.EnumerateRunes ().Sum (c => c.GetColumns ());

                    // The maximum number of characters to use for the tab name as specified
                    // by the user (MaxTabTextWidth).  But not more than the width of the view
                    // or we won't even be able to render a single tab!
                    long maxWidth = Math.Max (0, Math.Min (bounds.Width - 3, MaxTabTextWidth));

                    tab.Width = 2;
                    tab.Height = Style.ShowInitialLine ? 3 : 2;

                    // if tab view is width <= 3 don't render any tabs
                    if (maxWidth == 0)
                    {
                        tab.Visible = true;
                        tab.MouseClick += Tab_MouseClick!;
                        tab.Border!.MouseClick += Tab_MouseClick!;
                        tab.DisplayTextChanged += Tab_DisplayTextChanged;

                        tabs.Add (tab);

                        break;
                    }

                    if (tabTextWidth > maxWidth)
                    {
                        tab.Text = tab.DisplayText.Substring (0, (int)maxWidth);
                        tabTextWidth = (int)maxWidth;
                    }
                    else
                    {
                        tab.Text = tab.DisplayText;
                    }

                    tab.Width = tabTextWidth + 2;
                    tab.Height = Style.ShowInitialLine ? 3 : 2;

                    // if there is not enough space for this tab
                    if (i + tabTextWidth >= bounds.Width)
                    {
                        tab.Visible = false;

                        break;
                    }

                    // there is enough space!
                    tab.Visible = true;
                    tab.MouseClick += Tab_MouseClick!;
                    tab.Border!.MouseClick += Tab_MouseClick!;
                    tab.DisplayTextChanged += Tab_DisplayTextChanged;

                    tabs.Add (tab);

                    prevTab = tab;

                    i += tabTextWidth + 1;
                }

                break;
            case TabSide.Left:
            case TabSide.Right:
                var maxColWidth = 0;

                // Starting at the first or scrolled to tab
                foreach (Tab tab in Tabs.Skip (TabScrollOffset))
                {
                    tab.X = 0;

                    if (prevTab is { })
                    {
                        tab.Y = Pos.Bottom (prevTab) - 1;
                    }
                    else
                    {
                        tab.Y = 0;
                    }

                    // while there is space for the tab
                    int tabTextWidth = tab.DisplayText.EnumerateRunes ().Sum (c => c.GetColumns ());

                    // The maximum number of characters to use for the tab name as specified
                    // by the user (MaxTabTextWidth).  But not more than the width of the view
                    // or we won't even be able to render a single tab!
                    long maxWidth = Math.Max (0, Math.Min (bounds.Width - (Style.ShowInitialLine ? 2 : 1), MaxTabTextWidth));

                    maxColWidth = GetMaxColWidth (Math.Min (tabTextWidth, (int)maxWidth));

                    // The maximum height to use for the tab. But not more than the height of the view
                    // or we won't even be able to render a single tab!
                    int maxHeight = Math.Max (0, Math.Min (bounds.Height - 2, 2));

                    tab.Height = 2;
                    tab.TextAlignment = Style.TabsTextAlignment;

                    // if tab view is height <= 3 don't render any tabs
                    if (maxHeight == 0)
                    {
                        tab.Width = maxColWidth;
                        tab.Visible = true;
                        tab.MouseClick += Tab_MouseClick!;
                        tab.Border!.MouseClick += Tab_MouseClick!;
                        tab.DisplayTextChanged += Tab_DisplayTextChanged;

                        tabs.Add (tab);

                        break;
                    }

                    if (tabTextWidth > maxWidth)
                    {
                        tab.Text = tab.DisplayText.Substring (0, (int)maxWidth);
                        tabTextWidth = (int)maxWidth;
                    }
                    else
                    {
                        tab.Text = tab.DisplayText;
                    }

                    maxColWidth = GetMaxColWidth (tabTextWidth);
                    tab.Height = 3;

                    // if there is not enough space for this tab
                    if (i + 1 >= bounds.Height)
                    {
                        tab.Visible = false;

                        break;
                    }

                    // there is enough space!
                    tab.Visible = true;
                    tab.MouseClick += Tab_MouseClick!;
                    tab.Border!.MouseClick += Tab_MouseClick!;
                    tab.DisplayTextChanged += Tab_DisplayTextChanged;

                    tabs.Add (tab);

                    prevTab = tab;

                    i += 2;
                }

                foreach (Tab t in tabs)
                {
                    t.Width = maxColWidth;
                }

                _tabsBar.Width = maxColWidth;

                if (Style.TabsSide == TabSide.Right)
                {
                    _tabsBar.X = Pos.AnchorEnd (maxColWidth);
                    // Fill client area leaving space at right for tabs
                    _containerView.Width = Dim.Fill (maxColWidth);
                }

                int GetMaxColWidth (int textWidth)
                {
                    int maxViewportWidth = Math.Max (0, Viewport.Width - (Style.ShowBorder ? 2 : 0));

                    if (Math.Max (textWidth + (Style.ShowInitialLine ? 2 : 1), maxColWidth) > maxViewportWidth)
                    {
                        return maxViewportWidth;
                    }

                    return Math.Max (textWidth + (Style.ShowInitialLine ? 2 : 1), maxColWidth);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        if (TabCanSetFocus ())
        {
            SelectedTab?.SetFocus ();
        }
        else if (HasFocus)
        {
            SelectedTab?.View?.SetFocus ();
        }

        RenderTabLine (tabs.Count == 0 ? null : tabs.ToArray ());

        SetNeedsLayout ();

        return tabs.Count == 0 ? null : tabs.ToArray ();
    }

    private void Tab_DisplayTextChanged (object? sender, EventArgs e) { _tabLocations = CalculateViewport (Viewport); }

    /// <summary>Renders the line with the tab names in it.</summary>
    private void RenderTabLine (Tab []? tabLocations)
    {
        if (tabLocations is null)
        {
            return;
        }

        int topLine = Style.ShowInitialLine ? 1 : 0;

        foreach (Tab toRender in tabLocations)
        {
            Tab tab = toRender;

            if (toRender == SelectedTab)
            {
                switch (Style.TabsSide)
                {
                    case TabSide.Top:
                        tab.Border!.Thickness = new (1, topLine, 1, 0);
                        tab.Margin!.Thickness = new (0, 0, 0, 1);

                        break;
                    case TabSide.Bottom:
                        tab.Border!.Thickness = new (1, 0, 1, topLine);
                        tab.Margin!.Thickness = new (0, 1, 0, 0);

                        break;
                    case TabSide.Left:
                        tab.Border!.Thickness = new (topLine, 1, 0, 1);
                        tab.Margin!.Thickness = new (0, 0, 1, 0);

                        break;
                    case TabSide.Right:
                        tab.Border!.Thickness = new (0, 1, topLine, 1);
                        tab.Margin!.Thickness = new (1, 0, 0, 0);

                        break;
                    default:
                        throw new ArgumentOutOfRangeException ();
                }
            }
            else
            {
                tab.Margin!.Thickness = new (0, 0, 0, 0);

                switch (Style.TabsSide)
                {
                    case TabSide.Top:
                        tab.Border!.Thickness = new (1, topLine, 1, 1);

                        break;
                    case TabSide.Bottom:
                        tab.Border!.Thickness = new (1, 1, 1, topLine);

                        break;
                    case TabSide.Left:
                        tab.Border!.Thickness = new (topLine, 1, 1, 1);

                        break;
                    case TabSide.Right:
                        tab.Border!.Thickness = new (1, 1, topLine, 1);

                        break;
                    default:
                        throw new ArgumentOutOfRangeException ();
                }
            }

            // Ensures updating TextFormatter constrains
            tab.TextFormatter.ConstrainToWidth = tab.GetContentSize ().Width;
            tab.TextFormatter.ConstrainToHeight = tab.GetContentSize ().Height;
        }
    }

    /// <summary>
    ///     Returns the number of rows occupied by rendering the tabs, this depends on <see cref="TabStyle.ShowInitialLine"/>
    ///     and can be 0 (e.g. if <see cref="TabStyle.TabsSide"/> and you ask for <paramref name="top"/>).
    /// </summary>
    /// <param name="top">True to measure the space required at the top of the control, false to measure space at the bottom.</param>
    /// .
    /// <returns></returns>
    private int GetTabHeight (bool top)
    {
        if (top && Style.TabsSide == TabSide.Bottom)
        {
            return 0;
        }

        if (!top && Style.TabsSide == TabSide.Top)
        {
            return 0;
        }

        return Style.ShowInitialLine ? 3 : 2;
    }

    internal void Tab_MouseClick (object sender, MouseEventArgs e) { e.Handled = _tabsBar.NewMouseEvent (e) == true; }

    private void UnSetCurrentTabs ()
    {
        if (_tabLocations is null)
        {
            // Ensures unset any visible tab prior to TabScrollOffset
            for (var i = 0; i < TabScrollOffset; i++)
            {
                Tab tab = Tabs.ElementAt (i);

                if (tab.Visible)
                {
                    tab.MouseClick -= Tab_MouseClick!;
                    tab.Border!.MouseClick -= Tab_MouseClick!;
                    tab.DisplayTextChanged -= Tab_DisplayTextChanged;
                    tab.Visible = false;
                }
            }
        }
        else if (_tabLocations is { })
        {
            foreach (Tab tabToRender in _tabLocations)
            {
                tabToRender.MouseClick -= Tab_MouseClick!;
                tabToRender.Border!.MouseClick -= Tab_MouseClick!;
                tabToRender.DisplayTextChanged -= Tab_DisplayTextChanged;
                tabToRender.Visible = false;
            }

            _tabLocations = null;
        }
    }

    /// <summary>Raises the <see cref="TabClicked"/> event.</summary>
    /// <param name="tabMouseEventArgs"></param>
    internal virtual void OnTabClicked (TabMouseEventArgs tabMouseEventArgs) { TabClicked?.Invoke (this, tabMouseEventArgs); }
}

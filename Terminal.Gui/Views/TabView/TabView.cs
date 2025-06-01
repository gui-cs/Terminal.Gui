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
        TabStop = TabBehavior.TabStop; // Because TabView has focusable subviews, it must be a TabGroup
        _tabsBar = new TabRow (this);
        _containerView = new ();
        ApplyStyleChanges ();

        base.Add (_tabsBar);
        base.Add (_containerView);

        // Things this view knows how to do
        AddCommand (Command.Left, () => SwitchTabBy (-1));

        AddCommand (Command.Right, () => SwitchTabBy (1));

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
        _containerView.BorderStyle = Style.ShowBorder ? LineStyle.Single : LineStyle.None;
        _containerView.Width = Dim.Fill ();

        if (Style.TabsOnBottom)
        {
            // Tabs are along the bottom so just dodge the border
            if (Style.ShowBorder)
            {
                _containerView.Border!.Thickness = new Thickness (1, 1, 1, 0);
            }

            _containerView.Y = 0;

            int tabHeight = GetTabHeight (false);

            // Fill client area leaving space at bottom for tabs
            _containerView.Height = Dim.Fill (tabHeight);

            _tabsBar.Height = tabHeight;

            _tabsBar.Y = Pos.Bottom (_containerView);
        }
        else
        {
            // Tabs are along the top
            if (Style.ShowBorder)
            {
                _containerView.Border!.Thickness = new Thickness (1, 0, 1, 1);
            }

            _tabsBar.Y = 0;

            int tabHeight = GetTabHeight (true);

            //move content down to make space for tabs
            _containerView.Y = Pos.Bottom (_tabsBar);

            // Fill client area leaving space at bottom for border
            _containerView.Height = Dim.Fill ();

            // The top tab should be 2 or 3 rows high and on the top

            _tabsBar.Height = tabHeight;

            // Should be able to just use 0 but switching between top/bottom tabs repeatedly breaks in ValidatePosDim if just using the absolute value 0
        }

        SetNeedsLayout ();
    }

    /// <inheritdoc />
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        _tabLocations = CalculateViewport (Viewport).ToArray ();

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
        if (!CalculateViewport (Viewport).Any (t => Equals (SelectedTab, t)))
        {
            // Set scroll offset so the first tab rendered is the
            TabScrollOffset = Math.Max (0, Tabs.IndexOf (SelectedTab));
        }
    }

    /// <summary>Updates <see cref="TabScrollOffset"/> to be a valid index of <see cref="Tabs"/>.</summary>
    /// <param name="value">The value to validate.</param>
    /// <remarks>Changes will not be immediately visible in the display until you call <see cref="View.SetNeedsDraw()"/>.</remarks>
    /// <returns>The valid <see cref="TabScrollOffset"/> for the given value.</returns>
    public int EnsureValidScrollOffsets (int value) { return Math.Max (Math.Min (value, Tabs.Count - 1), 0); }

    /// <inheritdoc />
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
    protected virtual void OnSelectedTabChanged (Tab oldTab, Tab newTab)
    {
        SelectedTabChanged?.Invoke (this, new TabChangedEventArgs (oldTab, newTab));
    }

    /// <summary>Returns which tabs to render at each x location.</summary>
    /// <returns></returns>
    internal IEnumerable<Tab> CalculateViewport (Rectangle bounds)
    {
        UnSetCurrentTabs ();

        var i = 1;
        View? prevTab = null;

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
            tab.Height = Style.ShowTopLine ? 3 : 2;

            // if tab view is width <= 3 don't render any tabs
            if (maxWidth == 0)
            {
                tab.Visible = true;
                tab.MouseClick += Tab_MouseClick!;
                tab.Border!.MouseClick += Tab_MouseClick!;

                yield return tab;

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

            tab.Width = Math.Max (tabTextWidth + 2, 1);
            tab.Height = Style.ShowTopLine ? 3 : 2;

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

            yield return tab;

            prevTab = tab;

            i += tabTextWidth + 1;
        }

        if (TabCanSetFocus ())
        {
            SelectedTab?.SetFocus ();
        }
        else if (HasFocus)
        {
            SelectedTab?.View?.SetFocus ();
        }
    }

    /// <summary>
    ///     Returns the number of rows occupied by rendering the tabs, this depends on <see cref="TabStyle.ShowTopLine"/>
    ///     and can be 0 (e.g. if <see cref="TabStyle.TabsOnBottom"/> and you ask for <paramref name="top"/>).
    /// </summary>
    /// <param name="top">True to measure the space required at the top of the control, false to measure space at the bottom.</param>
    /// .
    /// <returns></returns>
    private int GetTabHeight (bool top)
    {
        if (top && Style.TabsOnBottom)
        {
            return 0;
        }

        if (!top && !Style.TabsOnBottom)
        {
            return 0;
        }

        return Style.ShowTopLine ? 3 : 2;
    }

    internal void Tab_MouseClick (object sender, MouseEventArgs e)
    {
        e.Handled = _tabsBar.NewMouseEvent (e) == true;
    }

    private void UnSetCurrentTabs ()
    {
        if (_tabLocations is null)
        {
            // Ensures unset any visible tab prior to TabScrollOffset
            for (int i = 0; i < TabScrollOffset; i++)
            {
                Tab tab = Tabs.ElementAt (i);

                if (tab.Visible)
                {
                    tab.MouseClick -= Tab_MouseClick!;
                    tab.Border!.MouseClick -= Tab_MouseClick!;
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
                tabToRender.Visible = false;
            }

            _tabLocations = null;
        }
    }

    /// <summary>Raises the <see cref="TabClicked"/> event.</summary>
    /// <param name="tabMouseEventArgs"></param>
    internal virtual void OnTabClicked (TabMouseEventArgs tabMouseEventArgs) { TabClicked?.Invoke (this, tabMouseEventArgs); }


}
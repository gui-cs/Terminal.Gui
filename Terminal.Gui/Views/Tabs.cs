using System.Collections.ObjectModel;

namespace Terminal.Gui.Views;

/// <summary>
///     A tabbed container <see cref="View"/> that renders each SubView as a selectable tab with a header drawn by
///     <see cref="Border"/>. The currently focused SubView is the selected (front-most) tab.
/// </summary>
/// <remarks>
///     <para>
///         Add any <see cref="View"/> instances via <see cref="View.Add(View)"/>. Each added view is automatically
///         configured with <see cref="BorderSettings.Tab"/>, <see cref="ViewArrangement.Overlapped"/> arrangement,
///         and a <see cref="IAdornment.Thickness"/> derived from <see cref="TabDepth"/> and <see cref="TabSide"/>.
///     </para>
///     <para>
///         The selected tab is determined by focus — whichever SubView has focus is drawn on top
///         and reported as the <see cref="Value"/>. Set <see cref="Value"/> programmatically to switch tabs.
///     </para>
///     <para>
///         Logical tab order is maintained separately from the draw order (<see cref="View.SubViews"/>).
///         Use <see cref="TabCollection"/> to enumerate tabs in logical order, and <see cref="InsertTab"/>
///         to add a tab at a specific position.
///     </para>
///     <para>
///         When tabs overflow the available space, scroll indicator buttons appear on the separator line
///         of each tab's border. Use <see cref="ScrollOffset"/> to scroll programmatically.
///     </para>
///     <para>
///         In earlier versions of Terminal.Gui, `TabView` provided similar functionality.
///     </para>
/// </remarks>
public class Tabs : View, IValue<View?>, IDesignable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Tabs"/> class.
    /// </summary>
    public Tabs ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;

        Width = Dim.Fill ();
        Height = Dim.Fill ();

        AddCommand (Command.Up, NavCommandHandler);
        AddCommand (Command.Down, NavCommandHandler);
        AddCommand (Command.Left, NavCommandHandler);
        AddCommand (Command.Right, NavCommandHandler);
    }

    private readonly List<WeakReference<View>> _tabList = [];

    /// <summary>
    ///     Resolves the tabs from the internal weak reference list, preserving logical order.
    /// </summary>
    private IEnumerable<View> ResolveTabCollection ()
    {
        foreach (WeakReference<View> wr in _tabList)
        {
            if (wr.TryGetTarget (out View? view))
            {
                yield return view;
            }
        }
    }

    /// <summary>
    ///     Gets the tabs in logical order. This may differ from <see cref="View.SubViews"/> order because
    ///     the focused tab is moved to the end of the draw list to render on top.
    /// </summary>
    public IEnumerable<View> TabCollection => ResolveTabCollection ();

    /// <summary>
    ///     Gets the logical index of the specified view within this <see cref="Tabs"/> container.
    /// </summary>
    /// <param name="view">The view to find.</param>
    /// <returns>The zero-based index, or -1 if the view is not a tab in this container.</returns>
    public int IndexOf (View view)
    {
        var i = 0;

        foreach (WeakReference<View> wr in _tabList)
        {
            if (wr.TryGetTarget (out View? target) && target == view)
            {
                return i;
            }

            i++;
        }

        return -1;
    }

    private Side _tabSide = Side.Top;

    /// <summary>
    ///     Gets or sets which side the tab headers are displayed on.
    /// </summary>
    /// <remarks>
    ///     Changing this property updates the <see cref="Border.TabSide"/>,
    ///     <see cref="IAdornment.Thickness"/>, scroll button positions, tab offsets,
    ///     and z-order for all tab SubViews.
    /// </remarks>
    public Side TabSide
    {
        get => _tabSide;
        set
        {
            if (_tabSide == value)
            {
                return;
            }

            _tabSide = value;

            UpdateTabBorderThickness ();
            UpdateScrollButtonPositions ();
            UpdateTabOffsets ();
            UpdateZOrder ();
            SetNeedsLayout ();
        }
    }

    private LineStyle _tabLineStyle = LineStyle.Rounded;

    /// <summary>
    ///     Gets or sets the <see cref="LineStyle"/> used for tab borders.
    ///     When set, updates the <see cref="View.BorderStyle"/> of all existing tab SubViews.
    /// </summary>
    public LineStyle TabLineStyle
    {
        get => _tabLineStyle;
        set
        {
            if (_tabLineStyle == value)
            {
                return;
            }

            _tabLineStyle = value;

            foreach (View tab in TabCollection)
            {
                tab.BorderStyle = _tabLineStyle;
            }

            UpdateTabBorderThickness ();
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     Gets or sets the depth of the tab header in rows (for <see cref="Side.Top"/>/<see cref="Side.Bottom"/>)
    ///     or columns (for <see cref="Side.Left"/>/<see cref="Side.Right"/>). The default is 3, which provides room
    ///     for the outside border, title text, and a 1-character separator line.
    /// </summary>
    /// <remarks>
    ///     Changing this value updates the <see cref="IAdornment.Thickness"/> of all tab SubViews. The depth determines
    ///     how much of the border is allocated to the tab header vs. the content area.
    /// </remarks>
    public int TabDepth
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            UpdateTabBorderThickness ();
        }
    } = 3;

    /// <inheritdoc/>
    protected override void OnFocusedChanged (View? previousFocused, View? focused)
    {
        base.OnFocusedChanged (previousFocused, focused);

        if (focused is TitleView)
        {
            return;
        }

        // Find which tab view now has focus (using logical order)
        View? focusedTab = TabCollection.FirstOrDefault (t => t.HasFocus);

        if (focusedTab is { })
        {
            Value = focusedTab;
        }
    }

    #region IValue<View?> Implementation

    private View? _value;

    /// <summary>
    ///     Gets or sets the currently selected tab view. Setting this focuses the specified view,
    ///     scrolls the tab headers to ensure it is visible, and updates the z-order so it draws on top.
    /// </summary>
    public View? Value { get => _value; set => ChangeValue (value); }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<View?>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<View?>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Called when <see cref="Value"/> is changing. Override to cancel the change.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<View?> args) => false;

    /// <summary>
    ///     Called when <see cref="Value"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<View?> args) { }

    private void ChangeValue (View? newValue)
    {
        if (_value == newValue)
        {
            return;
        }

        View? oldValue = _value;

        ValueChangingEventArgs<View?> changingArgs = new (oldValue, newValue);

        if (OnValueChanging (changingArgs) || changingArgs.Handled)
        {
            return;
        }

        ValueChanging?.Invoke (this, changingArgs);

        if (changingArgs.Handled)
        {
            return;
        }

        _value = newValue;

        if (_value is { HasFocus: false })
        {
            _value.SetFocus ();
        }

        if (_value is { })
        {
            EnsureTabVisible (_value);
        }

        UpdateZOrder ();
        SetNeedsLayout ();

        ValueChangedEventArgs<View?> changedArgs = new (oldValue, _value);
        OnValueChanged (changedArgs);
        ValueChanged?.Invoke (this, changedArgs);

        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _value));
    }

    #endregion

    #region SubView Management

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        return base.OnKeyDownNotHandled (key);

        if (MostFocused is TitleView)
        {
            if (TabSide is Side.Top or Side.Bottom)
            {
                // Prevent arrow keys from being handled by the title view, which would interfere with expected tab navigation
                if (key == Key.CursorRight)
                {
                    // Set focus to the next tab, wrapping to the first tab if currently on the last tab
                    View? nextTab = TabCollection.SkipWhile (t => !t.HasFocus).Skip (1).FirstOrDefault () ?? TabCollection.FirstOrDefault ();
                    var borderView = nextTab?.Border.View as BorderView;

                    return borderView?.TitleView?.SetFocus () ?? true;
                }

                if (key == Key.CursorLeft)
                {
                    // Set focus to the previous tab, wrapping to the last tab if currently on the first tab
                    View? previousTab = TabCollection.TakeWhile (t => !t.HasFocus).LastOrDefault () ?? TabCollection.LastOrDefault ();
                    var borderView = previousTab?.Border.View as BorderView;

                    return borderView?.TitleView?.SetFocus () ?? true;
                }

                if (key == Key.CursorDown && TabSide == Side.Top)
                {
                    // If the tab headers are on the top or bottom, allow down arrow to move focus into the selected tab content
                    if (Value?.PreviouslyFocused is { })
                    {
                        return Value?.PreviouslyFocused?.SetFocus () ?? false;
                    }

                    return Value?.FocusDeepest (NavigationDirection.Forward, null) ?? false;
                }

                if (key == Key.CursorDown && TabSide == Side.Bottom)
                {
                    // If the tab headers are on the top or bottom, allow down arrow to move focus into the selected tab content
                    HasFocus = false;

                    return true;
                }

                if (key == Key.CursorUp && TabSide == Side.Top)
                {
                    HasFocus = false;

                    return true;
                }

                if (key == Key.CursorUp && TabSide == Side.Bottom)
                {
                    // If the tab headers are on the top or bottom, allow down arrow to move focus into the selected tab content
                    if (Value?.PreviouslyFocused is { })
                    {
                        return Value?.PreviouslyFocused?.SetFocus () ?? false;
                    }

                    return Value?.FocusDeepest (NavigationDirection.Backward, null) ?? false;
                }
            }
            else
            {
                // TabSide is either Right or Left; same logic as above, but oriented for vertical tabs and using left/right arrows instead of up/down

                if (key == Key.CursorDown)
                {
                    // Set focus to the next tab, wrapping to the first tab if currently on the last tab
                    View? nextTab = TabCollection.SkipWhile (t => !t.HasFocus).Skip (1).FirstOrDefault () ?? TabCollection.FirstOrDefault ();

                    var borderView = nextTab?.Border.View as BorderView;

                    return borderView?.TitleView?.SetFocus () ?? true;
                }

                if (key == Key.CursorUp)
                {
                    // Set focus to the previous tab, wrapping to the last tab if currently on the first tab
                    View? previousTab = TabCollection.TakeWhile (t => !t.HasFocus).LastOrDefault () ?? TabCollection.LastOrDefault ();

                    return previousTab?.Border.View?.FocusDeepest (NavigationDirection.Backward, null) ?? true;
                }

                if (key == Key.CursorRight && TabSide == Side.Left)
                {
                    // Tab headers on the left: right arrow moves focus into the selected tab content
                    if (Value?.PreviouslyFocused is { })
                    {
                        return Value?.PreviouslyFocused?.SetFocus () ?? false;
                    }

                    return Value?.FocusDeepest (NavigationDirection.Forward, TabBehavior.NoStop) ?? false;
                }

                if (key == Key.CursorRight && TabSide == Side.Right)
                {
                    // Tab headers on the right: right arrow moves away from the strip
                    HasFocus = false;

                    return true;
                }

                if (key == Key.CursorLeft && TabSide == Side.Left)
                {
                    // Tab headers on the left: left arrow moves away from the strip
                    HasFocus = false;

                    return true;
                }

                if (key == Key.CursorLeft && TabSide == Side.Right)
                {
                    // Tab headers on the right: left arrow moves focus into the selected tab content
                    if (Value?.PreviouslyFocused is { })
                    {
                        return Value?.PreviouslyFocused?.SetFocus () ?? false;
                    }

                    return Value?.FocusDeepest (NavigationDirection.Backward, TabBehavior.NoStop) ?? false;
                }
            }
        }

        if (key == Application.GetDefaultKey (Command.NextTabGroup))
        {
            // Tab to the text TabHeader if focus is currently in the selected tab content

            return (TabCollection.SkipWhile (t => !t.HasFocus).Skip (1).FirstOrDefault () ?? TabCollection.FirstOrDefault ())?.SetFocus () ?? true;
        }

        if (key == Application.GetDefaultKey (Command.PreviousTabGroup))
        {
            // Tab to the TabHeaders if focus is currently in the selected tab content
            return (TabCollection.TakeWhile (t => !t.HasFocus).LastOrDefault () ?? TabCollection.LastOrDefault ())?.SetFocus () ?? true;
        }

        //// Top or Bottom
        //if (key == Key.CursorDown && TabSide is Side.Top or Side.Bottom)
        //{
        //    // Set focus to the next tab, wrapping to the first tab if currently on the last tab
        //    View? nextTab = TabCollection.SkipWhile (t => !t.HasFocus).Skip (1).FirstOrDefault () ?? TabCollection.FirstOrDefault ();
        //    return nextTab?.Border.View?.FocusDeepest (NavigationDirection.Forward, null) ?? true;
        //}
        //if (key == Key.CursorUp && TabSide is Side.Top or Side.Bottom)
        //{
        //    // Set focus to the previous tab, wrapping to the last tab if currently on the first tab
        //    View? previousTab = TabCollection.TakeWhile (t => !t.HasFocus).LastOrDefault () ?? TabCollection.LastOrDefault ();
        //    return previousTab?.Border.View?.FocusDeepest (NavigationDirection.Backward, null) ?? true;
        //}
        //// Left or Right
        //if (key == Key.CursorRight && TabSide is Side.Left or Side.Right)
        //{
        //    //Set focus to the next tab, wrapping to the first tab if currently on the last tab
        //    View? nextTab = TabCollection.SkipWhile (t => !t.HasFocus).Skip (1).FirstOrDefault () ?? TabCollection.FirstOrDefault ();
        //    return nextTab?.Border.View?.FocusDeepest (NavigationDirection.Forward, null) ?? true;
        //}
        //if (key == Key.CursorLeft && TabSide is Side.Left or Side.Right)
        //{
        //    // Set focus to the previous tab, wrapping to the last tab if currently on the first tab
        //    View? previousTab = TabCollection.TakeWhile (t => !t.HasFocus).LastOrDefault () ?? TabCollection.LastOrDefault ();
        //    return previousTab?.Border.View?.FocusDeepest (NavigationDirection.Backward, null) ?? true;
        //}
        // If focus is not on a tab title, allow tabbing into tab title
        return base.OnKeyDownNotHandled (key);
    }

    /// <summary>
    ///     Reorders the SubViews to match the logical tab order defined by <see cref="TabCollection"/>. This ensures that
    ///     navigation and other operations that rely on SubView order function according to the logical tab order, rather than
    ///     the draw order which may differ due to focused tab being drawn last.
    /// </summary>
    /// <param name="includeMargin"></param>
    /// <param name="includeBorder"></param>
    /// <param name="includePadding"></param>
    /// <returns></returns>
    public override IReadOnlyCollection<View> GetSubViews (bool includeMargin = false, bool includeBorder = false, bool includePadding = false)
    {
        List<View> subViewsOfThisTabs = new (base.GetSubViews (includeMargin, includeBorder, includePadding));

        // Reorder according to TabsCollection
        subViewsOfThisTabs.Sort ((v1, v2) =>
                                 {
                                     int index1 = TabCollection.TakeWhile (t => t != v1).Count ();
                                     int index2 = TabCollection.TakeWhile (t => t != v2).Count ();

                                     return index1.CompareTo (index2);
                                 });

        return subViewsOfThisTabs;
    }

    /// <inheritdoc/>
    protected override bool OnAdvancingFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        if (base.OnAdvancingFocus (direction, behavior))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Handles adding the new tab View to an internal tracking list, and ensuring <see cref="View.CanFocus"/> is
    ///     set to <see langword="false"/> so that focus will not change during the <see cref="View.Add(View)"/> flow.
    /// </summary>
    protected override bool OnSubViewAdding (EventArgs<View> args)
    {
        if (base.OnSubViewAdding (args))
        {
            return true;
        }

        // Add to internal tracking list
        _tabList.Add (new WeakReference<View> (args.Value));

        // Ensure CanFocus is false; otherwise Add will try to set focus on it, which can interfere
        // with the expected flow of focus being set to the first tab added, and then not changing on subsequent adds
        args.Value.CanFocus = false;

        return false;
    }

    /// <summary>
    ///     Configures a subview when it is added to the tab view, applying tab-specific layout, border, and focus settings.
    /// </summary>
    /// <remarks>
    ///     This method customizes the appearance and behavior of the subview to match the tabbed
    ///     interface, including setting border styles, layout, and focus properties. It also adds scroll indicator buttons
    ///     to the tab's border to provide visual affordance when scrolling is available.
    /// </remarks>
    /// <param name="view">The subview being added to the tab view. Cannot be null.</param>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        view.BorderStyle = _tabLineStyle;
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        view.Border.TabSide = _tabSide;
        view.Arrangement = ViewArrangement.Overlapped;
        view.Width = Dim.Fill ();
        view.Height = Dim.Fill ();
        view.SuperViewRendersLineCanvas = true;

        // Add scroll indicator buttons to the tab's border.
        // They occlude the separator line when visible, providing scroll affordance.
        AddScrollButtonsToBorder (view);

        UpdateZOrder ();
        UpdateTabBorderThickness ();
        UpdateTabOffsets ();

        // Re-enable focus on the view now that it has been configured as a tab.
        // Setting CanFocus causes the View to become focused.
        view.CanFocus = true;
        view.TabStop = TabBehavior.TabStop;
        view.Border.View?.CanFocus = true;

        // Setting the BorderView to NoStop ensures that the only way the TitleTabView will
        // get focus is if the user explicitly sets focus to it
        // or, if the users uses the Next/PreviousTabGroup key.
        view.Border.View?.TabStop = TabBehavior.NoStop;

        view.HotKeySpecifier = (Rune)'\xffff';

        view.Border.View?.CommandsToBubbleUp = [Command.Up, Command.Down, Command.Left, Command.Right];
    }

    /// <summary>
    ///     Handles removal of a SubView by removing it from the logical tab list.
    ///     If the removed view was the selected tab, the first remaining tab is selected.
    /// </summary>
    protected override void OnSubViewRemoved (View view)
    {
        base.OnSubViewRemoved (view);

        if (_disposing)
        {
            return;
        }

        // Remove the view (and any dead refs) from the tracking list
        _tabList.RemoveAll (wr => !wr.TryGetTarget (out View? target) || target == view);

        // If the removed view was the selected one, select the first tab
        if (Value == view)
        {
            _value = null;
            View? firstTab = TabCollection.FirstOrDefault ();

            if (firstTab is { })
            {
                Value = firstTab;
            }
        }

        UpdateZOrder ();
        UpdateTabBorderThickness ();
        UpdateTabOffsets ();
    }

    /// <summary>
    ///     Inserts a view as a tab at the specified logical index. The view is added as a SubView,
    ///     configured with tab border settings (same as <see cref="View.Add(View)"/>), and placed at
    ///     <paramref name="index"/> in the <see cref="TabCollection"/> order.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the tab. Clamped to the valid range.</param>
    /// <param name="view">The view to insert.</param>
    public void InsertTab (int index, View view)
    {
        // Add will trigger OnSubViewAdded, which appends to _tabList
        Add (view);

        // Move from the end (where OnSubViewAdded appended it) to the requested index
        WeakReference<View> lastRef = _tabList [^1];
        _tabList.RemoveAt (_tabList.Count - 1);
        _tabList.Insert (Math.Clamp (index, 0, _tabList.Count), lastRef);

        UpdateTabOffsets ();
        UpdateZOrder ();
    }

    #endregion

    #region Layout Helpers

    /// <summary>
    ///     Updates the z-order of tab SubViews so the focused tab is drawn last (on top). The tabs before the focused tab are
    ///     drawn in the order they were added (first added at back).
    ///     The tabs after the focused tab are drawn in reverse order they were added (last added at back).
    /// </summary>
    /// <remarks>
    ///     Z-ordering uses <see cref="View.SubViews"/> (draw order). Logical tab order is maintained
    ///     separately in the internal tab list.
    /// </remarks>
    private void UpdateZOrder ()
    {
        View? focusedTab = TabCollection.FirstOrDefault (t => t.HasFocus);

        if (focusedTab is { })
        {
            // Tabs before the focused tab are drawn in the order they were added (first added at back)
            foreach (View tab in TabCollection.TakeWhile (t => t != focusedTab))
            {
                MoveSubViewToEnd (tab);
            }

            // Focused tab is drawn on top of all others
            MoveSubViewToEnd (focusedTab);

            // Tabs after the focused tab are drawn in reverse order they were added (last added at back)
            foreach (View tab in TabCollection.SkipWhile (t => t != focusedTab).Skip (1))
            {
                MoveSubViewToStart (tab);
            }
        }
        else
        {
            // No focused tab - draw in reverse logical order (first tab at front)
            foreach (View tab in TabCollection.Reverse ())
            {
                MoveSubViewToStart (tab);
            }
        }
    }

    /// <summary>
    ///     Updates <see cref="Border.TabOffset"/> for all tabs based on <see cref="TabSide"/>
    ///     and the cumulative widths/heights of preceding tabs, adjusted by the current scroll offset.
    /// </summary>
    internal void UpdateTabOffsets ()
    {
        var offset = 0;

        foreach (View tab in TabCollection)
        {
            tab.Border.TabOffset = offset - ScrollOffset;

            int? tabLength = tab.Border.TabLength;

            if (tabLength is { })
            {
                // Subtract 1 because adjacent tabs share an edge
                offset += tabLength.Value - 1;
            }
        }

        UpdateScrollButtonVisibility ();
    }

    /// <summary>
    ///     Computes the total width (or height, for vertical tabs) of all tab headers, accounting for shared
    ///     borders between adjacent tabs.
    /// </summary>
    /// <returns>The total header span in cells, or 0 if there are no tabs.</returns>
    private int GetTotalHeaderSpan ()
    {
        var span = 0;

        foreach (View tab in TabCollection)
        {
            int? tabLength = tab.Border.TabLength;

            if (tabLength is { })
            {
                // Subtract 1 because adjacent tabs share an edge
                span += tabLength.Value - 1;
            }
        }

        // Add 1 because the last tab's trailing border is not shared
        return span > 0 ? span + 1 : 0;
    }

    /// <summary>
    ///     Updates <see cref="IAdornment.Thickness"/> and <see cref="Border.TabSide"/> for all tabs
    ///     based on the current <see cref="TabSide"/> and <see cref="TabDepth"/>.
    /// </summary>
    private void UpdateTabBorderThickness ()
    {
        foreach (View tab in TabCollection)
        {
            tab.Border.TabSide = _tabSide;

            tab.Border.Thickness = _tabSide switch
                                   {
                                       Side.Top => new Thickness (1, TabDepth, 1, 1),
                                       Side.Bottom => new Thickness (1, 1, 1, TabDepth),
                                       Side.Left => new Thickness (TabDepth, 1, 1, 1),
                                       Side.Right => new Thickness (1, 1, TabDepth, 1),
                                       _ => new Thickness (1, TabDepth, 1, 1)
                                   };
        }
    }

    #endregion

    #region Scrolling

    private bool? NavCommandHandler (ICommandContext? ctx)
    {
        if (ctx is null)
        {
            return false;
        }

        return TabSide switch
               {
                   // if side is top or bottom, left/right select previous/next tab; if side is left or right, up/down select previous/next tab
                   Side.Top or Side.Bottom when ctx.Command == Command.Right => SelectNextTab (),
                   Side.Top or Side.Bottom when ctx.Command == Command.Left => SelectPreviousTab (),
                   Side.Left or Side.Right when ctx.Command == Command.Down => SelectNextTab (),
                   Side.Left or Side.Right when ctx.Command == Command.Up => SelectPreviousTab (),
                   _ => false
               };
    }

    private bool? SelectNextTab ()
    {
        View? nextTab = TabCollection.SkipWhile (t => !t.HasFocus).Skip (1).FirstOrDefault () ?? TabCollection.FirstOrDefault ();

        return (nextTab?.Border.View as BorderView)?.TitleView?.SetFocus () ?? true;
    }

    private bool? SelectPreviousTab ()
    {
        View? previousTab = TabCollection.TakeWhile (t => !t.HasFocus).LastOrDefault () ?? TabCollection.LastOrDefault ();

        return (previousTab?.Border.View as BorderView)?.TitleView?.SetFocus () ?? true;
    }

    /// <summary>
    ///     Adds scroll indicator buttons to a tab's border. The buttons are positioned at the
    ///     start and end of the separator line and occlude it when visible.
    /// </summary>
    /// <param name="tab">The tab whose border receives the scroll buttons.</param>
    private void AddScrollButtonsToBorder (View tab)
    {
        ScrollButton scrollBack = new ();

        scrollBack.Accepting += (_, _) => { ScrollOffset--; };

        ScrollButton scrollForward = new ();

        scrollForward.Accepting += (_, _) => { ScrollOffset++; };

        PositionScrollButtons (scrollBack, scrollForward);

        tab.Border.View?.Add (scrollBack, scrollForward);
    }

    /// <summary>
    ///     Positions scroll buttons within a tab's border based on <see cref="TabSide"/>.
    /// </summary>
    private void PositionScrollButtons (ScrollButton scrollBack, ScrollButton scrollForward)
    {
        bool isHorizontal = _tabSide is Side.Top or Side.Bottom;

        if (isHorizontal)
        {
            int separatorY = _tabSide == Side.Top ? TabDepth - 1 : 0;
            scrollBack.Orientation = Orientation.Horizontal;
            scrollBack.Direction = NavigationDirection.Backward;
            scrollBack.X = 0;
            scrollBack.Y = separatorY;
            scrollForward.Orientation = Orientation.Horizontal;
            scrollForward.Direction = NavigationDirection.Forward;
            scrollForward.X = Pos.AnchorEnd ();
            scrollForward.Y = separatorY;
        }
        else
        {
            int separatorX = _tabSide == Side.Left ? TabDepth - 1 : 0;
            scrollBack.Orientation = Orientation.Vertical;
            scrollBack.Direction = NavigationDirection.Backward;
            scrollBack.X = separatorX;
            scrollBack.Y = 0;
            scrollForward.Orientation = Orientation.Vertical;
            scrollForward.Direction = NavigationDirection.Forward;
            scrollForward.X = separatorX;
            scrollForward.Y = Pos.AnchorEnd ();
        }
    }

    /// <summary>
    ///     Repositions all scroll indicator buttons when <see cref="TabSide"/> or <see cref="TabDepth"/> changes.
    /// </summary>
    private void UpdateScrollButtonPositions ()
    {
        foreach (View tab in TabCollection)
        {
            if (tab.Border.View is null)
            {
                continue;
            }

            ScrollButton? back = tab.Border.View.SubViews.OfType<ScrollButton> ().FirstOrDefault (b => b.Direction == NavigationDirection.Backward);
            ScrollButton? forward = tab.Border.View.SubViews.OfType<ScrollButton> ().FirstOrDefault (b => b.Direction == NavigationDirection.Forward);

            if (back is { } && forward is { })
            {
                PositionScrollButtons (back, forward);
            }
        }
    }

    /// <summary>
    ///     Updates the visibility of all scroll indicator buttons across all tabs
    ///     based on the current <see cref="ScrollOffset"/> and total header span.
    /// </summary>
    private void UpdateScrollButtonVisibility ()
    {
        int totalSpan = GetTotalHeaderSpan ();
        int visibleSize = _tabSide is Side.Top or Side.Bottom ? Viewport.Width : Viewport.Height;
        bool canScrollBack = ScrollOffset > 0;
        bool canScrollForward = totalSpan > ScrollOffset + visibleSize;

        foreach (View tab in TabCollection)
        {
            if (tab.Border.View is null)
            {
                continue;
            }

            foreach (ScrollButton btn in tab.Border.View.SubViews.OfType<ScrollButton> ())
            {
                btn.Visible = btn.Direction == NavigationDirection.Backward ? canScrollBack : canScrollForward;
            }
        }
    }

    /// <summary>
    ///     Gets or sets the current scroll offset for the tab headers. Adjusting this value scrolls the tab headers
    ///     along the <see cref="TabSide"/> edge.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The value is clamped to a valid range: negative values are clamped to <c>0</c>, and values exceeding
    ///         the total header span are clamped so that the last tab remains visible.
    ///     </para>
    ///     <para>
    ///         Setting this property updates all tab <see cref="Border.TabOffset"/> values, updates scroll button
    ///         visibility, and triggers a layout pass.
    ///     </para>
    /// </remarks>
    public int ScrollOffset
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            if (value > GetTotalHeaderSpan ())
            {
                // If value is greater than the maximum scroll offset, clamp it such that the last tab is flush with the edge of the viewport
                View? last = TabCollection.LastOrDefault ();

                if (last is { })
                {
                    field = GetTotalHeaderSpan () - (last.Border.TabOffset - 2);
                }
            }
            else if (value < 0)
            {
                // If value is less than 0, clamp it to 0
                field = 0;
            }
            else
            {
                field = value;
            }

            UpdateTabOffsets ();

            //UpdateZOrder ();
            SetNeedsLayout ();
        }
    }

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);

        if (e.OldViewport.Size != Size.Empty)
        {
            return;
        }

        // On initial layout, ensure the selected tab is visible (in case it's not the first tab or tabs exceed viewport size)
        if (Value is null)
        {
            return;
        }

        EnsureTabVisible (Value);
    }

    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        base.OnSubViewLayout (args);

        UpdateTabOffsets ();
    }

    /// <summary>
    ///     Adjusts <see cref="ScrollOffset"/> to ensure the specified tab's header is fully visible.
    /// </summary>
    /// <param name="tab">The tab whose header should be scrolled into view.</param>
    private void EnsureTabVisible (View tab)
    {
        int visibleSize = _tabSide is Side.Top or Side.Bottom ? Viewport.Width : Viewport.Height;

        // Don't adjust scroll before layout has determined the viewport size
        if (visibleSize <= 0)
        {
            return;
        }

        // Compute the absolute (unscrolled) offset for this tab
        int absOffset = TabCollection.TakeWhile (t => t != tab).Sum (t => (t.Border.TabLength ?? 0) - 1);

        int tabLength = tab.Border.TabLength ?? 0;
        int tabEnd = absOffset + tabLength;

        if (absOffset < ScrollOffset)
        {
            ScrollOffset = absOffset;
        }
        else if (tabEnd > ScrollOffset + visibleSize)
        {
            ScrollOffset = tabEnd - visibleSize;
        }

        UpdateTabOffsets ();
    }

    #endregion

    #region IDesignable

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        // BUGBUG: AttributePicker sets SuperViewRendersLineCanvas on it's subviews. In order to
        // BUGBUG: Prevent Tabs from being that superview, we must add via an intermediary View
        View attributesTab = new () { Id = "attributesTab", Title = "_Attribute" };
        AttributePicker attributePicker = new () { Y = 1, BorderStyle = LineStyle.Single };
        attributesTab.Add (attributePicker);

        // Add an OptionSelector directly
        OptionSelector<LineStyle> lineStyleTab = new () { Id = "lineStyleTab", Title = "_Line Style" };

        // Create an intermediary tab to hold multiple subviews
        View settingsTab = new () { Id = "settingsTab", Title = "Tab _Settings" };
        OptionSelector<Side> tabSideSelector = new () { Y = 1, BorderStyle = LineStyle.Single, Title = "S_ide" };
        tabSideSelector.Value = settingsTab.Border.TabSide;

        NumericUpDown<int> tabDepthNumericUpDown = new ()
        {
            Y = Pos.Top (tabSideSelector),
            X = Pos.Right (tabSideSelector) + 1,
            Width = 10,
            BorderStyle = LineStyle.Single,
            Title = "_Depth",
            Value = TabDepth
        };

        tabDepthNumericUpDown.ValueChanging += (_, e) =>
                                               {
                                                   if (e.NewValue < 0)
                                                   {
                                                       e.Handled = true;

                                                       return;
                                                   }

                                                   TabDepth = e.NewValue;
                                               };

        NumericUpDown<int> scrollOffsetNumericUpDown = new ()
        {
            Y = Pos.Bottom (tabSideSelector),
            Width = 20,
            BorderStyle = LineStyle.Single,
            Title = "Scroll _Offset",
            Value = ScrollOffset
        };

        scrollOffsetNumericUpDown.ValueChanging += (_, e) => { ScrollOffset = e.NewValue; };

        settingsTab.Add (tabSideSelector, tabDepthNumericUpDown, scrollOffsetNumericUpDown);

        View addRemoveTab = new () { Id = "addRemoveTab", Title = "Add_/Remove" };

        // Tab list showing all tabs in logical order
        ObservableCollection<string> tabListSource = new (TabCollection.Select (t => t.Title));

        ListView tabListView = new () { Width = Dim.Auto (), Height = Dim.Fill (), BorderStyle = LineStyle.Single, Title = "Ta_bs" };
        tabListView.SetSource (tabListSource);

        // Title input for new tabs
        Label titleLabel = new () { X = Pos.Right (tabListView) + 1, Text = "Title:" };

        TextField titleTextField = new () { X = Pos.Right (titleLabel) + 1, Y = Pos.Top (titleLabel), Text = "New Tab" };

        var setTitleButton = new Button { X = Pos.AnchorEnd (), Y = Pos.Top (titleTextField), Title = "Set _Title" };

        setTitleButton.Accepted += (_, _) =>
                                   {
                                       int? selectedIndex = tabListView.SelectedItem;

                                       if (selectedIndex is null)
                                       {
                                           return;
                                       }
                                       View tab = TabCollection.ElementAt (selectedIndex.Value);
                                       tab.Title = titleTextField.Text;
                                       RefreshList ();
                                   };

        titleTextField.Width = Dim.Fill (setTitleButton) - 1;

        // Add Before button
        Button addBeforeButton = new () { X = Pos.Right (tabListView) + 1, Y = Pos.Bottom (titleTextField), Text = "Add _Before" };

        addBeforeButton.Accepted += (_, _) =>
                                    {
                                        string title = titleTextField.Text;
                                        View newTab = new () { Title = title };
                                        int selectedIndex = tabListView.SelectedItem ?? 0;
                                        InsertTab (selectedIndex, newTab);
                                        RefreshList ();
                                        Value = addRemoveTab;
                                    };

        // Add After button
        Button addAfterButton = new () { X = Pos.Right (addBeforeButton) + 1, Y = Pos.Top (addBeforeButton), Text = "Add _After" };

        addAfterButton.Accepted += (_, _) =>
                                   {
                                       string title = titleTextField.Text;
                                       View newTab = new () { Title = title };
                                       int selectedIndex = (tabListView.SelectedItem ?? 0) + 1;
                                       InsertTab (selectedIndex, newTab);
                                       RefreshList ();
                                       Value = addRemoveTab;
                                   };

        // Remove button
        Button removeButton = new () { X = Pos.Right (addAfterButton) + 1, Y = Pos.Top (addAfterButton), Text = "_Remove" };

        removeButton.Accepted += (_, _) =>
                                 {
                                     int? selectedIndex = tabListView.SelectedItem;

                                     if (selectedIndex is null)
                                     {
                                         return;
                                     }

                                     List<View> tabs = TabCollection.ToList ();

                                     if (selectedIndex.Value >= tabs.Count)
                                     {
                                         return;
                                     }
                                     Remove (tabs [selectedIndex.Value]);
                                     tabs [selectedIndex.Value].Dispose ();
                                     RefreshList ();
                                     Value = addRemoveTab;
                                 };

        addRemoveTab.Add (tabListView, titleLabel, titleTextField, setTitleButton, addBeforeButton, addAfterButton, removeButton);

        // Refresh the list whenever Value changes (tab added/removed/selected)
        ValueChanged += (_, _) => RefreshList ();

        attributePicker.ValueChanged += (_, e) =>
                                        {
                                            if (e.NewValue is { })
                                            {
                                                SetScheme (GetScheme () with
                                                {
                                                    Normal = new Attribute (e.NewValue.Value.Foreground, e.NewValue.Value.Background),
                                                    Focus = new Attribute (e.NewValue.Value.Foreground, e.NewValue.Value.Background)
                                                });
                                            }
                                        };

        lineStyleTab.ValueChanged += (_, e) =>
                                     {
                                         if (e.Value is { })
                                         {
                                             TabLineStyle = e.Value.Value;
                                         }
                                     };

        tabSideSelector.ValueChanged += (_, e) =>
                                        {
                                            if (e.Value is { })
                                            {
                                                TabSide = e.Value.Value;
                                            }
                                        };

        Add (attributesTab, lineStyleTab, settingsTab, addRemoveTab);

        //tab1.SetFocus ();

        return true;

        // Helper to refresh the list from current TabCollection
        void RefreshList ()
        {
            tabListSource.Clear ();

            foreach (View t in TabCollection)
            {
                tabListSource.Add (t.Title);
            }
        }
    }

    #endregion

    private bool _disposing;

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            _disposing = true;
        }

        base.Dispose (disposing);
    }
}

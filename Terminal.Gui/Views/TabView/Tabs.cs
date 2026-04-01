using System.Collections.ObjectModel;

namespace Terminal.Gui.Views;

/// <summary>
///     A container <see cref="View"/> that manages a collection of <see cref="View"/> SubViews,
///     rendering them as a tabbed interface. The currently focused view is the selected tab.
/// </summary>
/// <remarks>
///     <para>
///         Add any <see cref="View"/> instances as SubViews via <see cref="View.Add(View)"/>. The <see cref="Tabs"/>
///         container automatically configures tab-related properties (border settings, thickness, arrangement,
///         and z-order) on each added view.
///     </para>
///     <para>
///         The selected tab is determined by focus — whichever view has focus is the
///         selected tab. Use <see cref="Value"/> to get or set the selected view.
///     </para>
///     <para>
///         Implements <see cref="IValue{T}"/> with <c>View?</c> as the value type, enabling
///         integration with the command/prompt system.
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

        Width = Dim.Fill ();
        Height = Dim.Fill ();

        SetupHeaderScrollBar ();
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
    ///     Gets the tabs in logical order, which may differ from SubViews order
    ///     due to z-ordering of the focused tab.
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
    ///     Changing this property updates the <see cref="Border.TabSide"/> and
    ///     <see cref="Drawing.Thickness"/> of all tab SubViews.
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
            UpdateTabOffsets ();
            UpdateZOrder ();
            PositionHeaderScrollBar ();
            SetNeedsLayout ();
        }
    }

    private LineStyle _tabLineStyle = LineStyle.Rounded;

    /// <summary>
    ///     Gets or sets the <see cref="LineStyle"/> used for tab borders.
    ///     When set, updates all tab SubViews.
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

            SetNeedsLayout ();
        }
    }

    #region IValue<View?> Implementation

    private View? _value;

    /// <summary>
    ///     Gets or sets the currently selected tab view.
    ///     Setting this focuses the specified view.
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
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        // Add to internal tracking list
        _tabList.Add (new WeakReference<View> (view));

        // Configure the view as a tab
        view.TabStop = TabBehavior.TabStop;
        view.CanFocus = true;
        view.BorderStyle = _tabLineStyle;
        view.Border.Settings = BorderSettings.Tab | BorderSettings.Title;
        view.Border.TabSide = _tabSide;
        view.Arrangement = ViewArrangement.Overlapped;
        view.Width = Dim.Fill ();
        view.Height = Dim.Fill ();
        view.SuperViewRendersLineCanvas = true;

        // Focus first tab added (common convention for tabbed interfaces).
        // Subsequent tabs will not steal focus when added.
        TabCollection.FirstOrDefault ()?.SetFocus ();

        UpdateZOrder ();
        UpdateTabBorderThickness ();
        UpdateTabOffsets ();
    }

    /// <inheritdoc/>
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

        UpdateTabBorderThickness ();
        UpdateTabOffsets ();
        UpdateZOrder ();
    }

    /// <summary>
    ///     Inserts a view as a tab at the specified logical index. The view is added as a SubView
    ///     and configured as a tab, but placed at <paramref name="index"/> in the logical tab order.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert the tab.</param>
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

        // Add 1 because the last tab's trailing border is not shared
        UpdateScrollBarSizes (offset > 0 ? offset + 1 : 0);
    }

    /// <summary>
    ///     Gets or sets the depth of the tab. The default is 3, which means the tab will have room for the outside border,
    ///     title, and a 1-character tab border. Adjust this if you have a thicker border or want more/less space in the tab
    ///     header.
    /// </summary>
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

    /// <summary>
    ///     Updates <see cref="Drawing.Thickness"/> for all tabs based on <see cref="TabSide"/>.
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

    private ScrollBar? _headerScrollBar;

    /// <summary>
    ///     Creates and configures the header scrollbar in the Border adornment.
    ///     The scrollbar overlays the content border line; its slider is transparent
    ///     so the border line shows through.
    /// </summary>
    private void SetupHeaderScrollBar ()
    {
        _headerScrollBar = new ScrollBar { VisibilityMode = ScrollBarVisibilityMode.Auto };

        _headerScrollBar.Slider.Visible = false;
        _headerScrollBar.Visible = false;

        _headerScrollBar.ValueChanged += (_, args) => { ScrollOffset = args.NewValue; };

        PositionHeaderScrollBar ();
    }

    /// <summary>
    ///     Gets or sets the current scroll offset for the tab headers. Adjusting this value scrolls the tab headers.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The value is clamped to a valid range: negative values are clamped to <c>0</c>, and values exceeding
    ///         the maximum scrollable extent (determined by the total header span and viewport size) are clamped so that
    ///         the last tab remains flush with the trailing edge of the viewport.
    ///     </para>
    ///     <para>
    ///         Setting this property updates all tab <see cref="Border.TabOffset"/> values, refreshes
    ///         the z-order, and triggers a layout pass.
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

            if (value > _headerScrollBar?.ScrollableContentSize)
            {
                // If value is greater than the maximum scroll offset, clamp it such that the last tab is flush with the edge of the viewport
                View? last = TabCollection.LastOrDefault ();

                if (last is { })
                {
                    field = _headerScrollBar?.ScrollableContentSize - (last.Border.TabOffset - 2) ?? 0;
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
            UpdateZOrder ();
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     Adds the header scrollbar to Padding. Called after initialization to avoid
    ///     modifying the subview collection during EndInit enumeration.
    /// </summary>
    private void AddHeaderScrollBarToPadding ()
    {
        if (_headerScrollBar is null || _headerScrollBarAdded)
        {
            return;
        }

        Padding.GetOrCreateView ();
        Padding.View!.Add (_headerScrollBar);
        _headerScrollBarAdded = true;
    }

    private bool _headerScrollBarAdded;

    /// <summary>
    ///     Positions and orients the header scrollbar based on <see cref="TabSide"/>.
    /// </summary>
    private void PositionHeaderScrollBar ()
    {
        if (_headerScrollBar is null)
        {
            return;
        }

        switch (_tabSide)
        {
            case Side.Top:
                _headerScrollBar.Orientation = Orientation.Horizontal;
                _headerScrollBar.X = 0;
                _headerScrollBar.Y = 0;
                _headerScrollBar.Width = Dim.Fill ();
                _headerScrollBar.Height = 1;

                break;

            case Side.Bottom:
                _headerScrollBar.Orientation = Orientation.Horizontal;
                _headerScrollBar.X = 0;
                _headerScrollBar.Y = Pos.AnchorEnd ();
                _headerScrollBar.Width = Dim.Fill ();
                _headerScrollBar.Height = 1;

                break;

            case Side.Left:
                _headerScrollBar.Orientation = Orientation.Vertical;
                _headerScrollBar.X = 0;
                _headerScrollBar.Y = 0;
                _headerScrollBar.Width = 1;
                _headerScrollBar.Height = Dim.Fill ();

                break;

            case Side.Right:
                _headerScrollBar.Orientation = Orientation.Vertical;
                _headerScrollBar.X = Pos.AnchorEnd ();
                _headerScrollBar.Y = 0;
                _headerScrollBar.Width = 1;
                _headerScrollBar.Height = Dim.Fill ();

                break;
        }
    }

    /// <summary>
    ///     Updates the header scrollbar's <see cref="ScrollBar.ScrollableContentSize"/>
    ///     and <see cref="ScrollBar.VisibleContentSize"/> based on the total tab header span.
    /// </summary>
    /// <param name="totalHeaderSpan">The total cumulative width (or height) of all tab headers.</param>
    private void UpdateScrollBarSizes (int totalHeaderSpan)
    {
        if (_headerScrollBar is null)
        {
            return;
        }

        _headerScrollBar.ScrollableContentSize = totalHeaderSpan;

        _headerScrollBar.VisibleContentSize = _tabSide is Side.Top or Side.Bottom ? Viewport.Width : Viewport.Height;

        int scrollBarSize = _headerScrollBar.Visible ? 1 : 0;

        Padding.Thickness = _tabSide switch
        {
            Side.Top => new Thickness (0, scrollBarSize, 0, 0),
            Side.Bottom => new Thickness (0, 0, 0, scrollBarSize),
            Side.Left => new Thickness (scrollBarSize, 0, 0, 0),
            Side.Right => new Thickness (0, 0, scrollBarSize, 0),
            _ => new Thickness (0, scrollBarSize, 0, 0)
        };
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

        // Ensure the scrollbar is added to Padding (deferred from constructor)
        AddHeaderScrollBarToPadding ();

        EnsureTabVisible (Value);
    }

    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        base.OnSubViewLayout (args);

        // Update scrollbar visibility/sizing
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
        var absOffset = 0;

        foreach (View t in TabCollection)
        {
            if (t == tab)
            {
                break;
            }

            absOffset += (t.Border.TabLength ?? 0) - 1;
        }

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

        _headerScrollBar?.Value = ScrollOffset;

        UpdateTabOffsets ();
    }

    #endregion

    #region Focus Handling

    /// <inheritdoc/>
    protected override void OnFocusedChanged (View? previousFocused, View? focused)
    {
        base.OnFocusedChanged (previousFocused, focused);

        if (focused is TabTitleView)
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

    #endregion

    #region IDesignable

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        // BUGBUG: AttributePicker sets SuperViewRendersLineCanvas on it's subviews. In order to
        // BUGBUG: Prevent Tabs from being that superview, we must add via an intermediary View
        View tab1 = new () { Title = "_Attribute" };
        AttributePicker attributePicker = new () { Y = 1, BorderStyle = LineStyle.Single };
        tab1.Add (attributePicker);

        // Add an OptionSelector directly
        OptionSelector<LineStyle> lineStyleSelector = new () { Title = "_Line Style" };

        // Create an intermediary tab to hold multiple subviews
        View tab3 = new () { Title = "Tab _Settings" };
        OptionSelector<Side> tabSideSelector = new () { Y = 1, BorderStyle = LineStyle.Single, Title = "S_ide" };
        tabSideSelector.Value = tab3.Border.TabSide;

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

        scrollOffsetNumericUpDown.ValueChanging += (_, e) =>
                                                   {
                                                       ScrollOffset = e.NewValue;
                                                   };

        tab3.Add (tabSideSelector, tabDepthNumericUpDown, scrollOffsetNumericUpDown);

        View addRemoveTab = new () { Title = "A_dd/Remove" };

        // Tab list showing all tabs in logical order
        ObservableCollection<string> tabListSource = new (TabCollection.Select (t => t.Title ?? "(untitled)"));

        ListView tabListView = new () { Width = Dim.Auto (), Height = Dim.Fill (), BorderStyle = LineStyle.Single, Title = "Ta_bs" };
        tabListView.SetSource (tabListSource);

        // Title input for new tabs
        Label titleLabel = new () { X = Pos.Right (tabListView) + 1, Text = "Title:" };

        TextField titleTextField = new () { X = Pos.Right (titleLabel) + 1, Y = Pos.Top (titleLabel), Width = Dim.Fill (), Text = "New Tab" };

        // Add Before button
        Button addBeforeButton = new () { X = Pos.Right (tabListView) + 1, Y = Pos.Bottom (titleTextField), Text = "Add _Before" };

        addBeforeButton.Accepting += (_, _) =>
                                     {
                                         string title = titleTextField.Text ?? "New Tab";
                                         View newTab = new () { Title = title };
                                         int selectedIndex = tabListView.SelectedItem ?? 0;
                                         InsertTab (selectedIndex, newTab);
                                         RefreshList ();
                                         Value = addRemoveTab;
                                     };

        // Add After button
        Button addAfterButton = new () { X = Pos.Right (addBeforeButton) + 1, Y = Pos.Top (addBeforeButton), Text = "Add _After" };

        addAfterButton.Accepting += (_, _) =>
                                    {
                                        string title = titleTextField.Text ?? "New Tab";
                                        View newTab = new () { Title = title };
                                        int selectedIndex = (tabListView.SelectedItem ?? 0) + 1;
                                        InsertTab (selectedIndex, newTab);
                                        RefreshList ();
                                        Value = addRemoveTab;
                                    };

        // Remove button
        Button removeButton = new () { X = Pos.Right (addAfterButton) + 1, Y = Pos.Top (addAfterButton), Text = "_Remove" };

        removeButton.Accepting += (_, _) =>
                                  {
                                      int? selectedIndex = tabListView.SelectedItem;

                                      if (selectedIndex is null)
                                      {
                                          return;
                                      }

                                      List<View> tabs = TabCollection.ToList ();

                                      if (selectedIndex.Value < tabs.Count)
                                      {
                                          Remove (tabs [selectedIndex.Value]);
                                          tabs [selectedIndex.Value].Dispose ();
                                          RefreshList ();
                                          Value = addRemoveTab;
                                      }
                                  };

        addRemoveTab.Add (tabListView, titleLabel, titleTextField, addBeforeButton, addAfterButton, removeButton);

        Add (tab1, lineStyleSelector, tab3, addRemoveTab);

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

        lineStyleSelector.ValueChanged += (_, e) =>
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

        return true;

        // Helper to refresh the list from current TabCollection
        void RefreshList ()
        {
            tabListSource.Clear ();

            foreach (View t in TabCollection)
            {
                tabListSource.Add (t.Title ?? "(untitled)");
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

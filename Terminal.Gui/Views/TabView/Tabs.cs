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
    ///     and the cumulative widths/heights of preceding tabs.
    /// </summary>
    internal void UpdateTabOffsets ()
    {
        var offset = 0;

        foreach (View tab in TabCollection)
        {
            tab.Border.TabOffset = offset;

            int? tabLength = tab.Border.TabLength;

            if (tabLength is { })
            {
                // Subtract 1 because adjacent tabs share an edge
                offset += tabLength.Value - 1;
            }
        }
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
        tab3.Add (tabSideSelector, tabDepthNumericUpDown);

        View tab4 = new () { Title = "A_dd/Remove" };

        Add (tab1, lineStyleSelector, tab3, tab4);

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

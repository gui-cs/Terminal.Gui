namespace Terminal.Gui.Views;

/// <summary>
///     A container <see cref="View"/> that manages a collection of <see cref="Tab"/> SubViews,
///     rendering them as a tabbed interface. The currently focused <see cref="Tab"/> is the selected tab.
/// </summary>
/// <remarks>
///     <para>
///         Add <see cref="Tab"/> instances as SubViews via <see cref="View.Add(View)"/>. The <see cref="Tabs"/>
///         container automatically manages tab indices, offsets, border thickness, and z-order.
///     </para>
///     <para>
///         The selected tab is determined by focus — whichever <see cref="Tab"/> has focus is the
///         selected tab. Use <see cref="Value"/> to get or set the selected <see cref="Tab"/>.
///     </para>
///     <para>
///         Implements <see cref="IValue{T}"/> with <c>Tab?</c> as the value type, enabling
///         integration with the command/prompt system.
///     </para>
/// </remarks>
public class Tabs : View, IValue<Tab?>, IDesignable
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

    /// <summary>
    ///     Gets the tabs in logical order (by <see cref="Tab.TabIndex"/>), which may differ
    ///     from SubViews order due to z-ordering of the focused tab.
    /// </summary>
    public IEnumerable<Tab> TabCollection => SubViews.OfType<Tab> ().OrderBy (t => t.TabIndex);

    private Side _tabSide = Side.Top;

    /// <summary>
    ///     Gets or sets which side the tab headers are displayed on.
    /// </summary>
    /// <remarks>
    ///     Changing this property updates the <see cref="Border.TabSide"/> and
    ///     <see cref="Drawing.Thickness"/> of all <see cref="Tab"/> SubViews.
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
    ///     When set, updates all <see cref="Tab"/> SubViews.
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

            foreach (Tab tab in TabCollection)
            {
                tab.BorderStyle = _tabLineStyle;
            }

            SetNeedsLayout ();
        }
    }

    #region IValue<Tab?> Implementation

    private Tab? _value;

    /// <summary>
    ///     Gets or sets the currently selected <see cref="Tab"/>.
    ///     Setting this focuses the specified tab.
    /// </summary>
    public Tab? Value { get => _value; set => ChangeValue (value); }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<Tab?>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<Tab?>>? ValueChanged;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Called when <see cref="Value"/> is changing. Override to cancel the change.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    /// <returns><see langword="true"/> to cancel the change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnValueChanging (ValueChangingEventArgs<Tab?> args) => false;

    /// <summary>
    ///     Called when <see cref="Value"/> has changed.
    /// </summary>
    /// <param name="args">The event arguments containing old and new values.</param>
    protected virtual void OnValueChanged (ValueChangedEventArgs<Tab?> args) { }

    private void ChangeValue (Tab? newValue)
    {
        if (_value == newValue)
        {
            return;
        }

        Tab? oldValue = _value;

        ValueChangingEventArgs<Tab?> changingArgs = new (oldValue, newValue);

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

        ValueChangedEventArgs<Tab?> changedArgs = new (oldValue, _value);
        OnValueChanged (changedArgs);
        ValueChanged?.Invoke (this, changedArgs);

        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (oldValue, _value));
    }

    #endregion

    #region SubView Management

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        if (view is not Tab)
        {
            throw new ArgumentException (@"Only Views of type Tab can be added to Tabs", nameof (view));
        }
        base.OnSubViewAdded (view);

        if (view is not Tab tab)
        {
            return;
        }

        // Assign TabIndex as max existing + 1
        int maxIndex = SubViews.OfType<Tab> ().Where (t => t != tab).Select (t => t.TabIndex).DefaultIfEmpty (-1).Max ();
        tab.TabIndex = maxIndex + 1;

        // Configure the tab
        tab.Border.TabSide = _tabSide;
        tab.BorderStyle = _tabLineStyle;
        base.SuperViewRendersLineCanvas = true;
        tab.Width = Dim.Fill ();
        tab.Height = Dim.Fill ();

        // this will cause the first tab added to be focused and selected by default, which is a common convention for tabbed interfaces.
        // Subsequent tabs will not steal focus when added.
        TabCollection.FirstOrDefault ()?.SetFocus ();

        UpdateTabBorderThickness ();
        UpdateTabOffsets ();
    }

    /// <inheritdoc/>
    protected override void OnSubViewRemoved (View view)
    {
        base.OnSubViewRemoved (view);

        if (view is not Tab removedTab || _disposing)
        {
            return;
        }

        // Re-index remaining tabs
        var i = 0;

        foreach (Tab tab in TabCollection)
        {
            tab.TabIndex = i++;
        }

        // If the removed tab was the selected one, select the first tab
        if (Value == removedTab)
        {
            _value = null;
            Tab? firstTab = TabCollection.FirstOrDefault ();

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

    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs args) => base.OnSubViewLayout (args);

    //UpdateZOrder();
    /// <summary>
    ///     Updates the z-order of tab SubViews so the focused tab is drawn last (on top). The tabs before the focused tab are
    ///     drawn in the order they were added (first added at back).
    ///     the tabs after the focused tab are drawn in reverse order they were added (last added at back).
    /// </summary>
    private void UpdateZOrder ()
    {
        Tab? focusedTab = SubViews.OfType<Tab> ().FirstOrDefault (t => t.HasFocus);

        if (focusedTab is { })
        {
            // Tabs before the focused tab are drawn in the order they were added (first added at back)
            foreach (Tab tab in TabCollection.TakeWhile (t => t != focusedTab))
            {
                MoveSubViewToEnd (tab);
            }

            // Focused tab is drawn on top of all others
            MoveSubViewToEnd (focusedTab);

            // Tabs after the focused tab are drawn in reverse order they were added (last added at back)
            foreach (Tab tab in TabCollection.SkipWhile (t => t != focusedTab).Skip (1))
            {
                MoveSubViewToStart (tab);
            }
        }
        else
        {
            // Tabs before the focused tab are drawn in the order they were added (first added at back)
            foreach (Tab tab in TabCollection.Reverse ())
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

        foreach (Tab tab in TabCollection)
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
    ///     Gets or sets the depth of the tab. The default is 3, which tht tab will have room for the outside border, title,
    ///     and a 1-character tab border. Adjust this if you have a thicker border or want more/less space in the tab header.
    /// </summary>
    public int TabDepth
    {
        get => field;
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
        foreach (Tab tab in TabCollection)
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

        // Find which Tab now has focus
        Tab? focusedTab = SubViews.OfType<Tab> ().FirstOrDefault (t => t.HasFocus);

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
        Tab tab1 = new () { Title = "_Attribute" };
        AttributePicker attributePicker = new () { Y = 1, BorderStyle = LineStyle.Single };
        tab1.Add (attributePicker);

        Tab tab2 = new () { Title = "_Line Style" };
        OptionSelector<LineStyle> lineStyleSelector = new () { Y = 1, BorderStyle = LineStyle.Single };
        tab2.Add (lineStyleSelector);

        Tab tab3 = new () { Title = "Tab _Settings" };
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

        tabDepthNumericUpDown.ValueChanging += (s, e) =>
                                               {
                                                   if (e.NewValue is < 0)
                                                   {
                                                       e.Handled = true;

                                                       return;
                                                   }

                                                   TabDepth = e.NewValue;
                                               };
        tab3.Add (tabSideSelector, tabDepthNumericUpDown);

        Tab tab4 = new () { Title = "Fourth" };

        Add (tab1, tab2, tab3, tab4);

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

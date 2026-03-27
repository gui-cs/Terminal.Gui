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
        tab.Width = Dim.Fill ();
        tab.Height = Dim.Fill ();

        UpdateTabBorderThickness ();
        UpdateTabOffsets ();
        UpdateZOrder ();
    }

    /// <inheritdoc/>
    protected override void OnSubViewRemoved (View view)
    {
        if (view is not Tab removedTab)
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
        if (_value == removedTab)
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

    /// <summary>
    ///     Updates the z-order of tab SubViews so the focused tab is drawn last (on top).
    /// </summary>
    private void UpdateZOrder ()
    {
        // Move tabs to start in logical order
        foreach (Tab tab in TabCollection)
        {
            MoveSubViewToStart (tab);
        }

        // Then move the selected tab to end so it draws on top
        if (_value is { })
        {
            MoveSubViewToEnd (_value);
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
    ///     Updates <see cref="Drawing.Thickness"/> for all tabs based on <see cref="TabSide"/>.
    /// </summary>
    private void UpdateTabBorderThickness ()
    {
        foreach (Tab tab in TabCollection)
        {
            tab.Border.TabSide = _tabSide;

            tab.Border.Thickness = _tabSide switch
                                   {
                                       Side.Top => new Thickness (1, 3, 1, 1),
                                       Side.Bottom => new Thickness (1, 1, 1, 3),
                                       Side.Left => new Thickness (3, 1, 1, 1),
                                       Side.Right => new Thickness (1, 1, 3, 1),
                                       _ => new Thickness (1, 3, 1, 1)
                                   };
        }
    }

    #endregion

    #region Focus Handling

    /// <inheritdoc />
    protected override void OnFocusedChanged (View? previousFocused, View? focused)
    {
        base.OnFocusedChanged (previousFocused, focused);

        // Find which Tab now has focus
        Tab? focusedTab = SubViews.OfType<Tab> ().FirstOrDefault (t => t.HasFocus);

        if (focusedTab is { } && focusedTab != _value)
        {
            Value = focusedTab;
        }
    }

    #endregion

    #region IDesignable

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Tab tab1 = new () { Title = "_Tab1", Text = "Tab1 Content" };
        Tab tab2 = new () { Title = "T_ab2", Text = "Tab2 Content" };
        Tab tab3 = new () { Title = "Ta_b3", Text = "Tab3 Content" };

        Add (tab1, tab2, tab3);
        Value = tab1;

        return true;
    }

    #endregion
}

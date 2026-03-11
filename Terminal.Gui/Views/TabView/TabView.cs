namespace Terminal.Gui.Views;

/// <summary>
///     A view that displays multiple <see cref="Tab"/> views, with a row of tab headers for switching
///     between them. Tabs are added as SubViews via <see cref="View.Add(View)"/>.
/// </summary>
/// <remarks>
///     <para>
///         Tab IS the content — developers add their content views directly to <see cref="Tab"/> SubViews.
///         The <see cref="View.Title"/> property of each Tab provides the header text.
///     </para>
///     <para>
///         TabRow, which renders the tab headers, lives in the Padding adornment (similar to how
///         <see cref="Dialog"/> hosts its buttons in Padding).
///     </para>
/// </remarks>
public class TabView : View, IDesignable
{
    private readonly TabRow _tabRow;

    private int? _selectedTabIndex;
    private bool _tabsOnBottom;
    private uint _maxTabTextWidth = 30;

    /// <summary>Creates a new <see cref="TabView"/>.</summary>
    public TabView ()
    {
        TabStop = TabBehavior.TabGroup;
        SuperViewRendersLineCanvas = true;
        BorderStyle = LineStyle.Rounded;

        _tabRow = new ();
        Padding!.Add (_tabRow);

        // Default: tabs on top — reserve 2 rows in Padding.Top for TabRow
        Padding.Thickness = Padding.Thickness with { Top = 2 };
        _tabRow.Y = 0;

        // Register commands
        AddCommand (Command.Left, () => SelectPreviousTab ());
        AddCommand (Command.Right, () => SelectNextTab ());
        AddCommand (Command.LeftStart, () => SelectFirstTab ());
        AddCommand (Command.RightEnd, () => SelectLastTab ());

        // Bind keys
        KeyBindings.Add (Key.CursorLeft.WithCtrl, Command.Left);
        KeyBindings.Add (Key.CursorRight.WithCtrl, Command.Right);
        KeyBindings.Add (Key.Home.WithCtrl, Command.LeftStart);
        KeyBindings.Add (Key.End.WithCtrl, Command.RightEnd);
    }

    /// <summary>Gets the list of <see cref="Tab"/> SubViews in this TabView.</summary>
    public IReadOnlyList<Tab> Tabs => SubViews.OfType<Tab> ().ToList ();

    /// <summary>
    ///     Gets or sets the index of the currently selected tab. <see langword="null"/> means no tab is selected.
    /// </summary>
    public int? SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            IReadOnlyList<Tab> tabs = Tabs;

            if (value.HasValue && (value.Value < 0 || value.Value >= tabs.Count))
            {
                return;
            }

            Tab? oldTab = SelectedTab;
            _selectedTabIndex = value;

            // Hide all tabs, show only the selected one (Wizard pattern)
            foreach (Tab tab in tabs)
            {
                tab.Visible = false;
            }

            if (value.HasValue)
            {
                tabs [value.Value].Visible = true;
            }

            // Update tab header appearance
            _tabRow.UpdateHeaderAppearance ();

            Tab? newTab = SelectedTab;

            OnSelectedTabChanged (new ValueChangedEventArgs<Tab?> (oldTab, newTab));

            SetNeedsLayout ();
        }
    }

    /// <summary>Gets the currently selected <see cref="Tab"/>, or <see langword="null"/> if no tab is selected.</summary>
    public Tab? SelectedTab => _selectedTabIndex.HasValue
                                   ? Tabs.ElementAtOrDefault (_selectedTabIndex.Value)
                                   : null;

    /// <summary>Gets or sets whether tabs are displayed at the bottom of the view instead of the top.</summary>
    public bool TabsOnBottom
    {
        get => _tabsOnBottom;
        set
        {
            if (_tabsOnBottom == value)
            {
                return;
            }

            _tabsOnBottom = value;

            if (value)
            {
                Padding!.Thickness = Padding.Thickness with { Top = 0, Bottom = 2 };
                _tabRow.Y = Pos.AnchorEnd ();
            }
            else
            {
                Padding!.Thickness = Padding.Thickness with { Top = 2, Bottom = 0 };
                _tabRow.Y = 0;
            }

            _tabRow.UpdateHeaderAppearance ();
            SetNeedsLayout ();
        }
    }

    /// <summary>Gets or sets the maximum display width for tab header text. Default is 30.</summary>
    public uint MaxTabTextWidth
    {
        get => _maxTabTextWidth;
        set
        {
            _maxTabTextWidth = value;
            _tabRow.RebuildHeaders ();
        }
    }

    /// <summary>Raised after <see cref="SelectedTabIndex"/> changes.</summary>
    public event EventHandler<ValueChangedEventArgs<Tab?>>? SelectedTabChanged;

    /// <summary>Raises the <see cref="SelectedTabChanged"/> event.</summary>
    protected virtual void OnSelectedTabChanged (ValueChangedEventArgs<Tab?> args)
    {
        SelectedTabChanged?.Invoke (this, args);
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        base.OnSubViewAdded (view);

        if (view is Tab)
        {
            _tabRow.RebuildHeaders ();

            // If no tab is selected, select this one
            if (!_selectedTabIndex.HasValue)
            {
                SelectedTabIndex = Tabs.Count - 1;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnSubViewRemoved (View view)
    {
        base.OnSubViewRemoved (view);

        if (view is Tab)
        {
            _tabRow.RebuildHeaders ();

            IReadOnlyList<Tab> tabs = Tabs;

            // Adjust selection if the removed tab was selected or if selection is out of range
            if (tabs.Count == 0)
            {
                _selectedTabIndex = null;
            }
            else if (_selectedTabIndex.HasValue && _selectedTabIndex.Value >= tabs.Count)
            {
                SelectedTabIndex = tabs.Count - 1;
            }
            else
            {
                // Re-apply to refresh visibility
                SelectedTabIndex = _selectedTabIndex;
            }
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Tab tab1 = new () { Title = "Tab_1" };
        tab1.Add (new Label { Text = "Label in Tab1" });

        Tab tab2 = new () { Title = "Tab _2" };
        tab2.Add (new TextField { Text = "TextField in Tab2", Width = 15 });

        Tab tab3 = new () { Title = "Tab T_hree" };
        tab3.Add (new Label { Text = "Label in Tab3" });

        Add (tab1, tab2, tab3);
        SelectedTabIndex = 0;

        return true;
    }

    private bool? SelectPreviousTab ()
    {
        IReadOnlyList<Tab> tabs = Tabs;

        if (tabs.Count == 0)
        {
            return false;
        }

        if (!_selectedTabIndex.HasValue || _selectedTabIndex.Value <= 0)
        {
            SelectedTabIndex = tabs.Count - 1;
        }
        else
        {
            SelectedTabIndex = _selectedTabIndex.Value - 1;
        }

        return true;
    }

    private bool? SelectNextTab ()
    {
        IReadOnlyList<Tab> tabs = Tabs;

        if (tabs.Count == 0)
        {
            return false;
        }

        if (!_selectedTabIndex.HasValue || _selectedTabIndex.Value >= tabs.Count - 1)
        {
            SelectedTabIndex = 0;
        }
        else
        {
            SelectedTabIndex = _selectedTabIndex.Value + 1;
        }

        return true;
    }

    private bool? SelectFirstTab ()
    {
        if (Tabs.Count == 0)
        {
            return false;
        }

        SelectedTabIndex = 0;

        return true;
    }

    private bool? SelectLastTab ()
    {
        IReadOnlyList<Tab> tabs = Tabs;

        if (tabs.Count == 0)
        {
            return false;
        }

        SelectedTabIndex = tabs.Count - 1;

        return true;
    }
}

namespace Terminal.Gui.Views;

/// <summary>
///     Internal view that renders tab headers for a <see cref="TabView"/>.
///     Lives in TabView's Padding adornment.
/// </summary>
internal class TabRow : View
{
    internal TabRow ()
    {
#if DEBUG
        Id = "TabRow";
#endif
        CanFocus = false;
        Width = Dim.Fill ();
        Height = 2;
        SuperViewRendersLineCanvas = true;
    }

    /// <summary>Gets the owning TabView by navigating: TabRow -> Padding (SuperView) -> TabView (Parent).</summary>
    private TabView? TabView => (SuperView as Adornment)?.Parent as TabView;

    /// <summary>Rebuilds tab header views from the current set of Tabs.</summary>
    internal void RebuildHeaders ()
    {
        RemoveAll ();

        TabView? tabView = TabView;

        if (tabView is null)
        {
            return;
        }

        IReadOnlyList<Tab> tabs = tabView.Tabs;
        View? previous = null;

        for (int i = 0; i < tabs.Count; i++)
        {
            int tabIndex = i;
            Tab tab = tabs [i];

            View header = new ()
            {
#if DEBUG
                Id = $"TabHeader_{tab.Title}",
#endif
                Title = tab.Title,
                BorderStyle = LineStyle.Rounded,
                SuperViewRendersLineCanvas = true,
                Width = Dim.Auto (DimAutoStyle.Text),
                Height = 2,
                CanFocus = false,
            };

            // Position: first header at X=0, others adjacent (overlapping by 1 for shared edges)
            if (previous is null)
            {
                header.X = 0;
            }
            else
            {
                header.X = Pos.Right (previous) - 1;
            }

            header.MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Activate);

            header.Accepting += (_, _) =>
                                {
                                    tabView.SelectedTabIndex = tabIndex;
                                };

            Add (header);
            previous = header;
        }

        UpdateHeaderAppearance ();
    }

    /// <summary>Updates tab header borders based on which tab is selected.</summary>
    internal void UpdateHeaderAppearance ()
    {
        TabView? tabView = TabView;

        if (tabView is null)
        {
            return;
        }

        IReadOnlyList<Tab> tabs = tabView.Tabs;
        int? selectedIndex = tabView.SelectedTabIndex;
        bool tabsOnBottom = tabView.TabsOnBottom;
        View [] headers = [.. SubViews];

        for (int i = 0; i < headers.Length && i < tabs.Count; i++)
        {
            View header = headers [i];

            if (i == selectedIndex)
            {
                // Selected tab: open bottom (tabs on top) or open top (tabs on bottom)
                header.Border!.Thickness = tabsOnBottom
                                               ? new Thickness (1, 0, 1, 1)
                                               : new Thickness (1, 1, 1, 0);
            }
            else
            {
                // Unselected tab: full border
                header.Border!.Thickness = new Thickness (1);
            }
        }

        // Update the TabView's border gaps to create the opening under/over the selected tab
        UpdateBorderGaps ();

        SetNeedsLayout ();
    }

    /// <summary>
    ///     Updates the TabView's Border gaps so the selected tab visually connects to the content area.
    /// </summary>
    private void UpdateBorderGaps ()
    {
        TabView? tabView = TabView;

        if (tabView is null)
        {
            return;
        }

        tabView.Border!.ClearAllGaps ();

        int? selectedIndex = tabView.SelectedTabIndex;

        if (!selectedIndex.HasValue)
        {
            return;
        }

        View [] headers = [.. SubViews];

        if (selectedIndex.Value >= headers.Length)
        {
            return;
        }

        View selectedHeader = headers [selectedIndex.Value];

        // The gap position is relative to the TabView border rectangle
        // The header's Frame.X is relative to TabRow, and TabRow is in Padding
        // We need the position relative to the border
        int gapPosition = selectedHeader.Frame.X + 1; // +1 to skip the header's left border
        int gapLength = Math.Max (0, selectedHeader.Frame.Width - 2); // Exclude the header's left/right borders

        if (gapLength <= 0)
        {
            return;
        }

        BorderGap gap = new (gapPosition, gapLength);

        if (tabView.TabsOnBottom)
        {
            tabView.Border.BottomGaps.Add (gap);
        }
        else
        {
            tabView.Border.TopGaps.Add (gap);
        }
    }
}

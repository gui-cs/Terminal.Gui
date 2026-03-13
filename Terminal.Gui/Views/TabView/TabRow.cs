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
        CanFocus = true;
        TabStop = TabBehavior.TabStop;

        // Extend past the Padding bounds to overlap with the Border's left/right columns
        X = -1;
        Width = Dim.Fill (-1);
        Height = 3;
        base.SuperViewRendersLineCanvas = true;
    }

    /// <summary>Gets the owning TabView by navigating: TabRow -> Padding (SuperView) -> TabView (Parent).</summary>
    private TabView? TabView => (SuperView as Adornment)?.Parent as TabView;

    /// <summary>Rebuilds tab header views from the current set of Tabs.</summary>
    internal void RebuildHeaders ()
    {
        // Dispose old headers before removing them — they are owned by TabRow
        View [] oldHeaders = [.. SubViews];

        for (int i = oldHeaders.Length - 1; i >= 0; i--)
        {
            Remove (oldHeaders [i]);
            oldHeaders [i].Dispose ();
        }

        TabView? tabView = TabView;

        if (tabView is null)
        {
            return;
        }

        IReadOnlyList<Tab> tabs = tabView.Tabs;
        View? previous = null;

        for (var i = 0; i < tabs.Count; i++)
        {
            int tabIndex = i;
            Tab tab = tabs [i];

            View header = new ()
            {
#if DEBUG
                Id = $"TabHeader_{tab.Title}",
#endif
                TabStop = TabBehavior.TabStop,
                Title = tab.Title,
                Text = tab.Title,
                BorderStyle = LineStyle.Rounded,
                SuperViewRendersLineCanvas = true,
                Width = Dim.Auto (DimAutoStyle.Text, maximumContentDim: (int)tabView.MaxTabTextWidth),
                Height = 3,
                CanFocus = true
            };

            header.GettingAttributeForRole += (sender, args) =>
                                              {
                                                  if (args.Role == VisualRole.HotNormal && tabView.SubViews.ElementAt (tabIndex).HasFocus)
                                                  {
                                                      args.Handled = true;
                                                      args.Result = header.GetAttributeForRole (VisualRole.HotFocus);
                                                  }
                                              };

            // Disable title rendering in the border — we use Text, not Title
            header.Border!.Settings &= ~BorderSettings.Title;

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

            header.Activating += (_, _) => { tabView.SelectedTabIndex = tabIndex; };

            header.HasFocusChanged += (_, args) =>
                                     {
                                         if (args.NewValue && tabView.SelectedTabIndex != tabIndex)
                                         {
                                             //tabView.SelectedTabIndex = tabIndex;
                                         }
                                     };

            Add (header);
            previous = header;
        }

        UpdateHeaderAppearance ();
        UpdateContentSizeForScrolling ();
    }

    /// <inheritdoc />
    protected override bool OnAdvancingFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        return base.OnAdvancingFocus (direction, behavior);
    }

    /// <inheritdoc />
    protected override void OnFocusedChanged (View? previousFocused, View? focused)
    {
        base.OnFocusedChanged (previousFocused, focused);
    }

    /// <summary>
    ///     Sets the ContentSize to the total width of all tab headers and enables the horizontal
    ///     scrollbar when headers overflow the viewport.
    /// </summary>
    internal void UpdateContentSizeForScrolling ()
    {
        TabView? tabView = TabView;

        if (tabView is null)
        {
            return;
        }

        IReadOnlyList<Tab> tabs = tabView.Tabs;

        if (tabs.Count == 0)
        {
            SetContentSize (null);

            return;
        }

        // Compute total header width: first header full width, each subsequent overlaps by 1
        int totalWidth = 0;

        for (var i = 0; i < tabs.Count; i++)
        {
            int textCols = tabs [i].Title.GetColumns ();
            int displayCols = (int)Math.Min (textCols, tabView.MaxTabTextWidth);
            int headerWidth = displayCols + 2; // +2 for left/right borders
            totalWidth += i == 0 ? headerWidth : headerWidth - 1;
        }

        // Set content size so the layout engine knows how wide the content really is
        SetContentSize (new Size (totalWidth, 3));
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
        bool tabsOnBottom = tabView.TabSide == Side.Bottom;
        View [] headers = [.. SubViews];

        for (var i = 0; i < headers.Length && i < tabs.Count; i++)
        {
            View header = headers [i];

            if (i == selectedIndex)
            {
                // Selected tab: open bottom (tabs on top) or open top (tabs on bottom)
                header.Border!.Thickness = tabsOnBottom ? new Thickness (1, 0, 1, 1) : new Thickness (1, 1, 1, 0);

                // When TabsOnBottom, Border.Top=0 shifts content to row 0, colliding with
                // the continuation line. Compensate with Padding.Top=1 to push text to row 1.
                header.Padding!.Thickness = tabsOnBottom ? new Thickness (0, 1, 0, 0) : new Thickness (0);
            }
            else
            {
                // Unselected tab: full border
                header.Border!.Thickness = new Thickness (1);
                header.Padding!.Thickness = new Thickness (0);
            }
        }

        // Update the TabView's border gaps
        UpdateBorderGaps ();

        SetNeedsLayout ();
    }

    /// <summary>
    ///     Scrolls the viewport so the tab header at the given index is visible.
    /// </summary>
    internal void EnsureHeaderVisible (int tabIndex)
    {
        TabView? tabView = TabView;

        if (tabView is null)
        {
            return;
        }

        IReadOnlyList<Tab> tabs = tabView.Tabs;

        if (tabIndex < 0 || tabIndex >= tabs.Count)
        {
            return;
        }

        // Compute the X position and width of the target header
        int headerX = 0;

        for (var i = 0; i < tabIndex; i++)
        {
            int textCols = tabs [i].Title.GetColumns ();
            int displayCols = (int)Math.Min (textCols, tabView.MaxTabTextWidth);
            int headerWidth = displayCols + 2;
            headerX += i == 0 ? headerWidth : headerWidth - 1;
        }

        int targetTextCols = tabs [tabIndex].Title.GetColumns ();
        int targetDisplayCols = (int)Math.Min (targetTextCols, tabView.MaxTabTextWidth);
        int targetWidth = targetDisplayCols + 2;

        // Scroll left if the header is before the viewport
        if (headerX < Viewport.X)
        {
            Viewport = Viewport with { X = headerX };
        }
        // Scroll right if the header extends past the viewport
        else if (headerX + targetWidth > Viewport.X + Viewport.Width)
        {
            Viewport = Viewport with { X = headerX + targetWidth - Viewport.Width };
        }
    }

    /// <summary>
    ///     Updates the TabView's Border gaps so the right (or left) border is suppressed in the tab header area,
    ///     and draws the continuation line from the last tab to the edge of the view.
    /// </summary>
    internal void UpdateBorderGaps ()
    {
        TabView? tabView = TabView;

        if (tabView is null)
        {
            return;
        }

        tabView.Border!.ClearAllGaps ();

        View [] headers = [.. SubViews];

        if (headers.Length == 0)
        {
            return;
        }

        bool tabsOnBottom = tabView.TabSide == Side.Bottom;

        // Suppress the right border above (or below) the continuation line.
        // The Border draws segmented lines around gaps, so at the continuation row
        // the right line segment starts fresh (only "down"), and auto-join with the
        // horizontal continuation line produces ╮ (not ┤).
        if (tabsOnBottom)
        {
            tabView.Border.RightGaps.Add (new BorderGap (tabView.Border.Frame.Height - 2, 2));
        }
        else
        {
            tabView.Border.RightGaps.Add (new BorderGap (0, 2));
        }
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        TabView? tabView = TabView;

        if (tabView is null)
        {
            return base.OnDrawingContent (context);
        }

        View [] headers = [.. SubViews];

        if (headers.Length == 0)
        {
            return base.OnDrawingContent (context);
        }

        bool tabsOnBottom = tabView.TabSide == Side.Bottom;

        // Draw the continuation line from the last tab header to the right edge of the view.
        // This line forms the top (or bottom) boundary of the content area where there are no tabs.
        View lastHeader = headers [^1];
        int lineY = tabsOnBottom ? 0 : Frame.Height - 1;

        // Use the TabView's border rectangle for precise screen coordinates
        Rectangle borderBounds = tabView.Border!.GetBorderRectangle ();
        int screenRightX = borderBounds.X + borderBounds.Width - 1;

        // The start position overlaps with the last header's right border
        Point screenStart = ViewportToScreen (new Point (lastHeader.Frame.Right - 1, lineY));
        int lineLength = screenRightX - screenStart.X + 1;

        if (lineLength > 0)
        {
            // The horizontal continuation line auto-joins with the Border's segmented right line
            // at the endpoint to produce the corner junction (e.g., ╮ for tabs on top).
            LineCanvas.AddLine (screenStart, lineLength, Orientation.Horizontal, tabView.BorderStyle);
        }

        return base.OnDrawingContent (context);
    }
}

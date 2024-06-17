namespace Terminal.Gui;

/// <summary>
///     Provides a horizontally or vertically oriented container for <see cref="Shortcut"/>s to be used as a menu, toolbar, or status
///     bar.
/// </summary>
/// <remarks>
///     <para>
///         Any <see cref="View"/> can be added to a <see cref="Bar"/>. However, the <see cref="Bar"/> is designed to work with
///         <see cref="Shortcut"/> objects. The <see cref="Shortcut"/> class provides a way to display a command, help, and key and
///         align them in a specific order.
///     </para>
/// </remarks>
public class Bar : View
{
    /// <inheritdoc/>
    public Bar () : this ([]) { }

    /// <inheritdoc/>
    public Bar (IEnumerable<Shortcut> shortcuts)
    {
        CanFocus = true;

        Width = Dim.Auto ();
        Height = Dim.Auto ();

        LayoutStarted += Bar_LayoutStarted;
        Initialized += Bar_Initialized;

        if (shortcuts is null)
        {
            return;
        }

        foreach (Shortcut shortcut in shortcuts)
        {
            Add (shortcut);
        }
    }

    private void Bar_Initialized (object sender, EventArgs e) { ColorScheme = Colors.ColorSchemes ["Menu"]; }

    /// <inheritdoc/>
    public override void SetBorderStyle (LineStyle value)
    {
        // The default changes the thickness. We don't want that. We just set the style.
        Border.LineStyle = value;
    }

    private Orientation _orientation = Orientation.Horizontal;

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/> for this <see cref="Bar"/>. The default is
    ///     <see cref="Orientation.Horizontal"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Horizontal orientation arranges the command, help, and key parts of each <see cref="Shortcut"/>s from right to left
    ///         Vertical orientation arranges the command, help, and key parts of each <see cref="Shortcut"/>s from left to right.
    ///     </para>
    /// </remarks>
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;
            SetNeedsLayout ();
        }
    }

    private AlignmentModes _alignmentModes = AlignmentModes.StartToEnd;

    /// <summary>
    ///     Gets or sets the <see cref="AlignmentModes"/> for this <see cref="Bar"/>. The default is
    ///     <see cref="AlignmentModes.StartToEnd"/>.
    /// </summary>
    public AlignmentModes AlignmentModes
    {
        get => _alignmentModes;
        set
        {
            _alignmentModes = value;
            SetNeedsLayout ();
        }
    }

    // TODO: Move this to View
    /// <summary>Inserts a <see cref="Shortcut"/> in the specified index of <see cref="View.Subviews"/>.</summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void AddShortcutAt (int index, Shortcut item)
    {
        List<View> savedSubViewList = Subviews.ToList ();
        int count = savedSubViewList.Count;
        RemoveAll ();

        for (var i = 0; i <= count; i++)
        {
            if (i == index)
            {
                Add (item);
            }

            if (i < count)
            {
                Add (savedSubViewList [i]);
            }
        }

        SetNeedsDisplay ();
    }

    // TODO: Move this to View

    /// <summary>Removes a <see cref="Shortcut"/> at specified index of <see cref="View.Subviews"/>.</summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <returns>The <see cref="Shortcut"/> removed.</returns>
    public Shortcut RemoveShortcut (int index)
    {
        View toRemove = null;

        for (var i = 0; i < Subviews.Count; i++)
        {
            if (i == index)
            {
                toRemove = Subviews [i];
            }
        }

        if (toRemove is { })
        {
            Remove (toRemove);
            SetNeedsDisplay ();
        }

        return toRemove as Shortcut;
    }

    private void Bar_LayoutStarted (object sender, LayoutEventArgs e)
    {
        View prevBarItem = null;

        switch (Orientation)
        {
            case Orientation.Horizontal:
                for (var index = 0; index < Subviews.Count; index++)
                {
                    View barItem = Subviews [index];

                    barItem.ColorScheme = ColorScheme;
                    barItem.X = Pos.Align (Alignment.Start, AlignmentModes);
                    barItem.Y = 0; //Pos.Center ();

                    // HACK: This should not be needed
                    barItem.SetRelativeLayout (GetContentSize ());
                }

                break;

            case Orientation.Vertical:
                // CommandView is aligned left, HelpView is aligned right, KeyView is aligned right
                // All CommandView's are the same width, all HelpView's are the same width,
                // all KeyView's are the same width

                var minKeyWidth = 0;

                List<Shortcut> shortcuts = Subviews.Where (s => s is Shortcut && s.Visible).Cast<Shortcut> ().ToList ();

                foreach (Shortcut shortcut in shortcuts)
                {
                    // Let AutoSize do its thing to get the minimum width of each CommandView and HelpView
                    //shortcut.CommandView.SetRelativeLayout (new Size (int.MaxValue, int.MaxValue));
                    minKeyWidth = int.Max (minKeyWidth, shortcut.KeyView.Text.GetColumns ());
                }

                // Set the overall size of the Bar and arrange the views vertically
                var maxBarItemWidth = 0;
                var totalHeight = 0;

                for (var index = 0; index < Subviews.Count; index++)
                {
                    View barItem = Subviews [index];

                    if (barItem is Shortcut scBarItem)
                    {
                        scBarItem.MinimumKeyViewSize = minKeyWidth;
                    }

                    if (!barItem.Visible)
                    {
                        continue;
                    }

                    if (prevBarItem == null)
                    {
                        barItem.Y = 0;
                    }
                    else
                    {
                        // Align the view to the bottom of the previous view
                        barItem.Y = Pos.Bottom (prevBarItem);
                    }

                    prevBarItem = barItem;

                    if (barItem is Shortcut shortcut)
                    {
                        maxBarItemWidth = Math.Max (maxBarItemWidth, shortcut.Frame.Width);
                    }
                    else
                    {
                        maxBarItemWidth = Math.Max (maxBarItemWidth, barItem.Frame.Width);
                    }

                    barItem.X = 0;
                    totalHeight += barItem.Frame.Height;
                }

                foreach (Shortcut shortcut in shortcuts)
                {
                    shortcut.Width = maxBarItemWidth;
                }

                Height = Dim.Auto (DimAutoStyle.Content, totalHeight);

                break;
        }
    }
}

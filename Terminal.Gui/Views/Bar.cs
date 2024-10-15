#nullable enable
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
public class Bar : View, IOrientation, IDesignable
{
    private readonly OrientationHelper _orientationHelper;

    /// <inheritdoc/>
    public Bar () : this ([]) { }

    /// <inheritdoc/>
    public Bar (IEnumerable<Shortcut> shortcuts)
    {
        CanFocus = true;

        Width = Dim.Auto ();
        Height = Dim.Auto ();

        _orientationHelper = new (this);
        _orientationHelper.OrientationChanging += (sender, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (sender, e) => OrientationChanged?.Invoke (this, e);

        Initialized += Bar_Initialized;
        MouseEvent += OnMouseEvent;

        if (shortcuts is null)
        {
            return;
        }

        foreach (Shortcut shortcut in shortcuts)
        {
            Add (shortcut);
        }
    }

    private void OnMouseEvent (object? sender, MouseEventArgs e)
    {
        NavigationDirection direction = NavigationDirection.Backward;

        if (e.Flags == MouseFlags.WheeledDown)
        {
            e.Handled = true;
        }

        if (e.Flags == MouseFlags.WheeledUp)
        {
            direction = NavigationDirection.Forward;
            e.Handled = true;
        }

        if (e.Flags == MouseFlags.WheeledRight)
        {
            e.Handled = true;
        }

        if (e.Flags == MouseFlags.WheeledLeft)
        {
            direction = NavigationDirection.Forward;
            e.Handled = true;
        }

        if (e.Handled)
        {
            e.Handled = AdvanceFocus (direction, TabBehavior.TabStop);
        }
    }

    private void Bar_Initialized (object? sender, EventArgs e)
    {
        ColorScheme = Colors.ColorSchemes ["Menu"];
        LayoutBarItems (GetContentSize ());
    }

    /// <inheritdoc/>
    public override void SetBorderStyle (LineStyle value)
    {
        // The default changes the thickness. We don't want that. We just set the style.
        Border.LineStyle = value;
    }

    #region IOrientation members

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
        get => _orientationHelper.Orientation;
        set => _orientationHelper.Orientation = value;
    }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        SetNeedsLayout ();
    }
    #endregion

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
    public Shortcut? RemoveShortcut (int index)
    {
        View? toRemove = null;

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

    /// <inheritdoc />
    internal override void OnLayoutStarted (LayoutEventArgs args)
    {
        base.OnLayoutStarted (args);

        LayoutBarItems (args.OldContentSize);
    }

    private void LayoutBarItems (Size contentSize)
    {
        View? prevBarItem = null;

        switch (Orientation)
        {
            case Orientation.Horizontal:
                for (var index = 0; index < Subviews.Count; index++)
                {
                    View barItem = Subviews [index];

                    barItem.ColorScheme = ColorScheme;
                    barItem.X = Pos.Align (Alignment.Start, AlignmentModes);
                    barItem.Y = 0; //Pos.Center ();
                }
                break;

            case Orientation.Vertical:
                // Set the overall size of the Bar and arrange the views vertically

                var minKeyWidth = 0;

                List<Shortcut> shortcuts = Subviews.Where (s => s is Shortcut && s.Visible).Cast<Shortcut> ().ToList ();
                foreach (Shortcut shortcut in shortcuts)
                {
                    // Let DimAuto do its thing to get the minimum width of each CommandView and HelpView
                    //shortcut.CommandView.SetRelativeLayout (new Size (int.MaxValue, int.MaxValue));
                    minKeyWidth = int.Max (minKeyWidth, shortcut.KeyView.Text.GetColumns ());
                }

                var maxBarItemWidth = 0;
                var totalHeight = 0;

                for (var index = 0; index < Subviews.Count; index++)
                {
                    View barItem = Subviews [index];

                    barItem.ColorScheme = ColorScheme;

                    if (!barItem.Visible)
                    {
                        continue;
                    }

                    if (barItem is Shortcut scBarItem)
                    {
                        scBarItem.MinimumKeyTextSize = minKeyWidth;
                        maxBarItemWidth = Math.Max (maxBarItemWidth, scBarItem.Frame.Width);
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

                    barItem.X = 0;
                    totalHeight += barItem.Frame.Height;
                }


                foreach (View barItem in Subviews)
                {
                    barItem.Width = maxBarItemWidth;
                }

                Height = Dim.Auto (DimAutoStyle.Content, totalHeight);

                break;
        }
    }

    /// <inheritdoc />
    public bool EnableForDesign ()
    {
        var shortcut = new Shortcut
        {
            Text = "Quit",
            Title = "Q_uit",
            Key = Key.Z.WithCtrl,
        };

        Add (shortcut);

        shortcut = new Shortcut
        {
            Text = "Help Text",
            Title = "Help",
            Key = Key.F1,
        };

        Add (shortcut);

        shortcut = new Shortcut
        {
            Text = "Czech",
            CommandView = new CheckBox ()
            {
                Title = "_Check"
            },
            Key = Key.F9,
            CanFocus = false
        };

        Add (shortcut);

        return true;
    }
}

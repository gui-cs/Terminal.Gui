#nullable enable

namespace Terminal.Gui.Views;

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
    public Bar (IEnumerable<View>? shortcuts)
    {
        CanFocus = true;

        Width = Dim.Auto ();
        Height = Dim.Auto ();

        _orientationHelper = new (this);

        // Initialized += Bar_Initialized;
        MouseEvent += OnMouseEvent;


        if (shortcuts is { })
        {
            foreach (View shortcut in shortcuts)
            {
                Add (shortcut);
            }
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

#pragma warning disable CS0067 // The event is never used
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
#pragma warning restore CS0067 // The event is never used

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        // BUGBUG: this should not be SuperView.GetContentSize
        LayoutBarItems (SuperView?.GetContentSize () ?? Application.Screen.Size);
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
            //SetNeedsDraw ();
            SetNeedsLayout ();
        }
    }

    // TODO: Move this to View
    /// <summary>Inserts a <see cref="Shortcut"/> in the specified index of <see cref="View.SubViews"/>.</summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void AddShortcutAt (int index, Shortcut item)
    {
        List<View> savedSubViewList = SubViews.ToList ();
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

        //SetNeedsDraw ();
        SetNeedsLayout ();
    }

    // TODO: Move this to View

    /// <summary>Removes a <see cref="Shortcut"/> at specified index of <see cref="View.SubViews"/>.</summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <returns>The <see cref="Shortcut"/> removed.</returns>
    public Shortcut? RemoveShortcut (int index)
    {
        View? toRemove = null;

        for (var i = 0; i < SubViews.Count; i++)
        {
            if (i == index)
            {
                toRemove = SubViews.ElementAt (i);
            }
        }

        if (toRemove is { })
        {
            Remove (toRemove);
            //SetNeedsDraw ();
            SetNeedsLayout ();
        }

        return toRemove as Shortcut;
    }

    /// <inheritdoc />
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        LayoutBarItems (args.OldContentSize);
    }

    private void LayoutBarItems (Size contentSize)
    {
        View? prevBarItem = null;

        switch (Orientation)
        {
            case Orientation.Horizontal:
                for (var index = 0; index < SubViews.Count; index++)
                {
                    View barItem = SubViews.ElementAt (index);

                    //barItem.Scheme = Scheme;
                    barItem.X = Pos.Align (Alignment.Start, AlignmentModes);
                    barItem.Y = 0; //Pos.Center ();

                    if (barItem is Shortcut sc)
                    {
                        sc.Width = sc.GetWidthDimAuto ();
                    }
                }

                break;

            case Orientation.Vertical:
                if (Width!.Has<DimAuto> (out _))
                {
                    // Set the overall size of the Bar and arrange the views vertically

                    var minKeyWidth = 0;

                    List<Shortcut> shortcuts = SubViews.OfType<Shortcut> ().Where (s => s.Visible).ToList ();

                    foreach (Shortcut shortcut in shortcuts)
                    {
                        // Get the largest width of all KeyView's
                        minKeyWidth = int.Max (minKeyWidth, shortcut.KeyView.Text.GetColumns ());
                    }

                    var maxBarItemWidth = 0;

                    for (var index = 0; index < SubViews.Count; index++)
                    {
                        View barItem = SubViews.ElementAt (index);


                       // barItem.Scheme = Scheme;

                        if (!barItem.Visible)
                        {
                            continue;
                        }

                        if (barItem is Shortcut scBarItem)
                        {
                            barItem.X = 0;
                            scBarItem.MinimumKeyTextSize = minKeyWidth;
                            scBarItem.Width = scBarItem.GetWidthDimAuto ();
                            barItem.Layout (Application.Screen.Size);
                            maxBarItemWidth = Math.Max (maxBarItemWidth, barItem.Frame.Width);
                        }

                        if (prevBarItem == null)
                        {
                            // TODO: Just use Pos.Align!
                            barItem.Y = 0;
                        }
                        else
                        {
                            // TODO: Just use Pos.Align!
                            // Align the view to the bottom of the previous view
                            barItem.Y = Pos.Bottom (prevBarItem);
                        }

                        prevBarItem = barItem;

                    }

                    foreach (var subView in SubViews)
                    {
                        if (subView is not Line)
                        {
                            subView.Width = Dim.Auto (DimAutoStyle.Auto, minimumContentDim: maxBarItemWidth, maximumContentDim: maxBarItemWidth);
                        }
                    }
                }
                else
                {
                    foreach (var subView in SubViews)
                    {
                        if (subView is not Line)
                        {
                            subView.Width = Dim.Fill ();
                        }
                    }
                }

                break;
        }
    }

    /// <inheritdoc />
    public virtual bool EnableForDesign ()
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

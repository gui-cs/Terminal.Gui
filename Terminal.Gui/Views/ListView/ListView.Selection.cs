namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>
    ///     If set to <see langword="true"/> more than one item can be selected. If <see langword="false"/> selecting an
    ///     item will cause all others to be un-selected. The default is <see langword="false"/>.
    /// </summary>
    public bool AllowsMultipleSelection
    {
        get;
        set
        {
            field = value;

            if (!field)
            {
                // Clear multi-selection tracking
                MultiSelectedItems.Clear ();

                if (Source is { })
                {
                    // Clear all marks except selected (existing behavior)
                    for (var i = 0; i < Source.Count; i++)
                    {
                        if (Source.IsMarked (i) && SelectedItem.HasValue && i != SelectedItem.Value)
                        {
                            Source.SetMark (i, false);
                        }
                    }
                }
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>Ensures the selected item is always visible on the screen.</summary>
    public void EnsureSelectedItemVisible ()
    {
        if (SelectedItem is null)
        {
            return;
        }

        if (SelectedItem < Viewport.Y)
        {
            Viewport = Viewport with { Y = SelectedItem.Value };
        }
        else if (Viewport.Height > 0 && SelectedItem >= Viewport.Y + Viewport.Height)
        {
            Viewport = Viewport with { Y = SelectedItem.Value - Viewport.Height + 1 };
        }
    }

    /// <summary>
    ///     Gets all selected item indices, including both <see cref="SelectedItem"/> and all items
    ///     in <see cref="MultiSelectedItems"/>.
    /// </summary>
    /// <returns>An enumerable of selected item indices in ascending order.</returns>
    public IEnumerable<int> GetAllSelectedItems ()
    {
        HashSet<int> all = [..MultiSelectedItems];

        if (SelectedItem.HasValue)
        {
            all.Add (SelectedItem.Value);
        }

        return all.OrderBy (i => i);
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the specified item is selected, either as <see cref="SelectedItem"/>
    ///     or in <see cref="MultiSelectedItems"/>.
    /// </summary>
    /// <param name="item">The item index to check.</param>
    /// <returns><see langword="true"/> if the item is selected; otherwise <see langword="false"/>.</returns>
    public bool IsSelected (int item) => item == SelectedItem || MultiSelectedItems.Contains (item);

    /// <summary>Marks the <see cref="SelectedItem"/> if it is not already marked.</summary>
    /// <returns><see langword="true"/> if the <see cref="SelectedItem"/> was marked.</returns>
    public bool MarkUnmarkSelectedItem ()
    {
        if (!AllowsMarking || Source is null || SelectedItem is null || !UnmarkAllButSelected ())
        {
            return false;
        }

        Source.SetMark (SelectedItem.Value, !Source.IsMarked (SelectedItem.Value));
        SetNeedsDraw ();

        return true;
    }

    /// <summary>
    ///     Selects all items when <see cref="AllowsMultipleSelection"/> is <see langword="true"/>.
    ///     All items are added to <see cref="MultiSelectedItems"/>.
    /// </summary>
    public void SelectAll ()
    {
        if (!AllowsMultipleSelection || Source is null)
        {
            return;
        }

        MultiSelectedItems.Clear ();

        for (var i = 0; i < Source.Count; i++)
        {
            MultiSelectedItems.Add (i);
        }

        SetNeedsDraw ();
    }

    /// <summary>
    ///     Sets the selected item, optionally extending the selection to create a range.
    /// </summary>
    /// <param name="item">The item index to select.</param>
    /// <param name="extendExistingSelection">
    ///     If <see langword="true"/> and <see cref="AllowsMultipleSelection"/> is enabled,
    ///     extends the selection from the anchor point to <paramref name="item"/>.
    /// </param>
    public void SetSelection (int item, bool extendExistingSelection)
    {
        if (Source is null || item < 0 || item >= Source.Count)
        {
            return;
        }

        if (!AllowsMultipleSelection || !extendExistingSelection)
        {
            // Clear multi-selection and set new anchor
            MultiSelectedItems.Clear ();
            _selectionAnchor = item;
        }
        else if (extendExistingSelection && _selectionAnchor.HasValue)
        {
            // Create range from anchor to item
            MultiSelectedItems.Clear ();
            int start = Math.Min (_selectionAnchor.Value, item);
            int end = Math.Max (_selectionAnchor.Value, item);

            for (int i = start; i <= end; i++)
            {
                MultiSelectedItems.Add (i);
            }

            // Note: anchor stays at original position

            if (AllowsMarking)
            {
                // Update marks to match multi-selection
                for (var i = 0; i < Source.Count; i++)
                {
                    Source.SetMark (i, MultiSelectedItems.Contains (i));
                }
            }
        }

        SelectedItem = item;
        EnsureSelectedItemVisible ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Clears all multi-selection, keeping only <see cref="SelectedItem"/>.
    /// </summary>
    public void UnselectAll ()
    {
        MultiSelectedItems.Clear ();
        SetNeedsDraw ();
    }

    /// <summary>Invokes the <see cref="OpenSelectedItem"/> event if it is defined.</summary>
    /// <returns><see langword="true"/> if the <see cref="OpenSelectedItem"/> event was fired.</returns>
    public bool OnOpenSelectedItem ()
    {
        if (Source is null || SelectedItem is null || Source.Count <= SelectedItem || SelectedItem < 0 || OpenSelectedItem is null)
        {
            return false;
        }

        object? value = Source.ToList () [SelectedItem.Value];
        OpenSelectedItem?.Invoke (this, new ListViewItemEventArgs (SelectedItem.Value, value!));

        // BUGBUG: this should not blindly return true.
        return true;
    }

    private int? _lastSelectedItem;
    private int? _selectionAnchor;

    /// <summary>
    ///     When <see cref="AllowsMultipleSelection"/> is enabled, contains the indices of all selected items.
    ///     This is independent of <see cref="AllowsMarking"/> and provides selection tracking without visual marking.
    /// </summary>
    public HashSet<int> MultiSelectedItems { get; } = [];

    /// <summary>Gets or sets the index of the currently selected item.</summary>
    /// <value>The selected item or null if no item is selected.</value>
    public int? SelectedItem
    {
        get;
        set
        {
            if (Source is null)
            {
                return;
            }

            if (value.HasValue && (value < 0 || value >= Source.Count))
            {
                throw new ArgumentException (@"SelectedItem must be greater than 0 or less than the number of items.");
            }

            int? oldValue = field;

            if (oldValue == value)
            {
                return;
            }

            ValueChangingEventArgs<int?> changingArgs = new (oldValue, value);

            if (OnValueChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            ValueChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            field = value;
            SetNeedsDraw ();

            // Initialize selection anchor when item is selected
            if (MultiSelectedItems.Count == 0 && value.HasValue)
            {
                _selectionAnchor = value.Value;
            }

            if (SelectedItem != _lastSelectedItem)
            {
                _lastSelectedItem = SelectedItem;
                EnsureSelectedItemVisible ();
            }

            ValueChangedEventArgs<int?> changedArgs = new (oldValue, field);
            OnValueChanged (changedArgs);
            ValueChanged?.Invoke (this, changedArgs);
        }
    }

    /// <summary>
    ///     If <see cref="AllowsMarking"/> and <see cref="AllowsMultipleSelection"/> are both <see langword="true"/>,
    ///     unmarks all marked items other than <see cref="SelectedItem"/>.
    /// </summary>
    /// <returns><see langword="true"/> if unmarking was successful.</returns>
    public bool UnmarkAllButSelected ()
    {
        if (!AllowsMarking)
        {
            return false;
        }

        if (AllowsMultipleSelection)
        {
            return true;
        }

        for (var i = 0; i < Source?.Count; i++)
        {
            if (!Source.IsMarked (i) || i == SelectedItem)
            {
                continue;
            }
            Source.SetMark (i, false);

            return true;
        }

        return true;
    }
}

namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>
    ///     Gets or sets whether multiple items can be marked simultaneously.
    /// </summary>
    /// <value>
    ///     <see langword="true"/> to allow multiple marks (checkbox style);
    ///     <see langword="false"/> for single mark (radio button style).
    ///     The default is <see langword="false"/>.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         When <see langword="true"/>, marking one item does not clear others (checkbox behavior).
    ///         When <see langword="false"/>, marking one item clears all other marks (radio button behavior).
    ///     </para>
    ///     <para>
    ///         Marks can exist and be set regardless of the <see cref="ShowMarks"/> property value.
    ///         <see cref="ShowMarks"/> only controls whether mark glyphs are visually displayed.
    ///     </para>
    /// </remarks>
    public bool MarkMultiple
    {
        get;
        set
        {
            field = value;

            if (!field && Source is { })
            {
                // Clear all marks except selected
                for (var i = 0; i < Source.Count; i++)
                {
                    if (Source.IsMarked (i) && SelectedItem.HasValue && i != SelectedItem.Value)
                    {
                        Source.SetMark (i, false);
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
    ///     Gets all marked item indices from the data source.
    /// </summary>
    /// <returns>An enumerable of marked item indices in ascending order.</returns>
    public IEnumerable<int> GetAllMarkedItems ()
    {
        if (Source is null)
        {
            return [];
        }

        List<int> marked = [];

        for (var i = 0; i < Source.Count; i++)
        {
            if (Source.IsMarked (i))
            {
                marked.Add (i);
            }
        }

        return marked;
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the specified item is selected (is the <see cref="SelectedItem"/>)
    ///     or marked (via <see cref="IListDataSource.IsMarked"/>).
    /// </summary>
    /// <param name="item">The item index to check.</param>
    /// <returns><see langword="true"/> if the item is selected or marked; otherwise <see langword="false"/>.</returns>
    public bool IsSelected (int item) => item == SelectedItem || (Source?.IsMarked (item) ?? false);

    /// <summary>Marks the <see cref="SelectedItem"/> if it is not already marked.</summary>
    /// <returns><see langword="true"/> if the <see cref="SelectedItem"/> was marked.</returns>
    public bool MarkUnmarkSelectedItem ()
    {
        // Allow marking if ShowMarks OR MarkMultiple is true (not in Combination 1)
        if ((!ShowMarks && !MarkMultiple) || Source is null || SelectedItem is null || !UnmarkAllButSelected ())
        {
            return false;
        }

        Source.SetMark (SelectedItem.Value, !Source.IsMarked (SelectedItem.Value));
        SetNeedsDraw ();

        return true;
    }

    /// <summary>
    ///     Sets the selected item, optionally extending marking to create a range.
    /// </summary>
    /// <param name="item">The item index to select.</param>
    /// <param name="extendExistingSelection">
    ///     If <see langword="true"/> and <see cref="MarkMultiple"/> is enabled,
    ///     extends marking from the anchor point to <paramref name="item"/>.
    /// </param>
    public void SetSelection (int item, bool extendExistingSelection)
    {
        if (Source is null || item < 0 || item >= Source.Count)
        {
            return;
        }

        if (!MarkMultiple || !extendExistingSelection)
        {
            // Single-selection mode or not extending: just move SelectedItem
            if (!MarkMultiple && ShowMarks)
            {
                // Clear all marks except the new selection (radio button mode)
                for (var i = 0; i < Source.Count; i++)
                {
                    Source.SetMark (i, i == item);
                }
            }

            _selectionAnchor = item;
        }
        else if (extendExistingSelection && _selectionAnchor.HasValue && (ShowMarks || MarkMultiple))
        {
            // Multi-marking mode: mark range from anchor to item
            int start = Math.Min (_selectionAnchor.Value, item);
            int end = Math.Max (_selectionAnchor.Value, item);

            for (int i = start; i <= end; i++)
            {
                Source.SetMark (i, true);
            }

            // Note: anchor stays at original position
        }

        SelectedItem = item;
        EnsureSelectedItemVisible ();
        SetNeedsDraw ();
    }

    /// <summary>
    ///     Clears all marks in the data source.
    /// </summary>
    public void UnmarkAll ()
    {
        if (Source is null)
        {
            return;
        }

        for (var i = 0; i < Source.Count; i++)
        {
            Source.SetMark (i, false);
        }

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

            // Initialize selection anchor when no anchor is set
            if (!_selectionAnchor.HasValue && value.HasValue)
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
    ///     If marking is enabled (<see cref="ShowMarks"/> OR <see cref="MarkMultiple"/>)
    ///     and <see cref="MarkMultiple"/> is <see langword="false"/>,
    ///     unmarks all marked items other than <see cref="SelectedItem"/>.
    /// </summary>
    /// <returns><see langword="true"/> if unmarking was successful.</returns>
    public bool UnmarkAllButSelected ()
    {
        // Allow marking if ShowMarks OR MarkMultiple is true (not in Combination 1)
        if (!ShowMarks && !MarkMultiple)
        {
            return false;
        }

        if (MarkMultiple)
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

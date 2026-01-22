namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>Changes the <see cref="SelectedItem"/> to the next item in the list, scrolling the list if needed.</summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="AllowsMultipleSelection"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    public bool MoveDown (bool extend = false)
    {
        if (Source is null || Source.Count == 0)
        {
            return false; //Nothing for us to move to
        }

        int newItem;

        if (SelectedItem is null || SelectedItem >= Source.Count)
        {
            // If SelectedItem is null or for some reason we are currently outside the
            // valid values range, we should select the first or bottommost valid value.
            // This can occur if the backing data source changes.
            newItem = SelectedItem is null ? 0 : Source.Count - 1;
        }
        else if (SelectedItem + 1 < Source.Count)
        {
            // Can move down by one.
            newItem = SelectedItem.Value + 1;
        }
        else if (SelectedItem >= Viewport.Y + Viewport.Height)
        {
            // Just scroll viewport
            Viewport = Viewport with { Y = Source.Count - Viewport.Height };

            return true;
        }
        else
        {
            // Already at bottom
            return false;
        }

        SetSelection (newItem, extend);

        if (SelectedItem >= Viewport.Y + Viewport.Height)
        {
            Viewport = Viewport with { Y = Viewport.Y + 1 };
        }
        else if (SelectedItem < Viewport.Y)
        {
            Viewport = Viewport with { Y = SelectedItem!.Value };
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to last item in the list, scrolling the list if needed.</summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="AllowsMultipleSelection"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    public bool MoveEnd (bool extend = false)
    {
        if (Source is not { Count: > 0 } || SelectedItem == Source.Count - 1)
        {
            return true;
        }

        int newItem = Source.Count - 1;
        SetSelection (newItem, extend);

        if (Viewport.Y + SelectedItem > Viewport.Height - 1)
        {
            Viewport = Viewport with
            {
                Y = SelectedItem < Viewport.Height - 1
                        ? Math.Max (Viewport.Height - SelectedItem!.Value + 1, 0)
                        : Math.Max (SelectedItem!.Value - Viewport.Height + 1, 0)
            };
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the first item in the list, scrolling the list if needed.</summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="AllowsMultipleSelection"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    public bool MoveHome (bool extend = false)
    {
        if (SelectedItem == 0)
        {
            return true;
        }

        SetSelection (0, extend);
        Viewport = Viewport with { Y = SelectedItem!.Value };

        return true;
    }

    /// <summary>
    ///     Changes the <see cref="SelectedItem"/> to the item just below the bottom of the visible list, scrolling if
    ///     needed.
    /// </summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="AllowsMultipleSelection"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    public bool MovePageDown (bool extend = false)
    {
        if (Source is null || Source.Count == 0)
        {
            return false;
        }

        int newItem = (SelectedItem ?? 0) + Viewport.Height;

        if (newItem >= Source.Count)
        {
            newItem = Source.Count - 1;
        }

        if (newItem == SelectedItem)
        {
            return true;
        }

        SetSelection (newItem, extend);

        if (Source.Count >= Viewport.Height)
        {
            Viewport = Viewport with { Y = SelectedItem!.Value };
        }
        else
        {
            Viewport = Viewport with { Y = 0 };
        }

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the item at the top of the visible list.</summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="AllowsMultipleSelection"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    public bool MovePageUp (bool extend = false)
    {
        if (Source is null || Source.Count == 0)
        {
            return false;
        }

        int newItem = (SelectedItem ?? 0) - Viewport.Height;

        if (newItem < 0)
        {
            newItem = 0;
        }

        if (newItem == SelectedItem || !(newItem < Source?.Count))
        {
            return true;
        }

        SetSelection (newItem, extend);
        Viewport = Viewport with { Y = SelectedItem!.Value };

        return true;
    }

    /// <summary>Changes the <see cref="SelectedItem"/> to the previous item in the list, scrolling the list if needed.</summary>
    /// <param name="extend">
    ///     If <see langword="true"/> and <see cref="AllowsMultipleSelection"/> is enabled,
    ///     extends the selection from the anchor to the new item. If <see langword="false"/>,
    ///     clears any existing multi-selection.
    /// </param>
    /// <returns><see langword="true"/> if the selection was moved.</returns>
    public bool MoveUp (bool extend = false)
    {
        if (Source is null || Source.Count == 0)
        {
            return false; //Nothing for us to move to
        }

        int newItem;

        if (SelectedItem is null || SelectedItem >= Source.Count)
        {
            // If SelectedItem is null or for some reason we are currently outside the
            // valid values range, we should select the bottommost valid value.
            // This can occur if the backing data source changes.
            newItem = Source.Count - 1;
        }
        else if (SelectedItem > 0)
        {
            newItem = SelectedItem.Value - 1;

            if (newItem > Source.Count)
            {
                newItem = Source.Count - 1;
            }
        }
        else if (SelectedItem < Viewport.Y)
        {
            // Just scroll viewport
            Viewport = Viewport with { Y = SelectedItem.Value };

            return true;
        }
        else
        {
            // Already at top
            return false;
        }

        SetSelection (newItem, extend);

        if (SelectedItem < Viewport.Y)
        {
            Viewport = Viewport with { Y = SelectedItem!.Value };
        }
        else if (SelectedItem > Viewport.Y + Viewport.Height)
        {
            Viewport = Viewport with { Y = SelectedItem!.Value - Viewport.Height + 1 };
        }

        return true;
    }
}

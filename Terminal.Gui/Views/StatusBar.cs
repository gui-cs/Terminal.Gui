namespace Terminal.Gui;

/// <summary>
///     A status bar is a <see cref="View"/> that snaps to the bottom of a <see cref="Toplevel"/> displaying set of
///     <see cref="Shortcut"/>s. The <see cref="StatusBar"/> should be context sensitive. This means, if the main menu
///     and an open text editor are visible, the items probably shown will be ~F1~ Help ~F2~ Save ~F3~ Load. While a dialog
///     to ask a file to load is executed, the remaining commands will probably be ~F1~ Help. So for each context must be a
///     new instance of a status bar.
/// </summary>
public class StatusBar : Bar
{

    public StatusBar ()
    {
        Orientation = Orientation.Horizontal;
        Y = Pos.AnchorEnd ();
        Width = Dim.Fill ();
    }

    /// <inheritdoc />
    public override void Add (View view)
    {
        view.CanFocus = false;
        base.Add (view);
    }

    /// <summary>Inserts a <see cref="Shortcut"/> in the specified index of <see cref="Items"/>.</summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The item to insert.</param>
    public void AddShortcutAt (int index, Shortcut item)
    {
        List<View> savedSubViewList = Subviews.ToList ();
        int count = savedSubViewList.Count;
        RemoveAll ();
        for (int i = 0; i < count; i++)
        {
            if (i == index)
            {
                Add (item);
            }
            Add (savedSubViewList [i]);
        }
        SetNeedsDisplay ();
    }

    /// <summary>Removes a <see cref="Shortcut"/> at specified index of <see cref="Items"/>.</summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    /// <returns>The <see cref="Shortcut"/> removed.</returns>
    public Shortcut RemoveItem (int index)
    {
        View toRemove = null;
        for (int i = 0; i < Subviews.Count; i++)
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
}

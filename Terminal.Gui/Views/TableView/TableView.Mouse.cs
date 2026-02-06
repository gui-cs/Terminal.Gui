#nullable disable
namespace Terminal.Gui.Views;

/// <summary>
///     Displays and enables infinite scrolling through tabular data based on a <see cref="ITableSource"/>.
///     <a href="../docs/tableview.md">See the TableView Deep Dive for more</a>.
/// </summary>
public partial class TableView
{
    ///<inheritdoc/>
    protected override bool OnMouseEvent (Mouse me)
    {
        if (!me.Flags.HasFlag (MouseFlags.LeftButtonClicked)
            && !me.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked)
            && me.Flags != MouseFlags.WheeledDown
            && me.Flags != MouseFlags.WheeledUp
            && me.Flags != MouseFlags.WheeledLeft
            && me.Flags != MouseFlags.WheeledRight)
        {
            return false;
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        if (TableIsNullOrInvisible ())
        {
            return false;
        }

        // Scroll wheel flags
        switch (me.Flags)
        {
            case MouseFlags.WheeledDown:
                Viewport = Viewport with {Y = Viewport.Y + 1};
                EnsureValidScrollOffsets ();

                //SetNeedsDraw ();
                return true;

            case MouseFlags.WheeledUp:
                Viewport = Viewport with {Y = Viewport.Y - 1};
                EnsureValidScrollOffsets ();

                //SetNeedsDraw ();
                return true;

            case MouseFlags.WheeledRight:
                Viewport = Viewport with {X = Viewport.X + 1};
                EnsureValidScrollOffsets ();

                //SetNeedsDraw ();
                return true;

            case MouseFlags.WheeledLeft:
                Viewport = Viewport with { X = Viewport.X - 1};
                EnsureValidScrollOffsets ();

                //SetNeedsDraw ();
                return true;
        }

        int boundsX = me.Position!.Value.X;
        int boundsY = me.Position!.Value.Y;

        if (me.Flags.HasFlag (MouseFlags.LeftButtonClicked))
        {
            Point? hit = ScreenToCell (boundsX, boundsY);

            if (hit is { })
            {
                if (MultiSelect && HasControlOrAlt (me))
                {
                    UnionSelection (hit.Value.X, hit.Value.Y);
                }
                else
                {
                    SetSelection (hit.Value.X, hit.Value.Y, me.Flags.HasFlag (MouseFlags.Shift));
                }

                Update ();
            }
        }

        // Double clicking a cell activates
        if (me.Flags != MouseFlags.LeftButtonDoubleClicked)
        {
            return me.Handled;
        }

        Point? clickedCell = ScreenToCell (boundsX, boundsY);

        if (clickedCell is not { })
        {
            return me.Handled;
        }

        return OnCellActivated (new CellActivatedEventArgs (Table, clickedCell.Value.X, clickedCell.Value.Y));
    }
}

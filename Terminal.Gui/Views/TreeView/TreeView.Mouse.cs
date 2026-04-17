// This code is based on http://objectlistview.sourceforge.net (GPLv3 tree/list controls
// by phillip.piper@gmail.com). Phillip has explicitly granted permission for his design
// and code to be used in this library under the MIT license.

#nullable disable

namespace Terminal.Gui.Views;

public partial class TreeView<T>
{
    // BUGBUG: OnMouseEvent is internal. TreeView should not be overriding.
    ///<inheritdoc/>
    protected override bool OnMouseEvent (Mouse me)
    {
        // If it is not an event we care about
        if (me is { IsSingleClicked: false, IsPressed: false, IsReleased: false, IsWheel: false }
            && !me.Flags.HasFlag (ObjectActivationButton ?? MouseFlags.LeftButtonDoubleClicked))
        {
            // do nothing
            return false;
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        if (me.Flags == MouseFlags.WheeledDown)
        {
            ScrollDown ();

            return true;
        }

        if (me.Flags == MouseFlags.WheeledUp)
        {
            ScrollUp ();

            return true;
        }

        if (me.Flags == MouseFlags.WheeledRight)
        {
            ScrollOffsetHorizontal++;
            SetNeedsDraw ();

            return true;
        }

        if (me.Flags == MouseFlags.WheeledLeft)
        {
            ScrollOffsetHorizontal--;
            SetNeedsDraw ();

            return true;
        }

        if (me.Flags.FastHasFlags (MouseFlags.LeftButtonClicked))
        {
            // The line they clicked on a branch
            Branch<T> clickedBranch = HitTest (me.Position!.Value.Y);

            if (clickedBranch is null)
            {
                return false;
            }

            bool isExpandToggleAttempt = clickedBranch.IsHitOnExpandableSymbol (me.Position!.Value.X);

            // If we are already selected (double click)
            if (Equals (SelectedObject, clickedBranch.Model))
            {
                isExpandToggleAttempt = true;
            }

            // if they clicked on the +/- expansion symbol
            if (isExpandToggleAttempt)
            {
                if (clickedBranch.IsExpanded)
                {
                    clickedBranch.Collapse ();
                    InvalidateLineMap ();
                }
                else if (clickedBranch.CanExpand ())
                {
                    clickedBranch.Expand ();
                    InvalidateLineMap ();
                }
                else
                {
                    SelectedObject = clickedBranch.Model; // It is a leaf node
                    _multiSelectedRegions.Clear ();
                }
            }
            else
            {
                // It is a first click somewhere in the current line that doesn't look like an expansion/collapse attempt
                SelectedObject = clickedBranch.Model;
                _multiSelectedRegions.Clear ();
            }

            SetNeedsDraw ();

            return true;
        }

        // If it is activation via mouse (e.g. double click)
        if (!ObjectActivationButton.HasValue || !me.Flags.HasFlag (ObjectActivationButton.Value))
        {
            return false;
        }

        // The line they clicked on a branch
        Branch<T> activatedBranch = HitTest (me.Position!.Value.Y);

        if (activatedBranch is null)
        {
            return false;
        }

        // Double click changes the selection to the clicked node as well as triggering
        // activation otherwise it feels wierd
        SelectedObject = activatedBranch.Model;
        SetNeedsDraw ();

        // trigger activation event
        OnObjectActivated (new ObjectActivatedEventArgs<T> (this, activatedBranch.Model));

        // mouse event is handled.
        return true;
    }

    /// <summary>Returns the branch at the given <paramref name="y"/> client coordinate e.g. following a click event.</summary>
    /// <param name="y">Client Y position in the controls bounds.</param>
    /// <returns>The clicked branch or null if outside of tree region.</returns>
    private Branch<T> HitTest (int y)
    {
        IReadOnlyCollection<Branch<T>> map = BuildLineMap ();

        int idx = y + ScrollOffsetVertical;

        // click is outside any visible nodes
        if (idx < 0 || idx >= map.Count)
        {
            return null;
        }

        // The line they clicked on
        return map.ElementAt (idx);
    }
}

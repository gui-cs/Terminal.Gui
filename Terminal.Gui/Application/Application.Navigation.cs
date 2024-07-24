#nullable enable
namespace Terminal.Gui;

internal static class ApplicationNavigation
{
    /// <summary>
    ///    Gets the deepest focused subview of the specified <paramref name="view"/>.
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    internal static View? GetDeepestFocusedSubview (View? view)
    {
        if (view is null)
        {
            return null;
        }

        foreach (View v in view.Subviews)
        {
            if (v.HasFocus)
            {
                return GetDeepestFocusedSubview (v);
            }
        }

        return view;
    }

    /// <summary>
    ///    Sets the focus to the next view in the <see cref="View.TabIndexes"/> list. If the last view is focused, the first view is focused.
    /// </summary>
    /// <param name="viewsInTabIndexes"></param>
    /// <param name="direction"></param>
    internal static void FocusNearestView (IEnumerable<View>? viewsInTabIndexes, View.NavigationDirection direction)
    {
        if (viewsInTabIndexes is null)
        {
            return;
        }

        var found = false;
        var focusProcessed = false;
        var idx = 0;

        foreach (View v in viewsInTabIndexes)
        {
            if (v == Application.Current)
            {
                found = true;
            }

            if (found && v != Application.Current)
            {
                if (direction == View.NavigationDirection.Forward)
                {
                    Application.Current!.SuperView?.FocusNext ();
                }
                else
                {
                    Application.Current!.SuperView?.FocusPrev ();
                }

                focusProcessed = true;

                if (Application.Current.SuperView?.Focused is { } && Application.Current.SuperView.Focused != Application.Current)
                {
                    return;
                }
            }
            else if (found && !focusProcessed && idx == viewsInTabIndexes.Count () - 1)
            {
                viewsInTabIndexes.ToList () [0].SetFocus ();
            }

            idx++;
        }
    }
    /// <summary>
    ///     Moves the focus to 
    /// </summary>
    internal static void MoveNextView ()
    {
        View? old = GetDeepestFocusedSubview (Application.Current!.Focused);

        if (!Application.Current.FocusNext ())
        {
            Application.Current.FocusNext ();
        }

        if (old != Application.Current.Focused && old != Application.Current.Focused?.Focused)
        {
            old?.SetNeedsDisplay ();
            Application.Current.Focused?.SetNeedsDisplay ();
        }
        else
        {
            FocusNearestView (Application.Current.SuperView?.TabIndexes, View.NavigationDirection.Forward);
        }
    }

    internal static void MoveNextViewOrTop ()
    {
        if (ApplicationOverlapped.OverlappedTop is null)
        {
            Toplevel? top = Application.Current!.Modal ? Application.Current : Application.Top;
            top!.FocusNext ();

            if (top.Focused is null)
            {
                top.FocusNext ();
            }

            top.SetNeedsDisplay ();
            ApplicationOverlapped.BringOverlappedTopToFront ();
        }
        else
        {
            ApplicationOverlapped.OverlappedMoveNext ();
        }
    }

    internal static void MovePreviousView ()
    {
        View? old = GetDeepestFocusedSubview (Application.Current!.Focused);

        if (!Application.Current.FocusPrev ())
        {
            Application.Current.FocusPrev ();
        }

        if (old != Application.Current.Focused && old != Application.Current.Focused?.Focused)
        {
            old?.SetNeedsDisplay ();
            Application.Current.Focused?.SetNeedsDisplay ();
        }
        else
        {
            FocusNearestView (Application.Current.SuperView?.TabIndexes?.Reverse (), View.NavigationDirection.Backward);
        }
    }

    internal static void MovePreviousViewOrTop ()
    {
        if (ApplicationOverlapped.OverlappedTop is null)
        {
            Toplevel? top = Application.Current!.Modal ? Application.Current : Application.Top;
            top!.FocusPrev ();

            if (top.Focused is null)
            {
                top.FocusPrev ();
            }

            top.SetNeedsDisplay ();
            ApplicationOverlapped.BringOverlappedTopToFront ();
        }
        else
        {
            ApplicationOverlapped.OverlappedMovePrevious ();
        }
    }
}

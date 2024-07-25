#nullable enable
using System.Security.Cryptography;

namespace Terminal.Gui;

/// <summary>
///     Helper class for <see cref="Application"/> navigation.
/// </summary>
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
    internal static void FocusNearestView (IEnumerable<View>? viewsInTabIndexes, NavigationDirection direction)
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
                Application.Current!.SuperView?.AdvanceFocus (direction);

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
    ///     Moves the focus to the next view. Honors <see cref="ViewArrangement.Overlapped"/> and will only move to the next subview
    ///     if the current and next subviews are not overlapped.
    /// </summary>
    internal static void MoveNextView ()
    {
        View? old = GetDeepestFocusedSubview (Application.Current!.Focused);

        if (!Application.Current.AdvanceFocus (NavigationDirection.Forward))
        {
            Application.Current.AdvanceFocus (NavigationDirection.Forward);
        }

        if (old != Application.Current.Focused && old != Application.Current.Focused?.Focused)
        {
            old?.SetNeedsDisplay ();
            Application.Current.Focused?.SetNeedsDisplay ();
        }
        else
        {
            FocusNearestView (Application.Current.SuperView?.TabIndexes, NavigationDirection.Forward);
        }
    }

    /// <summary>
    ///     Moves the focus to the next <see cref="Toplevel"/> subview or the next subview that has <see cref="ApplicationOverlapped.OverlappedTop"/> set.
    /// </summary>
    internal static void MoveNextViewOrTop ()
    {
        if (ApplicationOverlapped.OverlappedTop is null)
        {
            Toplevel? top = Application.Current!.Modal ? Application.Current : Application.Top;

            if (!Application.Current.AdvanceFocus (NavigationDirection.Forward))
            {
                Application.Current.AdvanceFocus (NavigationDirection.Forward);
            }

            if (top != Application.Current.Focused && top != Application.Current.Focused?.Focused)
            {
                top?.SetNeedsDisplay ();
                Application.Current.Focused?.SetNeedsDisplay ();
            }
            else
            {
                FocusNearestView (Application.Current.SuperView?.TabIndexes, NavigationDirection.Forward);
            }



            //top!.AdvanceFocus (NavigationDirection.Forward);

            //if (top.Focused is null)
            //{
            //    top.AdvanceFocus (NavigationDirection.Forward);
            //}

            //top.SetNeedsDisplay ();
            ApplicationOverlapped.BringOverlappedTopToFront ();
        }
        else
        {
            ApplicationOverlapped.OverlappedMoveNext ();
        }
    }

    /// <summary>
    ///     Moves the focus to the next view. Honors <see cref="ViewArrangement.Overlapped"/> and will only move to the next subview
    ///     if the current and next subviews are not overlapped.
    /// </summary>
    internal static void MovePreviousView ()
    {
        View? old = GetDeepestFocusedSubview (Application.Current!.Focused);

        if (!Application.Current.AdvanceFocus (NavigationDirection.Backward))
        {
            Application.Current.AdvanceFocus (NavigationDirection.Backward);
        }

        if (old != Application.Current.Focused && old != Application.Current.Focused?.Focused)
        {
            old?.SetNeedsDisplay ();
            Application.Current.Focused?.SetNeedsDisplay ();
        }
        else
        {
            FocusNearestView (Application.Current.SuperView?.TabIndexes?.Reverse (), NavigationDirection.Backward);
        }
    }

    internal static void MovePreviousViewOrTop ()
    {
        if (ApplicationOverlapped.OverlappedTop is null)
        {
            Toplevel? top = Application.Current!.Modal ? Application.Current : Application.Top;
            top!.AdvanceFocus (NavigationDirection.Backward);

            if (top.Focused is null)
            {
                top.AdvanceFocus (NavigationDirection.Backward);
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

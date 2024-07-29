#nullable enable
using System.Diagnostics;
using System.Reflection.PortableExecutable;
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
    ///     Moves the focus to the next focusable view.
    ///     Honors <see cref="ViewArrangement.Overlapped"/> and will only move to the next subview
    ///     if the current and next subviews are not overlapped.
    /// </summary>
    internal static void MoveNextView ()
    {
        View? old = GetDeepestFocusedSubview (Application.Current!.Focused);

        if (!Application.Current.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop))
        {
            Application.Current.AdvanceFocus (NavigationDirection.Forward, null);
        }

        if (old != Application.Current.Focused && old != Application.Current.Focused?.Focused)
        {
            old?.SetNeedsDisplay ();
            Application.Current.Focused?.SetNeedsDisplay ();
        }
        else
        {
            ApplicationOverlapped.SetFocusToNextViewWithWrap (Application.Current.SuperView?.TabIndexes, NavigationDirection.Forward);
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

            if (!Application.Current.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup))
            {
                Application.Current.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop);

                if (Application.Current.Focused is null)
                {
                    Application.Current.RestoreFocus ();
                }
            }

            if (top != Application.Current.Focused && top != Application.Current.Focused?.Focused)
            {
                top?.SetNeedsDisplay ();
                Application.Current.Focused?.SetNeedsDisplay ();
            }
            else
            {
                ApplicationOverlapped.SetFocusToNextViewWithWrap (Application.Current.SuperView?.TabIndexes, NavigationDirection.Forward);
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

    // TODO: These methods should return bool to indicate if the focus was moved or not.

    /// <summary>
    ///     Moves the focus to the next view. Honors <see cref="ViewArrangement.Overlapped"/> and will only move to the next subview
    ///     if the current and next subviews are not overlapped.
    /// </summary>
    internal static void MovePreviousView ()
    {
        View? old = GetDeepestFocusedSubview (Application.Current!.Focused);

        if (!Application.Current.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop))
        {
            Application.Current.AdvanceFocus (NavigationDirection.Backward, null);
        }

        if (old != Application.Current.Focused && old != Application.Current.Focused?.Focused)
        {
            old?.SetNeedsDisplay ();
            Application.Current.Focused?.SetNeedsDisplay ();
        }
        else
        {
            ApplicationOverlapped.SetFocusToNextViewWithWrap (Application.Current.SuperView?.TabIndexes?.Reverse (), NavigationDirection.Backward);
        }
    }

    internal static void MovePreviousViewOrTop ()
    {
        if (ApplicationOverlapped.OverlappedTop is null)
        {
            Toplevel? top = Application.Current!.Modal ? Application.Current : Application.Top;
            top!.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabGroup);

            if (top.Focused is null)
            {
                top.AdvanceFocus (NavigationDirection.Backward, null);
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

#nullable enable

namespace Terminal.Gui;

/// <summary>
///     Helper class for <see cref="Application"/> navigation. Held by <see cref="Application.Navigation"/>
/// </summary>
public class ApplicationNavigation
{

    /// <summary>
    ///     Initializes a new instance of the <see cref="ApplicationNavigation"/> class.
    /// </summary>
    public ApplicationNavigation ()
    {
        // TODO: Move navigation key bindings here from AddApplicationKeyBindings
    }

    private View? _focused = null;

    /// <summary>
    ///     Gets the most focused <see cref="View"/> in the application, if there is one.
    /// </summary>
    public View? GetFocused () { return _focused; }

    /// <summary>
    ///     INTERNAL method to record the most focused <see cref="View"/> in the application.
    /// </summary>
    /// <remarks>
    ///     Raises <see cref="FocusedChanged"/>.
    /// </remarks>
    internal void SetFocused (View? value)
    {
        if (_focused == value)
        {
            return;
        }

        _focused = value;

        FocusedChanged?.Invoke (null, EventArgs.Empty);

        return;
    }

    /// <summary>
    ///     Raised when the most focused <see cref="View"/> in the application has changed.
    /// </summary>
    public event EventHandler<EventArgs>? FocusedChanged;


    /// <summary>
    ///     Gets whether <paramref name="view"/> is in the Subview hierarchy of <paramref name="start"/>.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="view"></param>
    /// <returns></returns>
    public static bool IsInHierarchy (View start, View? view)
    {
        if (view is null)
        {
            return false;
        }

        if (view == start)
        {
            return true;
        }

        foreach (View subView in start.Subviews)
        {
            if (view == subView)
            {
                return true;
            }

            var found = IsInHierarchy (subView, view);
            if (found)
            {
                return found;
            }
        }

        return false;
    }


    /// <summary>
    ///     Gets the deepest focused subview of the specified <paramref name="view"/>.
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
    ///     Moves the focus to the next <see cref="Toplevel"/> subview or the next subview that has
    ///     <see cref="ApplicationOverlapped.OverlappedTop"/> set.
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
    ///     Moves the focus to the next view. Honors <see cref="ViewArrangement.Overlapped"/> and will only move to the next
    ///     subview
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

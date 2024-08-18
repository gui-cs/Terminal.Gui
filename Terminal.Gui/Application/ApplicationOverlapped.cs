#nullable enable
using System.Diagnostics;
using System.Reflection;

namespace Terminal.Gui;

/// <summary>
/// Helper class for managing overlapped views in the application.
/// </summary>
public static class ApplicationOverlapped
{

    /// <summary>
    ///     Gets or sets if <paramref name="top"/> is in overlapped mode within a Toplevel container.
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    public static bool IsOverlapped (Toplevel? top)
    {
        return ApplicationOverlapped.OverlappedTop is { } && ApplicationOverlapped.OverlappedTop != top && !top!.Modal;
    }

    /// <summary>
    ///     Gets the list of the Overlapped children which are not modal <see cref="Toplevel"/> from the
    ///     <see cref="OverlappedTop"/>.
    /// </summary>
    public static List<Toplevel>? OverlappedChildren
    {
        get
        {
            if (OverlappedTop is { })
            {
                List<Toplevel> overlappedChildren = new ();

                lock (Application.TopLevels)
                {
                    foreach (Toplevel top in Application.TopLevels)
                    {
                        if (top != OverlappedTop && !top.Modal)
                        {
                            overlappedChildren.Add (top);
                        }
                    }
                }

                return overlappedChildren;
            }

            return null;
        }
    }

    /// <summary>
    ///     The <see cref="Toplevel"/> object used for the application on startup which
    ///     <see cref="Toplevel.IsOverlappedContainer"/> is true.
    /// </summary>
    public static Toplevel? OverlappedTop
    {
        get
        {
            if (Application.Top is { IsOverlappedContainer: true })
            {
                return Application.Top;
            }

            return null;
        }
    }

    /// <summary>Brings the superview of the most focused overlapped view is on front.</summary>
    public static void BringOverlappedTopToFront ()
    {
        if (OverlappedTop is { })
        {
            return;
        }

        View? top = FindTopFromView (Application.Top?.MostFocused);

        if (top is Toplevel && Application.Top?.Subviews.Count > 1 && Application.Top.Subviews [^1] != top)
        {
            Application.Top.BringSubviewToFront (top);
        }
    }

    /// <summary>Gets the current visible Toplevel overlapped child that matches the arguments pattern.</summary>
    /// <param name="type">The type.</param>
    /// <param name="exclude">The strings to exclude.</param>
    /// <returns>The matched view.</returns>
    public static Toplevel? GetTopOverlappedChild (Type? type = null, string []? exclude = null)
    {
        if (OverlappedChildren is null || OverlappedTop is null)
        {
            return null;
        }

        foreach (Toplevel top in OverlappedChildren)
        {
            if (type is { } && top.GetType () == type && exclude?.Contains (top.Data.ToString ()) == false)
            {
                return top;
            }

            if ((type is { } && top.GetType () != type) || exclude?.Contains (top.Data.ToString ()) == true)
            {
                continue;
            }

            return top;
        }

        return null;
    }


    /// <summary>
    /// Sets the focus to the next view in the specified direction within the provided list of views.
    /// If the end of the list is reached, the focus wraps around to the first view in the list.
    /// The method considers the current focused view (`Application.Current`) and attempts to move the focus
    /// to the next view in the specified direction. If the focus cannot be set to the next view, it wraps around
    /// to the first view in the list.
    /// </summary>
    /// <param name="viewsInTabIndexes"></param>
    /// <param name="direction"></param>
    internal static void SetFocusToNextViewWithWrap (IEnumerable<View>? viewsInTabIndexes, NavigationDirection direction)
    {
        if (viewsInTabIndexes is null)
        {
            return;
        }

        // This code-path only executes in obtuse IsOverlappedContainer scenarios.
        Debug.Assert (Application.Current!.IsOverlappedContainer);

        bool foundCurrentView = false;
        bool focusSet = false;
        IEnumerable<View> indexes = viewsInTabIndexes as View [] ?? viewsInTabIndexes.ToArray ();
        int viewCount = indexes.Count ();
        int currentIndex = 0;

        foreach (View view in indexes)
        {
            if (view == Application.Current)
            {
                foundCurrentView = true;
            }
            else if (foundCurrentView && !focusSet)
            {
                // One of the views is Current, but view is not. Attempt to Advance...
                Application.Current!.SuperView?.AdvanceFocus (direction, null);
                // QUESTION: AdvanceFocus returns false AND sets Focused to null if no view was found to advance to. Should't we only set focusProcessed if it returned true?
                focusSet = true;

                if (Application.Current.SuperView?.Focused != Application.Current)
                {
                    return;
                }

                // Either AdvanceFocus didn't set focus or the view it set focus to is not current...
                // continue...
            }

            currentIndex++;

            if (foundCurrentView && !focusSet && currentIndex == viewCount)
            {
                // One of the views is Current AND AdvanceFocus didn't set focus AND we are at the last view in the list...
                // This means we should wrap around to the first view in the list.
                indexes.First ().SetFocus ();
            }
        }
    }

    /// <summary>
    ///     Move to the next Overlapped child from the <see cref="OverlappedTop"/> and set it as the <see cref="Application.Top"/> if
    ///     it is not already.
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    public static bool MoveToOverlappedChild (Toplevel? top)
    {
        ArgumentNullException.ThrowIfNull (top);

        if (top.Visible && OverlappedTop is { } && Application.Current?.Modal == false)
        {
            lock (Application.TopLevels)
            {
                Application.TopLevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
                Application.Current = top;
            }

            return true;
        }

        return false;
    }

    /// <summary>Move to the next Overlapped child from the <see cref="OverlappedTop"/>.</summary>
    public static void OverlappedMoveNext ()
    {
        if (OverlappedTop is { } && !Application.Current!.Modal)
        {
            lock (Application.TopLevels)
            {
                Application.TopLevels.MoveNext ();
                var isOverlapped = false;

                while (Application.TopLevels.Peek () == OverlappedTop || !Application.TopLevels.Peek ().Visible)
                {
                    if (!isOverlapped && Application.TopLevels.Peek () == OverlappedTop)
                    {
                        isOverlapped = true;
                    }
                    else if (isOverlapped && Application.TopLevels.Peek () == OverlappedTop)
                    {
                        MoveCurrent (Application.Top!);

                        break;
                    }

                    Application.TopLevels.MoveNext ();
                }

                Application.Current = Application.TopLevels.Peek ();
            }
        }
    }

    /// <summary>Move to the previous Overlapped child from the <see cref="OverlappedTop"/>.</summary>
    public static void OverlappedMovePrevious ()
    {
        if (OverlappedTop is { } && !Application.Current!.Modal)
        {
            lock (Application.TopLevels)
            {
                Application.TopLevels.MovePrevious ();
                var isOverlapped = false;

                while (Application.TopLevels.Peek () == OverlappedTop || !Application.TopLevels.Peek ().Visible)
                {
                    if (!isOverlapped && Application.TopLevels.Peek () == OverlappedTop)
                    {
                        isOverlapped = true;
                    }
                    else if (isOverlapped && Application.TopLevels.Peek () == OverlappedTop)
                    {
                        MoveCurrent (Application.Top!);

                        break;
                    }

                    Application.TopLevels.MovePrevious ();
                }

                 Application.Current = Application.TopLevels.Peek ();
            }
        }
    }

    internal static bool OverlappedChildNeedsDisplay ()
    {
        if (OverlappedTop is null)
        {
            return false;
        }

        lock (Application.TopLevels)
        {
            foreach (Toplevel top in Application.TopLevels)
            {
                if (top != Application.Current && top.Visible && (top.NeedsDisplay || top.SubViewNeedsDisplay || top.LayoutNeeded))
                {
                    OverlappedTop.SetSubViewNeedsDisplay ();

                    return true;
                }
            }
        }

        return false;
    }

    internal static bool SetCurrentOverlappedAsTop ()
    {
        if (OverlappedTop is null && Application.Current != Application.Top && Application.Current?.SuperView is null && Application.Current?.Modal == false)
        {
            Application.Top = Application.Current;

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Finds the first Toplevel in the stack that is Visible and who's Frame contains the <paramref name="location"/>.
    /// </summary>
    /// <param name="start"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    internal static Toplevel? FindDeepestTop (Toplevel start, in Point location)
    {
        if (!start.Frame.Contains (location))
        {
            return null;
        }

        lock (Application.TopLevels)
        {
            if (Application.TopLevels is not { Count: > 0 })
            {
                return start;
            }

            int rx = location.X - start.Frame.X;
            int ry = location.Y - start.Frame.Y;

            foreach (Toplevel t in Application.TopLevels)
            {
                if (t == Application.Current)
                {
                    continue;
                }

                if (t != start && t.Visible && t.Frame.Contains (rx, ry))
                {
                    start = t;

                    break;
                }
            }
        }

        return start;
    }

    /// <summary>
    ///     Given <paramref name="view"/>, returns the first Superview up the chain that is <see cref="Application.Top"/>.
    /// </summary>
    internal static View? FindTopFromView (View? view)
    {
        if (view is null)
        {
            return null;
        }

        View top = view.SuperView is { } && view.SuperView != Application.Top
                       ? view.SuperView
                       : view;

        while (top?.SuperView is { } && top?.SuperView != Application.Top)
        {
            top = top!.SuperView;
        }

        return top;
    }

    /// <summary>
    ///     If the <see cref="Application.Current"/> is not the <paramref name="top"/> then <paramref name="top"/> is moved to the top of
    ///     the Toplevel stack and made Current.
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    internal static bool MoveCurrent (Toplevel top)
    {
        // The Current is modal and the top is not modal Toplevel then
        // the Current must be moved above the first not modal Toplevel.
        if (OverlappedTop is { }
            && top != OverlappedTop
            && top != Application.Current
            && Application.Current?.Modal == true
            && !Application.TopLevels.Peek ().Modal)
        {
            lock (Application.TopLevels)
            {
                Application.TopLevels.MoveTo (Application.Current, 0, new ToplevelEqualityComparer ());
            }

            var index = 0;
            Toplevel [] savedToplevels = Application.TopLevels.ToArray ();

            foreach (Toplevel t in savedToplevels)
            {
                if (!t!.Modal && t != Application.Current && t != top && t != savedToplevels [index])
                {
                    lock (Application.TopLevels)
                    {
                        Application.TopLevels.MoveTo (top, index, new ToplevelEqualityComparer ());
                    }
                }

                index++;
            }

            return false;
        }

        // The Current and the top are both not running Toplevel then
        // the top must be moved above the first not running Toplevel.
        if (OverlappedTop is { }
            && top != OverlappedTop
            && top != Application.Current
            && Application.Current?.Running == false
            && top?.Running == false)
        {
            lock (Application.TopLevels)
            {
                Application.TopLevels.MoveTo (Application.Current, 0, new ToplevelEqualityComparer ());
            }

            var index = 0;

            foreach (Toplevel t in Application.TopLevels.ToArray ())
            {
                if (!t.Running && t != Application.Current && index > 0)
                {
                    lock (Application.TopLevels)
                    {
                        Application.TopLevels.MoveTo (top, index - 1, new ToplevelEqualityComparer ());
                    }
                }

                index++;
            }

            return false;
        }

        if ((OverlappedTop is { } && top?.Modal == true && Application.TopLevels.Peek () != top)
            || (OverlappedTop is { } && Application.Current != OverlappedTop && Application.Current?.Modal == false && top == OverlappedTop)
            || (OverlappedTop is { } && Application.Current?.Modal == false && top != Application.Current)
            || (OverlappedTop is { } && Application.Current?.Modal == true && top == OverlappedTop))
        {
            lock (Application.TopLevels)
            {
                Application.TopLevels.MoveTo (top!, 0, new ToplevelEqualityComparer ());
                Application.Current = top;
            }
        }

        return true;
    }
}

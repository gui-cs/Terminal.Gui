#nullable enable
namespace Terminal.Gui;

public static partial class Application
{
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

                lock (_topLevels)
                {
                    foreach (Toplevel top in _topLevels)
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
            if (Top is { IsOverlappedContainer: true })
            {
                return Top;
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

        View? top = FindTopFromView (Top?.MostFocused);

        if (top is Toplevel && Top?.Subviews.Count > 1 && Top.Subviews [^1] != top)
        {
            Top.BringSubviewToFront (top);
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
    ///     Move to the next Overlapped child from the <see cref="OverlappedTop"/> and set it as the <see cref="Top"/> if
    ///     it is not already.
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    public static bool MoveToOverlappedChild (Toplevel top)
    {
        if (top.Visible && OverlappedTop is { } && Current?.Modal == false)
        {
            lock (_topLevels)
            {
                _topLevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
                Current = top;
            }

            return true;
        }

        return false;
    }

    /// <summary>Move to the next Overlapped child from the <see cref="OverlappedTop"/>.</summary>
    public static void OverlappedMoveNext ()
    {
        if (OverlappedTop is { } && !Current!.Modal)
        {
            lock (_topLevels)
            {
                _topLevels.MoveNext ();
                var isOverlapped = false;

                while (_topLevels.Peek () == OverlappedTop || !_topLevels.Peek ().Visible)
                {
                    if (!isOverlapped && _topLevels.Peek () == OverlappedTop)
                    {
                        isOverlapped = true;
                    }
                    else if (isOverlapped && _topLevels.Peek () == OverlappedTop)
                    {
                        MoveCurrent (Top!);

                        break;
                    }

                    _topLevels.MoveNext ();
                }

                Current = _topLevels.Peek ();
            }
        }
    }

    /// <summary>Move to the previous Overlapped child from the <see cref="OverlappedTop"/>.</summary>
    public static void OverlappedMovePrevious ()
    {
        if (OverlappedTop is { } && !Current!.Modal)
        {
            lock (_topLevels)
            {
                _topLevels.MovePrevious ();
                var isOverlapped = false;

                while (_topLevels.Peek () == OverlappedTop || !_topLevels.Peek ().Visible)
                {
                    if (!isOverlapped && _topLevels.Peek () == OverlappedTop)
                    {
                        isOverlapped = true;
                    }
                    else if (isOverlapped && _topLevels.Peek () == OverlappedTop)
                    {
                        MoveCurrent (Top!);

                        break;
                    }

                    _topLevels.MovePrevious ();
                }

                Current = _topLevels.Peek ();
            }
        }
    }

    private static bool OverlappedChildNeedsDisplay ()
    {
        if (OverlappedTop is null)
        {
            return false;
        }

        lock (_topLevels)
        {
            foreach (Toplevel top in _topLevels)
            {
                if (top != Current && top.Visible && (top.NeedsDisplay || top.SubViewNeedsDisplay || top.LayoutNeeded))
                {
                    OverlappedTop.SetSubViewNeedsDisplay ();

                    return true;
                }
            }
        }

        return false;
    }

    private static bool SetCurrentOverlappedAsTop ()
    {
        if (OverlappedTop is null && Current != Top && Current?.SuperView is null && Current?.Modal == false)
        {
            Top = Current;

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
    private static Toplevel? FindDeepestTop (Toplevel start, in Point location)
    {
        if (!start.Frame.Contains (location))
        {
            return null;
        }

        lock (_topLevels)
        {
            if (_topLevels is not { Count: > 0 })
            {
                return start;
            }

            int rx = location.X - start.Frame.X;
            int ry = location.Y - start.Frame.Y;

            foreach (Toplevel t in _topLevels)
            {
                if (t == Current)
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
    ///     Given <paramref name="view"/>, returns the first Superview up the chain that is <see cref="Top"/>.
    /// </summary>
    private static View? FindTopFromView (View? view)
    {
        if (view is null)
        {
            return null;
        }

        View top = view.SuperView is { } && view.SuperView != Top
                       ? view.SuperView
                       : view;

        while (top?.SuperView is { } && top?.SuperView != Top)
        {
            top = top!.SuperView;
        }

        return top;
    }

    /// <summary>
    ///     If the <see cref="Current"/> is not the <paramref name="top"/> then <paramref name="top"/> is moved to the top of
    ///     the Toplevel stack and made Current.
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    private static bool MoveCurrent (Toplevel top)
    {
        // The Current is modal and the top is not modal Toplevel then
        // the Current must be moved above the first not modal Toplevel.
        if (OverlappedTop is { }
            && top != OverlappedTop
            && top != Current
            && Current?.Modal == true
            && !_topLevels.Peek ().Modal)
        {
            lock (_topLevels)
            {
                _topLevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
            }

            var index = 0;
            Toplevel [] savedToplevels = _topLevels.ToArray ();

            foreach (Toplevel t in savedToplevels)
            {
                if (!t!.Modal && t != Current && t != top && t != savedToplevels [index])
                {
                    lock (_topLevels)
                    {
                        _topLevels.MoveTo (top, index, new ToplevelEqualityComparer ());
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
            && top != Current
            && Current?.Running == false
            && top?.Running == false)
        {
            lock (_topLevels)
            {
                _topLevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
            }

            var index = 0;

            foreach (Toplevel t in _topLevels.ToArray ())
            {
                if (!t.Running && t != Current && index > 0)
                {
                    lock (_topLevels)
                    {
                        _topLevels.MoveTo (top, index - 1, new ToplevelEqualityComparer ());
                    }
                }

                index++;
            }

            return false;
        }

        if ((OverlappedTop is { } && top?.Modal == true && _topLevels.Peek () != top)
            || (OverlappedTop is { } && Current != OverlappedTop && Current?.Modal == false && top == OverlappedTop)
            || (OverlappedTop is { } && Current?.Modal == false && top != Current)
            || (OverlappedTop is { } && Current?.Modal == true && top == OverlappedTop))
        {
            lock (_topLevels)
            {
                _topLevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
                Current = top;
            }
        }

        return true;
    }
}

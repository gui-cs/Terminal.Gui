namespace Terminal.Gui;

public static partial class Application // Toplevel handling
{
    /// <summary>Holds the stack of TopLevel views.</summary>

    // BUGBUG: Technically, this is not the full lst of TopLevels. There be dragons here, e.g. see how Toplevel.Id is used. What
    // about TopLevels that are just a SubView of another View?
    internal static readonly Stack<Toplevel> _topLevels = new ();

    /// <summary>The <see cref="Toplevel"/> object used for the application on startup (<seealso cref="Top"/>)</summary>
    /// <value>The top.</value>
    public static Toplevel Top { get; private set; }

    /// <summary>
    ///     The current <see cref="Toplevel"/> object. This is updated in <see cref="Application.Begin"/> enters and leaves to
    ///     point to the current
    ///     <see cref="Toplevel"/> .
    /// </summary>
    /// <remarks>
    ///     Only relevant in scenarios where <see cref="Toplevel.IsOverlappedContainer"/> is <see langword="true"/>.
    /// </remarks>
    /// <value>The current.</value>
    public static Toplevel Current { get; private set; }

    private static void EnsureModalOrVisibleAlwaysOnTop (Toplevel topLevel)
    {
        if (!topLevel.Running
            || (topLevel == Current && topLevel.Visible)
            || OverlappedTop == null
            || _topLevels.Peek ().Modal)
        {
            return;
        }

        foreach (Toplevel top in _topLevels.Reverse ())
        {
            if (top.Modal && top != Current)
            {
                MoveCurrent (top);

                return;
            }
        }

        if (!topLevel.Visible && topLevel == Current)
        {
            OverlappedMoveNext ();
        }
    }

    private static Toplevel FindDeepestTop (Toplevel start, in Point location)
    {
        if (!start.Frame.Contains (location))
        {
            return null;
        }

        if (_topLevels is { Count: > 0 })
        {
            int rx = location.X - start.Frame.X;
            int ry = location.Y - start.Frame.Y;

            foreach (Toplevel t in _topLevels)
            {
                if (t != Current)
                {
                    if (t != start && t.Visible && t.Frame.Contains (rx, ry))
                    {
                        start = t;

                        break;
                    }
                }
            }
        }

        return start;
    }

    private static View FindTopFromView (View view)
    {
        View top = view?.SuperView is { } && view?.SuperView != Top
                       ? view.SuperView
                       : view;

        while (top?.SuperView is { } && top?.SuperView != Top)
        {
            top = top.SuperView;
        }

        return top;
    }

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

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    /// <remarks>
    ///     Event handlers can set <see cref="SizeChangedEventArgs.Cancel"/> to <see langword="true"/> to prevent
    ///     <see cref="Application"/> from changing it's size to match the new terminal size.
    /// </remarks>
    public static event EventHandler<SizeChangedEventArgs> SizeChanging;

    /// <summary>
    ///     Called when the application's size changes. Sets the size of all <see cref="Toplevel"/>s and fires the
    ///     <see cref="SizeChanging"/> event.
    /// </summary>
    /// <param name="args">The new size.</param>
    /// <returns><see lanword="true"/>if the size was changed.</returns>
    public static bool OnSizeChanging (SizeChangedEventArgs args)
    {
        SizeChanging?.Invoke (null, args);

        if (args.Cancel || args.Size is null)
        {
            return false;
        }

        foreach (Toplevel t in _topLevels)
        {
            t.SetRelativeLayout (args.Size.Value);
            t.LayoutSubviews ();
            t.PositionToplevels ();
            t.OnSizeChanging (new (args.Size));

            if (PositionCursor (t))
            {
                Driver.UpdateCursor ();
            }
        }

        Refresh ();

        return true;
    }
}

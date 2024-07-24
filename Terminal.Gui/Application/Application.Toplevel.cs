#nullable enable
namespace Terminal.Gui;

public static partial class Application // Toplevel handling
{
    // BUGBUG: Technically, this is not the full lst of TopLevels. There be dragons here, e.g. see how Toplevel.Id is used. What
    /// <summary>Holds the stack of TopLevel views.</summary>
    // about TopLevels that are just a SubView of another View?
    internal static readonly Stack<Toplevel> _topLevels = new ();

    /// <summary>The <see cref="Toplevel"/> object used for the application on startup (<seealso cref="Top"/>)</summary>
    /// <value>The top.</value>
    public static Toplevel? Top { get; private set; }

    // TODO: Determine why this can't just return _topLevels.Peek()?
    /// <summary>
    ///     The current <see cref="Toplevel"/> object. This is updated in <see cref="Application.Begin"/> enters and leaves to
    ///     point to the current
    ///     <see cref="Toplevel"/> .
    /// </summary>
    /// <remarks>
    ///     This will only be distinct from <see cref="Application.Top"/> in scenarios where <see cref="Toplevel.IsOverlappedContainer"/> is <see langword="true"/>.
    /// </remarks>
    /// <value>The current.</value>
    public static Toplevel? Current { get; private set; }

    /// <summary>
    ///     If <paramref name="topLevel"/> is not already Current and visible, finds the last Modal Toplevel in the stack and makes it Current.
    /// </summary>
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

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    /// <remarks>
    ///     Event handlers can set <see cref="SizeChangedEventArgs.Cancel"/> to <see langword="true"/> to prevent
    ///     <see cref="Application"/> from changing it's size to match the new terminal size.
    /// </remarks>
    public static event EventHandler<SizeChangedEventArgs>? SizeChanging;

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
                Driver?.UpdateCursor ();
            }
        }

        Refresh ();

        return true;
    }
}
